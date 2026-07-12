param(
    [Parameter(Mandatory = $true)]
    [string]$FilePath,

    [Parameter(Mandatory = $true)]
    [string]$CertificateBase64,

    [Parameter(Mandatory = $true)]
    [string]$CertificatePassword,

    [Parameter(Mandatory = $false)]
    [string]$TimestampUrl = "http://timestamp.digicert.com"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $FilePath -PathType Leaf)) {
    throw "The file to sign was not found: $FilePath"
}

if ([string]::IsNullOrWhiteSpace($CertificateBase64)) {
    throw "The Authenticode certificate value is empty."
}

if ([string]::IsNullOrWhiteSpace($CertificatePassword)) {
    throw "The Authenticode certificate password is empty."
}

if ([string]::IsNullOrWhiteSpace($TimestampUrl)) {
    throw "The Authenticode timestamp URL is empty."
}

$resolvedFilePath = (Resolve-Path -LiteralPath $FilePath).Path
$tempFolder = if ([string]::IsNullOrWhiteSpace($env:RUNNER_TEMP)) {
    [System.IO.Path]::GetTempPath()
} else {
    $env:RUNNER_TEMP
}
$pfxPath = Join-Path $tempFolder "projecthope-authenticode-$([Guid]::NewGuid().ToString('N')).pfx"

try {
    try {
        $certificateBytes = [Convert]::FromBase64String($CertificateBase64)
    } catch {
        throw "AUTHENTICODE_CERTIFICATE_BASE64 is not valid Base64 certificate data."
    }

    [System.IO.File]::WriteAllBytes($pfxPath, $certificateBytes)

    $signToolCommand = Get-Command "signtool.exe" -ErrorAction SilentlyContinue
    if ($signToolCommand) {
        $signToolPath = $signToolCommand.Source
    } else {
        $windowsKitsRoot = Join-Path ${env:ProgramFiles(x86)} "Windows Kits\10\bin"
        $signToolFile = Get-ChildItem -Path $windowsKitsRoot -Recurse -Filter "signtool.exe" -File -ErrorAction SilentlyContinue |
            Where-Object { $_.FullName -match '\\x64\\signtool\.exe$' } |
            Sort-Object FullName -Descending |
            Select-Object -First 1

        if (-not $signToolFile) {
            throw "signtool.exe was not found on the Windows runner."
        }

        $signToolPath = $signToolFile.FullName
    }

    Write-Host "Signing $resolvedFilePath with SHA-256 and RFC 3161 timestamping."

    $signArguments = @(
        "sign",
        "/fd", "SHA256",
        "/f", $pfxPath,
        "/p", $CertificatePassword,
        "/tr", $TimestampUrl,
        "/td", "SHA256",
        $resolvedFilePath
    )

    & $signToolPath @signArguments
    if ($LASTEXITCODE -ne 0) {
        throw "signtool.exe failed to sign the installer with exit code $LASTEXITCODE."
    }

    & $signToolPath "verify" "/pa" "/v" $resolvedFilePath
    if ($LASTEXITCODE -ne 0) {
        throw "signtool.exe could not verify the installer signature. Exit code: $LASTEXITCODE."
    }

    $signature = Get-AuthenticodeSignature -LiteralPath $resolvedFilePath
    if ($signature.Status -ne "Valid") {
        throw "PowerShell signature verification returned status '$($signature.Status)': $($signature.StatusMessage)"
    }

    Write-Host "Authenticode signature verified."
    Write-Host "Certificate subject: $($signature.SignerCertificate.Subject)"
    Write-Host "Certificate thumbprint: $($signature.SignerCertificate.Thumbprint)"
} finally {
    if (Test-Path -LiteralPath $pfxPath) {
        Remove-Item -LiteralPath $pfxPath -Force -ErrorAction SilentlyContinue
    }
}
