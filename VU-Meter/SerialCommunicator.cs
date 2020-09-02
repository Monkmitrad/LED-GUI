using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace VU_Meter
{
    class SerialCommunicator
    {
        private SerialPort _serialPort;
        private string _comPort;
        private readonly int _baudRate = 9600;

        public void Connect()
        {
            var instances = new ManagementClass("Win32_SerialPort").GetInstances();

            foreach (var instance in instances)
            {
                string description = instance["Description"].ToString();
                string id = instance["DeviceID"].ToString();

                if (description.ToLower().Contains("arduino"))
                {
                    _comPort = id;
                }
            }
            try
            {
                _serialPort = new SerialPort(_comPort, _baudRate, Parity.None, 8, StopBits.One)
                {
                    NewLine = "\r\n",
                    DtrEnable = false,
                    RtsEnable = false
                };
                _serialPort.Open();
            }
            catch (InvalidOperationException)
            {

                throw;
            }
        }

        public void WriteRGB(int r, int g, int b)
        {
            Write("C," + r + "," + g + "," + b, false);
        }

        public void WriteVU(int l, int r)
        {
            Write("V," + l + "," + r, false);
        }

        public void WriteVU(int l, int r, int intensity)
        {
            Write("V," + l + "," + r + ',' + intensity, false);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="direction">false = left; true = right</param>
        public void WriteRunning(bool direction, int speed)
        {
            if (direction)
            {
                //Right
                Write("R" + "," + speed, false);
            } else
            {
                //Left
                Write("L" + "," + speed, false);
            }
            
        }

        public void WriteLine(string text)
        {
            Write(text, true);
        }

        private void Write(string text, bool newline)
        {
            try
            {
                if (_serialPort != null)
                {
                    if (text != null)
                        _serialPort.Write("<" + text + ">");
                    if (newline)
                        _serialPort.Write("\n");
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Fehler beim Schreiben!", ex);
            }
        }

        public void Disconnect()
        {
            if (_serialPort.IsOpen)
            {
                _serialPort.Close();
            }
        }
    }
}
