namespace TaskFlow.Application.Organizations.Dtos;

public sealed record OrganizationResponse(Guid Id, string Name, string Slug, int MemberCount);
