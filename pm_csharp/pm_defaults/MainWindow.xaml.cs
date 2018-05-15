using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using csportmidi;
using System.Text.RegularExpressions;

namespace pmdefaults
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // inputIds maps from the index of an item in inputSelection to the
        // device ID. Used to open the selected device.
        private List<int?> inputIds;
        // analogous to inputIds, outputIds maps selection index to device ID
        private List<int?> outputIds;
        private ActivityLight _inputActivity;
        private JPM jpm;

        public MainWindow()
        {
            InitializeComponent();

            _inputActivity = new ActivityLight(this);

            inputIds = new List<int?>();
            outputIds = new List<int?>();

            /*
            timer = new Timer(50, this); // ms
            timer.addActionListener(this);
            */
            try
            {
                jpm = new JPM(this, _inputActivity, this);
                jpm.setTrace(true);
                loadDeviceChoices();
                 /*
                timer.start(); // don't start timer if there's an error
                */
            }
            catch (CsPortMidiException e)
            {
                Console.WriteLine(e);
            }
        }

        // This class extends CsPortMidi in order to override midi input handling
        // In this case, midi input simply blinks the activity light
        public class JPM : CsPortMidi
        {
            private readonly MainWindow outerInstance;

            internal ActivityLight light;
            internal MainWindow frame;
            internal int lightTime;
            internal bool lightState;
            internal int now; // current time in ms
            internal readonly int HALF_BLINK_PERIOD = 250; // ms

            public JPM(MainWindow outerInstance, ActivityLight al, MainWindow df)
            {
                this.outerInstance = outerInstance;
                light = al;
                frame = df;
                lightTime = 0;
                lightState = false; // meaning "off"
                now = 0;
            }

            public virtual void poll(int ms)
            {
                // blink the light. lightState is initially false (off).
                // to blink the light, set lightState to true and lightTime
                // to now + 0.25s; turn on light
                // now > ligntTime && lightState => set lightTime = now + 0.25s;
                //                                  set lightState = false
                //                                  turn off light
                // light can be blinked again when now > lightTime && !lightState
                now = ms;
                if (now > lightTime && lightState)
                {
                    lightTime = now + HALF_BLINK_PERIOD;
                    lightState = false;
                    light.State = false;
                }
                base.poll();
            }

            public override void handleMidiIn(CsPortMidiApi.PmEvent buffer)
            {
                Console.WriteLine("midi in: now " + now + " lightTime " + lightTime + " lightState " + lightState);
                if (now > lightTime && !lightState)
                {
                    lightState = true;
                    lightTime = now + HALF_BLINK_PERIOD;
                    light.State = true;
                }
            }
        }

        public class ActivityLight
        {
            private readonly MainWindow outerInstance;
            internal Brush color;
            //            public readonly Brush OFF_COLOR = Brush.Color.FromRgb(0, 0, 0);
            //            public readonly Brush ON_COLOR = Color.FromRgb(0, 255, 0);

            internal ActivityLight(MainWindow outerInstance) : base()
            {
                this.outerInstance = outerInstance;
                //                color = OFF_COLOR;
                Console.WriteLine("ActivityLight "); // + Size
            }

            public virtual bool State
            {
                set
                {
                    //                    color = (value ? ON_COLOR : OFF_COLOR);
                    outerInstance.inputActivity.Fill = color;
                }
            }
        }

        private void onRefreshButton(object sender, RoutedEventArgs e)
        {
            if (jpm.OpenInput)
            {
                jpm.closeInput();
            }
            if (jpm.OpenOutput)
            {
                jpm.closeOutput();
            }
            jpm.refreshDeviceLists();
            loadDeviceChoices();
        }

        private void onUpdateButton(object sender, RoutedEventArgs e)
        {
            savePreferences();
        }

        private void onCloseButton(object sender, RoutedEventArgs e)
        {
            if (jpm.OpenInput)
            {
                jpm.closeInput();
            }
            if (jpm.OpenOutput)
            {
                jpm.closeOutput();
            }
        }

        private void onTestButton(object sender, RoutedEventArgs e)
        {
            sendTestMessages();
        }

        private void onInputSelection(object sender, SelectionChangedEventArgs e)
        {
            int id = inputSelection.SelectedIndex;

            if (id < 0)
                return; // nothing selected

            id = inputIds[id].Value; // map to device ID
                                     // openInput will close any previously open input stream
            try
            {
                jpm.openInput(id, 100); // buffer size hopes to avoid overflow
            }
            catch (CsPortMidiException exception)
            {
                Console.WriteLine(exception);
            }
        }

        private void onOutputSelection(object sender, SelectionChangedEventArgs e)
        {
            jpm.closeOutput(); // remains closed until Test button reopens
        }

        internal virtual void loadDeviceChoices()
        {
            // initialize and load combo boxes with device descriptions
            inputSelection.Items.Clear();
            inputIds.Clear();
            outputSelection.Items.Clear();
            outputIds.Clear();

            int n = jpm.countDevices();
            for (int i = 0; i < n; i++)
            {
                string interf = jpm.getDeviceInterf(i);
                string name = jpm.getDeviceName(i);
                string selection = name + " [" + interf + "]";
                if (jpm.getDeviceInput(i))
                {
                    inputIds.Add(i);
                    inputSelection.Items.Add(selection);
                    inputSelection.SelectedIndex = 0;
                }
                else
                {
                    outputIds.Add(i);
                    outputSelection.Items.Add(selection);
                    outputSelection.SelectedIndex = 0;
                }
            }
        }

        internal virtual void sendTestMessages()
        {
            try
            {
                if (!jpm.OpenOutput)
                {
                    int id = outputSelection.SelectedIndex;
                    if (id < 0)
                    {
                        return; // nothing selected
                    }
                    id = outputIds[id].Value;
                    jpm.openOutput(id, 10, 10);
                }
                jpm.midiNote(0, 67, 100, 0); // send an A (440)
                jpm.midiNote(0, 67, 0, jpm.timeGet() + 65535);
            }
            catch (CsPortMidiException e)
            {
                Console.WriteLine(e);
            }
        }

        // make a string to put into preferences describing this device
        internal virtual string makePrefName(int id)
        {
            string name = jpm.getDeviceName(id);
            string interf = jpm.getDeviceInterf(id);
            // the syntax requires comma-space separator (see portmidi.h)
            return interf + ", " + name;
        }

        public virtual void savePreferences()
        {
            const string userRoot = "HKEY_CURRENT_USER\\Software\\JavaSoft\\Prefs";
            const string subkey = "/Port/Midi";
            const string keyName = userRoot + "\\" + subkey;

            int id = outputSelection.SelectedIndex;
            if (id >= 0)
            {
                string prefName = makePrefName(outputIds[id].Value);
                Console.WriteLine("PM_RECOMMENDED_OUTPUT_DEVICE: " + prefName);
                try 
                {
                    string key = Regex.Replace("PM_RECOMMENDED_OUTPUT_DEVICE", @"([A-Z])", "/$1");
                    string value = Regex.Replace(prefName, @"([A-Z])", "/$1");
                    Registry.SetValue(keyName, key, value, RegistryValueKind.String);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }

            id = inputSelection.SelectedIndex;
            if (id >= 0)
            {
                string prefName = makePrefName(inputIds[id].Value);
                Console.WriteLine("PM_RECOMMENDED_INPUT_DEVICE: " + prefName);
                try
                {
                    string key = Regex.Replace("PM_RECOMMENDED_INPUT_DEVICE", @"([A-Z])", "/$1");
                    string value = Regex.Replace(prefName, @"([A-Z])", "/$1");
                    Registry.SetValue(keyName, key, value, RegistryValueKind.String);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

    }
}
