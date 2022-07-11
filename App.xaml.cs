using HeroSlidebarTranslator.Properties;
using Microsoft.Win32.TaskScheduler;
using SharpDX.XInput;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AppResources = HeroSlidebarTranslator.Properties.Resources;
using WpfNotify = Hardcodet.Wpf.TaskbarNotification;

namespace HeroSlidebarTranslator
{
	/// <summary>
	/// Interaktionslogik für "App.xaml"
	/// </summary>
	public partial class App : Application
	{
		public static new App Current { get => (App)Application.Current; }

		public static string FullPath => new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath;

		public static ObservableCollection<Slidebar> OCSlidebarList { get; set; }

		private static WpfNotify.TaskbarIcon TrayTaskbaricon { get; } = new() { Visibility = Visibility.Visible, Icon = AppResources.heroslidebartranslator_normal };

		public bool? ActivationModeFore { get; set; } = false;
		public bool? ActivationModeRunn { get; set; } = false;

		public System.Timers.Timer TimerRefreshApp = new(1000) { Enabled = true };

		public static CultureInfo DefaultCulture = new("en");

		public static ActivationModes ActivationMode { get; set; } = ActivationModes.GameForeground;

		public static bool TranslatorsMasterEnable { get; set; } = false;

		public static bool BattAlertsEnabled { get; set; } = true;

		private static bool _AutoStartEnabled = false;
		public static bool AutoStartEnabled
		{
			get => _AutoStartEnabled;
			set
			{ _AutoStartEnabled = value; SetAutostart(value); }
		}
		public const string AutostartTaskName = "HeroSlidebarTranslator Autostart";

		protected Process[] ProcCH = Process.GetProcessesByName(CHProcName);
		public const string CHProcName = "Clone Hero";

		public static bool _ThisIsNotUserInput = false;

		public static event EventHandler RefreshAppNow;


		enum ExitCodes : int
		{
			Default = 0b0000,
			TrayRequested = 0b0010,
			WindowRequested = 0b0100,
			InstanceDuplicate = 0b1000
		}

		public enum ActivationModes
		{
			GameForeground = 0, GameRunning = 1, Always = 2
		}


		public App()
		{
			JsonSettingsManager.Initialize("appsettings.json");

			// Load global settings:
			string cult;
			JsonSettingsManager.GetSetting(AppSettingNames.Culture, out cult, "en");
			ChangeCulture(new CultureInfo(cult));
			bool ase;
			JsonSettingsManager.GetSetting(AppSettingNames.Autostart, out ase, false);
			AutoStartEnabled = ase;
			CheckAutostartEnabled();
			bool ba;
			JsonSettingsManager.GetSetting(AppSettingNames.BattAlerts, out ba, false);
			BattAlertsEnabled = ba;
			ActivationModes am;
			JsonSettingsManager.GetSetting(AppSettingNames.ActivationMode, out am, ActivationModes.GameForeground);
			ActivationMode = am;
			ActivationModeFore = am == ActivationModes.GameForeground;
			ActivationModeRunn = am == ActivationModes.GameRunning;

			try
			{
				SlidebarList.SetSlidebar(UserIndex.One, new(new(UserIndex.One)));
				SlidebarList.SetSlidebar(UserIndex.Two, new(new(UserIndex.Two)));
				SlidebarList.SetSlidebar(UserIndex.Three, new(new(UserIndex.Three)));
				SlidebarList.SetSlidebar(UserIndex.Four, new(new(UserIndex.Four)));

				OCSlidebarList = new()
				{
					SlidebarList.Itm[UserIndex.One],
					SlidebarList.Itm[UserIndex.Two],
					SlidebarList.Itm[UserIndex.Three],
					SlidebarList.Itm[UserIndex.Four]
				};

				foreach (Slidebar slidebar in SlidebarList.Itm.Values)
				{ slidebar.Controller.LowBattWarning += BattWarningIssuedHandler; }
			}
			catch (Exception ex)
			{ MessageBox.Show("Could not create list of controllers or slidebar handles. \n\n" + ex.Message); };

			LoadPlayerFileSettings();

			TrayTaskbaricon.NoLeftClickDelay = true;
			TrayTaskbaricon.LeftClickCommand = new RelayCommand(delegate (object param) { TrayTaskbaricon.ContextMenu.IsOpen = true; });
			TrayTaskbaricon.DoubleClickCommand = new RelayCommand(delegate (object param) { OpenSetupWindow(); });



			RefreshTraymenuTranslate();

			TimerRefreshApp.Elapsed += new ElapsedEventHandler(TimerRefreshApp_Elapsed);
		}


