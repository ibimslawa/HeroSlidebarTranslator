using SharpDX.XInput;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Timers;
using System.Windows;
using System.Windows.Forms;
using static HeroSlidebarTranslator.Extensions;
using static HeroSlidebarTranslator.SendKeysPInput;
using Timer = System.Timers.Timer;

namespace HeroSlidebarTranslator
{

	public enum SlidebarButtons : byte
	{
		Gre = 0, Red = 1, Yel = 2, Blu = 3, Ora = 4
	}



	public class SlidebarList
	{
		public static Dictionary<UserIndex, Slidebar> Itm = new();

		public static void SetSlidebar(UserIndex player, Slidebar slidebar)
		{
			if (Itm.ContainsKey(player)) { Itm[player] = slidebar; }
			else { Itm.Add(player, slidebar); }
		}


		public static void RefreshCtrlrs()
		{
			foreach (Slidebar item in Itm.Values)
			{ item.Controller.Refresh(); }
		}

		public static void TurnAllOnOff(bool trueForOn)
		{
			foreach (Slidebar bar in Itm.Values)
			{
				bar.TranslateOn = trueForOn;
			}
		}
	}


	public class SlidebarSettings
	{
		public GamepadAxes SelectedAxis { get; set; } = GamepadAxes.Axis0LTsX;
		public bool InvertAxis { get; set; } = false;
		public bool TranslateEnable { get; set; } = false;
		public int PollRate { get; set; } = 1000;

		public short IdleValue { get; set; } = -1000;
		public short IdleDead { get; set; } = 2500;

		public SlidebarPoint MappingGre { get; set; } = new(27500, new Keys?(Keys.A));
		public SlidebarPoint MappingRed { get; set; } = new(13400, new Keys?(Keys.S));
		public SlidebarPoint MappingYel { get; set; } = new(-5000, new Keys?(Keys.D));
		public SlidebarPoint MappingBlu { get; set; } = new(-16400, new Keys?(Keys.F));
		public SlidebarPoint MappingOra { get; set; } = new(-32500, new Keys?(Keys.G));


		public static List<IntWithUnit> PollRates { get; } =
			new List<IntWithUnit> { new IntWithUnit(1000, "Hz"), new IntWithUnit(500, "Hz"), new IntWithUnit(250, "Hz"), new IntWithUnit(125, "Hz") };
	}

	public class Slidebar : DependencyObject, INotifyPropertyChanged
	{

		public SlidebarSettings Settings;

		private static readonly Timer timerUpdate = new(1);

		private XController _controller;
		public XController Controller
		{
			get => _controller;
			set { _controller = value; NotifyPropertyChanged(); }
		}
		public GamepadAxes SelectedAxis
		{
			get => Settings.SelectedAxis;
			set { Settings.SelectedAxis = value; NotifyPropertyChanged(); }
		}
		public bool InvertAxis
		{
			get => Settings.InvertAxis;
			set { Settings.InvertAxis = value; NotifyPropertyChanged(); }
		}
		public bool TranslateEnable
		{
			get => Settings.TranslateEnable;
			set
			{
				Settings.TranslateEnable = value;
				if (!value) timerUpdate.Enabled = false;
				else if (value && _TranslateOn) timerUpdate.Enabled = true;
				NotifyPropertyChanged();
			}
		}
		public string TranslateEnableSymbol { get => TranslateEnable ? "\u2713" : "\u2717"; }

		public bool _TranslateOn = false;
		public bool TranslateOn
		{
			private get => _TranslateOn;
			set { timerUpdate.Enabled = value && TranslateEnable; _TranslateOn = value; }
		}

		public bool TranslateActive { get => timerUpdate.Enabled; }

		public double Interval { get { return timerUpdate.Interval; } }
		public LimInt PollRate
		{
			get => new LimInt(Settings.PollRate, 125, 1000);
			set
			{
				Settings.PollRate = value;
				timerUpdate.Interval = 1000.0 / value;
				NotifyPropertyChanged();
			}
		}
		public short IdleValue
		{
			get => Settings.IdleValue;
			set { Settings.IdleValue = value; NotifyPropertyChanged(); }
		}
		public short IdleDead
		{
			get => Settings.IdleDead;
			set { Settings.IdleDead = value; NotifyPropertyChanged(); }
		}

