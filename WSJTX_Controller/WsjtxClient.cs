//NOTE CAREFULLY: Several message classes require the use of a slightly modified WSJT-X program.
//Further information is in the README file.

using System;
//using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using WsjtxUdpLib.Messages;
using WsjtxUdpLib.Messages.Out;
using System.Drawing;
using System.Text.RegularExpressions;

namespace WSJTX_Controller
{
    public class WsjtxClient : IDisposable
    {
        public static int minSkipCount = 2, maxSkipCount = 20;

        public Controller ctrl;
        public bool altListPaused = false;
        public UdpClient udpClient;
        public int port;
        public IPAddress ipAddress;
        public bool multicast;
        public bool debug;
        public bool advanced;
        public string pgmName;
        public DateTime firstRunDateTime;
        public bool diagLog = false;
        public bool replyAndQuit = false;
        public bool paused = true;
        public bool showTxModes = false;

        private List<string> acceptableWsjtxVersions = new List<string> { "2.3.0/154" };
        private List<string> supportedModes = new List<string>() { "FT8", "FT4", "FST4" };

        //const
        public int maxPrevCqs = 2;
        public int maxPrevPotaCqs = 4;
        public int maxAutoGenEnqueue = 4;
        public int maxTimeoutCalls = 2;

        private StreamWriter logSw = null;
        private StreamWriter potaSw = null;
        private bool suspendComm = false;
        private bool settingChanged = false;
        private string cmdCheck = "";
        private bool commConfirmed = false;
        public string myCall = null, myGrid = null;
        private Dictionary<string, DecodeMessage> callDict = new Dictionary<string, DecodeMessage>();
        private Queue<string> callQueue = new Queue<string>();
        private List<string> reportList = new List<string>();
        private Dictionary<string, List<DecodeMessage>> allCallDict = new Dictionary<string, List<DecodeMessage>>();            //all calls to this station (except 73)
        private Dictionary<string, int> cqCallDict = new Dictionary<string, int>();         //cqs sent to specific stations
        private Dictionary<string, int> timeoutCallDict = new Dictionary<string, int>();    //calls sent to myCall immed after timeout
        private bool txEnabled = false;
        private bool transmitting = false;
        private bool autoCalling = true;
        private bool decoding = false;
        private WsjtxMessage.QsoStates qsoState = WsjtxMessage.QsoStates.CALLING;
        private string mode = "";
        private bool modeSupported = true;
        //private bool? lastModeSupported = null;
        private string rawMode = "";
        private bool txFirst = false;
        private bool dblClk = false;
        private int? trPeriod = null;       //msec
        private ulong dialFrequency = 0;
        private UInt32 txOffset = 0;
        private string replyCmd = null;     //no "reply to" cmd sent to WSJT-X yet, will not be a CQ
        private string curCmd = null;       //cmd last issed, can be CQ
        private DecodeMessage replyDecode = null;
        private string configuration = null;
        private TimeSpan latestDecodeTime;
        private string callInProg = null;
        private bool restartQueue = false;

        private WsjtxMessage.QsoStates lastQsoState = WsjtxMessage.QsoStates.INVALID;
        private UdpClient udpClient2;
        private IPEndPoint endPoint;
        private bool? lastXmitting = null;
        private bool? lastTxWatchdog = null;
        private string dxCall = null;
        private string lastMode = null;
        private ulong? lastDialFrequency = null;
        private bool? lastTxFirst = null;
        private bool? lastDecoding = null;
        private int? lastSpecOp = null;
        private string lastTxMsg = null;
        private bool? lastTxEnabled = null;
        private string lastCallInProg = null;
        private bool? lastAutoCalling = null;
        private bool? lastTxTimeout = null;
        private string lastReplyCmd = null;
        //private string lastCurCmd = null;
        private WsjtxMessage.QsoStates lastQsoStateDebug = WsjtxMessage.QsoStates.INVALID;
        private string lastDxCallDebug = null;
        private string lastTxMsgDebug = null;
        private string lastLastTxMsgDebug = null;

        private string lastDxCall = null;
        private int xmitCycleCount = 0;
        private bool txTimeout = false;
        private bool newDirCq = false;
        private int specOp = 0;
        private string tCall = null;            //call sign being processed at timeout
        private string txMsg = null;            //msg for the most-recent Tx
        private List<string> logList = new List<string>();      //calls logged for current mode/band for this session
        private Dictionary<string, List<string>> potaLogDict = new Dictionary<string, List<string>>();      //calls logged for any mode/band for this day: "call: date,band,mode"

        private AsyncCallback asyncCallback;
        private UdpState s;
        private static bool messageRecd;
        private static byte[] datagram;
        private static IPEndPoint fromEp = new IPEndPoint(IPAddress.Any, 0);
        private static bool recvStarted;
        private string failReason = "Failure reason: Unknown";

        public const int maxQueueLines = 6, maxQueueWidth = 18, maxLogWidth = 9;
        private byte[] ba;
        private EnableTxMessage emsg;
        //HaltTxMessage amsg;
        private WsjtxMessage msg = new UnknownMessage();
        private string errorDesc = null;
        private Random rnd = new Random();
        DateTime firstDecodeTime;
        private const string spacer = "           *";
        private bool ignoreTxDisable = false;
        private bool firstDecodePass = true;
        private System.Windows.Forms.Timer decodeEndTimer = new System.Windows.Forms.Timer();
        private System.Windows.Forms.Timer processDecodeTimer = new System.Windows.Forms.Timer();
        string path = $"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\{Assembly.GetExecutingAssembly().GetName().Name.ToString()}";
        List<int> audioOffsets = new List<int>();
        int oddOffset = 0;
        int lastOddOffset = 0;
        int evenOffset = 0;
        int lastEvenOffset = 0;
        const int offsetLoLimit = 300;
        const int offsetHiLimit = 2800;
        bool skipAudioOffsetCalc = true;
        const int maxTxTimeHrs = 4;      //hours
        const int maxDecodeAgeMinutes = 30;
        DateTime txStopDateTime = DateTime.MaxValue;
        private System.Windows.Forms.Timer heartbeatRecdTimer = new System.Windows.Forms.Timer();


        private struct UdpState
        {
            public UdpClient u;
            public IPEndPoint e;
        }

        private enum OpModes
        {
            IDLE,
            START,
            ACTIVE
        }
        private OpModes opMode;

        private enum Periods
        {
            UNK,
            ODD,
            EVEN
        }
        private Periods period;

        public WsjtxClient(Controller c, IPAddress reqIpAddress, int reqPort, bool reqMulticast, bool reqDebug, bool reqLog)
        {
            ctrl = c;           //used for accessing/updating UI
            ipAddress = reqIpAddress;
            port = reqPort;
            multicast = reqMulticast;
            //major.minor.build.private
            string allVer = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion;
            Version v;
            Version.TryParse(allVer, out v);
            string fileVer = $"{v.Major}.{v.Minor}.{v.Build}";
            WsjtxMessage.PgmVersion = fileVer;
            debug = reqDebug;
            opMode = OpModes.IDLE;
            ClearAudioOffsets();
            if (ctrl.freqCheckBox.Checked) WsjtxSettingChanged();
            WsjtxMessage.NegoState = WsjtxMessage.NegoStates.INITIAL;
            pgmName = ctrl.Text;      //or Assembly.GetExecutingAssembly().GetName().ToString();

            if (reqLog)            //request log file open
            {
                diagLog = SetLogFileState(true);
                if (diagLog)
                {
                    DebugOutput($"{DateTime.UtcNow.ToString("yyyy-MM-dd HHmmss")} UTC ###################### Program starting.... v{fileVer} ipAddress:{ipAddress} port:{port} multicast:{multicast}");
                }
            }

            ResetNego();
            UpdateDebug();

            string modeStr = multicast ? "multicast" : "unicast";
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
            catch
            {
                MessageBox.Show($"Unable to open the provided IP address ({ipAddress}) port ({port}) and mode: ({modeStr}).\n\nEnter a different IP address/port/mode in the dialog that follows.", pgmName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                ctrl.wsjtxClient = this;
                ctrl.setupButton_Click(null, null);
                return;
            }

            s = new UdpState();
            s.e = endPoint;
            s.u = udpClient;
            asyncCallback = new AsyncCallback(ReceiveCallback);

            DebugOutput($"{Time()} NegoState:{WsjtxMessage.NegoState}");
            DebugOutput($"{Time()} opMode:{opMode}");

            DebugOutput($"{Time()} Waiting for heartbeat...");

            ShowStatus();
            ShowQueue();
            ShowLogged();
            messageRecd = false;
            recvStarted = false;

            string cast = multicast ? "(multicast)" : "(unicast)";
            ctrl.verLabel.Text = $"by WM8Q v{fileVer}";
            ctrl.verLabel2.Text = "more.avantol@xoxy.net";
            ctrl.verLabel3.Text = "More features?";

            ctrl.timeoutLabel.Visible = false;

            UpdateModeSelection();
            UpdateModeVisible();
            UpdateTxStopTimeEnable();

            emsg = new EnableTxMessage();
            emsg.Id = WsjtxMessage.UniqueId;

            //amsg = new HaltTxMessage();
            //amsg.Id = WsjtxMessage.UniqueId;
            //amsg.AutoOnly = true;
            firstDecodeTime = DateTime.MinValue;

            decodeEndTimer.Interval = 3000;
            decodeEndTimer.Tick += new System.EventHandler(DecodesCompleted);

            processDecodeTimer.Tick += new System.EventHandler(ProcessDecodeTimerTick);

            ReadPotaLogDict();

            heartbeatRecdTimer.Interval = 60000;            //heartbeats every 15 sec
            heartbeatRecdTimer.Tick += new System.EventHandler(HeartbeatNotRecd);

            UpdateDebug();          //last before starting loop
        }

        public void UpdateAddrPortMulti(IPAddress reqIpAddress, int reqPort, bool reqMulticast)
        {
            suspendComm = true;

            try
            {
            }
            catch (Exception err)
            {
                DebugOutput($"{err}");
            }

            ipAddress = reqIpAddress;
            port = reqPort;
            multicast = reqMulticast;
            ctrl.CloseComm();
            ctrl.Close();
        }

        public void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                UdpClient u = ((UdpState)(ar.AsyncState)).u;
                fromEp = ((UdpState)(ar.AsyncState)).e;
                datagram = u.EndReceive(ar, ref fromEp);
                //string receiveString = Encoding.ASCII.GetString(datagram);
            }
            catch (Exception err)
            {
#if DEBUG
                Console.WriteLine($"Exception: ReceiveCallback() {err}");
#endif
                return;
            }

            //DebugOutput($"Received: {receiveString}");
            messageRecd = true;
        }

        public void UdpLoop()
        {
            if (udpClient == null) return;

            //timer expires at 11-12 msec minimum (due to OS limitations)
            if (messageRecd)
            {
                Update();
                messageRecd = false;
                recvStarted = false;
            }
            // Receive a UDP datagram
            if (!recvStarted)
            {
                if (udpClient == null) return;
                udpClient.BeginReceive(asyncCallback, s);
                recvStarted = true;
            }
        }

        public void AutoFreqChanged(bool enabled)
        {
            if (enabled)
            {
                //if (commConfirmed) EnableMonitoring();       may crash WSJT-X
                if ((oddOffset > 0 && evenOffset > 0) || opMode != OpModes.ACTIVE) return;

                ctrl.freqCheckBox.Text = "Select best TX frequency (pending)";
                ctrl.freqCheckBox.ForeColor = Color.DarkGreen;

                paused = true;
                processDecodeTimer.Stop();         //no decodes now
                DebugOutput($"{Time()} processDecodeTimer stop");
                DisableTx(false);
                opMode = OpModes.START;
                autoCalling = true;
                txTimeout = false;
                replyCmd = null;
                curCmd = null;
                replyDecode = null;
                tCall = null;
                newDirCq = false;
                dxCall = null;
                xmitCycleCount = 0;
                SetCallInProg(null);
                UpdateCallInProg();
                UpdateModeVisible();
                UpdateModeSelection();
                UpdateTxStopTimeEnable();
                ShowStatus();
                DebugOutput($"\n\n{Time()} AutoFreqChanged enabled:true, opMode:{opMode} NegoState:{WsjtxMessage.NegoState}");
            }
            else
            {
                ctrl.freqCheckBox.Text = "Select best TX frequency";
                ctrl.freqCheckBox.ForeColor = Color.Black;
                DebugOutput($"\n\n{Time()} AutoFreqChanged enabled:false");
            }
            UpdateDebug();
        }

        public void stopEnabledChanged()
        {
            if (ctrl.stopCheckBox.Checked)
            {
                string s = ctrl.stopTextBox.Text.Trim();
                int i;
                if (s.Length != 4 || !int.TryParse(s, out i) || i < 0 || i > 2359)
                {
                    ctrl.ShowMsg("Use 24-hour format, 4 digits (ex: 0900)", true);
                    ctrl.stopCheckBox.Checked = false;
                    return;
                }

                DateTime dtStop = ScheduledOffDateTime();
                if ((dtStop - DateTime.Now).TotalMinutes > maxTxTimeHrs * 60)           //local time
                {
                    ctrl.ShowMsg($"Maximum TX time is {maxTxTimeHrs} hours", true);
                    ctrl.stopCheckBox.Checked = false;
                    return;
                }

                txStopDateTime = dtStop;
                DebugOutput($"{Time()} txStopDateTime:{txStopDateTime}");
            }
        }

        //log file mode requested to be (possibly) changed
        public void LogModeChanged(bool enable)
        {
            if (enable == diagLog) return;       //no change requested

            diagLog = SetLogFileState(enable);
        }

        public void ReplyModeChanged(bool newMode)
        {
            bool prevMode = replyAndQuit;
            paused = false;
            replyAndQuit = newMode;
            DebugOutput($"{Time()} ReplyModeChanged replyAndQuit:{replyAndQuit} paused:{paused} callInProg:{callInProg} restartQueue:{restartQueue}");

            if (replyAndQuit)
            {
                autoCalling = true;
                DebugOutput($"{spacer}autoCalling:{autoCalling}");

                if (callQueue.Count > 0)    //txEnabled = false
                {
                    restartQueue = true;
                    DebugOutput($"{spacer}restartQueue:{restartQueue}");
                }
                else
                {
                    //if (!prevMode) txTimeout = true;     //stop CQing     //tempOnly
                }

                ctrl.ShowMsg($"CAUTION: Automatic transmit!", false);
                Play("beepbeep.wav");
            }
            else        //call CQ
            {
                CheckCallQueuePeriod(txFirst);        //remove queued calls from wrong time period

                if (callInProg == null || qsoState == WsjtxMessage.QsoStates.CALLING)
                {
                    txTimeout = true;
                    DebugOutput($"{spacer}txTimeout:{txTimeout}");
                    CheckNextXmit();
                }
                EnableTx();

                DateTime dtNow = DateTime.UtcNow;
                if (txFirst != IsEvenPeriod((dtNow.Second * 1000) + dtNow.Millisecond))
                {
                    ctrl.ShowMsg($"CAUTION: Automatic transmit!", false);
                    Play("beepbeep.wav");
                }

            }
                 ShowStatus();
                 UpdateDebug();
        }

        public void UpdateCallInProg()
        {
            if (!showTxModes)
            {
                if (callInProg == null)
                {
                    ctrl.inProgLabel.Visible = false;
                    ctrl.inProgTextBox.Visible = false;
                    ctrl.inProgTextBox.Text = "";
                }
                else
                {
                    ctrl.inProgTextBox.Text = callInProg;
                    ctrl.inProgTextBox.Visible = true;
                    ctrl.inProgLabel.Visible = true;
                }
            }
            else
            {
                if (ctrl.timer3.Enabled) return;
                if (callInProg == null)
                {
                    ctrl.msgTextBox.Text = "";
                }
                else
                {
                    ctrl.msgTextBox.Text = $"In progress: {callInProg}";
                }
            }
        }

        public void WsjtxSettingChanged()
        {
            settingChanged = true;
            newDirCq = true;
        }

        public void Pause()
        {
            paused = true;
            DisableTx(true);
            ShowStatus();
        }

        public void UpdateModeVisible()
        {
            if (advanced && opMode == OpModes.ACTIVE)
            {
                if (showTxModes)
                {
                    ctrl.listenModeButton.Visible = true;
                    ctrl.pauseButton.Visible = true;
                    ctrl.cqModeButton.Visible = true;
                    ctrl.modeHelpLabel.Visible = true;
                    ctrl.modeGroupBox.Visible = true;
                    ctrl.txWarnLabel.Visible = true;
                    ctrl.txWarnLabel2.Visible = true;
                }
                else
                {
                    ctrl.inProgTextBox.Visible = true;
                }
            }
            else
            {
                ctrl.listenModeButton.Visible = false;
                ctrl.pauseButton.Visible = false;
                ctrl.cqModeButton.Visible = false;
                ctrl.modeHelpLabel.Visible = false;
                ctrl.modeGroupBox.Visible = false;
                ctrl.txWarnLabel.Visible = false;
                ctrl.txWarnLabel2.Visible = false;
                ctrl.modeGroupBox.Visible = false;
            }
            DebugOutput($"{Time()} advanced:{advanced} showTxModes:{showTxModes} replyAndQuit:{replyAndQuit}");
        }