		private void Application_LoadCompleted(object sender, System.Windows.Navigation.NavigationEventArgs e)
		{

		}

		protected override void OnStartup(StartupEventArgs e)
		{
			//Mutex mit eindeutigem Namen (bspw. GUID)
			Mutex mutex = new Mutex(true, "{a0cd481a-0aad-4eb6-bda0-e36928f2c32a}");

			//Prüfung, ob Mutex schon länger aktiv ist..
			if (mutex.WaitOne(TimeSpan.Zero, true))
			{
				mutex.ReleaseMutex();
				//Mutex ist gerade gestartet..
				base.OnStartup(e);
			}
			else
			{
				//Mutex läuft bereits längere Zeit..
				Environment.Exit(exitCode: (int)ExitCodes.InstanceDuplicate);
			}

		}

		public void App_Startup(object sender, StartupEventArgs e)
		{
			// Application is running
			// Process command line args
			bool startInTray = false;
			for (int i = 0; i != e.Args.Length; ++i)
			{
				if (e.Args[i] is "/tray" or "/minimized")
				{
					startInTray = true;
				}
			}

			// Initializing TrayIcon
			TrayTaskbaricon.ContextMenu = (ContextMenu)TryFindResource("ContextmenuTray") ?? new ContextMenu();
			TrayTaskbaricon.ContextMenu.DataContext = Current;

			// Start main window in tray if specified
			MainWindow = new SetupWindow();
			if (startInTray)
			{
				MainWindow.WindowState = WindowState.Minimized;
				MainWindow.ShowInTaskbar = false;
			}

			XController.OnConnectionChange propChanged = (MainWindow as SetupWindow).RefreshControllerList;
			foreach (Slidebar slidebar in OCSlidebarList)
			{
				slidebar.Controller.AttachOnConnectionChangedHandler(propChanged);
			}

			// TODO: Handler hier nötig?
			// TimerRefreshControllers.Elapsed += new ElapsedEventHandler(TimerRefreshControllers_Elapsed);
			MainWindow.Show();
		}

		public void App_Exit(object sender, ExitEventArgs e)
		{
			SaveAllFileSettings();
			TrayTaskbaricon.Visibility = Visibility.Collapsed;
		}


		public void TimerRefreshApp_Elapsed(object sender, EventArgs e)
		{
			SlidebarList.RefreshCtrlrs();

			if (ProcCH.Length == 0 || ProcCH[0].HasExited) ProcCH = Process.GetProcessesByName(CHProcName);
			bool masterenable = false;

			switch (ActivationMode)
			{
				case ActivationModes.GameForeground:
					masterenable = ProcCH.Length > 0 && IsForegroundProcess((uint)ProcCH[0].Id);
					break;
				case ActivationModes.GameRunning:
					masterenable = ProcCH.Length > 0;
					break;
				case ActivationModes.Always:
					// Deregister handler because always active,
					// registered again when other value selected in ComboBox
					TimerRefreshApp.Elapsed -= TimerRefreshApp_Elapsed;
					masterenable = true;
					break;
			}
			TranslatorsMasterEnable = masterenable;
			SlidebarList.TurnAllOnOff(masterenable);
			Current.RefreshTraymenuTranslate();
		}


