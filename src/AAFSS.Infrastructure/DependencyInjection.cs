using AAFSS.Core.Services;
using AAFSS.Infrastructure.Configuration;
using AAFSS.Infrastructure.Data;
using AAFSS.Infrastructure.Data.Repositories;
using AAFSS.Infrastructure.Hdf5;
using AAFSS.Infrastructure.Import;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AAFSS.Infrastructure;

/// <summary>
/// Extension methods for registering Infrastructure layer services in the DI container.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds all infrastructure layer services, repositories, and factories to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // Configuration
        services.AddSingleton<AppConfiguration>();

        // Database
        services.AddDbContext<AafssDbContext>((sp, options) =>
        {
            var config = sp.GetRequiredService<AppConfiguration>();
            options.UseSqlite(config.ConnectionString);
        });

        // Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Repositories
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<IMissionProfileRepository, MissionProfileRepository>();
        services.AddScoped<IDataSourceRepository, DataSourceRepository>();
        services.AddScoped<ISpectrumRepository, SpectrumRepository>();
        services.AddScoped<IProcessingStepRepository, ProcessingStepRepository>();

        // HDF5
        services.AddSingleton<Hdf5DataStore>();
        services.AddTransient<Hdf5TimeSeriesReader>();
        services.AddTransient<Hdf5TimeSeriesWriter>();

        // Data Import
        services.AddTransient<CsvDataImporter>();
        services.AddTransient<ExcelDataImporter>();
        services.AddTransient<DataImportFactory>();
        services.AddTransient<DataValidator>();

        // Project File
        services.AddTransient<ProjectManagement.AafssProjectFile>();
        services.AddSingleton<ProjectManagement.AafssAutoSaveService>();
        services.AddSingleton<ProjectManagement.RecentProjectsService>();

        // Python Bridges (singletons for performance)
        services.AddSingleton<Python.PythonScriptExecutor>();
        services.AddSingleton<Python.SignalProcessingBridge>();
        services.AddSingleton<Python.FrequencyAnalysisBridge>();
        services.AddSingleton<Python.TimeDomainBridge>();
        services.AddSingleton<Python.StatisticalBridge>();
        services.AddSingleton<Python.FatigueBridge>();
        services.AddSingleton<Python.NumPyDataConverter>();

        // Plugin Host
        services.AddSingleton<Plugins.PluginHost>();
        services.AddSingleton<Plugins.PluginDiscoveryService>();

        // Export
        services.AddTransient<Export.ReportEngine>();
        services.AddTransient<Export.GjbReportBuilder>();
        services.AddTransient<Export.ChartToImageExporter>();

        // Core Service Implementations (in Infrastructure)
        services.AddTransient<IProjectManagementService, Services.ProjectManagementService>();
        services.AddTransient<IDataImportService, Services.DataImportService>();
        services.AddTransient<ISignalProcessingService, Services.SignalProcessingService>();
        services.AddTransient<IFrequencyAnalysisService, Services.FrequencyAnalysisService>();
        services.AddTransient<ITimeDomainAnalysisService, Services.TimeDomainAnalysisService>();
        services.AddTransient<IStatisticalModelingService, Services.StatisticalModelingService>();
        services.AddTransient<ISpectrumCompilationService, Services.SpectrumCompilationService>();
        services.AddTransient<IDamageCalculationService, Services.DamageCalculationService>();
        services.AddTransient<IValidationService, Services.ValidationService>();
        services.AddTransient<IReportGenerationService, Services.ReportGenerationService>();

        return services;
    }
}
