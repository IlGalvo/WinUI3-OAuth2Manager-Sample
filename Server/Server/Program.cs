using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using OpenIddict.Validation.AspNetCore;
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
    })
    .AddValidation(options =>
    {
        // Se il server e le API sono nello stesso progetto, puoi usare UseLocalServer
        options.UseLocalServer();
        options.UseAspNetCore();
    });

builder.Services.AddHostedService<ClientSeeder>();

builder.Services.AddAuthentication(options =>
{
    // Imposta lo schema di default per la validazione dei token.
    options.DefaultAuthenticateScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
}).AddCookie(); // se vuoi mantenere anche il supporto per i cookie
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
    var request = context.GetOpenIddictServerRequest() ??
                  throw new InvalidOperationException("Richiesta non valida.");

    if (request.IsAuthorizationCodeGrantType())
    {
        // Normalmente recuperi il principal associato al codice. 
        // Per semplicità, qui lo ricrei – in produzione verifica che il codice non
        // sia già stato consumato e realizza tutte le validazioni necessarie.
        var identity = new ClaimsIdentity(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        identity.AddClaim(OpenIddict.Abstractions.OpenIddictConstants.Claims.Subject, "dummy_user_id");
        identity.AddClaim(OpenIddict.Abstractions.OpenIddictConstants.Claims.Name, "Test User");

        var principal = new ClaimsPrincipal(identity);
        principal.SetScopes(request.GetScopes());

        await context.SignInAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme, principal);
    }
    else
    {
        // Se il grant type non viene riconosciuto, lancia una Challenge oppure 
        // restituisci un errore.
        await context.ChallengeAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }
});

app.MapGet("/connect/userinfo", async context =>
{
    // Verifica che la richiesta sia autenticata
    var user = context.User;

    if (user?.Identity is null || !user.Identity.IsAuthenticated)
    {
        context.Response.StatusCode = 401;
        await context.Response.WriteAsync("Non autorizzato.");
        return;
    }

    // Crea l'oggetto da restituire. Puoi includere più claim se necessario.
    var userInfo = new
    {
        sub = user.FindFirst(Claims.Subject)?.Value,
        name = user.FindFirst(Claims.Name)?.Value,
        email = user.FindFirst(Claims.Email)?.Value
    };

    // Restituisci il JSON con le informazioni dell'utente
    await context.Response.WriteAsJsonAsync(userInfo);
});

app.Run();