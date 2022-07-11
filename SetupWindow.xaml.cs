using SharpDX.XInput;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using static HeroSlidebarTranslator.App;
using MessageBox = System.Windows.MessageBox;

namespace HeroSlidebarTranslator
{


	/// <summary>
	/// Interaction logic for SetupWindow.xaml
	/// </summary>
	public partial class SetupWindow : Window, INotifyPropertyChanged
	{
		public UserIndex EditingUserIndex { get; set; }
		public ObservableCollection<Slidebar> OCSlidebarListBinding
		{
			get => OCSlidebarList;
			set { OCSlidebarList = value; OnPropertyChanged(); }
		}

		public SetupTranslPage PageP1 { get; set; }
		public SetupTranslPage PageP2 { get; set; }
		public SetupTranslPage PageP3 { get; set; }
		public SetupTranslPage PageP4 { get; set; }

		public SolidColorBrush Brush_ButtonHighlightBorder;
		public SolidColorBrush Brush_AppActive;
		public SolidColorBrush Brush_AppInactive;


		public bool old_TranslatorsMasterEnable = false;


		public SetupWindow()
		{
			LanguageProperty.OverrideMetadata(typeof(FrameworkElement), new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));

			EditingUserIndex = UserIndex.One;
			PageP1 = new SetupTranslPage(index: UserIndex.One, window: this);
			PageP2 = new SetupTranslPage(index: UserIndex.Two, window: this);
			PageP3 = new SetupTranslPage(index: UserIndex.Three, window: this);
			PageP4 = new SetupTranslPage(index: UserIndex.Four, window: this);

			InitializeComponent();

			// Set Label to current (old) culture
			LinklabelChangeLang.Content = CultureInfo.CurrentCulture.Name == "en" ? new CultureInfo("de").NativeName : new CultureInfo("en").NativeName;


			Brush_ButtonHighlightBorder = TryFindResource("Highlight1Brush") as SolidColorBrush ?? Brushes.White;
			Brush_AppActive = TryFindResource("AccentPaleBrush") as SolidColorBrush ?? Brushes.CornflowerBlue;
			Brush_AppInactive = TryFindResource("AppInactiveBrush") as SolidColorBrush ?? Brushes.DarkSlateGray;

			Current.TimerRefreshApp.Elapsed += TimerRefreshApp_Elapsed_MainWindow;
			RefreshAppNow += TimerRefreshApp_Elapsed_MainWindow;
		}

		#region EVENT HANDLERS

		public void RefreshControllerList()
		{
			Dispatcher.Invoke(() =>
			{
				ListboxControllers.Items.Refresh();
			});
		}


		private void SetupWindow_Loaded(object sender, RoutedEventArgs e)
		{
			ListboxControllers.SelectedIndex = 0;

			CheckboxBattAlerts.IsChecked = BattAlertsEnabled;
			CheckboxAutostartApp.IsChecked = AutoStartEnabled;
			ComboboxActivationMode.SelectedIndex = (int)ActivationMode;
		}

		private void SetupWindow_Deactivated(object sender, EventArgs e)
		{
			PageP1.AxisPreviewEnable = false;
			PageP2.AxisPreviewEnable = false;
			PageP3.AxisPreviewEnable = false;
			PageP4.AxisPreviewEnable = false;
		}
		private void SetupWindow_Closing(object sender, CancelEventArgs e)
		{
			WindowState = System.Windows.WindowState.Minimized;
			ShowInTaskbar = false;
			e.Cancel = true;
			SaveAllFileSettings();
		}

		private void SetupWindow_PreviewKeyDown(object sender, KeyEventArgs e)
		{
		}
		private void SetupWindow_PreviewKeyUp(object sender, KeyEventArgs e)
		{
		}

		private void ButtonQuitApp_Click(object sender, RoutedEventArgs e)
		{
			Application.Current.Shutdown();
		}

		private void ToggleTranslator_Checked(object sender, RoutedEventArgs e)
		{
			if (sender.GetType() == typeof(ToggleButton)) ((ToggleButton)sender).Content = "ACTIVE";
			SlidebarList.Itm[EditingUserIndex].TranslateEnable = true;
		}

