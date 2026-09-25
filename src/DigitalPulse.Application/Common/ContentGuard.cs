using DigitalPulse.Domain.Safety;

namespace DigitalPulse.Application.Common;

public static class ContentGuard
{
    public static void Require(params string?[] parts)
    {
        try
        {
            ContentSafety.EnsureAllowed(parts);
        }
        catch (InvalidOperationException ex)
        {
            throw AppException.Validation(ex.Message);
        }
    }
}
