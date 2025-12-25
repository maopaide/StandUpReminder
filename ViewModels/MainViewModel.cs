using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StandUpReminder.Models;
using StandUpReminder.Services;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Linq;

namespace StandUpReminder.ViewModels;

public partial class MainViewModel : ObservableObject
{
	private readonly SettingsService _settingsService;
	private readonly NotificationService _notificationService;
	private readonly ReminderService _reminderService;

	[ObservableProperty]
	private ReminderSettings _settings;

	[ObservableProperty]
	private string _status = "已停止";

	[ObservableProperty]
	private string _nextReminderText = "无";

	[ObservableProperty]
	private ObservableCollection<string> _todaySchedule = new();

	// 时间选择器绑定属性
	[ObservableProperty]
	private int _startHour;

	[ObservableProperty]
	private int _startMinute;

	[ObservableProperty]
	private int _endHour;

	[ObservableProperty]
	private int _endMinute;

	// === 新增：手动记录时间输入框绑定 ===
	[ObservableProperty]
	private int _recordHour;

	[ObservableProperty]
	private int _recordMinute;

	// === 新增：手动记录时间（两位数显示） ===
	[ObservableProperty]
	private string _recordHourText = "00";

	[ObservableProperty]
	private string _recordMinuteText = "00";

	// === 新增：实时刷新输入框时间（默认开） ===
	[ObservableProperty]
	private bool _isRecordTimeLive = true;

	// === 新增：用于避免编辑时被覆盖 ===
	[ObservableProperty]
	private bool _isEditingRecordTime;



	public ObservableCollection<int> Hours { get; } = new();
	public ObservableCollection<int> Minutes { get; } = new();
	public ObservableCollection<int> IntervalOptions { get; } = new()
	{
		15, 20, 25, 30, 35, 40, 45, 50, 55, 60, 75, 90, 120
	};

	private ObservableCollection<string> _standRecords = new ObservableCollection<string>();
	public ObservableCollection<string> StandRecords
	{
		get => _standRecords;
		set => SetProperty(ref _standRecords, value);
	}


	public MainViewModel()
	{
		_settingsService = new SettingsService();
		_notificationService = new NotificationService();
		_settings = _settingsService.Load();

		// ===== 新增：根据日期恢复或清空站立记录 =====
		if (_settings.StandRecordsDate.Date == DateTime.Today)
		{
			// 同一天：把存储中的记录恢复到可绑定的 StandRecords 集合
			if (_settings.StandRecords != null)
			{
				foreach (var record in _settings.StandRecords)
				{
					StandRecords.Add(record);
				}
			}
		}
		else
		{
			// 不是今天：清空旧记录，并把日期更新为今天
			_settings.StandRecordsDate = DateTime.Today;
			_settings.StandRecords = new List<string>();
		}
		

		_reminderService = new ReminderService(_notificationService, _settings);

		for (int i = 0; i < 24; i++) Hours.Add(i);
		for (int i = 0; i < 60; i += 5) Minutes.Add(i);
		Minutes.Insert(0, 0);

		StartHour = Settings.StartTime.Hours;
		StartMinute = Settings.StartTime.Minutes;
		EndHour = Settings.EndTime.Hours;
		EndMinute = Settings.EndTime.Minutes;
		// === 新增：默认记录时间为当前时间 ===
		RecordHour = DateTime.Now.Hour;
		RecordMinute = DateTime.Now.Minute;

		// === 新增：初始化记录时间为当前时间（两位数） ===
		SetRecordTimeToNow();


		_reminderService.StatusChanged += (s, status) =>
		{
			Application.Current.Dispatcher.Invoke(() => Status = status);
		};

		_notificationService.StandUpCompleted += (s, e) =>
		{
			Application.Current.Dispatcher.Invoke(() =>
			{
				RecordStandUp(); 
			});
		};

		// 新增：每日重置时清空站立记录
		_reminderService.DailyReset += (s, e) =>
		{
			Application.Current.Dispatcher.Invoke(() =>
			{
				StandRecords.Clear();
			});
		};

		// 启动定时更新
		var timer = new System.Windows.Threading.DispatcherTimer
		{
			Interval = TimeSpan.FromSeconds(1)
		};
		timer.Tick += (s, e) =>
		{
			UpdateNextReminderText();

			// === 新增：实时刷新“记录时间”输入框 ===
			if (IsRecordTimeLive && !IsEditingRecordTime)
			{
				SetRecordTimeToNow();
			}
		};

		timer.Start();

		// 自动启动
		if (Settings.IsEnabled)
		{
			StartReminder();
		}

		UpdateTodaySchedule();
	}

	partial void OnStartHourChanged(int value) => UpdateStartTime();
	partial void OnStartMinuteChanged(int value) => UpdateStartTime();
	partial void OnEndHourChanged(int value) => UpdateEndTime();
	partial void OnEndMinuteChanged(int value) => UpdateEndTime();

