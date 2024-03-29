library 'jenkins-ptcs-library@4.1.3'

def isMaster(branchName) { return branchName == "master" }
def isTest(branchName) { return branchName == "test" }

podTemplate(label: pod.label,
  containers: pod.templates + [
    containerTemplate(name: 'dotnet', image: 'mcr.microsoft.com/dotnet/sdk:6.0-alpine', ttyEnabled: true, command: '/bin/sh -c', args: 'cat'),
    containerTemplate(name: 'powershell', image: 'mcr.microsoft.com/azure-powershell:alpine-3.14', ttyEnabled: true, command: '/bin/sh -c', args: 'cat')
  ]
) {
    def branch = (env.BRANCH_NAME)
    def sourceFolder = 'src'
    def publishFolder = 'publish'
    def environment = isMaster(branch) ? 'Production' : 'Development'

    /*
     We want to avoid executing this during workdays so we'll execute during weekend
     {second} {minute} {hour} {day} {month} {day of the week}

     See https://docs.microsoft.com/en-us/azure/azure-functions/functions-bindings-timer?tabs=csharp#ncrontab-expressions
     for more information
    */
    def productionCleanupSchedule = '0 0 0 1-7 * SAT'

    node(pod.label) {
        stage('Checkout') {
            checkout scm
        }
        container('dotnet') {
            stage('Build') {
                sh """
                    dotnet publish $sourceFolder -c Release -o $publishFolder --version-suffix ${env.BUILD_NUMBER}
                """
            }
            stage('Test') {
                sh """
                    dotnet test $sourceFolder
                """
            }
        }

        if (isTest(branch) || isMaster(branch)) {
            def zipName = 'publish.zip'

            container('powershell'){
                stage('Package') {
                    sh """
                        pwsh -command "Compress-Archive -DestinationPath $zipName -Path $publishFolder/*"
                    """
                }

                if (isTest(branch)) {
                    toAzureTestEnv {
                        def buildNumber = (env.BUILD_NUMBER)
                        def ciRg = 'sub-cleaner-ci-' + buildNumber
                        def ciAppName = 'sub-cleaner-ci-' + buildNumber

                        stage('Create temporary Resource Group' ){
                            sh """
                                pwsh -command "New-AzResourceGroup -Name '$ciRg' -Location 'North Europe' -Tag @{subproject='2026956'; Description='Continuous Integration'}"
                            """
                        }
                        try {
                            stage('Create test environment') {
                                sh """
                                    pwsh -command "New-AzResourceGroupDeployment -Name azure-subscription-ci -TemplateFile deployment/azuredeploy.bicep -ResourceGroupName $ciRg -appName $ciAppName -environment $environment -slackChannel 'mock_mock' -simulate ([System.Convert]::ToBoolean('true')) -slackBearerToken (ConvertTo-SecureString -String 'mocktoken' -AsPlainText -Force) -cleanupSchedule '$productionCleanupSchedule'"
                                """
                            }
                            stage('Publish to test environment') {
                                sh """
                                    pwsh -command "Publish-AzWebApp -ResourceGroupName $ciRg -Name $ciAppName -ArchivePath $zipName -Force"
                                """
                            }
                        }
                        finally {
                            stage('Delete test environment'){
                                sh """
                                    pwsh -command "Remove-AzResourceGroup -Name '$ciRg' -Force"
                                """
                            }
                        }
                    }
                }
                if (isMaster(branch)) {
                    def messageChannel = 'devops'

                    // Production deployment is done to test environment because this application is supposed to clean test environment.
                    toAzureTestEnv {
                        def productionResourceGroup = 'pinja-sub-cleaner'

                        stage('Create production resource Group'){
                            sh """
                                pwsh -command "New-AzResourceGroup -Name '$productionResourceGroup' -Location 'North Europe' -Tag @{subproject='2026956'; Description='Continuous Integration'} -Force"
                            """
                        }
                        withCredentials([
                            string(credentialsId: 'azure_subscription_cleaner_slack', variable: 'SLACK_TOKEN')
                        ]){
                            stage('Create production environment'){
                                sh """
                                    pwsh -command "New-AzResourceGroupDeployment -Name azure-subscription-cleaner -TemplateFile deployment/azuredeploy.bicep -ResourceGroupName $productionResourceGroup -appName $productionResourceGroup -environment $environment -slackChannel '$messageChannel' -simulate ([System.Convert]::ToBoolean('false')) -slackBearerToken (ConvertTo-SecureString -String '$SLACK_TOKEN' -AsPlainText -Force) -cleanupSchedule '$productionCleanupSchedule'"
                                """
                            }
                        }
                        stage('Publish to production environment') {
                            sh """
                                pwsh -command "Publish-AzWebApp -ResourceGroupName $productionResourceGroup -Name $productionResourceGroup -ArchivePath $zipName -Force"
                            """
                        }
                    }
                }

            }
        }
    }
}
