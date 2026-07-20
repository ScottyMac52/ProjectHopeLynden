using System.Text.Json;
using Xunit;

namespace ProjectHopeLynden.Web.Tests.Installer;

public sealed class WindowsServiceInstallerTests
{
    [Fact]
    public void Installer_RegistersStartsAndRemovesStableService()
    {
        var script = ReadAsset("installer.iss");

        Assert.Contains("#define ServiceName \"ProjectHopeLynden\"", script);
        Assert.Contains(
            "#define ServiceDisplayName \"Project Hope Lynden Inventory Server\"",
            script);
        Assert.Contains("RunServiceHelper(GetInstalledServiceHelperPath, 'Install');", script);
        Assert.Contains("RunServiceHelper(GetInstalledServiceHelperPath, 'Remove');", script);
        Assert.Contains("CurUninstallStepChanged", script);
    }

    [Fact]
    public void Installer_StopsServiceBeforeDatabaseBackupAndFileReplacement()
    {
        var script = ReadAsset("installer.iss");
        var stopIndex = script.IndexOf(
            "RunServiceHelper(GetTemporaryServiceHelperPath, 'Stop');",
            StringComparison.Ordinal);
        var backupIndex = script.IndexOf(
            "BackupDatabaseIfPresent(GetProgramDataDatabasePath",
            StringComparison.Ordinal);

        Assert.True(stopIndex >= 0);
        Assert.True(backupIndex > stopIndex);
    }

    [Fact]
    public void Installer_OffersIncomingOrdersFeatureDisabledByDefault()
    {
        var script = ReadAsset("installer.iss");

        Assert.Contains(
            "Name: \"enableincomingorders\"; Description: \"Enable Incoming Orders Feature\"; " +
            "GroupDescription: \"Feature options:\"; Flags: unchecked",
            script);
        Assert.Contains("IsTaskSelected('enableincomingorders')", script);
        Assert.Contains("-IncomingOrdersEnabled", script);
    }

    [Fact]
    public void Installer_ConfiguresFeatureBeforeStartingService()
    {
        var script = ReadAsset("installer.iss");
        var configureIndex = script.IndexOf(
            "RunServiceHelper(GetInstalledServiceHelperPath, 'Configure');",
            StringComparison.Ordinal);
        var installIndex = script.IndexOf(
            "RunServiceHelper(GetInstalledServiceHelperPath, 'Install');",
            StringComparison.Ordinal);

        Assert.True(configureIndex >= 0);
        Assert.True(installIndex > configureIndex);
    }

    [Fact]
    public void Installer_OpensBrowserInsteadOfStartingSecondServerProcess()
    {
        var script = ReadAsset("installer.iss");

        Assert.Contains("Filename: \"http://localhost:5000/\"", script);
        Assert.DoesNotContain(
            "Filename: \"{app}\\{#AppExeName}\"; Description: \"Launch Project Hope Lynden Server\"",
            script);
    }

    [Fact]
    public void ServiceHelper_ConfiguresDelayedAutomaticStartupAndRecovery()
    {
        var script = ReadAsset("ProjectHopeService.ps1");

        Assert.Contains("[ValidateSet(\"Configure\", \"Install\", \"Stop\", \"Remove\")]", script);
        Assert.Contains("\"delayed-auto\"", script);
        Assert.Contains("Start-Service -Name $ServiceName", script);
        Assert.Contains("\"restart/5000/restart/15000/none/0\"", script);
        Assert.Contains("@(" + "\"delete\", $ServiceName)", script);
    }

    [Fact]
    public void ServiceHelper_UpdatesOnlyIncomingOrdersFeatureSetting()
    {
        var script = ReadAsset("ProjectHopeService.ps1");

        Assert.Contains("[ValidateSet(\"true\", \"false\")]", script);
        Assert.Contains("ConvertFrom-Json", script);
        Assert.Contains("$features.IncomingOrders = $enableIncomingOrders", script);
        Assert.Contains("ConvertTo-Json -Depth 20", script);
        Assert.Contains("Move-Item -LiteralPath $temporaryConfigPath", script);
    }

    [Fact]
    public void ProductionConfiguration_UsesLanPortFiveThousand()
    {
        using var document = JsonDocument.Parse(ReadAsset("appsettings.json"));

        Assert.Equal(
            "http://*:5000",
            document.RootElement.GetProperty("Urls").GetString());
    }

    private static string ReadAsset(string fileName)
    {
        return File.ReadAllText(Path.Combine(
            AppContext.BaseDirectory,
            "TestAssets",
            fileName));
    }
}