	private void UpdateStartTime()
	{
		Settings.StartTime = new TimeSpan(StartHour, StartMinute, 0);
		OnSettingsChanged();
	}

	private void UpdateEndTime()
	{
		Settings.EndTime = new TimeSpan(EndHour, EndMinute, 0);
		OnSettingsChanged();
	}

	private void OnSettingsChanged()
	{
		_reminderService.UpdateSettings(Settings);
		UpdateTodaySchedule();
		SaveSettings();
	}

	[RelayCommand]
	private void ToggleReminder()
	{
		Settings.IsEnabled = !Settings.IsEnabled;

		if (Settings.IsEnabled)
		{
			StartReminder();
		}
		else
		{
			StopReminder();
		}

		SaveSettings();
	}

	[RelayCommand]
	private void StartReminder()
	{
		Settings.IsEnabled = true;
		_reminderService.UpdateSettings(Settings);
		_reminderService.Start();
		UpdateTodaySchedule();
	}

	[RelayCommand]
	private void StopReminder()
	{
		Settings.IsEnabled = false;
		_reminderService.Stop();
		NextReminderText = "已暂停";
	}

	[RelayCommand]
	private void TestNotification()
	{
		_notificationService.ShowStandUpNotification(60);
	}

	[RelayCommand]
	private void CompleteStandUp()
	{
		//Settings.TodayCompletedCount++;
		//SaveSettings();
		//string standTime = DateTime.Now.ToString("HH:mm:ss");

		//_notificationService.ShowQuickNotification("👏 太棒了！",
			//$"今日已完成 {Settings.TodayCompletedCount} 次站立活动");

		RecordStandUp();
	}

	[RelayCommand]
	private void ResetTodayCount()
	{
		Settings.TodayCompletedCount = 0;
		StandRecords.Clear();
		SaveSettings();
	}

	private void UpdateNextReminderText()
	{
		if (!Settings.IsEnabled)
		{
			NextReminderText = "已暂停";
			return;
		}

		var nextTime = _reminderService.NextReminderTime;
		if (nextTime == null)
		{
			NextReminderText = "今日已结束";
			return;
		}

		var timeUntil = nextTime.Value - DateTime.Now;
		if (timeUntil.TotalSeconds < 0)
		{
			NextReminderText = "即将提醒...";
			return;
		}

		if (timeUntil.TotalHours >= 1)
		{
			NextReminderText = $"{nextTime:HH:mm} ({timeUntil.Hours}小时{timeUntil.Minutes}分钟后)";
		}
		else if (timeUntil.TotalMinutes >= 1)
		{
			NextReminderText = $"{nextTime:HH:mm} ({timeUntil.Minutes}分钟后)";
		}
		else
		{
			NextReminderText = $"{nextTime:HH:mm} ({timeUntil.Seconds}秒后)";
		}
	}

	private void UpdateTodaySchedule()
	{
		TodaySchedule.Clear();
		var times = _reminderService.GetTodayReminderTimes();
		var now = DateTime.Now.TimeOfDay;

		foreach (var time in times)
		{
			var status = time <= now ? "✅" : "⏳";
			TodaySchedule.Add($"{status} {time:hh\\:mm}");
		}

		if (TodaySchedule.Count == 0)
		{
			TodaySchedule.Add("今日无提醒安排");
		}
	}

	private void SaveSettings()
	{
		// 把当前 UI 中的站立记录写回设置对象
		Settings.StandRecords = StandRecords.ToList();
		Settings.StandRecordsDate = DateTime.Today;

		_settingsService.Save(Settings);
	}

	public void Cleanup()
	{
		_reminderService.Dispose();
		SaveSettings();
	}

	private void RecordStandUp()
	{
		Settings.TodayCompletedCount++;

		// === 修改：记录输入框中的时间，格式固定 HH:mm（24小时，两位数） ===
		if (!int.TryParse(RecordHourText, out var hour)) hour = DateTime.Now.Hour;
		if (!int.TryParse(RecordMinuteText, out var minute)) minute = DateTime.Now.Minute;

		hour = Math.Clamp(hour, 0, 23);
		minute = Math.Clamp(minute, 0, 59);

		var recorded = new TimeSpan(hour, minute, 0);
		StandRecords.Add(recorded.ToString(@"hh\:mm"));

		SaveSettings();

		_notificationService.ShowQuickNotification("👏 太棒了！",
			$"今日已完成 {Settings.TodayCompletedCount} 次站立活动");
	}


	private void SetRecordTimeToNow()
	{
		var now = DateTime.Now;
		RecordHourText = now.Hour.ToString("00");
		RecordMinuteText = now.Minute.ToString("00");
	}

}
