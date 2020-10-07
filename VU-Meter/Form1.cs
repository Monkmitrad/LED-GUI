using NAudio.CoreAudioApi;
using NAudio.Wave;
using System;
using System.Linq;
using System.Windows.Forms;

namespace VU_Meter
{
    public partial class Form1 : Form
    {
        private readonly SerialCommunicator _serial;
        private static readonly MMDeviceEnumerator enumerator = new MMDeviceEnumerator();
        private static MMDeviceCollection devices;
        private static MMDevice device;

        private bool vuIntensity = false;

        public Form1()
        {
            InitializeComponent();
            InitializeComboBox();

            _serial = new SerialCommunicator();
            _serial.Connect();

            devices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
            listBox1.Items.AddRange(devices.ToArray());
            if (listBox1.Items.Count > 0)
            {
                listBox1.SelectedIndex = 0;
            }
        }


        private void Timer1_Tick(object sender, EventArgs e)
        {
            float leftVolume = device.AudioMeterInformation.PeakValues[0];
            float rightVolume = device.AudioMeterInformation.PeakValues[1];
            float mapLeft = MapValue(leftVolume);
            float mapRight = MapValue(rightVolume);
            trackBar1.Value = Convert.ToInt32(mapLeft);
            trackBar2.Value = Convert.ToInt32(mapRight);
            //label1.Text = mapLeft.ToString() + " | " + mapRight.ToString();

            if (vuIntensity)
            {
                // match brightness of strip with mono peak
                int intensity = Convert.ToInt32(MapValue(leftVolume + rightVolume / 2, 0, 1.5f, 0, 255));
                label1.Text = intensity.ToString();
                _serial.WriteVU(Convert.ToInt32(mapLeft), Convert.ToInt32(mapRight), intensity);
            } else
            {
                // just use audio channel values
                _serial.WriteVU(Convert.ToInt32(mapLeft), Convert.ToInt32(mapRight));
            }
            
        }

        private float MapValue(float x)
        {
            int x1 = 0;
            int x2 = 1;
            int y1 = 0;
            int y2 = 25;

            var m = (y2 - y1) / (x2 - x1);
            var c = y1 - m * x1; // point of interest: c is also equal to y2 - m * x2, though float math might lead to slightly different results.

            return m * x + c;
        }

        private float MapValue(float x, float x1, float x2, float y1, float y2)
        {
            var m = (y2 - y1) / (x2 - x1);
            var c = y1 - m * x1; // point of interest: c is also equal to y2 - m * x2, though float math might lead to slightly different results.

            return m * x + c;
        }

        private void InitializeComboBox()
        {
            comboBox1.Items.Clear();
            comboBox1.Items.Add("CustomColor");
            comboBox1.Items.Add("WHITE");
            comboBox1.Items.Add("RED");
            comboBox1.Items.Add("GREEN");
            comboBox1.Items.Add("BLUE");
            comboBox1.Items.Add("VU-Meter");
            comboBox1.Items.Add("Running-Light-Left");
            comboBox1.Items.Add("Running-Light-Right");
            comboBox1.Items.Add("ColorFade");
            comboBox1.Items.Add("Strobe");
        }

        private void ComboBox1_SelectedValueChanged(object sender, EventArgs e)
        {
            if (timer1.Enabled)
            {
                timer1.Stop();
            }
            switch (comboBox1.Text)
            {
                case "CustomColor":
                    _serial.WriteRGB(colorDialog1.Color.R, colorDialog1.Color.G, colorDialog1.Color.B);
                    break;
                case "WHITE":
                    _serial.WriteRGB(100, 100, 100);
                    break;
                case "RED":
                    _serial.WriteRGB(100, 0, 0);
                    break;
                case "GREEN":
                    _serial.WriteRGB(0, 100, 0);
                    break;
                case "BLUE":
                    _serial.WriteRGB(0, 0, 100);
                    break;
                case "VU-Meter":
                    timer1.Start();
                    break;
                case "Running-Light-Left":
                    _serial.WriteRunning(false, trackBar3.Value);
                    break;
                case "Running-Light-Right":
                    _serial.WriteRunning(true, trackBar3.Value);
                    break;
                case "ColorFade":
                    _serial.WriteFade(trackBar3.Value);
                    break;
                case "Strobe":
                    _serial.WriteStrobe(trackBar5.Value, trackBar6.Value);
                    break;
                default:
                    break;
            }
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            if (timer1.Enabled)
            {
                timer1.Stop();
            }
        }

        private void TrackBar3_Scroll(object sender, EventArgs e)
        {
            switch (comboBox1.Text)
            {
                case "Running-Light-Left":
                    _serial.WriteRunning(false, trackBar3.Value);
                    break;
                case "Running-Light-Right":
                    _serial.WriteRunning(true, trackBar3.Value);
                    break;
                case "ColorFade":
                    _serial.WriteFade(trackBar3.Value);
                    break;
            }
        }

        private void Button2_Click(object sender, EventArgs e)
        {
            if (colorDialog1.ShowDialog() == DialogResult.OK)
            {
                label3.Text = colorDialog1.Color.ToString();
                if (comboBox1.Text == "CustomColor")
                {
                    _serial.WriteRGB(colorDialog1.Color.R, colorDialog1.Color.G, colorDialog1.Color.B);
                }
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            _serial.Disconnect();
        }

        private void CheckBox1_CheckedChanged(object sender, EventArgs e)
        {
            vuIntensity = checkBox1.Checked;
        }

        private void ListBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            device = devices[listBox1.SelectedIndex];
        }
    }
}