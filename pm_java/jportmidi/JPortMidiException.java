// JPortMidiException -- thrown by JPortMidi methods

package jportmidi;

public class JPortMidiException extends Exception {
    private static final long serialVersionUID = 101010L;
    public int error = 0;
    public JPortMidiException(int err, String msg) {
        super(msg);
        error = err;
    }
}

