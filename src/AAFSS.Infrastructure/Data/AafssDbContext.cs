using AAFSS.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace AAFSS.Infrastructure.Data;

/// <summary>
/// Entity Framework Core database context for AAFSS.
/// Manages all domain entity persistence to SQLite.
/// </summary>
public class AafssDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance with the specified options.
    /// </summary>
    public AafssDbContext(DbContextOptions<AafssDbContext> options) : base(options)
    {
    }

    // ─── DbSets ───────────────────────────────────────────

    /// <summary>Projects aggregate root.</summary>
    public DbSet<Project> Projects => Set<Project>();

    /// <summary>Mission profiles.</summary>
    public DbSet<MissionProfile> MissionProfiles => Set<MissionProfile>();

    /// <summary>Flight conditions.</summary>
    public DbSet<FlightCondition> FlightConditions => Set<FlightCondition>();

    /// <summary>Measurement points.</summary>
    public DbSet<MeasurementPoint> MeasurementPoints => Set<MeasurementPoint>();

    /// <summary>Data sources.</summary>
    public DbSet<DataSource> DataSources => Set<DataSource>();

    /// <summary>Time series data metadata.</summary>
    public DbSet<TimeSeriesData> TimeSeriesDatas => Set<TimeSeriesData>();

    /// <summary>Spectrum results.</summary>
    public DbSet<SpectrumResult> SpectrumResults => Set<SpectrumResult>();

    /// <summary>Rainflow counting results.</summary>
    public DbSet<RainflowResult> RainflowResults => Set<RainflowResult>();

    /// <summary>Statistical distribution models.</summary>
    public DbSet<StatisticalModel> StatisticalModels => Set<StatisticalModel>();

    /// <summary>Compiled spectra.</summary>
    public DbSet<CompiledSpectrum> CompiledSpectra => Set<CompiledSpectrum>();

    /// <summary>Validation reports.</summary>
    public DbSet<ValidationReport> ValidationReports => Set<ValidationReport>();

    /// <summary>Generated reports.</summary>
    public DbSet<GeneratedReport> GeneratedReports => Set<GeneratedReport>();

    /// <summary>Processing steps (audit trail).</summary>
    public DbSet<ProcessingStep> ProcessingSteps => Set<ProcessingStep>();

    /// <summary>
    /// Configures entity relationships, indexes, and constraints via Fluent API.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ApplyEntityConfigurations(modelBuilder);
    }

    /// <summary>
    /// Applies all entity type configurations.
    /// </summary>
    private static void ApplyEntityConfigurations(ModelBuilder modelBuilder)
    {
        // ─── Project ──────────────────────────────────────
        modelBuilder.Entity<Project>(entity =>
        {
            entity.ToTable("Projects");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(256);
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.Metadata).HasMaxLength(4000);
            entity.Property(e => e.FilePath).HasMaxLength(1024);

            entity.HasMany(e => e.Profiles)
                  .WithOne(p => p.Project)
                  .HasForeignKey(p => p.ProjectId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Spectra)
                  .WithOne(s => s.Project)
                  .HasForeignKey(s => s.ProjectId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Reports)
                  .WithOne(r => r.Project)
                  .HasForeignKey(r => r.ProjectId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ─── MissionProfile ───────────────────────────────
        modelBuilder.Entity<MissionProfile>(entity =>
        {
            entity.ToTable("MissionProfiles");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ProjectId);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(256);
            entity.Property(e => e.ParametersJson).HasMaxLength(4000);

            entity.HasMany(e => e.Conditions)
                  .WithOne(c => c.Profile)
                  .HasForeignKey(c => c.ProfileId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Points)
                  .WithOne(p => p.Profile)
                  .HasForeignKey(p => p.ProfileId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.DataSources)
                  .WithOne(d => d.Profile)
                  .HasForeignKey(d => d.ProfileId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ─── FlightCondition ──────────────────────────────
        modelBuilder.Entity<FlightCondition>(entity =>
        {
            entity.ToTable("FlightConditions");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ProfileId);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(256);
            entity.Property(e => e.PrimaryNoiseSource).HasMaxLength(256);
            entity.Property(e => e.AdditionalParametersJson).HasMaxLength(2000);
        });

        // ─── MeasurementPoint ─────────────────────────────
        modelBuilder.Entity<MeasurementPoint>(entity =>
        {
            entity.ToTable("MeasurementPoints");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ProfileId);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(256);
            entity.Property(e => e.Location).HasMaxLength(500);
            entity.Property(e => e.SensorSerialNumber).HasMaxLength(128);
        });

        // ─── DataSource ───────────────────────────────────
        modelBuilder.Entity<DataSource>(entity =>
        {
            entity.ToTable("DataSources");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ProfileId);
            entity.HasIndex(e => e.PointId);
            entity.Property(e => e.Format).IsRequired().HasMaxLength(32);
            entity.Property(e => e.FilePath).HasMaxLength(1024);
            entity.Property(e => e.Metadata).HasMaxLength(4000);
            entity.Property(e => e.ValidationResultJson).HasMaxLength(4000);

            entity.HasOne(e => e.MeasurementPoint)
                  .WithMany()
                  .HasForeignKey(e => e.PointId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.TimeSeriesData)
                  .WithOne(t => t.DataSource)
                  .HasForeignKey<TimeSeriesData>(t => t.DataSourceId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.ProcessingSteps)
                  .WithOne(p => p.DataSource)
                  .HasForeignKey(p => p.DataSourceId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.SpectrumResults)
                  .WithOne(s => s.DataSource)
                  .HasForeignKey(s => s.DataSourceId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.RainflowResults)
                  .WithOne(r => r.DataSource)
                  .HasForeignKey(r => r.DataSourceId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ─── TimeSeriesData ───────────────────────────────
        modelBuilder.Entity<TimeSeriesData>(entity =>
        {
            entity.ToTable("TimeSeriesDatas");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.DataSourceId).IsUnique();
            entity.Property(e => e.Hdf5Path).IsRequired().HasMaxLength(512);
            entity.Property(e => e.ChannelNamesJson).HasMaxLength(4000);
            entity.Property(e => e.ChannelUnitsJson).HasMaxLength(2000);
            entity.Property(e => e.Quantity).HasMaxLength(128);
        });

        // ─── SpectrumResult ───────────────────────────────
        modelBuilder.Entity<SpectrumResult>(entity =>
        {
            entity.ToTable("SpectrumResults");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.DataSourceId);
            entity.Property(e => e.WindowType).HasMaxLength(64);
        });

        // ─── RainflowResult ───────────────────────────────
        modelBuilder.Entity<RainflowResult>(entity =>
        {
            entity.ToTable("RainflowResults");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.DataSourceId);

            entity.HasMany(e => e.StatisticalModels)
                  .WithOne(s => s.RainflowResult)
                  .HasForeignKey(s => s.RainflowResultId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ─── StatisticalModel ─────────────────────────────
        modelBuilder.Entity<StatisticalModel>(entity =>
        {
            entity.ToTable("StatisticalModels");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.RainflowResultId);
            entity.Property(e => e.ParametersJson).HasMaxLength(4000);
            entity.Property(e => e.FitStatus).HasMaxLength(256);
        });

        // ─── CompiledSpectrum ─────────────────────────────
        modelBuilder.Entity<CompiledSpectrum>(entity =>
        {
            entity.ToTable("CompiledSpectra");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ProjectId);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(256);
            entity.Property(e => e.SourceSpectrumIdsJson).HasMaxLength(4000);

            entity.HasOne(e => e.ValidationReport)
                  .WithOne(v => v.Spectrum)
                  .HasForeignKey<ValidationReport>(v => v.SpectrumId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ─── ValidationReport ─────────────────────────────
        modelBuilder.Entity<ValidationReport>(entity =>
        {
            entity.ToTable("ValidationReports");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.SpectrumId).IsUnique();
            entity.Property(e => e.WarningsJson).HasMaxLength(4000);
        });

        // ─── GeneratedReport ──────────────────────────────
        modelBuilder.Entity<GeneratedReport>(entity =>
        {
            entity.ToTable("GeneratedReports");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ProjectId);
            entity.Property(e => e.TemplateName).IsRequired().HasMaxLength(256);
            entity.Property(e => e.FilePath).HasMaxLength(1024);
            entity.Property(e => e.IncludedSpectrumIdsJson).HasMaxLength(4000);
            entity.Property(e => e.ErrorMessage).HasMaxLength(2000);
        });

        // ─── ProcessingStep ───────────────────────────────
        modelBuilder.Entity<ProcessingStep>(entity =>
        {
            entity.ToTable("ProcessingSteps");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.DataSourceId);
            entity.HasIndex(e => new { e.DataSourceId, e.StepOrder }).IsUnique();
            entity.Property(e => e.OperationType).IsRequired().HasMaxLength(128);
            entity.Property(e => e.OperationParams).HasMaxLength(4000);
            entity.Property(e => e.InputRef).HasMaxLength(1024);
            entity.Property(e => e.OutputRef).HasMaxLength(1024);
            entity.Property(e => e.ErrorMessage).HasMaxLength(4000);
        });
    }

    /// <summary>
    /// Ensures the database is created and migrations are applied.
    /// </summary>
    public void EnsureDatabaseCreated()
    {
        Database.EnsureCreated();
    }
}
