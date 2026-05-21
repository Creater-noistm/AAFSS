using AAFSS.App.Messaging;
using AAFSS.Core.Models;
using AAFSS.Core.Queries;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using Serilog;

namespace AAFSS.App.ViewModels;

/// <summary>
/// ViewModel for report preview — displays generated GJB reports.
/// </summary>
public partial class ReportPreviewViewModel : DocumentViewModel
{
    private readonly IMediator _mediator;
    private readonly ILogger _logger;

    [ObservableProperty]
    private string _reportContent = string.Empty;

    [ObservableProperty]
    private string _reportHtml = string.Empty;

    [ObservableProperty]
    private string _reportTitle = "报告预览";

    [ObservableProperty]
    private string _reportStatus = string.Empty;

    [ObservableProperty]
    private string _reportPath = string.Empty;

    [ObservableProperty]
    private bool _hasContent;

    [ObservableProperty]
    private string[] _availableTemplates = Array.Empty<string>();

    [ObservableProperty]
    private string? _selectedTemplate;

    [ObservableProperty]
    private Guid? _reportId;

    [ObservableProperty]
    private Guid? _spectrumId;

    public ReportPreviewViewModel(IMediator mediator, ILogger logger) : base("报告预览")
    {
        _mediator = mediator;
        _logger = logger;

        WeakReferenceMessenger.Default.Register<TreeNodeSelectedMessage>(this, async (r, m) =>
        {
            if (m.EntityId.HasValue && m.NodeType == "GeneratedReport")
                await LoadReportAsync(m.EntityId.Value);
        });
    }

    [RelayCommand]
    private async Task LoadReportAsync(Guid reportId)
    {
        try
        {
            ReportId = reportId;

            // Placeholder for loading existing report
            ReportContent = $"报告 #{reportId}";
            ReportStatus = "已生成";
            Title = $"报告 - {reportId}";
            HasContent = true;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to load report");
        }
    }

    [RelayCommand]
    private async Task GenerateReportAsync()
    {
        if (!SpectrumId.HasValue) return;
        try
        {
            var template = SelectedTemplate ?? "GJB";
            var outputDir = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(), "AAFSS", "Reports");

            System.IO.Directory.CreateDirectory(outputDir);

            var command = new AAFSS.Core.Commands.GenerateReportCommand(
                ProjectId: Guid.Empty,
                SpectrumIds: new List<Guid> { SpectrumId.Value },
                TemplateName: template,
                OutputDirectory: outputDir);

            var report = await _mediator.Send(command);

            ReportId = report.Id;
            ReportPath = report.FilePath;
            ReportStatus = report.Status.ToString();
            Title = $"报告 - {report.TemplateName}";
            HasContent = true;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to generate report");
        }
    }

    [RelayCommand]
    private async Task LoadTemplatesAsync()
    {
        try
        {
            AvailableTemplates = new[] { "GJB", "GJB-Full", "Custom" };
            SelectedTemplate = "GJB";
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to load report templates");
        }
    }

    [RelayCommand]
    private void ExportReport()
    {
        if (!string.IsNullOrEmpty(ReportPath) && System.IO.File.Exists(ReportPath))
        {
            System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{ReportPath}\"");
        }
    }
}
