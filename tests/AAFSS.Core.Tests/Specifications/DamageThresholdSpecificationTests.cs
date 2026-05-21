using AAFSS.Core.Models;
using AAFSS.Core.Specifications;
using FluentAssertions;
using Xunit;

namespace AAFSS.Core.Tests.Specifications;

public class DamageThresholdSpecificationTests
{
    private readonly DamageThresholdSpecification _spec;

    public DamageThresholdSpecificationTests()
    {
        _spec = new DamageThresholdSpecification
        {
            TargetDamage = 1.0,
            GreenThreshold = 0.05,
            YellowThreshold = 0.10
        };
    }

    [Theory]
    [InlineData(1.0, ValidationLevel.Green)]
    [InlineData(0.97, ValidationLevel.Green)]
    [InlineData(1.04, ValidationLevel.Green)]
    [InlineData(0.93, ValidationLevel.Yellow)]
    [InlineData(1.08, ValidationLevel.Yellow)]
    [InlineData(0.85, ValidationLevel.Red)]
    [InlineData(1.20, ValidationLevel.Red)]
    public void Evaluate_ShouldReturnCorrectLevel(double actual, ValidationLevel expected)
    {
        _spec.Evaluate(actual).Should().Be(expected);
    }

    [Fact]
    public void Evaluate_ZeroTarget_ShouldReturnNotValidated()
    {
        var spec = new DamageThresholdSpecification { TargetDamage = 0 };
        spec.Evaluate(1.5).Should().Be(ValidationLevel.NotValidated);
    }

    [Fact]
    public void IsSatisfiedBy_Passed_ShouldReturnTrue()
    {
        var spectrum = new CompiledSpectrum { ValidationStatus = ValidationStatus.Passed };
        _spec.IsSatisfiedBy(spectrum).Should().BeTrue();
    }

    [Fact]
    public void IsSatisfiedBy_Warning_ShouldReturnTrue()
    {
        var spectrum = new CompiledSpectrum { ValidationStatus = ValidationStatus.Warning };
        _spec.IsSatisfiedBy(spectrum).Should().BeTrue();
    }

    [Fact]
    public void IsSatisfiedBy_Failed_ShouldReturnFalse()
    {
        var spectrum = new CompiledSpectrum { ValidationStatus = ValidationStatus.Failed };
        _spec.IsSatisfiedBy(spectrum).Should().BeFalse();
    }

    [Fact]
    public void IsSatisfiedBy_Pending_ShouldReturnFalse()
    {
        var spectrum = new CompiledSpectrum { ValidationStatus = ValidationStatus.Pending };
        _spec.IsSatisfiedBy(spectrum).Should().BeFalse();
    }

    [Fact]
    public void GetValidationStatus_Green_ShouldReturnPassed()
    {
        _spec.GetValidationStatus(1.01).Should().Be(ValidationStatus.Passed);
    }

    [Fact]
    public void GetValidationStatus_Yellow_ShouldReturnWarning()
    {
        _spec.GetValidationStatus(1.07).Should().Be(ValidationStatus.Warning);
    }

    [Fact]
    public void GetValidationStatus_Red_ShouldReturnFailed()
    {
        _spec.GetValidationStatus(0.80).Should().Be(ValidationStatus.Failed);
    }

    [Fact]
    public void Normalize_ShouldComputeRatio()
    {
        _spec.Normalize(0.95).Should().BeApproximately(0.95, 0.001);
        _spec.Normalize(1.10).Should().BeApproximately(1.10, 0.001);
    }

    [Fact]
    public void Normalize_ZeroTarget_ShouldReturnNaN()
    {
        var spec = new DamageThresholdSpecification { TargetDamage = 0 };
        spec.Normalize(1.0).Should().Be(double.NaN);
    }
}
