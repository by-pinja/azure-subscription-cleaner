library 'jenkins-ptcs-library@2.3.0'

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
            }
        }
    }
}
