namespace AAFSS.Core.Specifications;

/// <summary>
/// Base specification interface following the Specification pattern.
/// </summary>
public interface ISpecification<T>
{
    bool IsSatisfiedBy(T candidate);
    ISpecification<T> And(ISpecification<T> other);
    ISpecification<T> Or(ISpecification<T> other);
    ISpecification<T> Not();
}

/// <summary>
/// Abstract base class for composite specifications.
/// </summary>
public abstract class Specification<T> : ISpecification<T>
{
    public abstract bool IsSatisfiedBy(T candidate);

    public ISpecification<T> And(ISpecification<T> other)
        => new AndSpecification<T>(this, other);

    public ISpecification<T> Or(ISpecification<T> other)
        => new OrSpecification<T>(this, other);

    public ISpecification<T> Not()
        => new NotSpecification<T>(this);
}

internal class AndSpecification<T> : Specification<T>
{
    private readonly ISpecification<T> _left;
    private readonly ISpecification<T> _right;

    public AndSpecification(ISpecification<T> left, ISpecification<T> right)
    {
        _left = left;
        _right = right;
    }

    public override bool IsSatisfiedBy(T candidate)
        => _left.IsSatisfiedBy(candidate) && _right.IsSatisfiedBy(candidate);
}

internal class OrSpecification<T> : Specification<T>
{
    private readonly ISpecification<T> _left;
    private readonly ISpecification<T> _right;

    public OrSpecification(ISpecification<T> left, ISpecification<T> right)
    {
        _left = left;
        _right = right;
    }

    public override bool IsSatisfiedBy(T candidate)
        => _left.IsSatisfiedBy(candidate) || _right.IsSatisfiedBy(candidate);
}

internal class NotSpecification<T> : Specification<T>
{
    private readonly ISpecification<T> _inner;

    public NotSpecification(ISpecification<T> inner)
    {
        _inner = inner;
    }

    public override bool IsSatisfiedBy(T candidate)
        => !_inner.IsSatisfiedBy(candidate);
}
