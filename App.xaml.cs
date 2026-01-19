using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Data;
using System.Windows.Interop;

namespace StandUpReminder;

public partial class App : Application
{
	private const string MutexName = "StandUpReminderApp";
	private const int WM_SHOW_APP = 0x0400 + 1;

	private Mutex _mutex;

	protected override void OnStartup(StartupEventArgs e)
	{
		// ⭐ 关键：阻止 WPF 自动创建窗口
		ShutdownMode = ShutdownMode.OnExplicitShutdown;

		_mutex = new Mutex(true, MutexName, out bool createdNew);

		if (!createdNew)
		{
			// 第二个实例：只负责唤醒已有窗口
			SendShowMessageToRunningInstance();
			Shutdown();
			return;
		}

		// 第一个实例：注册消息
		ComponentDispatcher.ThreadPreprocessMessage += ThreadPreprocessMessageMethod;

		base.OnStartup(e);

		// ⭐ 手动创建主窗口（此时才会出现在任务栏）
		MainWindow = new MainWindow();
		MainWindow.Show();

		// 恢复正常关闭模式
		ShutdownMode = ShutdownMode.OnMainWindowClose;
	}

	protected override void OnExit(ExitEventArgs e)
	{
		_mutex?.ReleaseMutex();
		_mutex?.Dispose();
		base.OnExit(e);
	}

	private void ThreadPreprocessMessageMethod(ref MSG msg, ref bool handled)
	{
		if (msg.message == WM_SHOW_APP)
		{
			ShowMainWindow();
			handled = true;
		}
	}

	private void ShowMainWindow()
	{
		if (MainWindow == null)
			return;

		MainWindow.Dispatcher.Invoke(() =>
		{
			MainWindow.Show();
			MainWindow.WindowState = WindowState.Normal;
			MainWindow.Activate();
			MainWindow.Topmost = true;
			MainWindow.Topmost = false;
			MainWindow.Focus();
		});
	}

	private void SendShowMessageToRunningInstance()
	{
		IntPtr hwnd = FindWindow(null, "站立提醒助手");
		if (hwnd != IntPtr.Zero)
		{
			PostMessage(hwnd, WM_SHOW_APP, IntPtr.Zero, IntPtr.Zero);
		}
	}

	[DllImport("user32.dll")]
	private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

	[DllImport("user32.dll")]
	private static extern bool PostMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);
}
// 反转布尔转换器
public class InverseBoolConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return value is bool b && !b;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return value is bool b && !b;
	}
}

public class CountToVisibilityConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		int count = 0;

		// 获取集合数量（从 int 或可迭代对象）
		if (value is int intValue)
		{
			count = intValue;
		}
		else if (value is System.Collections.ICollection collection)
		{
			count = collection.Count;
		}

		bool inverse = parameter != null && parameter.ToString().Equals("inverse", StringComparison.OrdinalIgnoreCase);

		// 正常模式：count > 0 就显示，count == 0 隐藏
		// inverse 模式：count == 0 显示，count > 0 隐藏
		bool isVisible = count > 0;

		if (inverse)
			isVisible = !isVisible;

		return isVisible ? Visibility.Visible : Visibility.Collapsed;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
