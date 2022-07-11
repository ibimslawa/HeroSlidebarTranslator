using SharpDX.XInput;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using MessageBox = System.Windows.MessageBox;


namespace HeroSlidebarTranslator
{
	/// <summary>
	/// Interaktionslogik für SetupTranslatorPage.xaml
	/// </summary>
	public partial class SetupTranslPage : Page
	{
		public UserIndex AssgIndex { get; set; }

		public Slidebar GetSlidebar { get => SlidebarList.Itm[AssgIndex]; }

		public Frame ContainerFrame { get; private set; }

		private Timer TimerAxisPreview { get; } = new(50);

		public bool AxisPreviewEnable
		{
			get => TimerAxisPreview.Enabled;
			set { TimerAxisPreview.Enabled = value; LabelAxisPreview.IsEnabled = value; }
		}

		private SolidColorBrush ButtonHighlightBorderbrush;



		public SetupTranslPage(UserIndex index, Window window = null)
		{
			AssgIndex = index;
			DataContext = SlidebarList.Itm[AssgIndex];

			TimerAxisPreview.Elapsed += new ElapsedEventHandler(TimerAxisPreview_Elapsed);

			InitializeComponent();

			if (window is not null)
			{
				window.PreviewKeyDown -= SetupPage_PreviewKeyDown; window.PreviewKeyDown += SetupPage_PreviewKeyDown;
				window.PreviewKeyUp -= SetupPage_PreviewKeyUp; window.PreviewKeyUp += SetupPage_PreviewKeyUp;
			}

			ButtonHighlightBorderbrush = (SolidColorBrush)TryFindResource("Highlight1Brush") ?? Brushes.White;

			SlidebarList.Itm[AssgIndex].Controller.PropertyChanged += Controller_PropertyChanged;

		}

		public void UpdateAxisValueReading()
		{
			Dispatcher.Invoke(() =>
			{
				SliderSlidebarAxisPos.Value = SlidebarList.Itm[AssgIndex].GetAxisPos100();
				LabelAxisPreview.Content = SlidebarList.Itm[AssgIndex].GetAxisPos100() + "%";
			});
		}

		#region EVENT HANDLERS

		private void TimerAxisPreview_Elapsed(object sender, EventArgs e)
		{
			UpdateAxisValueReading();
		}


		private void SetupTranslatorPage_Loaded(object sender, RoutedEventArgs e)
		{
			App._ThisIsNotUserInput = false;
		}
		private void SetupTranslatorPage_Initialized(object sender, EventArgs e)
		{
		}

		private void SetupPage_LostFocus(object sender, RoutedEventArgs e)
		{

		}

		private void SetupPage_Closing(object sender, CancelEventArgs e)
		{
			TimerAxisPreview.Stop();
		}

