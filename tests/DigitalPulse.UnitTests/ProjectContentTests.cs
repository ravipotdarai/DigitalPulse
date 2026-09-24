using DigitalPulse.Domain.Projects;
using Xunit;

namespace DigitalPulse.UnitTests;

public sealed class ProjectContentTests
{
    [Fact]
    public void None_permission_blocks_every_variant()
    {
        var project = Sample("None");
        Assert.False(project.AllowsPublication(ContentVariantKind.WebsiteCaseStudy));
        Assert.False(project.AllowsPublication(ContentVariantKind.FacebookPost));
    }

    [Fact]
    public void Partial_permission_allows_case_study_and_whatsapp_drafts_only()
    {
        var project = Sample("Partial");
        Assert.True(project.AllowsPublication(ContentVariantKind.WebsiteCaseStudy));
        Assert.True(project.AllowsPublication(ContentVariantKind.WhatsAppTemplateDraft));
        Assert.False(project.AllowsPublication(ContentVariantKind.LinkedInPost));
    }

    [Fact]
    public void Factory_assembles_twelve_record_backed_variants()
    {
        var drafts = ProjectContentFactory.Build(Sample("Full"), ["Roasting"]);
        Assert.Equal(12, drafts.Count);
        Assert.Contains(drafts, d => d.Kind == ContentVariantKind.WhatsAppSessionMessage);
        Assert.All(drafts, d => Assert.Contains("Harbour Roast", d.Title, StringComparison.Ordinal));
        Assert.DoesNotContain(drafts, d => d.Body.Contains("AI generated", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Approval_holds_variants_outside_permission_scope()
    {
        var project = Sample("None");
        var variant = ContentVariant.Draft(project.TenantId, Guid.NewGuid(), ContentVariantKind.GooglePost, "Title", "Body");
        variant.ApproveForScope(project.AllowsPublication(variant.Kind));
        Assert.Equal(ContentItemStatus.Hold, variant.Status);
        Assert.Contains("permission scope", variant.PublicationHold, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Published", variant.Status.ToString(), StringComparison.Ordinal);
    }

    private static Project Sample(string permission) =>
        Project.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Harbour Roast",
            "Harbour Cafe",
            "FOOD",
            "Mumbai",
            "A flagship roast program.",
            "Repeat wholesale orders.",
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 3, 1),
            Enum.Parse<ProjectPermissionScope>(permission),
            ProjectConfidentiality.Internal);
}
