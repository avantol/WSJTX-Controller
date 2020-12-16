using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using WsjtxUdpLib;
using System.Net;
using System.Configuration;


namespace WSJTX_Controller
{
    public partial class Controller : Form
    {
        private WsjtxClient wsjtxClient;
        private bool formLoaded = false;
        private System.Windows.Forms.Timer timer1;
        public System.Windows.Forms.Timer timer2;
        public System.Windows.Forms.Timer timer3;
        public System.Windows.Forms.Timer timer4;
        public System.Windows.Forms.Timer timer5;

        public Controller()
        {
            InitializeComponent();
            timer1 = new System.Windows.Forms.Timer();
            timer1.Tick += new System.EventHandler(timer1_Tick);
            timer2 = new System.Windows.Forms.Timer();
            timer2.Tick += new System.EventHandler(timer2_Tick);
            timer3 = new System.Windows.Forms.Timer();
            timer3.Tick += new System.EventHandler(timer3_Tick);
            timer4 = new System.Windows.Forms.Timer();
            timer4.Tick += new System.EventHandler(timer4_Tick);
            timer5 = new System.Windows.Forms.Timer();
            timer5.Tick += new System.EventHandler(timer5_Tick);
        }

        [DllImport("Kernel32.dll")]
        static extern IntPtr GetConsoleWindow();
        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private void Form_Load(object sender, EventArgs e)
        {
            SuspendLayout();
            AllocConsole();

            //Properties.Settings.Default.Upgrade();
            //Properties.Settings.Default.Save();

            if (!Properties.Settings.Default.debug)
            {
                ShowWindow(GetConsoleWindow(), 0);
            }

            if (Properties.Settings.Default.windowPos != new Point(0, 0)) Location = Properties.Settings.Default.windowPos;
            if (Properties.Settings.Default.windowHt != 0) Height = Properties.Settings.Default.windowHt;

            string ipAddress = Properties.Settings.Default.ipAddress;
            int port = Properties.Settings.Default.port;
            bool multicast = Properties.Settings.Default.multicast;

            //start the UDP message server
            wsjtxClient = new WsjtxClient(this, IPAddress.Parse(ipAddress),port, multicast, Properties.Settings.Default.debug);

            wsjtxClient.configsCheckedFromString(Properties.Settings.Default.configsChecked);

            timeoutNumUpDown.Value = Properties.Settings.Default.timeout;
            directedCheckBox.Checked = Properties.Settings.Default.useDirected;

            directedTextBox.Enabled = directedCheckBox.Checked;
            directedTextBox.ForeColor = System.Drawing.Color.Gray;
            if (!directedTextBox.Enabled && Properties.Settings.Default.directeds == "")
            {
                directedTextBox.Text = "(separate by spaces)";
            }
            else
            {
                directedTextBox.Text = Properties.Settings.Default.directeds;
                directedTextBox.ForeColor = System.Drawing.Color.Black;
            }
            
            mycallCheckBox.Checked = Properties.Settings.Default.playMyCall;
            loggedCheckBox.Checked = Properties.Settings.Default.playLogged;
            alertCheckBox.Checked = Properties.Settings.Default.useAlertDirected;
            logEarlyCheckBox.Checked = Properties.Settings.Default.logEarly;
            //wsjtxClient.debug = Properties.Settings.Default.debug;
            wsjtxClient.advanced = Properties.Settings.Default.advanced;
            useRR73CheckBox.Checked = Properties.Settings.Default.useRR73;
            skipGridCheckBox.Checked = Properties.Settings.Default.skipGrid;

            alertTextBox.Enabled = alertCheckBox.Checked;
            alertTextBox.ForeColor = System.Drawing.Color.Gray;
            if (!alertTextBox.Enabled && Properties.Settings.Default.alertDirecteds == "")
            {
                alertTextBox.Text = "(separate by spaces)";
            }
            else
            {
                alertTextBox.Text = Properties.Settings.Default.alertDirecteds;
                alertTextBox.ForeColor = System.Drawing.Color.Black;
            }

            timer1.Interval = 10;           //actual is 11-12 msec (due to OS limitations)
            timer1.Start();

            if (wsjtxClient.advanced) advButton_Click(null, null);
            updateDebug();
            ResumeLayout();
            formLoaded = true;
            timer4.Interval = 60000;           //pop up dialog showing UDP settings at tick
            timer4.Start();
        }
        private void Controller_FormClosing(object sender, FormClosingEventArgs e)
        {
            Properties.Settings.Default.debug = wsjtxClient.debug;

            Properties.Settings.Default.windowPos = this.Location;
            Properties.Settings.Default.windowHt = this.Height;

            Properties.Settings.Default.ipAddress = wsjtxClient.ipAddress.ToString();
            Properties.Settings.Default.port = wsjtxClient.port;
            Properties.Settings.Default.multicast = wsjtxClient.multicast;

            Properties.Settings.Default.configsChecked = wsjtxClient.configsCheckedString();

            Properties.Settings.Default.timeout = (int)timeoutNumUpDown.Value;
            Properties.Settings.Default.useDirected = directedCheckBox.Checked;
            if (directedTextBox.Text == "(separate by spaces)") directedTextBox.Clear();
            Properties.Settings.Default.directeds = directedTextBox.Text;
            Properties.Settings.Default.playMyCall = mycallCheckBox.Checked;
            Properties.Settings.Default.playLogged = loggedCheckBox.Checked;
            Properties.Settings.Default.useAlertDirected = alertCheckBox.Checked;
            if (alertTextBox.Text == "(separate by spaces)") alertTextBox.Clear();
            Properties.Settings.Default.alertDirecteds = alertTextBox.Text;
            Properties.Settings.Default.logEarly = logEarlyCheckBox.Checked;
            Properties.Settings.Default.advanced = wsjtxClient.advanced;
            Properties.Settings.Default.useRR73 = useRR73CheckBox.Checked;
            Properties.Settings.Default.skipGrid = skipGridCheckBox.Checked;

            Properties.Settings.Default.Save();
            CloseComm();
        }

