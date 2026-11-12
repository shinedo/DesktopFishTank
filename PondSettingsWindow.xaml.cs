using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DesktopFishTank.Rendering;

namespace DesktopFishTank;

/// <summary>设置窗口回传的完整参数。</summary>
public sealed record PondSettingsValues(
    int FishCount,
    double MaxRenderScale,
    double Opacity,
    bool ShowHalo,
    IReadOnlyList<string> EnabledPalettes);

/// <summary>
/// 鱼群设置窗口：数量、最大体型、透明度用滑块连续调节，荧光配色逐项开关，
/// 改动立刻回传给主窗口生效，不需要确认。
/// </summary>
public partial class PondSettingsWindow : Window
{
    private const int DefaultCount = 8;
    private const double DefaultSize = 3.0;
    private const double DefaultOpacity = 100.0;

    private static readonly Brush LabelBrush =
        FrozenBrush(Color.FromRgb(0xDC, 0xE7, 0xF2));

    private readonly List<CheckBox> _paletteChecks = new();
    private bool _initializing = true;

    public PondSettingsWindow(
        int count,
        double maxRenderScale,
        double opacity,
        bool showHalo,
        IReadOnlyList<string>? enabledPalettes)
    {
        InitializeComponent();

        BuildPaletteChecks(enabledPalettes);

        // 在 XAML 里挂 ValueChanged 会在控件构造过程中提前触发，
        // 那时数值文本还没创建，会直接空引用；统一放到初始化之后挂。
        CountSlider.ValueChanged += OnValueChanged;
        SizeSlider.ValueChanged += OnValueChanged;
        OpacitySlider.ValueChanged += OnValueChanged;

        CountSlider.Value = Math.Clamp(count, CountSlider.Minimum, CountSlider.Maximum);
        SizeSlider.Value = Math.Clamp(maxRenderScale, SizeSlider.Minimum, SizeSlider.Maximum);
        OpacitySlider.Value = Math.Clamp(opacity * 100.0, OpacitySlider.Minimum, OpacitySlider.Maximum);
        HaloCheck.IsChecked = showHalo;

        UpdateLabels();
        _initializing = false;
    }

    /// <summary>任一参数变化时回传完整设置。</summary>
    public event Action<PondSettingsValues>? Changed;

    private void BuildPaletteChecks(IReadOnlyList<string>? enabledPalettes)
    {
        bool enableAll = enabledPalettes == null || enabledPalettes.Count == 0;

        foreach (FishPalette palette in FishPaletteCatalog.All)
        {
            var content = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center,
            };

            content.Children.Add(new Border
            {
                Width = 11,
                Height = 11,
                CornerRadius = new CornerRadius(2),
                Background = FrozenBrush(palette.Accent),
                Margin = new Thickness(0, 0, 6, 0),
                VerticalAlignment = VerticalAlignment.Center,
            });

            content.Children.Add(new TextBlock
            {
                Text = palette.Name,
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center,
            });

            var check = new CheckBox
            {
                Content = content,
                IsChecked = enableAll || ContainsName(enabledPalettes, palette.Name),
                Foreground = LabelBrush,
                Margin = new Thickness(0, 0, 14, 8),
                VerticalContentAlignment = VerticalAlignment.Center,
            };

            check.Checked += OnPaletteChanged;
            check.Unchecked += OnPaletteChanged;

            _paletteChecks.Add(check);
            PalettePanel.Children.Add(check);
        }
    }

    private static bool ContainsName(IReadOnlyList<string>? names, string name)
    {
        if (names == null)
        {
            return false;
        }

        for (int i = 0; i < names.Count; i++)
        {
            if (names[i] == name)
            {
                return true;
            }
        }

        return false;
    }

    private void OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        UpdateLabels();
        RaiseChanged();
    }

    private void OnHaloChanged(object sender, RoutedEventArgs e)
    {
        RaiseChanged();
    }

    private void OnPaletteChanged(object sender, RoutedEventArgs e)
    {
        if (_initializing)
        {
            return;
        }

        // 至少保留一种配色，取消最后一项时立刻恢复勾选。
        if (!HasCheckedPalette())
        {
            ((CheckBox)sender).IsChecked = true;
            return;
        }

        RaiseChanged();
    }

    private void OnSelectAllClick(object sender, RoutedEventArgs e)
    {
        SetAllPalettes(true);
        RaiseChanged();
    }

    private void OnResetClick(object sender, RoutedEventArgs e)
    {
        _initializing = true;

        CountSlider.Value = DefaultCount;
        SizeSlider.Value = DefaultSize;
        OpacitySlider.Value = DefaultOpacity;
        HaloCheck.IsChecked = true;
        SetAllPalettes(true);

        _initializing = false;
        UpdateLabels();
        RaiseChanged();
    }

    private void OnDoneClick(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void SetAllPalettes(bool isChecked)
    {
        bool wasInitializing = _initializing;
        _initializing = true;

        foreach (CheckBox check in _paletteChecks)
        {
            check.IsChecked = isChecked;
        }

        _initializing = wasInitializing;
    }

    private bool HasCheckedPalette()
    {
        foreach (CheckBox check in _paletteChecks)
        {
            if (check.IsChecked == true)
            {
                return true;
            }
        }

        return false;
    }

    private void UpdateLabels()
    {
        CountValue.Text = $"{(int)Math.Round(CountSlider.Value)} 条";
        SizeValue.Text = SizeSlider.Value >= SizeSlider.Maximum
            ? "不限制"
            : $"{SizeSlider.Value:0.00}×";
        OpacityValue.Text = $"{(int)Math.Round(OpacitySlider.Value)}%";
    }

    private void RaiseChanged()
    {
        if (_initializing)
        {
            return;
        }

        var names = new List<string>();
        for (int i = 0; i < _paletteChecks.Count; i++)
        {
            if (_paletteChecks[i].IsChecked == true)
            {
                names.Add(FishPaletteCatalog.All[i].Name);
            }
        }

        Changed?.Invoke(new PondSettingsValues(
            (int)Math.Round(CountSlider.Value),
            Math.Round(SizeSlider.Value, 2),
            Math.Round(OpacitySlider.Value) / 100.0,
            HaloCheck.IsChecked == true,
            names));
    }

    private static Brush FrozenBrush(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }
}
