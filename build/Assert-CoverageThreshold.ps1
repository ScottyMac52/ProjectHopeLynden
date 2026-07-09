param(
    [Parameter(Mandatory = $true)]
    [string]$CoverageFile,

    [Parameter(Mandatory = $true)]
    [double]$LineThreshold,

    [Parameter(Mandatory = $true)]
    [double]$BranchThreshold
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $CoverageFile)) {
    throw "Coverage file was not found: $CoverageFile"
}

[xml]$coverage = Get-Content $CoverageFile
$coverageNode = $coverage.coverage

if ($null -eq $coverageNode) {
    throw "Coverage file does not contain a Cobertura coverage root element."
}

$lineRate = [double]$coverageNode.'line-rate' * 100
$branchRate = [double]$coverageNode.'branch-rate' * 100
$linesCovered = [int]$coverageNode.'lines-covered'
$linesValid = [int]$coverageNode.'lines-valid'
$branchesCovered = [int]$coverageNode.'branches-covered'
$branchesValid = [int]$coverageNode.'branches-valid'

if ($branchesValid -eq 0) {
    $branchRate = 100
}

Write-Host ("Line coverage: {0:N2}% ({1}/{2})" -f $lineRate, $linesCovered, $linesValid)
Write-Host ("Branch coverage: {0:N2}% ({1}/{2})" -f $branchRate, $branchesCovered, $branchesValid)

if ($lineRate -lt $LineThreshold) {
    throw ("Line coverage {0:N2}% is below required threshold {1:N2}%." -f $lineRate, $LineThreshold)
}

if ($branchRate -lt $BranchThreshold) {
    throw ("Branch coverage {0:N2}% is below required threshold {1:N2}%." -f $branchRate, $BranchThreshold)
}
