using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;

namespace AAFSS.App.Controls;

/// <summary>
/// A reusable WPF custom control extending DataGrid with built-in pagination,
/// text filtering, column visibility toggles, status footer, and CSV export.
/// </summary>
[TemplatePart(Name = PartDataGrid, Type = typeof(DataGrid))]
[TemplatePart(Name = PartFilterBox, Type = typeof(TextBox))]
[TemplatePart(Name = PartPageLabel, Type = typeof(TextBlock))]
[TemplatePart(Name = PartPrevButton, Type = typeof(Button))]
[TemplatePart(Name = PartNextButton, Type = typeof(Button))]
[TemplatePart(Name = PartPageSizeCombo, Type = typeof(ComboBox))]
[TemplatePart(Name = PartStatusText, Type = typeof(TextBlock))]
[TemplatePart(Name = PartColumnPicker, Type = typeof(ListBox))]
[TemplatePart(Name = PartColumnPickerToggle, Type = typeof(ToggleButton))]
[TemplatePart(Name = PartColumnPickerPopup, Type = typeof(Popup))]
[TemplatePart(Name = PartTotalCount, Type = typeof(TextBlock))]
[TemplatePart(Name = PartStatusOverlay, Type = typeof(Border))]
public class DataGridControl : Control
{
    private const string PartDataGrid = "PART_DataGrid";
    private const string PartFilterBox = "PART_FilterBox";
    private const string PartPageLabel = "PART_PageLabel";
    private const string PartPrevButton = "PART_PrevButton";
    private const string PartNextButton = "PART_NextButton";
    private const string PartPageSizeCombo = "PART_PageSizeCombo";
    private const string PartStatusText = "PART_StatusText";
    private const string PartColumnPicker = "PART_ColumnPicker";
    private const string PartColumnPickerToggle = "PART_ColumnPickerToggle";
    private const string PartColumnPickerPopup = "PART_ColumnPickerPopup";
    private const string PartTotalCount = "PART_TotalCount";
    private const string PartStatusOverlay = "PART_StatusOverlay";

    #region Dependency Properties

    public static readonly DependencyProperty PageSizeProperty =
        DependencyProperty.Register(nameof(PageSize), typeof(int), typeof(DataGridControl),
            new PropertyMetadata(50, OnPaginationChanged));

    public static readonly DependencyProperty CurrentPageProperty =
        DependencyProperty.Register(nameof(CurrentPage), typeof(int), typeof(DataGridControl),
            new PropertyMetadata(1, OnPaginationChanged));

    public static readonly DependencyProperty TotalItemsProperty =
        DependencyProperty.Register(nameof(TotalItems), typeof(int), typeof(DataGridControl),
            new PropertyMetadata(0, OnPaginationChanged));

    public static readonly DependencyProperty FilterTextProperty =
        DependencyProperty.Register(nameof(FilterText), typeof(string), typeof(DataGridControl),
            new PropertyMetadata(string.Empty, OnFilterTextChanged));

    public static readonly DependencyProperty IsFilterVisibleProperty =
        DependencyProperty.Register(nameof(IsFilterVisible), typeof(bool), typeof(DataGridControl),
            new PropertyMetadata(true));

    public static readonly DependencyProperty IsPaginationVisibleProperty =
        DependencyProperty.Register(nameof(IsPaginationVisible), typeof(bool), typeof(DataGridControl),
            new PropertyMetadata(true));

    public static readonly DependencyProperty IsLoadingProperty =
        DependencyProperty.Register(nameof(IsLoading), typeof(bool), typeof(DataGridControl),
            new PropertyMetadata(false, OnLoadingChanged));

    public static readonly DependencyProperty StatusTextProperty =
        DependencyProperty.Register(nameof(StatusText), typeof(string), typeof(DataGridControl),
            new PropertyMetadata("就绪"));

