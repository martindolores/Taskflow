namespace TaskFlow.Application.Organizations.Exceptions;

public sealed class MemberNotFoundException(Guid userId) : Exception($"Member '{userId}' was not found.");
