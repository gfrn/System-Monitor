
using DesktopNotifications;
using Microsoft.QueryStringDotNET;
using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;
namespace SystemMonitor
{
    [ClassInterface(ClassInterfaceType.None)]
    [ComSourceInterfaces(typeof(INotificationActivationCallback))]
    [Guid("50cfb67f-bc8a-477d-938c-93cf6bfb3320"), ComVisible(true)]
    public class MyNotificationActivator : NotificationActivator
    {
        private readonly MainWindow mw = new MainWindow();
        public override void OnActivated(string arguments, NotificationUserInput userInput, string appUserModelId)
        {
            Application.Current.Dispatcher.Invoke(delegate
            {
                if (arguments.Length == 0)
                {
                    OpenWindowIfNeeded();
                    return;
                }

                switch (arguments)
                {
                    case "change":
                        mw.arduinoPort.Close();
                        mw.arduinoPort = new SerialPort(userInput["portBox"], 9600);
                        mw.arduinoPort.Open();
                        break;
                    case "retry":
                        mw.arduinoPort.Open();
                        break;
                    case "stop":
                        Environment.Exit(0);
                        break;
                    default:
                        //
                        break;
                }
            });
        }

        private void OpenWindowIfNeeded()
        {
            if (App.Current.Windows.Count == 0)
            {
                new MainWindow().Show();
            }
            App.Current.Windows[0].Activate();

            App.Current.Windows[0].WindowState = WindowState.Normal;
        }
    }
}

