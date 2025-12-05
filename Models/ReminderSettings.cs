using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace StandUpReminder.Models;

public partial class ReminderSettings : ObservableObject
{
	[ObservableProperty]
	private TimeSpan _startTime = new TimeSpan(9, 0, 0);

	[ObservableProperty]
	private TimeSpan _endTime = new TimeSpan(18, 0, 0);

	[ObservableProperty]
	private int _intervalMinutes = 45;

	[ObservableProperty]
	private int _notificationDurationMinutes = 60;

	[ObservableProperty]
	private bool _isEnabled = true;

	[ObservableProperty]
	private bool _playSoundOnNotification = true;

	[ObservableProperty]
	private bool _autoStartWithWindows = false;

	[ObservableProperty]
	private int _todayCompletedCount = 0;

	[ObservableProperty]
	private DateTime _lastResetDate = DateTime.Today;

	// 新增：保存当天的站立记录（只做存储，不做 UI 绑定）
	public List<string> StandRecords { get; set; } = new List<string>();

	// 新增：这些记录对应的日期，用于判断是否属于“今天”
	public DateTime StandRecordsDate { get; set; } = DateTime.Today;

	public ReminderSettings Clone()
	{
		return new ReminderSettings
		{
			StartTime = this.StartTime,
			EndTime = this.EndTime,
			IntervalMinutes = this.IntervalMinutes,
			NotificationDurationMinutes = this.NotificationDurationMinutes,
			IsEnabled = this.IsEnabled,
			PlaySoundOnNotification = this.PlaySoundOnNotification,
			AutoStartWithWindows = this.AutoStartWithWindows,
			TodayCompletedCount = this.TodayCompletedCount,
			LastResetDate = this.LastResetDate
		};
	}
}
