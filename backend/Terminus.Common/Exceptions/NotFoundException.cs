namespace Terminus.Common.Exceptions;

/// <summary>Traženi resurs ne postoji (HTTP 404).</summary>
public class NotFoundException(string message, Exception? innerException = null)
    : Exception(message, innerException);
