using Blazored.LocalStorage;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.HttpOverrides;
using MySite.Components;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Initialize Serilog
builder.Host.UseSerilog((hostingContext, loggerConfiguration) => loggerConfiguration
    .ReadFrom.Configuration(hostingContext.Configuration)
    .WriteTo.Console()
    .WriteTo.File("logs/mysite-.txt", rollingInterval: RollingInterval.Day));

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddRazorPages();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.None; // Or Lax, depending on your requirements
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // Only do this if you're behind a reverse proxy that you control.
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// Access configuration from builder
var configuration = builder.Configuration;

// Google Auth
builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
    })
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
    })
    .AddGoogle(googleOptions =>
    {
        googleOptions.ClientId = configuration["Authentication:Google:ClientId"];
        googleOptions.ClientSecret = configuration["Authentication:Google:ClientSecret"];
        googleOptions.CallbackPath = new PathString("/signin-google"); // Ensure this matches the Google Console setup
    });
builder.Services.AddOptions();
builder.Services.AddAuthorizationCore();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<HttpContextAccessor>();
// Register HttpClient for the whole applcation
builder.Services.AddHttpClient();
builder.Services.AddScoped<HttpClient>();
builder.Services.AddBlazoredLocalStorage();
//builder.Services.AddScoped<AuthenticationStateProvider, ApiAuthenticationStateProvider>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/login", async context =>
{
    // Example of handling the sign-in process
    var result = await context.AuthenticateAsync("Google");

    if (result?.Succeeded ?? false)
    {
        // Optionally, issue a local sign-in to persist the user session on the server

        // Redirect to the Blazor app with a success indicator
        context.Response.Redirect("/?auth=success");
    }
    else
    {    // Issue a challenge to the external login provider (Google)
        await context.ChallengeAsync(GoogleDefaults.AuthenticationScheme, new AuthenticationProperties
        {
            // This redirectUri is where Google will redirect the user after they authenticate
            // Adjust it as necessary, depending on your application's flow
            RedirectUri = "/" // Assuming you want to redirect back to the app's home page after login
        });
    }
});

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.Run();
