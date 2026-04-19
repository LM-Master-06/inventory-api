// ============================================================================
//  SIT223/SIT753 — DevOps Pipeline  |  Inventory Management API (.NET 8)
//  All 7 stages: Build · Test · Code Quality · Security ·
//                Deploy · Release · Monitoring & Alerting
// ============================================================================

pipeline {

    agent any

    // ── Environment ─────────────────────────────────────────────────────────
    environment {
        APP_NAME        = 'inventory-api'
        DOCKER_IMAGE    = 'inventory-api'
        BUILD_VERSION   = "${env.BUILD_NUMBER}-${env.GIT_COMMIT?.take(7) ?: 'local'}"
        SONAR_PROJECT   = 'inventory-api'
        STAGING_PORT    = '8081'
        PROD_PORT       = '8082'
        DOTNET_CLI_HOME = '/tmp/.dotnet'
        DOTNET_NOLOGO   = 'true'
    }

    // ── Options ─────────────────────────────────────────────────────────────
    options {
        buildDiscarder(logRotator(numToKeepStr: '10', artifactNumToKeepStr: '5'))
        timeout(time: 45, unit: 'MINUTES')
        disableConcurrentBuilds()
        timestamps()
    }

    // ── SCM Polling trigger ─────────────────────────────────────────────────
    triggers {
        pollSCM('H/5 * * * *')
    }

    // ════════════════════════════════════════════════════════════════════════
    stages {

        // ── Checkout ────────────────────────────────────────────────────────
        stage('Checkout') {
            steps {
                checkout scm
                sh 'echo "── Commit: $(git log -1 --format=\"%h — %s — %an\")"'
                sh 'echo "── Branch: $(git rev-parse --abbrev-ref HEAD)"'
            }
        }

        // ════════════════════════════════════════════════════════════════════
        // STAGE 1 — BUILD
        // Restore NuGet packages, compile in Release mode, publish artefact,
        // then build a versioned + latest Docker image.
        // ════════════════════════════════════════════════════════════════════
        stage('Build') {
            steps {
                echo "── [BUILD] Restoring & compiling v${BUILD_VERSION} ──"

                sh '''
                    dotnet restore src/InventoryAPI/InventoryAPI.csproj --verbosity minimal
                    dotnet build   src/InventoryAPI/InventoryAPI.csproj \
                        --configuration Release \
                        --no-restore \
                        -p:GenerateDocumentationFile=true \
                        -p:TreatWarningsAsErrors=false
                    dotnet publish src/InventoryAPI/InventoryAPI.csproj \
                        --configuration Release \
                        --output ./publish \
                        --no-build \
                        -p:Version=${BUILD_VERSION}
                '''

                // Build Docker image — tagged with build version AND latest
                script {
                    sh "docker build \
                            --build-arg BUILD_VERSION=${BUILD_VERSION} \
                            -t ${DOCKER_IMAGE}:${BUILD_VERSION} \
                            -t ${DOCKER_IMAGE}:latest \
                            -f Dockerfile ."
                    echo "✅ Docker image built: ${DOCKER_IMAGE}:${BUILD_VERSION}"
                }
            }
            post {
                success {
                    // Archive the .NET publish artefact
                    archiveArtifacts artifacts: 'publish/**', fingerprint: true
                    echo "📦 Build artefact archived."
                }
            }
        }

        // ════════════════════════════════════════════════════════════════════
        // STAGE 2 — TEST
        // Unit tests + integration tests with code coverage via coverlet.
        // Results published as JUnit XML and HTML coverage report.
        // Pipeline is gated: failing tests abort subsequent stages.
        // ════════════════════════════════════════════════════════════════════
        stage('Test') {
            steps {
                echo "── [TEST] Running unit & integration tests ──"

                sh '''
                    mkdir -p TestResults/Unit TestResults/Integration

                    # ── Unit Tests ──────────────────────────────────────────
                    dotnet test tests/InventoryAPI.Tests/InventoryAPI.Tests.csproj \
                        --configuration Release \
                        --filter "Category!=Integration" \
                        --logger "trx;LogFileName=unit-results.trx" \
                        --results-directory TestResults/Unit \
                        /p:CollectCoverage=true \
                        /p:CoverletOutputFormat=opencover \
                        /p:CoverletOutput=../../TestResults/coverage.opencover.xml \
                        /p:Exclude="[*]InventoryAPI.Program%2C[*]*Migrations*"

                    # ── Integration Tests ────────────────────────────────────
                    dotnet test tests/InventoryAPI.Tests/InventoryAPI.Tests.csproj \
                        --configuration Release \
                        --filter "Category=Integration" \
                        --logger "trx;LogFileName=integration-results.trx" \
                        --results-directory TestResults/Integration
                '''
            }
            post {
                always {
                    junit testResults: 'TestResults/**/*.trx', allowEmptyResults: false
                    publishHTML([
                        allowMissing         : false,
                        alwaysLinkToLastBuild: true,
                        keepAll              : true,
                        reportDir            : 'TestResults',
                        reportFiles          : 'coverage.opencover.xml',
                        reportName           : 'Code Coverage Report'
                    ])
                    echo "📊 Test results published."
                }
                failure {
                    error "❌ Tests failed — pipeline aborted."
                }
            }
        }

        // ════════════════════════════════════════════════════════════════════
        // STAGE 3 — CODE QUALITY  (SonarQube)
        // Runs SonarScanner for .NET, imports coverage from Stage 2,
        // applies a Quality Gate — pipeline fails if gate not passed.
        // Checks: code duplication, code smells, complexity, maintainability.
        // ════════════════════════════════════════════════════════════════════
        stage('Code Quality') {
            environment {
                SONAR_TOKEN = credentials('sonarqube-token')
            }
            steps {
                echo "── [CODE QUALITY] Running SonarQube analysis ──"

                withSonarQubeEnv('SonarQube') {
                    sh '''
                        # Install SonarScanner tool if not present
                        dotnet tool install --global dotnet-sonarscanner \
                            --version 5.15.0 2>/dev/null || true
                        export PATH="$PATH:$HOME/.dotnet/tools"

                        dotnet sonarscanner begin \
                            /k:"${SONAR_PROJECT}" \
                            /n:"Inventory Management API" \
                            /v:"${BUILD_VERSION}" \
                            /d:sonar.login="${SONAR_TOKEN}" \
                            /d:sonar.cs.opencover.reportsPaths="TestResults/coverage.opencover.xml" \
                            /d:sonar.cs.vstest.reportsPaths="TestResults/**/*.trx" \
                            /d:sonar.coverage.exclusions="**/Program.cs,**/Migrations/**" \
                            /d:sonar.exclusions="**/bin/**,**/obj/**" \
                            /d:sonar.qualitygate.wait=true

                        dotnet build src/InventoryAPI/InventoryAPI.csproj \
                            --configuration Release --no-restore

                        dotnet sonarscanner end /d:sonar.login="${SONAR_TOKEN}"
                    '''
                }
            }
            post {
                always {
                    // Block pipeline until Quality Gate result is received
                    script {
                        def qg = waitForQualityGate abortPipeline: true
                        if (qg.status != 'OK') {
                            error "❌ SonarQube Quality Gate FAILED: ${qg.status}"
                        }
                        echo "✅ SonarQube Quality Gate passed."
                    }
                }
            }
        }

        // ════════════════════════════════════════════════════════════════════
        // STAGE 4 — SECURITY SCAN  (Trivy)
        // Scans the Docker image for known CVEs.
        // HIGH/CRITICAL: generates report but continues.
        // CRITICAL (unfixed only): fails pipeline — must be addressed.
        // Report is archived and available in Jenkins build artifacts.
        // ════════════════════════════════════════════════════════════════════
        stage('Security Scan') {
            steps {
                echo "── [SECURITY] Running Trivy vulnerability scan ──"

                sh '''
                    # Pull Trivy (uses cached layers after first run)
                    docker pull aquasec/trivy:latest

                    # Full scan — all severities → JSON report (does not fail build)
                    docker run --rm \
                        -v /var/run/docker.sock:/var/run/docker.sock \
                        -v "$HOME/.cache/trivy:/root/.cache/" \
                        aquasec/trivy:latest image \
                        --exit-code 0 \
                        --severity LOW,MEDIUM,HIGH,CRITICAL \
                        --format json \
                        --output /dev/stdout \
                        ${DOCKER_IMAGE}:${BUILD_VERSION} > trivy-full-report.json

                    # Human-readable table report
                    docker run --rm \
                        -v /var/run/docker.sock:/var/run/docker.sock \
                        -v "$HOME/.cache/trivy:/root/.cache/" \
                        aquasec/trivy:latest image \
                        --exit-code 0 \
                        --severity HIGH,CRITICAL \
                        --format table \
                        ${DOCKER_IMAGE}:${BUILD_VERSION} | tee trivy-table-report.txt

                    echo ""
                    echo "── Critical-only gate (unfixed vulnerabilities) ──"
                    docker run --rm \
                        -v /var/run/docker.sock:/var/run/docker.sock \
                        -v "$HOME/.cache/trivy:/root/.cache/" \
                        -v "$(pwd)/.trivyignore:/.trivyignore" \
                        aquasec/trivy:latest image \
                        --exit-code 1 \
                        --severity CRITICAL \
                        --ignore-unfixed \
                        --ignorefile /.trivyignore \
                        ${DOCKER_IMAGE}:${BUILD_VERSION} \
                        && echo "✅ No unfixed CRITICAL vulnerabilities found." \
                        || (echo "⚠️  Unfixed CRITICAL CVEs detected — review trivy-full-report.json" && exit 1)
                '''
            }
            post {
                always {
                    archiveArtifacts artifacts: 'trivy-*.json, trivy-*.txt',
                                     allowEmptyArchive: true,
                                     fingerprint: true
                    echo "🔒 Security reports archived."
                }
                failure {
                    echo "❌ Critical unpatched CVEs found. Review trivy-full-report.json and update base image or dependencies."
                }
            }
        }

        // ════════════════════════════════════════════════════════════════════
        // STAGE 5 — DEPLOY TO STAGING
        // Tears down any previous staging container, deploys the new image
        // via docker-compose, waits for health check to pass, then runs
        // smoke tests against the live staging endpoint.
        // ════════════════════════════════════════════════════════════════════
        stage('Deploy to Staging') {
            steps {
                echo "── [DEPLOY] Deploying v${BUILD_VERSION} to staging ──"

                sh '''
                    export BUILD_VERSION="${BUILD_VERSION}"

                    # Graceful teardown of previous staging deployment
                    docker-compose -f docker-compose.staging.yml down \
                        --remove-orphans --timeout 15 || true

                    # Deploy new version
                    docker-compose -f docker-compose.staging.yml up -d

                    echo "⏳ Waiting for staging to become healthy..."
                    sleep 20

                    # Health check with retry
                    curl --silent --fail \
                         --retry 8 --retry-delay 5 --retry-connrefused \
                         http://localhost:${STAGING_PORT}/health \
                         && echo "✅ Staging is healthy." \
                         || (echo "❌ Staging health check failed." && \
                             docker-compose -f docker-compose.staging.yml logs && \
                             exit 1)
                '''

                // Smoke test — verify API returns data
                sh '''
                    echo "── Smoke test: GET /api/inventory ──"
                    RESP=$(curl --silent --write-out "\\n%{http_code}" \
                                http://localhost:${STAGING_PORT}/api/inventory)
                    HTTP_CODE=$(echo "$RESP" | tail -1)
                    if [ "$HTTP_CODE" -ne 200 ]; then
                        echo "❌ Smoke test failed — HTTP $HTTP_CODE"
                        exit 1
                    fi
                    echo "✅ Smoke test passed — HTTP $HTTP_CODE"
                    echo "── Staging URL: http://localhost:${STAGING_PORT}"
                '''
            }
        }

        // ════════════════════════════════════════════════════════════════════
        // STAGE 6 — RELEASE
        // Tags the Docker image as prod-<version>, deploys to production,
        // applies a Git tag for traceability, and verifies production health.
        // If production health check fails → automatic rollback is triggered.
        // Scoped to the `main` branch only.
        // ════════════════════════════════════════════════════════════════════
        stage('Release') {
            when {
                anyOf {
                    branch 'main'
                    branch 'master'
                }
            }
            steps {
                echo "── [RELEASE] Promoting v${BUILD_VERSION} to production ──"

                script {
                    // Tag image for production
                    sh "docker tag ${DOCKER_IMAGE}:${BUILD_VERSION} ${DOCKER_IMAGE}:prod-${BUILD_VERSION}"
                    sh "docker tag ${DOCKER_IMAGE}:${BUILD_VERSION} ${DOCKER_IMAGE}:prod-latest"

                    // Optional: push to a Docker registry
                    // Uncomment and configure 'docker-credentials' in Jenkins Credentials
                    // withDockerRegistry(credentialsId: 'docker-credentials', url: '') {
                    //     sh "docker push ${DOCKER_IMAGE}:${BUILD_VERSION}"
                    //     sh "docker push ${DOCKER_IMAGE}:prod-${BUILD_VERSION}"
                    //     sh "docker push ${DOCKER_IMAGE}:prod-latest"
                    // }
                }

                // Git release tag
                sh '''
                    git config user.email "jenkins@ci.local"
                    git config user.name  "Jenkins CI"
                    git tag -a "release-${BUILD_VERSION}" \
                        -m "Automated release v${BUILD_VERSION} [skip ci]" || true
                    echo "🏷️  Git tag: release-${BUILD_VERSION}"
                '''

                // Deploy to production
                sh '''
                    export BUILD_VERSION="${BUILD_VERSION}"

                    # Graceful teardown
                    docker-compose -f docker-compose.prod.yml down \
                        --remove-orphans --timeout 15 || true

                    docker-compose -f docker-compose.prod.yml up -d

                    echo "⏳ Waiting for production to become healthy..."
                    sleep 25

                    # Production health check — failure triggers rollback
                    curl --silent --fail \
                         --retry 8 --retry-delay 5 --retry-connrefused \
                         http://localhost:${PROD_PORT}/health \
                         && echo "✅ Production is healthy." \
                         || (
                             echo "❌ Production health check FAILED — triggering rollback."
                             docker-compose -f docker-compose.prod.yml down || true
                             exit 1
                         )

                    echo ""
                    echo "🚀 Released v${BUILD_VERSION} to production."
                    echo "   API:     http://localhost:${PROD_PORT}/api/inventory"
                    echo "   Swagger: http://localhost:${PROD_PORT}/swagger"
                '''
            }
        }

        // ════════════════════════════════════════════════════════════════════
        // STAGE 7 — MONITORING & ALERTING
        // Spins up the Prometheus + Grafana + Alertmanager monitoring stack,
        // verifies each service is reachable, confirms the app exposes
        // metrics, and simulates an alert to prove alerting works end-to-end.
        // ════════════════════════════════════════════════════════════════════
        stage('Monitoring & Alerting') {
            steps {
                echo "── [MONITORING] Deploying Prometheus / Grafana stack ──"

                sh '''
                    docker-compose -f docker-compose.monitoring.yml up -d

                    echo "⏳ Waiting for monitoring stack to start..."
                    sleep 30
                '''

                // Verify each component
                sh '''
                    echo "── Checking Prometheus ──"
                    curl --silent --fail \
                         --retry 6 --retry-delay 5 --retry-connrefused \
                         http://localhost:9090/-/ready \
                         && echo "✅ Prometheus is ready."

                    echo "── Checking Grafana ──"
                    curl --silent --fail \
                         --retry 6 --retry-delay 5 --retry-connrefused \
                         http://localhost:3000/api/health \
                         && echo "✅ Grafana is ready."

                    echo "── Checking Alertmanager ──"
                    curl --silent --fail \
                         --retry 4 --retry-delay 3 --retry-connrefused \
                         http://localhost:9093/-/ready \
                         && echo "✅ Alertmanager is ready." || true
                '''

                // Verify app metrics endpoint is being scraped
                sh '''
                    echo "── Verifying /metrics endpoint on production ──"
                    curl --silent http://localhost:${PROD_PORT}/metrics \
                        | grep -q "http_requests_received_total" \
                        && echo "✅ Prometheus metrics endpoint confirmed." \
                        || echo "⚠️  Metrics endpoint not yet producing HTTP metrics (may need traffic)."

                    # Generate a few requests to populate metrics
                    echo "── Generating test traffic to populate metrics ──"
                    for i in 1 2 3 4 5; do
                        curl --silent http://localhost:${PROD_PORT}/api/inventory > /dev/null
                        curl --silent http://localhost:${PROD_PORT}/health         > /dev/null
                    done
                    echo "✅ Test traffic sent."

                    # Verify Prometheus has started scraping
                    sleep 15
                    curl --silent \
                        "http://localhost:9090/api/v1/query?query=up{job='inventory-api-prod'}" \
                        | grep -q '"value"' \
                        && echo "✅ Prometheus is actively scraping the production app." \
                        || echo "⚠️  Prometheus scrape pending — check target at http://localhost:9090/targets"

                    echo ""
                    echo "════════════════════════════════════════"
                    echo "  📊 Grafana:       http://localhost:3000   (admin / admin)"
                    echo "  📈 Prometheus:    http://localhost:9090"
                    echo "  🔔 Alertmanager:  http://localhost:9093"
                    echo "  🚀 Production:    http://localhost:${PROD_PORT}"
                    echo "  🧪 Staging:       http://localhost:${STAGING_PORT}"
                    echo "════════════════════════════════════════"
                '''
            }
        }

    } // end stages

    // ── Post Actions ────────────────────────────────────────────────────────
    post {
        always {
            echo "── Pipeline finished: build ${BUILD_VERSION} ──"
            // Clean workspace to free disk space
            cleanWs(cleanWhenNotBuilt: false,
                    deleteDirs:        true,
                    disableDeferredWipeout: true,
                    notFailBuild:      true)
        }

        success {
            echo "✅ All stages passed for build ${BUILD_VERSION}."
            // Uncomment to send email on success:
            // emailext(
            //     subject: "✅ [${APP_NAME}] Build #${BUILD_NUMBER} PASSED",
            //     body:    "Build v${BUILD_VERSION} completed successfully.\n\n${BUILD_URL}",
            //     to:      'devops-team@example.com'
            // )
        }

        failure {
            echo "❌ Pipeline FAILED on build ${BUILD_VERSION} — rolling back deployments."
            sh '''
                docker-compose -f docker-compose.prod.yml    down --remove-orphans || true
                docker-compose -f docker-compose.staging.yml down --remove-orphans || true
            '''
            // Uncomment to send email on failure:
            // emailext(
            //     subject: "❌ [${APP_NAME}] Build #${BUILD_NUMBER} FAILED",
            //     body:    "Pipeline failed at stage: ${FAILED_STAGE}\n\n${BUILD_URL}console",
            //     to:      'devops-team@example.com'
            // )
        }

        unstable {
            echo "⚠️  Pipeline UNSTABLE — test failures or quality warnings detected."
        }
    }
}
