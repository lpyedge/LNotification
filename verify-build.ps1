# Verify build process before publishing
# This script simulates the GitHub Actions workflow locally

Write-Host "=== LNotification Build Verification ===" -ForegroundColor Cyan
Write-Host ""

# Step 1: Restore
Write-Host "[1/5] Restoring dependencies..." -ForegroundColor Yellow
dotnet restore src/LNotification/LNotification.csproj
if ($LASTEXITCODE -ne 0) { 
    Write-Host "✗ Restore failed" -ForegroundColor Red
    exit 1 
}
Write-Host "✓ Restore succeeded" -ForegroundColor Green
Write-Host ""

# Step 2: Build
Write-Host "[2/5] Building..." -ForegroundColor Yellow
dotnet build src/LNotification/LNotification.csproj --no-restore --configuration Release
if ($LASTEXITCODE -ne 0) { 
    Write-Host "✗ Build failed" -ForegroundColor Red
    exit 1 
}
Write-Host "✓ Build succeeded" -ForegroundColor Green
Write-Host ""

# Step 3: Test
Write-Host "[3/5] Running tests..." -ForegroundColor Yellow
dotnet test tests/LNotification.Tests/LNotification.Tests.csproj --configuration Release --verbosity normal
if ($LASTEXITCODE -ne 0) { 
    Write-Host "✗ Tests failed" -ForegroundColor Red
    exit 1 
}
Write-Host "✓ Tests passed" -ForegroundColor Green
Write-Host ""

# Step 4: Check vulnerabilities
Write-Host "[4/5] Checking for security vulnerabilities..." -ForegroundColor Yellow
Push-Location src/LNotification
$vulnOutput = dotnet list package --vulnerable --include-transitive 2>&1
Pop-Location
if ($vulnOutput -match "has the following vulnerable packages") { 
    Write-Host "✗ Vulnerabilities found:" -ForegroundColor Red
    Write-Host $vulnOutput
    exit 1 
}
Write-Host "✓ No vulnerabilities found" -ForegroundColor Green
Write-Host ""

# Step 5: Pack
Write-Host "[5/5] Creating NuGet package..." -ForegroundColor Yellow
if (Test-Path ./artifacts) { Remove-Item ./artifacts -Recurse -Force }

# Test both release and preview packages
dotnet pack src/LNotification/LNotification.csproj --no-build --configuration Release --output ./artifacts
if ($LASTEXITCODE -ne 0) { 
    Write-Host "✗ Pack failed" -ForegroundColor Red
    exit 1 
}

# Test preview package
$version = (Select-String -Path "src/LNotification/LNotification.csproj" -Pattern "<Version>(.*)</Version>").Matches.Groups[1].Value
dotnet pack src/LNotification/LNotification.csproj --no-build --configuration Release --output ./artifacts -p:PackageVersion="$version-preview"
if ($LASTEXITCODE -ne 0) { 
    Write-Host "✗ Preview pack failed" -ForegroundColor Red
    exit 1 
}
Write-Host "✓ Pack succeeded" -ForegroundColor Green
Write-Host ""

# Summary
Write-Host "=== Verification Complete ===" -ForegroundColor Cyan
Write-Host "Packages created in ./artifacts/" -ForegroundColor Green
Get-ChildItem ./artifacts/*.nupkg | ForEach-Object {
    $prerelease = if ($_.Name -match "-preview") { " (pre-release)" } else { "" }
    Write-Host "  → $($_.Name)$prerelease" -ForegroundColor White
}
Write-Host ""
Write-Host "Ready to publish! 🚀" -ForegroundColor Green