        private void Update()
        {
            if (suspendComm) return;

            try
            {
                msg = WsjtxMessage.Parse(datagram);
                //DebugOutput($"{Time()} msg:{msg} datagram[{datagram.Length}]:\n{DatagramString(datagram)}");
            }
            catch (ParseFailureException ex)
            {
                //File.WriteAllBytes($"{ex.MessageType}.couldnotparse.bin", ex.Datagram);
                DebugOutput($"{Time()} ERROR: Parse failure {ex.InnerException.Message}");
                DebugOutput($"datagram[{datagram.Length}]: {DatagramString(datagram)}");
                return;
            }

            if (msg == null)
            {
                DebugOutput($"{Time()} ERROR: null message, datagram[{datagram.Length}]: {DatagramString(datagram)}");
                return;
            }

            //rec'd first HeartbeatMessage
            //check version, send requested schema version
            //request a StatusMessage
            //go from INIT to SENT state
            if (msg.GetType().Name == "HeartbeatMessage" && (WsjtxMessage.NegoState == WsjtxMessage.NegoStates.INITIAL || WsjtxMessage.NegoState == WsjtxMessage.NegoStates.FAIL))
            {
                ctrl.timer4.Stop();             //stop connection fault dialog
                HeartbeatMessage imsg = (HeartbeatMessage)msg;
                DebugOutput($"{Time()}\n{imsg}");
                string curVerBld = $"{imsg.Version}/{imsg.Revision}";
                if (!acceptableWsjtxVersions.Contains(curVerBld))
                {
                    suspendComm = true;
                    MessageBox.Show($"WSJT-X v{imsg.Version}/{imsg.Revision} is not supported.\n\nSupported WSJT-X version(s):\n{AcceptableVersionsString()}\n\nYou can check the WSJT-X version/build by selecting 'Help | About' in WSJT-X.\n\n{pgmName} will try again when you close this dialog.", pgmName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    ResetNego();
                    suspendComm = false;
                    UpdateDebug();
                    return;
                }
                else
                {
                    var tmsg = new HeartbeatMessage();
                    tmsg.SchemaVersion = WsjtxMessage.PgmSchemaVersion;
                    tmsg.MaxSchemaNumber = (uint)WsjtxMessage.PgmSchemaVersion;
                    tmsg.SchemaVersion = WsjtxMessage.PgmSchemaVersion;
                    tmsg.Id = WsjtxMessage.UniqueId;
                    tmsg.Version = WsjtxMessage.PgmVersion;
                    tmsg.Revision = WsjtxMessage.PgmRevision;

                    ba = tmsg.GetBytes();
                    udpClient2 = new UdpClient();
                    udpClient2.Connect(fromEp);
                    udpClient2.Send(ba, ba.Length);
                    WsjtxMessage.NegoState = WsjtxMessage.NegoStates.SENT;
                    UpdateDebug();
                    DebugOutput($"{spacer}NegoState:{WsjtxMessage.NegoState}");
                    DebugOutput($"{Time()} >>>>>Sent'Heartbeat' msg:\n{tmsg}");
                    ShowStatus();
                }
                UpdateDebug();
                return;
            }

            //rec'd negotiation HeartbeatMessage
            //send another request for a StatusMessage
            //go from SENT to RECD state
            if (WsjtxMessage.NegoState == WsjtxMessage.NegoStates.SENT && msg.GetType().Name == "HeartbeatMessage")
            {
                HeartbeatMessage hmsg = (HeartbeatMessage)msg;
                DebugOutput($"{Time()}\n{hmsg}");
                WsjtxMessage.NegotiatedSchemaVersion = hmsg.SchemaVersion;
                WsjtxMessage.NegoState = WsjtxMessage.NegoStates.RECD;
                UpdateDebug();
                DebugOutput($"{spacer}NegoState:{WsjtxMessage.NegoState}");
                DebugOutput($"{spacer}negotiated schema version:{WsjtxMessage.NegotiatedSchemaVersion}");
                UpdateDebug();

                //send ACK request to WSJT-X, to get 
                //a StatusMessage reply to start normal operation
                Thread.Sleep(250);
                emsg.NewTxMsgIdx = 7;
                emsg.GenMsg = $"";          //no effect
                emsg.ReplyReqd = true;
                emsg.EnableTimeout = !debug;
                emsg.CmdCheck = cmdCheck;
                ba = emsg.GetBytes();
                udpClient2.Send(ba, ba.Length);
                DebugOutput($"{Time()} >>>>>Sent 'Ack Req' cmd:7 cmdCheck:{cmdCheck}\n{emsg}");

                ctrl.timer5.Interval = 30000;           //set up cmd check timeout
                ctrl.timer5.Start();
                DebugOutput($"{spacer}check cmd timer started");

                heartbeatRecdTimer.Start();
                DebugOutput($"{spacer}heartbeatRecdTimer started");
                return;
            }

            //while in INIT or SENT state:
            //get minimal info from StatusMessage needed for faster startup
            //and for special case of ack msg returned by WSJT-X after req for StatusMessage
            //check for no call sign or grid, exit if so
            if (WsjtxMessage.NegoState != WsjtxMessage.NegoStates.RECD && msg.GetType().Name == "StatusMessage")
            {
                StatusMessage smsg = (StatusMessage)msg;
                //DebugOutput($"\n{Time()}\n{smsg}");
                txEnabled = smsg.TxEnabled;
                if (lastTxEnabled == null) lastTxEnabled = smsg.TxEnabled;
                if (txEnabled != lastTxEnabled && txEnabled) ctrl.ShowMsg("Not ready yet... please wait", true);
                lastTxEnabled = txEnabled;
                mode = smsg.Mode;
                specOp = (int)smsg.SpecialOperationMode;
                CheckModeSupported();
                configuration = smsg.ConfigurationName.Trim().Replace(' ', '-');
                if (!CheckMyCall(smsg)) return;
                DebugOutput($"{Time()}\nStatus     myCall:'{myCall}' myGrid:'{myGrid}' mode:{mode} specOp:{specOp} configuration:{configuration} check:{smsg.Check}");
                UpdateDebug();
            }

            if (WsjtxMessage.NegoState != WsjtxMessage.NegoStates.RECD && msg.GetType().Name == "EnqueueDecodeMessage")
            {
                EnqueueDecodeMessage qmsg = (EnqueueDecodeMessage)msg;
                if (!qmsg.AutoGen) ctrl.ShowMsg("Not ready yet... please wait", true);
            }

            //************
            //CloseMessage
            //************
            if (msg.GetType().Name == "CloseMessage")
            {
                DebugOutput($"\n{Time()}\n{msg}");

                heartbeatRecdTimer.Stop();
                DebugOutput($"{Time()} heartbeatRecdTimer stop");

                if (udpClient2 != null) udpClient2.Close();
                ResetNego();     //wait for (new) WSJT-X mode
                return;
            }

            //****************
            //HeartbeatMessage
            //****************
            //in case 'Monitor' disabled, get StatusMessages
            if (msg.GetType().Name == "HeartbeatMessage")
            {
                DebugOutput($"{Time()}\n{msg}");
                emsg.NewTxMsgIdx = 7;
                emsg.GenMsg = $"";          //no effect
                emsg.ReplyReqd = (opMode != OpModes.ACTIVE);
                emsg.EnableTimeout = !debug;
                if (emsg.ReplyReqd) cmdCheck = RandomCheckString();
                emsg.CmdCheck = cmdCheck;
                ba = emsg.GetBytes();
                udpClient2.Send(ba, ba.Length);
                DebugOutput($"{Time()} >>>>>Sent 'Ack Req' cmd:7 cmdCheck:{cmdCheck}\n{emsg}");

                heartbeatRecdTimer.Stop();
                heartbeatRecdTimer.Start();
                DebugOutput($"{spacer}heartbeatRecdTimer restarted");

                //if (ctrl.freqCheckBox.Checked && commConfirmed) EnableMonitoring();   may crash WSJT-X
            }

            if (WsjtxMessage.NegoState == WsjtxMessage.NegoStates.RECD)
            {
                if (modeSupported)
                {
                    //*************
                    //DecodeMessage
                    //*************
                    //only resulting action is to add call to callQueue, optionally restart queue
                    if (msg.GetType().Name == "DecodeMessage" && myCall != null)
                    {
                        DecodeMessage dmsg = (DecodeMessage)msg;
                        latestDecodeTime = dmsg.SinceMidnight;
                        bool recdPrevSignoff = false;
                        rawMode = dmsg.Mode;    //different from mode string in status msg

                        if (dmsg.New)           //important to reject replays requested by other pgms, also reject all contest msgs
                        {
                            if (dmsg.DeltaFrequency > offsetLoLimit && dmsg.DeltaFrequency < offsetHiLimit) audioOffsets.Add(dmsg.DeltaFrequency);

                            if (dmsg.IsContest())
                            {
                                return;
                            }

                            string deCall = dmsg.DeCall();
                            if (dmsg.IsCallTo(myCall))
                            {
                                DebugOutput($"{dmsg}\n{spacer}msg:'{dmsg.Message}'");
                                DebugOutput($"{Time()} deCall:'{deCall}' callInProg:'{callInProg}' txEnabled:{txEnabled} transmitting:{transmitting} restartQueue:{restartQueue}");
                                int prevTimeouts = 0;
                                timeoutCallDict.TryGetValue(deCall, out prevTimeouts);
                                if (prevTimeouts >= maxTimeoutCalls)
                                {
                                    ctrl.ShowMsg($"Blocking {deCall} temporarily...", false);
                                    DebugOutput($"{spacer}ignoring call, prevTimeouts:{prevTimeouts} restartQueue:{restartQueue}");
                                    return;
                                }
                            }

                            //do some processing not directly related to replying immediately
                            if (deCall != null)
                            {
                                //check for decode being a call to myCall
                                if (myCall != null && dmsg.IsCallTo(myCall))
                                {
                                    dmsg.Priority = true;       //as opposed to a decode from anyone else
                                    if (ctrl.mycallCheckBox.Checked) Play("trumpet.wav");   //not the call just logged

                                    //detect previous signoff before adding call to allCallDict
                                    recdPrevSignoff = RecdSignoff(deCall);
                                    DebugOutput($"{spacer}recdPrevSignoff:{recdPrevSignoff}");

                                    //if call not logged this band/session: save Report (...+03) and RogerReport (...-02) decodes for out-of-order call processing
                                    if (!logList.Contains(deCall) && (WsjtxMessage.IsReport(dmsg.Message) || WsjtxMessage.IsRogerReport(dmsg.Message) || WsjtxMessage.IsReply(dmsg.Message) || WsjtxMessage.Is73orRR73(dmsg.Message)))
                                    {
                                        List<DecodeMessage> vlist;
                                        //create new List for deCall if nothing entered yet into the Dictionary
                                        if (!allCallDict.TryGetValue(deCall, out vlist)) allCallDict.Add(deCall, vlist = new List<DecodeMessage>());
                                        vlist.Add(dmsg);        //messages from deCall are in order rec'd, will be duplicates of any message types
                                    }
                                    CheckLateLog(deCall, dmsg);
                                    UpdateDebug();
                                }
                            }

                            if (!txEnabled && deCall != null && myCall != null && dmsg.IsCallTo(myCall) && !recdPrevSignoff && !dmsg.Is73orRR73())
                            {
                                if (!callQueue.Contains(deCall))
                                {
                                    DebugOutput($"{spacer}'{deCall}' not in queue");
                                    AddCall(deCall, dmsg);
                                    Play("blip.wav");

                                    if (replyAndQuit && !paused && !restartQueue && callQueue.Count == 1)       //txEnabled = false
                                    {
                                        restartQueue = true;
                                        DebugOutput($"{spacer}restartQueue:{restartQueue}");
                                    }
                                }
                                else
                                {
                                    DebugOutput($"{spacer}'{deCall}' already in queue");
                                    UpdateCall(deCall, dmsg);
                                }
                                UpdateDebug();
                            }

                            //decode processing of calls to myCall requires txEnabled
                            if (txEnabled && deCall != null && myCall != null && dmsg.IsCallTo(myCall))
                            {
                                DebugOutput($"{spacer}'{deCall}' is to {myCall}");
                                if ((deCall == callInProg || (txTimeout && deCall == tCall)) && recdPrevSignoff)        //cancel call in progress
                                {
                                    restartQueue = true;
                                    DebugOutput($"{spacer}already rec'd signoff, restartQueue:{restartQueue} qsoState:{qsoState}");
                                }
                                else
                                {
                                    if (!dmsg.Is73orRR73())       //not a 73 or RR73
                                    {
                                        DebugOutput($"{spacer}Not a 73 or RR73");
                                        if (deCall != callInProg)
                                        {
                                            DebugOutput($"{spacer}{deCall} is not callInProg:{callInProg}");
                                            if (!callQueue.Contains(deCall))        //call not in queue, enqueue the call data
                                            {
                                                DebugOutput($"{spacer}'{deCall}' not already in queue");
                                                AddCall(deCall, dmsg);
                                                Play("blip.wav");

                                                //interrupt CQing for a call to us, if callInProg not getting replies (or already logegd)
                                                bool noMsgsDeCall = !RecdAnyMsg(callInProg);
                                                if (noMsgsDeCall) restartQueue = true;
                                                DebugOutput($"{spacer}noMsgsDeCall:{noMsgsDeCall}  restartQueue:{restartQueue}");

                                                if (transmitting)
                                                {
                                                    DebugOutput($"{spacer}decode overlap transmit, qsoState:{qsoState}");
                                                }
                                            }
                                            else       //call is already in queue, update the call data
                                            {
                                                DebugOutput($"{spacer}'{deCall}' already in queue");
                                                UpdateCall(deCall, dmsg);
                                            }
                                        }
                                        else        //call is in progress
                                        {
                                            DebugOutput($"{spacer}{deCall} is callInProg, txTimeout:{txTimeout}");
                                            if (txTimeout && deCall == tCall)    //just timed out at last Tx
                                            {
                                                //this caller might call indefinitely, so count call attempts
                                                int prevTimeouts = 0;
                                                if (!timeoutCallDict.TryGetValue(deCall, out prevTimeouts) || prevTimeouts < maxTimeoutCalls)
                                                {
                                                    AddCall(deCall, dmsg);
                                                    DebugOutput($"{spacer}Timeout at last Tx for {deCall} prevTimeouts:{prevTimeouts}, re-add to queue");
                                                    if (prevTimeouts > 0)
                                                    {
                                                        timeoutCallDict.Remove(deCall);
                                                    }
                                                    timeoutCallDict.Add(deCall, prevTimeouts + 1);
                                                }
                                            }
                                        }
                                    }
                                    else        //decode is 73 or RR73 msg
                                    {
                                        DebugOutput($"{spacer}decode is 73 or RR73");
                                        if (deCall == callInProg)
                                        {
                                            restartQueue = true;        //txEnabled = true
                                            DebugOutput($"{spacer}call is in progress, restartQueue:{restartQueue}");
                                        }
                                    }
                                }
                                UpdateDebug();
                            }
                        }
                        return;
                    }

                    //********************
                    //EnqueueDecodeMessage
                    //********************
                    //only resulting action is to add call to callQueue, optionally restart queue
                    if (msg.GetType().Name == "EnqueueDecodeMessage" && myCall != null)
                    {
                        EnqueueDecodeMessage qmsg = (EnqueueDecodeMessage)msg;
                        DebugOutput($"{qmsg}\n{spacer}msg:'{qmsg.Message}'");
                        AddSelectedCall(qmsg);              //known to be "new" and not "replay"
                        UpdateDebug();
                    }
                }

                //*************
                //StatusMessage
                //*************
                if (msg.GetType().Name == "StatusMessage")
                {
                    StatusMessage smsg = (StatusMessage)msg;
                    DateTime dtNow = DateTime.UtcNow;
                    if (opMode < OpModes.ACTIVE) DebugOutput($"{Time()}\n{msg}");
                    qsoState = smsg.CurQsoState();
                    txEnabled = smsg.TxEnabled;
                    dxCall = smsg.DxCall;                               //unreliable info, can be edited manually
                    if (dxCall == "") dxCall = null;
                    mode = smsg.Mode;
                    specOp = (int)smsg.SpecialOperationMode;
                    txMsg = WsjtxMessage.RemoveAngleBrackets(smsg.LastTxMsg);        //msg from last Tx
                    txFirst = smsg.TxFirst;
                    decoding = smsg.Decoding;
                    transmitting = smsg.Transmitting;
                    dialFrequency = smsg.DialFrequency;
                    txOffset = smsg.TxDF;
                    dblClk = smsg.DblClk;           //event, not state

                    if (lastXmitting == null) lastXmitting = transmitting;     //initialize
                    if (lastQsoState == WsjtxMessage.QsoStates.INVALID) lastQsoState = qsoState;    //initialize WSJT-X user QSO state change detection
                    if (lastTxEnabled == null) lastTxEnabled = txEnabled;     //initializlastGenMsge
                    if (lastDecoding == null) lastDecoding = decoding;     //initialize
                    if (lastTxWatchdog == null) lastTxWatchdog = smsg.TxWatchdog;   //initialize
                    if (lastTxFirst == null) lastTxFirst = txFirst;                     //initialize
                    if (lastDialFrequency == null) lastDialFrequency = smsg.DialFrequency; //initialize
                    if (smsg.TRPeriod != null) trPeriod = (int)smsg.TRPeriod;

                    if (ctrl.timer5.Enabled && smsg.Check == cmdCheck)             //found the random cmd check string, cmd receive ack'd
                    {
                        ctrl.timer5.Stop();
                        commConfirmed = true;
                        DebugOutput($"{Time()} Check cmd rec'd, match");
                    }

                    if (myCall == null || myGrid == null)
                    {
                        CheckMyCall(smsg);
                    }
                    else
                    {
                        if (myCall != smsg.DeCall || myGrid != smsg.DeGrid)
                        {
                            DebugOutput($"{Time()} Call or grid changed, myCall:{smsg.DeCall} (was {myCall} myGrid:{smsg.DeGrid} (was {myGrid})");
                            myCall = smsg.DeCall;
                            myGrid = smsg.DeGrid;

                            ResetOpMode(false);
                            txTimeout = true;       //cancel current calling
                            SetCallInProg(null);    //not calling anyone
                            if (!paused) CheckNextXmit();
                        }
                    }

                    //detect xmit start/end ASAP
                    if (trPeriod != null && transmitting != lastXmitting)
                    {
                        if (transmitting)
                        {
                            StartProcessDecodeTimer();
                            ProcessTxStart();
                            if (firstDecodeTime == DateTime.MinValue) firstDecodeTime = DateTime.UtcNow;       //start counting until WSJT-X watchdog timer set
                        }
                        else                //end of transmit
                        {
                            ProcessTxEnd();
                        }
                        lastXmitting = transmitting;
                        ShowStatus();
                    }

                    //check for manual operation started
                    if (dblClk)             //event, not state: double-clicked on list of calls, WSJT-X will start calling immediately
                    {
                        DebugOutput($"{Time()} dblClk:{dblClk}");
                        if (callQueue.Count == 0 || callQueue.Peek() != dxCall)         //selected call is not the one to be dequeued next
                        {
                            if (autoCalling)                //only cancel timeout the first time manual operation detected
                            {
                                txTimeout = false;          //cancel timeout or settings change, which would start auto CQing
                                if (txFirst == IsEvenPeriod((dtNow.Second * 1000) + dtNow.Millisecond))
                                {
                                    xmitCycleCount = -1;         //Tx enabled during the same period Tx will happen, add one more tx cycle before timeout
                                }
                                else
                                {
                                    xmitCycleCount = 0;         //restart timeout for the new call
                                }
                                replyCmd = null;            //last reply cmd sent is no longer in effect
                                replyDecode = null;
                                ShowStatus();
                                UpdateDebug();
                                DebugOutput($"{spacer}new DX call selected manually during Rx: txTimeout:{txTimeout} xmitCycleCount:{xmitCycleCount} replyCmd:'{replyCmd}' autoCalling:{autoCalling}");
                            }
                        }
                        if (!replyAndQuit) autoCalling = false;
                        ShowStatus();
                        DebugOutputStatus();
                    }

                    //autoCalling status may have changed
                    if (!autoCalling)
                    {
                        SetCallInProg(dxCall);            //dxCall may be set later than dblClk
                    }


                    //detect WSJT-X mode change
                    if (mode != lastMode)
                    {
                        DebugOutput($"{Time()} Mode changed, mode:{mode} (was {lastMode})");
                        if (opMode == OpModes.ACTIVE)
                        {
                            ResetOpMode(true);
                            ClearAudioOffsets();
                            txTimeout = true;       //cancel current calling
                            SetCallInProg(null);      //not calling anyone
                            if (!paused) CheckNextXmit();
                        }
                        CheckModeSupported();
                        lastMode = mode;
                    }

                    //detect WSJT-X special operating mode change
                    if (specOp != lastSpecOp)
                    {
                        DebugOutput($"{Time()} Special operating mode changed, specOp:{specOp} (was {lastSpecOp})");
                        if (opMode == OpModes.ACTIVE) ResetOpMode(true);
                        CheckModeSupported();
                        lastSpecOp = specOp;
                    }

                    //check for time to flag starting first xmit
                    if (commConfirmed && supportedModes.Contains(mode) && specOp == 0 && opMode == OpModes.IDLE)
                    {

                        if (txEnabled)
                        {
                            DisableTx(true);        //do it this way for special handling of the status message that follows      
                        }
                        if (transmitting)
                        {
                            HaltTx();               //must do DisableTx first
                        }

                        EnableMonitoring();         //must do after DisableTx and HaltTx

                        opMode = OpModes.START;
                        ShowStatus();
                        UpdateModeVisible();
                        ClearAudioOffsets();
                        DebugOutput($"{Time()} opMode:{opMode}");
                    }

                    //detect decoding start/end
                    if (smsg.Decoding != lastDecoding)
                    {
                        if (smsg.Decoding)
                        {
                            decodeEndTimer.Stop();
                            decodeEndTimer.Start();                    //restart timer at every decode, will time out after last decode
                            DebugOutput($"{Time()} Decode start, decodeEndTimer restarted, processDecodeTimer.Enabled:{processDecodeTimer.Enabled}");
                            if (firstDecodePass)
                            {
                                if (!processDecodeTimer.Enabled)           //was started at end of last xmit
                                {
                                    int msec = (dtNow.Second * 1000) + dtNow.Millisecond;
                                    period = IsEvenPeriod(msec) ? Periods.EVEN : Periods.ODD;       //determine this period
                                    DebugOutput($"{spacer}msec:{msec} period:{period} trPeriod:{trPeriod}");
                                    int diffMsec = msec % (int)trPeriod;
                                    int cycleTimerAdj = CalcTimerAdj();
                                    int interval = Math.Max(((int)trPeriod) - diffMsec - cycleTimerAdj, 1);
                                    DebugOutput($"{spacer}diffMsec:{diffMsec} interval:{interval} cycleTimerAdj:{cycleTimerAdj}");
                                    if (interval > 0)
                                    {
                                        processDecodeTimer.Interval = interval;
                                        processDecodeTimer.Start();
                                        DebugOutput($"{spacer}processDecodeTimer start");
                                    }
                                }
                                firstDecodePass = false;
                                DebugOutput($"{spacer}firstDecodePass:{firstDecodePass}");
                            }
                        }
                        else
                        {
                            DebugOutput($"{Time()} Decode end");
                        }
                        lastDecoding = smsg.Decoding;
                    }

                    //check for changed QSO state in WSJT-X
                    if (lastQsoState != qsoState)
                    {
                        DebugOutput($"{Time()} qsoState:{qsoState} (was {lastQsoState})");
                        lastQsoState = qsoState;
                        DebugOutputStatus();
                    }

                    //check for Tx halt clicked when "Enable Tx" is already unchecked
                    if (smsg.TxHaltClk)
                    {
                        DebugOutput($"{Time()} TxHaltClk paused:{paused} replyAndQuit:{replyAndQuit} processDecodeTimer.Enabled:{processDecodeTimer.Enabled}");
                        if (processDecodeTimer.Enabled)
                        {
                            processDecodeTimer.Stop();       //no xmit cycle now
                            DebugOutput($"{spacer}processDecodeTimer stop");
                        }
                        paused = true;
                        UpdateModeSelection();
                    }

                    //check for changed Tx enabled
                    if (lastTxEnabled != txEnabled)
                    {
                        DebugOutput($"{Time()} txEnabled:{txEnabled} (was {lastTxEnabled}) paused:{paused} replyAndQuit:{replyAndQuit}\n{spacer}ignoreTxDisable:{ignoreTxDisable} processDecodeTimer.Enabled:{processDecodeTimer.Enabled}");

                        if (!txEnabled)         //this case will happen DisableTx() called
                        {
                            if (!ignoreTxDisable)
                            {
                                if (processDecodeTimer.Enabled)
                                {
                                    processDecodeTimer.Stop();       //no xmit cycle now
                                    DebugOutput($"{spacer}processDecodeTimer stop");
                                }
                                paused = true;
                                UpdateModeSelection();
                            }
                            ignoreTxDisable = false;
                            DebugOutput($"{spacer}ignoreTxDisable:{ignoreTxDisable}");
                        }

                        if (txEnabled && paused)
                        {
                            paused = false;
                            UpdateModeSelection();
                        }

                        if (txEnabled && !replyAndQuit && qsoState == WsjtxMessage.QsoStates.CALLING)
                        {
                            DebugOutput($"{Time()} Tx enabled: starting queue processing");
                            txTimeout = true;                   //triggers next in queue
                            DebugOutput($"{spacer}txTimeout:{txTimeout}");
                            if (txFirst == IsEvenPeriod((dtNow.Second * 1000) + dtNow.Millisecond))
                            {
                                xmitCycleCount = -1;        //Tx enabled during the same period Tx will happen, add one more tx cycle before timeout
                                DebugOutput($"{spacer}Tx enabled during Tx period");
                            }
                            else
                            {
                                xmitCycleCount = 0;
                                DebugOutput($"{spacer}Tx enabled during Rx period");
                            }
                            CheckNextXmit();            //process the timeout
                        }

                        ShowStatus();
                        lastTxEnabled = txEnabled;
                    }

                    //check for watchdog timer status changed
                    if (smsg.TxWatchdog != smsg.TxWatchdog)
                    {
                        DebugOutput($"{Time()} smsg.TxWatchdog:{smsg.TxWatchdog} (was {lastTxWatchdog})");
                        if (smsg.TxWatchdog && opMode == OpModes.ACTIVE)        //only need this event if in valid mode
                        {
                            if (firstDecodeTime != DateTime.MinValue)
                            {
                                if ((DateTime.UtcNow - firstDecodeTime).TotalMinutes < 15)
                                {
                                    ModelessDialog("Set the 'Tx watchdog' in WSJT-X to 15 minutes or longer.\n\nThis will be the timeout in case the Controller sends the same message repeatedly (for example, calling CQ when the band is closed).\n\nThe WSJT-X 'Tx watchdog' is under File | Settings, in the 'General' tab.");
                                }
                                else
                                {
                                    ModelessDialog("The 'Tx watchdog' in WSJT-X has timed out.\n\n(The WSJT-X 'Tx watchdog' setting is under File | Settings, in the 'General' tab).\n\nSelect an 'Operatng Mode' to continue.");
                                }
                                firstDecodeTime = DateTime.MinValue;        //allow timing to restart
                            }
                        }
                        lastTxWatchdog = smsg.TxWatchdog;
                    }

                    if (lastDialFrequency != null && (Math.Abs((float)lastDialFrequency - (float)dialFrequency) > 200))
                    {
                        DebugOutput($"{Time()} Freq changed:{dialFrequency / 1e6} (was:{lastDialFrequency / 1e6})");

                        if (FreqToBand(dialFrequency / 1e6) == FreqToBand(lastDialFrequency / 1e6))      //same band
                        {
                            if (ctrl.freqCheckBox.Checked)
                            {
                                ctrl.freqCheckBox.Checked = false;
                                ClearAudioOffsets();
                                ctrl.ShowMsg("'Select best Tx frequency' disabled", true);
                                DebugOutput($"{spacer}best Tx freq disabled");
                            }
                        }
                        else        //new band
                        {
                            if (opMode == OpModes.ACTIVE)
                            {
                                DebugOutput($"{spacer}Band changed:{FreqToBand(dialFrequency / 1e6)} (was:{FreqToBand(lastDialFrequency / 1e6)}");
                                if (ctrl.freqCheckBox.Checked) ResetOpMode(true);
                                ClearCalls(true);
                                ClearAudioOffsets();
                                logList.Clear();        //can re-log on new mode/band or in new session
                                ShowLogged();
                                txTimeout = true;       //cancel current calling
                                SetCallInProg(null);      //not calling anyone
                                paused = true;
                                UpdateModeSelection();
                                CheckNextXmit();
                                DebugOutput($"{spacer} Cleared queued calls:DialFrequency, txTimeout:{txTimeout} callInProg:'{callInProg}'");
                            }
                        }
                        lastDialFrequency = smsg.DialFrequency;
                    }

                    //detect WSJT-X Tx First change
                    if (txFirst != lastTxFirst)
                    {
                        DebugOutput($"{Time()} Tx first changed");
                        settingChanged = true;          //update CQ offset freq

                        if (!replyAndQuit)
                        {
                            ClearCalls(false);
                            if (autoCalling)
                            {
                                txTimeout = true;       //cancel current calling
                                SetCallInProg(null);    //not calling anyone
                                if (!paused) CheckNextXmit();        //there won't be a decode phase, so determine next Tx now
                            }
                            DebugOutput($"{Time()} Cleared queued calls: txFirst:{txFirst} txTimeout:{txTimeout} callInProg:'{callInProg}'");
                        }
                        lastTxFirst = txFirst;
                    }

                    CheckSetupCq();

                    //*****end of status *****
                    UpdateDebug();
                    return;
                }
            }
        }

        private void CheckSetupCq()
        {
            //check for setup for CQ
            if (commConfirmed && myCall != null && supportedModes.Contains(mode) && specOp == 0 && opMode == OpModes.START && (!ctrl.freqCheckBox.Checked || (oddOffset > 0 && evenOffset > 0)))
            {
                opMode = OpModes.ACTIVE;

                if (replyAndQuit)
                {
                    paused = true;
                    UpdateModeSelection();
                }

                UpdateDebug();
                UpdateModeVisible();
                UpdateTxStopTimeEnable();
                DebugOutput($"{Time()} opMode:{opMode}");
                ShowStatus();
                UpdateAddCall();

                //set/show frequency offset for period after decodes started
                emsg.NewTxMsgIdx = 10;
                emsg.GenMsg = $"";          //no effect
                emsg.SkipGrid = ctrl.skipGridCheckBox.Checked;
                emsg.UseRR73 = ctrl.useRR73CheckBox.Checked;
                emsg.CmdCheck = "";         //ignored
                emsg.Offset = AudioOffsetFromTxPeriod();
                ba = emsg.GetBytes();
                udpClient2.Send(ba, ba.Length);
                DebugOutput($"{Time()} >>>>>Sent 'Opt Req' cmd:10\n{emsg}");
                if (settingChanged)
                {
                    ctrl.WsjtxSettingConfirmed();
                    settingChanged = false;
                }

                //setup for CQ
                emsg.NewTxMsgIdx = 6;
                emsg.GenMsg = $"CQ{NextDirCq()} {myCall} {myGrid}";
                emsg.SkipGrid = ctrl.skipGridCheckBox.Checked;
                emsg.UseRR73 = ctrl.useRR73CheckBox.Checked;
                emsg.CmdCheck = "";         //ignored
                ba = emsg.GetBytes();           //re-enable Tx for CQ
                udpClient2.Send(ba, ba.Length);
                DebugOutput($"{Time()} >>>>>Sent 'Setup CQ' cmd:6\n{emsg}");
                qsoState = WsjtxMessage.QsoStates.CALLING;      //in case enqueueing call manually right now
                SetCallInProg(null);
                DebugOutput($"{spacer}qsoState:{qsoState} (was {lastQsoState}) callInProg:'{callInProg}'");
                curCmd = emsg.GenMsg;
                DebugOutputStatus();
            }
        }
        private void StartProcessDecodeTimer()
        {
            DateTime dtNow = DateTime.UtcNow;
            int diffMsec = ((dtNow.Second * 1000) + dtNow.Millisecond) % (int)trPeriod;
            int cycleTimerAdj = CalcTimerAdj();
            processDecodeTimer.Interval = (2 * (int)trPeriod) - diffMsec - cycleTimerAdj;
            processDecodeTimer.Start();
            DebugOutput($"{Time()} processDecodeTimer start: interval:{processDecodeTimer.Interval} msec");
        }

        private bool CheckMyCall(StatusMessage smsg)
        {
            if (smsg.DeCall == null || smsg.DeGrid == null || smsg.DeGrid.Length < 4)
            {
                suspendComm = true;
                MessageBox.Show($"Call sign and Grid are not entered in WSJT-X.\n\nEnter these in WSJT-X by selecting 'File | Settings' in the 'General' tab.\n\n(Grid must be at least 4 characters)\n\n{pgmName} will try again when you close this dialog.", pgmName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                ResetNego();
                suspendComm = false;
                return false;
            }

            if (myCall == null)
            {
                myCall = smsg.DeCall;
                myGrid = smsg.DeGrid;
                DebugOutput($"{Time()} CheckMyCall myCall:{myCall} myGrid:{myGrid}");
            }

            UpdateDebug();
            return true;
        }
        private void CheckNextXmit()
        {
            //******************
            //Timeout processing
            //******************
            DebugOutput($"{Time()} CheckNextXmit: txTimeout:{txTimeout} autoCalling:{autoCalling} callQueue.Count:{callQueue.Count} qsoState:{qsoState}");
            //check for time to initiate next xmit from queued calls
            if (txTimeout || (autoCalling && callQueue.Count > 0 && (qsoState == WsjtxMessage.QsoStates.CALLING || callInProg == null)))        //important to sync qso logged to end of xmit, and manually-added call(s) to status msgs
            {
                replyCmd = null;        //last reply cmd sent is no longer in effect
                replyDecode = null;
                SetCallInProg(null);    //not calling anyone (set this as late as possible to pick up possible reply to last Tx)
                DebugOutput($"{Time()} CheckNextXmit(1) start: txTimeout:{txTimeout}");
                DebugOutputStatus();

                //process the next call in the queue, if any present and correct time period
                bool timePeriodOk = true;
                DecodeMessage dmsg = new DecodeMessage();
                if (replyAndQuit)               //requires correct time period
                {
                    string pCall = PeekNextCall(out dmsg);
                    if (dmsg != null)
                    {
                        bool evenCall = IsEvenPeriod(dmsg.SinceMidnight.Seconds * 1000);
                        DateTime dtNow = DateTime.UtcNow;
                        bool evenPer = IsEvenPeriod(((dtNow.Second * 1000) + dtNow.Millisecond + 1500) % 60000);
                        timePeriodOk = evenCall != evenPer;
                        DebugOutput($"{spacer}Peek in queue, got '{pCall}' evenCall:{evenCall} evenPer:{evenPer} timePeriodOk:{timePeriodOk}");
                    }
                }
                if (callQueue.Count > 0 && timePeriodOk)            //have queued call signs from correct time period
                {
                    string nCall = GetNextCall(out dmsg);
                    DebugOutput($"{spacer}Have entries in queue, got '{nCall}'");

                    if (WsjtxMessage.IsCQ(dmsg.Message))                  //save the grid for logging
                    {
                        List<DecodeMessage> vlist;  //create new List for nCall if nothing entered yet into the Dictionary
                        if (!allCallDict.TryGetValue(nCall, out vlist)) allCallDict.Add(nCall, vlist = new List<DecodeMessage>());
                        if (CqMsg(nCall) == null) vlist.Add(dmsg);        //don't duplicate CQ msgs; will be duplicates of rpt message types
                    }

                    //set call options
                    emsg.NewTxMsgIdx = 10;
                    emsg.GenMsg = $"";          //no effect
                    emsg.SkipGrid = ctrl.skipGridCheckBox.Checked;
                    emsg.UseRR73 = ctrl.useRR73CheckBox.Checked;
                    emsg.CmdCheck = "";         //ignored
                    emsg.Offset = AudioOffsetFromMsg(dmsg);
                    ba = emsg.GetBytes();
                    udpClient2.Send(ba, ba.Length);
                    DebugOutput($"{Time()} >>>>>Sent 'Opt Req' cmd:10\n{emsg}");
                    if (settingChanged)
                    {
                        ctrl.WsjtxSettingConfirmed();
                        settingChanged = false;
                    }

                    //set WSJT-X call enable with Reply message
                    var rmsg = new ReplyMessage();
                    rmsg.SchemaVersion = WsjtxMessage.NegotiatedSchemaVersion;
                    rmsg.Id = WsjtxMessage.UniqueId;
                    rmsg.SinceMidnight = dmsg.SinceMidnight;
                    rmsg.Snr = dmsg.Snr;
                    rmsg.DeltaTime = dmsg.DeltaTime;
                    rmsg.DeltaFrequency = dmsg.DeltaFrequency;
                    rmsg.Mode = dmsg.Mode;
                    rmsg.Message = dmsg.Message;
                    rmsg.UseStdReply = dmsg.UseStdReply;
                    ba = rmsg.GetBytes();
                    udpClient2.Send(ba, ba.Length);
                    replyCmd = dmsg.Message;            //save the last reply cmd to determine which call is in progress
                    replyDecode = dmsg;                 //save the decode the reply cmd derived from
                    curCmd = dmsg.Message;
                    SetCallInProg(nCall);
                    DebugOutput($"{Time()} >>>>>Sent 'Reply To Msg' cmd:\n{rmsg} lastTxMsg:'{lastTxMsg}'\nreplyCmd:'{replyCmd}'");
                    //ctrl.ShowMsg($"Replying to {nCall}...", false);

                    if (replyAndQuit) EnableTx();
                }
                else            //no queued call signs, start (or if replyAndQuit: prepare for) CQing
                {
                    if (replyAndQuit)
                    {
                        DisableTx(true);
                    }
                    else
                    {
                        //set/show frequency offset for period after decodes started
                        emsg.NewTxMsgIdx = 10;
                        emsg.GenMsg = $"";          //no effect
                        emsg.SkipGrid = ctrl.skipGridCheckBox.Checked;
                        emsg.UseRR73 = ctrl.useRR73CheckBox.Checked;
                        emsg.CmdCheck = "";         //ignored
                        emsg.Offset = AudioOffsetFromTxPeriod();
                        ba = emsg.GetBytes();
                        udpClient2.Send(ba, ba.Length);
                        DebugOutput($"{Time()} >>>>>Sent 'Opt Req' cmd:10\n{emsg}");
                        if (settingChanged)
                        {
                            ctrl.WsjtxSettingConfirmed();
                            settingChanged = false;
                        }

                        DebugOutput($"{spacer}No entries in queue, start CQing");
                        emsg.NewTxMsgIdx = 6;
                        emsg.GenMsg = $"CQ{NextDirCq()} {myCall} {myGrid}";
                        emsg.SkipGrid = ctrl.skipGridCheckBox.Checked;
                        emsg.UseRR73 = ctrl.useRR73CheckBox.Checked;
                        emsg.CmdCheck = "";         //ignored
                        ba = emsg.GetBytes();           //set up for CQ, auto, call 1st
                        udpClient2.Send(ba, ba.Length);
                        DebugOutput($"{Time()} >>>>>Sent 'Setup CQ' cmd:6\n{emsg}");
                        qsoState = WsjtxMessage.QsoStates.CALLING;      //in case enqueueing call manually right now
                        replyCmd = null;        //invalidate last reply cmd since not replying
                        replyDecode = null;
                        curCmd = emsg.GenMsg;
                        SetCallInProg(null);
                        DebugOutput($"{spacer}qsoState:{qsoState} (was {lastQsoState} replyCmd:'{replyCmd}')");
                        newDirCq = false;           //if set, was processed here
                    }
                }
                restartQueue = false;           //get ready for next decode phase
                txTimeout = false;              //ready for next timeout
                autoCalling = true;
                DebugOutputStatus();
                DebugOutput($"{Time()} CheckNextXmit end: restartQueue:{restartQueue} txTimeout:{txTimeout} autoCalling:{autoCalling}");
                UpdateDebug();      //unconditional
                return;             //don't process newDirCq
            }

            //************************************
            //Directed CQ / new setting processing
            //************************************
            if (qsoState == WsjtxMessage.QsoStates.CALLING && newDirCq)
            {
                //set/show frequency offset for period after decodes started
                emsg.NewTxMsgIdx = 10;
                emsg.GenMsg = $"";          //no effect
                emsg.SkipGrid = ctrl.skipGridCheckBox.Checked;
                emsg.UseRR73 = ctrl.useRR73CheckBox.Checked;
                emsg.CmdCheck = "";         //ignored
                emsg.Offset = AudioOffsetFromTxPeriod();
                ba = emsg.GetBytes();
                udpClient2.Send(ba, ba.Length);
                DebugOutput($"{Time()} >>>>>Sent 'Opt Req' cmd:10\n{emsg}");
                if (settingChanged)
                {
                    ctrl.WsjtxSettingConfirmed();
                    settingChanged = false;
                }

                DebugOutput($"{Time()} CheckNextXmit(2) start");
                emsg.NewTxMsgIdx = 6;
                emsg.GenMsg = $"CQ{NextDirCq()} {myCall} {myGrid}";
                emsg.SkipGrid = ctrl.skipGridCheckBox.Checked;
                emsg.UseRR73 = ctrl.useRR73CheckBox.Checked;
                emsg.CmdCheck = "";         //ignored
                ba = emsg.GetBytes();           //set up for CQ, auto, call 1st
                udpClient2.Send(ba, ba.Length);
                DebugOutput($"{Time()} >>>>>Sent 'Setup CQ' cmd:6\n{emsg}");
                qsoState = WsjtxMessage.QsoStates.CALLING;      //in case enqueueing call manually right now
                replyCmd = null;        //invalidate last reply cmd since not replying
                replyDecode = null;
                curCmd = emsg.GenMsg;
                newDirCq = false;
                SetCallInProg(null);
                DebugOutputStatus();
                DebugOutput($"{Time()} CheckNextXmit(2) end");
                UpdateDebug();      //unconditional
                return;
            }
        }

        private void ProcessDecodes()
        {
            if (ctrl.stopCheckBox.Checked && (DateTime.Now >= txStopDateTime))          //local time
            {
                if (!paused)
                {
                    paused = true;
                    DisableTx(true);
                    UpdateModeSelection();
                    ShowStatus();
                    UpdateDebug();
                    ctrl.ShowMsg("Transmit paused", true);
                }
                ctrl.stopCheckBox.Checked = false;
                DebugOutput($"\n{Time()} Stop tx time {txStopDateTime}, paused:{paused}");
            }

            if (paused && TrimCallQueue() && debug)
            {
                DebugOutput(AllCallDictString());
                DebugOutput(ReportListString());
            }

            DebugOutput($"{Time()} ProcessDecodes: restartQueue:{restartQueue} txTimeout:{txTimeout} txEnabled:{txEnabled}\n{spacer}replyAndQuit:{replyAndQuit} paused:{paused} txEnabled:{txEnabled}");
            if (restartQueue)           //queue went from empty to having entries, during decode(s) phase: restart queue processing
            {
                txTimeout = true;       //important to only set this now, not during decode phase, since decodes can happen after TX starts
                SetCallInProg(null);    //not calling anyone (set this as late as possible to pick up possible reply to last Tx)
                DebugOutput($"{spacer}qsoState:{qsoState} txTimeout:{txTimeout} callInProg:'{callInProg}'");
                UpdateDebug();
            }
            //check for Tx started manually during Rx
            if (!paused && (txEnabled || replyAndQuit))
            {
                CheckNextXmit();
            }
            DebugOutput($"{Time()} ProcessDecodes done\n");
        }

        //check for time to log (best done at Tx start to avoid any logging/dequeueing timing problem if done at Tx end)
        private void ProcessTxStart()
        {
            string toCall = WsjtxMessage.ToCall(txMsg);
            string lastToCall = WsjtxMessage.ToCall(lastTxMsg);
            DebugOutput($"\n{Time()} Tx start: toCall:'{toCall}' lastToCall:'{lastToCall}' processDecodeTimer interval:{processDecodeTimer.Interval} msec");

            if (toCall == "CQ")
            {
                SetCallInProg(null);
            }
            else
            {
                SetCallInProg(toCall);
            }
            DebugOutputStatus();
            if (debug)
            {
                DebugOutput(AllCallDictString());
                DebugOutput(ReportListString());
                DebugOutput(LogListString());
                DebugOutput(PotaLogDictString());
                DebugOutput($"{spacer}Is73orRR73:{WsjtxMessage.Is73orRR73(txMsg)} logEarlyCheckBox:{ctrl.logEarlyCheckBox.Checked} IsRogers:{WsjtxMessage.IsRogers(txMsg)} RecdReport:{RecdReport(toCall)} RecdRogerReport:{RecdRogerReport(toCall)}\n{spacer}reportList.Contains:{reportList.Contains(toCall)} logList.Contains:{logList.Contains(toCall)}");
            }

            if (!logList.Contains(toCall))          //toCall not logged yet this mode/band for this session
            {
                //check for time to log early
                //  option enabled                   sending RRR now                 prev. recd Report   or prev. recd RogerReport  and prev. sent any report             
                if (ctrl.logEarlyCheckBox.Checked && WsjtxMessage.IsRogers(txMsg) && (RecdReport(toCall) || RecdRogerReport(toCall)) && reportList.Contains(toCall))
                {
                    DebugOutput($"{spacer}early logging: toCall:'{toCall}'");
                    LogQso(toCall);
                }

                //check for QSO completing, normal logging
                // sending 73 or RR73 now             prev. recd Report  or prev. recd RogerReport       prev. sent any report
                if (WsjtxMessage.Is73orRR73(txMsg) && (RecdReport(toCall) || RecdRogerReport(toCall)) && reportList.Contains(toCall))
                {
                    DebugOutput($"{spacer}normal logging: toCall:'{toCall}'");
                    LogQso(toCall);
                }
            }

            DebugOutput($"{Time()} Tx start done: txMsg:'{txMsg}' lastTxMsg:'{lastTxMsg}' toCall:'{toCall}' lastToCall:'{lastToCall}'\n");
            UpdateDebug();      //unconditional
        }

        //check for QSO end or timeout (and possibly logging (if txMsg changed between TX start and Tx end)
        private void ProcessTxEnd()
        {
            lastDxCall = dxCall;            //save dxCall from start of Rx phase
            string toCall = WsjtxMessage.ToCall(txMsg);
            string lastToCall = WsjtxMessage.ToCall(lastTxMsg);
            string deCall = WsjtxMessage.DeCall(replyCmd);
            string cmdToCall = WsjtxMessage.ToCall(curCmd);

            DebugOutput($"\n{Time()} Tx end: toCall:'{toCall}' lastToCall:'{lastToCall}' deCall:'{deCall}' cmdToCall:'{cmdToCall}'");
            DebugOutputStatus();
            //could have clicked on "CQ" button in WSJT-X
            if (toCall == "CQ")
            {
                autoCalling = true;
                SetCallInProg(null);
                DebugOutput($"{spacer}Possible CQ button, callInProg:'{callInProg}' autoCalling:{autoCalling}");

                //check for CQ button manually selected, one CQ is allowed if replyAndQuit mode
                if (replyAndQuit)
                {
                    txTimeout = true;
                    DebugOutput($"{spacer}txTimeout:{txTimeout} replyAndQuit:{replyAndQuit}");
                }
            }
            else
            {
                SetCallInProg(toCall);
            }

            //save all call signs a report msg was sent to
            if ((WsjtxMessage.IsReport(txMsg) || WsjtxMessage.IsRogerReport(txMsg)) && !reportList.Contains(toCall)) reportList.Add(toCall);

            if (debug)
            {
                DebugOutput($"{spacer}logEarlyCheckBox:{ctrl.logEarlyCheckBox.Checked} IsRogers:{WsjtxMessage.IsRogers(txMsg)} RecdReport:{RecdReport(toCall)} RecdRogerReport:{RecdRogerReport(toCall)}\n{spacer}reportList.Contains:{reportList.Contains(toCall)} logList.Contains:{logList.Contains(toCall)}");
            }

            if (!logList.Contains(toCall))          //toCall not logged yet this mode/band for this session
            {
                //check for time to log early; NOTE: doing this at Tx end because WSJT-X may have changed Tx msgs (between Tx start and Tx end) due to late decode for the current call
                //  option enabled                   just sent RRR                and prev. recd Report  or prev. recd RogerReport   and prev. sent any report
                if (ctrl.logEarlyCheckBox.Checked && WsjtxMessage.IsRogers(txMsg) && (RecdReport(toCall) || RecdRogerReport(toCall)) && reportList.Contains(toCall))
                {
                    DebugOutput($"{spacer}early logging: toCall:'{toCall}'");
                    LogQso(toCall);
                }
                //check for QSO completed, trigger next call in the queue
                if (WsjtxMessage.Is73orRR73(txMsg))
                {
                    txTimeout = true;      //timeout to Tx the next call in the queue
                    xmitCycleCount = 0;
                    autoCalling = true;
                    tCall = toCall;
                    DebugOutput($"{Time()} Reset(2): (is 73 or RR73) xmitCycleCount:{xmitCycleCount} txTimeout:{txTimeout}\n           autoCalling:{autoCalling} callInProg:'{callInProg}' tCall:'{tCall}'");

                    //NOTE: doing this at Tx end because WSJT-X may have changed Tx msgs (between Tx start and Tx end) due to late decode for the current call
                    // prev. recd Report    or prev. recd RogerReport   and prev. sent any report
                    if ((RecdReport(toCall) || RecdRogerReport(toCall)) && reportList.Contains(toCall))
                    {
                        DebugOutput($"{spacer}normal logging: toCall:'{toCall}'");
                        LogQso(toCall);
                    }
                }
            }

            //count tx cycles: check for changed Tx call in WSJT-X
            if (!IsSameMessage(lastTxMsg, txMsg))
            {
                if (xmitCycleCount >= 0)
                {
                    //check  for "to" call changed since last xmit end
                    // !restartQueue = didn't just add this call to queue during late decode thath overlapped Tx start
                    if (!restartQueue && toCall != lastToCall && callQueue.Contains(toCall))
                    {
                        RemoveCall(toCall);         //manually switched to Txing a call that was also in the queue
                    }
                    xmitCycleCount = 0;
                    DebugOutput($"{Time()} Reset(1) (different msg) xmitCycleCount:{xmitCycleCount} txMsg:'{txMsg}' lastTxMsg:'{lastTxMsg}'");
                }
                lastTxMsg = txMsg;
            }
            else        //same "to" call as last xmit, count xmit cycles
            {
                if (toCall != "CQ")        //don't count CQ (or non-std) calls
                {
                    xmitCycleCount++;           //count xmits to same call sign at end of xmit cycle
                    DebugOutput($"{Time()} (same msg) xmitCycleCount:{xmitCycleCount} txMsg:'{txMsg}' lastTxMsg:'{lastTxMsg}'");
                    if (xmitCycleCount >= (int)ctrl.timeoutNumUpDown.Value - 1)  //n msgs = n-1 diffs
                    {
                        xmitCycleCount = 0;
                        lastTxMsg = null;
                        txTimeout = true;
                        autoCalling = true;
                        tCall = toCall;        //call to remove from queue, will be null if non-std msg
                        DebugOutput($"{Time()} Reset(3) (timeout) xmitCycleCount:{xmitCycleCount} txTimeout:{txTimeout} tCall:'{tCall}' autoCalling:{autoCalling} callInProg:'{callInProg}'");
                    }
                }
                else
                {
                    //same CQ or non-std call
                    xmitCycleCount = 0;
                    DebugOutput($"{Time()} Reset(4) (no action, CQ or non-std) xmitCycleCount:{xmitCycleCount}");
                }
            }

            if (txTimeout)
            {
                DebugOutput($"{spacer}'{tCall}' timed out or completed");
                RemoveCall(tCall);
            }

            //check for time to process new directed CQ
            if (!replyAndQuit && toCall == "CQ" && (ctrl.directedCheckBox.Checked && ctrl.directedTextBox.Text.Trim().Length > 0))
            {
                xmitCycleCount = 0;
                newDirCq = true;
                DebugOutput($"{Time()} Reset(5) (new directed CQ) xmitCycleCount:{xmitCycleCount} newDirCq:{newDirCq}");
            }

            if (replyAndQuit && WsjtxMessage.Is73orRR73(txMsg) && callQueue.Count == 0)
            {
                DebugOutput($"{Time()} replyAndQuit:True Is73orRR73:True callQueue.Count:0");
                DisableTx(true);
                DebugOutputStatus();
            }

            DebugOutputStatus();
            if (TrimAllCallDict() && debug)
            {
                DebugOutput(AllCallDictString());
                DebugOutput(ReportListString());
            }
            DebugOutput($"{Time()} Tx end done\n");
            ShowTimeout();
            UpdateDebug();      //unconditional
        }

        //log a QSO (early or normal timing in QSO progress)
        private void LogQso(string call)
        {
            List<DecodeMessage> msgList;
            if (!allCallDict.TryGetValue(call, out msgList)) return;          //no previous call(s) from DX station
            DecodeMessage rMsg;
            if ((rMsg = msgList.Find(RogerReport)) == null && (rMsg = msgList.Find(Report)) == null) return;        //the DX station never reported a signal
            if (!reportList.Contains(call)) return;         //never reported SNR to the DX station
            RequestLog(call, rMsg, null);
            RemoveAllCall(call);       //prevents duplicate logging, unless caller starts over again
            RemoveCall(call);
        }

        private bool IsEvenPeriod(int msec)     //milliseconds past start of the current minute
        {
            if (mode == "FT4")
            {
                return (msec >= 0 && msec < 07000) || (msec >= 15000 && msec < 22000) || (msec >= 30000 && msec < 37000) || (msec >= 45000 && msec < 52000);
            }

            if (mode == "FT8")
            {
                return (msec >= 0 && msec < 15000) || (msec >= 30000 && msec < 45000);
            }

            if (mode == "FST4")
            {
                return (msec >= 0 && msec < 30000);
            }

            DebugOutput($"IsEvenPeriod mode:{mode} not supported");
            return false;
        }

        private string NextDirCq()
        {
            string dirCq = "";
            if (ctrl.directedCheckBox.Checked && ctrl.directedTextBox.Text.Trim().Length > 0)
            {
                string[] dirWords = ctrl.directedTextBox.Text.Trim().ToUpper().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                string s = dirWords[rnd.Next() % dirWords.Length];
                if (s != "*" && s.Length <= 4) dirCq = " " + s;          //is directed
                DebugOutput($"{Time()} dirCq:{dirCq}");
            }
            return dirCq;
        }

        private void ResetNego()
        {
            ResetOpMode(true);
            WsjtxMessage.Reinit();                      //NegoState = INITIAL;
            DebugOutput($"\n\n{Time()} opMode:{opMode} NegoState:{WsjtxMessage.NegoState}");
            DebugOutput($"{Time()} Waiting for heartbeat...");
            cmdCheck = RandomCheckString();
            commConfirmed = false;
            ShowStatus();
            UpdateDebug();
        }

        private void ResetOpMode(bool clearLog)
        {
            processDecodeTimer.Stop();         //no decodes now
            DebugOutput($"{Time()} processDecodeTimer stop");
            paused = true;
            DisableTx(false);
            opMode = OpModes.IDLE;
            ShowStatus();
            myCall = null;
            myGrid = null;
            autoCalling = true;
            SetCallInProg(null);
            txTimeout = false;
            replyCmd = null;
            curCmd = null;
            replyDecode = null;
            tCall = null;
            newDirCq = false;
            dxCall = null;
            xmitCycleCount = 0;
            if (clearLog) logList.Clear();        //can re-log on new mode, band, or session
            ShowLogged();
            ClearCalls(true);
            //                                                              local time
            if (ctrl.stopCheckBox.Checked && ctrl.freqCheckBox.Checked && (DateTime.Now >= txStopDateTime.AddMinutes(-2))) ctrl.stopCheckBox.Checked = false;        //could miss stop time during wait for best offset freqs
            UpdateModeVisible();
            UpdateModeSelection();
            UpdateTxStopTimeEnable();
            UpdateDebug();
            UpdateAddCall();
            ShowStatus();
            DebugOutput($"\n\n{Time()} opMode:{opMode} NegoState:{WsjtxMessage.NegoState}");
        }

        private void ClearCalls(bool inclCqCalls)
        {
            callQueue.Clear();
            callDict.Clear();
            if (inclCqCalls)
            {
                cqCallDict.Clear();
                timeoutCallDict.Clear();
            }
            ShowQueue();
            allCallDict.Clear();
            reportList.Clear();
            xmitCycleCount = 0;
            ShowTimeout();
            if (processDecodeTimer.Enabled)
            {
                processDecodeTimer.Stop();
                DebugOutput($"{Time()} processDecodeTimer stop");
            }
        }

        private void ClearAudioOffsets()
        {
            period = Periods.UNK;
            oddOffset = 0;
            evenOffset = 0;
            skipAudioOffsetCalc = true;         //wait for complete set of decodes
        }

        private void UpdateAddCall()
        {
            ctrl.addCallLabel.Visible = (advanced && opMode == OpModes.ACTIVE);
        }

        private bool UpdateCall(string call, DecodeMessage msg)
        {
            if (call != null && callDict.ContainsKey(call))
            {
                //check for call saved as a low-priority CQ but now high-priority call to myCall
                DecodeMessage dmsg;
                if (msg.Priority && callDict.TryGetValue(call, out dmsg) && !dmsg.Priority)
                {
                    DebugOutput($"{Time()} Update priority {call}:{CallQueueString()} {CallDictString()}");
                    RemoveCall(call);
                    return AddCall(call, msg);
                }

                callDict.Remove(call);
                callDict.Add(call, msg);
                ShowQueue();
                DebugOutput($"{Time()} Updated {call}:{CallQueueString()} {CallDictString()}");
                return true;
            }
            DebugOutput($"{Time()} Not updated {call}:{CallQueueString()} {CallDictString()}");
            return false;
        }

        //remove call from queue/dictionary;
        //call not required to be present
        //return false if failure
        private bool RemoveCall(string call)
        {
            DecodeMessage msg;
            if (call != null && callDict.TryGetValue(call, out msg))     //dictionary contains call data for this call sign
            {
                callDict.Remove(call);

                string[] qArray = new string[callQueue.Count];
                callQueue.CopyTo(qArray, 0);
                callQueue.Clear();
                for (int i = 0; i < qArray.Length; i++)
                {
                    if (qArray[i] != call) callQueue.Enqueue(qArray[i]);
                }

                if (callDict.Count != callQueue.Count)
                {
                    Console.Beep();
                    DebugOutput("ERROR: queueDict and callDict out of sync");
                    errorDesc = " queueDict out of sync";
                    UpdateDebug();
                    return false;
                }

                ShowQueue();
                DebugOutput($"{Time()} Removed {call}: {CallQueueString()} {CallDictString()}");
                return true;
            }
            DebugOutput($"{Time()} Not removed, not in callQueue '{call}': {CallQueueString()} {CallDictString()}");
            return false;
        }

        //add call/decode to queue/dict;
        //priority decodes (to myCall) move toward the head of the queue
        //because non-priority calls are added manually to queue (i.e., not rec'd, prospective for QSO)
        //but priority calls are decoded calls to myCall (i.e., rec'd and immediately ready for QSO);
        //return false if already added
        private bool AddCall(string call, DecodeMessage msg)
        {
            DecodeMessage dmsg;
            if (!callDict.TryGetValue(call, out dmsg))     //dictionary does not contain call data for this call sign
            {
                if (msg.Priority)           //may need to insert this priority call ahead of non-priority calls
                {
                    var callArray = callQueue.ToArray();        //make accessible
                    var tmpQueue = new Queue<string>();         //will be the updated queue

                    //go thru calls in reverse time order
                    int i;
                    for (i = 0; i < callArray.Length; i++)
                    {
                        DecodeMessage decode;
                        callDict.TryGetValue(callArray[i], out decode);     //get the decode for an existing call in the queue
                        if (!decode.Priority)               //reached the end of priority calls (if any)
                        {
                            break;
                        }
                        else
                        {
                            tmpQueue.Enqueue(callArray[i]); //add the existing priority call 
                        }
                    }
                    tmpQueue.Enqueue(call);         //add the new priority call (before oldest non-priority call, or at end of all-priority-call queue)

                    //fill in the remaining non-priority callls
                    for (int j = i; j < callArray.Length; j++)
                    {
                        tmpQueue.Enqueue(callArray[j]);
                    }
                    callQueue = tmpQueue;
                }
                else            //is a non-priority call, add to end of all calls
                {
                    callQueue.Enqueue(call);
                }

                callDict.Add(call, msg);
                ShowQueue();
                DebugOutput($"{Time()} Enqueued {call}: {CallQueueString()} {CallDictString()}");
                return true;
            }
            DebugOutput($"{Time()} Not enqueued {call}: {CallQueueString()} {CallDictString()}");
            return false;
        }

        //peek at next call/msg in queue;
        //queue not assume to have any entries;
        //return null if failure
        private string PeekNextCall(out DecodeMessage dmsg)
        {
            dmsg = null;
            if (callQueue.Count == 0)
            {
                DebugOutput($"{Time()} No peek: {CallQueueString()} {CallDictString()}");
                return null;
            }

            string call = callQueue.Peek();

            if (!callDict.TryGetValue(call, out dmsg))
            {
                Console.Beep();
                DebugOutput("ERROR: '{call}' not found");
                errorDesc = "'{call}' not found";
                UpdateDebug();
                return null;
            }

            if (WsjtxMessage.Is73(dmsg.Message)) dmsg.Message = dmsg.Message.Replace("73", "");            //important, otherwise WSJT-X will not respond
            DebugOutput($"{Time()} Peek {call}: msg:'{dmsg.Message}' {CallQueueString()} {CallDictString()}");
            return call;
        }

        //get next call/msg in queue;
        //queue not assume to have any entries;
        //return null if failure
        private string GetNextCall(out DecodeMessage dmsg)
        {
            dmsg = null;
            if (callQueue.Count == 0)
            {
                DebugOutput($"{Time()} Not dequeued: {CallQueueString()} {CallDictString()}");
                return null;
            }

            string call = callQueue.Dequeue();

            if (!callDict.TryGetValue(call, out dmsg))
            {
                Console.Beep();
                DebugOutput("ERROR: '{call}' not found");
                errorDesc = "'{call}' not found";
                UpdateDebug();
                return null;
            }

            if (callDict.ContainsKey(call)) callDict.Remove(call);

            if (callDict.Count != callQueue.Count)
            {
                Console.Beep();
                DebugOutput("ERROR: callDict and queueDict out of sync");
                errorDesc = " callDict out of sync";
                UpdateDebug();
                return null;
            }

            ShowQueue();
            if (WsjtxMessage.Is73(dmsg.Message)) dmsg.Message = dmsg.Message.Replace("73", "");            //important, otherwise WSJT-X will not respond
            DebugOutput($"{Time()} Dequeued {call}: msg:'{dmsg.Message}' {CallQueueString()} {CallDictString()}");
            return call;
        }

        private void WriteToDisk(string txt)
        {
            try
            {
                File.AppendText("log.txt");
            }
            catch (Exception) { }
        }

        private string CallQueueString()
        {
            string delim = "";
            StringBuilder sb = new StringBuilder();
            sb.Append("callQueue [");
            foreach (string call in callQueue)
            {
                sb.Append(delim + call);
                delim = " ";
            }
            sb.Append("]");
            return sb.ToString();
        }

        private string ReportListString()
        {
            string delim = "";
            StringBuilder sb = new StringBuilder();
            sb.Append($"{spacer}reportList [");
            foreach (string call in reportList)
            {
                sb.Append(delim + call);
                delim = " ";
            }
            sb.Append("]");
            return sb.ToString();
        }
        private string LogListString()
        {
            string delim = "";
            StringBuilder sb = new StringBuilder();
            sb.Append($"{spacer}logList [");
            foreach (string call in logList)
            {
                sb.Append(delim + call);
                delim = " ";
            }
            sb.Append("]");
            return sb.ToString();
        }

        private string CallDictString()
        {
            string delim = "";
            StringBuilder sb = new StringBuilder();
            sb.Append("callDict [");
            foreach (var entry in callDict)
            {
                sb.Append(delim + entry.Key);
                delim = " ";
            }
            sb.Append("]");
            return sb.ToString();
        }

        private string AllCallDictString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append($"{spacer}allCallDict");
            if (allCallDict.Count == 0)
            {
                sb.Append(" []");
            }
            else
            {
                sb.Append(":");
            }

            foreach (var entry in allCallDict)
            {
                sb.Append($"\n{spacer}{entry.Key} ");
                string delim = "";
                sb.Append("[");
                foreach (DecodeMessage msg in entry.Value)
                {
                    sb.Append($"{delim}{msg.Message} @{msg.SinceMidnight}");
                    delim = ", ";
                }
                sb.Append("]");
            }

            return sb.ToString();
        }

        private string PotaLogDictString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append($"{spacer}potaLogDict");
            if (potaLogDict.Count == 0)
            {
                sb.Append(" []");
            }
            else
            {
                sb.Append(":");
            }
            foreach (var entry in potaLogDict)
            {
                string delim = "";
                sb.Append($"\n{spacer}{entry.Key} [");
                foreach (var info in entry.Value)
                {
                    sb.Append($"{delim}{info}");
                    delim = "  ";
                }
                sb.Append("]");
            }

            return sb.ToString();
        }

