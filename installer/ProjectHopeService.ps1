[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("Install", "Stop", "Remove")]
    [string]$Action,

    [Parameter(Mandatory = $true)]
    [string]$ServiceName,

    [Parameter(Mandatory = $false)]
    [string]$DisplayName,

    [Parameter(Mandatory = $false)]
    [string]$ExecutablePath
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
