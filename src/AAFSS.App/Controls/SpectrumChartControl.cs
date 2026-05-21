using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ScottPlot;
using ScottPlot.WPF;

namespace AAFSS.App.Controls;

/// <summary>
/// A reusable WPF custom control wrapping ScottPlot for acoustic spectrum visualization.
/// Supports multiple series with OASPL thresholds, confidence interval bands,
/// log/linear toggle, cursor readout, and export.
/// </summary>
[TemplatePart(Name = PartPlot, Type = typeof(WpfPlot))]
[TemplatePart(Name = PartCursorLabel, Type = typeof(TextBlock))]
[TemplatePart(Name = PartChartTitle, Type = typeof(TextBlock))]
[TemplatePart(Name = PartStatusBar, Type = typeof(Border))]
public class SpectrumChartControl : Control
{
    private const string PartPlot = "PART_Plot";
    private const string PartCursorLabel = "PART_CursorLabel";
    private const string PartChartTitle = "PART_ChartTitle";
    private const string PartStatusBar = "PART_StatusBar";

    #region Dependency Properties

    public static readonly DependencyProperty ChartTitleProperty =
        DependencyProperty.Register(nameof(ChartTitle), typeof(string), typeof(SpectrumChartControl),
            new PropertyMetadata("频谱图", OnVisualPropertyChanged));

    public static readonly DependencyProperty ShowGridProperty =
        DependencyProperty.Register(nameof(ShowGrid), typeof(bool), typeof(SpectrumChartControl),
            new PropertyMetadata(true, OnVisualPropertyChanged));

    public static readonly DependencyProperty ShowLegendProperty =
        DependencyProperty.Register(nameof(ShowLegend), typeof(bool), typeof(SpectrumChartControl),
            new PropertyMetadata(true, OnVisualPropertyChanged));

    public static readonly DependencyProperty IsLogXProperty =
        DependencyProperty.Register(nameof(IsLogX), typeof(bool), typeof(SpectrumChartControl),
            new PropertyMetadata(true, OnVisualPropertyChanged));

    public static readonly DependencyProperty XLabelProperty =
        DependencyProperty.Register(nameof(XLabel), typeof(string), typeof(SpectrumChartControl),
            new PropertyMetadata("频率 (Hz)", OnVisualPropertyChanged));

    public static readonly DependencyProperty YLabelProperty =
        DependencyProperty.Register(nameof(YLabel), typeof(string), typeof(SpectrumChartControl),
            new PropertyMetadata("声压级 (dB)", OnVisualPropertyChanged));

    public static readonly DependencyProperty OasplThresholdProperty =
        DependencyProperty.Register(nameof(OasplThreshold), typeof(double), typeof(SpectrumChartControl),
            new PropertyMetadata(140.0, OnVisualPropertyChanged));

    public static readonly DependencyProperty ShowOasplLineProperty =
        DependencyProperty.Register(nameof(ShowOasplLine), typeof(bool), typeof(SpectrumChartControl),
            new PropertyMetadata(true, OnVisualPropertyChanged));

    public static readonly DependencyProperty SeriesItemsProperty =
        DependencyProperty.Register(nameof(SeriesItems), typeof(ObservableCollection<SpectrumSeriesItem>),
            typeof(SpectrumChartControl),
            new PropertyMetadata(null, OnSeriesItemsChanged));

    public static readonly DependencyProperty IsLoadingProperty =
        DependencyProperty.Register(nameof(IsLoading), typeof(bool), typeof(SpectrumChartControl),
            new PropertyMetadata(false, OnLoadingChanged));

    public static readonly DependencyProperty CursorFrequencyProperty =
        DependencyProperty.Register(nameof(CursorFrequency), typeof(double), typeof(SpectrumChartControl),
            new PropertyMetadata(0.0));

    public static readonly DependencyProperty CursorAmplitudeProperty =
        DependencyProperty.Register(nameof(CursorAmplitude), typeof(double), typeof(SpectrumChartControl),
            new PropertyMetadata(0.0));

    #endregion

    #region CLR Properties

