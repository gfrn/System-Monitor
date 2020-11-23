// ******************************************************************
// Copyright (c) Microsoft. All rights reserved.
// This code is licensed under the MIT License (MIT).
// THE CODE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
// IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
// TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH
// THE CODE OR THE USE OR OTHER DEALINGS IN THE CODE.
// ******************************************************************

using DesktopNotifications;
using Microsoft.QueryStringDotNET;
using Microsoft.Toolkit.Uwp.Notifications;
using OpenHardwareMonitor.Hardware;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Net.Http;
using System.Security.Principal;
using System.Text;
using System.Threading;
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
using SystemMonitor.Properties;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace SystemMonitor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 

    internal sealed class CpuTemperatureReader : IDisposable
    {
        private readonly Computer _computer;

        public CpuTemperatureReader()
        {
            _computer = new Computer { CPUEnabled = true, GPUEnabled = true };
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
        public SerialPort arduinoPort; // = new SerialPort("COM3", 9600);
        public MainWindow()
        {
            InitializeComponent();
            Thread MonThread = new Thread(new ThreadStart(MonitorUsage));

            MonThread.Start();
        }

        public void MonitorUsage()
        {
            while (true)
            {
                if (SerialPort.GetPortNames().ToList().Contains(Settings.Default.Port))
                {
                    arduinoPort = new SerialPort(Settings.Default.Port, 9600);
                    break;
                }
                else
                {
                    ShowNotification("Port error!", "Can't access port. Is it available? Try picking another port:", true, true);
                    Thread.Sleep(15000);
                }
            }

            if (!IsAdministrator()) { ShowNotification("Permission error!", "You are not an administrator. This may result in unwanted behavior or lacking sensors.", false, false); }

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
                    else
                    {
                        ShowNotification("Comm error!", "Port was closed", true, true); break;
                    }
                }
                if (arduinoPort.IsOpen) { arduinoPort.Write("\n"); }

                Thread.Sleep(5000);
            }
        }
        public static bool IsAdministrator()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private void ShowNotification(string title, string message, bool retry, bool changePort)
        {
            ToastContent toastContent = new ToastContent()
            {
                Visual = new ToastVisual()
                {
                    BindingGeneric = new ToastBindingGeneric()
                    {
                        Children =
                        {
                            new AdaptiveText()
                            {
                                Text = title
                            },

                            new AdaptiveText()
                            {
                                Text = message
                            }
                        }
                    }
                }
            };

            ToastActionsCustom buttons = new ToastActionsCustom();

            buttons.Buttons.Add(new ToastButton("Stop", "stop"));
            if (retry) { buttons.Buttons.Add(new ToastButton("Retry", "retry")); }
            if (changePort)
            {
                buttons.Buttons.Add(new ToastButton("Change Port", "changePort"));

                ToastSelectionBox portBox = new ToastSelectionBox("portBox");

                foreach (var port in SerialPort.GetPortNames())
                {
                    portBox.Items.Add(new ToastSelectionBoxItem(port, port));
                }
                buttons.Inputs.Add(portBox);
            }

            toastContent.Actions = buttons;
            var doc = new XmlDocument();
            doc.LoadXml(toastContent.GetContent());

            var toast = new ToastNotification(doc);

            DesktopNotificationManagerCompat.CreateToastNotifier().Show(toast);
        }

        private void ButtonClearToasts_Click(object sender, RoutedEventArgs e)
        {
            DesktopNotificationManagerCompat.History.Clear();
        }
    }
}
