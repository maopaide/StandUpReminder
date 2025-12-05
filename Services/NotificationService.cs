using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Threading.Tasks;
using Windows.UI.Notifications;

namespace StandUpReminder.Services;

public class NotificationService
{
	private const string NotificationTag = "StandUpReminder";
	private const string NotificationGroup = "StandUpGroup";

	public event EventHandler? StandUpCompleted;
	public event EventHandler? SnoozeRequested;

	private bool _isInitialized = false;

	public NotificationService()
	{
		InitializeNotificationHandling();
	}

	private void InitializeNotificationHandling()
	{
		if (_isInitialized) return;

		try
		{
			// 监听通知交互
			ToastNotificationManagerCompat.OnActivated += OnNotificationActivated;
			_isInitialized = true;
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"初始化通知处理失败: {ex.Message}");
		}
	}

	private void OnNotificationActivated(ToastNotificationActivatedEventArgsCompat e)
	{
		try
		{
			// 解析参数
			if (string.IsNullOrEmpty(e.Argument))
				return;

			var args = ToastArguments.Parse(e.Argument);

			if (args.TryGetValue("action", out string? action))
			{
				switch (action)
				{
					case "complete":
						System.Windows.Application.Current?.Dispatcher?.BeginInvoke(() =>
						{
							try
							{
								StandUpCompleted?.Invoke(this, EventArgs.Empty);
							}
							catch (Exception ex)
							{
								System.Diagnostics.Debug.WriteLine($"完成站立处理失败: {ex.Message}");
							}
						});
						break;

					case "snooze":
						// 延迟5分钟后再次提醒
						Task.Run(async () =>
						{
							await Task.Delay(TimeSpan.FromMinutes(5));
							System.Windows.Application.Current?.Dispatcher?.BeginInvoke(() =>
							{
								try
								{
									ShowStandUpNotification(5);
								}
								catch (Exception ex)
								{
									System.Diagnostics.Debug.WriteLine($"延迟提醒失败: {ex.Message}");
								}
							});
						});
						break;

					case "dismiss":
						// 忽略，不做任何处理
						break;

					case "viewReminder":
						// 点击通知主体，打开主窗口
						System.Windows.Application.Current?.Dispatcher?.BeginInvoke(() =>
						{
							try
							{
								var mainWindow = System.Windows.Application.Current?.MainWindow;
								if (mainWindow != null)
								{
									mainWindow.Show();
									mainWindow.WindowState = System.Windows.WindowState.Normal;
									mainWindow.Activate();
								}
							}
							catch (Exception ex)
							{
								System.Diagnostics.Debug.WriteLine($"打开主窗口失败: {ex.Message}");
							}
						});
						break;
				}
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"处理通知激活失败: {ex.Message}");
		}
	}

	public void ShowStandUpNotification(int durationMinutes)
	{
		try
		{
			// 清除之前的通知
			ClearNotifications();

			var expirationTime = DateTime.Now.AddMinutes(durationMinutes);

			var builder = new ToastContentBuilder()
				// 使用 Reminder 场景，通知会持续显示直到用户处理
				.SetToastScenario(ToastScenario.Reminder)

				.AddArgument("action", "viewReminder")
				.AddText("🧍 该站起来活动了！")
				.AddText("久坐对身体不好，站起来伸展一下吧！")
				.AddText("建议活动：伸展四肢、眺望远方、喝杯水")

				.AddButton(new ToastButton()
					.SetContent("✅ 已完成站立")
					.AddArgument("action", "complete")
					.SetBackgroundActivation())

				.AddButton(new ToastButton()
					.SetContent("⏰ 5分钟后")
					.AddArgument("action", "snooze")
					.SetBackgroundActivation())

				.AddButton(new ToastButton()
					.SetContent("忽略")
					.SetDismissActivation())

				.AddAudio(new ToastAudio()
				{
					Src = new Uri("ms-winsoundevent:Notification.Reminder")
				});

			var toastContent = builder.GetToastContent();
			var toast = new ToastNotification(toastContent.GetXml())
			{
				Tag = NotificationTag,
				Group = NotificationGroup,
				ExpirationTime = expirationTime,
				Priority = ToastNotificationPriority.High
			};

			// 添加失败处理
			toast.Failed += (s, args) =>
			{
				System.Diagnostics.Debug.WriteLine($"通知显示失败: {args.ErrorCode}");
			};

			ToastNotificationManagerCompat.CreateToastNotifier().Show(toast);
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"显示通知失败: {ex.Message}");
		}
	}

	public void ClearNotifications()
	{
		try
		{
			ToastNotificationManagerCompat.History.Remove(NotificationTag, NotificationGroup);
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"清除通知失败: {ex.Message}");
		}
	}

	public void ShowQuickNotification(string title, string message)
	{
		try
		{
			new ToastContentBuilder()
				.AddText(title)
				.AddText(message)
				.Show();
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"显示快速通知失败: {ex.Message}");
		}
	}
}
