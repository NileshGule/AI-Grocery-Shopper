using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using UI.Agents;
using Microsoft.AspNetCore.DataProtection;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages()
.AddRazorPagesOptions(options =>
{
    options.Conventions.ConfigureFilter(new IgnoreAntiforgeryTokenAttribute());
});
builder.Services.AddHttpClient();

// Persist data protection keys to a shared folder so antiforgery (and other protected payloads)
// can be decrypted after container restarts or across multiple instances.
var keysFolder = builder.Configuration["DataProtection:KeyPath"] ?? "/keys";
Directory.CreateDirectory(keysFolder);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keysFolder))
    .SetApplicationName("AI-Grocery-Shopper");

// Register agent orchestrator implementation
builder.Services.AddScoped<IAgentOrchestrator, AgentOrchestrator>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();

app.MapRazorPages();

app.Run();
