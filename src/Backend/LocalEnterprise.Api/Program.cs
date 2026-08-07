using LocalEnterprise.Application;
using LocalEnterprise.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHealthChecks();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
}

app.UseHttpsRedirection();

app.MapGet("/", () => Results.Ok(new { service = "LocalEnterprise.Api", status = "running" }));
app.MapGet("/error", () => Results.Problem("Unexpected error"));
app.MapHealthChecks("/healthz");

app.Run();
