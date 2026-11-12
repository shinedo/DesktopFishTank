using System.Windows;
using System.Windows.Input;
using DesktopFishTank.Input;

namespace DesktopFishTank;

/// <summary>
/// 快捷键设置窗口。点击「重新绑定」后捕获下一次非修饰键按下，
/// 组合中的 Ctrl / Alt / Shift / Win 取自按下瞬间的键盘状态。
/// 只按住修饰键然后松开，则绑定「仅修饰键」组合（如 Ctrl + Alt）。
/// </summary>
public partial class SettingsWindow : Window
{
    private readonly HotkeyBinding? _conflictBinding;
    private readonly string _hintWhenBound;
    private readonly string _hintWhenEmpty;

    private HotkeyBinding _binding;
    private bool _capturing;
    private bool _sawCtrl;
    private bool _sawAlt;
    private bool _sawShift;
    private bool _sawWin;
    private bool _downCtrl;
    private bool _downAlt;
    private bool _downShift;
    private bool _downWin;

    public SettingsWindow(
        string title,
        string description,
        string hintWhenBound,
        string hintWhenEmpty,
        HotkeyBinding binding,
        HotkeyBinding? conflictBinding = null)
    {
        InitializeComponent();

        Title = title;
        TitleText.Text = title;
        DescriptionText.Text = description;
        _hintWhenBound = hintWhenBound;
        _hintWhenEmpty = hintWhenEmpty;
        _conflictBinding = conflictBinding;
        _binding = binding.Clone();
        UpdateDisplay();
    }

    public HotkeyBinding Result => _binding;

    private void OnCaptureClick(object sender, RoutedEventArgs e)
    {
        _capturing = true;
        ClearModifierState();
        BindingText.Text = "请按下组合键…";
        CaptureHint.Text = "按 Esc 取消；只按修饰键后松开则绑定修饰键组合";
        CaptureButton.IsEnabled = false;
    }

    private void OnClearClick(object sender, RoutedEventArgs e)
    {
        ResetCapture();
        _binding = new HotkeyBinding();
        UpdateDisplay();
    }

    private void OnDoneClick(object sender, RoutedEventArgs e)
    {
        if (_conflictBinding != null && _binding.HasSameCombo(_conflictBinding))
        {
            MessageBox.Show(
                "该组合已被另一个操作占用，请换一组按键。",
                "快捷键冲突",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        DialogResult = true;
        Close();
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (!_capturing)
        {
            return;
        }

        e.Handled = true;

        Key key = e.Key == Key.System ? e.SystemKey : e.Key;

        if (key == Key.Escape)
        {
            ResetCapture();
            UpdateDisplay();
            return;
        }

        if (TryMarkModifier(key))
        {
            CaptureHint.Text = "继续按主键，或直接松开修饰键只绑定修饰键";
            return;
        }

        ModifierKeys modifiers = Keyboard.Modifiers;
        _binding = new HotkeyBinding
        {
            Ctrl = (modifiers & ModifierKeys.Control) != 0,
            Alt = (modifiers & ModifierKeys.Alt) != 0,
            Shift = (modifiers & ModifierKeys.Shift) != 0,
            Win = (modifiers & ModifierKeys.Windows) != 0,
            VirtualKey = KeyInterop.VirtualKeyFromKey(key),
        };

        ResetCapture();
        UpdateDisplay();
    }

    private void OnPreviewKeyUp(object sender, KeyEventArgs e)
    {
        if (!_capturing)
        {
            return;
        }

        Key key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (!IsModifierKey(key))
        {
            return;
        }

        e.Handled = true;
        MarkModifierDown(key, false);

        // 所有修饰键都松开且期间没按主键：绑定这组修饰键。
        if (!IsAnyModifierDown())
        {
            _binding = new HotkeyBinding
            {
                Ctrl = _sawCtrl,
                Alt = _sawAlt,
                Shift = _sawShift,
                Win = _sawWin,
            };

            ResetCapture();
            UpdateDisplay();
        }
    }

    /// <summary>记录捕获期间出现过的修饰键；返回是否为修饰键。</summary>
    private bool TryMarkModifier(Key key)
    {
        if (!IsModifierKey(key))
        {
            return false;
        }

        MarkModifierDown(key, true);
        return true;
    }

    /// <summary>
    /// 更新修饰键的按下状态。<paramref name="saw"/> 只记录「出现过」，
    /// 以免系统自动重复的 KeyDown 影响松开判定。
    /// </summary>
    private void MarkModifierDown(Key key, bool isDown)
    {
        switch (key)
        {
            case Key.LeftCtrl:
            case Key.RightCtrl:
                _downCtrl = isDown;
                _sawCtrl |= isDown;
                break;
            case Key.LeftAlt:
            case Key.RightAlt:
                _downAlt = isDown;
                _sawAlt |= isDown;
                break;
            case Key.LeftShift:
            case Key.RightShift:
                _downShift = isDown;
                _sawShift |= isDown;
                break;
            case Key.LWin:
            case Key.RWin:
                _downWin = isDown;
                _sawWin |= isDown;
                break;
        }
    }

    private bool IsAnyModifierDown()
    {
        return _downCtrl || _downAlt || _downShift || _downWin;
    }

    private void ClearModifierState()
    {
        _sawCtrl = false;
        _sawAlt = false;
        _sawShift = false;
        _sawWin = false;
        _downCtrl = false;
        _downAlt = false;
        _downShift = false;
        _downWin = false;
    }

    private static bool IsModifierKey(Key key)
    {
        return key is Key.LeftCtrl or Key.RightCtrl
            or Key.LeftAlt or Key.RightAlt
            or Key.LeftShift or Key.RightShift
            or Key.LWin or Key.RWin;
    }

    private void ResetCapture()
    {
        _capturing = false;
        ClearModifierState();
        CaptureButton.IsEnabled = true;
    }

    private void UpdateDisplay()
    {
        BindingText.Text = _binding.IsEmpty ? "未绑定" : _binding.DisplayName;

        if (_binding.IsEmpty)
        {
            CaptureHint.Text = _hintWhenEmpty;
        }
        else if (_conflictBinding != null && _binding.HasSameCombo(_conflictBinding))
        {
            CaptureHint.Text = "与另一个操作的快捷键相同，请换一组按键";
        }
        else
        {
            CaptureHint.Text = _hintWhenBound;
        }

        CaptureButton.IsEnabled = true;
    }
}
