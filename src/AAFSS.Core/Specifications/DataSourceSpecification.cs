using System.Linq.Expressions;
using AAFSS.Core.Models;

namespace AAFSS.Core.Specifications;

/// <summary>
/// Specification pattern for filtering and querying data sources.
/// Composable filter logic for repository queries and UI search/filter.
/// </summary>
public abstract class DataSourceSpecification
{
    /// <summary>
    /// The compiled expression predicate for this specification.
    /// </summary>
    public abstract Expression<Func<DataSource, bool>> ToExpression();

    public bool IsSatisfiedBy(DataSource dataSource)
    {
        return ToExpression().Compile().Invoke(dataSource);
    }

    // === Compound operators ===

    public DataSourceSpecification And(DataSourceSpecification other)
        => new AndDsSpecification(this, other);

    public DataSourceSpecification Or(DataSourceSpecification other)
        => new OrDsSpecification(this, other);

    public DataSourceSpecification Not()
        => new NotDsSpecification(this);

    // === Concrete specifications ===

    /// <summary>
    /// Data sources belonging to a specific project.
    /// </summary>
    public static DataSourceSpecification ByProject(Guid projectId)
        => new ByProjectDsSpec(projectId);

    /// <summary>
    /// Data sources of a specific type.
    /// </summary>
    public static DataSourceSpecification ByType(DataSourceType type)
        => new ByTypeDsSpec(type);

    /// <summary>
    /// Data sources imported after a given date.
    /// </summary>
    public static DataSourceSpecification ImportedAfter(DateTime date)
        => new ImportedAfterDsSpec(date);

    /// <summary>
    /// Data sources with a minimum number of data points.
    /// </summary>
    public static DataSourceSpecification MinSamples(long minPoints)
        => new MinSamplesDsSpec(minPoints);

    /// <summary>
    /// Data sources from a specific sensor type.
    /// </summary>
    public static DataSourceSpecification BySensorType(SensorType sensorType)
        => new BySensorTypeDsSpec(sensorType);

    /// <summary>
    /// Data sources matching a file name pattern.
    /// </summary>
    public static DataSourceSpecification FileNameContains(string pattern)
        => new FileNameContainsDsSpec(pattern);
}

// === Compound specifications ===

internal class AndDsSpecification : DataSourceSpecification
{
    private readonly DataSourceSpecification _left;
    private readonly DataSourceSpecification _right;

    public AndDsSpecification(DataSourceSpecification left, DataSourceSpecification right)
    {
        _left = left;
        _right = right;
    }

    public override Expression<Func<DataSource, bool>> ToExpression()
    {
        var leftExpr = _left.ToExpression();
        var rightExpr = _right.ToExpression();
        var param = Expression.Parameter(typeof(DataSource));
        var and = Expression.AndAlso(Expression.Invoke(leftExpr, param), Expression.Invoke(rightExpr, param));
        return Expression.Lambda<Func<DataSource, bool>>(and, param);
    }
}

internal class OrDsSpecification : DataSourceSpecification
{
    private readonly DataSourceSpecification _left;
    private readonly DataSourceSpecification _right;

    public OrDsSpecification(DataSourceSpecification left, DataSourceSpecification right)
    {
        _left = left;
        _right = right;
    }

    public override Expression<Func<DataSource, bool>> ToExpression()
    {
        var leftExpr = _left.ToExpression();
        var rightExpr = _right.ToExpression();
        var param = Expression.Parameter(typeof(DataSource));
        var or = Expression.OrElse(Expression.Invoke(leftExpr, param), Expression.Invoke(rightExpr, param));
        return Expression.Lambda<Func<DataSource, bool>>(or, param);
    }
}

internal class NotDsSpecification : DataSourceSpecification
{
    private readonly DataSourceSpecification _inner;

    public NotDsSpecification(DataSourceSpecification inner) => _inner = inner;

    public override Expression<Func<DataSource, bool>> ToExpression()
    {
        var innerExpr = _inner.ToExpression();
        var param = Expression.Parameter(typeof(DataSource));
        var not = Expression.Not(Expression.Invoke(innerExpr, param));
        return Expression.Lambda<Func<DataSource, bool>>(not, param);
    }
}

// === Concrete specifications ===

internal class ByProjectDsSpec : DataSourceSpecification
{
    private readonly Guid _projectId;
    public ByProjectDsSpec(Guid projectId) => _projectId = projectId;
    public override Expression<Func<DataSource, bool>> ToExpression()
        => ds => ds.ProjectId == _projectId;
}

internal class ByTypeDsSpec : DataSourceSpecification
{
    private readonly DataSourceType _type;
    public ByTypeDsSpec(DataSourceType type) => _type = type;
    public override Expression<Func<DataSource, bool>> ToExpression()
        => ds => ds.Type == _type;
}

internal class ImportedAfterDsSpec : DataSourceSpecification
{
    private readonly DateTime _date;
    public ImportedAfterDsSpec(DateTime date) => _date = date;
    public override Expression<Func<DataSource, bool>> ToExpression()
        => ds => ds.ImportedAt >= _date;
}

internal class MinSamplesDsSpec : DataSourceSpecification
{
    private readonly long _minPoints;
    public MinSamplesDsSpec(long minPoints) => _minPoints = minPoints;
    public override Expression<Func<DataSource, bool>> ToExpression()
        => ds => ds.TotalDataPoints >= _minPoints;
}

internal class BySensorTypeDsSpec : DataSourceSpecification
{
    private readonly SensorType _sensorType;
    public BySensorTypeDsSpec(SensorType sensorType) => _sensorType = sensorType;
    public override Expression<Func<DataSource, bool>> ToExpression()
        => ds => ds.SensorType == _sensorType;
}

internal class FileNameContainsDsSpec : DataSourceSpecification
{
    private readonly string _pattern;
    public FileNameContainsDsSpec(string pattern) => _pattern = pattern;
    public override Expression<Func<DataSource, bool>> ToExpression()
        => ds => ds.FilePath.Contains(_pattern, StringComparison.OrdinalIgnoreCase);
}
