using System;
using System.Windows;
using CloseDriver.Core.Interfaces;
using CloseDriver.Core.ViewModels;
using CloseDriver.Wpf.Converters;
using CloseDriver.Bluetooth.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CloseDriver.Wpf;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
	public static new App Current => (App)Application.Current;

	public IServiceProvider Services { get; }

	public App()
	{
		// App.xaml has no StartupUri (DI owns MainWindow creation), so WPF's markup
		// compiler does not generate InitializeComponent for App and the resources
		// declared in App.xaml are never loaded. Register them in code instead.
		Resources["NullToVisibilityConverter"] = new NullToVisibilityConverter();

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
		services.AddTransient<Dialogs.DeviceDialog>();

		return services.BuildServiceProvider();
	}

	protected override void OnStartup(StartupEventArgs e)
	{
		base.OnStartup(e);

		var mainWindow = Services.GetRequiredService<MainWindow>();
		mainWindow.Show();
	}
}

