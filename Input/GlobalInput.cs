using DesktopFish.Interop;

namespace DesktopFish.Input;

/// <summary>
/// 全局按键状态查询。用 GetAsyncKeyState 轮询，不注册系统热键，
/// 因此不会占用其他程序的快捷键，也能检测「按住」与「松开」。
/// </summary>
internal static class GlobalInput
{
    public const int VK_SHIFT = 0x10;
    public const int VK_CONTROL = 0x11;
    public const int VK_MENU = 0x12;
    public const int VK_LWIN = 0x5B;
    public const int VK_RWIN = 0x5C;

    public static bool IsKeyDown(int virtualKey)
    {
        return (NativeMethods.GetAsyncKeyState(virtualKey) & 0x8000) != 0;
    }

    public static bool IsWindowsKeyDown()
    {
        return IsKeyDown(VK_LWIN) || IsKeyDown(VK_RWIN);
    }
}
