using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace StandUpReminder;

public partial class App : Application
{
	protected override void OnStartup(StartupEventArgs e)
	{
		base.OnStartup(e);

		// 确保只运行一个实例
		var mutex = new System.Threading.Mutex(true, "StandUpReminderApp", out bool createdNew);
		if (!createdNew)
		{
			MessageBox.Show("程序已在运行中！", "站立提醒助手", MessageBoxButton.OK, MessageBoxImage.Information);
			Current.Shutdown();
			return;
		}
	}
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
