using System.Net;

namespace SmartShip.Core.Exceptions;

public abstract class SmartShipException : Exception
{
    protected SmartShipException(HttpStatusCode statusCode, string message, string? errorCode = null, Exception? innerException = null)
        : base(message, innerException)
    {
        StatusCode = (int)statusCode;
        ErrorCode = errorCode;
    }

    public int StatusCode { get; }

    public string? ErrorCode { get; }
}

public sealed class SmartShipBadRequestException : SmartShipException
{
    public SmartShipBadRequestException(string message, string? errorCode = null, Exception? innerException = null)
        : base(HttpStatusCode.BadRequest, message, errorCode, innerException)
    {
    }
}

public sealed class SmartShipUnauthorizedException : SmartShipException
{
    public SmartShipUnauthorizedException(string message, string? errorCode = null, Exception? innerException = null)
        : base(HttpStatusCode.Unauthorized, message, errorCode, innerException)
    {
    }
}

public sealed class SmartShipForbiddenException : SmartShipException
{
    public SmartShipForbiddenException(string message, string? errorCode = null, Exception? innerException = null)
        : base(HttpStatusCode.Forbidden, message, errorCode, innerException)
    {
    }
}

public sealed class SmartShipNotFoundException : SmartShipException
{
    public SmartShipNotFoundException(string message, string? errorCode = null, Exception? innerException = null)
        : base(HttpStatusCode.NotFound, message, errorCode, innerException)
    {
    }
}

public sealed class SmartShipConflictException : SmartShipException
{
    public SmartShipConflictException(string message, string? errorCode = null, Exception? innerException = null)
        : base(HttpStatusCode.Conflict, message, errorCode, innerException)
    {
    }
}

public sealed class SmartShipValidationException : SmartShipException
{
    public SmartShipValidationException(
        string message,
        IReadOnlyDictionary<string, string[]> errors,
        string? errorCode = null,
        Exception? innerException = null)
        : base(HttpStatusCode.BadRequest, message, errorCode, innerException)
    {
        Errors = errors;
    }

    public IReadOnlyDictionary<string, string[]> Errors { get; }
}
