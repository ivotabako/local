using System.Security.Claims;
using LocalEnterprise.Auth.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;

using static OpenIddict.Abstractions.OpenIddictConstants;

if (args.Length == 2 && string.Equals(args[0], "hash-password", StringComparison.OrdinalIgnoreCase))
{
    var hasher = new PasswordHasher<AuthUser>();
    var hash = hasher.HashPassword(new AuthUser { Username = "local-dev" }, args[1]);
    Console.WriteLine(hash);
    return;
}

var builder = WebApplication.CreateBuilder(args);

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["https://localhost:4200", "http://localhost:4200"];

builder.Services.AddSingleton<IPasswordHasher<AuthUser>, PasswordHasher<AuthUser>>();
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.LoginPath = "/account/login";
    });
builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
if (string.IsNullOrWhiteSpace(jwtOptions.Issuer) ||
    string.IsNullOrWhiteSpace(jwtOptions.Audience))
{
    throw new InvalidOperationException("Jwt settings are missing. Configure Jwt:Issuer and Jwt:Audience.");
}

var users = builder.Configuration.GetSection("Auth:Users").Get<List<AuthUser>>() ?? [];
if (users.Count == 0 || users.Any(x =>
        string.IsNullOrWhiteSpace(x.Username) ||
    string.IsNullOrWhiteSpace(x.PasswordHash)))
{
    throw new InvalidOperationException(
    "No valid auth users configured. Add Auth:Users entries with username/password hash in user secrets or environment variables.");
}

builder.Services.AddOpenIddict()
    .AddServer(options =>
    {
        options.EnableDegradedMode();
        options.SetIssuer(new Uri(jwtOptions.Issuer));
        options.SetAuthorizationEndpointUris("connect/authorize");
        options.SetTokenEndpointUris("connect/token");
        options.AllowAuthorizationCodeFlow();
        options.RequireProofKeyForCodeExchange();
        options.AcceptAnonymousClients();
        options.DisableAccessTokenEncryption();
        options.SetAccessTokenLifetime(TimeSpan.FromMinutes(Math.Max(5, jwtOptions.AccessTokenMinutes)));
        options.RegisterScopes("localenterprise.api");
        options.AddEphemeralEncryptionKey();
        options.AddEphemeralSigningKey();
        options.AddEventHandler<OpenIddict.Server.OpenIddictServerEvents.ValidateAuthorizationRequestContext>(eventBuilder =>
            eventBuilder.UseInlineHandler(context =>
            {
                var responseType = context.Request.ResponseType?.ToString();
                if (!string.Equals(responseType, "code", StringComparison.Ordinal))
                {
                    context.Reject(
                        error: Errors.UnsupportedResponseType,
                        description: "Only the authorization code flow is enabled for this local development server.",
                        uri: null);
                }

                return default;
            }));
        options.AddEventHandler<OpenIddict.Server.OpenIddictServerEvents.ValidateTokenRequestContext>(builder =>
            builder.UseInlineHandler(context =>
            {
                if (!context.Request.IsAuthorizationCodeGrantType())
                {
                    context.Reject(
                        error: Errors.UnsupportedGrantType,
                        description: "Only the OAuth authorization code flow is enabled for this local development server.",
                        uri: null);
                }

                return default;
            }));
        options.UseAspNetCore()
            .EnableAuthorizationEndpointPassthrough();
    });

var app = builder.Build();

app.UseHttpsRedirection();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Ok(new { service = "LocalEnterprise.Auth", status = "running" }));

app.MapGet("/connect/authorize", async (HttpContext context) =>
{
    var authenticatedPrincipal = (await context.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme)).Principal;
    if (authenticatedPrincipal is null)
    {
        var returnUrl = context.Request.Path + context.Request.QueryString;
        return Results.Redirect($"/account/login?returnUrl={Uri.EscapeDataString(returnUrl)}");
    }

    var username = authenticatedPrincipal.FindFirstValue(ClaimTypes.NameIdentifier) ??
                   authenticatedPrincipal.Identity?.Name;

    if (string.IsNullOrWhiteSpace(username))
    {
        return Results.Unauthorized();
    }

    var identity = new ClaimsIdentity(
        authenticationType: TokenValidationParameters.DefaultAuthenticationType,
        nameType: Claims.Name,
        roleType: Claims.Role);

    identity.SetClaim(Claims.Subject, username);
    identity.SetClaim(Claims.Name, username);
    identity.SetClaim(Claims.PreferredUsername, username);

    foreach (var role in authenticatedPrincipal.FindAll(ClaimTypes.Role))
    {
        identity.AddClaim(new Claim(Claims.Role, role.Value));
    }

    foreach (var claim in identity.Claims)
    {
        claim.SetDestinations(claim.Type switch
        {
            Claims.Name or Claims.PreferredUsername or Claims.Role => [Destinations.AccessToken],
            _ => [Destinations.AccessToken]
        });
    }

    var tokenPrincipal = new ClaimsPrincipal(identity);
    tokenPrincipal.SetScopes("localenterprise.api");
    tokenPrincipal.SetResources(jwtOptions.Audience);

    return Results.SignIn(tokenPrincipal, authenticationScheme: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
});

