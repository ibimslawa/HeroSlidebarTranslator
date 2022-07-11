using SharpDX.XInput;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using AppResources = HeroSlidebarTranslator.Properties.Resources;

namespace HeroSlidebarTranslator
{

	public enum GamepadAxes : byte
	{
		Axis0LTsX = 0,
		Axis1LTsY = 1,
		Axis2RTsX = 2,
		Axis3RTsY = 3,
		Axis4LT = 4,
		Axis5RT = 5
	}


	public class XController : Controller, INotifyPropertyChanged
	{
		public string PlayerDisplayName { get; private set; }
		public string IsConnectedString { get { return IsConnected ? AppResources.Xbox_ConnectionConnected : AppResources.Xbox_ConnectionAbsent; } }
		public Capabilities Capabilities
		{
			get
			{
				try { return GetCapabilities(DeviceQueryType.Gamepad); }
				catch (Exception) { return new Capabilities(); }
			}
		}
		public string ControllerType => Capabilities.Type.ToString();
		public string ControllerSubType => Capabilities.SubType.ToString();

		public BatteryLevel BattLevel
		{
			get
			{
				BatteryLevel level = IsConnected ? GetBatteryInformation(BatteryDeviceType.Gamepad).BatteryLevel : BatteryLevel.Empty;
				if (level is BatteryLevel.Low or BatteryLevel.Empty) OnLowBattWarning(new BatteryEventArgs());
				return level;
			}
		}
		public BatteryType BattType { get { return IsConnected ? GetBatteryInformation(BatteryDeviceType.Gamepad).BatteryType : BatteryType.Unknown; } }
		public string BattLevelText
		{
			get
			{
				if (!IsConnected) return string.Empty;
				else
				{
					switch (GetBatteryInformation(BatteryDeviceType.Gamepad).BatteryLevel)
					{
						case BatteryLevel.Empty:
							OnLowBattWarning(new BatteryEventArgs() { Level = BatteryLevel.Empty }); return AppResources.Xbox_BatteryLevelEmpty;
						case BatteryLevel.Low:
							OnLowBattWarning(new BatteryEventArgs() { Level = BatteryLevel.Low }); return AppResources.Xbox_BatteryLevelLow;
						case BatteryLevel.Medium: return AppResources.Xbox_BatteryLevelMedium;
						case BatteryLevel.Full: return AppResources.Xbox_BatteryLevelFull;
						default: return string.Empty;
					};
				}
			}
		}
		public string BattLevelSymbol
		{
			get
			{
				if (!IsConnected) return string.Empty;
				else if (GetBatteryInformation(BatteryDeviceType.Gamepad).BatteryType == BatteryType.Disconnected) return "\u2502\u2005\u2571\u2005\u2502";
				else
				{
					switch (GetBatteryInformation(BatteryDeviceType.Gamepad).BatteryLevel)
					{
						case BatteryLevel.Empty:
							OnLowBattWarning(new BatteryEventArgs() { Level = BatteryLevel.Empty }); return "\u2502\u2505\u2505\u2505\u2502";
						case BatteryLevel.Low:
							OnLowBattWarning(new BatteryEventArgs() { Level = BatteryLevel.Low }); return "\u2502\u258b\u2505\u2505\u2502";
						case BatteryLevel.Medium: return "\u2502\u258b\u258b\u2505\u2502";
						case BatteryLevel.Full: return "\u2502\u258b\u258b\u258b\u2502";
						default: return "\u2502  ?  \u2502";
					};
				}
			}
		}
		public string BattTypeText
		{
			get
			{
				if (!IsConnected) return AppResources.Xbox_BatteryTypeUnknown;
				else return GetBatteryInformation(BatteryDeviceType.Gamepad).BatteryType switch
				{
					BatteryType.Disconnected => AppResources.Xbox_BatteryTypeDisconnected,
					BatteryType.Wired => AppResources.Xbox_BatteryTypeUnknown,
					BatteryType.Alkaline => AppResources.Xbox_BatteryTypeAlkaline,
					BatteryType.Nimh => AppResources.Xbox_BatteryTypeNimh,
					_ => AppResources.Xbox_BatteryTypeUnknown,
				};
			}
		}

