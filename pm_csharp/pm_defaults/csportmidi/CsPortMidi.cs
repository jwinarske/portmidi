using System;

namespace csportmidi
{

	/* PortMidi is a general class intended for any Java program using
	   the PortMidi library. It encapsulates CsPortMidiApi with a more
	   object-oriented interface. A single PortMidi object can manage
	   up to one input stream and one output stream.
	
	   This class is not safely callable from multiple threads. It
	   is the client's responsibility to periodically call the Poll
	   method which checks for midi input and handles it.
	*/

	using csportmidi;
    using System.Runtime.InteropServices;

    public class CsPortMidi
    {

        // timecode to send message immediately
        public readonly int NOW = 0;

        // midi codes
        public const int MIDI_NOTE_OFF = 0x80;
        public const int MIDI_NOTE_ON = 0x90;
        public const int CTRL_ALL_OFF = 123;
        public const int MIDI_PITCH_BEND = 0xE0;
        public const int MIDI_CLOCK = 0xF8;
        public const int MIDI_CONTROL = 0xB0;
        public const int MIDI_PROGRAM = 0xC0;
        public const int MIDI_START = 0xFA;
        public const int MIDI_STOP = 0xFC;
        public const int MIDI_POLY_TOUCH = 0xA0;
        public const int MIDI_TOUCH = 0xD0;

        // error code -- cannot refresh device list while stream is open:
        public readonly int pmStreamOpen = -5000;
        public readonly int pmOutputNotOpen = -4999;

        // access to CSPortMidiApi is through a single, global instance
        private static CsPortMidiApi pm;
        // a reference count tracks how many objects have it open
        private static int pmRefCount = 0;
        private static int openCount = 0;

        public int error; // user can check here for error codes
        unsafe private CsPortMidiApi.PortMidiStream *input;
        unsafe private CsPortMidiApi.PortMidiStream *output;
		private CsPortMidiApi.PmEvent buffer;
		protected internal int timestamp; // remember timestamp from incoming messages
		protected internal bool trace = false; // used to print midi msgs for debugging


		public CsPortMidi()
		{
			if (pmRefCount == 0)
			{
				pm = new CsPortMidiApi();
				pmRefCount++;
				checkError(CsPortMidiApi.Pm_Initialize());
			}
			buffer = new CsPortMidiApi.PmEvent();
		}

		public virtual bool Trace
		{
			get
			{
				return trace;
			}
		}

		// set the trace flag and return previous value
		public virtual bool setTrace(bool flag)
		{
			bool previous = trace;
			trace = flag;
			return previous;
		}

		// WARNING: you must not call this if any devices are open
		public virtual void refreshDeviceLists()
		{
			if (openCount > 0)
			{
				throw new CsPortMidiException(pmStreamOpen, "RefreshDeviceLists called while stream is open");
			}
			checkError(CsPortMidiApi.Pm_Terminate());
			checkError(CsPortMidiApi.Pm_Initialize());
		}

		// there is no control over when/whether this is called, but it seems
		// to be a good idea to close things when this object is collected
		unsafe ~CsPortMidi()
		{
			if (input != null)
			{
				error = CsPortMidiApi.Pm_Close(input);
			}
			if (input != null)
			{
				int rslt = CsPortMidiApi.Pm_Close(output);
				// we may lose an error code from closing output, but don't
				// lose any real error from closing input...
				if (error == pm.pmNoError)
				{
					error = rslt;
				}
			}
			pmRefCount--;
			if (pmRefCount == 0)
			{
				error = CsPortMidiApi.Pm_Terminate();
			}
		}

		internal virtual int checkError(int err)
		{
			// note that Pm_Read and Pm_Write return positive result values 
			// which are not errors, so compare with >=
			if (err >= pm.pmNoError)
			{
				return err;
			}
			if (err == pm.pmHostError)
			{
				throw new CsPortMidiException(err, CsPortMidiApi.Pm_GetHostErrorText());
			}
			else
			{
				throw new CsPortMidiException(err, CsPortMidiApi.Pm_GetErrorText(err));
			}
		}

		// ******** ACCESS TO TIME ***********

		public virtual void timeStart(int resolution)
		{
			checkError(CsPortMidiApi.Pt_TimeStart(resolution));
		}