		public int IdleValue100 { get => IdleValue.ConvShort100(); set => IdleValue = value.Conv100Short(); }
		public int IdleDead100 { get => IdleDead.ConvShort100(); set => IdleDead = value.Conv100Short(); }
		public int GrePos100 { get => Settings.MappingGre.AxisValue.ConvShort100(); set => Settings.MappingGre.AxisValue = value.Conv100Short(); }
		public int RedPos100 { get => Settings.MappingRed.AxisValue.ConvShort100(); set => Settings.MappingRed.AxisValue = value.Conv100Short(); }
		public int YelPos100 { get => Settings.MappingYel.AxisValue.ConvShort100(); set => Settings.MappingYel.AxisValue = value.Conv100Short(); }
		public int BluPos100 { get => Settings.MappingBlu.AxisValue.ConvShort100(); set => Settings.MappingBlu.AxisValue = value.Conv100Short(); }
		public int OraPos100 { get => Settings.MappingOra.AxisValue.ConvShort100(); set => Settings.MappingOra.AxisValue = value.Conv100Short(); }


		public event PropertyChangedEventHandler PropertyChanged;

		private List<SlidebarPoint> allSlidebarPoints;


		public Slidebar(XController controller, GamepadAxes selectedAxis = GamepadAxes.Axis0LTsX)
		{
			Settings = new();
			Controller = controller; SelectedAxis = selectedAxis;
			UpdatePointList();
			timerUpdate.Elapsed += timerUpdate_Elapsed;
		}
		public Slidebar(XController controller, Keys? keyGre, Keys? keyRed, Keys? keyYel, Keys? keyBlu, Keys? keyOra, GamepadAxes selectedAxis = GamepadAxes.Axis0LTsX)
			: this(controller, selectedAxis)
		{
			Settings.MappingGre.KeyPrim = keyGre;
			Settings.MappingRed.KeyPrim = keyRed;
			Settings.MappingYel.KeyPrim = keyYel;
			Settings.MappingBlu.KeyPrim = keyBlu;
			Settings.MappingOra.KeyPrim = keyOra;
		}


		// This method is called by the Set accessor of each property.  
		// The CallerMemberName attribute that is applied to the optional propertyName  
		// parameter causes the property name of the caller to be substituted as an argument.  
		private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}


