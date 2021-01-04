using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Media;

namespace WSJTX_Controller
{
    public partial class SetupDlg : Form
    {
        public SetupDlg()
        {
            InitializeComponent();
        }


        public WsjtxClient wsjtxClient;
        public Controller ctrl;

        private void CancelButton_Click(object sender, EventArgs e)
        {
            ctrl.SetupDlgClosed();
            Close();
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            bool multicast = multicastcheckBox.Checked;
            UInt16 port;
            IPAddress ipAddress;
            //IPEndPoint endPoint;
            //UdpClient udpClient;
            DialogResult res;

            if (!UInt16.TryParse(portTextBox.Text, out port))
            {
                MessageBox.Show("A port number must be between 0 and 65535.\n\nExample: 2237", wsjtxClient.pgmName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (addrTextBox.Text.Split('.').Length != 4)
            {
                string ex = multicast ? "239.255.0.0" : "127.0.0.1";
                MessageBox.Show($"An IP address must be 4 numbers between 0 and 255, each separated by a period.\n\nExample: {ex}", wsjtxClient.pgmName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                ipAddress = IPAddress.Parse(addrTextBox.Text);
            }
            catch (Exception err)
            {
                err.ToString();
                string ex = multicast ? "239.255.0.0" : "127.0.0.1";
                MessageBox.Show($"An IP address must be 4 numbers between 0 and 255, each separated by a period.\n\nExample: {ex}", wsjtxClient.pgmName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            /*
            try
            {
                if (multicast)
                {
                    udpClient = new UdpClient();
                    udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    udpClient.Client.Bind(endPoint = new IPEndPoint(IPAddress.Any, port));
                    udpClient.JoinMulticastGroup(ipAddress);
                }
                else
                {
                    udpClient = new UdpClient(endPoint = new IPEndPoint(ipAddress, port));
                }
            }
            catch (Exception err)
            {
                err.ToString();
                res = MessageBox.Show($"The settings you selected may not work for this multicast mode.\n\nDo you want to use the standard settings instead?", wsjtxClient.pgmName, MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (res == DialogResult.No) return;

                if (multicast)
                {
                    addrTextBox.Text = "239.255.0.0";
                }
                else
                {
                    addrTextBox.Text = "127.0.0.1";
                }
                portTextBox.Text = "2237";
                return;
            }
            */

            //if no change, no need to notify
            if (wsjtxClient.ipAddress.ToString() == ipAddress.ToString() && wsjtxClient.port == port && wsjtxClient.multicast == multicast)
            {
                ctrl.SetupDlgClosed();
                Close();
                return;
            }

            res = MessageBox.Show($"{wsjtxClient.pgmName} will exit now, restart {wsjtxClient.pgmName} for the new settings to take effect", wsjtxClient.pgmName, MessageBoxButtons.OKCancel, MessageBoxIcon.Information);
            if (res == DialogResult.Cancel) return;

            wsjtxClient.UpdateAddrPortMulti(ipAddress, port, multicast);
            ctrl.SetupDlgClosed();
            Close();
        }

        private void SetupDlg_Load(object sender, EventArgs e)
        {
            addrTextBox.Text = wsjtxClient.ipAddress.ToString();
            portTextBox.Text = wsjtxClient.port.ToString();
            multicastcheckBox.Checked = wsjtxClient.multicast;
        }

        private void SetupDlg_FormClosing(object sender, FormClosingEventArgs e)
        {
            //wsjtxClient.ipAddress = addrTextBox.Text;
            //wsjtxClient.port = portTextBox.Text;
        }
    }
}
