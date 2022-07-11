using System.ComponentModel;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;

namespace HeroSlidebarTranslator
{
	/// <summary>
	/// Interaktionslogik für UserControl1.xaml
	/// </summary>
	public partial class KeyBindBox : System.Windows.Controls.UserControl
	{
		public static readonly DependencyProperty BoundKeyProperty =
			DependencyProperty.Register(nameof(BoundKey), typeof(Key?), typeof(KeyBindBox), new FrameworkPropertyMetadata(default(Key?), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
		public static readonly DependencyProperty BoundKeyKeysProperty =
			DependencyProperty.Register(nameof(BoundKeyKeys), typeof(Keys?), typeof(KeyBindBox), new FrameworkPropertyMetadata(default(Keys?), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

		public Key? BoundKey
		{
			get => (Key?)GetValue(BoundKeyProperty);
			set
			{
				SetValue(BoundKeyProperty, value);
				SetValue(BoundKeyKeysProperty, value is null ? null : (Keys?)KeyInterop.VirtualKeyFromKey((Key)value));
				RaiseEvent(new RoutedEventArgs(KeyBoundEvent, this));
			}
		}
		public Key BoundKeyValued { get => BoundKey ?? Key.None; }
		public Keys? BoundKeyKeys
		{
			get => (Keys?)GetValue(BoundKeyKeysProperty);
			set
			{
				SetValue(BoundKeyKeysProperty, value);
				SetValue(BoundKeyProperty, value is null ? null : (Key?)KeyInterop.KeyFromVirtualKey((int)value));
				RaiseEvent(new RoutedEventArgs(KeyBoundEvent, this));
			}
		}


		public static readonly RoutedEvent KeyBoundEvent = EventManager.RegisterRoutedEvent("KeyBound", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(KeyBindBox));

		//public event PropertyChangedEventHandler PropertyChanged;

		//// This method is called by the Set accessor of each property.
		//// The CallerMemberName attribute that is applied to the optional propertyName
		//// parameter causes the property name of the caller to be substituted as an argument.
		//private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
		//{
		//	if (PropertyChanged != null)
		//	{
		//		PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		//	}
		//}


		// Provide CLR accessors for the event
		[Browsable(true)]
		[Category("Action")]
		[Description("Invoked when user binds a key by pressing it")]
		public event RoutedEventHandler KeyBound
		{
			add { AddHandler(KeyBoundEvent, value); }
			remove { RemoveHandler(KeyBoundEvent, value); }
		}


		public KeyBindBox()
		{
			InitializeComponent();
		}

		public static void OnBoundKeyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{

		}

		private void KeybindingTextBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
		{
			// Don't let the event pass further
			// because we don't want standard textbox shortcuts working
			e.Handled = true;

			// Get key data
			Key key = e.Key;

			// When Alt is pressed, SystemKey is used instead
			if (key == Key.System) { key = e.SystemKey; }

			// Pressing delete, backspace or escape without modifiers clears the current value
			if (key is Key.Delete or Key.Back)
			{
				BoundKey = null;
				return;
			}

			// If no actual key or a key to be ignored was pressed - return
			if (key is Key.LeftCtrl or Key.RightCtrl or
				Key.LeftAlt or Key.RightAlt or
				Key.LeftShift or Key.RightShift or
				Key.LWin or Key.RWin or
				Key.Clear or Key.OemClear or
				Key.Apps or
				Key.Escape or Key.Tab or Key.OemBackTab or Key.Capital)
			{ return; }

			// Update the value
			BoundKey = new Key?(key);
			// Trick the UI to defocus the textbox
			PanelBackground.Focusable = true; PanelBackground.Focus(); PanelBackground.Focusable = false;

		}

	}


}
