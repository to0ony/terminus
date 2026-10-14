namespace Terminus.Common.Exceptions;

/// <summary>Operacija je u sukobu s postojećim podacima (HTTP 409).</summary>
public class ConflictException(string message, Exception? innerException = null)
    : Exception(message, innerException);
