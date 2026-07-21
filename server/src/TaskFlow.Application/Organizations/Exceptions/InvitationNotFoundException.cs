namespace TaskFlow.Application.Organizations.Exceptions;

public sealed class InvitationNotFoundException(Guid invitationId) : Exception($"Invitation '{invitationId}' was not found.");
