namespace TaskFlow.Infrastructure.Email;

public sealed class EmailOptions
{
    public BrevoOptions Brevo { get; init; } = new();

    public string FromAddress { get; init; } = string.Empty;

    public string FromName { get; init; } = string.Empty;

    public string FrontendBaseUrl { get; set; } = string.Empty;
}

public sealed class BrevoOptions
{
    public string? ApiKey { get; init; }
}