		public virtual void timeStop()
		{
			checkError(CsPortMidiApi.Pt_TimeStop());
		}

		public virtual int timeGet()
		{
            return CsPortMidiApi.Pt_Time();
		}

		public virtual bool timeStarted()
		{
			return CsPortMidiApi.Pt_TimeStarted();
		}

		// ******* QUERY DEVICE INFORMATION *********

		public virtual int countDevices()
		{
			return checkError(CsPortMidiApi.Pm_CountDevices());
		}

		public virtual int DefaultInputDeviceID
		{
			get
			{
				return checkError(CsPortMidiApi.Pm_GetDefaultInputDeviceID());
			}
		}

		public virtual int DefaultOutputDeviceID
		{
			get
			{
				return checkError(CsPortMidiApi.Pm_GetDefaultOutputDeviceID());
			}
		}

		unsafe public virtual string getDeviceInterf(int i)
		{
            string strInterf;
            CsPortMidiApi.PmDeviceInfo* dev = (CsPortMidiApi.PmDeviceInfo*)CsPortMidiApi.Pm_GetDeviceInfo(i);
            IntPtr pU = dev->interf;
            if (pU == IntPtr.Zero)
                strInterf = null;
            else
                strInterf =Marshal.PtrToStringAnsi(pU);
            return strInterf;
        }

		unsafe public virtual string getDeviceName(int i)
		{
            string strName;
            CsPortMidiApi.PmDeviceInfo* dev = (CsPortMidiApi.PmDeviceInfo*)CsPortMidiApi.Pm_GetDeviceInfo(i);
            IntPtr pU = dev->name;
            if (pU == IntPtr.Zero)
                strName = null;
            else
                strName = Marshal.PtrToStringAnsi(pU);
            return strName;
		}

		unsafe public virtual bool getDeviceInput(int i)
		{
            bool result;
            CsPortMidiApi.PmDeviceInfo* dev = (CsPortMidiApi.PmDeviceInfo*)CsPortMidiApi.Pm_GetDeviceInfo(i);
            if (dev == null)
                result = false;
            else
            {
                result = dev->input == 1 ? true : false;
            }
            return result;
		}

		unsafe public virtual bool getDeviceOutput(int i)
		{
            bool result;
            CsPortMidiApi.PmDeviceInfo* dev = (CsPortMidiApi.PmDeviceInfo*)CsPortMidiApi.Pm_GetDeviceInfo(i);
            if (dev == null)
                result = false;
            else
            {
                result = dev->output == 1 ? true : false;
            }
            return result;
        }

        // ********** MIDI INTERFACE ************

        unsafe public virtual bool OpenInput
		{
			get
			{
				return input != null;
			}
		}

		public virtual void openInput(int inputDevice, int bufferSize)
		{
			openInput(inputDevice, "", bufferSize);
		}

		unsafe public virtual void openInput(int inputDevice, string inputDriverInfo, int bufferSize)
		{
            CsPortMidiApi.PortMidiStream stream;

            if (OpenInput)
			{
                CsPortMidiApi.Pm_Close(input);
			}
			else
			{
                stream = new CsPortMidiApi.PortMidiStream();
                input = &stream;
			}
			if (trace)
			{
				Console.WriteLine("openInput " + getDeviceName(inputDevice));
			}
            fixed (CsPortMidiApi.PortMidiStream** p = &input)
            {
                checkError(CsPortMidiApi.Pm_OpenInput(p, inputDevice, inputDriverInfo, bufferSize));
            }
			// if no exception, then increase count of open streams
			openCount++;
		}

		unsafe public virtual bool OpenOutput
		{
			get
			{
				return output != null;
			}
		}

		public virtual void openOutput(int outputDevice, int bufferSize, int latency)
		{
			openOutput(outputDevice, "", bufferSize, latency);
		}

		unsafe public virtual void openOutput(int outputDevice, string outputDriverInfo, int bufferSize, int latency)
		{
            CsPortMidiApi.PortMidiStream stream;

            if (OpenOutput)
			{
                CsPortMidiApi.Pm_Close(output);
			}
			else
			{
                stream = new CsPortMidiApi.PortMidiStream();
                output = &stream;
			}
			if (trace)
			{
				Console.WriteLine("openOutput " + getDeviceName(outputDevice));
			}
            fixed (CsPortMidiApi.PortMidiStream** p = &output)
            {
                checkError(CsPortMidiApi.Pm_OpenOutput(p, outputDevice, outputDriverInfo, bufferSize, latency));
            }
			// if no exception, then increase count of open streams
			openCount++;
		}

