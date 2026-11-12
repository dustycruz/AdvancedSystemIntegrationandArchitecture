namespace Order.Api.Saga;

/// <summary>
/// A saga reply arrived before the transaction that recorded the previous step had committed (the next
/// command is published just before that commit, and a fast participant can answer within milliseconds).
/// Throwing makes the consumer retry the message after a short delay instead of dropping it.
/// </summary>
public sealed class SagaStepNotReadyException(string message) : Exception(message);
