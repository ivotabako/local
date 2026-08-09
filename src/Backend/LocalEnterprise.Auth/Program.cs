using System.Security.Claims;
using LocalEnterprise.Auth.Security;
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

builder.Services.AddSingleton<IPasswordHasher<AuthUser>, PasswordHasher<AuthUser>>();
builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? ["https://localhost:4200", "http://localhost:4200"];

        policy.WithOrigins(origins)
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
        options.SetTokenEndpointUris("connect/token");
        options.AllowPasswordFlow();
        options.AcceptAnonymousClients();
        options.DisableAccessTokenEncryption();
        options.SetAccessTokenLifetime(TimeSpan.FromMinutes(Math.Max(5, jwtOptions.AccessTokenMinutes)));
        options.RegisterScopes("localenterprise.api");
        options.AddEphemeralEncryptionKey();
        options.AddEphemeralSigningKey();
        options.AddEventHandler<OpenIddict.Server.OpenIddictServerEvents.ValidateTokenRequestContext>(builder =>
            builder.UseInlineHandler(context =>
            {
                if (!context.Request.IsPasswordGrantType())
                {
                    context.Reject(
                        error: Errors.UnsupportedGrantType,
                        description: "Only the OAuth password grant is enabled for this local development server.",
                        uri: null);
                }

                return default;
            }));
        options.UseAspNetCore()
            .EnableTokenEndpointPassthrough();
    });

var app = builder.Build();

app.UseHttpsRedirection();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Ok(new { service = "LocalEnterprise.Auth", status = "running" }));

app.MapPost("/connect/token", async (HttpContext context, IPasswordHasher<AuthUser> passwordHasher) =>
{
    var form = await context.Request.ReadFormAsync();
    var grantType = form["grant_type"].ToString();
    if (!string.Equals(grantType, "password", StringComparison.Ordinal))
    {
        return Results.BadRequest(new
        {
            error = Errors.UnsupportedGrantType,
            error_description = "Only the OAuth password grant is enabled for this local development server."
        });
    }

    var username = form["username"].ToString();
    var password = form["password"].ToString();

    var match = users.FirstOrDefault(x =>
        string.Equals(x.Username, username, StringComparison.OrdinalIgnoreCase));

    if (match is null)
    {
        return Results.Unauthorized();
    }

    var passwordVerification = passwordHasher.VerifyHashedPassword(match, match.PasswordHash, password);
    if (passwordVerification == PasswordVerificationResult.Failed)
    {
        return Results.Unauthorized();
    }

    var identity = new ClaimsIdentity(
        authenticationType: TokenValidationParameters.DefaultAuthenticationType,
        nameType: Claims.Name,
        roleType: Claims.Role);

    identity.SetClaim(Claims.Subject, match.Username);
    identity.SetClaim(Claims.Name, match.Username);
    identity.SetClaim(Claims.PreferredUsername, match.Username);

    foreach (var role in match.Roles)
    {
        identity.AddClaim(new Claim(Claims.Role, role));
    }

    foreach (var claim in identity.Claims)
    {
        claim.SetDestinations(claim.Type switch
        {
            Claims.Name or Claims.PreferredUsername or Claims.Role => [Destinations.AccessToken],
            _ => [Destinations.AccessToken]
        });
    }

    var principal = new ClaimsPrincipal(identity);
    principal.SetScopes("localenterprise.api");
    principal.SetResources(jwtOptions.Audience);

    return Results.SignIn(principal, authenticationScheme: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
});

app.Run();
