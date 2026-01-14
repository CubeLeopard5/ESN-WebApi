# Script PowerShell pour exécuter les tests avec code coverage

Write-Host "🧪 Exécution des tests avec code coverage..." -ForegroundColor Cyan

# Nettoyer les anciens rapports
if (Test-Path "TestResults") {
    Remove-Item -Recurse -Force TestResults
    Write-Host "✓ Anciens résultats supprimés" -ForegroundColor Green
}

if (Test-Path "coverage-report") {
    Remove-Item -Recurse -Force coverage-report
    Write-Host "✓ Anciens rapports supprimés" -ForegroundColor Green
}

# Exécuter les tests avec coverage
Write-Host "`n📊 Génération du coverage..." -ForegroundColor Cyan
dotnet test Tests/Tests.csproj `
    --collect:"XPlat Code Coverage" `
    --results-directory:"./TestResults" `
    /p:CollectCoverage=true `
    /p:CoverletOutputFormat=cobertura `
    /p:CoverletOutput="./TestResults/coverage.cobertura.xml" `
    /p:Exclude="[Tests]*,[*.Tests]*"

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Les tests ont échoué!" -ForegroundColor Red
    exit $LASTEXITCODE
}

Write-Host "`n✅ Tests réussis!" -ForegroundColor Green

# Trouver le fichier de coverage généré
$coverageFile = Get-ChildItem -Path "TestResults" -Filter "coverage.cobertura.xml" -Recurse | Select-Object -First 1

if ($null -eq $coverageFile) {
    Write-Host "⚠️  Fichier de coverage non trouvé, recherche d'alternatives..." -ForegroundColor Yellow
    $coverageFile = Get-ChildItem -Path "TestResults" -Filter "*.cobertura.xml" -Recurse | Select-Object -First 1
}

if ($null -ne $coverageFile) {
    Write-Host "`n📈 Génération du rapport HTML..." -ForegroundColor Cyan

    # Installer ReportGenerator globalement si nécessaire
    $reportGenInstalled = dotnet tool list -g | Select-String "reportgenerator"
    if ($null -eq $reportGenInstalled) {
        Write-Host "📦 Installation de ReportGenerator..." -ForegroundColor Yellow
        dotnet tool install -g dotnet-reportgenerator-globaltool
    }

    # Générer le rapport HTML
    reportgenerator `
        "-reports:$($coverageFile.FullName)" `
        "-targetdir:coverage-report" `
        "-reporttypes:Html;HtmlSummary;Badges;Cobertura" `
        "-verbosity:Info"

    if ($LASTEXITCODE -eq 0) {
        Write-Host "`n✅ Rapport de coverage généré!" -ForegroundColor Green
        Write-Host "`n📂 Ouvrez le rapport: coverage-report/index.html" -ForegroundColor Cyan

        # Afficher un résumé
        if (Test-Path "coverage-report/Summary.txt") {
            Write-Host "`n📊 Résumé du coverage:" -ForegroundColor Cyan
            Get-Content "coverage-report/Summary.txt"
        }

        # Ouvrir automatiquement le rapport dans le navigateur
        $indexPath = Join-Path (Get-Location) "coverage-report\index.html"
        if (Test-Path $indexPath) {
            Start-Process $indexPath
        }
    } else {
        Write-Host "❌ Erreur lors de la génération du rapport" -ForegroundColor Red
    }
} else {
    Write-Host "❌ Aucun fichier de coverage trouvé dans TestResults" -ForegroundColor Red
}

Write-Host "`n✨ Terminé!" -ForegroundColor Green
