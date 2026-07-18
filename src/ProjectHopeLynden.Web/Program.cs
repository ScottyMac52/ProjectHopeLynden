using ProjectHopeLynden.Infrastructure.DependencyInjection;
using ProjectHopeLynden.Web.Hosting;
using ProjectHopeLynden.Web.Features;
using ProjectHopeLynden.Web.Startup;

var builder = WebApplication.CreateBuilder(args);

ProjectHopeWindowsServiceHost.Configure(builder);

var databaseConnectionString = builder.Configuration.GetConnectionString("ProjectHopeDatabase")
    ?? throw new InvalidOperationException("The ProjectHopeDatabase connection string is not configured.");

var databaseBackupFolder = builder.Configuration["DatabaseBackup:Folder"]
    ?? throw new InvalidOperationException("The DatabaseBackup folder is not configured.");

builder.Services.AddRazorPages();
builder.Services.Configure<ProjectHopeFeatureOptions>(
    builder.Configuration.GetSection(ProjectHopeFeatureOptions.SectionName));
builder.Services.AddProjectHopePersistence(databaseConnectionString);
builder.Services.AddProjectHopeDatabaseBackup(databaseBackupFolder);

var app = builder.Build();

await app.InitializeProjectHopeDatabaseAsync();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.MapRazorPages();

app.Run();

public partial class Program
{
}
