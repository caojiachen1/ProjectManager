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

        // 计算可用宽度（DataGrid 实际宽度减去 CheckBox 列、操作列和滚动条的预留）
        const double padding = 16.0; // 每列左右各 8 像素
        const double checkBoxColumnWidth = 60.0; // CheckBox 列的估计宽度
        const double scrollBarWidth = 45.0; // 滚动条预留宽度
        const double opColumnWidth = 60.0; // 操作列固定宽度（与 XAML 一致）

        // 可用于其他列的宽度需减去 checkbox、操作列与滚动条的宽度
        var availableWidth = PluginsDataGrid.ActualWidth - checkBoxColumnWidth - opColumnWidth - scrollBarWidth;
        
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

        // 我们希望在空间不足时优先压缩“最后提交信息”列（index 5），同时保持最右侧操作列固定宽度。
        // 计算每列期望宽度（含 padding），但暂不应用。
        var desiredWidths = new Dictionary<int, double>();
        for (int i = 1; i < Math.Max(PluginsDataGrid.Columns.Count - 1, 1); i++)
        {
            if (columnMaxWidths.ContainsKey(i) && columnMaxWidths[i] > 0)
            {
                desiredWidths[i] = columnMaxWidths[i] + padding;
            }
        }

        // 常量：提交列索引与最小宽度
        const int commitColumnIndex = 5;
        const double commitMinWidth = 50.0;
        const double otherMinWidth = 80.0;

        // 操作列固定宽度（与 XAML 对齐）

        // 计算除了操作列和 checkbox 列外，可用于普通列的总可用宽度
        // 注意 availableWidth 已减去了 checkbox 和 scrollbar 的预留

        // 先计算除提交列之外的其他列所需宽度
        double othersDesired = 0;
        var otherIndices = new List<int>();
        for (int i = 1; i < Math.Max(PluginsDataGrid.Columns.Count - 1, 1); i++)
        {
            if (i == commitColumnIndex) continue;
            if (desiredWidths.ContainsKey(i))
            {
                othersDesired += desiredWidths[i];
                otherIndices.Add(i);
            }
        }

        // 计算分配给提交列的宽度：尽量保留其他列所需宽度，将剩余空间分配给提交列
        var remainingForCommit = availableWidth - othersDesired;

        double commitAssigned;
        if (desiredWidths.ContainsKey(commitColumnIndex))
        {
            var commitDesired = desiredWidths[commitColumnIndex];
            if (remainingForCommit >= commitDesired)
            {
                // 空间足够，使用期望值
                commitAssigned = commitDesired;
            }
            else if (remainingForCommit >= commitMinWidth)
            {
                // 空间不足但能至少满足最小值，分配剩余空间
                commitAssigned = remainingForCommit;
            }
            else
            {
                // 连最小值也无法直接分配，先把提交列设为最小，然后需要从其他列压缩以腾出空间
                commitAssigned = commitMinWidth;
                var deficit = commitMinWidth - Math.Max(0, remainingForCommit);

                // 需要从 otherIndices 按比例或按最小宽度压缩 othersDesired 来补偿 deficit
                double adjustableSum = 0;
                foreach (var idx in otherIndices)
                {
                    adjustableSum += Math.Max(0, desiredWidths[idx] - (otherMinWidth + padding));
                }

                if (adjustableSum <= 0)
                {
                    // 无法从其他列压缩（都已接近最小），那就按最小值分配（可能会超出 availableWidth）
                    // 这种极端情况下，我们仍然设置其他列为最小宽度
                    foreach (var idx in otherIndices)
                    {
                        desiredWidths[idx] = otherMinWidth + padding;
                    }
                }
                else
                {
                    // 按比例从可压缩量中抽取 deficit
                    foreach (var idx in otherIndices)
                    {
                        var canReduce = Math.Max(0, desiredWidths[idx] - (otherMinWidth + padding));
                        var reduction = (canReduce / adjustableSum) * deficit;
                        desiredWidths[idx] = Math.Max(otherMinWidth + padding, desiredWidths[idx] - reduction);
                    }
                }
            }
        }
        else
        {
            // 如果没有提交列，则 nothing to do
            commitAssigned = 0;
        }

        // 现在将计算好的宽度应用到列（commit列和其他列），注意 desiredWidths 中含 padding
        for (int i = 1; i < Math.Max(PluginsDataGrid.Columns.Count - 1, 1); i++)
        {
            if (i == commitColumnIndex)
            {
                if (commitAssigned > 0)
                {
                    var w = Math.Max(commitAssigned, commitMinWidth);
                    PluginsDataGrid.Columns[i].Width = new DataGridLength(w);
                }
            }
            else if (desiredWidths.ContainsKey(i))
            {
                var w = Math.Max(desiredWidths[i], otherMinWidth + padding);
                PluginsDataGrid.Columns[i].Width = new DataGridLength(w);
            }
        }

        // 确保最后一列（操作列）使用固定宽度显示
        if (PluginsDataGrid.Columns.Count > 0)
        {
            var lastIndex = PluginsDataGrid.Columns.Count - 1;
            // 固定宽度（与 XAML 中定义一致）
            var fixedWidth = 60.0;
            PluginsDataGrid.Columns[lastIndex].Width = new DataGridLength(fixedWidth);
            PluginsDataGrid.Columns[lastIndex].MinWidth = fixedWidth;
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
