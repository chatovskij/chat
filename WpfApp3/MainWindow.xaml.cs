using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace WpfApp3
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        bool alive = false; // does thread works for receiving?
        UdpClient client;
        const int LOCALPORT = 8002; // receive port
        const int REMOTEPORT = 8002; // send port
        const string HOST = "192.168.0.255";
        private static IPAddress remoteIPAddress = IPAddress.Parse(HOST); //for group broadcast

        string userName;

        private SynchronizationContext _context;

        public MainWindow()
        {
            InitializeComponent();

            _context = SynchronizationContext.Current;

            loginButton.IsEnabled = true;
            logoutButton.IsEnabled = false; 
            sendMessageButton.IsEnabled = false;
            chatTextBox.IsReadOnly = true;
            userNameTextBox.IsReadOnly = false;
        }

        private void loginButton_Click(object sender, EventArgs e)
        {
            userName = userNameTextBox.Text;
            userNameTextBox.IsReadOnly = true;

            try
            {
                client = new UdpClient(LOCALPORT);
                // connect to the group broadcasting
                //client.JoinMulticastGroup(groupAddress, TTL); //error here
                IPEndPoint endPoint = new IPEndPoint(remoteIPAddress, REMOTEPORT);

                // run the task for receive messages
                Task receiveTask = new Task(ReceiveMessages);
                receiveTask.Start();

                // send the message about entering of the new user 
                string message = userName + " entered to the chat\r\n";
                byte[] data = Encoding.Unicode.GetBytes(message);
                client.Send(data, data.Length, HOST, REMOTEPORT);

                loginButton.IsEnabled = false;
                logoutButton.IsEnabled = true;
                sendMessageButton.IsEnabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void ReceiveMessages()
        {
            alive = true;
            try
            {
                while (alive)
                {
                    IPEndPoint remoteIp = null;
                    byte[] data = client.Receive(ref remoteIp);
                    string message = Encoding.Unicode.GetString(data);

                    _context.Post(delegate (object state) {
                        chatTextBox.AppendText(message);
                    }, null);
                }
            }
            catch (ObjectDisposedException)
            {
                if (!alive)
                    return;
                throw;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void sendButton_Click(object sender, EventArgs e)
        {
            try
            {
                string message = String.Format("{0}: {1}\r\n", userName, messageTextBox.Text);
                byte[] data = Encoding.Unicode.GetBytes(message);
                client.Send(data, data.Length, HOST, REMOTEPORT);
                messageTextBox.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void logoutButton_Click(object sender, EventArgs e)
        {
            ExitChat();
        }

        private void ExitChat()
        {
            string message = userName + " left the chat\r\n";
            byte[] data = Encoding.Unicode.GetBytes(message);
            client.Send(data, data.Length, HOST, REMOTEPORT);
            //client.DropMulticastGroup(groupAddress);
            
            alive = false;
            client.Close();

            loginButton.IsEnabled = true;
            logoutButton.IsEnabled = false;
            sendMessageButton.IsEnabled = false;
        }

        private void MainWindow_FormClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (alive)
                ExitChat();
        }

    }
}
