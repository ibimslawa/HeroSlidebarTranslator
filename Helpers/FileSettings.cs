using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;


namespace HeroSlidebarTranslator.Properties
{

	public static class JsonSettingsManager
	{
		private static string SettingsPath { get; set; }
		private static ReaderWriterLockSlim _RwLock { get; set; }

		private static JObject settings;

		public const int RwlTimeout = 5;

		public static void Initialize(string settingsPath)
		{
			_RwLock = new ReaderWriterLockSlim();
			if (!settingsPath.Contains("\\"))
				SettingsPath = Path.GetDirectoryName(App.FullPath) + "\\" + settingsPath;
			else
				SettingsPath = settingsPath;
			LoadSettings();
		}

		/// <summary>
		/// Creates a JSON file for settings at <see cref="SettingsPath"/>
		/// </summary>
		private static void CreateSettingsFile()
		{
			try
			{
				if (!File.Exists(SettingsPath))
				{
					Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath));
					File.Create(SettingsPath).Dispose();
				}
			}
			catch (Exception ex)
			{
				throw new FileNotFoundException($"The provided {nameof(SettingsPath)} was invalid", ex);
			}
		}

		/// <summary>
		/// Loads the contents of the settings file into <see cref="settings"/>
		/// </summary>
		private static void LoadSettings()
		{
			if (settings == null)
			{
				using (new WriteLock(_RwLock))
				{
					CreateSettingsFile();
					string fileContents = File.ReadAllText(SettingsPath);
					settings = string.IsNullOrWhiteSpace(fileContents) ? new JObject() : JObject.Parse(fileContents);
				}
			}
		}

		/// <summary>
		/// Add a new setting or update existing setting with the same name
		/// </summary>
		/// <param name="setting">Setting name</param>
		/// <param name="value">Setting value</param>
		public static void AddSetting(string setting, object value)
		{
			LoadSettings();
			using (new WriteLock(_RwLock))
			{
				if (settings[setting] == null && value != null)
					settings.Add(setting, JToken.FromObject(value));
				else if (value == null && settings[setting] != null)
					settings[setting] = null;
				else if (value != null)
					settings[setting] = JToken.FromObject(value);
			}
		}

		/// <summary>
		/// Adds a batch of new settings, replacing already existing values with the same name
		/// </summary>
		/// <param name="settings"></param>
		public static void AddSettings(Dictionary<string, object> settings)
		{
			foreach (var setting in settings)
			{
				AddSetting(setting.Key, setting.Value);
			}
		}

		/// <summary>
		/// Gets the value of the setting with the provided name as type <c>T</c>
		/// </summary>
		/// <typeparam name="T">Result type</typeparam>
		/// <param name="setting">Setting name</param>
		/// <param name="result">Result value</param>
		/// <param name="settingDefault">Result value</param>
		public static void GetSetting<T>(string setting, out T result, T settingDefault)
		{
			GetSetting(setting, out result);
			if (result == null) result = settingDefault;
		}

		/// <summary>
		/// Gets the value of the setting with the provided name as type <c>T</c>
		/// </summary>
		/// <typeparam name="T">Result type</typeparam>
		/// <param name="setting">Setting name</param>
		/// <param name="result">Result value</param>
		/// <returns>True when setting was successfully found, false when setting is not found</returns>
		public static bool GetSetting<T>(string setting, out T result)
		{
			result = default;

			try
			{
				LoadSettings();

				using (new ReadLock(_RwLock))
				{
					if (settings[setting] == null) return false;

					result = settings[setting].ToObject<T>();
				}
				return true;
			}
			catch (Exception)
			{
				try
				{
					using (new ReadLock(_RwLock))
					{
						result = settings[setting].Value<T>();
					}
					return true;
				}
				catch (Exception)
				{
					return false;
				}
			}
		}

		/// <summary>
		/// Returns a dictionary of all settings
		/// </summary>
		/// <returns></returns>
		public static bool GetSettings(Dictionary<string, object> values) => GetSettings(out values);

		internal static void AddSetting(string v, Action<bool> registerAutostart)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Returns a dictionary of all settings, where all settings are of the same type
		/// </summary>
		/// <typeparam name="TValue">The type of all of the settings</typeparam>
		/// <returns></returns>
		public static bool GetSettings<TValue>(out Dictionary<string, TValue> values)
		{
			try
			{
				LoadSettings();
				using (new ReadLock(_RwLock))
				{
					values = settings.ToObject<Dictionary<string, TValue>>();
					return true;
				}
			}
			catch
			{
				values = null;
				return false;
			}
		}

		/// <summary>
		/// Saves all queued settings to <see cref="SettingsPath"/>
		/// </summary>
		public static void SaveSettings()
		{
			using (new WriteLock(_RwLock))
			{
				File.WriteAllText(SettingsPath, settings.ToString(Formatting.Indented));
			}
		}


		/// <summary>
		/// Completely delete the settings file at <see cref="SettingsPath"/>. This action is irreversable.
		/// </summary>
		public static void DeleteSettings()
		{
			using (new WriteLock(_RwLock))
			{
				File.Delete(SettingsPath);
				settings = null;
			}
		}

	}


	public class ReadLock : IDisposable
	{
		static int readerTimeouts = 0;
		static int reads = 0;
		static ReaderWriterLockSlim rwl;
		public ReadLock(ReaderWriterLockSlim _rwl)
		{
			rwl = _rwl;
			try
			{
				rwl.EnterReadLock();
				try
				{
					Interlocked.Increment(ref reads);
				}
				finally { }
			}
			catch (ApplicationException)
			{
				// The reader lock request timed out.
				Interlocked.Increment(ref readerTimeouts);
			}
		}

		public void Dispose()
		{
			// Ensure that the lock is released.
			rwl.ExitReadLock();
		}
	}

	public class WriteLock : IDisposable
	{
		static int writerTimeouts = 0;
		static int writes = 0;
		static ReaderWriterLockSlim rwl;
		public WriteLock(ReaderWriterLockSlim _rwl)
		{
			rwl = _rwl;
			try
			{
				rwl.TryEnterWriteLock(JsonSettingsManager.RwlTimeout);
				try
				{
					// It's safe for this thread to access from the shared resource.
					Interlocked.Increment(ref writes);
				}
				finally { }
			}
			catch (ApplicationException)
			{
				// The writer lock request timed out.
				Interlocked.Increment(ref writerTimeouts);
			}
		}

		public void Dispose()
		{
			// Ensure that the lock is released.
			rwl.ExitWriteLock();
		}
	}

}