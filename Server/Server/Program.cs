using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using Server;
using System.Security.Claims;
using static OpenIddict.Abstractions.OpenIddictConstants;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders().AddConsole();
builder.Services.AddHttpsRedirection(x =>
{
    x.HttpsPort = 5001;
});

builder.Services.AddDbContext<ApplicationDbContext>(x =>
{
    x.UseInMemoryDatabase("AuthDb");
    x.UseOpenIddict();
});

builder.Services
    .AddOpenIddict()
    .AddCore(x =>
    {
        x.UseEntityFrameworkCore().UseDbContext<ApplicationDbContext>();
    })
    .AddServer(x =>
    {
        x.SetAuthorizationEndpointUris("/connect/authorize").SetTokenEndpointUris("/connect/token");
        x.RegisterScopes(Scopes.OpenId, Scopes.Profile, Scopes.Email);
        x.AllowAuthorizationCodeFlow().RequireProofKeyForCodeExchange();
        x.AddEphemeralEncryptionKey().AddEphemeralSigningKey().DisableAccessTokenEncryption();
        x.UseAspNetCore().EnableAuthorizationEndpointPassthrough().EnableTokenEndpointPassthrough();
    });

builder.Services.AddHostedService<ClientSeeder>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie();
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/connect/authorize", async context =>
{
    var request = context.GetOpenIddictServerRequest() ?? throw new InvalidOperationException("Richiesta non valida");

    var identity = new ClaimsIdentity(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme, Claims.Name, Claims.Role);
    identity.AddClaim(Claims.Subject, "dummy_user_id");
    identity.AddClaim(Claims.Name, "Test User");

    var principal = new ClaimsPrincipal(identity);
    principal.SetScopes(Scopes.OpenId, Scopes.Profile, Scopes.Email);

    await context.SignInAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme, principal);

    context.Response.Clear();

    var template = await File.ReadAllTextAsync("page.html");
    var code = Uri.EscapeDataString(context.GetOpenIddictServerResponse()!.Code!);
    var state = Uri.EscapeDataString(context.GetOpenIddictServerResponse()!.State!);
    var redirect = new UriBuilder(request.RedirectUri!)
    {
        Query = $"code={code}&state={state}"
    }.Uri.ToString();

    var html = string.Format(template, redirect);

    context.Response.ContentType = "text/html; charset=utf-8";
    await context.Response.WriteAsync(html);
});

app.MapPost("/connect/token", async context =>
{
    await context.ChallengeAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
});

app.Run();