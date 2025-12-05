using StandUpReminder.ViewModels;
using System.ComponentModel;
using System.Windows;

namespace StandUpReminder;

public partial class MainWindow : Window
{
	private bool _isExiting = false;

	public MainWindow()
	{
		InitializeComponent();
	}

	private async void ShowTimedBalloonTip(string title, string message, int milliseconds = 3000)
	{
		TaskbarIcon.ShowBalloonTip(title, message, Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info);

		await Task.Delay(milliseconds);

		// 隐藏气泡提示
		TaskbarIcon.HideBalloonTip();
	}

	private void Window_StateChanged(object sender, System.EventArgs e)
	{
		if (WindowState == WindowState.Minimized)
		{
			Hide();
			ShowTimedBalloonTip("站立提醒助手", "程序已最小化到系统托盘", 3000);
		}
	}

	private void Window_Closing(object sender, CancelEventArgs e)
	{
		if (!_isExiting)
		{
			e.Cancel = true;
			Hide();
			ShowTimedBalloonTip("站立提醒助手", "程序已最小化到系统托盘，单击图标可打开", 3000);
		}
		else
		{
			(DataContext as MainViewModel)?.Cleanup();
			TaskbarIcon.Dispose();
		}
	}


	private void TaskbarIcon_TrayMouseDoubleClick(object sender, RoutedEventArgs e)
	{
		ShowMainWindow();
	}

	private void TaskbarIcon_TrayLeftMouseUp(object sender, RoutedEventArgs e)
	{
		ShowMainWindow();
	}

	private void ShowWindow_Click(object sender, RoutedEventArgs e)
	{
		ShowMainWindow();
	}

	private void ShowMainWindow()
	{
		Show();
		WindowState = WindowState.Normal;
		Activate();
	}

	private void Exit_Click(object sender, RoutedEventArgs e)
	{
		_isExiting = true;
		Close();
	}
}