		unsafe public virtual int Filter
		{
			set
			{
				if (input == null)
				{
					return; // no effect if input not open
				}
				checkError(CsPortMidiApi.Pm_SetFilter(input, value));
			}
		}

		unsafe public virtual int ChannelMask
		{
			set
			{
				if (input == null)
				{
					return; // no effect if input not open
				}
                checkError(CsPortMidiApi.Pm_SetChannelMask(input, value));
            }
		}

		unsafe public virtual void abort()
		{
			if (output == null)
			{
				return; // no effect if output not open
			}
            checkError(CsPortMidiApi.Pm_Abort(output));
        }

		// In keeping with the idea that this class represents an input and output,
		// there are separate Close methods for input and output streams, avoiding
		// the need for clients to ever deal directly with a stream object
		unsafe public virtual void closeInput()
		{
			if (input == null)
			{
				return; // no effect if input not open
			}
			checkError(CsPortMidiApi.Pm_Close(input));
			input = null;
			openCount--;
		}

		unsafe public virtual void closeOutput()
		{
			if (output == null)
			{
				return; // no effect if output not open
			}
			checkError(CsPortMidiApi.Pm_Close(output));
			output = null;
			openCount--;
		}

		// Poll should be called by client to process input messages (if any)
		unsafe public virtual void poll()
		{
			if (input == null)
			{
				return; // does nothing until input is opened
			}
			while (true)
			{
				int rslt = CsPortMidiApi.Pm_Read(input, buffer);
				checkError(rslt);
				if (rslt == 0)
				{
					return; // no more messages
				}
				handleMidiIn(buffer);
			}
		}

		unsafe public virtual void writeShort(int @when, int msg)
		{
			if (output == null)
			{
				throw new CsPortMidiException(pmOutputNotOpen, "Output stream not open");
			}
			if (trace)
			{
				Console.WriteLine("writeShort: 0x" + msg.ToString("X8"));
			}
			checkError(CsPortMidiApi.Pm_WriteShort(output, @when, msg));
		}

		unsafe public virtual void writeSysEx(int @when, sbyte[] msg)
		{
			if (output == null)
			{
				throw new CsPortMidiException(pmOutputNotOpen, "Output stream not open");
			}
			if (trace)
			{
				Console.Write("writeSysEx: ");
				for (int i = 0; i < msg.Length; i++)
				{
					Console.Write(msg[i].ToString("x"));
				}
				Console.Write("\n");
			}
			checkError(CsPortMidiApi.Pm_WriteSysEx(output, @when, msg));
		}

		public virtual int midiChanMessage(int chan, int status, int data1, int data2)
		{
			return (((data2 << 16) & 0xFF0000) | ((data1 << 8) & 0xFF00) | (status & 0xF0) | (chan & 0xF));
		}

		public virtual int midiMessage(int status, int data1, int data2)
		{
			return ((((data2) << 16) & 0xFF0000) | (((data1) << 8) & 0xFF00) | ((status) & 0xFF));
		}

		public virtual void midiAllOff(int channel)
		{
			midiAllOff(channel, NOW);
		}

		public virtual void midiAllOff(int chan, int @when)
		{
			writeShort(@when, midiChanMessage(chan, MIDI_CONTROL, CTRL_ALL_OFF, 0));
		}

		public virtual void midiPitchBend(int chan, int value)
		{
			midiPitchBend(chan, value, NOW);
		}

		public virtual void midiPitchBend(int chan, int value, int @when)
		{
			writeShort(@when, midiChanMessage(chan, MIDI_PITCH_BEND, value, value >> 7));
		}

		public virtual void midiClock()
		{
			midiClock(NOW);
		}

		public virtual void midiClock(int @when)
		{
			writeShort(@when, midiMessage(MIDI_CLOCK, 0, 0));
		}

		public virtual void midiControl(int chan, int control, int value)
		{
			midiControl(chan, control, value, NOW);
		}

