using OpenHardwareMonitor.Hardware;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;
using System.Security.Principal;
using System.IO.Ports;

namespace System_Monitor
{
    internal sealed class CpuTemperatureReader : IDisposable
    {
        private readonly Computer _computer;

        public CpuTemperatureReader()
        {
            _computer = new Computer { CPUEnabled = true, GPUEnabled = true};
            _computer.Open();
        }

        public IReadOnlyDictionary<string, float> GetTemperaturesInCelsius()
        {
            var coreAndTemperature = new Dictionary<string, float>();
            foreach (var hardware in _computer.Hardware)
            {
                hardware.Update();
                foreach (var sensor in hardware.Sensors)
                {
                    if ((sensor.SensorType == SensorType.Temperature || sensor.SensorType == SensorType.Power && sensor.Name.Contains("GPU")) && sensor.Value.HasValue)
                        coreAndTemperature.Add(sensor.Name, sensor.Value.Value);
                }
            }

            return coreAndTemperature;
        }

        public void Dispose()
        {
            try
            {
                _computer.Close();
            }
            catch (Exception)
            {
                //ignore closing errors
            }
        }
    }
    public partial class MainWindow : Window
    {
        TaskCompletionSource<bool> keepExecuting = null;
        SerialPort arduinoPort; // = new SerialPort("COM3", 9600);
        public static bool IsAdministrator()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        public void showNotification(string title, string message)
        {
            string toastXmlString =
            $@"<toast><visual>
            <binding template='ToastGeneric'>
            <text>{title}</text>
            <text>{message}</text>
            </binding>
            </visual></toast>";

            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(toastXmlString);

            var toastNotification = new ToastNotification(xmlDoc);

            var toastNotifier = ToastNotificationManager.CreateToastNotifier();
            toastNotifier.Show(toastNotification);

            toastNotification.Activated += toastNotification_Activated;
        }

        private void toastNotification_Activated(ToastNotification sender, object args)
        {
            string content = sender.Content.FirstChild.InnerText.ToString();
            if (content.Contains("Port error!") || content.Contains("Permission error!"))
            {
                Environment.Exit(0);
            }
            else
            {
                arduinoPort.Open();
            }
        }

        public MainWindow()
        {
            while (true)
            {
                if(SerialPort.GetPortNames().ToList().Contains("COM3"))
                {
                    arduinoPort = new SerialPort("COM3", 9600);
                    break;
                }
                else
                {
                    showNotification("Port error!", "Can't access port. Is it connected/being used by something else? Retrying in 15 seconds. Click to stop.");
                    Thread.Sleep(15000);
                }
            }

            if (!IsAdministrator()) { showNotification("Permission error!", "You are not an administrator. This may result in unwanted behavior or lacking sensors. Click to stop."); }
            
            CpuTemperatureReader cpu = new CpuTemperatureReader();
            while (!arduinoPort.IsOpen)
            {
              arduinoPort.Open();
            }

            while (true)
            {
                var sensors = cpu.GetTemperaturesInCelsius();

                foreach (KeyValuePair<string, float> sensorRead in sensors)
                {
                    if (arduinoPort.IsOpen) { arduinoPort.Write(sensorRead.Value.ToString("0") + ":"); }
                    else { showNotification("Comm error!", "Port was closed. Click to try connecting."); break; }
                }
                if (arduinoPort.IsOpen) { arduinoPort.Write("\n"); }

                Thread.Sleep(5000);
            }
        }
    }
}
