@Library('github-release-helpers@v0.2.1')
def releaseInfo

def getBranchTag(env) {
  return (env.BRANCH_NAME == "main") ? "latest" : env.BRANCH_NAME
}

pipeline {
    agent any

    environment {
        DOCKER_REPO = 'paultrampert'
        DOCKER_REPO_CREDENTIALS = 'docker_hub'
        IMAGE_NAME = "spahost"
        BRANCH_TAG = getBranchTag(env)
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
                    image.tag("$DOCKER_REPO/$IMAGE_NAME:$BRANCH_TAG")
                }
            }
        }

        stage('Push Image') {
            steps {
                withCredentials([usernamePassword(credentialsId: 'docker_hub', passwordVariable: 'password', usernameVariable: 'username')]) {
                    sh "docker push $DOCKER_REPO/$IMAGE_NAME:${releaseInfo.nextVersion().toString()}"
                    sh "docker push $DOCKER_REPO/$IMAGE_NAME:$BRANCH_TAG"
                }
            }
        }

        stage('Github Release') {
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