        private string Time()
        {
            var dt = DateTime.UtcNow;
            return dt.ToString("HHmmss.fff");
        }

        public void Closing()
        {
            DebugOutput($"\n\n{DateTime.UtcNow.ToString("yyyy-MM-dd HHmmss")} UTC ###################### Program closing...");

            try
            {
                if (emsg != null && udpClient2 != null)
                {
                    //notify WSJT-X
                    emsg.NewTxMsgIdx = 0;           //de-init WSJT-X
                    emsg.GenMsg = $"";         //ignored
                    emsg.SkipGrid = ctrl.skipGridCheckBox.Checked;
                    emsg.UseRR73 = ctrl.useRR73CheckBox.Checked;
                    emsg.CmdCheck = "";         //ignored
                    ba = emsg.GetBytes();
                    udpClient2.Send(ba, ba.Length);
                    DebugOutput($"{Time()} >>>>>Sent 'De-init Req' cmd:0\n{emsg}");
                    Thread.Sleep(500);
                    udpClient2.Close();
                }
                if (udpClient != null) udpClient.Close();     //causes unresolvable "disposed object" problem at EndReceive
            }
            catch (Exception e)         //udpClient might be disposed already
            {
                e.ToString();
                DebugOutput($"{Time()} Error at Closing, udpClient:{udpClient} udpClient2:{udpClient2}");
            }
            udpClient = null;

            if (potaSw != null)
            {
                potaSw.Flush();
                potaSw.Close();
                potaSw = null;
            }

            SetLogFileState(false);         //close log file
        }

