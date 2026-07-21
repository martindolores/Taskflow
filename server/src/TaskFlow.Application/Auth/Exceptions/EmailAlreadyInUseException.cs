namespace TaskFlow.Application.Auth.Exceptions;

public sealed class EmailAlreadyInUseException(string email) : Exception($"An account with email '{email}' already exists.");
