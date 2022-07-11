using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;

namespace HeroSlidebarTranslator
{
	/// <summary>
	/// Interaktionslogik für LinkLabel.xaml
	/// </summary>
	[ContentProperty("Text")]
	public partial class LinkLabel : UserControl
	{
		public LinkLabel()
		{
			InitializeComponent();
		}

		public static readonly RoutedEvent ClickLinkEvent = EventManager.RegisterRoutedEvent(
			name: "LinkClicked", routingStrategy: RoutingStrategy.Bubble, handlerType: typeof(RoutedEventHandler), ownerType: typeof(LinkLabel));

		public event RoutedEventHandler LinkClicked
		{
			add { AddHandler(ClickLinkEvent, value); }
			remove { RemoveHandler(ClickLinkEvent, value); }
		}
		void RaiseLinkClickedEvent()
		{
			// Create a RoutedEventArgs instance.
			RoutedEventArgs routedEventArgs = new(routedEvent: ClickLinkEvent);
			// Raise the event, which will bubble up through the element tree.
			RaiseEvent(routedEventArgs);
		}

		protected override void OnMouseDown(MouseButtonEventArgs e)
		{
			// Some condition combined with the Click event will trigger the ClickLink event.
			if (e.LeftButton == MouseButtonState.Pressed)
				RaiseLinkClickedEvent();
			// Call the base class method method so Click event subscribers are notified.
			base.OnMouseDown(e);
		}

	}

}
