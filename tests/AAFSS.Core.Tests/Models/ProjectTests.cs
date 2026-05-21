using AAFSS.Core.Models;
using FluentAssertions;
using Xunit;

namespace AAFSS.Core.Tests.Models;

public class ProjectTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaults()
    {
        var project = new Project();

        project.Id.Should().NotBe(Guid.Empty);
        project.Name.Should().BeEmpty();
        project.Description.Should().BeEmpty();
        project.Metadata.Should().Be("{}");
        project.FilePath.Should().BeNull();
        project.Profiles.Should().BeEmpty();
        project.Spectra.Should().BeEmpty();
        project.Reports.Should().BeEmpty();
        project.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        project.ModifiedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void AddProfile_ShouldSetProjectIdAndAddToList()
    {
        var project = new Project();
        var profile = new MissionProfile { Name = "起飞-全加力" };

        project.AddProfile(profile);

        profile.ProjectId.Should().Be(project.Id);
        project.Profiles.Should().ContainSingle().Which.Should().Be(profile);
        project.ModifiedAt.Should().BeAfter(project.CreatedAt);
    }

    [Fact]
    public void AddProfile_WithNull_ShouldThrow()
    {
        var project = new Project();
        var act = () => project.AddProfile(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void RemoveProfile_Existing_ShouldRemoveAndUpdateTimestamp()
    {
        var project = new Project();
        var profile = new MissionProfile();
        project.AddProfile(profile);
        var modifiedBefore = project.ModifiedAt;

        System.Threading.Thread.Sleep(10);
        project.RemoveProfile(profile.Id);

        project.Profiles.Should().BeEmpty();
        project.ModifiedAt.Should().BeAfter(modifiedBefore);
    }

    [Fact]
    public void RemoveProfile_NonExisting_ShouldNotThrow()
    {
        var project = new Project();
        project.AddProfile(new MissionProfile());

        var act = () => project.RemoveProfile(Guid.NewGuid());

        act.Should().NotThrow();
        project.Profiles.Should().HaveCount(1);
    }

    [Fact]
    public void AddSpectrum_ShouldSetProjectIdAndAddToList()
    {
        var project = new Project();
        var spectrum = new CompiledSpectrum { Name = "Test Spectrum" };

        project.AddSpectrum(spectrum);

        spectrum.ProjectId.Should().Be(project.Id);
        project.Spectra.Should().ContainSingle().Which.Should().Be(spectrum);
    }

    [Fact]
    public void AddReport_ShouldSetProjectIdAndAddToList()
    {
        var project = new Project();
        var report = new GeneratedReport { TemplateName = "GJB_67" };

        project.AddReport(report);

        report.ProjectId.Should().Be(project.Id);
        project.Reports.Should().ContainSingle().Which.Should().Be(report);
    }

    [Fact]
    public void AddMultipleProfiles_ShouldAllHaveCorrectProjectId()
    {
        var project = new Project();
        var profiles = new[]
        {
            new MissionProfile { Name = "Profile 1" },
            new MissionProfile { Name = "Profile 2" },
            new MissionProfile { Name = "Profile 3" }
        };

        foreach (var p in profiles)
            project.AddProfile(p);

        project.Profiles.Should().HaveCount(3);
        project.Profiles.Should().AllSatisfy(p => p.ProjectId.Should().Be(project.Id));
    }
}
