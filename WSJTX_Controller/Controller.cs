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
using System.Threading;
using System.Media;

namespace WSJTX_Controller
{
    public partial class Controller : Form
    {
        public WsjtxClient wsjtxClient;
        private bool formLoaded = false;
        private SetupDlg setupDlg = null;
        private ErrorDlg errDlg = null;
        public bool alwaysOnTop = false;

        private System.Windows.Forms.Timer timer1;
        public System.Windows.Forms.Timer timer2;
        public System.Windows.Forms.Timer timer3;
        public System.Windows.Forms.Timer timer4;
        public System.Windows.Forms.Timer timer5;
        public System.Windows.Forms.Timer timer6;
        public System.Windows.Forms.Timer timer7;

        public Controller()
        {
            InitializeComponent();
            timer1 = new System.Windows.Forms.Timer();
            timer1.Tick += new System.EventHandler(timer1_Tick);
            timer2 = new System.Windows.Forms.Timer();
            timer2.Tick += new System.EventHandler(timer2_Tick);
            timer3 = new System.Windows.Forms.Timer();
            timer3.Interval = 5000;
            timer3.Tick += new System.EventHandler(timer3_Tick);
            timer4 = new System.Windows.Forms.Timer();
            timer4.Tick += new System.EventHandler(timer4_Tick);
            timer5 = new System.Windows.Forms.Timer();
            timer5.Tick += new System.EventHandler(timer5_Tick);
            timer6 = new System.Windows.Forms.Timer();
            timer6.Tick += new System.EventHandler(timer6_Tick);
            timer7 = new System.Windows.Forms.Timer();
            timer7.Tick += new System.EventHandler(timer7_Tick);
        }

        [DllImport("Kernel32.dll")]
        static extern IntPtr GetConsoleWindow();
        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private void Form_Load(object sender, EventArgs e)
        {
            DateTime firstRunDateTime;

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

            if (Properties.Settings.Default.firstRunDateTime == DateTime.MinValue)
            {
                firstRunDateTime = DateTime.Now;
            }
            else
            {
                firstRunDateTime = Properties.Settings.Default.firstRunDateTime;
            }

            string ipAddress = Properties.Settings.Default.ipAddress;
            int port = Properties.Settings.Default.port;
            bool multicast = Properties.Settings.Default.multicast;

            //start the UDP message server
            wsjtxClient = new WsjtxClient(this, IPAddress.Parse(ipAddress),port, multicast, Properties.Settings.Default.debug, firstRunDateTime);

            wsjtxClient.ConfigsCheckedFromString(Properties.Settings.Default.configsChecked);

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
            alwaysOnTop = Properties.Settings.Default.alwaysOnTop;
            useRR73CheckBox.Checked = Properties.Settings.Default.useRR73;
            skipGridCheckBox.Checked = Properties.Settings.Default.skipGrid;
            replyCqCheckBox.Checked = Properties.Settings.Default.autoReplyCq;
            replyCqCheckBox_Click(null, null);

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

            if (!wsjtxClient.advanced)
            {
                timer6.Interval = 3000;
                timer6.Start();
            }
            else
            {
                advButton_Click(null, null);
            }
            TopMost = alwaysOnTop;


            UpdateDebug();
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

            Properties.Settings.Default.configsChecked = wsjtxClient.ConfigsCheckedString();

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
            Properties.Settings.Default.alwaysOnTop = alwaysOnTop;
            Properties.Settings.Default.useRR73 = useRR73CheckBox.Checked;
            Properties.Settings.Default.skipGrid = skipGridCheckBox.Checked;
            Properties.Settings.Default.firstRunDateTime = wsjtxClient.firstRunDateTime;
            Properties.Settings.Default.autoReplyCq = replyCqCheckBox.Checked;

            Properties.Settings.Default.Save();
            CloseComm();
        }

        public void CloseComm()
        {
            if (timer1 != null) timer1.Stop();
            timer1 = null;
            timer3.Stop();
            timer4.Stop();
            timer5.Stop();
            timer6.Stop();
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
            wsjtxClient.ProcessDecodes();
        }
        private void timer3_Tick(object sender, EventArgs e)
        {
            timer3.Stop();
            if (errDlg != null)
            {
                errDlg.Close();
                errDlg = null;
            }
        }

        private void timer4_Tick(object sender, EventArgs e)
        {
            BringToFront();
            wsjtxClient.ConnectionDialog();
        }

        private void timer5_Tick(object sender, EventArgs e)
        {
            BringToFront();
            wsjtxClient.CmdCheckDialog();
        }

