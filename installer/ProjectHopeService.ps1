[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("Configure", "Install", "Stop", "Remove")]
    [string]$Action,

    [Parameter(Mandatory = $true)]
    [string]$ServiceName,

    [Parameter(Mandatory = $false)]
    [string]$DisplayName,

    [Parameter(Mandatory = $false)]
    [string]$ExecutablePath,

    [Parameter(Mandatory = $false)]
    [string]$ConfigPath,

    [Parameter(Mandatory = $false)]
    [ValidateSet("true", "false")]
    [string]$IncomingOrdersEnabled
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Get-ProjectHopeService {
    Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
}

function Wait-ForServiceState {
    param(
        [Parameter(Mandatory = $true)]
        [System.ServiceProcess.ServiceControllerStatus]$Status,

        [Parameter(Mandatory = $false)]
        [int]$TimeoutSeconds = 30
    )

    $service = Get-ProjectHopeService
    if ($null -eq $service) {
        return
    }

    $service.WaitForStatus($Status, [TimeSpan]::FromSeconds($TimeoutSeconds))
}

function Stop-ProjectHopeService {
    $service = Get-ProjectHopeService
    if ($null -eq $service) {
        return
    }

    if ($service.Status -ne [System.ServiceProcess.ServiceControllerStatus]::Stopped) {
        Stop-Service -Name $ServiceName -Force
        Wait-ForServiceState -Status Stopped
    }
}

function Invoke-ServiceControl {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    & "$env:SystemRoot\System32\sc.exe" @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "sc.exe failed with exit code $LASTEXITCODE while running: sc.exe $($Arguments -join ' ')"
    }
}

switch ($Action) {
    "Configure" {
        if ([string]::IsNullOrWhiteSpace($ConfigPath)) {
            throw "ConfigPath is required when configuring application features."
        }

        if ([string]::IsNullOrWhiteSpace($IncomingOrdersEnabled)) {
            throw "IncomingOrdersEnabled is required when configuring application features."
        }

        $resolvedConfigPath = [IO.Path]::GetFullPath($ConfigPath)
        if (-not (Test-Path -LiteralPath $resolvedConfigPath -PathType Leaf)) {
            throw "The Project Hope application configuration was not found: $resolvedConfigPath"
        }

        $temporaryConfigPath = $null
        try {
            $configuration = Get-Content -LiteralPath $resolvedConfigPath -Raw | ConvertFrom-Json
            if ($null -eq $configuration) {
                throw "The application configuration is empty."
            }

            if ($null -eq $configuration.PSObject.Properties["Features"]) {
                $configuration | Add-Member -MemberType NoteProperty -Name Features -Value ([PSCustomObject]@{})
            }

            $features = $configuration.Features
            $enableIncomingOrders = [bool]::Parse($IncomingOrdersEnabled)
            if ($null -eq $features.PSObject.Properties["IncomingOrders"]) {
                $features | Add-Member -MemberType NoteProperty -Name IncomingOrders -Value $enableIncomingOrders
            }
            else {
                $features.IncomingOrders = $enableIncomingOrders
            }

            $temporaryConfigPath = "$resolvedConfigPath.tmp"
            $configuration |
                ConvertTo-Json -Depth 20 |
                Set-Content -LiteralPath $temporaryConfigPath -Encoding UTF8
            Move-Item -LiteralPath $temporaryConfigPath -Destination $resolvedConfigPath -Force
        }
        catch {
            if ($null -ne $temporaryConfigPath -and (Test-Path -LiteralPath $temporaryConfigPath)) {
                Remove-Item -LiteralPath $temporaryConfigPath -Force -ErrorAction SilentlyContinue
            }

            throw "The Incoming Orders feature setting could not be updated in '$resolvedConfigPath': $($_.Exception.Message)"
        }
    }

    "Stop" {
        Stop-ProjectHopeService
    }

    "Install" {
        if ([string]::IsNullOrWhiteSpace($DisplayName)) {
            throw "DisplayName is required when installing the service."
        }

        if ([string]::IsNullOrWhiteSpace($ExecutablePath)) {
            throw "ExecutablePath is required when installing the service."
        }

        $resolvedExecutablePath = [IO.Path]::GetFullPath($ExecutablePath)
        if (-not (Test-Path -LiteralPath $resolvedExecutablePath -PathType Leaf)) {
            throw "The Project Hope server executable was not found: $resolvedExecutablePath"
        }

        Stop-ProjectHopeService

        $quotedExecutablePath = '"' + $resolvedExecutablePath + '"'
        $service = Get-ProjectHopeService

        if ($null -eq $service) {
            Invoke-ServiceControl -Arguments @(
                "create",
                $ServiceName,
                "binPath=",
                $quotedExecutablePath,
                "start=",
                "delayed-auto",
                "DisplayName=",
                $DisplayName)
        }
        else {
            Invoke-ServiceControl -Arguments @(
                "config",
                $ServiceName,
                "binPath=",
                $quotedExecutablePath,
                "start=",
                "delayed-auto",
                "DisplayName=",
                $DisplayName)
        }

        Invoke-ServiceControl -Arguments @(
            "description",
            $ServiceName,
            "Hosts the Project Hope Food Bank of Lynden inventory application.")

        Invoke-ServiceControl -Arguments @(
            "failure",
            $ServiceName,
            "reset=",
            "86400",
            "actions=",
            "restart/5000/restart/15000/none/0")

        Start-Service -Name $ServiceName
        Wait-ForServiceState -Status Running
    }

    "Remove" {
        Stop-ProjectHopeService

        if ($null -ne (Get-ProjectHopeService)) {
            Invoke-ServiceControl -Arguments @("delete", $ServiceName)

            $deadline = [DateTime]::UtcNow.AddSeconds(30)
            while ($null -ne (Get-ProjectHopeService) -and [DateTime]::UtcNow -lt $deadline) {
                Start-Sleep -Milliseconds 250
            }

            if ($null -ne (Get-ProjectHopeService)) {
                throw "The Windows service was marked for deletion but did not disappear within 30 seconds."
            }
        }
    }
}
