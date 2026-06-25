using System;
using System.IO;
using System.Windows;
using CloseDriver.Core.Interfaces;
using CloseDriver.Core.Protocol;
using CloseDriver.Core.ViewModels;
using CloseDriver.Bluetooth.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

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
		var logPath = Path.Combine(AppContext.BaseDirectory, "CloseDriver.log");
		Log.Logger = new LoggerConfiguration()
			.MinimumLevel.Debug()
			.WriteTo.File(
				logPath,
				outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}",
				rollingInterval: RollingInterval.Day,
				retainedFileCountLimit: 7)
			.CreateLogger();

		Services = ConfigureServices();
	}

	private static IServiceProvider ConfigureServices()
	{
		var services = new ServiceCollection();

		// Register Logging
		services.AddLogging( (ILoggingBuilder builder) =>
		{
			builder.AddSerilog(dispose: true);
			builder.SetMinimumLevel(LogLevel.Debug);
		});

		// Register Services
		services.AddSingleton<IBluetoothService, WindowsBluetoothService>();
		services.AddSingleton<FardriverFrameReassembler>();

		// Register ViewModels
		services.AddTransient<MainViewModel>();
		services.AddTransient<DeviceDialogViewModel>();
		services.AddSingleton<DashboardViewModel>();
		services.AddSingleton<ConfigurationViewModel>();

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

