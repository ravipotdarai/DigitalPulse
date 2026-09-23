using DigitalPulse.Domain.Directories;
using Xunit;

namespace DigitalPulse.UnitTests;

public sealed class DirectoryTaskTests
{
    [Fact]
    public void Verification_requires_every_assisted_step()
    {
        var task = DirectoryTask.Prepare(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "INDIAMART",
            "Harbour Coffee",
            "+91 22 1234 5678",
            "https://harbour.example",
            "Cafe",
            "Pour-over",
            [("Sign in", "Official login"), ("Update name", "Harbour Coffee")]);

        Assert.Throws<InvalidOperationException>(() => task.Verify("Looks correct"));
        foreach (var step in task.Steps)
        {
            task.CompleteStep(step.Id);
        }

        task.Verify("Confirmed on the official seller profile.");
        Assert.Equal(DirectoryTaskStatus.Verified, task.Status);
        Assert.NotNull(task.VerifiedAtUtc);
    }
}
