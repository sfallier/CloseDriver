using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using CloseDriver.Core.Interfaces;
using CloseDriver.Core.ViewModels;
using CloseDriver.Wpf.Services;

namespace CloseDriver.Wpf;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public new static App Current => (App)Application.Current;

    public IServiceProvider Services { get; }

    public App()
    {
        Services = ConfigureServices();
    }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // Register Logging
        services.AddLogging(configure => 
        {
            configure.AddDebug();
            configure.SetMinimumLevel(LogLevel.Debug);
        });

        // Register Services
        services.AddSingleton<IBluetoothService, WindowsBluetoothService>();

        // Register ViewModels
        services.AddTransient<MainViewModel>();
        services.AddTransient<DeviceDialogViewModel>();
        services.AddTransient<SettingsViewModel>();

        // Register Views
        services.AddTransient<MainWindow>();
        services.AddTransient<CloseDriver.Wpf.Dialogs.DeviceDialog>();

        return services.BuildServiceProvider();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var mainWindow = Services.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }
}

