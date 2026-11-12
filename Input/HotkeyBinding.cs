using System.Collections.Generic;

namespace DesktopFish.Input;

/// <summary>
/// 一个按住型快捷键组合：修饰键 + 可选主键。
/// 主键为 0 时表示「只按修饰键」，用于按住 Ctrl + Alt 这类交互。
/// 匹配要求修饰键状态严格一致，避免 Ctrl+Shift+F 被 Ctrl+Alt+F 误触发。
/// </summary>
public sealed class HotkeyBinding
{
    public bool Ctrl { get; set; }

    public bool Alt { get; set; }

    public bool Shift { get; set; }

    public bool Win { get; set; }

    public int VirtualKey { get; set; }

    /// <summary>既没有修饰键也没有主键。</summary>
    public bool IsEmpty => VirtualKey == 0 && !Ctrl && !Alt && !Shift && !Win;

    /// <summary>吸引模式默认「只按 Ctrl + Shift」，不搭配主键。</summary>
    public static HotkeyBinding DefaultAttract => new()
    {
        Ctrl = true,
        Shift = true,
    };

    /// <summary>交互模式默认沿用一直以来的「按住 Ctrl + Alt」。</summary>
    public static HotkeyBinding DefaultInteract => new()
    {
        Ctrl = true,
        Alt = true,
    };

    public bool HasSameCombo(HotkeyBinding other)
    {
        return Ctrl == other.Ctrl
            && Alt == other.Alt
            && Shift == other.Shift
            && Win == other.Win
            && VirtualKey == other.VirtualKey;
    }

    public bool Matches()
    {
        if (IsEmpty)
        {
            return false;
        }

        if (Ctrl != GlobalInput.IsKeyDown(GlobalInput.VK_CONTROL))
        {
            return false;
        }

        if (Alt != GlobalInput.IsKeyDown(GlobalInput.VK_MENU))
        {
            return false;
        }

        if (Shift != GlobalInput.IsKeyDown(GlobalInput.VK_SHIFT))
        {
            return false;
        }

        if (Win != GlobalInput.IsWindowsKeyDown())
        {
            return false;
        }

        // 只有修饰键的组合：修饰键状态一致即视为按住。
        if (VirtualKey == 0)
        {
            return true;
        }

        return GlobalInput.IsKeyDown(VirtualKey);
    }

    public HotkeyBinding Clone()
    {
        return new HotkeyBinding
        {
            Ctrl = Ctrl,
            Alt = Alt,
            Shift = Shift,
            Win = Win,
            VirtualKey = VirtualKey,
        };
    }

    public string DisplayName
    {
        get
        {
            if (IsEmpty)
            {
                return "未绑定";
            }

            var parts = new List<string>(5);
            if (Ctrl)
            {
                parts.Add("Ctrl");
            }

            if (Alt)
            {
                parts.Add("Alt");
            }

            if (Shift)
            {
                parts.Add("Shift");
            }

            if (Win)
            {
                parts.Add("Win");
            }

            if (VirtualKey != 0)
            {
                parts.Add(GetKeyName(VirtualKey));
            }

            return string.Join(" + ", parts);
        }
    }

    public static bool IsModifierKey(int virtualKey)
    {
        return virtualKey is 0x10 or 0x11 or 0x12 or 0x5B or 0x5C
            or 0xA0 or 0xA1 or 0xA2 or 0xA3 or 0xA4 or 0xA5;
    }

    private static string GetKeyName(int virtualKey)
    {
        if (virtualKey is >= 0x30 and <= 0x39)
        {
            return ((char)virtualKey).ToString();
        }

        if (virtualKey is >= 0x41 and <= 0x5A)
        {
            return ((char)virtualKey).ToString();
        }

        if (virtualKey is >= 0x60 and <= 0x69)
        {
            return "Num" + (virtualKey - 0x60);
        }

        if (virtualKey is >= 0x70 and <= 0x87)
        {
            return "F" + (virtualKey - 0x6F);
        }

        return virtualKey switch
        {
            0x08 => "Backspace",
            0x09 => "Tab",
            0x0D => "Enter",
            0x1B => "Esc",
            0x20 => "Space",
            0x21 => "PageUp",
            0x22 => "PageDown",
            0x23 => "End",
            0x24 => "Home",
            0x25 => "Left",
            0x26 => "Up",
            0x27 => "Right",
            0x28 => "Down",
            0x2D => "Insert",
            0x2E => "Delete",
            0xBA => ";",
            0xBB => "=",
            0xBC => ",",
            0xBD => "-",
            0xBE => ".",
            0xBF => "/",
            0xC0 => "`",
            0xDB => "[",
            0xDC => "\\",
            0xDD => "]",
            0xDE => "'",
            _ => "VK 0x" + virtualKey.ToString("X2"),
        };
    }
}
