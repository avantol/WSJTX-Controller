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
using System.IO;
using System.Reflection;

namespace WSJTX_Controller
{
    public partial class Controller : Form
    {
        public WsjtxClient wsjtxClient;
        private bool formLoaded = false;
        private SetupDlg setupDlg = null;
        public bool alwaysOnTop = false;
        private IniFile iniFile = null;
        private bool firstRun = false;


        private System.Windows.Forms.Timer timer1;
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

#if DEBUG
        //project type must be Console application for this to work

        [DllImport("Kernel32.dll")]
        static extern IntPtr GetConsoleWindow();
        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
#endif
        private void Form_Load(object sender, EventArgs e)
        {
            SuspendLayout();

            //use .ini file for settings (avoid .Net config file mess)
            string pgmName = Assembly.GetExecutingAssembly().GetName().Name.ToString();
            string path = $"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\{pgmName}";
            string pathFileNameExt = path + "\\" + pgmName + ".ini";
            try
            {
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                iniFile = new IniFile(pathFileNameExt);
            }
            catch
            {
                MessageBox.Show("Unable to create settings file: " + pathFileNameExt + "\n\nContinuing with default settings...", pgmName, MessageBoxButtons.OK);
            }

            string ipAddress = null;
            int port = 0;
            bool multicast = true;
            bool advanced = false;
            bool debug = false;
            bool diagLog = false;
            bool replyAndQuit = false;
            bool showTxModes = false;

            freqCheckBox.Checked = false;
            stopCheckBox.Checked = false;           //not saved

            if (iniFile == null || !iniFile.KeyExists("advanced"))     //.ini file not written yet, read properties (possibly set defaults)
            {
                firstRun = Properties.Settings.Default.firstRun;
                debug = Properties.Settings.Default.debug;
                if (Properties.Settings.Default.windowPos != new Point(0, 0)) this.Location = Properties.Settings.Default.windowPos;
                if (Properties.Settings.Default.windowHt != 0) this.Height = Properties.Settings.Default.windowHt;
                ipAddress = Properties.Settings.Default.ipAddress;
                port = Properties.Settings.Default.port;
                multicast = Properties.Settings.Default.multicast;
                timeoutNumUpDown.Value = Properties.Settings.Default.timeout;
                directedTextBox.Text = Properties.Settings.Default.directeds;
                directedCheckBox.Checked = Properties.Settings.Default.useDirected;
                mycallCheckBox.Checked = Properties.Settings.Default.playMyCall;
                loggedCheckBox.Checked = Properties.Settings.Default.playLogged;
                alertTextBox.Text = Properties.Settings.Default.alertDirecteds;
                alertCheckBox.Checked = Properties.Settings.Default.useAlertDirected;
                logEarlyCheckBox.Checked = Properties.Settings.Default.logEarly;
                advanced = Properties.Settings.Default.advanced;
                alwaysOnTop = Properties.Settings.Default.alwaysOnTop;
                useRR73CheckBox.Checked = Properties.Settings.Default.useRR73;
                skipGridCheckBox.Checked = Properties.Settings.Default.skipGrid;
                replyCqCheckBox.Checked = Properties.Settings.Default.autoReplyCq;
                exceptCheckBox.Checked = Properties.Settings.Default.enableExclude;
                diagLog = Properties.Settings.Default.diagLog;
            }
            else        //read settings from .ini file (avoid .Net config file mess)
            {
                firstRun = iniFile.Read("firstRun") == "True";
                debug = iniFile.Read("debug") == "True";
                var rect = System.Windows.Forms.Screen.PrimaryScreen.Bounds;
                int x = Math.Max(Convert.ToInt32(iniFile.Read("windowPosX")), 0);
                int y = Math.Max(Convert.ToInt32(iniFile.Read("windowPosY")), 0);
                if (x > rect.Width) x = rect.Width / 2;
                if (y > rect.Height) y = rect.Height / 2;
                this.Location = new Point(x, y);
                this.Height = Convert.ToInt32(iniFile.Read("windowHt"));
                ipAddress = iniFile.Read("ipAddress");
                port = Convert.ToInt32(iniFile.Read("port"));
                multicast = iniFile.Read("multicast") == "True";
                timeoutNumUpDown.Value = Convert.ToInt32(iniFile.Read("timeout"));
                directedTextBox.Text = iniFile.Read("directeds");
                directedCheckBox.Checked = iniFile.Read("useDirected") == "True";
                mycallCheckBox.Checked = iniFile.Read("playMyCall") == "True";
                loggedCheckBox.Checked = iniFile.Read("playLogged") == "True";
                alertTextBox.Text = iniFile.Read("alertDirecteds");
                alertCheckBox.Checked = iniFile.Read("useAlertDirected") == "True";
                logEarlyCheckBox.Checked = iniFile.Read("logEarly") == "True";
                advanced = iniFile.Read("advanced") == "True";
                alwaysOnTop = iniFile.Read("alwaysOnTop") == "True";
                useRR73CheckBox.Checked = iniFile.Read("useRR73") == "True";
                skipGridCheckBox.Checked = iniFile.Read("skipGrid") == "True";
                replyCqCheckBox.Checked = iniFile.Read("autoReplyCq") == "True";
                exceptCheckBox.Checked = iniFile.Read("enableExclude") == "True";
                diagLog = iniFile.Read("diagLog") == "True";

                //start of .ini-file-only settings (not in .Net config)
                if (iniFile.KeyExists("replyAndQuit")) replyAndQuit = iniFile.Read("replyAndQuit") == "True";
                if (iniFile.KeyExists("showTxModes")) showTxModes = iniFile.Read("showTxModes") == "True";
                if (iniFile.KeyExists("bestOffset")) freqCheckBox.Checked = iniFile.Read("bestOffset") == "True";
                if (iniFile.KeyExists("stopTxTime")) stopTextBox.Text = iniFile.Read("stopTxTime");
            }

            if (!advanced)
            {
                showTxModes = false;
                freqCheckBox.Checked = false;
            }

            if (!showTxModes)
            {
                replyAndQuit = false;
            }

            replyCqCheckBox_Click(null, null);

            if (directedTextBox.Text == "") directedCheckBox.Checked = false;
            directedTextBox.Enabled = directedCheckBox.Checked;
            directedTextBox.ForeColor = System.Drawing.Color.Gray;
            if (!directedTextBox.Enabled && directedTextBox.Text == "")
            {
                directedTextBox.Text = "(separate by spaces)";
            }
            else
            {
                directedTextBox.ForeColor = System.Drawing.Color.Black;
            }

            if (alertTextBox.Text == "") alertCheckBox.Checked = false;
            alertTextBox.Enabled = alertCheckBox.Checked;
            alertTextBox.ForeColor = System.Drawing.Color.Gray;
            if (!alertTextBox.Enabled && alertTextBox.Text == "")
            {
                alertTextBox.Text = "(separate by spaces)";
            }
            else
            {
                alertTextBox.ForeColor = System.Drawing.Color.Black;
            }

            cqModeButton.Checked = !replyAndQuit;
            listenModeButton.Checked = replyAndQuit;
                
#if DEBUG
            AllocConsole();

            if (!debug)
            {
                ShowWindow(GetConsoleWindow(), 0);
            }
#endif

            //start the UDP message server
            wsjtxClient = new WsjtxClient(this, IPAddress.Parse(ipAddress), port, multicast, debug, diagLog);
            wsjtxClient.advanced = advanced;
            wsjtxClient.replyAndQuit = replyAndQuit;
            wsjtxClient.showTxModes = showTxModes;

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
            if (iniFile != null)
            {
                iniFile.Write("debug", wsjtxClient.debug.ToString());
                iniFile.Write("windowPosX", (Math.Max(this.Location.X, 0)).ToString());
                iniFile.Write("windowPosY", (Math.Max(this.Location.Y, 0)).ToString());
                iniFile.Write("windowHt", this.Height.ToString());
                iniFile.Write("ipAddress", wsjtxClient.ipAddress.ToString());   //string
                iniFile.Write("port", wsjtxClient.port.ToString());
                iniFile.Write("multicast", wsjtxClient.multicast.ToString());
                iniFile.Write("timeout", ((int)timeoutNumUpDown.Value).ToString());
                iniFile.Write("useDirected", directedCheckBox.Checked.ToString());
                if (directedTextBox.Text == "(separate by spaces)") directedTextBox.Clear();
                iniFile.Write("directeds", directedTextBox.Text.Trim());
                iniFile.Write("playMyCall", mycallCheckBox.Checked.ToString());
                iniFile.Write("playLogged", loggedCheckBox.Checked.ToString());
                iniFile.Write("useAlertDirected", alertCheckBox.Checked.ToString());
                if (alertTextBox.Text == "(separate by spaces)") alertTextBox.Clear();
                iniFile.Write("alertDirecteds", alertTextBox.Text.Trim());
                iniFile.Write("logEarly", logEarlyCheckBox.Checked.ToString());
                iniFile.Write("advanced", wsjtxClient.advanced.ToString());
                iniFile.Write("alwaysOnTop", alwaysOnTop.ToString());
                iniFile.Write("useRR73", useRR73CheckBox.Checked.ToString());
                iniFile.Write("skipGrid", skipGridCheckBox.Checked.ToString());
                iniFile.Write("firstRun", false.ToString());
                iniFile.Write("autoReplyCq", replyCqCheckBox.Checked.ToString());
                iniFile.Write("enableExclude", exceptCheckBox.Checked.ToString());
                iniFile.Write("diagLog", wsjtxClient.diagLog.ToString());
                if (wsjtxClient.replyAndQuit) iniFile.Write("replyAndQuit", wsjtxClient.replyAndQuit.ToString());
                if (wsjtxClient.showTxModes) iniFile.Write("showTxModes", wsjtxClient.showTxModes.ToString());
                iniFile.Write("bestOffset", freqCheckBox.Checked.ToString());
                iniFile.Write("stopTxTime", stopTextBox.Text.Trim());
            }

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

#if DEBUG
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool AllocConsole();
#endif

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (timer1 == null) return;
            wsjtxClient.UdpLoop();
        }

