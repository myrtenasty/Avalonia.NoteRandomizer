using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia.NoteRandomizer.Models;
using Newtonsoft.Json;
using Serilog;

namespace Avalonia.NoteRandomizer.Services;

public class SettingsService
{
    private const string SettingsFileName = "settings.json";
    private readonly string _settingsFilePath;
    public SettingsModel Settings { get; private set; }

    public SettingsService()
    {
        // 获取用户应用程序数据目录
        string? appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        Assembly assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();

        var productAttribute = assembly.GetCustomAttribute<AssemblyProductAttribute>();
        string appName = productAttribute?.Product ?? "DefaultProduct";

        string? appFolder = Path.Combine(appDataPath, appName);
        if (!Directory.Exists(appFolder))
        {
            Directory.CreateDirectory(appFolder);
        }

        _settingsFilePath = Path.Combine(appFolder, SettingsFileName);

        // 加载设置
        Settings = LoadSettings();
    }

    private SettingsModel LoadSettings()
    {
        if (File.Exists(_settingsFilePath))
        {
            try
            {
                string? json = File.ReadAllText(_settingsFilePath);
                return JsonConvert.DeserializeObject<SettingsModel>(json) ?? new SettingsModel();
            }
            catch (Exception ex)
            {
                Log.Error($"加载设置时出错：{ex.Message}");
                return new SettingsModel();
            }
        }
        else
        {
            return new SettingsModel();
        }
    }

    public void SaveSettings()
    {
        try
        {
            string? json = JsonConvert.SerializeObject(Settings, Formatting.Indented);
            File.WriteAllText(_settingsFilePath, json);
        }
        catch (Exception ex)
        {
            Log.Error($"保存设置时出错：{ex.Message}");
        }
    }

    // 异步保存
    public async Task SaveSettingsAsync()
    {
        try
        {
            string? json = JsonConvert.SerializeObject(Settings, Formatting.Indented);
            await using var writer = new StreamWriter(_settingsFilePath, false);
            await writer.WriteAsync(json);
        }
        catch (Exception ex)
        {
            Log.Error($"异步保存设置时出错：{ex.Message}");
        }
    }
}