using NAudio.CoreAudioApi;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace VU_Meter
{
    public partial class Form1 : Form
    {
        private SerialCommunicator _serial;

        static MMDeviceEnumerator enumerator = new MMDeviceEnumerator();
        static MMDevice device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);
        public Form1()
        {
            InitializeComponent();
            InitializeComboBox();
            _serial = new SerialCommunicator();
            _serial.Connect();

            
        }


        private void Timer1_Tick(object sender, EventArgs e)
        {
            float leftVolume = device.AudioMeterInformation.PeakValues[0];
            float rightVolume = device.AudioMeterInformation.PeakValues[1];
            float mapLeft = MapValue(leftVolume);
            float mapRight = MapValue(rightVolume);
            trackBar1.Value = Convert.ToInt32(mapLeft);
            trackBar2.Value = Convert.ToInt32(mapRight);
            label1.Text = mapLeft.ToString() + " | " + mapRight.ToString();
            panel1.BackColor = Color.FromArgb(Convert.ToInt32(mapLeft), Color.Green);
            panel2.BackColor = Color.FromArgb(Convert.ToInt32(mapRight), Color.Red);

            _serial.WriteVU(Convert.ToInt32(mapLeft), Convert.ToInt32(mapRight));
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
            comboBox1.Items.Add("WHITE");
            comboBox1.Items.Add("RED");
            comboBox1.Items.Add("GREEN");
            comboBox1.Items.Add("BLUE");
            comboBox1.Items.Add("VU-Meter");
            comboBox1.Items.Add("Running-Light-Left");
            comboBox1.Items.Add("Running-Light-Right");
        }

        private void comboBox1_SelectedValueChanged(object sender, EventArgs e)
        {
            if (timer1.Enabled)
            {
                timer1.Stop();
            }
            switch (comboBox1.Text)
            {
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
                default:
                    break;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (timer1.Enabled)
            {
                timer1.Stop();
            }
            
        }

        private void trackBar3_Scroll(object sender, EventArgs e)
        {
            if (comboBox1.Text == "Running-Light-Left")
            {
                _serial.WriteRunning(false, trackBar3.Value);
            } else if (comboBox1.Text == "Running-Light-Right")
            {
                _serial.WriteRunning(true, trackBar3.Value);
            }
        }
    }
}
