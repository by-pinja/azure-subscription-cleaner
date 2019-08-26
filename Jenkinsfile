library 'jenkins-ptcs-library@0.6.1'

podTemplate(label: pod.label,
  containers: pod.templates + [
    containerTemplate(name: 'dotnet', image: 'mcr.microsoft.com/dotnet/core/sdk:3.0.100-preview8-alpine3.9', ttyEnabled: true, command: '/bin/sh -c', args: 'cat')
  ]
) {
    node(pod.label) {
        stage('Checkout') {
            checkout scm
        }
        container('dotnet') {
            stage('Build') {
                sh """
                    dotnet build src
                """
            }
            stage('Test') {
                sh """
                    dotnet test src
                """
            }
        }
    }
}