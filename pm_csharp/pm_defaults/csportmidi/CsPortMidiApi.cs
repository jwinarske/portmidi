using System;
using System.Runtime.InteropServices;

namespace csportmidi
{

	public class CsPortMidiApi
	{
        [StructLayout(LayoutKind.Sequential)]
        public struct PortMidiStream
        {
            internal IntPtr address;
        };
        [StructLayout(LayoutKind.Sequential)]
        public struct PmEvent
		{
			public int message;
			public int timestamp;
		}

        // PmError bindings
        public readonly int pmNoError = 0;
		public readonly int pmNoData = 0;
		public readonly int pmGotData = -1;
		public readonly int pmHostError = -10000;
		public readonly int pmInvalidDeviceId = -9999;
		public readonly int pmInsufficientMemory = -9998;
		public readonly int pmBufferTooSmall = -9997;
		public readonly int pmBufferOverflow = -9996;
		public readonly int pmBadPtr = -9995;
		public readonly int pmBadData = -9994;
		public readonly int pmInternalError = -9993;
		public readonly int pmBufferMaxSize = -9992;

        [DllImport(Defines.DllName)]
        public static extern int Pm_Initialize();
        [DllImport(Defines.DllName)]
        public static extern int Pm_Terminate();
        [DllImport(Defines.DllName)]
        unsafe public static extern int Pm_HasHostError(PortMidiStream *stream);
        [DllImport(Defines.DllName)]
        public static extern String Pm_GetErrorText(int errnum);
        [DllImport(Defines.DllName)]
        public static extern String Pm_GetHostErrorText();
		internal readonly int pmNoDevice = -1;
        [DllImport(Defines.DllName)]
        public static extern int Pm_CountDevices();
        [DllImport(Defines.DllName)]
        public static extern int Pm_GetDefaultInputDeviceID();
        [DllImport(Defines.DllName)]
        public static extern int Pm_GetDefaultOutputDeviceID();
        [DllImport(Defines.DllName)]
        public static extern IntPtr Pm_GetDeviceInfo(int i);
        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct PmDeviceInfo
        {
            public int structVersion; /**< this internal structure version */
            public IntPtr /* const char* */ interf; /**< underlying MIDI API, e.g. MMSystem or DirectX */
            public IntPtr /* const char* */ name;   /**< device name, e.g. USB MidiSport 1x1 */
            public int input; /**< true iff input is available */
            public int output; /**< true iff output is available */
            public int opened; /**< used by generic PortMidi code to do error checking on arguments */
        }
        [DllImport (Defines.DllName)]
		public static extern String Pm_GetDeviceName(int i);
        [DllImport(Defines.DllName)]
        unsafe public static extern int Pm_OpenInput(PortMidiStream **stream, int inputDevice, string inputDriverInfo, int bufferSize);
        [DllImport(Defines.DllName)]
        unsafe public static extern int Pm_OpenOutput(PortMidiStream **stream, int outputDevice, string outnputDriverInfo, int bufferSize, int latency);
		public static readonly int PM_FILT_ACTIVE = (1 << 0x0E);
		public static readonly int PM_FILT_SYSEX = (1 << 0x00);
		public static readonly int PM_FILT_CLOCK = (1 << 0x08);
		public static readonly int PM_FILT_PLAY = (1 << 0x0A) | (1 << 0x0C) | (1 << 0x0B);
		public static readonly int PM_FILT_TICK = (1 << 0x09);
		public static readonly int PM_FILT_FD = (1 << 0x0D);
		public static readonly int PM_FILT_UNDEFINED = PM_FILT_FD;
		public static readonly int PM_FILT_RESET = (1 << 0x0F);
		public static readonly int PM_FILT_REALTIME = PM_FILT_ACTIVE | PM_FILT_SYSEX | PM_FILT_CLOCK;
		public static readonly int PM_FILT_NOTE = (1 << 0x19) | (1 << 0x18);
		public static readonly int PM_FILT_CHANNEL_AFTERTOUCH = (1 << 0x1D);
		public static readonly int PM_FILT_POLY_AFTERTOUCH = (1 << 0x1A);
		public static readonly int PM_FILT_AFTERTOUCH = (PM_FILT_CHANNEL_AFTERTOUCH | PM_FILT_POLY_AFTERTOUCH);
		public static readonly int PM_FILT_PROGRAM = (1 << 0x1C);
		public static readonly int PM_FILT_CONTROL = (1 << 0x1B);
		public static readonly int PM_FILT_PITCHBEND = (1 << 0x1E);
		public static readonly int PM_FILT_MTC = (1 << 0x01);
		public static readonly int PM_FILT_SONG_POSITION = (1 << 0x02);
		public static readonly int PM_FILT_SONG_SELECT = (1 << 0x03);
		public static readonly int PM_FILT_TUNE = (1 << 0x06);
		public static readonly int PM_FILT_SYSTEMCOMMON = (PM_FILT_MTC | PM_FILT_SONG_POSITION | PM_FILT_SONG_SELECT | PM_FILT_TUNE);
        [DllImport(Defines.DllName)]
        unsafe public static extern int Pm_SetFilter(PortMidiStream *stream, int filters);
		public static int Pm_Channel(int channel)
		{
			return 1 << channel;
		}
        [DllImport(Defines.DllName)]
        unsafe public static extern int Pm_SetChannelMask(PortMidiStream *stream, int mask);
        [DllImport(Defines.DllName)]
        unsafe public static extern int Pm_Abort(PortMidiStream *stream);
        [DllImport(Defines.DllName)]
        unsafe public static extern int Pm_Close(PortMidiStream *stream);
		public static int Pm_Message(int status, int data1, int data2)
		{
			return (((data2 << 16) & 0xFF0000) | ((data1 << 8) & 0xFF00) | (status & 0xFF));
		}
		public static int Pm_MessageStatus(int msg)
		{
			return msg & 0xFF;
		}
		public static int Pm_MessageData1(int msg)
		{
			return (msg >> 8) & 0xFF;
		}
		public static int Pm_MessageData2(int msg)
		{
			return (msg >> 16) & 0xFF;
		}
        // only supports reading one buffer at a time
        [DllImport(Defines.DllName)]
        unsafe public static extern int Pm_Read(PortMidiStream *stream, PmEvent buffer);
        [DllImport(Defines.DllName)]
        unsafe public static extern int Pm_Poll(PortMidiStream *stream);
        // only supports writing one buffer at a time
        [DllImport(Defines.DllName)]
        unsafe public static extern int Pm_Write(PortMidiStream *stream, PmEvent buffer);
        [DllImport(Defines.DllName)]
        unsafe public static extern int Pm_WriteShort(PortMidiStream *stream, int @when, int msg);
        [DllImport(Defines.DllName)]
        unsafe public static extern int Pm_WriteSysEx(PortMidiStream *stream, int @when, sbyte[] msg);

		public readonly int ptNoError = 0;
		public readonly int ptAlreadyStarted = -10000;
		public readonly int ptAlreadyStopped = -9999;
		public readonly int PtInsufficientMemory = -9998;
        [DllImport(Defines.DllName)]
        public static extern int Pt_TimeStart(int resolution);
        [DllImport(Defines.DllName)]
        public static extern int Pt_TimeStop();
        [DllImport(Defines.DllName)]
        public static extern int Pt_Time();
        [DllImport(Defines.DllName)]
        public static extern bool Pt_TimeStarted();
	}

}