        public void CloseComm()
        {
            if (timer1 != null) timer1.Stop();
            timer1 = null;
            timer4.Stop();
            timer5.Stop();
            wsjtxClient.Closing();
        }

        private void Controller_FormClosed(object sender, FormClosedEventArgs e)
        {

        }

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool AllocConsole();

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (timer1 == null) return;
            wsjtxClient.UdpLoop();
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            wsjtxClient.processDecodes();
        }

        private void timer3_Tick(object sender, EventArgs e)
        {
            wsjtxClient.AddAltCallSeparator();
            timer3.Stop();
        }

        private void timer4_Tick(object sender, EventArgs e)
        {
            wsjtxClient.ConnectionDialog();
        }

        private void timer5_Tick(object sender, EventArgs e)
        {
            wsjtxClient.CmdCheckDialog();
        }
        private void timeoutNumUpDown_ValueChanged(object sender, EventArgs e)
        {
            if (timeoutNumUpDown.Value < WsjtxClient.minSkipCount)
            {
                Console.Beep();
                timeoutNumUpDown.Value = WsjtxClient.minSkipCount;
            }

            if (timeoutNumUpDown.Value > WsjtxClient.maxSkipCount)
            {
                Console.Beep();
                timeoutNumUpDown.Value = WsjtxClient.maxSkipCount;

            }

            if (!(wsjtxClient == null)) wsjtxClient.ShowTimeout();
        }

        private void altClearButton_Click(object sender, EventArgs e)
        {
            wsjtxClient.ClearAltCallList();

            Console.Clear();            //tempOnly
        }

        private void altListBox_DoubleClick(object sender, EventArgs e)
        {
            wsjtxClient.AltCallSelected((Control.ModifierKeys & Keys.Shift) == Keys.Shift);
        }