		private void ToggleTranslator_Unchecked(object sender, RoutedEventArgs e)
		{
			if (sender.GetType() == typeof(ToggleButton)) ((ToggleButton)sender).Content = "OFF";
			SlidebarList.Itm[EditingUserIndex].TranslateEnable = false;
		}

		private void ComboboxActivationMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (!_ThisIsNotUserInput)
			{
				ActivationModes newmode = (ActivationModes)Enum.ToObject(typeof(ActivationModes), (sender as ComboBox).SelectedIndex);
				ChangeActivationMode(newmode);
			}
		}

		private void CheckboxAutostartApp_CheckedChanged(object sender, RoutedEventArgs e)
		{
			bool enable = CheckboxAutostartApp.IsChecked ?? false;
			AutoStartEnabled = enable;
			CheckAutostartEnabled();
		}

		private void CheckboxBattAlerts_CheckedChanged(object sender, RoutedEventArgs e)
		{
			BattAlertsEnabled = CheckboxBattAlerts.IsChecked ?? false;
		}

		private void LinklabelChangeLang_ClickLink(object sender, RoutedEventArgs e)
		{
			LinklabelChangeLang.Content = DefaultCulture.NativeName;
			CultureInfo newCult = DefaultCulture.Name == "en" ? new CultureInfo("de") : new CultureInfo("en");
			// Apply new culture
			ChangeCulture(newCult);

			if (MessageBox.Show(Properties.Resources.Text_RestartApp, Properties.Resources.Text_RestartApp, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
			{
				Process.Start(Application.ResourceAssembly.Location);
				Application.Current.Shutdown();
			}
		}

		private void ListboxControllers_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (IsInitialized)
			{
				_ThisIsNotUserInput = true;
				ListboxControllers.Items.Refresh();

				SetupTranslPage page = FrameSetupPane.Content as SetupTranslPage;
				// Stop axis preview for old page:
				if (page is not null) page.CheckboxAxisPreview.IsChecked = false;
				// Assign new page:
				switch (ListboxControllers.SelectedIndex)
				{
					case 0: page = PageP1; break;
					case 1: page = PageP2; break;
					case 2: page = PageP3; break;
					case 3: page = PageP4; break;
					default: page = null; break;
				}
				if (page is not null)
				{
					EditingUserIndex = page.AssgIndex;
					page.RefreshPageFromSettings();
				}
				FrameSetupPane.Content = page;

				_ThisIsNotUserInput = false;
			}
		}


		public void TimerRefreshApp_Elapsed_MainWindow(object sender, ElapsedEventArgs e)
		{
			TimerRefreshApp_Elapsed_MainWindow(sender, EventArgs.Empty);
		}
		public void TimerRefreshApp_Elapsed_MainWindow(object sender, EventArgs e)
		{
			_ThisIsNotUserInput = true;

			RefreshControllerList();

			bool test = TranslatorsMasterEnable != old_TranslatorsMasterEnable;
			if (TranslatorsMasterEnable != old_TranslatorsMasterEnable || sender is App ) // (sender is App) true when called from context menu
			{
				Dispatcher.Invoke(() =>
					{
						if (TranslatorsMasterEnable)
						{
							LabelActivity.Content = Properties.Resources.State_AppActivated;
							LabelActivity.Background = Brush_AppActive;
						}
						else
						{
							LabelActivity.Content = Properties.Resources.State_AppDeactivated;
							LabelActivity.Background = Brush_AppInactive;
						}
						ComboboxActivationMode.SelectedIndex = (int)ActivationMode;
					});
				
				old_TranslatorsMasterEnable = TranslatorsMasterEnable;
			}
			_ThisIsNotUserInput = false;
		}


		#endregion

		#region COMMANDS
		private void OpenCmdExecuted(object target, ExecutedRoutedEventArgs e)
		{
			string command, targetobj;
			command = ((RoutedCommand)e.Command).Name;
			targetobj = ((FrameworkElement)target).Name;
			_ = MessageBox.Show("The " + command + " command has been invoked on target object " + targetobj);
		}

		private void OpenCmdCanExecute(object sender, CanExecuteRoutedEventArgs e) { e.CanExecute = true; }

		#endregion


		public event PropertyChangedEventHandler PropertyChanged;
		protected void OnPropertyChanged([CallerMemberName] string new_Value = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(new_Value));
		}

	}

}
