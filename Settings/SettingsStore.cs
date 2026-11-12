using System;
using System.IO;
using System.Text.Json;

namespace DesktopFishTank.Settings;

/// <summary>
/// 读写 %LOCALAPPDATA%\DesktopFishTank\settings.json。
/// 配置损坏或磁盘不可写时回退到默认值，不影响鱼塘启动。
/// </summary>
public static class SettingsStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
    };

    private static string FolderPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "DesktopFishTank");

    private static string FilePath => Path.Combine(FolderPath, "settings.json");

    /// <summary>历次改名前用过的配置位置，按由新到旧排列。</summary>
    private static string[] LegacyFilePaths { get; } = new[]
    {
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DesktopFish",
            "settings.json"),
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MechFishPond",
            "settings.json"),
    };

    public static AppSettings Load()
    {
        try
        {
            MigrateLegacySettings();

            if (!File.Exists(FilePath))
            {
                return new AppSettings();
            }

            string json = File.ReadAllText(FilePath);
            AppSettings? settings = JsonSerializer.Deserialize<AppSettings>(json, SerializerOptions);
            return settings ?? new AppSettings();
        }
        catch (Exception)
        {
            return new AppSettings();
        }
    }

    /// <summary>
    /// 工程改名后新目录还没有配置：把旧目录的 settings.json 复制过来，
    /// 避免用户已调好的数量、体型、透明度等设置丢失。旧文件保留不动。
    /// </summary>
    private static void MigrateLegacySettings()
    {
        if (File.Exists(FilePath))
        {
            return;
        }

        foreach (string legacyPath in LegacyFilePaths)
        {
            if (!File.Exists(legacyPath))
            {
                continue;
            }

            Directory.CreateDirectory(FolderPath);
            File.Copy(legacyPath, FilePath, overwrite: false);
            return;
        }
    }

    public static void Save(AppSettings settings)
    {
        try
        {
            string? folder = Path.GetDirectoryName(FilePath);
            if (!string.IsNullOrEmpty(folder))
            {
                Directory.CreateDirectory(folder);
            }

            File.WriteAllText(FilePath, JsonSerializer.Serialize(settings, SerializerOptions));
        }
        catch (Exception)
        {
            // 保存失败不阻断使用，下次启动仍沿用上一次成功写入的配置。
        }
    }
}
