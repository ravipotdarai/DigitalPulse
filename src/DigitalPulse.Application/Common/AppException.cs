namespace DigitalPulse.Application.Common;

public sealed class AppException : Exception
{
    public string Code { get; }
    public int StatusCode { get; }

    public AppException(string code, string message, int statusCode = 400) : base(message)
    {
        Code = code;
        StatusCode = statusCode;
    }

    public static AppException Unauthorized(string message = "Unauthorized.") =>
        new("unauthorized", message, 401);

    public static AppException Forbidden(string message = "Forbidden.") =>
        new("forbidden", message, 403);

    public static AppException NotFound(string message) =>
        new("not_found", message, 404);

    public static AppException Conflict(string message) =>
        new("conflict", message, 409);

    public static AppException Validation(string message) =>
        new("validation_failed", message, 400);
}
