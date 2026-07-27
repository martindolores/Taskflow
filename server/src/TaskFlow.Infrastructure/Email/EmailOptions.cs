namespace TaskFlow.Infrastructure.Email;

public sealed class EmailOptions
{
    public SmtpOptions Smtp { get; init; } = new();

    public string FromAddress { get; init; } = string.Empty;

    public string FromName { get; init; } = string.Empty;

    public string FrontendBaseUrl { get; set; } = string.Empty;
}

public sealed class SmtpOptions
{
    public string Host { get; init; } = string.Empty;

    public int Port { get; init; } = 587;

    public string? Username { get; init; }

    public string? Password { get; init; }
}
