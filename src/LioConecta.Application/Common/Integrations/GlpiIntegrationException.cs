namespace LioConecta.Application.Common.Integrations;

public sealed class GlpiIntegrationException(string message, Exception? innerException = null)
    : Exception(message, innerException);
