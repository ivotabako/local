using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LocalEnterprise.Auth.Security;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

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
    string.IsNullOrWhiteSpace(jwtOptions.Audience) ||
    string.IsNullOrWhiteSpace(jwtOptions.SigningKey))
{
    throw new InvalidOperationException("Jwt settings are missing. Configure Jwt:Issuer, Jwt:Audience, and Jwt:SigningKey.");
}

var users = builder.Configuration.GetSection("Auth:Users").Get<List<AuthUser>>() ?? [];
if (users.Count == 0 || users.Any(x =>
        string.IsNullOrWhiteSpace(x.Username) ||
        string.IsNullOrWhiteSpace(x.Password)))
{
    throw new InvalidOperationException(
        "No valid auth users configured. Add Auth:Users entries with username/password in user secrets or appsettings.");
}

var app = builder.Build();

app.UseHttpsRedirection();
app.UseCors("Frontend");

app.MapGet("/", () => Results.Ok(new { service = "LocalEnterprise.Auth", status = "running" }));

app.MapPost("/connect/token", (TokenRequest request) =>
{
    var match = users.FirstOrDefault(x =>
        string.Equals(x.Username, request.Username, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(x.Password, request.Password, StringComparison.Ordinal));

    if (match is null)
    {
        return Results.Unauthorized();
    }

    var now = DateTime.UtcNow;
    var expires = now.AddMinutes(Math.Max(5, jwtOptions.AccessTokenMinutes));

    var claims = new List<Claim>
    {
        new(JwtRegisteredClaimNames.Sub, match.Username),
        new(JwtRegisteredClaimNames.UniqueName, match.Username),
        new("scope", "localenterprise.api")
    };

    foreach (var role in match.Roles)
    {
        claims.Add(new Claim(ClaimTypes.Role, role));
    }

    var credentials = new SigningCredentials(
        new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
        SecurityAlgorithms.HmacSha256);

    var token = new JwtSecurityToken(
        issuer: jwtOptions.Issuer,
        audience: jwtOptions.Audience,
        claims: claims,
        notBefore: now,
        expires: expires,
        signingCredentials: credentials);

    var encoded = new JwtSecurityTokenHandler().WriteToken(token);

    return Results.Ok(new
    {
        access_token = encoded,
        token_type = "Bearer",
        expires_in = (int)(expires - now).TotalSeconds,
        scope = "localenterprise.api"
    });
});

app.Run();

public sealed record TokenRequest(string Username, string Password);