		public void OpenSetupWindow()
		{
			MainWindow.ShowInTaskbar = true;
			MainWindow.WindowState = WindowState.Normal;
			MainWindow.Show(); MainWindow.Activate();
		}

		public void RefreshTraymenuTranslate()
		{
			bool anyActive = false;
			foreach (Slidebar bar in SlidebarList.Itm.Values)
			{
				if (bar.TranslateActive) anyActive = true;
			}
			if (anyActive) TrayTaskbaricon.Icon = AppResources.heroslidebartranslator_active;
			else TrayTaskbaricon.Icon = AppResources.heroslidebartranslator_normal;
		}
		public void RefreshTraymenuControllers()
		{

		}

		public static void BattWarningIssuedHandler(object sender, BatteryEventArgs e)
		{
			if (BattAlertsEnabled)
			{
				XController controller = (sender as XController);
				TrayTaskbaricon.ShowBalloonTip(AppResources.TextF_LowBattWarning_Caption, string.Format(AppResources.TextF_LowBattWarning, controller.PlayerDisplayName + " " + controller.ControllerSubType), WpfNotify.BalloonIcon.Warning);
			}
		}

		public static void ChangeActivationMode(ActivationModes newmode)
		{
			// From Always to other mode:
			if (newmode != ActivationModes.Always && ActivationMode == ActivationModes.Always)
			{
				// Unsubscibe event handle cause its not needed:
				Current.TimerRefreshApp.Elapsed -= Current.TimerRefreshApp_Elapsed;
				Current.TimerRefreshApp.Elapsed += Current.TimerRefreshApp_Elapsed;
			}
			// From other mode to Always handled in Elapsed handler
			ActivationMode = newmode;
		}

		#region Menuitem event handles

		private void MenuitemCloseApp_Click(object sender, RoutedEventArgs e)
		{
			Application.Current.Shutdown((int)ExitCodes.TrayRequested);
		}

		private void MenuitemRunStartup_CheckedChanged(object sender, RoutedEventArgs e)
		{
			AutoStartEnabled = (sender as MenuItem)?.IsChecked ?? false;
		}

		private void MenuitemOpenSetup_Click(object sender, RoutedEventArgs e)
		{
			OpenSetupWindow();
		}

		private void MenuitemActivtnMode_Checked(object sender, RoutedEventArgs e)
		{
			if (!_ThisIsNotUserInput)
			{
				MenuItem menuItem = (sender as MenuItem) ?? new MenuItem();
				switch (menuItem.Tag.ToString().ToUpper())
				{
					case "RUNN": ChangeActivationMode(ActivationModes.GameRunning); break;
					case "ALWA": ChangeActivationMode(ActivationModes.Always); break;
					default: ChangeActivationMode(ActivationModes.GameForeground); break; // default includes "FORE"
				}
				RefreshAppNow?.Invoke(this, EventArgs.Empty);
				menuItem.IsChecked = true;
			}
		}

		#endregion


		public static void CheckAutostartEnabled()
		{
			TaskService tsvc = new TaskService();
			var task = tsvc.FindTask(AutostartTaskName);
			if (task is not null) _AutoStartEnabled = task.Enabled;
			else _AutoStartEnabled = false;
		}

		public void LoadPlayerFileSettings()
		{
			JsonSettingsManager.GetSetting("Player1", out SlidebarList.Itm[UserIndex.One].Settings, new());
			JsonSettingsManager.GetSetting("Player2", out SlidebarList.Itm[UserIndex.Two].Settings, new());
			JsonSettingsManager.GetSetting("Player3", out SlidebarList.Itm[UserIndex.Three].Settings, new());
			JsonSettingsManager.GetSetting("Player4", out SlidebarList.Itm[UserIndex.Four].Settings, new());
		}

