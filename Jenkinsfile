library 'jenkins-ptcs-library@2.3.0'

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

        if (isTest(branch)){
            def zipName = 'publish.zip'

            container('powershell'){
                stage('Package') {
                    sh """
                        pwsh -command "Compress-Archive -DestinationPath $zipName -Path $publishFolder/*"
                    """
                }

                toAzureTestEnv {
                    def buildNumber = (env.BUILD_NUMBER)
                    def ciRg = 'sub-cleaner-ci-' + buildNumber
                    def ciAppName = 'sub-cleaner-ci-' + buildNumber

                    stage('Create temporary Resource Group'){
                        sh """
                            pwsh -command "New-AzResourceGroup -Name '$ciRg' -Location 'North Europe' -Tag @{subproject='2026956'; Description='Continuous Integration'}"
                        """
                    }
                    stage('Create test environment'){
                        sh """
                            pwsh -command "New-AzResourceGroupDeployment -Name azure-subscription-ci -TemplateFile deployment/azuredeploy.json -ResourceGroupName $ciRg -appName $ciAppName -environment $environment"
                        """
                    }
                    try {
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
        }
    }
}
