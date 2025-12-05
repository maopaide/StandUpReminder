using StandUpReminder.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using Timer = System.Timers.Timer;

namespace StandUpReminder.Services;

public class ReminderService : IDisposable
{
	private int _isProcessing = 0;
	private readonly NotificationService _notificationService;
	private readonly Timer _timer;
	private ReminderSettings _settings;
	private List<TimeSpan> _reminderTimes = new();
	private int _currentReminderIndex = 0;

	public event EventHandler<string>? StatusChanged;
	public event EventHandler? ReminderTriggered;
	public event EventHandler? DailyReset;

	public DateTime? NextReminderTime { get; private set; }

	public ReminderService(NotificationService notificationService, ReminderSettings settings)
	{
		_notificationService = notificationService;
		_settings = settings;

		_timer = new Timer(1000); // 每秒检查一次
		_timer.Elapsed += OnTimerElapsed;
	}

	public void UpdateSettings(ReminderSettings settings)
	{
		_settings = settings;
		CalculateReminderTimes();
		UpdateNextReminder();
	}

	public void Start()
	{
		if (!_settings.IsEnabled) return;

		CalculateReminderTimes();
		UpdateNextReminder();
		_timer.Start();

		StatusChanged?.Invoke(this, "运行中");
	}

	public void Stop()
	{
		_timer.Stop();
		NextReminderTime = null;
		StatusChanged?.Invoke(this, "已暂停");
	}

	private void CalculateReminderTimes()
	{
		_reminderTimes.Clear();

		if (_settings.IntervalMinutes <= 0) return;

		var currentTime = _settings.StartTime;
		while (currentTime <= _settings.EndTime)
		{
			_reminderTimes.Add(currentTime);
			currentTime = currentTime.Add(TimeSpan.FromMinutes(_settings.IntervalMinutes));
		}

		// 移除第一个时间点（开始时间本身不需要提醒）
		if (_reminderTimes.Count > 0 && _reminderTimes[0] == _settings.StartTime)
		{
			// 保留第一个，因为比如9:00开始，45分钟后是9:45
			// 实际上第一个提醒应该是 StartTime + Interval
			_reminderTimes = _reminderTimes.Skip(1).ToList();
		}

		// 重新计算：从开始时间加上间隔开始
		_reminderTimes.Clear();
		currentTime = _settings.StartTime.Add(TimeSpan.FromMinutes(_settings.IntervalMinutes));
		while (currentTime <= _settings.EndTime)
		{
			_reminderTimes.Add(currentTime);
			currentTime = currentTime.Add(TimeSpan.FromMinutes(_settings.IntervalMinutes));
		}
	}

	private void UpdateNextReminder()
	{
		if (_reminderTimes.Count == 0)
		{
			NextReminderTime = null;
			return;
		}

		var now = DateTime.Now.TimeOfDay;
		var today = DateTime.Today;

		// 找到下一个提醒时间
		var nextTime = _reminderTimes.FirstOrDefault(t => t > now);

		if (nextTime == default)
		{
			// 今天的提醒都已经过了，设置为明天的第一个
			if (_reminderTimes.Count > 0)
			{
				NextReminderTime = today.AddDays(1).Add(_reminderTimes[0]);
			}
		}
		else
		{
			NextReminderTime = today.Add(nextTime);
		}

		_currentReminderIndex = _reminderTimes.FindIndex(t => t > now);
		if (_currentReminderIndex == -1) _currentReminderIndex = 0;
	}

	private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
	{
		if (Interlocked.Exchange(ref _isProcessing, 1) == 1)
		{
			return;
		}

		try
		{
			if (!_settings.IsEnabled || NextReminderTime == null) return;

			var now = DateTime.Now;

			// 检查是否需要重置每日计数
			if (_settings.LastResetDate.Date < now.Date)
			{
				_settings.TodayCompletedCount = 0;
				_settings.LastResetDate = now.Date;

				// 通知外部：新的一天开始了
				DailyReset?.Invoke(this, EventArgs.Empty);
			}

			// 检查是否到达提醒时间
			if (now >= NextReminderTime && now < NextReminderTime.Value.AddSeconds(2))
			{
				TriggerReminder();
				UpdateNextReminder();
			}
		}
		finally
		{
			// 允许下一次 Tick 进入
			_isProcessing = 0;
		}
	}

	private void TriggerReminder()
	{
		System.Diagnostics.Debug.WriteLine($"[Reminder] Trigger at {DateTime.Now:HH:mm:ss.fff}");
		_notificationService.ShowStandUpNotification(_settings.NotificationDurationMinutes);
		ReminderTriggered?.Invoke(this, EventArgs.Empty);
	}

	public List<TimeSpan> GetTodayReminderTimes()
	{
		CalculateReminderTimes();
		return _reminderTimes.ToList();
	}

	public void Dispose()
	{
		_timer.Stop();
		_timer.Dispose();
	}
}