		public static void SaveAllFileSettings()
		{
			JsonSettingsManager.AddSetting(AppSettingNames.Autostart, AutoStartEnabled);
			JsonSettingsManager.AddSetting(AppSettingNames.ActivationMode, ActivationMode);
			JsonSettingsManager.AddSetting(AppSettingNames.BattAlerts, BattAlertsEnabled);
			JsonSettingsManager.AddSetting(AppSettingNames.Culture, DefaultCulture.TwoLetterISOLanguageName);

			JsonSettingsManager.AddSetting("Player1", SlidebarList.Itm[UserIndex.One].Settings);
			JsonSettingsManager.AddSetting("Player2", SlidebarList.Itm[UserIndex.Two].Settings);
			JsonSettingsManager.AddSetting("Player3", SlidebarList.Itm[UserIndex.Three].Settings);
			JsonSettingsManager.AddSetting("Player4", SlidebarList.Itm[UserIndex.Four].Settings);

			JsonSettingsManager.SaveSettings();
		}

		public static bool ChangeCulture(CultureInfo culture)
		{
			Thread.CurrentThread.CurrentCulture = culture;
			Thread.CurrentThread.CurrentUICulture = culture;
			AppResources.Culture = culture;

			DefaultCulture = culture;

			JsonSettingsManager.AddSetting(AppSettingNames.Culture, culture.TwoLetterISOLanguageName);

			return true;
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="register">Pass true to register the autostart or false to delete the task.</param>
		/// <returns>Register / Unregister successful.</returns>
		private static bool SetAutostart(bool register = true)
		{
			TaskService tksvc = new TaskService();
			TaskDefinition tkdef = tksvc.NewTask();
			if (register)
			{
				try
				{
					tkdef.RegistrationInfo.Author = "HeroSlidebarTranslator";
					tkdef.RegistrationInfo.Description = "Starts HeroSlidebarTranslator in the tray when the user logs in.";
					tkdef.Triggers.Add(new LogonTrigger { UserId = Environment.UserDomainName + "\\" + Environment.UserName, Delay = new(0, 0, 1), Enabled = true });

					if (!string.IsNullOrWhiteSpace(FullPath) && File.Exists(FullPath))
					{
						ExecAction exa = new ExecAction(FullPath, "/tray", null);
						tkdef.Actions.Add(exa);
					}
					else { return false; }

					tksvc.RootFolder.RegisterTaskDefinition(AutostartTaskName, tkdef);
				}
				catch (Exception ex)
				{
					MessageBox.Show("Unable to register the autostart task in the task scheduler. \n\n" + ex.Message, "Failed to register task", MessageBoxButton.OK, MessageBoxImage.Error);
				}
				return true;
			}
			else if (!register)
			{
				try { tksvc.RootFolder.DeleteTask(AutostartTaskName); }
				catch { return false; }
				return true;
			}
			return false;
		}

		[DllImport("user32.dll")]
		private static extern IntPtr GetForegroundWindow();

		[DllImport("user32.dll", SetLastError = true)]
		static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

		bool IsForegroundProcess(uint pid)
		{
			IntPtr hwnd = GetForegroundWindow();
			if (hwnd == null) return false;

			uint foregroundPid;
			if (GetWindowThreadProcessId(hwnd, out foregroundPid) == 0) return false;

			return (foregroundPid == pid);
		}

	}


	public class ComboBoxValueItem : ComboBoxItem
	{
		public int Value { get; set; }

		public ComboBoxValueItem() : base() { }
		public ComboBoxValueItem(object content, int value) : base() { Content = content; Value = value; }


		public override string ToString()
		{
			return base.ToString();
		}
	}


	public static class AppSettingNames
	{
		public const string ActivationMode = "ActivationMode";
		public const string Autostart = "Autostart";
		public const string BattAlerts = "BattAlerts";
		public const string Culture = "Culture";
	}


	#region COMMAND CLASSES

