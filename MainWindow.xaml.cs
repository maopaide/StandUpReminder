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
			//ShowTimedBalloonTip("站立提醒助手", "程序已最小化到系统托盘", 3000);
		}
	}

	private void Window_Closing(object sender, CancelEventArgs e)
	{
		if (!_isExiting)
		{
			e.Cancel = true;
			Hide();
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

	// === 新增：编辑记录时间时暂停实时刷新，避免覆盖输入 ===
	private void RecordTimeBox_GotFocus(object sender, RoutedEventArgs e)
	{
		if (DataContext is MainViewModel vm)
			vm.IsEditingRecordTime = true;
	}

	// === 新增：结束编辑后恢复实时刷新，并把单个数字补齐两位 ===
	private void RecordTimeBox_LostFocus(object sender, RoutedEventArgs e)
	{
		if (DataContext is not MainViewModel vm) return;

		vm.IsEditingRecordTime = false;

		// 失焦时把格式修正为两位数（并做范围夹取）
		if (!int.TryParse(vm.RecordHourText, out var hour)) hour = DateTime.Now.Hour;
		if (!int.TryParse(vm.RecordMinuteText, out var minute)) minute = DateTime.Now.Minute;

		hour = Math.Clamp(hour, 0, 23);
		minute = Math.Clamp(minute, 0, 59);

		vm.RecordHourText = hour.ToString("00");
		vm.RecordMinuteText = minute.ToString("00");
	}

}
