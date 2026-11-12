namespace Store.Messaging.Publishing;

/// <summary>The broker did not confirm a message after every publish retry.</summary>
public sealed class MessagePublishException(string message, Exception? innerException = null)
    : Exception(message, innerException);
