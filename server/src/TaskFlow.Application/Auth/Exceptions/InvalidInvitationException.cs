namespace TaskFlow.Application.Auth.Exceptions;

public sealed class InvalidInvitationException() : Exception("This invitation is invalid or has expired.");
