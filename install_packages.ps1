# Install all AAFSS NuGet packages using standalone nuget.exe
$nuget = "C:\Users\noist\AppData\Local\Temp\nuget.exe"
$output = "C:\Users\noist\.nuget\packages"
$source = "https://api.nuget.org/v3/index.json"

$packages = @(
    @{Id="Microsoft.Extensions.DependencyInjection"; Version="8.0.0"},
    @{Id="Microsoft.Extensions.Configuration.Json"; Version="8.0.0"},
    @{Id="CommunityToolkit.Mvvm"; Version="8.2.2"},
    @{Id="AvalonDock"; Version="4.72.0"},
    @{Id="Fluent.Ribbon"; Version="10.0.0"},
    @{Id="ScottPlot.WPF"; Version="5.0.39"},
    @{Id="HelixToolkit.Wpf"; Version="2.25.0"},
    @{Id="MediatR"; Version="12.2.0"},
    @{Id="MediatR.Extensions.Microsoft.DependencyInjection"; Version="12.2.0"},
    @{Id="Serilog"; Version="4.0.1"},
    @{Id="Serilog.Sinks.File"; Version="5.0.0"},
    @{Id="Serilog.Extensions.Hosting"; Version="8.0.0"},
    @{Id="System.Composition"; Version="8.0.0"},
    @{Id="WorkflowCore"; Version="3.10.0"},
    @{Id="WorkflowCore.Persistence.Sqlite"; Version="3.10.0"},
    @{Id="Microsoft.EntityFrameworkCore.Sqlite"; Version="8.0.0"},
    @{Id="Microsoft.EntityFrameworkCore.Design"; Version="8.0.0"},
    @{Id="PureHDF"; Version="2.1.0"},
    @{Id="pythonnet"; Version="3.0.3"},
    @{Id="CsvHelper"; Version="33.0.0"},
    @{Id="ExcelDataReader"; Version="3.7.0"},
    @{Id="DocX"; Version="3.0.0"},
    @{Id="DocumentFormat.OpenXml"; Version="3.0.0"},
    @{Id="ScottPlot"; Version="5.0.47"},
    @{Id="xunit"; Version="2.8.0"},
    @{Id="xunit.runner.visualstudio"; Version="2.8.0"},
    @{Id="Moq"; Version="4.20.0"},
    @{Id="FluentAssertions"; Version="6.12.0"},
    @{Id="Microsoft.NET.Test.Sdk"; Version="17.9.0"}
)

foreach ($pkg in $packages) {
    $pkgPath = Join-Path $output $pkg.Id.ToLower() $pkg.Version
    if (Test-Path $pkgPath) {
        Write-Host "SKIP (exists): $($pkg.Id) $($pkg.Version)"
        continue
    }
    Write-Host "INSTALL: $($pkg.Id) $($pkg.Version)"
    $args = @(
        "install", $pkg.Id,
        "-Version", $pkg.Version,
        "-OutputDirectory", $output,
        "-Source", $source,
        "-NoHttpCache",
        "-NonInteractive",
        "-Verbosity", "normal"
    )
    & $nuget $args 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERROR installing $($pkg.Id)" -ForegroundColor Red
    }
}

Write-Host "`nDONE installing packages"
