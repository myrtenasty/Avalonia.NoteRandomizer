using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.NoteRandomizer.Services;
using Avalonia.NoteRandomizer.Utils;
using Avalonia.NoteRandomizer.ViewModels;
using Avalonia.NoteRandomizer.Views;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Formatting;
using Serilog.Formatting.Display;

namespace Avalonia.NoteRandomizer;

public partial class App : Application
{
    public static IServiceProvider ServiceProvider { get; private set; } = default!;

    public static MainWindow? MainWindow
    {
        get
        {
            if (Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                return desktop.MainWindow as MainWindow;
            }
            else
            {
                throw new InvalidOperationException("无法获取 MainWindow");
            }
        }
    }

    public static MainWindowViewModel MainWindowViewModel => ServiceProvider.GetRequiredService<MainWindowViewModel>();

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        ConfigureServices();
        ConfigureLogging();
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();
            desktop.MainWindow = new MainWindow
            {
                DataContext = ServiceProvider.GetService<MainWindowViewModel>()
            };
            desktop.Startup += OnStartup;
            desktop.Exit += OnExit;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }

    private static void ConfigureServices()
    {
        var services = new ServiceCollection();

        // 注册服务和 ViewModel
        services.AddSingleton<LogCollector>();
        services.AddSingleton<SettingsService>(); // 程序设置
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<TimerService>();

        // 构建 ServiceProvider
        ServiceProvider = services.BuildServiceProvider();
    }

    private static void ConfigureLogging()
    {
        // 定义日志文件的路径
        string logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
        Directory.CreateDirectory(logDirectory); // 确保日志目录存在

        string logFilePath = Path.Combine(logDirectory, "log-.txt"); // Serilog 会根据日期自动创建文件
        var outputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}";
        ITextFormatter textFormatter = new MessageTemplateTextFormatter(outputTemplate, null);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug() // 设置最低日志级别
            .Enrich.FromLogContext()
            .WriteTo.Console() // 将日志写入控制台
            .WriteTo.File(
                path: logFilePath,
                rollingInterval: RollingInterval.Day, // 按天滚动日志文件
                retainedFileCountLimit: 7, // 保留最近7天的日志文件
                outputTemplate: outputTemplate
            )
            .WriteTo.Sink(new LogCollectorSink(ServiceProvider.GetRequiredService<LogCollector>(), textFormatter))
            .CreateLogger();

        // 可选：捕获未处理的异常
        AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
        {
            Log.Fatal(args.ExceptionObject as Exception, "未处理的异常");
            Log.CloseAndFlush();
        };

        // 可选：捕获任务未处理的异常
        TaskScheduler.UnobservedTaskException += (sender, args) =>
        {
            Log.Fatal(args.Exception, "未观察到的任务异常");
            args.SetObserved();
        };
    }

    private void OnStartup(object? s, ControlledApplicationLifetimeStartupEventArgs e)
    {
        Log.Information("\n============================================================\n");
        Log.Information("应用程序启动");
    }

    private void OnExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        Log.Information("应用程序退出");
        // 获取 SettingsService 并保存设置
        var settingsService = ServiceProvider.GetService<SettingsService>();
        settingsService?.SaveSettings();
        
        // 释放 TimerService
        ServiceProvider.GetService<TimerService>()?.Dispose();

        // 关闭 Serilog
        Log.CloseAndFlush();
    }
}