		public virtual void midiControl(int chan, int control, int value, int @when)
		{
			writeShort(@when, midiChanMessage(chan, MIDI_CONTROL, control, value));
		}

		public virtual void midiNote(int chan, int pitch, int vel)
		{
			midiNote(chan, pitch, vel, NOW);
		}

		public virtual void midiNote(int chan, int pitch, int vel, int @when)
		{
			writeShort(@when, midiChanMessage(chan, MIDI_NOTE_ON, pitch, vel));
		}

		public virtual void midiProgram(int chan, int program)
		{
			midiProgram(chan, program, NOW);
		}

		public virtual void midiProgram(int chan, int program, int @when)
		{
			writeShort(@when, midiChanMessage(chan, MIDI_PROGRAM, program, 0));
		}

		public virtual void midiStart()
		{
			midiStart(NOW);
		}

		public virtual void midiStart(int @when)
		{
			writeShort(@when, midiMessage(MIDI_START, 0, 0));
		}

		public virtual void midiStop()
		{
			midiStop(NOW);
		}

		public virtual void midiStop(int @when)
		{
			writeShort(@when, midiMessage(MIDI_STOP, 0, 0));
		}

		public virtual void midiPolyTouch(int chan, int key, int value)
		{
			midiPolyTouch(chan, key, value, NOW);
		}

		public virtual void midiPolyTouch(int chan, int key, int value, int @when)
		{
			writeShort(@when, midiChanMessage(chan, MIDI_POLY_TOUCH, key, value));
		}

		public virtual void midiTouch(int chan, int value)
		{
			midiTouch(chan, value, NOW);
		}

		public virtual void midiTouch(int chan, int value, int @when)
		{
			writeShort(@when, midiChanMessage(chan, MIDI_TOUCH, value, 0));
		}

		// ****** now we implement the message handlers ******

		// an array for incoming sysex messages that can grow. 
		// The downside is that after getting a message, we 

		private byte[] sysexBuffer = null;
		private int sysexBufferIndex = 0;

		internal virtual void sysexBufferReset()
		{
			sysexBufferIndex = 0;
			if (sysexBuffer == null)
			{
				sysexBuffer = new byte[256];
			}
		}

		internal virtual void sysexBufferCheck()
		{
			if (sysexBuffer.Length < sysexBufferIndex + 4)
			{
				byte[] bigger = new byte[sysexBuffer.Length * 2];
				for (int i = 0; i < sysexBufferIndex; i++)
				{
					bigger[i] = sysexBuffer[i];
				}
				sysexBuffer = bigger;
			}
			// now we have space to write some bytes
		}

		// call this to insert Sysex and EOX status bytes
		// call sysexBufferAppendBytes to insert anything else
		internal virtual void sysexBufferAppendStatus(byte status)
		{
			sysexBuffer[sysexBufferIndex++] = status;
		}

		internal virtual void sysexBufferAppendBytes(int msg, int len)
		{
			for (int i = 0; i < len; i++)
			{
				byte b = (byte) msg;
				if ((msg & 0x80) != 0)
				{
					if (b == 0xF7)
					{ // end of sysex
						sysexBufferAppendStatus(b);
						sysex(sysexBuffer, sysexBufferIndex);
						return;
					}
					// recursively handle embedded real-time messages
					CsPortMidiApi.PmEvent buffer = new CsPortMidiApi.PmEvent();
					buffer.timestamp = timestamp;
					buffer.message = b;
					handleMidiIn(buffer);
				}
				else
				{
					sysexBuffer[sysexBufferIndex++] = b;
				}
				msg = msg >> 8;
			}
		}

		internal virtual void sysexBegin(int msg)
		{
			sysexBufferReset(); // start from 0, we have at least 256 bytes now
			sysexBufferAppendStatus(unchecked((byte)(msg & 0xFF))); // first byte is special
			sysexBufferAppendBytes(msg >> 8, 3); // process remaining bytes
		}

