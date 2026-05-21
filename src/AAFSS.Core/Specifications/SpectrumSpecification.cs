using System.Linq.Expressions;
using AAFSS.Core.Models;

namespace AAFSS.Core.Specifications;

/// <summary>
/// Specification pattern for filtering and querying compiled spectra.
/// Implements composite filter logic for use in repositories and UI queries.
/// </summary>
public abstract class SpectrumSpecification
{
    /// <summary>
    /// The compiled expression predicate for this specification.
    /// </summary>
    public abstract Expression<Func<CompiledSpectrum, bool>> ToExpression();

    /// <summary>
    /// Evaluates whether the given spectrum satisfies this specification.
    /// </summary>
    public bool IsSatisfiedBy(CompiledSpectrum spectrum)
    {
        return ToExpression().Compile().Invoke(spectrum);
    }

    // === Compound operators ===

    public SpectrumSpecification And(SpectrumSpecification other)
    {
        return new AndSpecification(this, other);
    }

    public SpectrumSpecification Or(SpectrumSpecification other)
    {
        return new OrSpecification(this, other);
    }

    public SpectrumSpecification Not()
    {
        return new NotSpecification(this);
    }

    // === Concrete specifications ===

    /// <summary>
    /// All spectra belonging to a specific project.
    /// </summary>
    public static SpectrumSpecification ByProject(Guid projectId)
        => new ProjectSpecification(projectId);

    /// <summary>
    /// Spectra of a specific category (base, severe, envelope, etc.).
    /// </summary>
    public static SpectrumSpecification ByCategory(SpectrumCategory category)
        => new CategorySpecification(category);

    /// <summary>
    /// Spectra compiled using a specific method.
    /// </summary>
    public static SpectrumSpecification ByMethod(CompilationMethod method)
        => new MethodSpecification(method);

    /// <summary>
    /// Spectra with OASPL above a given threshold.
    /// </summary>
    public static SpectrumSpecification AboveOaspl(double thresholdDb)
        => new OasplAboveSpecification(thresholdDb);

    /// <summary>
    /// Spectra validated with a specific level.
    /// </summary>
    public static SpectrumSpecification ByValidationLevel(ValidationLevel level)
        => new ValidationLevelSpecification(level);

    /// <summary>
    /// Spectra created within a date range.
    /// </summary>
    public static SpectrumSpecification CreatedBetween(DateTime from, DateTime to)
        => new DateRangeSpecification(from, to);
}

/// <summary>
/// AND combination of two specifications.
/// </summary>
internal class AndSpecification : SpectrumSpecification
{
    private readonly SpectrumSpecification _left;
    private readonly SpectrumSpecification _right;

    public AndSpecification(SpectrumSpecification left, SpectrumSpecification right)
    {
        _left = left;
        _right = right;
    }

    public override Expression<Func<CompiledSpectrum, bool>> ToExpression()
    {
        var leftExpr = _left.ToExpression();
        var rightExpr = _right.ToExpression();
        var parameter = Expression.Parameter(typeof(CompiledSpectrum));

        var invokedLeft = Expression.Invoke(leftExpr, parameter);
        var invokedRight = Expression.Invoke(rightExpr, parameter);
        var andExpression = Expression.AndAlso(invokedLeft, invokedRight);

        return Expression.Lambda<Func<CompiledSpectrum, bool>>(andExpression, parameter);
    }
}

/// <summary>
/// OR combination of two specifications.
/// </summary>
internal class OrSpecification : SpectrumSpecification
{
    private readonly SpectrumSpecification _left;
    private readonly SpectrumSpecification _right;

    public OrSpecification(SpectrumSpecification left, SpectrumSpecification right)
    {
        _left = left;
        _right = right;
    }

    public override Expression<Func<CompiledSpectrum, bool>> ToExpression()
    {
        var leftExpr = _left.ToExpression();
        var rightExpr = _right.ToExpression();
        var parameter = Expression.Parameter(typeof(CompiledSpectrum));

        var invokedLeft = Expression.Invoke(leftExpr, parameter);
        var invokedRight = Expression.Invoke(rightExpr, parameter);
        var orExpression = Expression.OrElse(invokedLeft, invokedRight);

        return Expression.Lambda<Func<CompiledSpectrum, bool>>(orExpression, parameter);
    }
}

/// <summary>
/// NOT negation of a specification.
/// </summary>
internal class NotSpecification : SpectrumSpecification
{
    private readonly SpectrumSpecification _inner;

    public NotSpecification(SpectrumSpecification inner)
    {
        _inner = inner;
    }

    public override Expression<Func<CompiledSpectrum, bool>> ToExpression()
    {
        var innerExpr = _inner.ToExpression();
        var parameter = Expression.Parameter(typeof(CompiledSpectrum));

        var invoked = Expression.Invoke(innerExpr, parameter);
        var notExpression = Expression.Not(invoked);

        return Expression.Lambda<Func<CompiledSpectrum, bool>>(notExpression, parameter);
    }
}

// === Concrete specification implementations ===

internal class ProjectSpecification : SpectrumSpecification
{
    private readonly Guid _projectId;

    public ProjectSpecification(Guid projectId) => _projectId = projectId;

    public override Expression<Func<CompiledSpectrum, bool>> ToExpression()
        => s => s.ProjectId == _projectId;
}

internal class CategorySpecification : SpectrumSpecification
{
    private readonly SpectrumCategory _category;

    public CategorySpecification(SpectrumCategory category) => _category = category;

    public override Expression<Func<CompiledSpectrum, bool>> ToExpression()
        => s => s.Category == _category;
}

internal class MethodSpecification : SpectrumSpecification
{
    private readonly CompilationMethod _method;

    public MethodSpecification(CompilationMethod method) => _method = method;

    public override Expression<Func<CompiledSpectrum, bool>> ToExpression()
        => s => s.Method == _method;
}

internal class OasplAboveSpecification : SpectrumSpecification
{
    private readonly double _threshold;

    public OasplAboveSpecification(double threshold) => _threshold = threshold;

    public override Expression<Func<CompiledSpectrum, bool>> ToExpression()
        => s => s.Oaspl >= _threshold;
}

internal class ValidationLevelSpecification : SpectrumSpecification
{
    private readonly ValidationLevel _level;

    public ValidationLevelSpecification(ValidationLevel level) => _level = level;

    public override Expression<Func<CompiledSpectrum, bool>> ToExpression()
        => s => s.ValidationLevel == _level;
}

internal class DateRangeSpecification : SpectrumSpecification
{
    private readonly DateTime _from;
    private readonly DateTime _to;

    public DateRangeSpecification(DateTime from, DateTime to)
    {
        _from = from;
        _to = to;
    }

    public override Expression<Func<CompiledSpectrum, bool>> ToExpression()
        => s => s.CompiledAt >= _from && s.CompiledAt <= _to;
}
