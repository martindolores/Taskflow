namespace TaskFlow.Application.Auth.Exceptions;

public sealed class EmailNotConfirmedException() : Exception("Please confirm your email address before logging in.");
