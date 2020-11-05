using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Management;
using System.Net;
using System.Net.Sockets;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Windows.Forms.VisualStyles;

namespace WindowsService1
{
    public partial class MDMService : ServiceBase
    {
        private Boolean Stop = false;
        private Socket listener;

        public MDMService()
        {
            InitializeComponent();

            eventLog1 = new System.Diagnostics.EventLog();
            if (!System.Diagnostics.EventLog.SourceExists("MDM WMI Service"))
            {
                System.Diagnostics.EventLog.CreateEventSource(
                    "MDM WMI Service", "MDMWMI");
            }
            
            eventLog1.Source = "MDM WMI Service";
            eventLog1.Log = "MDMWMI";
        }

        protected override void OnStart(string[] args)
        {
            eventLog1.WriteEntry("In OnStart.");

            // Update the service state to Start Pending.
            ServiceStatus serviceStatus = new ServiceStatus();
            serviceStatus.dwCurrentState = ServiceState.SERVICE_START_PENDING;
            serviceStatus.dwWaitHint = 100000;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);

            new Thread(new ThreadStart(StartListening)).Start();

            // Update the service state to Running.
            serviceStatus.dwCurrentState = ServiceState.SERVICE_RUNNING;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);
        }

        protected override void OnStop()
        {
            eventLog1.WriteEntry("In OnStop.");

            // Update the service state to Stop Pending.
            ServiceStatus serviceStatus = new ServiceStatus();
            serviceStatus.dwCurrentState = ServiceState.SERVICE_STOP_PENDING;
            serviceStatus.dwWaitHint = 100000;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);

            StopListening();

            // Update the service state to Stopped.
            serviceStatus.dwCurrentState = ServiceState.SERVICE_STOPPED;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);
        }

        protected override void OnCustomCommand(int command)
        {
            eventLog1.WriteEntry("Received command " + command + " successfully.");

            switch (command)
            {
                case 128:
                    break;
                default:
                    break;
            }
        }

        public void StopListening()
        {
            Stop = true;
            if (listener != null)
                listener.Close();
        }

        public void StartListening()
        {
            // Data buffer for incoming data.  
            byte[] bytes = new Byte[1024];

            // Establish the local endpoint for the socket.  
            // Dns.GetHostName returns the name of the
            // host running the application.  
            IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress ipAddress = ipHostInfo.AddressList[0];
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 11000);

            // Create a TCP/IP socket.  
            listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            // Bind the socket to the local endpoint and
            // listen for incoming connections.  
            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(10);

                String data;
                // Start listening for connections.  
                while (!Stop)
                {
                    eventLog1.WriteEntry("Waiting for a connection...");

                    // Program is suspended while waiting for an incoming connection.  
                    Socket handler = listener.Accept();
                    data = null;

                    // An incoming connection needs to be processed.  
                    while (!Stop)
                    {
                        int bytesRec = handler.Receive(bytes);
                        data += Encoding.ASCII.GetString(bytes, 0, bytesRec);

                        int idx = data.IndexOf("<EOF>");
                        if (idx > -1)
                        {
                            data = data.Substring(0, idx);
                            break;
                        }
                    }

                    // Show the data on the console.  
                    eventLog1.WriteEntry("Command received : " + data);

                    String reply = processCommand(data);

                    // Echo the data back to the client.  
                    byte[] msg = Encoding.ASCII.GetBytes(reply);

                    handler.Send(msg);
                    handler.Shutdown(SocketShutdown.Both);
                    handler.Close();
                }

            }
            catch (Exception e)
            {
                if (!Stop)
                    eventLog1.WriteEntry("Failed to process message " + e.ToString());
            }

        }

        private String processCommand(String cmd)
        {
            SelectQuery osQuery;
            ManagementScope mgmtScope;
            String reply = "";

            String[] data = cmd.Split(':');
            switch (data[0])
            {
                case "GetCamera":

                    //  If available... not on my PCs...
                    //
                    /*osQuery = new SelectQuery("MDM_Policy_Config01_Camera02");
                    mgmtScope = new ManagementScope("root\\cimv2\\mdm\\dmmap");
                    mgmtScope.Connect();
                  
                    var mgmtSrchr1 = new ManagementObjectSearcher(mgmtScope, osQuery);

                    foreach (var os in mgmtSrchr1.Get())
                    {
                        eventLog1.WriteEntry("Got result " + os);

                        foreach (PropertyData prop in os.Properties)
                        {
                            reply += prop.Name + ":" + prop.Value + "\r\n";

                            eventLog1.WriteEntry("Received property " + prop.Name + " = " + prop.Value);
                        }
                    }*/

                    eventLog1.WriteEntry("Getting camera allowed");
                    {
                        RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\PolicyManager\current\device\Camera");
                        Object value = key.GetValue("AllowCamera");
                        eventLog1.WriteEntry("Got key : " + key + " value " + value + " / " + value.GetType().FullName);

                        int allow = (int)value;

                        eventLog1.WriteEntry("Got camera allowed : " + allow);

                        return allow.ToString();
                    }

                case "GetComplexity":
                    break;

                case "GetMinimumLength":
                    break;

                case "AllowCamera":

                    {
                        RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\PolicyManager\current\device\Camera");
                        key.SetValue("AllowCamera", (int)1);
                    }

                    return "ok";

                case "DisallowCamera":

                    {
                        RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\PolicyManager\current\device\Camera");
                        key.SetValue("AllowCamera", (int)0);
                    }

                    return "ok";

                case "EnableComplexity":

                    //  TODO: Use this information (or better?)
                    //
                    //  https://social.technet.microsoft.com/Forums/windowsserver/en-US/0d1067a8-4897-428c-abea-72a9f4454f04/password-complexity?forum=winservercore
                    //
                    //  1- Export security information
                    //  2- Edit exported text file
                    //  3- Import security information
                    //

                    break;

                case "DisableComplexity":

                    //  TODO: Same as above
                    //

                    break;

                case "SetMinimumLength":

                    if (data.Length == 2)
                    {
                        int min;
                        if (int.TryParse(data[1], out min))
                        {
                            if (min >= 1 && min <= 14)
                            {
                                //  TODO: Same as above
                                //

                                return "ok";
                            }
                        }
                    }

                    return "ko";

                case "GetOSInfo":

                    mgmtScope = new ManagementScope("root\\cimv2");
                    mgmtScope.Connect();

                    osQuery = new SelectQuery("Win32_OperatingSystem");
              
                    var mgmtSrchr = new ManagementObjectSearcher(mgmtScope, osQuery);

                    foreach (var os in mgmtSrchr.Get())
                    {
                        eventLog1.WriteEntry("Got result " + os);

                        foreach (PropertyData prop in os.Properties)
                        {
                            reply += prop.Name + ":" + prop.Value + "\r\n";
                        }
                    }

                    return reply;

                case "RebootDevice":

                    //  TODO: Untested...
                    //
                    System.Diagnostics.Process process = new System.Diagnostics.Process();
                    System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
                    startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                    startInfo.FileName = "cmd.exe";
                    startInfo.Arguments = "/C shutdown /r";
                    process.StartInfo = startInfo;
                    process.Start();

                    return "ok";
            }

            return cmd;
        }
    }
}
