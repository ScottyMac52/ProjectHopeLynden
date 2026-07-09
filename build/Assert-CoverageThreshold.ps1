param(
    [Parameter(Mandatory = $true)]
    [string]$CoverageFile,

    [Parameter(Mandatory = $true)]
    [double]$LineThreshold,

    [Parameter(Mandatory = $true)]
    [double]$BranchThreshold
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path $CoverageFile)) {
    throw "Coverage file was not found: $CoverageFile"
}

[xml]$coverage = Get-Content $CoverageFile -Raw
$coverageNode = $coverage.coverage

if ($null -eq $coverageNode) {
    throw "Coverage file does not contain a Cobertura coverage root element: $CoverageFile"
}

$lineRate = [double]$coverageNode.'line-rate' * 100
$branchRate = [double]$coverageNode.'branch-rate' * 100
$branchesValid = [int]$coverageNode.'branches-valid'

if ($branchesValid -eq 0) {
    $branchRate = 100
}

Write-Host ("Line coverage:   {0:N2}%" -f $lineRate)
Write-Host ("Branch coverage: {0:N2}%" -f $branchRate)
Write-Host ("Required line coverage:   {0:N2}%" -f $LineThreshold)
Write-Host ("Required branch coverage: {0:N2}%" -f $BranchThreshold)

if ($lineRate -lt $LineThreshold) {
    throw ("Line coverage {0:N2}% is below required threshold {1:N2}%." -f $lineRate, $LineThreshold)
}

if ($branchRate -lt $BranchThreshold) {
    throw ("Branch coverage {0:N2}% is below required threshold {1:N2}%." -f $branchRate, $BranchThreshold)
}

Write-Host 'Coverage thresholds satisfied.'
