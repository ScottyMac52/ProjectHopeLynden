param(
    [Parameter(Mandatory = $true)]
    [string]$FilePath,

    [Parameter(Mandatory = $true)]
    [string]$CertificateBase64,

    [Parameter(Mandatory = $true)]
    [string]$CertificatePassword,

    [Parameter(Mandatory = $false)]
    [string]$TimestampUrl = "http://timestamp.digicert.com",

    [Parameter(Mandatory = $false)]
    [switch]$SkipTimestamp
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Invoke-SignTool {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ToolPath,

        [Parameter(Mandatory = $true)]
        [string[]]$Arguments,

        [Parameter(Mandatory = $true)]
        [string]$Operation,

        [Parameter(Mandatory = $false)]
        [int]$TimeoutSeconds = 120
    )

    $startInfo = [System.Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = $ToolPath
    $startInfo.UseShellExecute = $false
    $startInfo.CreateNoWindow = $true
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true

    foreach ($argument in $Arguments) {
        [void]$startInfo.ArgumentList.Add($argument)
    }

    $process = [System.Diagnostics.Process]::new()
    $process.StartInfo = $startInfo

    try {
        if (-not $process.Start()) {
            throw "$Operation could not start signtool.exe."
        }

        $standardOutputTask = $process.StandardOutput.ReadToEndAsync()
        $standardErrorTask = $process.StandardError.ReadToEndAsync()

        if (-not $process.WaitForExit($TimeoutSeconds * 1000)) {
            try {
                $process.Kill($true)
            } catch {
                Write-Warning "Unable to terminate the timed-out signtool process: $_"
            }

            throw "$Operation timed out after $TimeoutSeconds seconds."
        }

        $standardOutput = $standardOutputTask.GetAwaiter().GetResult()
        $standardError = $standardErrorTask.GetAwaiter().GetResult()

        if (-not [string]::IsNullOrWhiteSpace($standardOutput)) {
            Write-Host $standardOutput.TrimEnd()
        }

        if (-not [string]::IsNullOrWhiteSpace($standardError)) {
            Write-Host $standardError.TrimEnd()
        }

        if ($process.ExitCode -ne 0) {
            throw "$Operation failed with signtool exit code $($process.ExitCode)."
        }
    } finally {
        $process.Dispose()
    }
}

if (-not (Test-Path -LiteralPath $FilePath -PathType Leaf)) {
    throw "The file to sign was not found: $FilePath"
}

if ([string]::IsNullOrWhiteSpace($CertificateBase64)) {
    throw "The Authenticode certificate value is empty."
}

if ([string]::IsNullOrWhiteSpace($CertificatePassword)) {
    throw "The Authenticode certificate password is empty."
}

if (-not $SkipTimestamp -and [string]::IsNullOrWhiteSpace($TimestampUrl)) {
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
    Write-Host "Decoding temporary Authenticode certificate."

    try {
        $certificateBytes = [Convert]::FromBase64String($CertificateBase64)
    } catch {
        throw "AUTHENTICODE_CERTIFICATE_BASE64 is not valid Base64 certificate data."
    }

    [System.IO.File]::WriteAllBytes($pfxPath, $certificateBytes)

    Write-Host "Locating signtool.exe."
    $signToolCommand = Get-Command "signtool.exe" -ErrorAction SilentlyContinue
    if ($signToolCommand) {
        $signToolPath = $signToolCommand.Source
    } else {
        $windowsKitsRoot = Join-Path ${env:ProgramFiles(x86)} "Windows Kits\10\bin"
        $signToolFile = Get-ChildItem -Path (Join-Path $windowsKitsRoot "*\x64\signtool.exe") -File -ErrorAction SilentlyContinue |
            Sort-Object FullName -Descending |
            Select-Object -First 1

        if (-not $signToolFile) {
            throw "signtool.exe was not found on the Windows runner."
        }

        $signToolPath = $signToolFile.FullName
    }

    Write-Host "Using signtool: $signToolPath"

    $signArguments = @(
        "sign",
        "/fd", "SHA256",
        "/f", $pfxPath,
        "/p", $CertificatePassword
    )

    if ($SkipTimestamp) {
        Write-Host "Signing $resolvedFilePath with SHA-256 without timestamping for isolated smoke verification."
    } else {
        Write-Host "Signing $resolvedFilePath with SHA-256 and RFC 3161 timestamping."
        $signArguments += @(
            "/tr", $TimestampUrl,
            "/td", "SHA256"
        )
    }

    $signArguments += $resolvedFilePath

    Invoke-SignTool `
        -ToolPath $signToolPath `
        -Arguments $signArguments `
        -Operation "Authenticode signing"

    Write-Host "Verifying Authenticode signature with signtool.exe."
    Invoke-SignTool `
        -ToolPath $signToolPath `
        -Arguments @("verify", "/pa", "/v", $resolvedFilePath) `
        -Operation "Authenticode verification"

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