        private void altPauseButton_Click(object sender, EventArgs e)
        {
            wsjtxClient.AltPauseButtonToggeled();
        }

        private void alertCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            alertTextBox.Enabled = alertCheckBox.Checked;
            if (formLoaded && alertCheckBox.Checked) wsjtxClient.Play("beepbeep.wav");

            if (alertCheckBox.Checked && alertTextBox.Text == "(separate by spaces)")
            {
                alertTextBox.Clear();
                alertTextBox.ForeColor = System.Drawing.Color.Black;
            }
            if (!alertCheckBox.Checked && alertTextBox.Text == "") alertTextBox.Text = "(separate by spaces)";
        }

        private void alertTextBox_Click(object sender, EventArgs e)
        {
        }


        private void directedTextBox_Click(object sender, EventArgs e)
        {
        }

        private void directedCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            directedTextBox.Enabled = directedCheckBox.Checked;
            if (directedCheckBox.Checked && directedTextBox.Text == "(separate by spaces)")
            {
                directedTextBox.Clear();
                directedTextBox.ForeColor = System.Drawing.Color.Black;
            }
            if (!directedCheckBox.Checked && directedTextBox.Text == "") directedTextBox.Text = "(separate by spaces)";
        }

        private void loggedCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (formLoaded && loggedCheckBox.Checked) wsjtxClient.Play("echo.wav");
        }

        private void mycallCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (formLoaded && mycallCheckBox.Checked) wsjtxClient.Play("trumpet.wav");
        }

        private void verLabel_DoubleClick(object sender, EventArgs e)
        {
            wsjtxClient.debug = !wsjtxClient.debug;
            updateDebug();
        }

        private void updateDebug()
        {
            if (wsjtxClient.debug)
            {
                Height = this.MaximumSize.Height;
                ShowWindow(GetConsoleWindow(), 5);
            }
            else
            {
                Height = this.MinimumSize.Height;
                ShowWindow(GetConsoleWindow(), 0);
            }
        }

        private void altListBox_Click(object sender, EventArgs e)
        {
            wsjtxClient.AltListBoxClicked();
        }

        private void advButton_Click(object sender, EventArgs e)
        {
            advTextBox1.Visible = false;
            advTextBox2.Visible = false;
            advButton.Visible = false;

            label3.Visible = true;
            label4.Visible = true;
            altPauseButton.Visible = true;
            altClearButton.Visible = true;
            altListBox.Visible = true;
            alertCheckBox.Visible = true;
            alertTextBox.Visible = true;
            directedTextBox.Visible = true;
            directedCheckBox.Visible = true;
            label3.Visible = true;
            label4.Visible = true;
            altPauseButton.Visible = true;
            altClearButton.Visible = true;
            altListBox.Visible = true;
            alertCheckBox.Visible = true;
            alertTextBox.Visible = true;
            directedTextBox.Visible = true;
            directedCheckBox.Visible = true;
            logEarlyCheckBox.Visible = true;
            useRR73CheckBox.Visible = true;
            skipGridCheckBox.Visible = true;

            wsjtxClient.advanced = true;
        }

        private void skipGridCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (!formLoaded) return;
            skipGridCheckBox.Text = "Skip grid (next CQ)";
            skipGridCheckBox.ForeColor = Color.DarkGreen;
            wsjtxClient.WsjtxSettingChanged();
        }

        private void useRR73CheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (!formLoaded) return;
            useRR73CheckBox.Text = "Use RR73 (next CQ)";
            useRR73CheckBox.ForeColor = Color.DarkGreen;
            wsjtxClient.WsjtxSettingChanged();
        }

        public void WsjtxSettingConfirmed()
        {
            skipGridCheckBox.Text = "Skip grid msg";
            skipGridCheckBox.ForeColor = Color.Black;
            useRR73CheckBox.Text = "Use RR73 msg";
            useRR73CheckBox.ForeColor = Color.Black;
        }
    }
}
