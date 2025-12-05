using Newtonsoft.Json;
using StandUpReminder.Models;
using System;
using System.IO;

namespace StandUpReminder.Services;

public class SettingsService
{
	private readonly string _settingsPath;

	public SettingsService()
	{
		var appDataPath = Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
			"StandUpReminder");

		Directory.CreateDirectory(appDataPath);
		_settingsPath = Path.Combine(appDataPath, "settings.json");
	}

	public ReminderSettings Load()
	{
		try
		{
			if (File.Exists(_settingsPath))
			{
				var json = File.ReadAllText(_settingsPath);
				var settings = JsonConvert.DeserializeObject<ReminderSettings>(json);
				return settings ?? new ReminderSettings();
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"加载设置失败: {ex.Message}");
		}

		return new ReminderSettings();
	}

	public void Save(ReminderSettings settings)
	{
		try
		{
			var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
			File.WriteAllText(_settingsPath, json);
		}
		catch (Exception ex)
		{
			Console.WriteLine($"保存设置失败: {ex.Message}");
		}
	}
}
