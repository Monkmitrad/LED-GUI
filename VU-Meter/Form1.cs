using NAudio;
using NAudio.Dsp;
using NAudio.Wave;
using NAudio.CoreAudioApi;
using NAudio.Utils;
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
using System.Threading;
using NAudio.Wave.SampleProviders;

namespace VU_Meter
{
    public partial class Form1 : Form
    {
        private SerialCommunicator _serial;

        static MMDeviceEnumerator enumerator = new MMDeviceEnumerator();
        static MMDevice device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);

        private WaveIn input;
        private SampleAggregator aggregator;
        int fftLength = 128;
        float[] peaks;


        public Form1()
        {
            InitializeComponent();
            InitializeComboBox();

            _serial = new SerialCommunicator();
            _serial.Connect();

            for (int i = 0; i < WaveIn.DeviceCount; i++)
            {
                WaveInCapabilities info = WaveIn.GetCapabilities(i);
                listBox1.Items.Add(i + " - " + info.ProductName);
            }
            listBox1.SelectedIndex = 0;

            peaks = new float[fftLength];

            aggregator = new SampleAggregator(fftLength: fftLength * 2);
            aggregator.PerformFFT = true;
            aggregator.FftCalculated += AggregatorCalculatedFFT;

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
            comboBox1.Items.Add("CustomColor");
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
            }
            else if (comboBox1.Text == "Running-Light-Right")
            {
                _serial.WriteRunning(true, trackBar3.Value);
            }
        }

        private void button2_Click(object sender, EventArgs e)
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

        private void button3_Click(object sender, EventArgs e)
        {
            input = new WaveIn { DeviceNumber = 5 };
            //input = new WasapiCapture(device);
            input.DataAvailable += WaveInDataVailable;
            input.StartRecording();
            
        }

        private void WaveInDataVailable(object sender, WaveInEventArgs e)
        {
            uint val = 0;

            int bps = input.WaveFormat.BitsPerSample / 8;
            for (int i = 0; i < e.BytesRecorded; i += bps)
            {
                switch (input.WaveFormat.BitsPerSample)     // Getting the current value depending on the wave resolution
                {
                    case 16:
                        val = BitConverter.ToUInt16(e.Buffer, i);
                        break;

                    case 24:
                        val = Convert.ToUInt32(e.Buffer[i] + (e.Buffer[i + 1] << 8) + (e.Buffer[i + 2] << 16));
                        break;

                    case 32:
                        val = BitConverter.ToUInt32(e.Buffer, i);
                        break;
                }

                //float real = (float)(val * 2 / (Math.Pow(2, input.WaveFormat.BitsPerSample) - 1.0d) - 1.0f);    // converting the value to a range from -1 to +1 and add it to the fft aggregator
                float real = (float)(val / (Math.Pow(2, input.WaveFormat.BitsPerSample) - 1.0d));	// converting the value to a range from 0 to +1 and add it to the fft aggregator

                aggregator.Add(real);
            }
        }

        private void AggregatorCalculatedFFT(object sender, FftEventArgs e)
        {
            float max = 0;
            for (int i = 0; i < fftLength; i++)
            {
                float value = (float)Math.Sqrt(e.Result[i].X * e.Result[i].X + e.Result[i].Y * e.Result[i].Y);
                if (value < 0.1)
                {
                    value = 0;
                }
                peaks[i] = value;
                if (value > max)
                {
                    max = value;
                }
            }
            label4.Text = peaks[2].ToString();
            label5.Text = peaks[fftLength / 4].ToString();
            label6.Text = peaks[fftLength / 2].ToString();
            label7.Text = peaks[fftLength - 1].ToString();
            label8.Text = max.ToString();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            input.StopRecording();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (input != null)
            {
                input.StopRecording();
            }
            _serial.Disconnect();
        }
    }
}