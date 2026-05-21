using AAFSS.Core.Commands;
using AAFSS.Core.Events;
using AAFSS.Core.Queries;
using AAFSS.Core.Workflows;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using WorkflowCore.Interface;

namespace AAFSS.Core.Extensions;

/// <summary>
/// Extension methods for registering AAFSS.Core services, MediatR handlers,
/// and WorkflowCore workflows into the DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all AAFSS.Core services including MediatR handlers from this assembly
    /// and WorkflowCore workflow definitions.
    /// </summary>
    public static IServiceCollection AddAafssCore(this IServiceCollection services)
    {
        // MediatR - Commands, Queries, and Events from Core assembly
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(ImportDataCommand).Assembly);
            cfg.RegisterServicesFromAssembly(typeof(GetSpectrumDataQuery).Assembly);
            cfg.RegisterServicesFromAssembly(typeof(DataImportedEvent).Assembly);
        });

        // WorkflowCore workflow definitions
        services.AddTransient<SpectrumCompilationWorkflow>();
        services.AddTransient<ValidationWorkflow>();

        // Workflow step bodies
        RegisterWorkflowSteps(services);

        return services;
    }

    /// <summary>
    /// Registers all workflow step body classes for WorkflowCore DI resolution.
    /// </summary>
    private static void RegisterWorkflowSteps(IServiceCollection services)
    {
        // SpectrumCompilationWorkflow steps
        services.AddTransient<ImportDataStep>();
        services.AddTransient<PreprocessSignalStep>();
        services.AddTransient<ComputeSpectrumStep>();
        services.AddTransient<ComputeRainflowStep>();
        services.AddTransient<FitDistributionStep>();
        services.AddTransient<CompileEnvelopeStep>();
        services.AddTransient<ValidateDamageStep>();

        // ValidationWorkflow steps
        services.AddTransient<RunValidationStep>();
        services.AddTransient<GenerateValidationReportStep>();
    }
}