        public void Dispose()
        {
            //udpClient.Dispose();
            //udpClient = null;
            //udpClient2.Dispose();
            //udpClient2 = null;
        }

        [DllImport("winmm.dll", SetLastError = true)]
        static extern bool PlaySound(string pszSound, UIntPtr hmod, uint fdwSound);

        [Flags]
        private enum SoundFlags
        {
            /// <summary>play synchronously (default)</summary>
            SND_SYNC = 0x0000,
            /// <summary>play asynchronously</summary>
            SND_ASYNC = 0x0001,
            /// <summary>silence (!default) if sound not found</summary>
            SND_NODEFAULT = 0x0002,
            /// <summary>pszSound points to a memory file</summary>
            SND_MEMORY = 0x0004,
            /// <summary>loop the sound until next sndPlaySound</summary>
            SND_LOOP = 0x0008,
            /// <summary>don’t stop any currently playing sound</summary>
            SND_NOSTOP = 0x0010,
            /// <summary>Stop Playing Wave</summary>
            SND_PURGE = 0x40,
            /// <summary>don’t wait if the driver is busy</summary>
            SND_NOWAIT = 0x00002000,
            /// <summary>name is a registry alias</summary>
            SND_ALIAS = 0x00010000,
            /// <summary>alias is a predefined id</summary>
            SND_ALIAS_ID = 0x00110000,
            /// <summary>name is file name</summary>
            SND_FILENAME = 0x00020000,
            /// <summary>name is resource name or atom</summary>
            SND_RESOURCE = 0x00040004
        }