        private void timer3_Tick(object sender, EventArgs e)
        {
            timer3.Stop();
            if (wsjtxClient.showTxModes)
            {
                wsjtxClient.UpdateCallInProg();
            }
            else
            {
                msgTextBox.Text = "";
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
            if (MessageBox.Show($"This program can be completely automatic, you don't need to do anything for continuous CQs and replies (except to 'Enable Tx' in WSJT-X).{Environment.NewLine}{Environment.NewLine}After you're familiar with the basic automatic operation, you might be interested in more options.{Environment.NewLine}{Environment.NewLine}(You'll have the choice to see these options later){Environment.NewLine}{Environment.NewLine}Do you want to see more options now?", wsjtxClient.pgmName, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                advButton_Click(null, null);
            }
            if (firstRun)
            {
                Thread.Sleep(2000);
                MessageBox.Show($"For this program to work correctly, you must now set the 'Tx watchdog' in WSJT-X to 15 minutes or longer.\n\nThis will be the timeout in case the Controller sends the same message repeatedly (for example, calling CQ when the band is closed).\n\nThe WSJT-X 'Tx watchdog' is under File | Settings, in the 'General' tab.{Environment.NewLine}{Environment.NewLine}After you have done this, click OK to continue.", wsjtxClient.pgmName, MessageBoxButtons.OK, MessageBoxIcon.Information);
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
            label18.ForeColor = Color.Black;
            label12.ForeColor = Color.Black;
            label4.ForeColor = Color.Black;
            label17.ForeColor = Color.Black;
            label14.ForeColor = Color.Black;
            label15.ForeColor = Color.Black;
            label16.ForeColor = Color.Black;
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
            if (!formLoaded) return;
            alertTextBox.Enabled = alertCheckBox.Checked;

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
            if (!formLoaded) return;
            directedTextBox.Enabled = directedCheckBox.Checked;
            if (directedCheckBox.Checked && directedTextBox.Text == "(separate by spaces)")
            {
                directedTextBox.Clear();
                directedTextBox.ForeColor = System.Drawing.Color.Black;
            }
            if (!directedCheckBox.Checked && directedTextBox.Text == "") directedTextBox.Text = "(separate by spaces)";

            if (!directedCheckBox.Checked)
            {
                wsjtxClient.WsjtxSettingChanged();              //resets CQ to non-directed
            }
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
#if DEBUG
                AllocConsole();
                ShowWindow(GetConsoleWindow(), 5);
#endif
                Height = this.MaximumSize.Height;
                FormBorderStyle = FormBorderStyle.Fixed3D;
                wsjtxClient.UpdateDebug();
                BringToFront();
            }
            else
            {
                Height = this.MinimumSize.Height;
                FormBorderStyle = FormBorderStyle.FixedSingle;
#if DEBUG
                ShowWindow(GetConsoleWindow(), 0);
#endif
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
            exceptCheckBox.Visible = true;
            ExcludeHelpLabel.Visible = true;
            freqCheckBox.Visible = true;
            stopTextBox.Visible = true;
            stopCheckBox.Visible = true;
            timeLabel.Visible = true;

            wsjtxClient.advanced = true;
            wsjtxClient.UpdateModeVisible();
        }

        private void skipGridCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (!formLoaded) return;
            skipGridCheckBox.Text = "Skip grid (pending)";
            skipGridCheckBox.ForeColor = Color.DarkGreen;
            wsjtxClient.WsjtxSettingChanged();
        }

        private void useRR73CheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (!formLoaded) return;
            useRR73CheckBox.Text = "Use RR73 (pending)";
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
                SystemSounds.Beep.Play();
            }
                
            timer3.Stop();
            msgTextBox.Text = text;
            timer3.Start();
        }

