@Library('github-release-helpers@v0.2.1')
def releaseInfo

def getBranchTag(env) {
  def result = env.BRANCH_NAME
  if (result.indexOf('/') > -1) {
    result = result.substring(result.lastIndexOf('/') + 1, result.length())
  }
  return (result == "main") ? "latest" : result
}

pipeline {
    agent any

    environment {
        DOCKER_REPO = 'paultrampert'
        DOCKER_REPO_CREDENTIALS = 'docker_hub'
        IMAGE_NAME = "spahost"
        BRANCH_TAG = getBranchTag(env)
        PROJECT_NAME = "PTrampert.SpaHost"
    }

    options {
        buildDiscarder(logRotator(numToKeepStr:'5'))
        timestamps()
    }

    stages {
        stage('Build Release Info') {
            steps {
                script {
                    releaseInfo = generateGithubReleaseInfo(
                        'PaulTrampert',
                        "$PROJECT_NAME",
                        'v',
                        'Github User/Pass',
                        'https://api.github.com',
                        BRANCH_NAME == "main" ? null : BRANCH_NAME,
                        env.BUILD_NUMBER
                    )

                    echo "Next version is ${releaseInfo.nextVersion().toString()}."
                    echo "Changelog:\n${releaseInfo.changelogToMarkdown()}"
                }
            }
        }

        stage('Build Image') {
            steps {
                script {
                    def image = docker.build("$DOCKER_REPO/$IMAGE_NAME:${releaseInfo.nextVersion().toString()}", "--no-cache -f PTrampert.SpaHost/Dockerfile .")
                    image.tag("$BRANCH_TAG")
                }
            }
        }

        stage('Push Image') {
            steps {
                withCredentials([usernamePassword(credentialsId: 'docker_hub', passwordVariable: 'password', usernameVariable: 'username')]) {
                    sh "docker login -u $username -p $password"
                    sh "docker push $DOCKER_REPO/$IMAGE_NAME:${releaseInfo.nextVersion().toString()}"
                    sh "docker push $DOCKER_REPO/$IMAGE_NAME:$BRANCH_TAG"
                }
            }
            post {
                always {
                    sh "docker logout"
                }
            }
        }

        stage('Github Release') {
            when { expression { env.BRANCH_NAME == 'main' } }
            steps {
                script {
                    publishGithubRelease(
                        'PaulTrampert',
                        PROJECT_NAME,
                        releaseInfo,
                        'v',
                        'Github User/Pass',
                        'https://api.github.com'
                    )
                }
            }
        }
    }
}