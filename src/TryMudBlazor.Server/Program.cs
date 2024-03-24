using MudBlazor.Examples.Data;
using TryMudBlazor.Client;
using TryMudBlazor.Client.Shared;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddScoped<IPeriodicTableService, PeriodicTableService>();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(
        builder =>
        {
            builder.WithOrigins("https://www.mudblazor.com", "https://mudblazor.com");
        });
});

builder.Services.AddControllers();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseCors();

// Needed for wasm project
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.MapControllers();
// Serve the wasm project if no other matches
app.MapFallbackToFile("index.html");

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
    //.AddAdditionalAssemblies([typeof(MainLayout).Assembly]);

app.Run();
