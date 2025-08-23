using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace ProjectManager.Behaviors
{
    /// <summary>
    /// 将字符串集合渲染到 RichTextBox，并解析常见 ANSI 转义序列，为“更像真实终端”的着色、粗体等提供支持。
    /// 轻量实现：覆盖 16 色前景/背景、粗体、下划线，忽略不支持的序列；可选自动滚动与条数上限。
    /// </summary>
    public static class AnsiRichTextBehavior
    {
        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.RegisterAttached(
            "ItemsSource", typeof(INotifyCollectionChanged), typeof(AnsiRichTextBehavior),
            new PropertyMetadata(null, OnItemsSourceChanged));

        public static readonly DependencyProperty EnableAnsiParsingProperty = DependencyProperty.RegisterAttached(
            "EnableAnsiParsing", typeof(bool), typeof(AnsiRichTextBehavior), new PropertyMetadata(true));

        public static readonly DependencyProperty AutoScrollProperty = DependencyProperty.RegisterAttached(
            "AutoScroll", typeof(bool), typeof(AnsiRichTextBehavior), new PropertyMetadata(true));

        public static readonly DependencyProperty MaxParagraphsProperty = DependencyProperty.RegisterAttached(
            "MaxParagraphs", typeof(int), typeof(AnsiRichTextBehavior), new PropertyMetadata(2000));

        public static readonly DependencyProperty PreferFontFamiliesProperty = DependencyProperty.RegisterAttached(
            "PreferFontFamilies", typeof(string), typeof(AnsiRichTextBehavior), new PropertyMetadata(null, OnPreferFontFamiliesChanged));

        public static void SetItemsSource(DependencyObject element, INotifyCollectionChanged? value) => element.SetValue(ItemsSourceProperty, value);
        public static INotifyCollectionChanged? GetItemsSource(DependencyObject element) => (INotifyCollectionChanged?)element.GetValue(ItemsSourceProperty);

        public static void SetEnableAnsiParsing(DependencyObject element, bool value) => element.SetValue(EnableAnsiParsingProperty, value);
        public static bool GetEnableAnsiParsing(DependencyObject element) => (bool)element.GetValue(EnableAnsiParsingProperty);

        public static void SetAutoScroll(DependencyObject element, bool value) => element.SetValue(AutoScrollProperty, value);
        public static bool GetAutoScroll(DependencyObject element) => (bool)element.GetValue(AutoScrollProperty);

        public static void SetMaxParagraphs(DependencyObject element, int value) => element.SetValue(MaxParagraphsProperty, value);
        public static int GetMaxParagraphs(DependencyObject element) => (int)element.GetValue(MaxParagraphsProperty);

        public static void SetPreferFontFamilies(DependencyObject element, string? value) => element.SetValue(PreferFontFamiliesProperty, value);
        public static string? GetPreferFontFamilies(DependencyObject element) => (string?)element.GetValue(PreferFontFamiliesProperty);

        private static void OnPreferFontFamiliesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not RichTextBox rtb) return;
            var list = e.NewValue as string;
            if (string.IsNullOrWhiteSpace(list)) return;
            TryApplyPreferredFont(rtb, list);
            // 同步更新 FlowDocument 的排版，避免刷新后字体回退
            if (rtb.Document != null)
            {
                rtb.Document.FontFamily = rtb.FontFamily;
                rtb.Document.FontSize = rtb.FontSize;
                rtb.Document.FontWeight = rtb.FontWeight;
                rtb.Document.LineStackingStrategy = LineStackingStrategy.BlockLineHeight;
                rtb.Document.LineHeight = Math.Max(1.0, rtb.FontSize * 1.2);
            }
        }

        private static void TryApplyPreferredFont(RichTextBox rtb, string list)
        {
            var candidates = list.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (candidates.Length == 0) return;
            foreach (var candidate in candidates)
            {
                var font = FindSystemFontFamily(candidate);
                if (font != null)
                {
                    rtb.FontFamily = font;
                    return;
                }
            }
        }

        private static FontFamily? FindSystemFontFamily(string name)
        {
            foreach (var fam in Fonts.SystemFontFamilies)
            {
                // 常见匹配策略：Source/包含关系
                if (fam.Source.Equals(name, StringComparison.OrdinalIgnoreCase)) return fam;
                if (fam.Source.Contains(name, StringComparison.OrdinalIgnoreCase)) return fam;
                // 使用族名称字典尝试匹配（可能是本地化名称）
                try
                {
                    foreach (var kv in fam.FamilyNames)
                    {
                        if (kv.Value.Equals(name, StringComparison.OrdinalIgnoreCase)) return fam;
                        if (kv.Value.Contains(name, StringComparison.OrdinalIgnoreCase)) return fam;
                    }
                }
                catch { }
            }
            return null;
        }

    private static readonly Dictionary<RichTextBox, RenderState> _states = new();
    private static readonly Dictionary<RichTextBox, NotifyCollectionChangedEventHandler> _handlers = new();

        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not RichTextBox rtb) return;
            if (e.OldValue is INotifyCollectionChanged oldCol)
            {
                if (_handlers.TryGetValue(rtb, out var oldHandler))
                {
                    try { oldCol.CollectionChanged -= oldHandler; } catch { }
                    _handlers.Remove(rtb);
                }
            }

            if (e.NewValue is INotifyCollectionChanged newCol)
            {
                // 初次全量渲染
                if (e.NewValue is System.Collections.IEnumerable enumerable)
                {
                    rtb.Document = new FlowDocument();
                    // 确保文档使用与 RichTextBox 一致的字体、字号与字重，并设置行高 1.2
                    rtb.Document.FontFamily = rtb.FontFamily;
                    rtb.Document.FontSize = rtb.FontSize;
                    rtb.Document.FontWeight = rtb.FontWeight;
                    rtb.Document.LineStackingStrategy = LineStackingStrategy.BlockLineHeight;
                    rtb.Document.LineHeight = Math.Max(1.0, rtb.FontSize * 1.2);
                    EnsureState(rtb);
                    // 创建第一段
                    if (rtb.Document.Blocks.LastBlock is not Paragraph)
                    {
                        var p = new Paragraph { Margin = new Thickness(0) };
                        rtb.Document.Blocks.Add(p);
                        _states[rtb].CurrentParagraph = p;
                    }
                    foreach (var item in enumerable)
                    {
                        AppendFragment(rtb, item?.ToString() ?? string.Empty, GetEnableAnsiParsing(rtb));
                    }
                }

                NotifyCollectionChangedEventHandler handler = (s, ev) =>
                {
                    if (ev.Action == NotifyCollectionChangedAction.Add && ev.NewItems != null)
                    {
                        foreach (var item in ev.NewItems)
                        {
                            AppendFragment(rtb, item?.ToString() ?? string.Empty, GetEnableAnsiParsing(rtb));
                        }
                    }
                    else if (ev.Action == NotifyCollectionChangedAction.Reset)
                    {
                        rtb.Document = new FlowDocument();
                        rtb.Document.FontFamily = rtb.FontFamily;
                        rtb.Document.FontSize = rtb.FontSize;
                        rtb.Document.FontWeight = rtb.FontWeight;
                        rtb.Document.LineStackingStrategy = LineStackingStrategy.BlockLineHeight;
                        rtb.Document.LineHeight = Math.Max(1.0, rtb.FontSize * 1.2);
                        _states.Remove(rtb);
                        EnsureState(rtb);
                        var p = new Paragraph { Margin = new Thickness(0) };
                        rtb.Document.Blocks.Add(p);
                        _states[rtb].CurrentParagraph = p;
                    }

                    // 限制段落数量
                    var max = GetMaxParagraphs(rtb);
                    while (rtb.Document.Blocks.Count > max)
                    {
                        rtb.Document.Blocks.Remove(rtb.Document.Blocks.FirstBlock);
                    }

                    // 自动滚动到末尾
                    if (GetAutoScroll(rtb))
                    {
                        rtb.ScrollToEnd();
                    }
                };
                newCol.CollectionChanged += handler;
                _handlers[rtb] = handler;
            }
        }

        private static readonly Regex SgrRegex = new("\u001B\\[[0-9;]*m", RegexOptions.Compiled);

        private static void AppendFragment(RichTextBox rtb, string fragment, bool parseAnsi)
        {
            if (string.IsNullOrEmpty(fragment)) return;
            EnsureState(rtb);
            var state = _states[rtb];
            if (state.CurrentParagraph == null)
            {
                state.CurrentParagraph = new Paragraph { Margin = new Thickness(0) };
                rtb.Document.Blocks.Add(state.CurrentParagraph);
            }

            int i = 0;
            while (i < fragment.Length)
            {
                char ch = fragment[i];
                // 控制字符处理
                if (ch == '\r')
                {
                    state.PendingCarriageReturn = true;
                    i++; continue;
                }
                if (ch == '\n')
                {
                    // 换行：新段落
                    state.PendingCarriageReturn = false;
                    state.CurrentParagraph = new Paragraph { Margin = new Thickness(0) };
                    rtb.Document.Blocks.Add(state.CurrentParagraph);
                    i++; continue;
                }
                if (ch == '\b')
                {
                    RemoveLastChar(state.CurrentParagraph);
                    i++; continue;
                }
                if (ch == '\u001B' && i + 1 < fragment.Length)
                {
                    // 解析 CSI 序列
                    int consumed = TryConsumeEscape(rtb, state, fragment, i);
                    if (consumed > 0) { i += consumed; continue; }
                }

                // 普通可打印文本：批量收集直到遇到控制/ESC/LF
                int start = i;
                while (i < fragment.Length)
                {
                    char c2 = fragment[i];
                    if (c2 == '\r' || c2 == '\n' || c2 == '\b' || c2 == '\u001B') break;
                    i++;
                }
                var text = fragment.Substring(start, i - start);
                if (text.Length > 0)
                {
                    if (state.PendingCarriageReturn)
                    {
                        ClearParagraph(state.CurrentParagraph);
                        state.PendingCarriageReturn = false;
                    }
                    if (parseAnsi)
                        AppendTextWithAnsi(state, text);
                    else
                        state.CurrentParagraph.Inlines.Add(new Run(text));
                }
            }
        }

        private static void AppendTextWithAnsi(RenderState state, string text)
        {
            if (state.CurrentParagraph == null)
            {
                // 安全保护：如果不存在段落，则跳过
                return;
            }
            // 将包含 SGR 的文本段拆分
            int lastIndex = 0;
            foreach (Match m in SgrRegex.Matches(text))
            {
                if (m.Index > lastIndex)
                {
                    state.CurrentParagraph.Inlines.Add(CreateRun(text.Substring(lastIndex, m.Index - lastIndex), state.CurrentStyle));
                }
                ApplySgrSequence(state.CurrentStyle, m.Value);
                lastIndex = m.Index + m.Length;
            }
            if (lastIndex < text.Length)
            {
                state.CurrentParagraph.Inlines.Add(CreateRun(text.Substring(lastIndex), state.CurrentStyle));
            }
        }

        private static Run CreateRun(string content, AnsiStyle style)
        {
            var run = new Run(content);
            if (style.Bold) run.FontWeight = FontWeights.Bold;
            if (style.Italic) run.FontStyle = FontStyles.Italic;

            // 文本装饰组合
            TextDecorationCollection? decos = null;
            if (style.Underline)
            {
                decos ??= new TextDecorationCollection();
                foreach (var d in TextDecorations.Underline) decos.Add(d);
            }
            if (style.Strike)
            {
                decos ??= new TextDecorationCollection();
                foreach (var d in TextDecorations.Strikethrough) decos.Add(d);
            }
            if (decos != null) run.TextDecorations = decos;

            // 颜色与反显/半亮处理
            var (fg, bg) = style.GetEffectiveColors();
            var fgBrush = new SolidColorBrush(fg);
            if (style.Dim) fgBrush.Opacity = 0.7;
            run.Foreground = fgBrush;
            if (bg.HasValue)
            {
                run.Background = new SolidColorBrush(bg.Value);
            }

            // 隐藏：用背景色绘制前景（近似隐藏）
            if (style.Hidden)
            {
                if (bg.HasValue)
                {
                    run.Foreground = new SolidColorBrush(bg.Value);
                }
                else
                {
                    run.Foreground = new SolidColorBrush(Colors.Transparent);
                }
            }
            return run;
        }

    private static void ApplySgrSequence(AnsiStyle style, string esc)
        {
            // esc: "\u001B[...m"
            var codesStr = esc.Substring(2, esc.Length - 3); // remove \u001B[
            if (string.IsNullOrEmpty(codesStr)) { style.Reset(); return; }
            var parts = codesStr.Split(';');
            if (parts.Length == 0) { style.Reset(); return; }

            for (int i = 0; i < parts.Length; i++)
            {
                if (!int.TryParse(parts[i], out var code)) { continue; }
                switch (code)
                {
                    case 0: style.Reset(); break;
                    case 1: style.Bold = true; style.Dim = false; break;
                    case 2: style.Dim = true; break;
                    case 3: style.Italic = true; break;
                    case 4: style.Underline = true; break;
                    case 5: style.Blink = true; break; // 无原生支持，忽略效果
                    case 7: style.Inverse = true; break;
                    case 8: style.Hidden = true; break;
                    case 9: style.Strike = true; break;
                    case 21: style.Bold = false; break; // 或双下划线，按取消粗体处理
                    case 22: style.Bold = false; style.Dim = false; break;
                    case 23: style.Italic = false; break;
                    case 24: style.Underline = false; break;
                    case 25: style.Blink = false; break;
                    case 27: style.Inverse = false; break;
                    case 28: style.Hidden = false; break;
                    case 29: style.Strike = false; break;
                    // 前景 30-37 基本色, 90-97 亮色
                    case >= 30 and <= 37:
                        style.Foreground = MapBasicColor(code - 30, bright: false);
                        break;
                    case >= 90 and <= 97:
                        style.Foreground = MapBasicColor(code - 90, bright: true);
                        break;
                    case 39:
                        style.Foreground = AnsiStyle.DefaultForeground; // 默认前景（Campbell）
                        break;
                    // 背景 40-47, 100-107
                    case >= 40 and <= 47:
                        style.Background = MapBasicColor(code - 40, bright: false);
                        break;
                    case >= 100 and <= 107:
                        style.Background = MapBasicColor(code - 100, bright: true);
                        break;
                    case 49:
                        style.Background = null;
                        break;
                    // 扩展色 38/48
                    case 38:
                    case 48:
                        bool isFg = code == 38;
                        if (i + 1 < parts.Length && int.TryParse(parts[i + 1], out var mode))
                        {
                            if (mode == 5 && i + 2 < parts.Length && int.TryParse(parts[i + 2], out var idx))
                            {
                                var c = Map256Color(idx);
                                if (isFg) style.Foreground = c; else style.Background = c;
                                i += 2;
                            }
                            else if (mode == 2 && i + 4 < parts.Length &&
                                     int.TryParse(parts[i + 2], out var r) &&
                                     int.TryParse(parts[i + 3], out var g) &&
                                     int.TryParse(parts[i + 4], out var b))
                            {
                                var c = Color.FromRgb((byte)Clamp(r), (byte)Clamp(g), (byte)Clamp(b));
                                if (isFg) style.Foreground = c; else style.Background = c;
                                i += 4;
                            }
                        }
                        break;
                    default:
                        // 未处理代码，忽略
                        break;
                }
            }
        }

        private static Color MapBasicColor(int idx, bool bright)
        {
            // Windows Terminal - Campbell 配色
            // 标准:  黑 #0C0C0C, 红 #C50F1F, 绿 #13A10E, 黄 #C19C00, 蓝 #0037DA, 品红 #881798, 青 #3A96DD, 白 #CCCCCC
            // 亮色: 深灰 #767676, 亮红 #E74856, 亮绿 #16C60C, 亮黄 #F9F1A5, 亮蓝 #3B78FF, 亮品红 #B4009E, 亮青 #61D6D6, 亮白 #F2F2F2
            return (idx, bright) switch
            {
                (0, false) => FromHex("#0C0C0C"),
                (1, false) => FromHex("#C50F1F"),
                (2, false) => FromHex("#13A10E"),
                (3, false) => FromHex("#C19C00"),
                (4, false) => FromHex("#0037DA"),
                (5, false) => FromHex("#881798"),
                (6, false) => FromHex("#3A96DD"),
                (7, false) => FromHex("#CCCCCC"),
                (0, true) => FromHex("#767676"),
                (1, true) => FromHex("#E74856"),
                (2, true) => FromHex("#16C60C"),
                (3, true) => FromHex("#F9F1A5"),
                (4, true) => FromHex("#3B78FF"),
                (5, true) => FromHex("#B4009E"),
                (6, true) => FromHex("#61D6D6"),
                (7, true) => FromHex("#F2F2F2"),
                _ => FromHex("#CCCCCC")
            };
        }

        private static Color Map256Color(int idx)
        {
            idx = Math.Clamp(idx, 0, 255);
            if (idx < 16)
            {
                // 0-15: 标准/亮色
                var bright = idx >= 8;
                var baseIdx = idx % 8;
                return MapBasicColor(baseIdx, bright);
            }
            if (idx < 232)
            {
                // 16-231: 6x6x6 立方体
                int n = idx - 16;
                int r = n / 36; int g = (n % 36) / 6; int b = n % 6;
                byte cv(int v) => (byte)(v == 0 ? 0 : 55 + 40 * v);
                return Color.FromRgb(cv(r), cv(g), cv(b));
            }
            // 232-255: 灰阶
            int gray = 8 + 10 * (idx - 232);
            return Color.FromRgb((byte)gray, (byte)gray, (byte)gray);
        }

        private static int Clamp(int v) => v < 0 ? 0 : (v > 255 ? 255 : v);
        private static Color FromHex(string hex)
        {
            if (hex.StartsWith("#")) hex = hex.Substring(1);
            byte a = 255;
            byte r = 0, g = 0, b = 0;
            if (hex.Length == 6)
            {
                r = Convert.ToByte(hex.Substring(0, 2), 16);
                g = Convert.ToByte(hex.Substring(2, 2), 16);
                b = Convert.ToByte(hex.Substring(4, 2), 16);
            }
            else if (hex.Length == 8)
            {
                a = Convert.ToByte(hex.Substring(0, 2), 16);
                r = Convert.ToByte(hex.Substring(2, 2), 16);
                g = Convert.ToByte(hex.Substring(4, 2), 16);
                b = Convert.ToByte(hex.Substring(6, 2), 16);
            }
            return Color.FromArgb(a, r, g, b);
        }

        private static int TryConsumeEscape(RichTextBox rtb, RenderState state, string s, int index)
        {
            // 期望 \u001B [ ... final
            if (index + 1 >= s.Length) return 0;
            if (s[index] != '\u001B' || s[index + 1] != '[') return 0;
            int j = index + 2;
            // 收集参数直到以字母结尾
            while (j < s.Length && !char.IsLetter(s[j])) j++;
            if (j >= s.Length) return 0;
            char final = s[j];
            var payload = s.Substring(index, j - index + 1); // 包含 ESC ... final

            switch (final)
            {
                case 'm':
                    ApplySgrSequence(state.CurrentStyle, payload);
                    return payload.Length;
                case 'K':
                    // 清除到行尾：我们近似清空整行
                    if (state.CurrentParagraph != null)
                        ClearParagraph(state.CurrentParagraph);
                    return payload.Length;
                case 'J':
                    // 0 清屏到光标后，1 清屏到光标前，2 清整屏；我们统一清整屏
                    rtb.Document.Blocks.Clear();
                    state.CurrentParagraph = new Paragraph { Margin = new Thickness(0) };
                    rtb.Document.Blocks.Add(state.CurrentParagraph);
                    return payload.Length;
                case 'H':
                case 'f':
                    // 光标移动到原点：近似为回车清行
                    state.PendingCarriageReturn = true;
                    return payload.Length;
                default:
                    // 未实现的 CSI，忽略
                    return payload.Length;
            }
        }

        private static void EnsureState(RichTextBox rtb)
        {
            if (!_states.ContainsKey(rtb))
            {
                _states[rtb] = new RenderState
                {
                    CurrentStyle = new AnsiStyle(),
                    CurrentParagraph = null,
                    PendingCarriageReturn = false
                };
            }
        }

        private static void ClearParagraph(Paragraph p)
        {
            p.Inlines.Clear();
        }

        private static void RemoveLastChar(Paragraph p)
        {
            if (p.Inlines.LastInline is Run r && !string.IsNullOrEmpty(r.Text))
            {
                r.Text = r.Text.Substring(0, r.Text.Length - 1);
                if (r.Text.Length == 0)
                {
                    p.Inlines.Remove(r);
                }
            }
            else if (p.Inlines.Count > 0)
            {
                p.Inlines.Remove(p.Inlines.LastInline);
            }
        }

        private class RenderState
        {
            public Paragraph? CurrentParagraph { get; set; }
            public AnsiStyle CurrentStyle { get; set; } = new();
            public bool PendingCarriageReturn { get; set; }
        }

        private class AnsiStyle
        {
            public bool Bold { get; set; }
            public bool Dim { get; set; }
            public bool Italic { get; set; }
            public bool Underline { get; set; }
            public bool Blink { get; set; }
            public bool Inverse { get; set; }
            public bool Hidden { get; set; }
            public bool Strike { get; set; }
            public static readonly Color DefaultForeground = FromHex("#CCCCCC");
            public static readonly Color DefaultBackground = FromHex("#0C0C0C");
            public Color Foreground { get; set; } = DefaultForeground;
            public Color? Background { get; set; }
            public void Reset()
            {
                Bold = false; Dim = false; Italic = false; Underline = false; Blink = false; Inverse = false; Hidden = false; Strike = false; Foreground = DefaultForeground; Background = null;
            }
            public (Color fg, Color? bg) GetEffectiveColors()
            {
                if (!Inverse) return (Foreground, Background);
                // 反显：互换
                var fg = Background ?? DefaultBackground;
                var bg = (Color?)Foreground;
                return (fg, bg);
            }
        }
    }
}
