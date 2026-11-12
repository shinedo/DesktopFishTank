using System;
using Microsoft.Win32;

namespace DesktopFishTank.Settings;

/// <summary>
/// 开机自启：写入当前用户的 Run 键，不需要管理员权限，也不改动系统服务。
/// 注册表本身就是状态源，不再往配置文件里重复存一份。
/// </summary>
internal static class StartupRegistration
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

    /// <summary>Run 键下的值名，卸载或取消自启时按它删除。</summary>
    private const string ValueName = "DesktopFishTank";

    /// <summary>当前是否已登记开机自启；读取失败按未启用处理。</summary>
    public static bool IsEnabled()
    {
        try
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false);
            return key?.GetValue(ValueName) is string value && !string.IsNullOrWhiteSpace(value);
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// 启用或取消开机自启，返回是否写入成功。
    /// 启用时记录当前进程的可执行文件路径，发布程序换目录后需要重新勾选一次。
    /// </summary>
    public static bool SetEnabled(bool enabled)
    {
        try
        {
            using RegistryKey key = Registry.CurrentUser.CreateSubKey(RunKeyPath, true);

            if (enabled)
            {
                string? path = Environment.ProcessPath;
                if (string.IsNullOrEmpty(path))
                {
                    return false;
                }

                // 路径带引号，目录里有空格时也能正确启动。
                key.SetValue(ValueName, "\"" + path + "\"");
            }
            else
            {
                key.DeleteValue(ValueName, false);
            }

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
