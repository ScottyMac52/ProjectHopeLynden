using ProjectHopeLynden.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

var databaseConnectionString = builder.Configuration.GetConnectionString("ProjectHopeDatabase")
    ?? throw new InvalidOperationException("The ProjectHopeDatabase connection string is not configured.");

builder.Services.AddRazorPages();
builder.Services.AddProjectHopePersistence(databaseConnectionString);

var app = builder.Build();

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