		public XController(UserIndex player = UserIndex.Any, OnConnectionChange function = null)
			: base(player)
		{
			PlayerDisplayName = player switch
			{
				UserIndex.One => "P1",
				UserIndex.Two => "P2",
				UserIndex.Three => "P3",
				UserIndex.Four => "P4",
				UserIndex.Any => AppResources.Xbox_PlayerAny,
				_ => "--",
			};
			onConnectionChange += function;
			lastConnectionStatus = IsConnected;
			onConnectionChange?.Invoke();
		}


		public event EventHandler<BatteryEventArgs> LowBattWarning;

		public event PropertyChangedEventHandler PropertyChanged;

		public delegate void OnConnectionChange();
		private OnConnectionChange onConnectionChange;
		bool lastConnectionStatus;

		public void OnLowBattWarning(BatteryEventArgs e)
		{
			EventHandler<BatteryEventArgs> handler = LowBattWarning;
			if (handler != null)
			{
				handler(this, e);
			}
		}

		// This method is called by the Set accessor of each property.  
		// The CallerMemberName attribute that is applied to the optional propertyName  
		// parameter causes the property name of the caller to be substituted as an argument.  
		private void NotifyPropertyChanged([CallerMemberName] string propertyName = "") =>
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

		public void AttachOnConnectionChangedHandler(OnConnectionChange handler) => onConnectionChange += handler;
		public void DetachOnConnectionChangedHandler(OnConnectionChange handler) => onConnectionChange -= handler;


		public void Refresh()
		{
			if (lastConnectionStatus != IsConnected)
			{
				lastConnectionStatus = IsConnected;
				onConnectionChange?.Invoke();
			}
		}

		public short GetAxis(GamepadAxes axis)
		{
			if (IsConnected)
			{
				State state = GetState();
				return axis switch
				{
					GamepadAxes.Axis0LTsX => state.Gamepad.LeftThumbX,
					GamepadAxes.Axis1LTsY => state.Gamepad.LeftThumbY,
					GamepadAxes.Axis2RTsX => state.Gamepad.RightThumbX,
					GamepadAxes.Axis3RTsY => state.Gamepad.RightThumbY,
					GamepadAxes.Axis4LT => state.Gamepad.RightTrigger,
					GamepadAxes.Axis5RT => state.Gamepad.RightTrigger,
					_ => 0,
				};
			}
			else { return 0; }
		}

		/// <summary>
		/// Trys to turn off the controller with the assigned UserIndex.
		/// </summary>
		/// <returns>Returns false if the dll call failed.</returns>
		public bool TryPowerOff()
		{
			try { XInputPowerOffController((int)UserIndex); return true; }
			catch { return false; }

		}

		/// <summary>
		/// Turns off an XInput device.
		/// </summary>
		/// <param name="playerIndex">The player index of the controller.</param>
		/// <returns>Dll call return</returns>
		[DllImport("xinput1_3.dll", EntryPoint = "#103")]
		static extern int XInputPowerOffController(int playerIndex);
	}
	public class BatteryEventArgs : EventArgs { public BatteryLevel Level; }




	#region Type converters for properties

	public class Prop<T> : INotifyPropertyChanged
	{
		private T _value;

		public T Value
		{
			get => _value;
			set { _value = value; NotifyPropertyChanged(nameof(Value)); }
		}

		public event PropertyChangedEventHandler PropertyChanged;

		internal void NotifyPropertyChanged(string propertyName) =>
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

		public static explicit operator T(Prop<T> value)
		{
			return value.Value;
		}
	}
	#endregion
}
