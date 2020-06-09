library 'jenkins-ptcs-library@3.0.0'

def isMaster(branchName) { return branchName == "master" }
def isTest(branchName) { return branchName == "test" }

podTemplate(label: pod.label,
  containers: pod.templates + [
    containerTemplate(name: 'dotnet', image: 'ptcos/multi-netcore-sdk:0.0.2', ttyEnabled: true, command: '/bin/sh -c', args: 'cat'),
    containerTemplate(name: 'powershell', image: 'azuresdk/azure-powershell-core:master', ttyEnabled: true, command: '/bin/sh -c', args: 'cat')
  ]
) {
    def branch = (env.BRANCH_NAME)
    def sourceFolder = 'src'
    def publishFolder = 'publish'
    def environment = isMaster(branch) ? 'Production' : 'Development'

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
                                    pwsh -command "New-AzResourceGroupDeployment -Name azure-subscription-ci -TemplateFile deployment/azuredeploy.json -ResourceGroupName $ciRg -appName $ciAppName -environment $environment -slackChannel 'mock_mock' -simulate ([System.Convert]::ToBoolean('true')) -slackBearerToken (ConvertTo-SecureString -String 'mocktoken' -AsPlainText -Force)"
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
                                pwsh -command "New-AzResourceGroup -Name '$productionResourceGroup' -Location 'North Europe' -Tag @{subproject='2026956'; Description='Continuous Integration'}"
                            """
                        }
                        stage('Create production environment'){
                            sh """
                                pwsh -command "New-AzResourceGroupDeployment -Name azure-subscription-cleaner -TemplateFile deployment/azuredeploy.json -ResourceGroupName $productionResourceGroup -appName $productionResourceGroup -environment $environment -slackChannel '$messageChannel' -simulate \$true -slackBearerToken (ConvertTo-SecureString -String 'mocktoken' -AsPlainText -Force)"
                            """
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
