using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AAFSS.Core.Extensions;

/// <summary>
/// Extension methods for MediatR pipeline behaviors and cross-cutting concerns.
/// Provides logging, validation, and performance tracking decorators for
/// commands and queries.
/// </summary>
public static class MediatRExtensions
{
    /// <summary>
    /// Registers MediatR with all required pipeline behaviors for AAFSS.
    /// Includes logging behavior for commands/queries and performance monitoring.
    /// </summary>
    public static IServiceCollection AddAafssMediatR(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(Commands.ImportDataCommand).Assembly);
            cfg.RegisterServicesFromAssembly(typeof(Queries.GetSpectrumDataQuery).Assembly);
            cfg.RegisterServicesFromAssembly(typeof(Events.DataImportedEvent).Assembly);

            // Pipeline behaviors (executed in registration order)
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));
        });

        return services;
    }
}

/// <summary>
/// MediatR pipeline behavior that logs all incoming requests and outgoing responses
/// at Debug level for diagnostics.
/// </summary>
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly Microsoft.Extensions.Logging.ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(Microsoft.Extensions.Logging.ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        _logger.LogDebug("MediatR request: {RequestName} {@Request}", requestName, request);

        var response = await next();

        _logger.LogDebug("MediatR response: {RequestName} {@Response}", requestName, response);
        return response;
    }
}

/// <summary>
/// MediatR pipeline behavior that tracks and warns on slow-running requests
/// (exceeding 500ms). Logs duration at Information level for all requests.
/// </summary>
public class PerformanceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly Microsoft.Extensions.Logging.ILogger<PerformanceBehavior<TRequest, TResponse>> _logger;
    private static readonly TimeSpan SlowThreshold = TimeSpan.FromMilliseconds(500);

    public PerformanceBehavior(Microsoft.Extensions.Logging.ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var startTime = System.Diagnostics.Stopwatch.GetTimestamp();
        var response = await next();
        var elapsed = System.Diagnostics.Stopwatch.GetElapsedTime(startTime);

        var requestName = typeof(TRequest).Name;

        if (elapsed > SlowThreshold)
        {
            _logger.LogWarning("Slow MediatR request: {RequestName} took {ElapsedMs:F0}ms (threshold: {ThresholdMs}ms)",
                requestName, elapsed.TotalMilliseconds, SlowThreshold.TotalMilliseconds);
        }
        else
        {
            _logger.LogInformation("MediatR request: {RequestName} completed in {ElapsedMs:F0}ms",
                requestName, elapsed.TotalMilliseconds);
        }

        return response;
    }
}