    public string ChartTitle
    {
        get => (string)GetValue(ChartTitleProperty);
        set => SetValue(ChartTitleProperty, value);
    }

    public bool ShowGrid
    {
        get => (bool)GetValue(ShowGridProperty);
        set => SetValue(ShowGridProperty, value);
    }

    public bool ShowLegend
    {
        get => (bool)GetValue(ShowLegendProperty);
        set => SetValue(ShowLegendProperty, value);
    }

    public bool IsLogX
    {
        get => (bool)GetValue(IsLogXProperty);
        set => SetValue(IsLogXProperty, value);
    }

    public string XLabel
    {
        get => (string)GetValue(XLabelProperty);
        set => SetValue(XLabelProperty, value);
    }

    public string YLabel
    {
        get => (string)GetValue(YLabelProperty);
        set => SetValue(YLabelProperty, value);
    }

    public double OasplThreshold
    {
        get => (double)GetValue(OasplThresholdProperty);
        set => SetValue(OasplThresholdProperty, value);
    }

    public bool ShowOasplLine
    {
        get => (bool)GetValue(ShowOasplLineProperty);
        set => SetValue(ShowOasplLineProperty, value);
    }

    public ObservableCollection<SpectrumSeriesItem> SeriesItems
    {
        get => (ObservableCollection<SpectrumSeriesItem>)GetValue(SeriesItemsProperty);
        set => SetValue(SeriesItemsProperty, value);
    }

    public bool IsLoading
    {
        get => (bool)GetValue(IsLoadingProperty);
        set => SetValue(IsLoadingProperty, value);
    }

    public double CursorFrequency
    {
        get => (double)GetValue(CursorFrequencyProperty);
        set => SetValue(CursorFrequencyProperty, value);
    }

    public double CursorAmplitude
    {
        get => (double)GetValue(CursorAmplitudeProperty);
        set => SetValue(CursorAmplitudeProperty, value);
    }

    #endregion

    #region Events

    public static readonly RoutedEvent CursorPositionChangedEvent =
        EventManager.RegisterRoutedEvent(nameof(CursorPositionChanged), RoutingStrategy.Bubble,
            typeof(RoutedEventHandler), typeof(SpectrumChartControl));

    public event RoutedEventHandler CursorPositionChanged
    {
        add => AddHandler(CursorPositionChangedEvent, value);
        remove => RemoveHandler(CursorPositionChangedEvent, value);
    }

    public static readonly RoutedEvent ChartRefreshedEvent =
        EventManager.RegisterRoutedEvent(nameof(ChartRefreshed), RoutingStrategy.Bubble,
            typeof(RoutedEventHandler), typeof(SpectrumChartControl));

    public event RoutedEventHandler ChartRefreshed
    {
        add => AddHandler(ChartRefreshedEvent, value);
        remove => RemoveHandler(ChartRefreshedEvent, value);
    }

    #endregion

    #region Internal State

    private WpfPlot? _plot;
    private TextBlock? _cursorLabel;
    private TextBlock? _chartTitleBlock;
    private Border? _statusBar;

    // Color palette for series
    private static readonly ScottPlot.Color[] SeriesColors =
    [
        Color.FromRgb(66, 165, 245),   // Blue
        Color.FromRgb(239, 83, 80),    // Red
        Color.FromRgb(102, 187, 106),  // Green
        Color.FromRgb(255, 183, 77),   // Orange
        Color.FromRgb(171, 71, 188),   // Purple
        Color.FromRgb(0, 188, 212),    // Cyan
        Color.FromRgb(255, 112, 67),   // Deep Orange
        Color.FromRgb(92, 107, 192),   // Indigo
    ];

    #endregion