        private void UseDirectedHelpLabel_Click(object sender, EventArgs e)
        {
            //help for setting directed CQs
            new Thread(new ThreadStart(delegate
            {
                MessageBox.Show
                (
                  $"To send directed CQs:{Environment.NewLine}- Enter the two-character code(s) for the directed CQs you want to transmit, separated by spaces.{Environment.NewLine}- Enter an asterisk (*) for an ordinary non-directed CQ.{Environment.NewLine}- The directed CQs will be used in random order.{Environment.NewLine}{Environment.NewLine}Example: EU DX *",
                  wsjtxClient.pgmName,
                  MessageBoxButtons.OK,
                  MessageBoxIcon.Information
                );
            })).Start();
        }

        private void AlertDirectedHelpLabel_Click(object sender, EventArgs e)
        {
            //help for replying to specific directed CQs
            new Thread(new ThreadStart(delegate
            {
                MessageBox.Show
                (
                  $"To reply to specific directed CQs from callers you haven't worked yet:{Environment.NewLine}- Enter the code(s) for the directed CQs, separated by spaces.{Environment.NewLine}{Environment.NewLine}Example: POTA NA USA WY{Environment.NewLine}{Environment.NewLine}'CQ DX' is replied to only if the caller is on a different continent from this station.{Environment.NewLine}{Environment.NewLine}(Note: 'CQ POTA' or 'CQ SOTA' is an exception to the 'already worked' rule, these calls will cause an auto-reply if you haven't already logged that call in the current mode/band in the current day).",
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
                  $"A 'normal' CQ is one that isn't directed to any specific place. If you enable 'Reply to normal CQs', the Controller will continuously add up to {wsjtxClient.maxAutoGenEnqueue} CQs to the reply list that meet these conditions:{Environment.NewLine}{Environment.NewLine}- The caller has not already been logged on the current band, and{Environment.NewLine}- The caller is on your Rx time slot, and{Environment.NewLine}- The caller hasn't been replied to more than {wsjtxClient.maxPrevCqs} times during this mode / band session,{Environment.NewLine}and{Environment.NewLine}- The CQ is not a 'directed' CQ, or{Environment.NewLine}if 'CQ DX': the caller is on a different continent.",
                  wsjtxClient.pgmName,
                  MessageBoxButtons.OK,
                  MessageBoxIcon.Information
                );
            })).Start();
        }
        private void ExcludeHelpLabel_Click(object sender, EventArgs e)
        {
            //help for excluding certain directed CQs
            new Thread(new ThreadStart(delegate
            {
                MessageBox.Show
                (
                  $"If you enable 'DX stations only', the Controller will exclude call signs on your continent from 'Reply to normal CQs'.{Environment.NewLine}{Environment.NewLine}For example, this is useful in case you've already worked all states/entities on your continent, and only want to reply to CQs from other continents.{Environment.NewLine}{Environment.NewLine}(Note: If you have entered directed CQs to reply to, those CQs from your continent will be replied to regardless of this setting)",
                  wsjtxClient.pgmName,
                  MessageBoxButtons.OK,
                  MessageBoxIcon.Information
                );
            })).Start();
        }

