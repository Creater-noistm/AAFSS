using AAFSS.Core.Specifications;
using FluentAssertions;
using Xunit;

namespace AAFSS.Core.Tests.Specifications;

public class SpecificationPatternTests
{
    private class PositiveSpec : Specification<int>
    {
        public override bool IsSatisfiedBy(int candidate) => candidate > 0;
    }

    private class EvenSpec : Specification<int>
    {
        public override bool IsSatisfiedBy(int candidate) => candidate % 2 == 0;
    }

    [Fact]
    public void IsSatisfiedBy_ShouldReturnCorrectResult()
    {
        var spec = new PositiveSpec();
        spec.IsSatisfiedBy(5).Should().BeTrue();
        spec.IsSatisfiedBy(-1).Should().BeFalse();
    }

    [Fact]
    public void And_ShouldCombineSpecifications()
    {
        var spec = new PositiveSpec().And(new EvenSpec());

        spec.IsSatisfiedBy(4).Should().BeTrue();
        spec.IsSatisfiedBy(3).Should().BeFalse();
        spec.IsSatisfiedBy(-2).Should().BeFalse();
    }

    [Fact]
    public void Or_ShouldCombineWithOr()
    {
        var spec = new PositiveSpec().Or(new EvenSpec());

        spec.IsSatisfiedBy(3).Should().BeTrue();
        spec.IsSatisfiedBy(-2).Should().BeTrue();
        spec.IsSatisfiedBy(-3).Should().BeFalse();
    }

    [Fact]
    public void Not_ShouldNegate()
    {
        var spec = new PositiveSpec().Not();

        spec.IsSatisfiedBy(5).Should().BeFalse();
        spec.IsSatisfiedBy(-1).Should().BeTrue();
    }
}