app.MapGet("/account/login", (string? returnUrl, string? error) =>
{
    var safeReturnUrl = string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl;
    var errorHtml = string.IsNullOrWhiteSpace(error)
            ? string.Empty
            : $"<p style='color:#c62828;margin:0 0 1rem 0'>{System.Net.WebUtility.HtmlEncode(error)}</p>";

    var html = $"""
<!doctype html>
<html lang='en'>
    <head>
        <meta charset='utf-8' />
        <meta name='viewport' content='width=device-width, initial-scale=1' />
        <title>LocalEnterprise Login</title>
    </head>
    <body style='font-family:Segoe UI,Tahoma,sans-serif;background:#f3f4f6;margin:0;padding:2rem;'>
        <main style='max-width:420px;margin:2rem auto;background:#fff;border-radius:12px;padding:1.5rem;box-shadow:0 10px 30px rgba(0,0,0,.08);'>
            <h1 style='margin-top:0;'>Sign in</h1>
            <p style='margin-top:0;color:#4b5563;'>Local development authorization server</p>
            {errorHtml}
            <form method='post' action='/account/login'>
                <input type='hidden' name='returnUrl' value='{System.Net.WebUtility.HtmlEncode(safeReturnUrl)}' />
                <label style='display:block;margin-bottom:.75rem;'>Username
                    <input name='username' autocomplete='username' required style='display:block;width:100%;padding:.625rem;margin-top:.25rem;border:1px solid #d1d5db;border-radius:8px;' />
                </label>
                <label style='display:block;margin-bottom:1rem;'>Password
                    <input type='password' name='password' autocomplete='current-password' required style='display:block;width:100%;padding:.625rem;margin-top:.25rem;border:1px solid #d1d5db;border-radius:8px;' />
                </label>
                <button type='submit' style='background:#111827;color:#fff;border:0;border-radius:8px;padding:.625rem 1rem;cursor:pointer;'>Continue</button>
            </form>
        </main>
    </body>
</html>
""";

    return Results.Content(html, "text/html; charset=utf-8");
});

app.MapPost("/account/login", async (HttpContext context, IPasswordHasher<AuthUser> passwordHasher) =>
{
    var form = await context.Request.ReadFormAsync();
    var username = form["username"].ToString();
    var password = form["password"].ToString();
    var returnUrl = form["returnUrl"].ToString();

    var match = users.FirstOrDefault(x => string.Equals(x.Username, username, StringComparison.OrdinalIgnoreCase));
    if (match is null ||
            passwordHasher.VerifyHashedPassword(match, match.PasswordHash, password) == PasswordVerificationResult.Failed)
    {
        var redirectUrl = $"/account/login?returnUrl={Uri.EscapeDataString(returnUrl)}&error={Uri.EscapeDataString("Invalid credentials.")}";
        return Results.Redirect(redirectUrl);
    }

    var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);
    identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, match.Username));
    identity.AddClaim(new Claim(ClaimTypes.Name, match.Username));
    foreach (var role in match.Roles)
    {
        identity.AddClaim(new Claim(ClaimTypes.Role, role));
    }

    await context.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties
            {
                IsPersistent = false,
                ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(Math.Max(5, jwtOptions.AccessTokenMinutes))
            });

    if (string.IsNullOrWhiteSpace(returnUrl) || !Uri.IsWellFormedUriString(returnUrl, UriKind.Relative))
    {
        returnUrl = "/";
    }

    return Results.Redirect(returnUrl);
});

app.MapGet("/account/logout", async (HttpContext context, string? postLogoutRedirectUri) =>
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

    if (IsAllowedPostLogoutRedirect(postLogoutRedirectUri, allowedOrigins))
    {
        return Results.Redirect(postLogoutRedirectUri!);
    }

    return Results.Redirect("/");
});

static bool IsAllowedPostLogoutRedirect(string? redirectUri, IEnumerable<string> allowedOriginList)
{
    if (string.IsNullOrWhiteSpace(redirectUri))
    {
        return false;
    }

    if (!Uri.TryCreate(redirectUri, UriKind.Absolute, out var parsedRedirect) ||
        (parsedRedirect.Scheme != Uri.UriSchemeHttps && parsedRedirect.Scheme != Uri.UriSchemeHttp))
    {
        return false;
    }

    foreach (var allowedOrigin in allowedOriginList)
    {
        if (!Uri.TryCreate(allowedOrigin, UriKind.Absolute, out var parsedAllowedOrigin))
        {
            continue;
        }

        var sameHost = string.Equals(parsedRedirect.Host, parsedAllowedOrigin.Host, StringComparison.OrdinalIgnoreCase);
        var sameScheme = string.Equals(parsedRedirect.Scheme, parsedAllowedOrigin.Scheme, StringComparison.OrdinalIgnoreCase);
        var samePort = parsedRedirect.Port == parsedAllowedOrigin.Port;

        if (sameHost && sameScheme && samePort)
        {
            return true;
        }
    }

    return false;
}

app.Run();
