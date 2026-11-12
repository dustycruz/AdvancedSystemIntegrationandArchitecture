namespace Store.Messaging;

/// <summary>Custom AMQP header names used by the platform.</summary>
public static class MessagingHeaders
{
    /// <summary>1-based processing attempt of a message; incremented each time a consumer retries it.</summary>
    public const string Attempt = "x-attempt";

    /// <summary>Last error a consumer hit for the message, kept when it is retried or dead-lettered.</summary>
    public const string LastError = "x-last-error";
}