    public static readonly DependencyProperty ItemsSourceProperty =
        DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable), typeof(DataGridControl),
            new PropertyMetadata(null, OnItemsSourceChanged));

    public static readonly DependencyProperty AutoGenerateColumnsProperty =
        DependencyProperty.Register(nameof(AutoGenerateColumns), typeof(bool), typeof(DataGridControl),
            new PropertyMetadata(true, OnAutoGenerateChanged));

    #endregion

    #region CLR Properties

    public int PageSize
    {
        get => (int)GetValue(PageSizeProperty);
        set => SetValue(PageSizeProperty, value);
    }

    public int CurrentPage
    {
        get => (int)GetValue(CurrentPageProperty);
        set => SetValue(CurrentPageProperty, value);
    }

    public int TotalItems
    {
        get => (int)GetValue(TotalItemsProperty);
        set => SetValue(TotalItemsProperty, value);
    }

    public string FilterText
    {
        get => (string)GetValue(FilterTextProperty);
        set => SetValue(FilterTextProperty, value);
    }

    public bool IsFilterVisible
    {
        get => (bool)GetValue(IsFilterVisibleProperty);
        set => SetValue(IsFilterVisibleProperty, value);
    }

    public bool IsPaginationVisible
    {
        get => (bool)GetValue(IsPaginationVisibleProperty);
        set => SetValue(IsPaginationVisibleProperty, value);
    }

    public bool IsLoading
    {
        get => (bool)GetValue(IsLoadingProperty);
        set => SetValue(IsLoadingProperty, value);
    }

    public string StatusText
    {
        get => (string)GetValue(StatusTextProperty);
        set => SetValue(StatusTextProperty, value);
    }

    public IEnumerable ItemsSource
    {
        get => (IEnumerable)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public bool AutoGenerateColumns
    {
        get => (bool)GetValue(AutoGenerateColumnsProperty);
        set => SetValue(AutoGenerateColumnsProperty, value);
    }

    #endregion

    #region Events

    public static readonly RoutedEvent PageChangedEvent =
        EventManager.RegisterRoutedEvent(nameof(PageChanged), RoutingStrategy.Bubble,
            typeof(RoutedEventHandler), typeof(DataGridControl));

    public event RoutedEventHandler PageChanged
    {
        add => AddHandler(PageChangedEvent, value);
        remove => RemoveHandler(PageChangedEvent, value);
    }

    public static readonly RoutedEvent FilterAppliedEvent =
        EventManager.RegisterRoutedEvent(nameof(FilterApplied), RoutingStrategy.Bubble,
            typeof(RoutedEventHandler), typeof(DataGridControl));

    public event RoutedEventHandler FilterApplied
    {
        add => AddHandler(FilterAppliedEvent, value);
        remove => RemoveHandler(FilterAppliedEvent, value);
    }

    public static readonly RoutedEvent RowSelectedEvent =
        EventManager.RegisterRoutedEvent(nameof(RowSelected), RoutingStrategy.Bubble,
            typeof(RoutedEventHandler), typeof(DataGridControl));

    public event RoutedEventHandler RowSelected
    {
        add => AddHandler(RowSelectedEvent, value);
        remove => RemoveHandler(RowSelectedEvent, value);
    }

    #endregion

    #region Internal State

    private DataGrid? _grid;
    private TextBox? _filterBox;
    private TextBlock? _pageLabel;
    private Button? _prevButton;
    private Button? _nextButton;
    private ComboBox? _pageSizeCombo;
    private TextBlock? _statusText;
    private TextBlock? _totalCountLabel;
    private ListBox? _columnPicker;
    private ToggleButton? _columnPickerToggle;
    private Popup? _columnPickerPopup;
    private Border? _statusOverlay;

    private readonly ObservableCollection<ColumnVisibilityItem> _columnVisibilities = new();
    private ICollectionView? _collectionView;

    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalItems / PageSize) : 0;

    #endregion

    static DataGridControl()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(DataGridControl),
            new FrameworkPropertyMetadata(typeof(DataGridControl)));
    }

    public DataGridControl()
    {
        CommandBindings.Add(new CommandBinding(DataGridCommands.ExportCsv,
            (s, e) => OnExportCsvCommand()));
    }

    private void OnExportCsvCommand()
    {
        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "CSV Files (*.csv)|*.csv",
            DefaultExt = ".csv",
            FileName = $"export_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
        };
        if (dlg.ShowDialog() == true)
            ExportToCsv(dlg.FileName);
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _grid = GetTemplateChild(PartDataGrid) as DataGrid;
        _filterBox = GetTemplateChild(PartFilterBox) as TextBox;
        _pageLabel = GetTemplateChild(PartPageLabel) as TextBlock;
        _prevButton = GetTemplateChild(PartPrevButton) as Button;
        _nextButton = GetTemplateChild(PartNextButton) as Button;
        _pageSizeCombo = GetTemplateChild(PartPageSizeCombo) as ComboBox;
        _statusText = GetTemplateChild(PartStatusText) as TextBlock;
        _totalCountLabel = GetTemplateChild(PartTotalCount) as TextBlock;
        _columnPicker = GetTemplateChild(PartColumnPicker) as ListBox;
        _columnPickerToggle = GetTemplateChild(PartColumnPickerToggle) as ToggleButton;
        _columnPickerPopup = GetTemplateChild(PartColumnPickerPopup) as Popup;
        _statusOverlay = GetTemplateChild(PartStatusOverlay) as Border;

        // Wire up events
        if (_filterBox != null)
            _filterBox.TextChanged += OnFilterBoxTextChanged;
        if (_prevButton != null)
            _prevButton.Click += OnPrevPage;
        if (_nextButton != null)
            _nextButton.Click += OnNextPage;
        if (_pageSizeCombo != null)
        {
            _pageSizeCombo.SelectionChanged += OnPageSizeSelectionChanged;
            _pageSizeCombo.ItemsSource = new[] { 10, 25, 50, 100, 250, 500 };
            _pageSizeCombo.SelectedItem = PageSize;
        }
        if (_grid != null)
        {
            _grid.SelectionChanged += OnGridSelectionChanged;
            _grid.AutoGeneratingColumn += OnGridAutoGeneratingColumn;
        }
        if (_columnPickerToggle != null)
        {
            _columnPickerToggle.Click += (s, e) =>
            {
                if (_columnPickerPopup != null)
                    _columnPickerPopup.IsOpen = !_columnPickerPopup.IsOpen;
            };
        }

        UpdatePaginationUI();
    }

    #region Public Methods

    /// <summary>
    /// Set the ItemsSource for the data grid.
    /// </summary>
    public void SetItemsSource(IEnumerable source)
    {
        if (_grid == null) return;

        _grid.ItemsSource = source;
        _collectionView = CollectionViewSource.GetDefaultView(source);

        if (_collectionView != null && !string.IsNullOrEmpty(FilterText))
        {
            _collectionView.Filter = FilterPredicate;
            _collectionView.Refresh();
        }
    }

    /// <summary>
    /// Apply or re-apply the current filter.
    /// </summary>
    public void ApplyFilter()
    {
        if (_collectionView != null && _grid != null)
        {
            _collectionView.Filter = string.IsNullOrEmpty(FilterText) ? null : FilterPredicate;
            _collectionView.Refresh();
            UpdatePaginationUI();
            RaiseEvent(new RoutedEventArgs(FilterAppliedEvent, this));
        }
    }

    /// <summary>
    /// Clear the text filter and reset.
    /// </summary>
    public void ClearFilter()
    {
        FilterText = string.Empty;
        if (_filterBox != null)
            _filterBox.Text = string.Empty;
        ApplyFilter();
    }

    /// <summary>
    /// Navigate to a specific page (1-based).
    /// </summary>
    public void GoToPage(int page)
    {
        if (page < 1 || page > TotalPages || page == CurrentPage) return;
        CurrentPage = page;
        UpdatePaginationUI();
        RaiseEvent(new RoutedEventArgs(PageChangedEvent, this));
    }

    /// <summary>
    /// Export the grid data to CSV.
    /// </summary>
    public void ExportToCsv(string filePath)
    {
        if (_grid == null || _grid.ItemsSource == null) return;

        using var writer = new StreamWriter(filePath, false, System.Text.Encoding.UTF8);

        // Write header
        var visibleColumns = _grid.Columns
            .Where(c => c.Visibility == Visibility.Visible)
            .ToList();

        writer.WriteLine(string.Join(",", visibleColumns.Select(c =>
            EscapeCsvField(c.Header?.ToString() ?? ""))));

        // Write rows
        foreach (var item in _grid.ItemsSource)
        {
            var row = new List<string>();
            foreach (var col in visibleColumns)
            {
                var cellValue = GetCellValue(item, col);
                row.Add(EscapeCsvField(cellValue));
            }
            writer.WriteLine(string.Join(",", row));
        }
    }

    /// <summary>
    /// Refresh column visibility items based on current grid columns.
    /// </summary>
    public void RefreshColumnVisibilityItems()
    {
        if (_grid == null) return;

        _columnVisibilities.Clear();
        foreach (var col in _grid.Columns)
        {
            _columnVisibilities.Add(new ColumnVisibilityItem
            {
                Header = col.Header?.ToString() ?? $"Column {col.DisplayIndex}",
                IsVisible = col.Visibility == Visibility.Visible,
                Column = col
            });
        }

        if (_columnPicker != null)
            _columnPicker.ItemsSource = _columnVisibilities;
    }

    #endregion

    #region Private Methods

    private bool FilterPredicate(object obj)
    {
        if (string.IsNullOrEmpty(FilterText)) return true;

        var search = FilterText.ToLowerInvariant();
        foreach (var prop in obj.GetType().GetProperties())
        {
            var val = prop.GetValue(obj)?.ToString()?.ToLowerInvariant();
            if (val != null && val.Contains(search))
                return true;
        }
        return false;
    }

    private static string GetCellValue(object item, DataGridColumn column)
    {
        if (column is DataGridBoundColumn boundCol)
        {
            var binding = boundCol.Binding as Binding;
            if (binding?.Path.Path != null)
            {
                var prop = item.GetType().GetProperty(binding.Path.Path);
                return prop?.GetValue(item)?.ToString() ?? "";
            }
        }
        return "";
    }

    private static string EscapeCsvField(string field)
    {
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n'))
            return $"\"{field.Replace("\"", "\"\"")}\"";
        return field;
    }

    private void UpdatePaginationUI()
    {
        if (_pageLabel != null)
            _pageLabel.Text = $"{CurrentPage} / {TotalPages}";
        if (_statusText != null)
            _statusText.Text = StatusText;
        if (_totalCountLabel != null)
            _totalCountLabel.Text = $"共 {TotalItems:N0} 行";
        if (_prevButton != null)
            _prevButton.IsEnabled = CurrentPage > 1;
        if (_nextButton != null)
            _nextButton.IsEnabled = CurrentPage < TotalPages;
    }

    private void OnFilterBoxTextChanged(object sender, TextChangedEventArgs e)
    {
        FilterText = _filterBox?.Text ?? "";
        ApplyFilter();
    }

    private void OnPrevPage(object sender, RoutedEventArgs e)
    {
        GoToPage(CurrentPage - 1);
    }

    private void OnNextPage(object sender, RoutedEventArgs e)
    {
        GoToPage(CurrentPage + 1);
    }

    private void OnPageSizeSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_pageSizeCombo?.SelectedItem is int size)
        {
            PageSize = size;
            CurrentPage = 1;
            UpdatePaginationUI();
            RaiseEvent(new RoutedEventArgs(PageChangedEvent, this));
        }
    }

    private void OnGridSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        RaiseEvent(new RoutedEventArgs(RowSelectedEvent, this));
    }

    private void OnGridAutoGeneratingColumn(object? sender, DataGridAutoGeneratingColumnEventArgs e)
    {
        e.Column.CanUserSort = true;
    }

    private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DataGridControl control && e.NewValue is IEnumerable source)
        {
            control.SetItemsSource(source);
        }
    }

    private static void OnPaginationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DataGridControl control)
            control.UpdatePaginationUI();
    }

    private static void OnFilterTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DataGridControl control)
            control.ApplyFilter();
    }

    private static void OnLoadingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DataGridControl control)
        {
            if (control._statusOverlay != null)
                control._statusOverlay.Visibility = (bool)e.NewValue ? Visibility.Visible : Visibility.Collapsed;
            control.UpdatePaginationUI();
        }
    }

    private static void OnAutoGenerateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DataGridControl control && control._grid != null)
            control._grid.AutoGenerateColumns = (bool)e.NewValue;
    }

    #endregion
}

/// <summary>
/// Represents column visibility toggle state for the column picker.
/// </summary>
public class ColumnVisibilityItem : INotifyPropertyChanged
{
    private bool _isVisible;
    private DataGridColumn? _column;

    public string Header { get; set; } = string.Empty;

    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            if (_isVisible == value) return;
            _isVisible = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsVisible)));
            if (_column != null)
                _column.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    public DataGridColumn? Column
    {
        get => _column;
        set
        {
            _column = value;
            if (_column != null)
                _column.Visibility = _isVisible ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