		public virtual void handleMidiIn(CsPortMidiApi.PmEvent buffer)
		{
			if (trace)
			{
				Console.WriteLine("handleMidiIn: " + buffer.message.ToString("x"));
			}
			// rather than pass timestamps to every handler, where typically 
			// timestamps are ignored, just save the timestamp as a member
			// variable where methods can access it if they want it
			timestamp = buffer.timestamp;
			int status = buffer.message & 0xFF;
			if (status < 0x80)
			{
				sysexBufferCheck(); // make enough space
				sysexBufferAppendBytes(buffer.message, 4); // process 4 bytes
				return;
			}
			int command = status & 0xF0;
			int channel = status & 0x0F;
			int data1 = (buffer.message >> 8) & 0xFF;
			int data2 = (buffer.message >> 16) & 0xFF;
			switch (command)
			{
			case MIDI_NOTE_OFF:
				noteOff(channel, data1, data2);
				break;
			case MIDI_NOTE_ON:
				if (data2 > 0)
				{
					noteOn(channel, data1, data2);
					break;
				}
				else
				{
					noteOff(channel, data1);
				}
				break;
			case MIDI_CONTROL:
				control(channel, data1, data2);
				break;
			case MIDI_POLY_TOUCH:
				polyTouch(channel, data1, data2);
				break;
			case MIDI_TOUCH:
				touch(channel, data1);
				break;
			case MIDI_PITCH_BEND:
				pitchBend(channel, (data1 + (data2 << 7)) - 8192);
				break;
			case MIDI_PROGRAM:
				program(channel, data1);
				break;
			case 0xF0:
				switch (channel)
				{
				case 0:
					sysexBegin(buffer.message);
					break;
				case 1:
					mtcQuarterFrame(data1);
					goto case 2;
				case 2:
					songPosition(data1 + (data2 << 7));
					break;
				case 3:
					songSelect(data1);
					break;
				case 4: // unused
	                break;
				case 5: // unused
	                break;
				case 6:
					tuneRequest();
					break;
				case 7:
					sysexBufferAppendBytes(buffer.message, buffer.message);
					break;
				case 8:
					clock();
					break;
				case 9:
					tick();
					break;
				case 0xA:
					clockStart();
					break;
				case 0xB:
					clockContinue();
					break;
				case 0xC:
					clockStop();
					break;
				case 0xD: // unused
	                break;
				case 0xE:
					activeSense();
					break;
				case 0xF:
					reset();
					break;
				}
                break;
			}
		}

		// the value ranges from +8181 to -8192. The interpretation is 
		// synthesizer dependent. Often the range is +/- one whole step
		// (two semitones), but the range is usually adjustable within
		// the synthesizer.
		internal virtual void pitchBend(int channel, int value)
		{
			return;
		}
		internal virtual void control(int channel, int control, int value)
		{
			return;
		}
		internal virtual void noteOn(int channel, int pitch, int velocity)
		{
			return;
		}
		// you can handle velocity in note-off if you want, but the default
		// is to drop the velocity and call the simpler NoteOff handler
		internal virtual void noteOff(int channel, int pitch, int velocity)
		{
			noteOff(channel, pitch);
		}
		// if the subclass wants to implement NoteOff with velocity, it
		// should override the following to make sure all NoteOffs are handled
		internal virtual void noteOff(int channel, int pitch)
		{
			return;
		}
		internal virtual void program(int channel, int program)
		{
			return;
		}
		// the byte array may be bigger than the message, length tells how
		// many bytes in the array are part of the message
		internal virtual void sysex(byte[] msg, int length)
		{
			return;
		}
		internal virtual void polyTouch(int channel, int key, int value)
		{
			return;
		}
		internal virtual void touch(int channel, int value)
		{
			return;
		}
		internal virtual void mtcQuarterFrame(int value)
		{
			return;
		}
		// the value is a 14-bit integer representing 16th notes
		internal virtual void songPosition(int value)
		{
			return;
		}
		internal virtual void songSelect(int value)
		{
			return;
		}
		internal virtual void tuneRequest()
		{
			return;
		}
		internal virtual void clock()
		{
			return;
		} // represents 1/24th of a quarter note
		internal virtual void tick()
		{
			return;
		} // represents 10ms
		internal virtual void clockStart()
		{
			return;
		}
		internal virtual void clockStop()
		{
			return;
		}
		internal virtual void clockContinue()
		{
			return;
		}
		internal virtual void activeSense()
		{
			return;
		}
		internal virtual void reset()
		{
			return;
		}
    }

}