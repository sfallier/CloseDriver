using System.Windows;
using CloseDriver.Core.ViewModels;
using CloseDriver.Wpf.Dialogs;
using Microsoft.Extensions.DependencyInjection;

namespace CloseDriver.Wpf;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
	private readonly MainViewModel _viewModel;
	private readonly DashboardViewModel _dashboardViewModel;
	private readonly ConfigurationViewModel _configurationViewModel;

	public MainWindow(MainViewModel viewModel, DashboardViewModel dashboardViewModel, ConfigurationViewModel configurationViewModel)
	{
		InitializeComponent();
		_viewModel = viewModel;
		_dashboardViewModel = dashboardViewModel;
		_configurationViewModel = configurationViewModel;
		DataContext = _viewModel;

		DashboardView.DataContext = _dashboardViewModel;
		ConfigurationView.DataContext = _configurationViewModel;
	}

	private async void ScanButton_Click(object sender, RoutedEventArgs e)
	{
		var deviceDialog = App.Current.Services.GetRequiredService<DeviceDialog>();
		var result = deviceDialog.ShowDialog();

		if (result == true)
		{
			var dialogViewModel = (DeviceDialogViewModel)deviceDialog.DataContext;
			if (dialogViewModel.ConfirmedDevice != null)
			{
				await _viewModel.ConnectDeviceAsync(dialogViewModel.ConfirmedDevice);
			}
		}
	}
}
