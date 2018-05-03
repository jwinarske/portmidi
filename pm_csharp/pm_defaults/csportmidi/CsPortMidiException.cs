using System;

// CSPortMidiException -- thrown by CsPortMidi methods

namespace csportmidi
{

	public class CsPortMidiException : Exception
	{
		public int error = 0;
		public CsPortMidiException(int err, string msg) : base(msg)
		{
			error = err;
		}
	}


}