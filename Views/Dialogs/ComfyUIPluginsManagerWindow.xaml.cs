using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Wpf.Ui.Controls;
using ProjectManager.ViewModels.Dialogs;

namespace ProjectManager.Views.Dialogs;

public partial class ComfyUIPluginsManagerWindow : FluentWindow
{
    private ComfyUIPluginsManagerViewModel? _viewModel;
    private string? _customNodesPath;

    public ComfyUIPluginsManagerWindow(ComfyUIPluginsManagerViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;

        // 订阅集合变化事件，以便在插件添加时自动更新列宽
        if (_viewModel?.Plugins != null)
        {
            _viewModel.Plugins.CollectionChanged += (s, e) =>
            {
                // 每当插件列表发生变化时，更新列宽
                Dispatcher.BeginInvoke(new Action(() => UpdateUnifiedColumnWidth()));

                // 订阅新添加项的属性变更，以便 Git 信息更新时也能更新列宽
                if (e.NewItems != null)
                {
                    foreach (var newItem in e.NewItems)
                    {
                        if (newItem is System.ComponentModel.INotifyPropertyChanged npc)
                        {
                            npc.PropertyChanged += (sender, args) =>
                            {
                                Dispatcher.BeginInvoke(new Action(() => UpdateUnifiedColumnWidth()));
                            };
                        }
                    }
                }
            };
        }

        // 在窗口加载完成后启动数据加载
        Loaded += OnLoaded;
    }

    public void Initialize(string customNodesPath, Window owner)
    {
        Owner = owner;
        _customNodesPath = customNodesPath;
        // 不在这里加载数据，等待 Loaded 事件触发后再加载
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // 窗口加载完成后设置为最大化（与 MainWindow 一致的方式）
        this.WindowState = WindowState.Maximized;

        // 窗口已经显示，现在开始非阻塞加载数据
        if (!string.IsNullOrWhiteSpace(_customNodesPath))
        {
            _viewModel?.StartLoadFromCustomNodes(_customNodesPath);
        }
    }

    /// <summary>
    /// 计算每列中最长内容的宽度，并为每列设置合适的宽度，同时确保所有列都能显示。
    /// </summary>
    private void UpdateUnifiedColumnWidth()
    {
        if (PluginsDataGrid == null)
            return;

        var plugins = _viewModel?.Plugins;
        if (plugins == null || !plugins.Any())
            return;

        // 使用窗口的字体设置，如果未指定则使用系统默认
        var typeface = new Typeface(FontFamily ?? new FontFamily("Segoe UI"),
            FontStyle,
            FontWeight,
            FontStretch);

        // 为每列单独计算最大宽度
        var columnMaxWidths = new Dictionary<int, double>();

        foreach (var plugin in plugins)
        {
            // 列 1: 插件名
            UpdateColumnMaxWidth(columnMaxWidths, 1, plugin.Name ?? string.Empty, typeface, FontSize);
            
            // 列 2: 远端地址
            UpdateColumnMaxWidth(columnMaxWidths, 2, plugin.RemoteUrl ?? string.Empty, typeface, FontSize);
            
            // 列 3: 当前分支
            UpdateColumnMaxWidth(columnMaxWidths, 3, plugin.Branch ?? string.Empty, typeface, FontSize);
            
            // 列 4: 版本 ID
            UpdateColumnMaxWidth(columnMaxWidths, 4, plugin.VersionId ?? string.Empty, typeface, FontSize);
            
            // 列 5: 最后提交信息
            UpdateColumnMaxWidth(columnMaxWidths, 5, plugin.LastCommitMessage ?? string.Empty, typeface, FontSize);
            
            // 列 6: 更新日期
            var lastUpdatedText = plugin.LastUpdated.HasValue
                ? plugin.LastUpdated.Value.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.CurrentCulture)
                : string.Empty;
            UpdateColumnMaxWidth(columnMaxWidths, 6, lastUpdatedText, typeface, FontSize);
        }

        // 添加列头的宽度到计算中
        var headers = new[] { "插件名", "远端地址", "当前分支", "版本 ID", "最后提交信息", "更新日期" };
        for (int i = 0; i < headers.Length; i++)
        {
            var headerWidth = MeasureTextWidth(headers[i], typeface, FontSize);
            if (columnMaxWidths.ContainsKey(i + 1))
            {
                columnMaxWidths[i + 1] = Math.Max(columnMaxWidths[i + 1], headerWidth);
            }
            else
            {
                columnMaxWidths[i + 1] = headerWidth;
            }
        }

        // 计算可用宽度（DataGrid 实际宽度减去 CheckBox 列和边距）
        const double padding = 16.0; // 每列左右各 8 像素
        const double checkBoxColumnWidth = 60.0; // CheckBox 列的估计宽度
        const double scrollBarWidth = 20.0; // 滚动条预留宽度
        
        var availableWidth = PluginsDataGrid.ActualWidth - checkBoxColumnWidth - scrollBarWidth;
        
        if (availableWidth <= 0)
        {
            // DataGrid 还未布局完成，使用默认宽度
            return;
        }

        // 计算所有列的总宽度（加上 padding）
        double totalDesiredWidth = 0;
        foreach (var width in columnMaxWidths.Values)
        {
            totalDesiredWidth += width + padding;
        }

        // 如果总宽度超过可用宽度，按比例缩小每列
        double scaleFactor = 1.0;
        if (totalDesiredWidth > availableWidth)
        {
            scaleFactor = availableWidth / totalDesiredWidth;
        }

        // 为每列设置宽度
        for (int i = 1; i < PluginsDataGrid.Columns.Count; i++)
        {
            if (columnMaxWidths.ContainsKey(i) && columnMaxWidths[i] > 0)
            {
                var columnWidth = (columnMaxWidths[i] + padding) * scaleFactor;
                // 设置最小宽度，确保列头可见
                columnWidth = Math.Max(columnWidth, 80);
                PluginsDataGrid.Columns[i].Width = new DataGridLength(columnWidth);
            }
        }
    }

    private void UpdateColumnMaxWidth(Dictionary<int, double> columnMaxWidths, int columnIndex, string text, Typeface typeface, double fontSize)
    {
        var width = MeasureTextWidth(text, typeface, fontSize);
        if (columnMaxWidths.ContainsKey(columnIndex))
        {
            columnMaxWidths[columnIndex] = Math.Max(columnMaxWidths[columnIndex], width);
        }
        else
        {
            columnMaxWidths[columnIndex] = width;
        }
    }

    private double MeasureTextWidth(string text, Typeface typeface, double fontSize)
    {
        if (string.IsNullOrEmpty(text))
            return 0.0;

        var formattedText = new FormattedText(
            text,
            CultureInfo.CurrentUICulture,
            FlowDirection.LeftToRight,
            typeface,
            fontSize,
            Brushes.Black,
            VisualTreeHelper.GetDpi(this).PixelsPerDip);

        return formattedText.WidthIncludingTrailingWhitespace;
    }
}
