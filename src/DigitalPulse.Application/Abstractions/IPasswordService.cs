namespace DigitalPulse.Application.Abstractions;

public interface IPasswordService
{
    string Hash(string password);
    bool Verify(string hash, string password);
}