    static SpectrumChartControl()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(SpectrumChartControl),
            new FrameworkPropertyMetadata(typeof(SpectrumChartControl)));
    }

    public SpectrumChartControl()
    {
        var series = new ObservableCollection<SpectrumSeriesItem>();
        series.CollectionChanged += OnSeriesCollectionChanged;
        SetCurrentValue(SeriesItemsProperty, series);

        // Register command bindings for toolbar buttons
        CommandBindings.Add(new CommandBinding(SpectrumChartCommands.ToggleGrid,
            (s, e) => { ShowGrid = !ShowGrid; }));
        CommandBindings.Add(new CommandBinding(SpectrumChartCommands.ToggleLegend,
            (s, e) => { ShowLegend = !ShowLegend; }));
        CommandBindings.Add(new CommandBinding(SpectrumChartCommands.ToggleLogScale,
            (s, e) => { IsLogX = !IsLogX; }));
        CommandBindings.Add(new CommandBinding(SpectrumChartCommands.ExportPng,
            (s, e) => OnExportPngCommand()));
    }

    private void OnExportPngCommand()
    {
        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "PNG Image (*.png)|*.png|JPEG (*.jpg)|*.jpg",
            DefaultExt = ".png",
            FileName = $"{ChartTitle}_{DateTime.Now:yyyyMMdd_HHmmss}.png"
        };
        if (dlg.ShowDialog() == true)
            ExportPng(dlg.FileName);
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _plot = GetTemplateChild(PartPlot) as WpfPlot;
        _cursorLabel = GetTemplateChild(PartCursorLabel) as TextBlock;
        _chartTitleBlock = GetTemplateChild(PartChartTitle) as TextBlock;
        _statusBar = GetTemplateChild(PartStatusBar) as Border;

        if (_plot != null)
        {
            ConfigurePlotDefaults();
            SubscribePlotEvents();
            RefreshChart();
        }

        if (_chartTitleBlock != null)
            _chartTitleBlock.Text = ChartTitle;
    }

    #region Public Methods

    /// <summary>
    /// Full refresh — rebuilds the entire chart from current series data.
    /// </summary>
    public void RefreshChart()
    {
        if (_plot == null) return;

        var plt = _plot.Plot;
        plt.Clear();

        plt.Title(ChartTitle);
        plt.XLabel(XLabel);
        plt.YLabel(YLabel);

        // Set axis scale
        if (IsLogX)
            plt.Axes.SetAxis(AxisType.X, AxisType.Log, "XAxis");
        else
            plt.Axes.SetAxis(AxisType.X, AxisType.Standard, "XAxis");

        // Grid
        plt.Grid.IsVisible = ShowGrid;

        // Draw series
        var items = SeriesItems;
        if (items != null)
        {
            int colorIdx = 0;
            foreach (var item in items)
            {
                if (!item.IsVisible || item.Frequencies.Length == 0) continue;

                var color = SeriesColors[colorIdx % SeriesColors.Length];
                var scatter = plt.Add.Scatter(item.Frequencies, item.Amplitudes);
                scatter.Color = new ScottPlot.Color(color.R, color.G, color.B);
                scatter.LegendText = item.Name;
                scatter.LineWidth = item.LineWidth;
                scatter.MarkerSize = item.MarkerSize;
                colorIdx++;
            }
        }

        // OASPL threshold line
        if (ShowOasplLine && items is { Count: > 0 })
        {
            var xs = items.First().Frequencies;
            if (xs.Length > 0)
            {
                var ys = Enumerable.Repeat(OasplThreshold, xs.Length).ToArray();
                var hline = plt.Add.Scatter(xs, ys);
                hline.Color = new ScottPlot.Color(239, 83, 80); // Red
                hline.LineWidth = 1;
                hline.LineStyle = LineStyle.Dash;
                hline.LegendText = $"OASPL 阈值 ({OasplThreshold:F0} dB)";
            }
        }

        plt.Legend.IsVisible = ShowLegend;

        // Configure axes
        plt.Axes.AutoScale();
        if (IsLogX && items is { Count: > 0 })
        {
            var freqs = items.SelectMany(i => i.Frequencies).ToArray();
            if (freqs.Length > 0)
                plt.Axes.SetLimitsX(freqs.Min() * 0.9, freqs.Max() * 1.1);
        }

        _plot.Refresh();
        RaiseEvent(new RoutedEventArgs(ChartRefreshedEvent, this));
    }

    /// <summary>
    /// Export the current chart to a PNG file.
    /// </summary>
    public void ExportPng(string filePath, int width = 1920, int height = 1080)
    {
        if (_plot == null) return;
        _plot.Plot.SavePng(filePath, width, height);
    }

    /// <summary>
    /// Export spectrum data to CSV.
    /// </summary>
    public void ExportCsv(string filePath)
    {
        var items = SeriesItems;
        if (items == null || items.Count == 0) return;

        using var writer = new StreamWriter(filePath, false, System.Text.Encoding.UTF8);

        // Header
        writer.Write("Frequency");
        foreach (var item in items)
            writer.Write($",{item.Name}");
        writer.WriteLine();

        // Combine all unique frequencies
        var allFreqs = items.SelectMany(i => i.Frequencies).Distinct().OrderBy(f => f).ToArray();
        foreach (var freq in allFreqs)
        {
            writer.Write(freq.ToString("F2"));
            foreach (var item in items)
            {
                var idx = Array.IndexOf(item.Frequencies, freq);
                writer.Write(idx >= 0 ? $",{item.Amplitudes[idx]:F3}" : ",");
            }
            writer.WriteLine();
        }
    }

    /// <summary>
    /// Reset the chart zoom to fit all data.
    /// </summary>
    public void ResetZoom()
    {
        _plot?.Plot.Axes.AutoScale();
        _plot?.Refresh();
    }

    #endregion

    #region Private Methods

    private void ConfigurePlotDefaults()
    {
        if (_plot == null) return;

        var plt = _plot.Plot;
        plt.Style.BackgroundColor(new ScottPlot.Color(30, 30, 30));
        plt.Style.DataBackgroundColor(new ScottPlot.Color(18, 18, 18));
        plt.Style.GridColor(new ScottPlot.Color(66, 66, 66));
        plt.Style.TickColor(new ScottPlot.Color(158, 158, 158));
        plt.Style.LabelColor(new ScottPlot.Color(224, 224, 224));
        plt.Style.TitleColor(new ScottPlot.Color(224, 224, 224));
    }

    private void SubscribePlotEvents()
    {
        if (_plot == null) return;

        _plot.MouseMove += (s, e) =>
        {
            var mouse = _plot.Interaction.GetMouseCoordinates();
            CursorFrequency = mouse.X;
            CursorAmplitude = mouse.Y;

            if (_cursorLabel != null)
                _cursorLabel.Text = $"X: {mouse.X:F1} Hz  |  Y: {mouse.Y:F1} dB";

            RaiseEvent(new RoutedEventArgs(CursorPositionChangedEvent, this));
        };
    }

    private static void OnVisualPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SpectrumChartControl control)
        {
            if (e.Property == ChartTitleProperty && control._chartTitleBlock != null)
                control._chartTitleBlock.Text = (string)e.NewValue;
            else
                control.RefreshChart();
        }
    }

    private static void OnSeriesItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not SpectrumChartControl control) return;
        if (e.OldValue is ObservableCollection<SpectrumSeriesItem> oldCol)
            oldCol.CollectionChanged -= control.OnSeriesCollectionChanged;
        if (e.NewValue is ObservableCollection<SpectrumSeriesItem> newCol)
            newCol.CollectionChanged += control.OnSeriesCollectionChanged;
        control.RefreshChart();
    }

    private static void OnLoadingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SpectrumChartControl control && control._statusBar != null)
        {
            control._statusBar.Visibility = (bool)e.NewValue ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private void OnSeriesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RefreshChart();
    }

    #endregion
}

/// <summary>
/// Represents a single data series rendered on the spectrum chart.
/// </summary>
public class SpectrumSeriesItem
{
    public string Name { get; set; } = string.Empty;
    public double[] Frequencies { get; set; } = Array.Empty<double>();
    public double[] Amplitudes { get; set; } = Array.Empty<double>();
    public bool IsVisible { get; set; } = true;
    public int ColorIndex { get; set; }
    public float LineWidth { get; set; } = 1.5f;
    public float MarkerSize { get; set; } = 0;
    public string? SpectrumType { get; set; }
    public double Oaspl { get; set; }
}