        public void Play(string strFileName)
        {
            PlaySound(strFileName, UIntPtr.Zero,
               (uint)(SoundFlags.SND_ASYNC));
        }

        private void ShowQueue()
        {
            if (callQueue.Count == 0)
            {
                ctrl.callText.Font = new Font("Microsoft Sans Serif", 8.25F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
                ctrl.callText.ForeColor = Color.Gray;
                ctrl.callText.Text = "[None]";
                return;
            }

            StringBuilder sb = new StringBuilder();
            int callCount = 0;
            foreach (string call in callQueue)
            {
                DecodeMessage d;
                if (callDict.TryGetValue(call, out d))
                {
                    if (++callCount > maxQueueLines)
                    {
                        sb.Append("...more....");
                        break;
                    }
                    //string oe = debug ? (IsEvenPeriod(d.SinceMidnight.Seconds * 1000) ? "E " : "O ") : "";
                    //string callOe = oe + d.Message;
                    //sb.Append(callOe.Substring(0, Math.Min(callOe.Length, maxQueueWidth)));
                    sb.Append(d.Message.Substring(0, Math.Min(d.Message.Length, maxQueueWidth)));
                    sb.Append("\n");
                }
            }
            ctrl.callText.Font = new Font("Consolas", 10.0F, FontStyle.Bold, GraphicsUnit.Point, ((byte)(0)));
            ctrl.callText.ForeColor = Color.Black;
            ctrl.callText.Text = sb.ToString();
        }

        private void ShowStatus()
        {
            string status = "";
            Color foreColor = Color.Black;
            Color backColor = Color.Yellow;     //caution

            try
            {
                if (WsjtxMessage.NegoState == WsjtxMessage.NegoStates.FAIL || !modeSupported)
                {
                    status = failReason;
                    backColor = Color.Orange;
                    return;
                }

                if (WsjtxMessage.NegoState == WsjtxMessage.NegoStates.INITIAL)
                {
                    status = "Waiting for WSJT-X...";
                    foreColor = Color.White;
                    backColor = Color.Red;
                }
                else
                {
                    switch ((int)opMode)
                    {
                        case (int)OpModes.START:
                            if (ctrl.freqCheckBox.Checked)
                            {
                                status = "Analyzing RX data, no TX until ready";
                            }
                            else
                            {
                                status = "Connecting, wait until ready";
                            }
                            foreColor = Color.White;
                            backColor = Color.Red;
                            return;
                        case (int)OpModes.IDLE:
                            status = "Connecting, wait until ready";
                            foreColor = Color.White;
                            backColor = Color.Red;
                            return;
                        case (int)OpModes.ACTIVE:
                            if (paused && advanced && showTxModes)
                            {
                                status = "To start: Select an operating mode";
                                foreColor = Color.White;
                                backColor = Color.Green;
                            }
                            else
                            {
                                if (replyAndQuit)
                                {
                                    status = "Automatic transmit enabled";
                                }
                                else
                                {
                                    if (txEnabled)
                                    {
                                        if (autoCalling)
                                        {
                                            if (showTxModes)
                                            {
                                                status = "Automatic transmit enabled";
                                            }
                                            else
                                            {
                                                status = "Automatic operation enabled";
                                                foreColor = Color.White;
                                                backColor = Color.Green;
                                            }
                                        }
                                        else
                                        {
                                            status = "Automatic operation paused...";
                                            foreColor = Color.White;
                                            backColor = Color.Green;
                                        }
                                    }
                                    else
                                    {
                                        status = "Select 'Enable Tx' in WSJT-X";
                                        foreColor = Color.White;
                                        backColor = Color.Green;
                                    }
                                }
                            }
                            break;
                    }
                }
            }
            finally
            {
                ctrl.statusText.ForeColor = foreColor;
                ctrl.statusText.BackColor = backColor;
                ctrl.statusText.Text = status;
            }
        }

        private void ShowLogged()
        {
            ctrl.loggedLabel.Text = $"Calls logged ({logList.Count})";

            if (logList.Count == 0)
            {
                ctrl.loggedText.Font = new Font("Microsoft Sans Serif", 8.25F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
                ctrl.loggedText.ForeColor = Color.Gray;
                ctrl.loggedText.Text = "[None]";
                return;
            }

            StringBuilder sb = new StringBuilder();
            var rList = logList.GetRange(Math.Max(logList.Count - maxQueueLines, 0), Math.Min(logList.Count, maxQueueLines));
            rList.Reverse();
            foreach (string call in rList)
            {
                sb.Append(call.Substring(0, Math.Min(call.Length, maxLogWidth)));
                sb.Append("\n");

            }
            if (logList.Count > maxQueueLines)
            {
                sb.Append("..more..");
            }
            ctrl.loggedText.Font = new Font("Consolas", 10.0F, FontStyle.Bold, GraphicsUnit.Point, ((byte)(0)));
            ctrl.loggedText.ForeColor = Color.Black;
            ctrl.loggedText.Text = sb.ToString();
        }

        //process a manually- or automatically-generated request to add a decode to call reply queue
        public void AddSelectedCall(EnqueueDecodeMessage emsg)
        {
            string msg = emsg.Message;
            string deCall = WsjtxMessage.DeCall(msg);
            string toCall = WsjtxMessage.ToCall(msg);
            string directedTo = WsjtxMessage.DirectedTo(msg);
            bool isPota = WsjtxMessage.IsPotaOrSota(msg);
            bool isCq = WsjtxMessage.IsCQ(emsg.Message);                //CQ format check
            bool isContest = WsjtxMessage.IsContest(emsg.Message);
            bool isWantedNonDirected = ctrl.replyCqCheckBox.Checked && (!ctrl.exceptCheckBox.Checked || emsg.IsDx) && directedTo == null;
            bool isWantedDirected = ctrl.alertCheckBox.Checked && isDirectedAlert(directedTo, emsg.IsDx);
            DebugOutput($"{Time()} isCq:{isCq} deCall:'{deCall}' IsDx:{emsg.IsDx} isWantedNonDirected:{isWantedNonDirected} isWantedDirected:{isWantedDirected}\n{spacer}isContest:{isContest} isPota:{isPota} directedTo:'{directedTo}'");

            //auto-generated notification of a CQ from WSJT-X;
            //call sign known to not have been logged yet by WSJT-X this band, *except* if POTA/SOTA
            //IsDx only valid for auto-generated message, not for manually-selected message
            if (emsg.AutoGen)       //automatically-generated queue request
            {
                if (!isCq || isContest || !advanced) return;                //non-std format

                //check for call to be queued
                if (isWantedNonDirected || isWantedDirected)
                {
                    DebugOutput($"{spacer}callInProg:'{callInProg}' callQueue.Count:{callQueue.Count} callQueue.Contains:{callQueue.Contains(deCall)} logList.Contains:{logList.Contains(deCall)}");
                    if (myCall == null || opMode != OpModes.ACTIVE
                        || (!replyAndQuit && IsEvenPeriod(emsg.SinceMidnight.Seconds * 1000) == txFirst)
                        || msg.Contains("...")) return;

                    if (deCall == null || callQueue.Contains(deCall) || deCall == callInProg) return;

                    if (isPota) DebugOutput($"{PotaLogDictString()}");
                    List<string> list;
                    if (isPota && potaLogDict.TryGetValue(deCall, out list))
                    {
                        string band = FreqToBand(dialFrequency / 1e6);
                        string date = DateTime.Now.ToShortDateString();     //local date/time
                        string potaInfo = $"{date},{band},{mode}";
                        DebugOutput($"{spacer}potaInfo:{potaInfo}");
                        if (list.Contains(potaInfo)) return;         //already logged today (local date/time) on this mode and band
                    }

                    if (callQueue.Count < maxAutoGenEnqueue || isWantedDirected)
                    {
                        int prevCqs = 0;
                        int maxCqs = isPota ? maxPrevPotaCqs : maxPrevCqs;
                        if (!cqCallDict.TryGetValue(deCall, out prevCqs) || prevCqs < maxCqs)
                        {
                            DebugOutput($"{spacer}prevCqs:{prevCqs}");
                            if (isWantedDirected) emsg.Priority = true;   //since known to be wanted, give same priority as an actual reply
                            AddCall(deCall, emsg);              //add to call queue

                            if (!paused && !txEnabled && replyAndQuit && callQueue.Count == 1)
                            {
                                restartQueue = true;
                                DebugOutput($"{spacer}restartQueue:{restartQueue}");
                            }

                            if (prevCqs > 0)                   //track how many times Controlller replied to CQ from this call sign
                            {
                                cqCallDict.Remove(deCall);
                            }
                            cqCallDict.Add(deCall, prevCqs + 1);
                            DebugOutput($"{spacer}CQ added, prevCqs:{prevCqs}");
                            if (toCall != myCall) Play("blip.wav");     //not already played "my call" sound
                            UpdateDebug();
                        }
                        else
                        {
                            DebugOutput($"{spacer}CQ not added, prevCqs:{prevCqs}");
                        }
                    }
                }
                return;
            }

            //manually-selected queue request, can be any type of msg
            //can enable Tx
            DebugOutput($"{spacer}modifier:{emsg.Modifier} AutoGen:{emsg.AutoGen}\n{emsg}");
            if (myCall == null || opMode != OpModes.ACTIVE)
            {
                ctrl.ShowMsg("Not ready to add calls yet", true);
                return;
            }

            if ((!showTxModes || !replyAndQuit) && IsEvenPeriod(emsg.SinceMidnight.Seconds * 1000) == txFirst)
            {
                string s = txFirst ? "odd" : "even/1st";
                ctrl.ShowMsg($"Select calls in '{s}' sequence", true);
                return;
            }
            if (msg.Contains("..."))
            {
                ctrl.ShowMsg("Can't add call from hashed msg", true);
                return;
            }

            if (isContest)
            {
                ctrl.ShowMsg("Can't add contest call", true);
                return;
            }

            if (deCall == null)
            {
                ctrl.ShowMsg("No 'from' call in message", true);
                return;
            }

            if (emsg.Modifier)      //ctrl + alt key
            {
                if (msg.Contains("CQ"))
                {
                    ctrl.ShowMsg("Message only contains CQ", true);
                    return;
                }
                if (toCall == null)
                {
                    ctrl.ShowMsg("No 'to' call in message", true);
                    return;
                }
                if (toCall == myCall)
                {
                    ctrl.ShowMsg($"{toCall} is to this station", true);
                    return;
                }
                if (paused)
                {
                    ctrl.ShowMsg($"Select 'Operating mode' first", true);
                    return;
                }

                DebugOutput($"{Time()} ctrl/alt/dbl-click on {toCall}");
                //build a CQ message to reply to
                DecodeMessage nmsg;
                nmsg = new DecodeMessage();
                nmsg.Mode = rawMode;
                nmsg.SchemaVersion = WsjtxMessage.NegotiatedSchemaVersion;
                nmsg.New = true;
                nmsg.OffAir = false;
                nmsg.UseStdReply = true;          //override skipGrid since no SNR available
                nmsg.Id = WsjtxMessage.UniqueId;
                nmsg.Snr = -10;             //not used
                nmsg.DeltaTime = 0.0;       //not used
                nmsg.DeltaFrequency = 1500; //not used
                nmsg.Message = $"CQ {toCall}";
                nmsg.SinceMidnight = latestDecodeTime + new TimeSpan(0, 0, 0, 0, (int)trPeriod);
                DebugOutput($"{nmsg}");
                ClearCalls(false);                       //nothing left to do this tx period
                AddCall(toCall, nmsg);              //add to call queue

                txTimeout = true;                   //switch to other tx period
                autoCalling = true;
                DebugOutput($"{spacer}txTimeout:{txTimeout} autoCalling:{autoCalling} callInProg:'{callInProg}'");
                CheckNextXmit();
                lastTxFirst = !txFirst;     //inhibit CheckNextXmit() from txFirst change detection
                Play("blip.wav");
            }
            else   //only alt key
            {
                if (callQueue.Contains(deCall))
                {
                    ctrl.ShowMsg($"{deCall} already on call list", true);
                    return;
                }
                if (txEnabled && deCall == callInProg)
                {
                    ctrl.ShowMsg($"{deCall} is already in progress", true);
                    return;
                }

                DebugOutput($"{Time()} alt/dbl-click on {toCall}");
                //message to reply to
                if (isWantedDirected || (toCall == myCall)) emsg.Priority = true;
                AddCall(deCall, emsg);              //add to call queue

                if (!paused && !txEnabled && replyAndQuit && callQueue.Count == 1)
                {
                    restartQueue = true;
                    DebugOutput($"{spacer}restartQueue:{restartQueue}");
                }

                Play("blip.wav");
            }
            UpdateDebug();
        }

        public void ShowTimeout()
        {
            if (xmitCycleCount == 0)
            {
                ctrl.timeoutLabel.Visible = false;
            }
            else
            {
                ctrl.timeoutLabel.Visible = true;
                switch (Math.Max(1, ctrl.timeoutNumUpDown.Value - xmitCycleCount - 1))
                {
                    case 1:
                        ctrl.timeoutLabel.ForeColor = Color.Red;
                        break;

                    case 2:
                        ctrl.timeoutLabel.ForeColor = Color.Orange;
                        break;

                    default:
                        ctrl.timeoutLabel.ForeColor = Color.Green;
                        break;
                }
                ctrl.timeoutLabel.Text = $"(now: {xmitCycleCount + 1})";
            }
        }

        public void UpdateDebug()
        {
            if (!debug) return;
            string s;
            bool chg = false;

            try
            {
                if (autoCalling != lastAutoCalling)
                {
                    ctrl.label5.ForeColor = Color.Red;
                    chg = true;
                }
                s = (autoCalling ? "Auto" : "Manual");
                ctrl.label5.Text = $"{s}";
                lastAutoCalling = autoCalling;

                ctrl.label6.Text = $"UDP: {msg.GetType().Name.Substring(0, 6)}";
                ctrl.label7.Text = $"txEn: {txEnabled.ToString().Substring(0, 1)}";
                ctrl.label23.Text = $"dblClk: {dblClk.ToString().Substring(0, 1)}";

                if (replyCmd != lastReplyCmd)
                {
                    ctrl.label8.ForeColor = Color.Red;
                    ctrl.label21.ForeColor = Color.Red;
                    chg = true;
                }
                ctrl.label8.Text = $"cmd from: {WsjtxMessage.DeCall(replyCmd)}";
                lastReplyCmd = replyCmd;

                ctrl.label9.Text = $"opMode: {opMode}-{WsjtxMessage.NegoState}";

                string txTo = (txMsg == null ? "" : WsjtxMessage.ToCall(txMsg));
                s = (txTo == "CQ" ? null : txTo);
                ctrl.label12.Text = $"tx to: {s}";

                if (callInProg != lastCallInProg)
                {
                    ctrl.label13.ForeColor = Color.Red;
                    chg = true;
                }
                ctrl.label13.Text = $"in-prog: {callInProg}";
                lastCallInProg = callInProg;

                if (qsoState != lastQsoStateDebug)
                {
                    ctrl.label14.ForeColor = Color.Red;
                    chg = true;
                }

                ctrl.label14.Text = $"qso:{qsoState}";
                lastQsoStateDebug = qsoState;

                if (evenOffset != lastEvenOffset)
                {
                    ctrl.label15.ForeColor = Color.Red;
                    chg = true;
                }
                ctrl.label15.Text = $"evn:{evenOffset}";
                lastEvenOffset = evenOffset;

                if (oddOffset != lastOddOffset)
                {
                    ctrl.label16.ForeColor = Color.Red;
                    chg = true;
                }
                ctrl.label16.Text = $"odd:{oddOffset}";
                lastOddOffset = oddOffset;

                if (txTimeout != lastTxTimeout)
                {
                    ctrl.label10.ForeColor = Color.Red;
                    chg = true;
                }
                ctrl.label10.Text = $"t/o: {txTimeout.ToString().Substring(0, 1)}";
                lastTxTimeout = txTimeout;

                ctrl.label11.Text = $"txFirst: {txFirst.ToString().Substring(0, 1)}";
                ctrl.label24.Text = $"rstQ: {restartQueue.ToString().Substring(0, 1)}";
                ctrl.label25.Text = $"tx: {transmitting.ToString().Substring(0, 1)}";

                if (txMsg != lastTxMsgDebug)
                {
                    ctrl.label19.ForeColor = Color.Red;
                    chg = true;
                }
                ctrl.label19.Text = $"tx:  {txMsg}";
                lastTxMsgDebug = txMsg;

                if (lastTxMsg != lastLastTxMsgDebug)
                {
                    ctrl.label18.ForeColor = Color.Red;
                    chg = true;
                }
                ctrl.label18.Text = $"last: {lastTxMsg}";
                lastLastTxMsgDebug = lastTxMsg;

                if (lastDxCallDebug != dxCall)
                {
                    ctrl.label4.ForeColor = Color.Red;
                    chg = true;
                }
                ctrl.label4.Text = $"dxCall: {dxCall}";
                lastDxCallDebug = dxCall;

                int i = 0;
                foreach (var entry in allCallDict)
                {
                    i = i + entry.Value.Count;
                }
                ctrl.label21.Text = $"replyCmd: {replyCmd}";

                //if (curCmd != lastCurCmd) ctrl.label17.ForeColor = Color.Red;
                ctrl.label17.Text = $"curCmd: {curCmd}";
                //lastCurCmd = curCmd;

                ctrl.label20.Text = $"t/o cnt: {xmitCycleCount}";
                ctrl.label22.Text = $"tCall: {tCall}";

                if (chg)
                {
                    ctrl.timer7.Interval = 2000;
                    ctrl.timer7.Start();
                }
            }
            catch (Exception err)
            {
                DebugOutput($"ERROR: UpdateDebug: err:{err}");
            }
        }

        public void ConnectionDialog()
        {
            ctrl.timer4.Stop();
            if (WsjtxMessage.NegoState == WsjtxMessage.NegoStates.INITIAL)
            {
                suspendComm = true;         //in case udpClient msgs start 
                string s = multicast ? "\nTry a different 'Outgoing interface'." : "";
                ModelessDialog($"No response from WSJT-X.\n\nIs WSJT-X running? If so:\nIs the WSJT-X 'UDP Server' set to {ipAddress}?\nIs the WSJT-X 'UDP Server port number' set to {port}?\nIs the WSJT-X 'Accept UDP requests' selection enabled?{s}\n\nSelect 'File | Settings', in the 'Reporting' tab to view these settings.\n\n{pgmName} will continue waiting for WSJT-X to respond when you close this dialog.");
                suspendComm = false;
            }
        }

        public void CmdCheckDialog()
        {
            ctrl.timer5.Stop();
            if (commConfirmed) return;

            suspendComm = true;
            MessageBox.Show($"Unable to make a two-way connection with WSJT-X.\n\nIs the WSJT-X 'Accept UDP requests' selection enabled?\n\nSelect 'File | Settings', in the 'Reporting' tab to view this setting.\n\n{pgmName} will try again when you close this dialog.", pgmName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            ResetOpMode(true);

            emsg.NewTxMsgIdx = 7;
            emsg.GenMsg = $"";          //no effect
            emsg.ReplyReqd = true;
            emsg.EnableTimeout = !debug;
            cmdCheck = RandomCheckString();
            emsg.CmdCheck = cmdCheck;
            ba = emsg.GetBytes();
            udpClient2.Send(ba, ba.Length);
            DebugOutput($"{Time()} >>>>>Sent 'Ack Req' cmd:7 cmdCheck:{cmdCheck}\n{emsg}");

            ctrl.timer5.Interval = 10000;           //set up cmd check timeout
            ctrl.timer5.Start();
            DebugOutput($"{Time()} Check cmd timer restarted");

            suspendComm = false;
        }

        private void ModelessDialog(string text)
        {
            new Thread(new ThreadStart(delegate
             {
                 MessageBox.Show
                 (
                   text,
                   pgmName,
                   MessageBoxButtons.OK,
                   MessageBoxIcon.Warning
                 );
             })).Start();
        }

        private string AcceptableVersionsString()
        {
            string delim = "";
            StringBuilder sb = new StringBuilder();

            foreach (string s in acceptableWsjtxVersions)
            {
                sb.Append(delim);
                sb.Append(s);
                delim = "\n";
            }

            return sb.ToString();
        }

        private string RandomCheckString()
        {
            string s = rnd.Next().ToString();
            if (s.Length > 8) s = s.Substring(0, 8);
            return s;
        }

        private void DebugOutput(string s)
        {
            if (diagLog)
            {
                try
                {
                    if (logSw != null) logSw.WriteLine(s);
                }
                catch (Exception e)
                {
#if DEBUG
                    Console.WriteLine(e);
#endif
                }
            }

#if DEBUG
            if (debug)
            {
                Console.WriteLine(s);
            }
#endif
        }

        //during decoding, check for late signoff (73 or RR73) 
        //from a call sign that isn't (or won't be) the call in progress;
        //if reports have bee exchanged, log the QSO;
        //logging is done directly via log file, not via WSJT-X
        private void CheckLateLog(string call, DecodeMessage msg)
        {
            DebugOutput($"{Time()} CheckLateLog: call:'{call}' callInProg:'{callInProg}' txTimeout:{txTimeout} msg:{msg.Message} Is73orRR73:{WsjtxMessage.Is73orRR73(msg.Message)}");
            if (call == null || !WsjtxMessage.Is73orRR73(msg.Message))
            {
                DebugOutput($"{spacer}no late log: call is in progress (or just timed out), or is not RRR73 or 73");
                return;
            }

            if (logList.Contains(call))         //call already logged for thos mode or band for this session
            {
                DebugOutput($"{spacer}no late log: call is already logged");
                return;
            }

            List<DecodeMessage> msgList;
            if (!allCallDict.TryGetValue(call, out msgList))
            {
                DebugOutput($"{spacer}no late log: no previous call(s) rec'd");
                return;          //no previous call(s) from DX station
            }

            DecodeMessage rMsg;
            if ((rMsg = msgList.Find(RogerReport)) == null && (rMsg = msgList.Find(Report)) == null)
            {
                DebugOutput($"{spacer}no late log: no previous report(s) rec'd");
                return;        //the DX station never reported a signal
            }

            if (!reportList.Contains(call))
            {
                DebugOutput($"{spacer}no late log: no previous report(s) sent");
                return;         //never reported SNR to the DX station
            }

            RequestLog(call, rMsg, msg);              //process a "late" QSO completion
            RemoveAllCall(call);       //prevents duplicate logging, unless caller starts over again
            RemoveCall(call);
        }

        private bool Rogers(DecodeMessage msg)
        {
            return WsjtxMessage.IsRogers(msg.Message);
        }
        private bool RogerReport(DecodeMessage msg)
        {
            return WsjtxMessage.IsRogerReport(msg.Message);
        }
        private bool Report(DecodeMessage msg)
        {
            return WsjtxMessage.IsReport(msg.Message);
        }

        private bool Reply(DecodeMessage msg)
        {
            return WsjtxMessage.IsReply(msg.Message);
        }

        private bool Signoff(DecodeMessage msg)
        {
            return WsjtxMessage.Is73orRR73(msg.Message);
        }


        private bool CQ(DecodeMessage msg)
        {
            return WsjtxMessage.IsCQ(msg.Message);
        }

        //request WSJT-X log a QSO to the WSJT-X .ADI log file and re-broadcast to UDP listeners;
        //logging done only via WSJT-X because WSJT-X keeps track of 'logged-before' status, 
        //which is important to processing CQ notification msgs received from WSJT-X
        //recdMsg null if logging because of a sent msg
        private void RequestLog(string call, DecodeMessage reptMsg, DecodeMessage recdMsg)
        {
            string qsoDateOff, qsoTimeOff;

            //<call:4>W1AW  <gridsquare:4>EM77 <mode:3>FT8 <rst_sent:3>-10 <rst_rcvd:3>+01 <qso_date:8>20201226 
            //<time_on:6>042215 <qso_date_off:8>20201226 <time_off:6>042300 <band:3>40m <freq:8>7.076439 
            //<station_callsign:4>WM8Q <my_gridsquare:6>DN61OK <eor>

            string rstSent = $"{reptMsg.Snr:+#;-#;+00}";
            string rstRecd = WsjtxMessage.RstRecd(reptMsg.Message);
            string qsoDateOn = reptMsg.RxDate.ToString("yyyyMMdd");
            string qsoTimeOn = reptMsg.SinceMidnight.ToString("hhmmss");      //one of the report decodes
            DecodeMessage cqMsg = CqMsg(call);
            bool isPota = cqMsg != null && WsjtxMessage.IsPotaOrSota(cqMsg.Message);
            var dtNow = DateTime.UtcNow;

            if (recdMsg == null)            //logging because of xmitted RRR, 73, or RR73 (not because of a red'c msg)
            {
                qsoDateOff = dtNow.ToString("yyyyMMdd");
                qsoTimeOff = dtNow.TimeOfDay.ToString("hhmmss");
            }
            else
            {
                qsoDateOff = recdMsg.RxDate.ToString("yyyyMMdd");
                qsoTimeOff = recdMsg.SinceMidnight.ToString("hhmmss");
            }
            string qsoMode = mode;
            string grid = "";
            DecodeMessage gridMsg = ReplyMsg(call);
            if (gridMsg == null) gridMsg = cqMsg;
            if (gridMsg != null)
            {
                string g = WsjtxMessage.Grid(gridMsg.Message);
                if (g != null) grid = g;                //CQ does have a grid
            }
            string freq = ((dialFrequency + txOffset) / 1e6).ToString("F6", System.Globalization.CultureInfo.InvariantCulture);
            string band = FreqToBand(dialFrequency / 1e6);

            string adifRecord = $"<call:{call.Length}>{call} <gridsquare:{grid.Length}>{grid} <mode:{mode.Length}>{mode} <rst_sent:{rstSent.Length}>{rstSent} <rst_rcvd:{rstRecd.Length}>{rstRecd} <qso_date:{qsoDateOn.Length}>{qsoDateOn} <time_on:{qsoTimeOn.Length}>{qsoTimeOn} <qso_date_off:{qsoDateOff.Length}>{qsoDateOff} <time_off:{qsoTimeOff.Length}>{qsoTimeOff} <band:{band.Length}>{band} <freq:{freq.Length}>{freq} <station_callsign:{myCall.Length}>{myCall} <my_gridsquare:{myGrid.Length}>{myGrid}";

            //request add record to log / worked before (using explicit parameters, unlike typical WSJT-X logging)
            //send ADIF record to WSJT-X for re-broadcast to logging pgms
            emsg.NewTxMsgIdx = 255;     //function code
            emsg.GenMsg = $"{call}${grid}${band}${mode}";
            emsg.SkipGrid = false;      //no effect
            emsg.UseRR73 = false;      //no effect
            emsg.CmdCheck = adifRecord;
            ba = emsg.GetBytes();
            udpClient2.Send(ba, ba.Length);
            DebugOutput($"{Time()} >>>>>Sent 'Broadcast' cmd:255\n{emsg}");

            if (ctrl.loggedCheckBox.Checked) Play("echo.wav");
            ctrl.ShowMsg($"Logging QSO with {call}", false);
            logList.Add(call);      //even if already logged this mode/band for this session
            if (isPota) AddPotaLogDict(call, DateTime.Now, band, mode);         //local date/time
            UpdateCqCall(call);     //no more CQ responses to this call
            ShowLogged();
            DebugOutput($"{Time()} QSO logged: call'{call}'");
            UpdateDebug();
        }

        private string FreqToBand(double? freq)
        {
            if (freq == null) return "";
            if (freq >= 0.1357 && freq <= 0.1378) return "2200m";
            if (freq >= 0.472 && freq <= 0.479) return "630m";
            if (freq >= 1.8 && freq <= 2.0) return "160m";
            if (freq >= 3.5 && freq <= 4.0) return "80m";
            if (freq >= 5.35 && freq <= 5.37) return "60m";
            if (freq >= 7.0 && freq <= 7.3) return "40m";
            if (freq >= 10.1 && freq <= 10.15) return "30m";
            if (freq >= 14.0 && freq <= 14.35) return "20m";
            if (freq >= 18.068 && freq <= 18.168) return "17m";
            if (freq >= 21.0 && freq <= 21.45) return "15m";
            if (freq >= 24.89 && freq <= 24.99) return "12m";
            if (freq >= 28.0 && freq <= 29.7) return "10m";
            if (freq >= 50.0 && freq <= 54.0) return "6m";
            if (freq >= 144.0 && freq <= 148.0) return "2m";
            return "";
        }

        private void RemoveAllCall(string call)
        {
            if (call == null) return;
            if (allCallDict.ContainsKey(call))
            {
                allCallDict.Remove(call);
                DebugOutput($"{spacer}removed '{call}' from allCallDict");
            }

            if (reportList.Contains(call))
            {
                reportList.Remove(call);
                DebugOutput($"{spacer}removed '{call}' from reportList");
            }
        }

        private string CurrentStatus()
        {
            return $"myCall:'{myCall}' callInProg:'{callInProg}' qsoState:{qsoState} lastQsoState:{lastQsoState} txMsg:'{txMsg}'\n           lastTxMsg:'{lastTxMsg}' replyCmd:'{replyCmd}' curCmd:'{curCmd}'\n           txTimeout:{txTimeout} xmitCycleCount:{xmitCycleCount} transmitting:{transmitting} mode:{mode} txEnabled:{txEnabled} autoCalling:{autoCalling}\n           txFirst:{txFirst} dxCall:'{dxCall}' trPeriod:{trPeriod} dblClk:{dblClk} ignoreTxDisable:{ignoreTxDisable}\n           newDirCq:{newDirCq} tCall:'{tCall}' decoding:{decoding} restartQueue:{restartQueue} paused:{paused} replyAndQuit:{replyAndQuit}\n           {CallQueueString()}";
        }

        private void DebugOutputStatus()
        {
            DebugOutput($"(update)   {CurrentStatus()}");
        }

        //detect supported mode
        private void CheckModeSupported()
        {
            string s = "";
            modeSupported = supportedModes.Contains(mode) && specOp == 0;
            DebugOutput($"{Time()} modeSupported:{modeSupported}");
            if (!modeSupported)
            {
                if (specOp != 0) s = "Special ";
                DebugOutput($"{spacer}opMode:{opMode} specOp:{specOp}, waiting for mode status...");
                failReason = $"{s}{mode} mode not supported";
            }
            ShowStatus();
        }

        private string DatagramString(byte[] datagram)
        {
            var sb = new StringBuilder();
            string delim = "";
            for (int i = 0; i < datagram.Length; i++)
            {
                sb.Append(delim);
                sb.Append(datagram[i].ToString("X2"));
                delim = " ";
            }
            return sb.ToString();
        }

        //stop responding to CQs from this call
        private void UpdateCqCall(string call)
        {
            int prevCqs;
            if (cqCallDict.TryGetValue(call, out prevCqs))
            {
                cqCallDict.Remove(call);
            }
            cqCallDict.Add(call, maxPrevCqs);
        }

        public void EditCallQueue(int idx)
        {
            if (callQueue.Count < idx + 1) return;
            var callArray = callQueue.ToArray();
            string call = callArray[idx];
            if (MessageBox.Show($"Delete {call}?", pgmName, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                RemoveCall(call);
            }
        }

        //must be actual DX (relative to current continent) to match "DX"
        private bool isDirectedAlert(string dirTo, bool isDx)
        {
            if (dirTo == null) return false;

            string s = ctrl.alertTextBox.Text.ToUpper();
            string[] a = s.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string elem in a)
            {
                if (elem == dirTo && (elem != "DX" || isDx)) return true;
            }
            return false;
        }

        //return true if received a R-XX or R+XX from the specified call
        private bool RecdRogerReport(string call)
        {
            if (call == null) return false;
            List<DecodeMessage> msgList;
            if (!allCallDict.TryGetValue(call, out msgList)) return false;          //no previous call(s) from DX station
            //DebugOutput($"{spacer}recd previous call(s)");
            return msgList.Find(RogerReport) != null;        //the DX station never sent R-XX or R+XX
        }

        //return true if received a -XX or +XX from the specified call
        private bool RecdReport(string call)
        {
            if (call == null) return false;
            List<DecodeMessage> msgList;
            if (!allCallDict.TryGetValue(call, out msgList)) return false;          //no previous call(s) from DX station
            //DebugOutput($"{spacer}recd previous call(s)");
            return msgList.Find(Report) != null;        //the DX station never sent -XX or +XX
        }

        //return true if received a grid from specified call
        private bool RecdReply(string call)
        {
            if (call == null) return false;
            List<DecodeMessage> msgList;
            if (!allCallDict.TryGetValue(call, out msgList)) return false;          //no previous call(s) from DX station
            //DebugOutput($"{spacer}recd previous call(s)");
            return msgList.Find(Reply) != null;        //the DX station never sent grid
        }

        private bool RecdSignoff(string call)
        {
            if (call == null) return false;
            List<DecodeMessage> msgList;
            if (!allCallDict.TryGetValue(call, out msgList)) return false;          //no previous call(s) from DX station
            //DebugOutput($"{spacer}recd previous call(s)");
            return msgList.Find(Signoff) != null;        //the DX station never sent 73 or RR73
        }

        private DecodeMessage ReplyMsg(string call)
        {
            if (call == null) return null;
            List<DecodeMessage> msgList;
            if (!allCallDict.TryGetValue(call, out msgList)) return null;          //no previous call(s) from DX station
            //DebugOutput($"{spacer}recd previous call(s)");
            return msgList.Find(Reply);
        }

        private DecodeMessage CqMsg(string call)
        {
            if (call == null) return null;
            List<DecodeMessage> msgList;
            if (!allCallDict.TryGetValue(call, out msgList)) return null;          //no previous call(s) from DX station
            //DebugOutput($"{spacer}recd previous call(s)");
            return msgList.Find(CQ);
        }

        private bool RecdAnyMsg(string call)
        {
            if (call == null) return false;
            return RecdReply(call) || RecdReport(call) || RecdRogerReport(call);
        }

        private bool TrimAllCallDict()
        {
            bool removed = false;
            var keys = new List<string>();
            var dtNow = DateTime.UtcNow;
            var ts = new TimeSpan(0, maxDecodeAgeMinutes, 0);

            foreach (var entry in allCallDict)
            {
                var list = entry.Value;
                if (entry.Key != callInProg && list.Count > 0)
                {
                    var decode = list[0];           //just check the oldest entry
                    if ((dtNow - (decode.RxDate + decode.SinceMidnight)) > ts)  //entry is older than wanted
                    {
                        keys.Add(entry.Key);        //collect keys to delete
                    }
                }
            }

            //delete keys to old decodes and sent reports
            foreach (string key in keys)
            {
                if (!callQueue.Contains(key))
                {
                    RemoveAllCall(key);
                    removed = true;
                }
            }

            if (removed) DebugOutput($"{spacer}TrimAllCallDict: expired calls removed from allCallDict and/or reportList");
            return removed;
        }

        private bool TrimCallQueue()
        {
            bool removed = false;
            var keys = new List<string>();
            var dtNow = DateTime.UtcNow;
            var ts = new TimeSpan(0, maxDecodeAgeMinutes, 0);

            foreach (var entry in callDict)
            {
                if (entry.Key != callInProg && (dtNow - (entry.Value.RxDate + entry.Value.SinceMidnight)) > ts)  //entry is older than wanted
                {
                    keys.Add(entry.Key);        //collect keys to delete
                }
            }

            //delete keys to old decodes
            foreach (string key in keys)
            {
                RemoveCall(key);
                removed = true;
            }

            if (removed) DebugOutput($"{spacer}TrimCallQueue: expired calls removed from callQueue and callDict");
            return removed;
        }


        private void SetCallInProg(string call)
        {
            if (call != callInProg)
            {
                DebugOutput($"{spacer}SetCallInProg: callInProg:'{call}' (was '{callInProg}') lastTxMsg:{lastTxMsg}");
            }
            callInProg = call;
            UpdateCallInProg();
        }

        private void EnableTx()     //status response from WSJT-X completes this action
        {
            emsg.NewTxMsgIdx = 9;
            emsg.GenMsg = $"";         //ignored
            emsg.CmdCheck = "";         //ignored
            ba = emsg.GetBytes();
            udpClient2.Send(ba, ba.Length);
            DebugOutput($"{Time()} >>>>>Sent 'Enable Tx' cmd:9\n{emsg}");
        }

        private void DisableTx(bool expectStatusResponse)
        {
            DebugOutput($"{Time()} Disable Tx, expectStatusResponse:{expectStatusResponse} ignoreTxDisable:{ignoreTxDisable} processDecodeTimer.Enabled:{processDecodeTimer.Enabled}");
            if (processDecodeTimer.Enabled)
            {
                processDecodeTimer.Stop();       //no xmit cycle now
                DebugOutput($"{Time()} processDecodeTimer stop");
            }

            //inhibit status response from WSJT-X, if response expected
            ignoreTxDisable = expectStatusResponse && txEnabled;
            DebugOutput($"{spacer}ignoreTxDisable:{ignoreTxDisable}");

            try
            {
                if (emsg == null || udpClient2 == null) return;
                emsg.NewTxMsgIdx = 8;
                emsg.GenMsg = $"";         //ignored
                emsg.CmdCheck = "";         //ignored
                ba = emsg.GetBytes();
                udpClient2.Send(ba, ba.Length);
                DebugOutput($"{Time()} >>>>>Sent 'Disable Tx' cmd:8 txEnabled:{txEnabled}\n{emsg}");
            }
            catch
            {
                ignoreTxDisable = false;                //there will be no status response to ignore
                DebugOutput($"{Time()} 'Disable Tx' failed, ignoreTxDisable:{ignoreTxDisable}");        //only happens during closing
            }
        }

        private void EnableMonitoring()
        {
            emsg.NewTxMsgIdx = 11;
            emsg.GenMsg = $"";         //ignored
            emsg.CmdCheck = "";         //ignored
            ba = emsg.GetBytes();
            udpClient2.Send(ba, ba.Length);
            DebugOutput($"{Time()} >>>>>Sent 'Enable Monitoring' cmd:11\n{emsg}");
        }

        private void HaltTx()
        {
            emsg.NewTxMsgIdx = 12;
            emsg.GenMsg = $"";         //ignored
            emsg.CmdCheck = "";         //ignored
            ba = emsg.GetBytes();
            udpClient2.Send(ba, ba.Length);
            DebugOutput($"{Time()} >>>>>Sent 'Halt Tx' cmd:12\n{emsg}");
        }

        private void UpdateModeSelection()
        {
            if (paused)
            {
                ctrl.pauseButton.Checked = true;
                ctrl.cqModeButton.Checked = false;
                ctrl.listenModeButton.Checked = false;
            }
            else
            {
                ctrl.pauseButton.Checked = false;
                ctrl.cqModeButton.Checked = !replyAndQuit;
                ctrl.listenModeButton.Checked = replyAndQuit;
            }
        }

        private void UpdateTxStopTimeEnable()
        {
            bool active = (opMode == OpModes.ACTIVE);
            ctrl.stopCheckBox.Enabled = active;
            ctrl.stopTextBox.Enabled = active;
            ctrl.timeLabel.Enabled = active;
        }

        private void ProcessDecodeTimerTick(object sender, EventArgs e)
        {
            processDecodeTimer.Stop();
            DebugOutput($"\n{Time()} processDecodeTimer tick, stop");
            ProcessDecodes();
        }

        //the last decode pass has completed, ready to detect first decode pass
        private void DecodesCompleted(object sender, EventArgs e)
        {
            decodeEndTimer.Stop();
            firstDecodePass = true;
            DebugOutput($"{Time()} Last decode completed, decodeEndTimer stop, firstDecodePass:{firstDecodePass}");

            if (!skipAudioOffsetCalc)
            {
                CalcBestOffset(audioOffsets, period);       //calc for period when decodes started
            }
            skipAudioOffsetCalc = false;
        }

        private void HeartbeatNotRecd(object sender, EventArgs e)
        {
            //no heartbeat from WSJT-X, re-init communication
            heartbeatRecdTimer.Stop();
            DebugOutput($"{Time()} heartbeatRecdTimer timed out");
            if (WsjtxMessage.NegoState == WsjtxMessage.NegoStates.RECD)
            {
                ctrl.ShowMsg("WSJT-X disconnected", false);
                Play("dive.wav");
            }
            ResetNego();
        }

        private void CheckCallQueuePeriod(bool newTxFirst)
        {
            bool removed = false;
            var calls = new List<string>();

            foreach (var entry in callDict)
            {
                var decode = entry.Value;
                if (IsEvenPeriod(decode.SinceMidnight.Seconds * 1000) == newTxFirst)  //entry is wrong time period for new txFirst
                {
                    calls.Add(entry.Key);        //collect keys to delete
                }
            }

            //delete from callQueue
            foreach (string call in calls)
            {
                RemoveCall(call);
                removed = true;
            }

            if (removed) DebugOutput($"{spacer}CheckCallQueuePeriod: calls removed: {CallQueueString()} {CallDictString()}");
            return;
        }

        private bool IsSameMessage(string tx, string lastTx)
        {
            if (tx == lastTx) return true;
            if (WsjtxMessage.ToCall(tx) != WsjtxMessage.ToCall(lastTx)) return false;
            if (WsjtxMessage.IsReport(tx) && WsjtxMessage.IsReport(lastTx)) return true;
            if (WsjtxMessage.IsRogerReport(tx) && WsjtxMessage.IsRogerReport(lastTx)) return true;
            return false;
        }

        //set log file open/closed state
        //return new diagnostic log file state (true = open)
        private bool SetLogFileState(bool enable)
        {
            if (enable)         //want log file opened for write
            {
                if (logSw == null)     //log not already open
                {
                    try
                    {
                        if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                        logSw = File.AppendText($"{path}\\log_{DateTime.Now.Date.ToShortDateString().Replace('/', '-')}.txt");      //local time
                        logSw.AutoFlush = true;
                        logSw.WriteLine($"\n\n{Time()} Opened log");
                    }
                    catch (Exception err)
                    {
                        err.ToString();
                        logSw = null;
                        return false;       //log file state = closed
                    }
                }
                return true;       //log file state = open
            }
            else    //want log file flushed and closed
            {
                if (logSw != null)
                {
                    logSw.WriteLine($"{Time()} Closing log...");
                    logSw.Flush();
                    logSw.Close();
                    logSw = null;
                }
                return false;       //log file state = closed
            }
        }

        private void ReadPotaLogDict()
        {
            List<string> updList = new List<string>();
            string pathFileNameExt = $"{path}\\pota.txt";
            StreamReader potaSr = null;
            potaSw = null;
            potaLogDict.Clear();

            try
            {
                if (File.Exists(pathFileNameExt))
                {
                    string line = null;
                    string today = DateTime.Now.ToShortDateString();        //local time
                    potaSr = File.OpenText(pathFileNameExt);
                    DebugOutput($"{Time()} POTA log opened for read");

                    while ((line = potaSr.ReadLine()) != null)
                    {
                        string[] parts = line.Split(new char[] { ',' });   //call,date,band,mode
                        if (parts.Length == 4 && parts[1] == today)
                        {                       //date     band       mode
                            string potaInfo = $"{parts[1]},{parts[2]},{parts[3]}";
                            List<string> curList;
                            //                          call
                            if (potaLogDict.TryGetValue(parts[0], out curList))
                            {
                                if (!curList.Contains(potaInfo)) curList.Add(potaInfo);
                            }
                            else
                            {
                                List<string> newList = new List<string>();
                                newList.Add(potaInfo);
                                //              call
                                potaLogDict.Add(parts[0], newList);
                            }

                            updList.Add(line);
                        }
                    }
                    potaSr.Close();
                }
            }
            catch (Exception err)
            {
                DebugOutput($"{Time()} POTA log open/read failed: {err.ToString()}");
                if (potaSr != null) potaSr.Close();
                return;
            }

            //open, re-write updated file; leave file open if no error
            try
            {
                if (File.Exists(pathFileNameExt)) File.Delete(pathFileNameExt);
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                potaSw = File.AppendText(pathFileNameExt);
                potaSw.AutoFlush = true;
                DebugOutput($"{Time()} POTA log opened for write");

                foreach (string line in updList)
                {
                    potaSw.WriteLine(line);
                }
            }
            catch (Exception err)
            {
                DebugOutput($"{Time()} POTA log open/rewrite failed: {err.ToString()}");
                potaSw = null;
            }
            DebugOutput($"{PotaLogDictString()}");
        }

        private void AddPotaLogDict(string potaCall, DateTime potaDtLocal, string potaBand, string potaMode)     //UTC
        {
            bool updateLog = false;

            string potaInfo = $"{potaDtLocal.Date.ToShortDateString()},{potaBand},{potaMode}";
            DebugOutput($"{Time()} AddPotaLogDict, potaInfo:{potaInfo}");
            DebugOutput($"{PotaLogDictString()}");
            List<string> curList;
            if (potaLogDict.TryGetValue(potaCall, out curList))
            {
                if (!curList.Contains(potaInfo))
                {
                    curList.Add(potaInfo);
                    updateLog = true;
                }
            }
            else
            {
                List<string> newList = new List<string>();
                newList.Add(potaInfo);
                potaLogDict.Add(potaCall, newList);
                updateLog = true;
            }

            if (potaSw != null && updateLog)
            {
                potaSw.WriteLine($"{potaCall},{potaInfo}");
                DebugOutput($"{PotaLogDictString()}");
            }
        }

        private void CalcBestOffset(List<int> offsetList, Periods decodePeriod)
        {
            if (period == Periods.UNK)
            {
                oddOffset = 0;
                evenOffset = 0;
                return;
            }

            int bestOffset = 0;
            int maxInterval = 0;

            //set limits
            offsetList.Add(offsetLoLimit);
            offsetList.Add(offsetHiLimit);

            offsetList.Sort();
            int[] offsets = offsetList.ToArray();

            for (int i = 0; i < offsets.Length - 1; i++)
            {
                if (offsets[i + 1] - offsets[i] > maxInterval)
                {
                    maxInterval = offsets[i + 1] - offsets[i];
                    bestOffset = (offsets[i + 1] + offsets[i]) / 2;
                }
            }

            if (decodePeriod == Periods.EVEN)
            {
                evenOffset = bestOffset;
            }
            else
            {
                oddOffset = bestOffset;
            }
            offsetList.Clear();

            if (oddOffset > 0 && evenOffset > 0)
            {
                ctrl.freqCheckBox.Text = "Select best TX frequency";
                ctrl.freqCheckBox.ForeColor = Color.Black;
            }

            if (ctrl.freqCheckBox.Checked) CheckSetupCq();
            UpdateDebug();
        }

        private UInt32 AudioOffsetFromMsg(DecodeMessage msg)        //msg is a reply msg, so tx msg will be opposite time period
        {
            if (!ctrl.freqCheckBox.Checked) return 0;

            if (IsEvenPeriod(msg.SinceMidnight.Seconds * 1000))
            {
                return (UInt32)oddOffset;
            }
            else
            {
                return (UInt32)evenOffset;
            }
        }

        private UInt32 AudioOffsetFromTxPeriod()
        {
            if (period == Periods.UNK || !ctrl.freqCheckBox.Checked) return 0;

            if (txFirst)
            {
                return (UInt32)evenOffset;
            }
            else
            {
                return (UInt32)oddOffset;
            }
        }

        private DateTime ScheduledOffDateTime()
        {
            DateTime dtNow = DateTime.Now;              //local time
            string stop = ctrl.stopTextBox.Text.Trim(); ;
            DateTime dtStop = new DateTime(
                dtNow.Year, 
                dtNow.Month, 
                dtNow.Day, 
                Convert.ToInt32(stop.Substring(0, 2)), 
                Convert.ToInt32(stop.Substring(2, 2)), 
                0);

            if (dtStop <= dtNow) dtStop = dtStop.AddHours(24);

            return dtStop;
        }

        private int CalcTimerAdj()
        {
            return (mode == "FT8" ? 300 : (mode == "FT4" ? 300 : (mode == "FST4" ? 750 : 300)));      //msec
        }
    }
}

