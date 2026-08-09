using System.Security.Claims;
using LocalEnterprise.Application.Identity;
using OpenIddict.Abstractions;

namespace LocalEnterprise.Auth.Endpoints;

public static class UserAccountEndpoints
{
    public static RouteGroupBuilder MapUserAccountEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users").WithTags("Users");

        group.MapGet("/me", async (ClaimsPrincipal principal, IUserAccountService service, CancellationToken cancellationToken) =>
        {
            var subject = principal.FindFirstValue(OpenIddictConstants.Claims.Subject);
            if (!Guid.TryParse(subject, out var userId))
            {
                return Results.Unauthorized();
            }

            var user = await service.GetByIdAsync(userId, cancellationToken);
            return user is null ? Results.NotFound() : Results.Ok(user);
        }).RequireAuthorization("AuthApiScope");

        group.MapGet("/", async (IUserAccountService service, CancellationToken cancellationToken) =>
        {
            var users = await service.ListAsync(cancellationToken);
            return Results.Ok(users);
        }).RequireAuthorization("AuthAdmin");

        group.MapGet("/{id:guid}", async (Guid id, IUserAccountService service, CancellationToken cancellationToken) =>
        {
            var user = await service.GetByIdAsync(id, cancellationToken);
            return user is null ? Results.NotFound() : Results.Ok(user);
        }).RequireAuthorization("AuthAdmin");

        group.MapPost("/", async (CreateUserAccountRequest request, ClaimsPrincipal principal, IUserAccountService service, CancellationToken cancellationToken) =>
        {
            var createdBy = principal.FindFirstValue(OpenIddictConstants.Claims.Name) ?? principal.Identity?.Name;
            var result = await service.CreateAsync(request, createdBy, cancellationToken);
            if (!result.Succeeded)
            {
                return result.ErrorCode == UserAccountErrors.DuplicateUsername
                    ? Results.Conflict(new { error = result.Error })
                    : Results.BadRequest(new { error = result.Error });
            }

            return Results.Created($"/api/users/{result.User!.Id}", result.User);
        }).RequireAuthorization("AuthAdmin");

        group.MapPost("/change-password", async (ChangePasswordRequest request, ClaimsPrincipal principal, IUserAccountService service, CancellationToken cancellationToken) =>
        {
            var subject = principal.FindFirstValue(OpenIddictConstants.Claims.Subject);
            if (!Guid.TryParse(subject, out var userId))
            {
                return Results.Unauthorized();
            }

            var result = await service.ChangePasswordAsync(userId, request, cancellationToken);
            if (!result.Succeeded)
            {
                return result.ErrorCode switch
                {
                    UserAccountErrors.UserNotFound => Results.NotFound(),
                    UserAccountErrors.IncorrectCurrentPassword => Results.BadRequest(new { error = result.Error }),
                    UserAccountErrors.WeakPassword => Results.BadRequest(new { error = result.Error }),
                    _ => Results.BadRequest(new { error = result.Error })
                };
            }

            return Results.Ok(result.User);
        }).RequireAuthorization("AuthApiScope");

        group.MapPost("/{id:guid}/reset-password", async (Guid id, ResetPasswordRequest request, IUserAccountService service, CancellationToken cancellationToken) =>
        {
            var result = await service.ResetPasswordAsync(id, request.NewPassword, cancellationToken);
            if (!result.Succeeded)
            {
                return result.ErrorCode switch
                {
                    UserAccountErrors.UserNotFound => Results.NotFound(),
                    _ => Results.BadRequest(new { error = result.Error })
                };
            }

            return Results.Ok(result.User);
        }).RequireAuthorization("AuthAdmin");

        group.MapPost("/{id:guid}/lock", async (Guid id, IUserAccountService service, CancellationToken cancellationToken) =>
        {
            var result = await service.SetLockStateAsync(id, true, cancellationToken);
            return !result.Succeeded
                ? Results.NotFound()
                : Results.Ok(result.User);
        }).RequireAuthorization("AuthAdmin");

        group.MapPost("/{id:guid}/unlock", async (Guid id, IUserAccountService service, CancellationToken cancellationToken) =>
        {
            var result = await service.SetLockStateAsync(id, false, cancellationToken);
            return !result.Succeeded
                ? Results.NotFound()
                : Results.Ok(result.User);
        }).RequireAuthorization("AuthAdmin");

        group.MapPost("/me/2fa/enrollment", async (ClaimsPrincipal principal, IUserAccountService service, CancellationToken cancellationToken) =>
        {
            var subject = principal.FindFirstValue(OpenIddictConstants.Claims.Subject);
            if (!Guid.TryParse(subject, out var userId))
            {
                return Results.Unauthorized();
            }

            var result = await service.BeginTwoFactorEnrollmentAsync(userId, "LocalEnterprise", cancellationToken);
            if (!result.Succeeded)
            {
                return result.ErrorCode == UserAccountErrors.UserNotFound
                    ? Results.NotFound()
                    : Results.BadRequest(new { error = result.Error });
            }

            return Results.Ok(new TwoFactorEnrollmentDto(result.SharedSecret, result.ProvisioningUri, false));
        }).RequireAuthorization("AuthApiScope");

        group.MapPost("/me/2fa/verify", async (ClaimsPrincipal principal, VerifyTwoFactorCodeRequest request, IUserAccountService service, CancellationToken cancellationToken) =>
        {
            var subject = principal.FindFirstValue(OpenIddictConstants.Claims.Subject);
            if (!Guid.TryParse(subject, out var userId))
            {
                return Results.Unauthorized();
            }

            var result = await service.EnableTwoFactorAsync(userId, request.Code, cancellationToken);
            if (!result.Succeeded || result.User is null)
            {
                return result.ErrorCode switch
                {
                    UserAccountErrors.UserNotFound => Results.NotFound(),
                    _ => Results.BadRequest(new { error = result.Error })
                };
            }

            return Results.Ok(new TwoFactorVerificationResultDto(result.User, result.RecoveryCodes));
        }).RequireAuthorization("AuthApiScope");

        group.MapPut("/{id:guid}", async (Guid id, UpdateUserAccountRequest request, IUserAccountService service, CancellationToken cancellationToken) =>
        {
            var result = await service.UpdateAsync(id, request, cancellationToken);
            if (!result.Succeeded)
            {
                return result.ErrorCode switch
                {
                    UserAccountErrors.UserNotFound => Results.NotFound(),
                    UserAccountErrors.CannotRemoveLastAdminRole => Results.Conflict(new { error = result.Error }),
                    _ => Results.BadRequest(new { error = result.Error })
                };
            }

            return Results.Ok(result.User);
        }).RequireAuthorization("AuthAdmin");

        group.MapDelete("/{id:guid}", async (Guid id, ClaimsPrincipal principal, IUserAccountService service, CancellationToken cancellationToken) =>
        {
            Guid? actorId = Guid.TryParse(principal.FindFirstValue(OpenIddictConstants.Claims.Subject), out var parsedActorId)
                ? parsedActorId
                : null;

            var result = await service.DeleteAsync(id, actorId, cancellationToken);
            if (!result.Succeeded)
            {
                return result.ErrorCode switch
                {
                    UserAccountErrors.UserNotFound => Results.NotFound(),
                    UserAccountErrors.CannotDeleteCurrentUser => Results.BadRequest(new { error = result.Error }),
                    UserAccountErrors.CannotDeleteLastAdmin => Results.Conflict(new { error = result.Error }),
                    _ => Results.BadRequest(new { error = result.Error })
                };
            }

            return Results.NoContent();
        }).RequireAuthorization("AuthAdmin");

        return group;
    }
}