	/// <summary>
	/// A command whose sole purpose is to 
	/// relay its functionality to other
	/// objects by invoking delegates. The
	/// default return value for the CanExecute
	/// method is 'true'.
	/// </summary>
	public class RelayCommand : ICommand
	{
		#region Fields

		readonly Action<object> _execute;
		readonly Predicate<object> _canExecute;

		#endregion // Fields

		#region Constructors

		/// <summary>
		/// Creates a new command that can always execute.
		/// </summary>
		/// <param name="execute">The execution logic.</param>
		public RelayCommand(Action<object> execute)
			: this(execute, null)
		{
		}

		/// <summary>
		/// Creates a new command.
		/// </summary>
		/// <param name="execute">The execution logic.</param>
		/// <param name="canExecute">The execution status logic.</param>
		public RelayCommand(Action<object> execute, Predicate<object> canExecute)
		{
			if (execute == null)
				throw new ArgumentNullException("execute");

			_execute = execute;
			_canExecute = canExecute;
		}

		#endregion // Constructors

		#region ICommand Members

		[DebuggerStepThrough]
		public bool CanExecute(object parameters)
		{
			return _canExecute == null || _canExecute(parameters);
		}

		public event EventHandler CanExecuteChanged
		{
			add { CommandManager.RequerySuggested += value; }
			remove { CommandManager.RequerySuggested -= value; }
		}

		public void Execute(object parameters)
		{
			_execute(parameters);
		}

		#endregion // ICommand Members
	}
	#endregion

	#region MenuItemExtension

	public class MenuItemExtensions : DependencyObject
	{
		public static Dictionary<MenuItem, string> ElementToGroupNames = new Dictionary<MenuItem, string>();

		public static readonly DependencyProperty GroupNameProperty =
			DependencyProperty.RegisterAttached("GroupName", typeof(string), typeof(MenuItemExtensions), new PropertyMetadata(string.Empty, OnGroupNameChanged));

		public static void SetGroupName(MenuItem element, String value)
		{
			element.SetValue(GroupNameProperty, value);
		}

		public static String GetGroupName(MenuItem element)
		{
			return element.GetValue(GroupNameProperty).ToString();
		}

		private static void OnGroupNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			//Add an entry to the group name collection
			var menuItem = d as MenuItem;

			if (menuItem != null)
			{
				string newGroupName = e.NewValue.ToString();
				string oldGroupName = e.OldValue.ToString();
				if (string.IsNullOrEmpty(newGroupName))
				{
					//Removing the toggle button from grouping
					RemoveCheckboxFromGrouping(menuItem);
				}
				else
				{
					//Switching to a new group
					if (newGroupName != oldGroupName)
					{
						if (!string.IsNullOrEmpty(oldGroupName))
						{
							//Remove the old group mapping
							RemoveCheckboxFromGrouping(menuItem);
						}
						ElementToGroupNames.Add(menuItem, e.NewValue.ToString());
						menuItem.Checked += MenuItemChecked;
					}
				}
			}
		}

		private static void RemoveCheckboxFromGrouping(MenuItem checkBox)
		{
			ElementToGroupNames.Remove(checkBox);
			checkBox.Checked -= MenuItemChecked;
		}


		static void MenuItemChecked(object sender, RoutedEventArgs e)
		{
			var menuItem = e.OriginalSource as MenuItem;
			foreach (var item in ElementToGroupNames)
			{
				if (item.Key != menuItem && item.Value == GetGroupName(menuItem))
				{
					item.Key.IsChecked = false;
				}
			}
		}
	}

	#endregion

	#region EXTENSIONS

	public static partial class ExtensionMethods
	{
		public static decimal Map(this decimal value, decimal fromSource, decimal toSource, decimal fromTarget, decimal toTarget)
		{
			return ((value - fromSource) / (toSource - fromSource) * (toTarget - fromTarget)) + fromTarget;
		}

	}

	#endregion


}
