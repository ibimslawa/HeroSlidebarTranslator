using System;
using System.Runtime.InteropServices;

namespace HeroSlidebarTranslator
{

	public static class SendKeysPInput
	{

		#region MapVirtualKey

		/// <summary>
		/// The MapVirtualKey function translates (maps) a virtual-key code into a scan
		/// code or character value, or translates a scan code into a virtual-key code    
		/// </summary>
		/// <param name="uCode">[in] Specifies the virtual-key code or scan code for a key.
		/// How this value is interpreted depends on the value of the uMapType parameter
		/// </param>
		/// <param name="uMapType">[in] Specifies the translation to perform. The value of this
		/// parameter depends on the value of the uCode parameter.
		/// </param>
		/// <returns>Either a scan code, a virtual-key code, or a character value, depending on
		/// the value of uCode and uMapType. If there is no translation, the return value is zero
		/// </returns>
		[DllImport("User32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		public static extern uint MapVirtualKey(uint uCode, uint uMapType);

		public const int INPUT_KEYBOARD = 1;

		public const int KEYEVENTF_EXTENDEDKEY = 0x0001;
		public const int KEYEVENTF_KEYUP = 0x0002;
		public const int KEYEVENTF_UNICODE = 0x0004;
		public const int KEYEVENTF_SCANCODE = 0x0008;

		#endregion

		#region SendKey

		[StructLayout(LayoutKind.Sequential)]
		public struct MOUSEINPUT
		{
			public int dx;
			public int dy;
			public int mouseData;
			public int dwFlags;
			public int time;
			public IntPtr dwExtraInfo;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct KEYBDINPUT
		{
			public short wVk;
			public short wScan;
			public int dwFlags;
			public int time;
			public IntPtr dwExtraInfo;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct HARDWAREINPUT
		{
			public int uMsg;
			public short wParamL;
			public short wParamH;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct INPUT
		{
			public int type;
			public INPUTUNION inputUnion;
		}

		[StructLayout(LayoutKind.Explicit)]
		public struct INPUTUNION
		{
			[FieldOffset(0)]
			public MOUSEINPUT mi;
			[FieldOffset(0)]
			public KEYBDINPUT ki;
			[FieldOffset(0)]
			public HARDWAREINPUT hi;
		}

		/// <summary>
		/// Declaration of external SendInput method
		/// </summary>
		/// <param name="nInputs"></param>
		/// <param name="pInput"></param>
		/// <param name="cbSize"></param>
		/// <returns></returns>
		[DllImport("User32.dll", SetLastError = true)]
		public static extern int SendInput(int nInputs, [MarshalAs(UnmanagedType.LPArray)] INPUT[] pInput, int cbSize);


		/// <summary>
		/// 
		/// </summary>
		/// <param name="wVk"></param>
		/// <param name="bExtendedkey"></param>
		/// <param name="bDown"></param>
		/// <param name="bUp"></param>
		public static void SendKeyPI(short wVk, bool bExtendedkey, bool bDown, bool bUp)
		{
			INPUT[] input = new INPUT[1];
			if (bDown)
			{
				input[0].inputUnion.ki.wVk = wVk;
				input[0].inputUnion.ki.wScan = (short)MapVirtualKey((uint)wVk, 0U);
				input[0].inputUnion.ki.dwFlags = KEYEVENTF_SCANCODE;
				if (bExtendedkey)
				{ input[0].inputUnion.ki.dwFlags |= KEYEVENTF_EXTENDEDKEY; }

				input[0].inputUnion.ki.time = 0;
				input[0].type = INPUT_KEYBOARD;
				_ = SendInput(1, input, Marshal.SizeOf(input[0]));
			}
			if (bUp)
			{
				input[0].inputUnion.ki.wVk = wVk;
				input[0].inputUnion.ki.wScan = (short)MapVirtualKey((uint)wVk, 0U);
				input[0].inputUnion.ki.dwFlags = KEYEVENTF_SCANCODE | KEYEVENTF_KEYUP;
				if (bExtendedkey)
				{ input[0].inputUnion.ki.dwFlags |= KEYEVENTF_EXTENDEDKEY; }

				input[0].inputUnion.ki.time = 0;
				input[0].type = INPUT_KEYBOARD;
				_ = SendInput(1, input, Marshal.SizeOf(input[0]));
			}
		}

		#endregion
	}
}
