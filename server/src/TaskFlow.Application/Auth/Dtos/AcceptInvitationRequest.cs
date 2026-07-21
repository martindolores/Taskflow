namespace TaskFlow.Application.Auth.Dtos;

public sealed record AcceptInvitationRequest(string Token, string Password, string FirstName, string LastName);