        private void timer6_Tick(object sender, EventArgs e)
        {
            timer6.Stop();
            BringToFront();
            if (MessageBox.Show($"This program can be completely automatic, you don't need to do anything for continuous CQs and replies (except to 'Enable Tx' in WSJT-X).{Environment.NewLine}{Environment.NewLine}After you're familiar with the basic automatic operation, you might be interested in more options.{Environment.NewLine}{Environment.NewLine}(You'll have the choice to see these options later){Environment.NewLine}Do you want to see more options now?", wsjtxClient.pgmName, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                advButton_Click(null, null);
            }
        }

        private void timer7_Tick(object sender, EventArgs e)
        {
            timer7.Stop();
            label5.ForeColor = Color.Black;
            label13.ForeColor = Color.Black;
            label10.ForeColor = Color.Black;
            label20.ForeColor = Color.Black;
            label21.ForeColor = Color.Black;
            label8.ForeColor = Color.Black;
            label19.ForeColor = Color.Black;
            label12.ForeColor = Color.Black;
            label4.ForeColor = Color.Black;
            label17.ForeColor = Color.Black;
            label14.ForeColor = Color.Black;
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
            UpdateDebug();
        }

        private void UpdateDebug()
        {
            if (wsjtxClient.debug)
            {
                ShowWindow(GetConsoleWindow(), 5);
                Height = this.MaximumSize.Height;
                FormBorderStyle = FormBorderStyle.Fixed3D;
                wsjtxClient.UpdateDebug();
                BringToFront();
            }
            else
            {
                Height = this.MinimumSize.Height;
                FormBorderStyle = FormBorderStyle.FixedSingle;
                ShowWindow(GetConsoleWindow(), 0);
            }
        }

        private void advButton_Click(object sender, EventArgs e)
        {
            alertCheckBox.Visible = true;
            alertTextBox.Visible = true;
            directedTextBox.Visible = true;
            directedCheckBox.Visible = true;
            alertCheckBox.Visible = true;
            alertTextBox.Visible = true;
            directedTextBox.Visible = true;
            directedCheckBox.Visible = true;
            logEarlyCheckBox.Visible = true;
            useRR73CheckBox.Visible = true;
            skipGridCheckBox.Visible = true;
            addCallLabel.Visible = false;
            replyCqCheckBox.Visible = true;
            AutoReplyHelpLabel.Visible = true;
            UseDirectedHelpLabel.Visible = true;
            AlertDirectedHelpLabel.Visible = true;
            LogEarlyHelpLabel.Visible = true;
            //ExceptTextBox.Visible = true;

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

        public void setupButton_Click(object sender, EventArgs e)
        {
            if (setupDlg != null)
            {
                setupDlg.BringToFront();
                return;
            }
            setupDlg = new SetupDlg();
            setupDlg.wsjtxClient = wsjtxClient;
            setupDlg.ctrl = this;
            setupDlg.Show();
        }
        
        public void SetupDlgClosed()
        {
            TopMost = alwaysOnTop;
            setupDlg = null;
        }

        private void addCallLabel_Click(object sender, EventArgs e)
        {
            new Thread(new ThreadStart(delegate
            {
                MessageBox.Show
                (
                  $"The calls that are directed to you are automatically placed on the call reply list.{Environment.NewLine}{Environment.NewLine}To manually add more call signs to the reply list:{Environment.NewLine}- Press and hold the 'Alt' key, then{Environment.NewLine}- Double-click on the line containing the desired 'from' call sign in the 'Band Activity' list.{Environment.NewLine}{Environment.NewLine}To manually remove a call sign from the reply list:{Environment.NewLine}- Click on the call, then confirm.{Environment.NewLine}{Environment.NewLine}Also try this:{Environment.NewLine}When you see a station others are calling to (like a rare DX!), to switch *immediately* to calling that station:{Environment.NewLine}- Press and hold the 'Ctrl' and 'Alt' keys, then{Environment.NewLine}- Double-click on any line in the 'Band Activity' list where that station is the 'to' call sign.{Environment.NewLine}{Environment.NewLine}When you double-click on a call in the 'Band Activity list *without* using the 'Alt' key:{Environment.NewLine}- This causes an immediate reply, instead of placing the call on a list of calls to reply to.{Environment.NewLine}- Automatic operation continues after this call is processed.{Environment.NewLine}{Environment.NewLine}One last note:{Environment.NewLine}- Stations that are currently calling you have priority over the call signs you've added manually to the reply list.{Environment.NewLine}- This assures that the calling stations get answered promptly, and the replying to manually added call signs can wait for when there's less activity.{Environment.NewLine}{Environment.NewLine}You can leave this dialog open while you try out these hints.",
                  wsjtxClient.pgmName,
                  MessageBoxButtons.OK,
                  MessageBoxIcon.Information
                );
            })).Start();
        }

        public void ShowMsg(string text, bool sound)
        {
            if (sound)
            {
                if (text.Contains("Not ready") && wsjtxClient.myCall == "K0LW")
                {
                    wsjtxClient.Play("dive.wav");
                    text = "Not yet, Lee!";
                }
                else
                {
                    SystemSounds.Beep.Play();
                }
            }
                
                
                
            if (errDlg != null)
            {
                errDlg.Close();
                errDlg = null;
                timer3.Stop();
            }
            errDlg = new ErrorDlg();
            Point p = Location;
            int w = (Width - errDlg.Width) / 2;
            p.Offset(w, 144);
            errDlg.Location = p;
            errDlg.textBox.Text = text;
            errDlg.Show();
            timer3.Start();
        }

        private void UseDirectedHelpLabel_Click(object sender, EventArgs e)
        {
            //help for setting directed CQs
            new Thread(new ThreadStart(delegate
            {
                MessageBox.Show
                (
                  $"To send directed CQs:{Environment.NewLine}- Enter the two-character code(s) for the directed CQs, separated by spaces.{Environment.NewLine}- Enter an asterisk (^) for an ordinary non-directed CQ.{Environment.NewLine}- The directed CQs will be used in random order.{Environment.NewLine}{Environment.NewLine}{Environment.NewLine}Example: EU DX *",
                  wsjtxClient.pgmName,
                  MessageBoxButtons.OK,
                  MessageBoxIcon.Information
                );
            })).Start();
        }

        private void AlertDirectedHelpLabel_Click(object sender, EventArgs e)
        {
            string s;
            if (replyCqCheckBox.Checked)
            {
                s = $"To reply to specific directed CQs (from callers you haven't worked yet):{Environment.NewLine}- Enter the two-character code(s) for the directed CQs, separated by spaces.{Environment.NewLine}{Environment.NewLine}Example: NA US WY";
            }
            else
            {
                s = $"To hear a notification when specific directed CQs appear in the 'Band Activity' list:{Environment.NewLine}- Enter the two-character code(s) for the directed CQs, separated by spaces.{Environment.NewLine}{Environment.NewLine}Example: NA US WY";
            }

            //help for warning for specific directed CQs
            new Thread(new ThreadStart(delegate
            {
                MessageBox.Show
                (
                  s,
                  wsjtxClient.pgmName,
                  MessageBoxButtons.OK,
                  MessageBoxIcon.Information
                );
            })).Start();
        }

        private void LogEarlyHelpLabel_Click(object sender, EventArgs e)
        {
            //help for early logging
            new Thread(new ThreadStart(delegate
            {
                MessageBox.Show
                (
                  $"To maximize the chance of completed QSOs, consider 'early logging':{Environment.NewLine}{Environment.NewLine}The defining requirement for any QSO is the exchange of call signs and signal reports.{Environment.NewLine}Once either party sends an 'RRR' message (and reports have been exchanged), those requirements have been met... a '73' is not necessary for logging the QSO.{Environment.NewLine}{Environment.NewLine}Note that the QSO will continue after early logging, completing when 'RR73' or '73' is sent, or '73' is received.",
                  wsjtxClient.pgmName,
                  MessageBoxButtons.OK,
                  MessageBoxIcon.Information
                );
            })).Start();
        }

        private void verLabel2_Click(object sender, EventArgs e)
        {
            string command = "mailto:more.avantol@xoxy.net?subject=WSJTX-Controller";
            System.Diagnostics.Process.Start(command);
        }

        private void AutoReplyHelpLabel_Click(object sender, EventArgs e)
        {
            //help for setting directed CQs
            new Thread(new ThreadStart(delegate
            {
                MessageBox.Show
                (
                  $"If you enable 'Auto-reply to CQs', the Controller will continuously add up to {wsjtxClient.maxAutoGenEnqueue} CQs to the reply list that meet these criteria:{Environment.NewLine}{Environment.NewLine}- The caller has not already been logged on the current band, and{Environment.NewLine}- The caller hasn't been replied to more than {wsjtxClient.maxPrevCqs} times during this mode / band session, and{Environment.NewLine}- If the CQ is directed to 'DX', the caller is on a different continent, or if directed to other than 'DX', the CQ is directed to one of the codes in the 'Reply CQs directed to' list (if enabled).",
                  wsjtxClient.pgmName,
                  MessageBoxButtons.OK,
                  MessageBoxIcon.Information
                );
            })).Start();

        }

        private void replyCqCheckBox_Click(object sender, EventArgs e)
        {
            if (replyCqCheckBox.Checked)
            {
                alertCheckBox.Text = "Reply CQs directed to:";
            }
            else
            {
                alertCheckBox.Text = "Alert CQs directed to:";
            }
        }

        private void callText_MouseUp(object sender, MouseEventArgs e)
        {
            int idx = (e.Y - 8) / 15;
            if (idx >= 0 && idx <= WsjtxClient.maxQueueLines) wsjtxClient.EditCallQueue(idx);
        }
    }
}
