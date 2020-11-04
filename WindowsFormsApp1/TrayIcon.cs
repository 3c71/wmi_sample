using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.ServiceProcess;
using System.Text;
using System.Windows.Forms;
using WindowsFormsApp1.Properties;

namespace WindowsFormsApp1
{
    public class TrayIcon : ApplicationContext
    {
        private enum YourMethods
        {
            AllowCamera = 128,
            DisallowCamera = 129,
            EnableComplexity = 130,
            DisableComplexity = 131,
            SetMinimumLength = 132,
            RebootDevice = 133,

            GetOSInfo = 134,
            GetCamera = 135,
        };

        private NotifyIcon trayIcon;

        public TrayIcon()
        {
            //  TODO: Put that in child thread to avoid UI freeze
            //
            {
                ServiceController service = new ServiceController("MDMService");
                try
                {
                    TimeSpan timeout = TimeSpan.FromMilliseconds(3000);

                    service.Start();
                    service.WaitForStatus(ServiceControllerStatus.Running, timeout);
                }
                catch
                {
                }

                createTrayIcon();
            }

            //  TODO: Implement polling/monitoring of current state to update icon appropriately
            //
        }

        void createTrayIcon()
        {
            Console.WriteLine("Creating tray icon " + trayIcon);

            if (trayIcon == null)
            {
                // Initialize Tray Icon
                trayIcon = new NotifyIcon()
                {
                    Icon = Resources.Icon1,
                    Visible = true
                };
            }

            String camera = SendMessage(YourMethods.GetCamera.ToString());
            Boolean allowCamera = camera != null ? camera.Equals("1") : false;

            MenuItem cameraItem = allowCamera ? new MenuItem("Disallow camera", Disallow) : new MenuItem("Allow camera", Allow);

            //  Get complex password state as above and minimum password to display in MenuItems
            //

            trayIcon.ContextMenu = new ContextMenu(new MenuItem[] {
                cameraItem,
                new MenuItem("Force complex password", EnableComplex),
                new MenuItem("Do not force complex password", DisableComplex),
                new MenuItem("Set password minimum length", SetPasswordMinimumLength),
                new MenuItem("Get OS information", GetOSInformation),
                new MenuItem("Reboot device", Reboot),
                new MenuItem("Exit", Exit), });
        }

        void Exit(object sender, EventArgs e)
        {
            // Hide tray icon, otherwise it will remain shown until user mouses over it
            trayIcon.Visible = false;

            ServiceController service = new ServiceController("MDMService");
            try
            {
                TimeSpan timeout = TimeSpan.FromMilliseconds(3000);

                service.Stop();
                service.WaitForStatus(ServiceControllerStatus.Stopped, timeout);
            }
            catch
            {
            }

            Application.Exit();
        }

        void Allow(object sender, EventArgs e)
        {
            SendMessage(YourMethods.AllowCamera.ToString());

            createTrayIcon();
        }

        void Disallow(object sender, EventArgs e)
        {
            SendMessage(YourMethods.DisallowCamera.ToString());

            createTrayIcon();
        }

        void EnableComplex(object sender, EventArgs e)
        {
            SendMessage(YourMethods.EnableComplexity.ToString());
        }

        void DisableComplex(object sender, EventArgs e)
        {
            SendMessage(YourMethods.DisableComplexity.ToString());
        }

        void SetPasswordMinimumLength(object sender, EventArgs e)
        {
            //  TODO: For some reason this creates a new tray icon, leaving one unusable without menu
            //
            Form1 form = new Form1();
            form.FormClosed += Form_FormClosed;

            //form.ShowDialog();
        }

        private void Form_FormClosed(object sender, FormClosedEventArgs e)
        {
            Form1 form = (Form1)sender;

            if (form.minimum != -1)
            {
                SendMessage(YourMethods.SetMinimumLength.ToString() + ":" + form.minimum);
            }
        }

        void GetOSInformation(object sender, EventArgs e)
        {
            String info = SendMessage(YourMethods.GetOSInfo.ToString());

            System.Windows.Forms.MessageBox.Show(info);
        }

        void Reboot(object sender, EventArgs e)
        {
            SendMessage(YourMethods.RebootDevice.ToString());
        }

        private void sendCommand(int command)
        {
            ServiceController sc = new ServiceController("MDMService", Environment.MachineName);
            ServiceControllerPermission scp = new ServiceControllerPermission(ServiceControllerPermissionAccess.Control, Environment.MachineName, "MDMService");
            scp.Assert();
            sc.Refresh();

            sc.ExecuteCommand(command);
        }

        public static String SendMessage(String textMessage)
        {
            // Data buffer for incoming data.  
            byte[] bytes = new byte[1024];

            // Connect to a remote device.  
            try
            {
                // Establish the remote endpoint for the socket.  
                // This example uses port 11000 on the local computer.  
                IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
                IPAddress ipAddress = ipHostInfo.AddressList[0];
                IPEndPoint remoteEP = new IPEndPoint(ipAddress, 11000);

                // Create a TCP/IP  socket.  
                Socket sender = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                // Connect the socket to the remote endpoint. Catch any errors.  
                try
                {
                    sender.Connect(remoteEP);

                    Console.WriteLine("Socket connected to {0}", sender.RemoteEndPoint.ToString());

                    // Encode the data string into a byte array.  
                    byte[] msg = Encoding.ASCII.GetBytes(textMessage + "<EOF>");

                    // Send the data through the socket.  
                    int bytesSent = sender.Send(msg);

                    // Receive the response from the remote device.  
                    int bytesRec = sender.Receive(bytes);
                    String reply = Encoding.ASCII.GetString(bytes, 0, bytesRec);

                    Console.WriteLine("Received {0}", reply);

                    // Release the socket.  
                    sender.Shutdown(SocketShutdown.Both);
                    sender.Close();

                    return reply;

                }
                catch (ArgumentNullException ane)
                {
                    Console.WriteLine("ArgumentNullException : {0}", ane.ToString());
                }
                catch (SocketException se)
                {
                    Console.WriteLine("SocketException : {0}", se.ToString());
                }
                catch (Exception e)
                {
                    Console.WriteLine("Unexpected exception : {0}", e.ToString());
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            return null;
        }

    }
}
