using TaskFlow.Api.Filters;
using TaskFlow.Application.Organizations;
using TaskFlow.Application.Organizations.Dtos;

namespace TaskFlow.Api.Endpoints;

public static class OrganizationEndpoints
{
    public static IEndpointRouteBuilder MapOrganizationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/organization").RequireAuthorization();

        group.MapGet("/", async (IOrganizationService organizationService, CancellationToken cancellationToken) =>
        {
            var response = await organizationService.GetOrganizationAsync(cancellationToken);
            return Results.Ok(response);
        });

        group.MapGet("/members", async (IOrganizationService organizationService, CancellationToken cancellationToken) =>
        {
            var response = await organizationService.GetMembersAsync(cancellationToken);
            return Results.Ok(response);
        });

        group.MapPost("/invitations", async (CreateInvitationRequest request, IOrganizationService organizationService, CancellationToken cancellationToken) =>
            {
                var response = await organizationService.CreateInvitationAsync(request, cancellationToken);
                return Results.Created($"/api/organization/invitations/{response.Id}", response);
            })
            .RequireAuthorization("AdminOnly")
            .AddEndpointFilter<ValidationFilter<CreateInvitationRequest>>();

        group.MapGet("/invitations", async (IOrganizationService organizationService, CancellationToken cancellationToken) =>
            {
                var response = await organizationService.GetInvitationsAsync(cancellationToken);
                return Results.Ok(response);
            })
            .RequireAuthorization("AdminOnly");

        group.MapDelete("/invitations/{id:guid}", async (Guid id, IOrganizationService organizationService, CancellationToken cancellationToken) =>
            {
                await organizationService.RevokeInvitationAsync(id, cancellationToken);
                return Results.NoContent();
            })
            .RequireAuthorization("AdminOnly");

        group.MapPatch("/members/{userId:guid}/role", async (Guid userId, UpdateMemberRoleRequest request, IOrganizationService organizationService, CancellationToken cancellationToken) =>
            {
                var response = await organizationService.UpdateMemberRoleAsync(userId, request, cancellationToken);
                return Results.Ok(response);
            })
            .RequireAuthorization("AdminOnly")
            .AddEndpointFilter<ValidationFilter<UpdateMemberRoleRequest>>();

        group.MapDelete("/members/{userId:guid}", async (Guid userId, IOrganizationService organizationService, CancellationToken cancellationToken) =>
            {
                await organizationService.DeactivateMemberAsync(userId, cancellationToken);
                return Results.NoContent();
            })
            .RequireAuthorization("AdminOnly");

        return app;
    }
}