		private static void On100PropertyChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			short newValueShort = ((int)e.NewValue).Conv100Short();
			switch (e.Property.Name)
			{
				case "IdleValue100": (sender as Slidebar).IdleValue = newValueShort; break;
				case "IdleDead100": (sender as Slidebar).IdleDead = newValueShort; break;
				case "GreenPos100": (sender as Slidebar).Settings.MappingGre.AxisValue = newValueShort; break;
				default: break;
			}
		}

		private int currentPos = 0, prevPos = int.MaxValue;
		private SlidebarPoint currentPoint = null, prevPoint = null;
		private bool startingToTouch = false;

		private void timerUpdate_Elapsed(object sender, ElapsedEventArgs e)
		{
			if (Controller.IsConnected)
			{
				currentPos = GetAxisPos();

				if (currentPos >= (IdleValue - IdleDead) && currentPos <= (IdleValue + IdleDead)) // Touch input is in deadzone
				{
					startingToTouch = false;
					if (prevPoint is not null) SendKeysFromPoint(prevPoint, false);
				}
				else // Touch input is on a point
				{
					// Holding down
					if (currentPoint is not null && currentPos >= prevPos + AXISFLUTTER && currentPos <= prevPos - AXISFLUTTER)
					{
						SendKeysFromPoint(currentPoint, true);
						startingToTouch = false;
					}
					else // Here comes a new touch input
					{
						if (!startingToTouch) // Beginning to touch, let it settle
						{ startingToTouch = true; }
						else // Real value, now send out the key
						{
							// find nearest mapping to axis value:
							currentPoint = null; int diff, mindiff = int.MaxValue;
							foreach (SlidebarPoint point in allSlidebarPoints)
							{
								diff = Math.Abs(point.AxisValue - currentPos);
								if (mindiff > diff)
								{ mindiff = diff; currentPoint = point; }
							}
							// send out a valid mapping:
							if (currentPoint is not null)
							{
								SendKeysFromPoint(currentPoint, true);
							}
						}
						// Send KeyUp for released keys:
						if (prevPoint is not null && prevPoint.AxisValue != currentPoint.AxisValue) { SendKeysFromPoint(prevPoint, false); }
					}
				}

				prevPos = currentPos; prevPoint = currentPoint;
			}
		}
		private void SendKeysFromPoint(SlidebarPoint pt, bool trueForBtnDown)
		{
			if (pt.KeyPrim is not null) { SendKeyPI((short)pt.KeyPrim, false, trueForBtnDown, !trueForBtnDown); }
			if (pt.GetType() == typeof(SlidebarBetwPoint))
			{
				SlidebarBetwPoint pit = (SlidebarBetwPoint)pt;
				if (pit.KeySec is not null)
					SendKeyPI((short)pit.KeySec, false, trueForBtnDown, !trueForBtnDown);
			}
		}


		public short GetAxisPos() { return InvertAxis ? Controller.GetAxis(SelectedAxis) : (short)(Controller.GetAxis(SelectedAxis) * -1); }

		public int GetAxisPos100() { return GetAxisPos().ConvShort100(); }

		public IEnumerable<SlidebarPoint> GetAllMappings()
		{
			return new List<SlidebarPoint> { Settings.MappingGre, Settings.MappingRed, Settings.MappingYel, Settings.MappingBlu, Settings.MappingOra };
		}

		public void UpdatePointList()
		{
			allSlidebarPoints = new List<SlidebarPoint>()
			{
				Settings.MappingGre,  // Green
				new SlidebarBetwPoint((Settings.MappingGre.AxisValue + Settings.MappingRed.AxisValue) / 2, Settings.MappingGre.KeyPrim, Settings.MappingRed.KeyPrim),  // Green + Red
				Settings.MappingRed,  // Red
				new SlidebarBetwPoint((Settings.MappingRed.AxisValue + Settings.MappingYel.AxisValue) / 2, Settings.MappingRed.KeyPrim, Settings.MappingYel.KeyPrim),  // Red + Yellow
				Settings.MappingYel,  // Yellow
				new SlidebarBetwPoint((Settings.MappingYel.AxisValue + Settings.MappingBlu.AxisValue) / 2, Settings.MappingYel.KeyPrim, Settings.MappingBlu.KeyPrim),  // Yellow + Blue
				Settings.MappingBlu,  // Blue
				new SlidebarBetwPoint((Settings.MappingBlu.AxisValue + Settings.MappingOra.AxisValue) / 2, Settings.MappingBlu.KeyPrim, Settings.MappingOra.KeyPrim),  // Blue + Orange
				Settings.MappingOra,  // Orange
			};
		}

		public void Invert() { InvertAxis = !InvertAxis; }

		public void SetMapping(SlidebarButtons btn, short axisval, Keys? key)
		{
			switch (btn)
			{
				case SlidebarButtons.Gre: Settings.MappingGre = new(axisval, key); break;
				case SlidebarButtons.Red: Settings.MappingRed = new(axisval, key); break;
				case SlidebarButtons.Yel: Settings.MappingYel = new(axisval, key); break;
				case SlidebarButtons.Blu: Settings.MappingBlu = new(axisval, key); break;
				case SlidebarButtons.Ora: Settings.MappingOra = new(axisval, key); break;
			}
			UpdatePointList();
		}

		public void RefreshController() => Controller.Refresh();

	}


	public class SlidebarPoint
	{
		public short AxisValue { get; set; }

		public Keys? KeyPrim { get; set; }


		public SlidebarPoint(short axisVal, Keys? key) { AxisValue = axisVal; KeyPrim = key; }


		public int GetAxisValueInt() { return AxisValue; }

		public int GetAxisValue100()
		{
			return AxisValue.ConvShort100();
		}

		public override string ToString() { return $"[{AxisValue}:{KeyPrim}]"; }

	}

	public class SlidebarBetwPoint : SlidebarPoint
	{
		public Keys? KeySec { get; set; }

		public SlidebarBetwPoint(short axisVal, Keys? keyPrim, Keys? keySec)
			: base(axisVal, keyPrim)
		{
			KeySec = keySec;
		}
		public SlidebarBetwPoint(int axisVal, Keys? keyPrim, Keys? keySec)
			: this((short)axisVal, keyPrim, keySec) { }

		public override string ToString() { return $"{{{AxisValue}:{KeyPrim},{KeySec}}}"; }
	}


	public class IntWithUnit
	{
		public int Value { get; set; }
		public string Unit { get; private set; }

		public IntWithUnit(int value, string unit) { Value = value; Unit = unit.Trim(); }

		public override string ToString() => Value.ToString() + " " + Unit;

		public static implicit operator int(IntWithUnit iwu) => iwu.Value;
	}



	public struct LimInt
	{
		private int _value;
		public int LowerLimit { get; private set; }
		public int UpperLimit { get; private set; }

		public LimInt(int value, int lolim, int uplim)
		{
			if (lolim <= uplim) { LowerLimit = lolim; UpperLimit = uplim; }
			else { LowerLimit = uplim; UpperLimit = lolim; }

			if (value < LowerLimit) { _value = LowerLimit; }
			else if (value > UpperLimit) { _value = UpperLimit; }
			else { _value = value; }
		}

		public LimInt NewVal(int newval)
		{
			return new LimInt(newval, LowerLimit, UpperLimit);
		}

		public void NewLimits(int newlolim, int newuplim)
		{
			LowerLimit = newlolim; UpperLimit = newuplim;
		}

		public override string ToString() => _value.ToString();

		public static implicit operator int(LimInt li) => li._value;
	}


	public static partial class Extensions
	{
		public static Slidebar GetDefault(this Slidebar sb)
		{
			return sb.Controller.UserIndex switch
			{
				UserIndex.One => new Slidebar(sb.Controller, Keys.A, Keys.S, Keys.D, Keys.F, Keys.G, sb.SelectedAxis),
				UserIndex.Two => new Slidebar(sb.Controller, Keys.Q, Keys.W, Keys.E, Keys.R, Keys.T, sb.SelectedAxis),
				UserIndex.Three => new Slidebar(sb.Controller, Keys.Y, Keys.X, Keys.C, Keys.V, Keys.B, sb.SelectedAxis),
				UserIndex.Four => new Slidebar(sb.Controller, Keys.D1, Keys.D2, Keys.D3, Keys.D4, Keys.D5, sb.SelectedAxis),
				_ => new Slidebar(sb.Controller, Keys.D1, Keys.D2, Keys.D3, Keys.D4, Keys.D5, sb.SelectedAxis),
			};
		}

		public const short AXISFLUTTER = 0; // 2048


		/// <param name="value">The number to be remapped</param>
		/// <param name="inputFrom">Input range low</param>
		/// <param name="inputTo">Input range high</param>
		/// <param name="outputFrom">Output range low</param>
		/// <param name="outputTo">Output range high</param>
		/// <returns>Returns the remapped number.</returns>
		//public static float Remap(float value, float inputFrom, float inputTo, float outputFrom, float outputTo) { return (value - inputFrom) / (inputTo - inputFrom) * (outputTo - outputFrom) + outputFrom; }
		public static double Remap(double value, double inputFrom, double inputTo, double outputFrom, double outputTo) { return (value - inputFrom) / (inputTo - inputFrom) * (outputTo - outputFrom) + outputFrom; }

		public static short Conv100Short(this int value) => (short)Remap(value, -100.0, 100.0, short.MinValue, short.MaxValue);
		public static short Conv100Short(this double value) => (short)Remap(value, -100.0, 100.0, short.MinValue, short.MaxValue);
		public static int ConvShort100(this short value) => (int)Math.Round(Remap(value, short.MinValue, short.MaxValue, -100.0, 100.0));


	}

}