        private void replyCqCheckBox_Click(object sender, EventArgs e)
        {
            exceptCheckBox.Enabled = replyCqCheckBox.Checked;
        }

        private void callText_MouseUp(object sender, MouseEventArgs e)
        {
            int idx = (e.Y - 8) / 15;
            if (idx >= 0 && idx <= WsjtxClient.maxQueueLines) wsjtxClient.EditCallQueue(idx);
        }

        private void modeHelpLabel_Click(object sender, EventArgs e)
        {
            new Thread(new ThreadStart(delegate
            {
                MessageBox.Show
                (
                  $"Choose what you want this progam to do after replying to all queued calls:{Environment.NewLine}{Environment.NewLine}- Call CQ until there is a reply, and automatically complete each contact,{Environment.NewLine}or{Environment.NewLine}- Listen for CQs, and automatically reply to them.{Environment.NewLine}{Environment.NewLine}The advantage to listening for CQs is that both odd and even Rx time slots can be monitored. This is helpful for maximizing POTA QSOs, for example.{Environment.NewLine}{Environment.NewLine}(Note: If you choose 'Listen for CQs', be sure to select a large enough number of retries in 'Skip call after -- TXs' so that the stations you call have a chance to reply before any automatic switch to the opposite time slot).{Environment.NewLine}{Environment.NewLine}'Pause Tx' will disable transmit after the current transmit period finishes.",
                  wsjtxClient.pgmName,
                  MessageBoxButtons.OK,
                  MessageBoxIcon.Information
                );
            })).Start();

        }

        private void cqModeButton_Click(object sender, EventArgs e)
        {
            wsjtxClient.ReplyModeChanged(false);
        }

        private void listenModeButton_Click(object sender, EventArgs e)
        {
            wsjtxClient.ReplyModeChanged(true);
        }

        private void pauseButton_Click(object sender, EventArgs e)
        {
            wsjtxClient.Pause();
        }

        private void freqCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (!formLoaded) return;
            wsjtxClient.WsjtxSettingChanged();
            wsjtxClient.AutoFreqChanged(freqCheckBox.Checked);
        }

        private void stopCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (!formLoaded) return;
            wsjtxClient.stopEnabledChanged();
        }

        private void stopTextBox_TextChanged(object sender, EventArgs e)
        {
            stopCheckBox.Checked = false;
        }
    }
}