		internal void SetupPage_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == ControllerGreKeybox.BoundKeyValued) { ControllerGreenBorder.BorderBrush = ButtonHighlightBorderbrush; }
			if (e.Key == ControllerRedKeybox.BoundKeyValued) { ControllerRedBorder.BorderBrush = ButtonHighlightBorderbrush; }
			if (e.Key == ControllerYelKeybox.BoundKeyValued) { ControllerYellowBorder.BorderBrush = ButtonHighlightBorderbrush; }
			if (e.Key == ControllerBluKeybox.BoundKeyValued) { ControllerBlueBorder.BorderBrush = ButtonHighlightBorderbrush; }
			if (e.Key == ControllerOraKeybox.BoundKeyValued) { ControllerOrangeBorder.BorderBrush = ButtonHighlightBorderbrush; }
		}
		internal void SetupPage_PreviewKeyUp(object sender, KeyEventArgs e)
		{
			if (e.Key == ControllerGreKeybox.BoundKeyValued) { ControllerGreenBorder.BorderBrush = Brushes.Transparent; }
			if (e.Key == ControllerRedKeybox.BoundKeyValued) { ControllerRedBorder.BorderBrush = Brushes.Transparent; }
			if (e.Key == ControllerYelKeybox.BoundKeyValued) { ControllerYellowBorder.BorderBrush = Brushes.Transparent; }
			if (e.Key == ControllerBluKeybox.BoundKeyValued) { ControllerBlueBorder.BorderBrush = Brushes.Transparent; }
			if (e.Key == ControllerOraKeybox.BoundKeyValued) { ControllerOrangeBorder.BorderBrush = Brushes.Transparent; }
		}


		private void ToggleTranslator_Checked(object sender, RoutedEventArgs e)
		{
			(sender as ToggleButton).Content = Properties.Resources.Symbol_Tick + " " + Properties.Resources.State_On;
			SlidebarList.Itm[AssgIndex].TranslateEnable = true;
			ToggleTranslator_CheckedChanged();
		}

		private void ToggleTranslator_Unchecked(object sender, RoutedEventArgs e)
		{
			(sender as ToggleButton).Content = Properties.Resources.Symbol_Cross + " " + Properties.Resources.State_Off;
			SlidebarList.Itm[AssgIndex].TranslateEnable = false;
			ToggleTranslator_CheckedChanged();
		}
		private void ToggleTranslator_CheckedChanged()
		{
			// Refresh list for the active tick
			(App.Current.MainWindow as SetupWindow)?.RefreshControllerList();
			// Refresh the icons
			App.Current.RefreshTraymenuTranslate();
		}

		private void CheckboxInvertAxis_CheckedChanged(object sender, RoutedEventArgs e)
		{
			GetSlidebar.InvertAxis = CheckboxInvertAxis.IsChecked ?? false;
		}

		private void ComboSelectAxis_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			SlidebarList.Itm[AssgIndex].SelectedAxis = (GamepadAxes)(ComboSelectAxis.SelectedIndex <= 5 ? ComboSelectAxis.SelectedIndex : 0);
		}

		private void ComboPollRate_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (ComboPollRate.IsDropDownOpen)
			{
				//OPTIONAL:
				//Causes the combobox selection changed to not be fired again if anything
				//in the function below changes the selection (as in my weird case)
				ComboPollRate.IsDropDownOpen = false;

				//now put the code you want to fire when a user selects an option here
				if (ComboPollRate.SelectedItem is not null)
					SlidebarList.Itm[AssgIndex].PollRate = SlidebarList.Itm[AssgIndex].PollRate.NewVal((ComboPollRate.SelectedItem as IntWithUnit).Value);
			}
		}

		private void CaptureButton_Click(object sender, RoutedEventArgs e)
		{
			Button btn = (Button)sender; int val = SlidebarList.Itm[AssgIndex].GetAxisPos100();
			switch (btn.Tag.ToString().ToUpper())
			{
				case "GRE": ControllerGreUpdown.Value = val; break;
				case "RED": ControllerRedUpdown.Value = val; break;
				case "YEL": ControllerYelUpdown.Value = val; break;
				case "BLU": ControllerBluUpdown.Value = val; break;
				case "ORA": ControllerOraUpdown.Value = val; break;
				case "IDLE": ControllerIdleValueUpdown.Value = val; break;
				default: break;
			}
			UpdateAxisValueReading();
		}

		private void ControllerButtonSetting_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
		{
			if (IsInitialized && !App._ThisIsNotUserInput)
			{
				// Set minimums and maximums so that they cannot surpass each others value
				ControllerGreUpdown.Minimum = ControllerRedUpdown.Value + 1;
				ControllerRedUpdown.Maximum = ControllerGreUpdown.Value - 1;
				ControllerRedUpdown.Minimum = ControllerYelUpdown.Value + 1;
				ControllerYelUpdown.Maximum = ControllerRedUpdown.Value - 1;
				ControllerYelUpdown.Minimum = ControllerBluUpdown.Value + 1;
				ControllerBluUpdown.Maximum = ControllerYelUpdown.Value - 1;
				ControllerBluUpdown.Minimum = ControllerOraUpdown.Value + 1;
				ControllerOraUpdown.Maximum = ControllerBluUpdown.Value - 1;

				// Set values to SLidebar settings
				SlidebarList.Itm[AssgIndex].Settings.MappingGre.AxisValue = (ControllerGreUpdown.Value ?? 0).Conv100Short();
				SlidebarList.Itm[AssgIndex].Settings.MappingRed.AxisValue = (ControllerRedUpdown.Value ?? 0).Conv100Short();
				SlidebarList.Itm[AssgIndex].Settings.MappingYel.AxisValue = (ControllerYelUpdown.Value ?? 0).Conv100Short();
				SlidebarList.Itm[AssgIndex].Settings.MappingBlu.AxisValue = (ControllerBluUpdown.Value ?? 0).Conv100Short();
				SlidebarList.Itm[AssgIndex].Settings.MappingOra.AxisValue = (ControllerOraUpdown.Value ?? 0).Conv100Short();
			}
		}

		private void ControllerGreKeybox_KeyBound(object sender, RoutedEventArgs e)
		{
			SlidebarList.Itm[AssgIndex].Settings.MappingGre.KeyPrim = ControllerGreKeybox.BoundKeyKeys;
		}
		private void ControllerRedKeybox_KeyBound(object sender, RoutedEventArgs e)
		{
			SlidebarList.Itm[AssgIndex].Settings.MappingRed.KeyPrim = ControllerRedKeybox.BoundKeyKeys;
		}
		private void ControllerYelKeybox_KeyBound(object sender, RoutedEventArgs e)
		{
			SlidebarList.Itm[AssgIndex].Settings.MappingYel.KeyPrim = ControllerYelKeybox.BoundKeyKeys;
		}
		private void ControllerBluKeybox_KeyBound(object sender, RoutedEventArgs e)
		{
			SlidebarList.Itm[AssgIndex].Settings.MappingBlu.KeyPrim = ControllerBluKeybox.BoundKeyKeys;
		}
		private void ControllerOraKeybox_KeyBound(object sender, RoutedEventArgs e)
		{
			SlidebarList.Itm[AssgIndex].Settings.MappingOra.KeyPrim = ControllerOraKeybox.BoundKeyKeys;
		}

		private void ControllerIdleValueUpdown_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
		{
			SlidebarList.Itm[AssgIndex].Settings.IdleValue = (ControllerIdleValueUpdown.Value ?? 0).Conv100Short();
		}
		private void ControllerIdleDeadUpdown_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
		{
			SlidebarList.Itm[AssgIndex].Settings.IdleDead = (ControllerIdleDeadUpdown.Value ?? 0).Conv100Short();
		}


		private void LinkPowerOffController_LinkClicked(object sender, RoutedEventArgs e)
		{
			if (!SlidebarList.Itm[AssgIndex].Controller.TryPowerOff())
				MessageBox.Show(Properties.Resources.Info_XInputDllError, Properties.Resources.Info_XInputDllError_Caption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
		}

		private void LinkRestoreDefaults_LinkClicked(object sender, RoutedEventArgs e)
		{
			SlidebarList.Itm[AssgIndex] = SlidebarList.Itm[AssgIndex].GetDefault();
			RefreshPageFromSettings();
		}


		#endregion

		#region COMMANDS
		private void OpenCmdExecuted(object target, ExecutedRoutedEventArgs e)
		{
			string command, targetobj;
			command = (e.Command as RoutedCommand).Name;
			targetobj = (target as FrameworkElement).Name;
			_ = MessageBox.Show("The " + command + " command has been invoked on target object " + targetobj);
		}

		private void OpenCmdCanExecute(object sender, CanExecuteRoutedEventArgs e) { e.CanExecute = true; }

		#endregion


		public event PropertyChangedEventHandler PropertyChanged;
		protected void OnPropertyChanged([CallerMemberName] string new_Value = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(new_Value));
		}


		private void Controller_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (IsInitialized)
				Dispatcher.Invoke(() =>
				{
					LabelBattType.GetBindingExpression(Label.ContentProperty).UpdateSource();
					LabelBattLevel.GetBindingExpression(Label.ContentProperty).UpdateSource();
				});
		}

		public bool RefreshPageFromSettings()
		{
			SlidebarSettings s = SlidebarList.Itm[AssgIndex].Settings;

			ToggleTranslator.IsChecked = s.TranslateEnable;
			CheckboxInvertAxis.IsChecked = s.InvertAxis;
			ComboSelectAxis.SelectedIndex = (int)s.SelectedAxis;
			if (ComboPollRate.ItemsSource is not null)
			{
				var sel = (ComboPollRate.ItemsSource as List<IntWithUnit>).Find(x => x.Value == new IntWithUnit(s.PollRate, "Hz").Value);
				ComboPollRate.SelectedItem = sel;
			}
			ControllerIdleValueUpdown.Value = SlidebarList.Itm[AssgIndex].IdleValue100;
			ControllerIdleDeadUpdown.Value = SlidebarList.Itm[AssgIndex].IdleDead100;
			ControllerGreUpdown.Value = s.MappingGre.GetAxisValue100();
			ControllerGreKeybox.BoundKeyKeys = s.MappingGre.KeyPrim;
			ControllerRedUpdown.Value = s.MappingRed.GetAxisValue100();
			ControllerRedKeybox.BoundKeyKeys = s.MappingRed.KeyPrim;
			ControllerYelUpdown.Value = s.MappingYel.GetAxisValue100();
			ControllerYelKeybox.BoundKeyKeys = s.MappingYel.KeyPrim;
			ControllerBluUpdown.Value = s.MappingBlu.GetAxisValue100();
			ControllerBluKeybox.BoundKeyKeys = s.MappingBlu.KeyPrim;
			ControllerOraUpdown.Value = s.MappingOra.GetAxisValue100();
			ControllerOraKeybox.BoundKeyKeys = s.MappingOra.KeyPrim;

			SlidebarList.Itm[AssgIndex].RefreshController();

			return false;
		}

	}



	public class ControllerListTemplateSelector : DataTemplateSelector
	{
		public DataTemplate FormatAbsent { get; set; }
		public DataTemplate FormatCntdGp { get; set; }
		public DataTemplate FormatCntdGtr { get; set; }

		public override DataTemplate SelectTemplate(object item, DependencyObject container)
		{
			DataTemplate selectedTemplate = null;
			if (item is not null && item is Slidebar)
			{
				Slidebar data = item as Slidebar;
				if (data.Controller.IsConnected)
				{
					if (data.Controller.ControllerSubType.Contains("Guitar"))
						selectedTemplate = FormatCntdGtr ?? FormatCntdGp ?? FormatAbsent;
					else
						selectedTemplate = FormatCntdGp ?? FormatAbsent;
				}
				else selectedTemplate = FormatAbsent;
			}
			return selectedTemplate;
		}
	}

}
