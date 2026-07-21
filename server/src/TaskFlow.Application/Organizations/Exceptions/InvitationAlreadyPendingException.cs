namespace TaskFlow.Application.Organizations.Exceptions;

public sealed class InvitationAlreadyPendingException(string email) : Exception($"An invitation is already pending for '{email}'.");
