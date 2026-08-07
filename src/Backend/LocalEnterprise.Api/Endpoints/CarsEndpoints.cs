using LocalEnterprise.Application.Cars;
using Microsoft.AspNetCore.Authorization;

namespace LocalEnterprise.Api.Endpoints;

public static class CarsEndpoints
{
    public static RouteGroupBuilder MapCarsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/cars")
            .RequireAuthorization(new AuthorizeAttribute { Policy = "ApiScope" })
            .WithTags("Cars");

        group.MapGet("/", async (ICarService service, CancellationToken cancellationToken) =>
        {
            var items = await service.ListAsync(cancellationToken);
            return Results.Ok(items);
        });

        group.MapGet("/{id:guid}", async (Guid id, ICarService service, CancellationToken cancellationToken) =>
        {
            var item = await service.GetByIdAsync(id, cancellationToken);
            return item is null ? Results.NotFound() : Results.Ok(item);
        });

        group.MapPost("/", async (CreateCarRequest request, ICarService service, CancellationToken cancellationToken) =>
        {
            var result = await service.CreateAsync(request, cancellationToken);
            if (!result.Succeeded)
            {
                return Results.Conflict(new { error = result.Error });
            }

            return Results.Created($"/api/cars/{result.Car!.Id}", result.Car);
        });

        group.MapPut("/{id:guid}", async (Guid id, UpdateCarRequest request, ICarService service, CancellationToken cancellationToken) =>
        {
            var result = await service.UpdateAsync(id, request, cancellationToken);
            if (!result.Succeeded)
            {
                if (string.Equals(result.Error, "Car not found.", StringComparison.Ordinal))
                {
                    return Results.NotFound();
                }

                return Results.Conflict(new { error = result.Error });
            }

            return Results.Ok(result.Car);
        });

        group.MapDelete("/{id:guid}", async (Guid id, ICarService service, CancellationToken cancellationToken) =>
        {
            var deleted = await service.DeleteAsync(id, cancellationToken);
            return deleted ? Results.NoContent() : Results.NotFound();
        });

        return group;
    }
}
