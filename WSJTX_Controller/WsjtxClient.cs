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

        private List<string> acceptableWsjtxVersions = new List<string> { "2.2.2/237", "2.3.0-rc2/105", "2.3.0-rc2/106", "2.3.0-rc2/107" };
        private List<string> supportedModes = new List<string>() { "FT8", "FT4", "FST4" };

        //const
        public int maxPrevCqs = 2;
        public int maxAutoGenEnqueue = 4;

        private bool logToFile = false;
        private StreamWriter sw;
        private bool suspendComm = false;
        private bool settingChanged = false;
        private string cmdCheck = "";
        private bool commConfirmed = false;
        public string myCall = null, myGrid = null;
        private Dictionary<string, DecodeMessage> callDict = new Dictionary<string, DecodeMessage>();
        private Queue<string> callQueue = new Queue<string>();
        private List<string> reportList = new List<string>();
        private List<string> configList = new List<string>();
        private Dictionary<string, List<DecodeMessage>> allCallDict = new Dictionary<string, List<DecodeMessage>>();
        private Dictionary<string, int> cqCallDict = new Dictionary<string, int>();
        private bool txEnabled = false;
        private bool transmitting = false;
        private bool autoCalling = true;
        private bool decoding = false;
        private bool qsoLogged = false;
        private WsjtxMessage.QsoStates qsoState = WsjtxMessage.QsoStates.CALLING;
        private string mode = "";
        private bool modeSupported = true;
        private bool? lastModeSupported = null;
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
        private string lastCurCmd = null;
        private WsjtxMessage.QsoStates lastQsoStateDebug = WsjtxMessage.QsoStates.INVALID;
        private string lastDxCallDebug = null;

        private string lastDxCall = null;
        private string lastStatus = null;
        private int xmitCycleCount = 0;
        private bool txTimeout = false;
        private bool newDirCq = false;
        private int specOp = 0;
        private string qCall = null;            //call sign for last QSO logged
        private string tCall = null;            //call sign being processed at timeout
        private string txMsg = null;            //msg for the most-recent Tx
        private List<string> logList = new List<string>();

        private AsyncCallback asyncCallback;
        private UdpState s;
        private static bool messageRecd;
        private static byte[] datagram;
        private static IPEndPoint fromEp = new IPEndPoint(IPAddress.Any, 0);
        private static bool recvStarted;
        private string failReason = "Failure reason: Unknown";

        private const int maxQueueLines = 6, maxQueueWidth = 19, maxLogWidth = 9;
        private byte[] ba;
        private EnableTxMessage emsg;
        //HaltTxMessage amsg;
        private WsjtxMessage msg = new UnknownMessage();
        private string errorDesc = null;
        private Random rnd = new Random();
        DateTime firstDecodeTime;
        private const string spacer = "           *";
        private string WsjtxLogPathFilename = $"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\WSJT-X\\wsjtx_log.adi";

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

        public WsjtxClient(Controller c, IPAddress reqIpAddress, int reqPort, bool reqMulticast, bool reqDebug, DateTime dt)
        {
            ctrl = c;           //used for accessing/updating UI
            ipAddress = reqIpAddress;
            port = reqPort;
            multicast = reqMulticast;
            firstRunDateTime = dt;
            //major.minor.build.private
            string allVer = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion;
            Version v;
            Version.TryParse(allVer, out v);
            string fileVer = $"{v.Major}.{v.Minor}.{v.Build}";
            WsjtxMessage.PgmVersion = fileVer;
            debug = reqDebug;
            opMode = OpModes.IDLE;
            WsjtxMessage.NegoState = WsjtxMessage.NegoStates.INITIAL;
            pgmName = ctrl.Text;      //or Assembly.GetExecutingAssembly().GetName().ToString();

            logToFile = (DateTime.Now - firstRunDateTime).TotalDays < 28;
            if (logToFile)
            {
                try
                {
                    string path = $"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\{Assembly.GetExecutingAssembly().GetName().Name.ToString()}";
                    if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                    sw = File.AppendText($"{path}\\log_{DateTime.Now.Date.ToShortDateString().Replace('/', '-')}.txt");
                    sw.AutoFlush = true;
                    DebugOutput($"\n\n{DateTime.UtcNow.ToString("yyyy-MM-dd HHmmss")} UTC ###################### Program starting.... v{fileVer} ipAddress:{ipAddress} port:{port} multicast:{multicast}");
                }
                catch (Exception err)
                {
                    err.ToString();
                    logToFile = false;
                    sw = null;
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

            ctrl.alertTextBox.Enabled = false;
            ctrl.directedTextBox.Enabled = false;
            ctrl.timeoutLabel.Visible = false;

            emsg = new EnableTxMessage();
            emsg.Id = WsjtxMessage.UniqueId;

            //amsg = new HaltTxMessage();
            //amsg.Id = WsjtxMessage.UniqueId;
            //amsg.AutoOnly = true;
            firstDecodeTime = DateTime.MinValue;

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
                Console.WriteLine($"Exception: ReceiveCallback() {err}");
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
                emsg.SkipGrid = false;      //no effect
                emsg.UseRR73 = false;      //no effect
                emsg.CmdCheck = cmdCheck;
                ba = emsg.GetBytes();
                udpClient2.Send(ba, ba.Length);
                DebugOutput($"{Time()} >>>>>Sent 'Ack Req' cmd:7 cmdCheck:{cmdCheck}\n{emsg}");

                ctrl.timer5.Interval = 30000;           //set up cmd check timeout
                ctrl.timer5.Start();
                DebugOutput($"{spacer}check cmd timer started");
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
                if (smsg.DblClk || (txEnabled != lastTxEnabled && txEnabled)) ctrl.ShowMsg("Not ready yet... please wait", true);
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
                EnqueueDecodeMessage emsg = (EnqueueDecodeMessage)msg;
                if (!emsg.AutoGen) ctrl.ShowMsg("Not ready yet... please wait", true);
            }

            //************
            //CloseMessage
            //************
            if (msg.GetType().Name == "CloseMessage")
            {
                DebugOutput($"{msg}");

                if (udpClient2 != null) udpClient2.Close();
                ResetNego();     //wait for (new) WSJT-X mode
                //DebugOutput(msg);
                return;
            }

            //****************
            //HeartbeatMessage
            //****************
            //in case 'Monitor' disabled, get StatusMessages
            if (!commConfirmed && msg.GetType().Name == "HeartbeatMessage")
            {
                DebugOutput($"{Time()}\n{msg}");
                emsg.NewTxMsgIdx = 7;
                emsg.GenMsg = $"";          //no effect
                emsg.SkipGrid = false;      //no effect
                emsg.UseRR73 = false;      //no effect
                emsg.CmdCheck = cmdCheck;
                ba = emsg.GetBytes();
                udpClient2.Send(ba, ba.Length);
                DebugOutput($"{Time()} >>>>>Sent 'Ack Req' cmd:7 cmdCheck:{cmdCheck}\n{emsg}");
            }

            if (WsjtxMessage.NegoState == WsjtxMessage.NegoStates.RECD)
            {
                if (modeSupported)
                {
                    //*************
                    //DecodeMessage
                    //*************
                    if (msg.GetType().Name == "DecodeMessage" && myCall != null)
                    {
                        DecodeMessage dmsg = (DecodeMessage)msg;
                        latestDecodeTime = dmsg.SinceMidnight;
                        rawMode = dmsg.Mode;    //different from mode string in status msg
                        if (dmsg.New)           //important to reject replays requested by other pgms
                        {
                            if (dmsg.IsCallTo(myCall)) DebugOutput($"{dmsg}\n           *msg:'{dmsg.Message}'");

                            string deCall = dmsg.DeCall();
                            //do some processing not directly related to replying immediately
                            if (deCall != null)
                            {
                                //check for directed CQ
                                if (ctrl.alertCheckBox.Checked && !ctrl.replyCqCheckBox.Checked)
                                {
                                    string d = WsjtxMessage.DirectedTo(dmsg.Message);
                                    if (d != null && ctrl.alertTextBox.Text.ToUpper().Contains(d))
                                        Play("beepbeep.wav");   //directed CQ
                                }

                                //check for decode being a call to myCall
                                if (myCall != null && dmsg.IsCallTo(myCall))
                                {
                                    dmsg.Priority = true;       //as opposed to a decode from anyone else
                                    UpdateDebug();
                                    if (ctrl.mycallCheckBox.Checked) Play("trumpet.wav");   //not the call just logged

                                    //if call not logged: save Report (...+03) and RogerReport (...-02) decodes for out-of-order call processing
                                    if (!logList.Contains(deCall) && WsjtxMessage.IsReport(dmsg.Message) || WsjtxMessage.IsRogerReport(dmsg.Message))
                                    {
                                        List<DecodeMessage> vlist;
                                        //create new List for deCall if nothing entered yet into the Dictionary
                                        if (!allCallDict.TryGetValue(deCall, out vlist)) allCallDict.Add(deCall, vlist = new List<DecodeMessage>());
                                        vlist.Add(dmsg);        //messages from deCall are in order rec's, will be duplicate message types
                                    }
                                    CheckLateLog(deCall, dmsg);
                                }
                            }

                            //decode processing of calls to myCall requires txEnabled
                            if (txEnabled && deCall != null && myCall != null && dmsg.IsCallTo(myCall))
                            {
                                DebugOutput($"{spacer}'{deCall}' is to {myCall}");
                                if (!dmsg.Is73orRR73())       //not a 73 or RR73
                                {
                                    DebugOutput($"{spacer}Not a 73 or RR73");
                                    if (!callQueue.Contains(deCall))        //call not in queue, possibly enqueue the call data
                                    {
                                        DebugOutput($"{spacer}'{deCall}' not already in queue");
                                        if (qsoState == WsjtxMessage.QsoStates.CALLING && callQueue.Count == 0)
                                        {
                                            //"Call 1st" never in effect, so set up to reply to this call
                                            if (autoCalling && deCall != callInProg)                //this call or another call currently being processed by WSJT-X
                                            {
                                                DebugOutput($"{spacer}callQueue empty, autoCalling:{autoCalling}, adding '{deCall}', not currently being processed");
                                                AddCall(deCall, dmsg);
                                                txTimeout = true;
                                                callInProg = null;
                                                DebugOutput($"{spacer}qsoState:{qsoState} txTimeout:{txTimeout} tCall:'{tCall}' callInProg:'{callInProg}'");
                                            }
                                        }
                                        else   //not CALLING or call queue has entries
                                        {
                                            DebugOutput($"{spacer}calls queued or not CALLING, qsoState:{qsoState})");
                                            if (deCall == callInProg)                //call currently being processed by WSJT-X
                                            {
                                                DebugOutput($"{spacer}'{deCall}' currently being processed");
                                            }
                                            else
                                            {
                                                DebugOutput($"{spacer}'{deCall}' not currently being processed");
                                                AddCall(deCall, dmsg);          //known to not be in queue
                                            }
                                        }
                                    }
                                    else       //call is already in queue, possibly update the call data
                                    {
                                        DebugOutput($"{spacer}'{deCall}' already in queue");
                                        if (deCall == callInProg)                //call currently being processed by WSJT-X
                                        {
                                            DebugOutput($"{spacer}'{deCall}' currently being processed");
                                            RemoveCall(deCall);             //may have been queued previously
                                        }
                                        else        //update the call in queue
                                        {
                                            DebugOutput($"{spacer}'{deCall}' not currently being processed, update queue");
                                            UpdateCall(deCall, dmsg);
                                        }
                                    }
                                }
                                else        //decode is 73 or RR73 msg
                                {
                                    DebugOutput($"{spacer}decode is 73 or RR73");
                                }
                                UpdateDebug();
                            }
                        }
                        return;
                    }

                    //EnqueueDecodeMessage
                    //*************
                    if (msg.GetType().Name == "EnqueueDecodeMessage" && myCall != null)
                    {
                        EnqueueDecodeMessage emsg = (EnqueueDecodeMessage)msg;
                        AddSelectedCall(emsg);
                        DebugOutput($"{Time()} AddSelectedCall: Modifier:{emsg.Modifier} AutoGen:{emsg.AutoGen}\n{emsg}");
                        UpdateDebug();
                    }

                    //****************
                    //QsoLoggedMessage
                    //****************
                    if (txEnabled && msg.GetType().Name == "QsoLoggedMessage")
                    {
                        //ack'ing either early or normal logging
                        //WSJT-X's auto-logging is disabled
                        QsoLoggedMessage lmsg = (QsoLoggedMessage)msg;
                        DebugOutput($"{lmsg}");
                        qCall = lmsg.DxCall;
                        if (ctrl.loggedCheckBox.Checked) Play("echo.wav");
                        logList.Add(qCall);    //even if already logged this mode/band
                        ShowLogged();
                        ctrl.ShowMsg($"Logging QSO with {qCall}", false);
                        DebugOutput($"{Time()} QSO logging ackd: qCall'{qCall}'");
                        RemoveAllCall(qCall);
                        UpdateCqCall(qCall);    //no more CQ responses to this call
                        UpdateDebug();

                        //check for call sign in queue/dictionary,
                        //this would be the case for the user manually logging a QSO in WSJT-X.
                        //if this is the case, remove this call sign since it has already been processed
                        RemoveCall(qCall);
                        return;
                    }
                }

                //*************
                //StatusMessage
                //*************
                if (msg.GetType().Name == "StatusMessage")
                {
                    StatusMessage smsg = (StatusMessage)msg;
                    DateTime dtNow = DateTime.Now;
                    //DebugOutput($"\n{Time()}\n{smsg}");                        
                    qsoState = smsg.CurQsoState();
                    txEnabled = smsg.TxEnabled;
                    dxCall = smsg.DxCall;                               //unreliable info, can be edited manually
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
                    if (lastTxMsg == null) lastTxMsg = txMsg;   //initialize
                    if (smsg.TRPeriod != null) trPeriod = (int)smsg.TRPeriod;

                    if (ctrl.timer5.Enabled && smsg.Check == cmdCheck)             //found the random cmd check string, cmd receive ack'd
                    {
                        ctrl.timer5.Stop();
                        commConfirmed = true;
                        DebugOutput($"{Time()} Check cmd rec'd, match");
                    }

                    if (myCall == null) CheckMyCall(smsg);

                    //detect xmit start/end ASAP
                    if (trPeriod != null && transmitting != lastXmitting)
                    {
                        if (transmitting)
                        {
                            StartTimer2();
                            ProcessTxStart();
                            if (firstDecodeTime == DateTime.MinValue) firstDecodeTime = DateTime.Now;       //start counting until WSJT-X watchdog timer set
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
                                if (debug) Console.Beep();
                                ShowStatus();
                                UpdateDebug();
                                DebugOutput($"{spacer}new DX call selected manually during Rx: txTimeout:{txTimeout} xmitCycleCount:{xmitCycleCount} replyCmd:'{replyCmd}' autoCalling:{autoCalling}");
                            }
                        }
                        autoCalling = false;
                        ShowStatus();
                        DebugOutput($"{Time()}\nStatus     {CurrentStatus()}");
                    }
                    if (!autoCalling)
                    {
                        callInProg = dxCall;            //dxCall may be set later than dblClk
                        DebugOutput($"{Time()} callInProg:'{callInProg}'");
                    }


                    //detect WSJT-X mode change
                    if (mode != lastMode)
                    {
                        DebugOutput($"{Time()} mode:{mode} (was {lastMode})");
                        ResetOpMode();
                        CheckModeSupported();
                        lastMode = mode;
                    }

                    //detect WSJT-X special operating mode change
                    if (specOp != lastSpecOp)
                    {
                        DebugOutput($"{Time()} specOp:{specOp} (was {lastSpecOp})");
                        ResetOpMode();
                        CheckModeSupported();
                        lastSpecOp = specOp;
                    }

                    //check for time to flag starting first xmit
                    if (supportedModes.Contains(mode) && (mode != "FT8" || specOp == 0) && opMode == OpModes.IDLE)
                    {
                        opMode = OpModes.START;
                        ShowStatus();
                        UpdateDebug();
                        DebugOutput($"{Time()} opMode:{opMode}");
                    }

                    //detect decoding start/end
                    if (smsg.Decoding != lastDecoding)
                    {
                        if (smsg.Decoding)
                        {
                            DebugOutput($"{Time()} Decode start");
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
                        DebugOutput($"{Time()}\nStatus     {CurrentStatus()}");
                    }

                    //check for changed Tx enabled
                    if (lastTxEnabled != txEnabled)
                    {
                        DebugOutput($"{Time()} txEnabled:{txEnabled} (was {lastTxEnabled})");
                        if (!txEnabled) ctrl.timer2.Stop();       //no xmit cycle now

                        if (txEnabled && callQueue.Count > 0 && qsoState == WsjtxMessage.QsoStates.CALLING)
                        {
                            DebugOutput($"{Time()} Tx enabled: starting queue processing");
                            txTimeout = true;                   //triggers next in queue
                            tCall = null;                       //prevent call removal from queue
                            DebugOutput($"{spacer}txTimeout:{txTimeout} tCall:'{tCall}'");
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
                                if ((DateTime.Now - firstDecodeTime).TotalMinutes < 15)
                                {
                                    ModelessDialog("Set the 'Tx watchdog' in WSJT-X to 15 minutes or more.\n\nThis will be the timeout in case the Controller sends the same message repeatedly (ex: Calling CQ, when the band is inactive).\n\nThe WSJT-X 'Tx watchdog' is under File | Settings, in the 'General' tab.");
                                }
                                else
                                {
                                    ModelessDialog("The 'Tx watchdog' in WSJT-X has timed out.\n\nSelect 'Enable Tx' when ready to continue.\n\n(The WSJT-X 'Tx watchdog' setting is under File | Settings, in the 'General' tab).");
                                }
                                firstDecodeTime = DateTime.MinValue;        //allow timing to restart
                            }
                        }
                        lastTxWatchdog = smsg.TxWatchdog;
                    }

                    if (lastDialFrequency != null && (Math.Abs((Int64)lastDialFrequency - (Int64)dialFrequency) > 500000))
                    {
                        ClearCalls(true);
                        logList.Clear();            //can re-log on new band
                        DebugOutput($"{Time()} Cleared queued calls:DialFrequency");
                        lastDialFrequency = smsg.DialFrequency;
                    }

                    //detect WSJT-X Tx First change
                    if (txFirst != lastTxFirst)
                    {
                        ClearCalls(false);
                        DebugOutput($"{Time()} Cleared queued calls: txFirst:{txFirst}");
                        lastTxFirst = txFirst;
                    }

                    //check for setup for CQ
                    if (commConfirmed && myCall != null && supportedModes.Contains(mode) && (mode != "FT8" || specOp == 0) && opMode == OpModes.START)
                    {
                        opMode = OpModes.ACTIVE;
                        UpdateDebug();
                        DebugOutput($"{Time()} opMode:{opMode}");
                        ShowStatus();
                        UpdateAddCall();

                        //setup for CQ
                        emsg.NewTxMsgIdx = 6;
                        emsg.GenMsg = $"CQ{NextDirCq()} {myCall} {myGrid}";
                        emsg.SkipGrid = ctrl.skipGridCheckBox.Checked;
                        emsg.UseRR73 = ctrl.useRR73CheckBox.Checked;
                        emsg.CmdCheck = cmdCheck;
                        ba = emsg.GetBytes();           //re-enable Tx for CQ
                        udpClient2.Send(ba, ba.Length);
                        DebugOutput($"{Time()} >>>>>Sent 'Setup CQ' cmd:6 cmdCheck:{cmdCheck}\n{emsg}");
                        qsoState = WsjtxMessage.QsoStates.CALLING;      //in case enqueueing call manually right now
                        callInProg = null;
                        DebugOutput($"{spacer}qsoState:{qsoState} (was {lastQsoState}) callInProg:'{callInProg}'");
                        curCmd = emsg.GenMsg;
                        DebugOutput($"{Time()}\nStatus     {CurrentStatus()}");
                    }

                    //*****end of status *****
                    UpdateDebug();
                    return;
                }
            }
        }

        private void StartTimer2()
        {
            DateTime dtNow = DateTime.UtcNow;
            int diffMsec = ((dtNow.Second * 1000) + dtNow.Millisecond) % (int)trPeriod;
            int cycleTimerAdj = (mode == "FT8" ? 300 : (mode == "FT4" ? 500 : 0));      //msec
            ctrl.timer2.Interval = (2 * (int)trPeriod) - diffMsec - cycleTimerAdj;
            ctrl.timer2.Start();
        }

        private bool CheckMyCall(StatusMessage smsg)
        {
            if (smsg.DeCall == null || smsg.DeGrid == null)
            {
                suspendComm = true;
                MessageBox.Show($"Call sign and Grid are not entered in WSJT-X.\n\nEnter these in WSJT-X by selecting 'File | Settings' in the 'General' tab.\n\n{pgmName} will try again when you close this dialog.", pgmName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                ResetNego();
                suspendComm = false;
                return false;
            }

            if (myCall == null)
            {
                myCall = smsg.DeCall;
                myGrid = smsg.DeGrid;
                if (myGrid.Length > 4)
                {
                    myGrid = myGrid.Substring(0, 4);
                }
                DebugOutput($"{Time()} myCall:{myCall} myGrid:{myGrid}");
            }
            UpdateDebug();
            return true;
        }
        private void CheckNextXmit()
        {
            //******************
            //Timeout processing
            //******************
            //check for time to initiate next xmit from queued calls
            if (txTimeout || (autoCalling && callQueue.Count > 0 && qsoState == WsjtxMessage.QsoStates.CALLING))        //important to sync qso logged to end of xmit, and manually-added call(s) to status msgs
            {
                replyCmd = null;        //last reply cmd sent is no longer in effect
                replyDecode = null;
                DebugOutput($"{Time()} CheckNextXmit(1) start: txTimeout:{txTimeout} tCall:{tCall}");
                DebugOutputStatus();
                //check for call sign in process timed out and must be removed;
                //dictionary won't contain data for this call sign if QSO handled only by WSJT-X
                if (tCall != null)     //null if call added manually, otherwise is timed out and queue updated here
                {
                    DebugOutput($"{spacer}'{tCall}' might be in process but timed out");
                    RemoveCall(tCall);
                    tCall = null;
                    DebugOutput($"{spacer}tCall:'{tCall}'");
                }

                //process the next call in the queue. if any present
                if (callQueue.Count > 0)            //have queued call signs
                {
                    DecodeMessage dmsg = new DecodeMessage();
                    string nCall = GetNextCall(out dmsg);
                    DebugOutput($"{spacer}Have entries in queue, got '{nCall}'");

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
                    callInProg = nCall;
                    DebugOutput($"{Time()} >>>>>Sent 'Reply To Msg' cmd:\n{rmsg} lastTxMsg:'{lastTxMsg}'\nreplyCmd:'{replyCmd}'");
                }
                else            //no queued call signs, start CQing
                {
                    DebugOutput($"{spacer}No entries in queue, start CQing");
                    emsg.NewTxMsgIdx = 6;
                    emsg.GenMsg = $"CQ{NextDirCq()} {myCall} {myGrid}";
                    emsg.SkipGrid = ctrl.skipGridCheckBox.Checked;
                    emsg.UseRR73 = ctrl.useRR73CheckBox.Checked;
                    emsg.CmdCheck = cmdCheck;
                    ba = emsg.GetBytes();           //set up for CQ, auto, call 1st
                    udpClient2.Send(ba, ba.Length);
                    DebugOutput($"{Time()} >>>>>Sent 'Setup CQ' cmd:6 cmdCheck:{cmdCheck}\n{emsg}");
                    qsoState = WsjtxMessage.QsoStates.CALLING;      //in case enqueueing call manually right now
                    replyCmd = null;        //invalidate last reply cmd since not replying
                    replyDecode = null;
                    curCmd = emsg.GenMsg;
                    callInProg = null;
                    DebugOutput($"{spacer}qsoState:{qsoState} (was {lastQsoState} replyCmd:'{replyCmd}')");
                    newDirCq = false;           //if set, was processed here
                }
                txTimeout = false;              //ready for next timeout
                qsoLogged = false;              //clear "logged" status
                autoCalling = true;
                DebugOutputStatus();
                DebugOutput($"{Time()} CheckNextXmit end");
                UpdateDebug();      //unconditional
                return;             //don't process newDirCq
            }

            //************************************
            //Directed CQ / new setting processing
            //************************************
            if (qsoState == WsjtxMessage.QsoStates.CALLING && newDirCq)
            {
                DebugOutput($"{Time()} CheckNextXmit(2) start");
                emsg.NewTxMsgIdx = 6;
                emsg.GenMsg = $"CQ{NextDirCq()} {myCall} {myGrid}";
                emsg.SkipGrid = ctrl.skipGridCheckBox.Checked;
                emsg.UseRR73 = ctrl.useRR73CheckBox.Checked;
                emsg.CmdCheck = cmdCheck;
                ba = emsg.GetBytes();           //set up for CQ, auto, call 1st
                udpClient2.Send(ba, ba.Length);
                DebugOutput($"{Time()} >>>>>Sent 'Setup CQ' cmd:6 cmdCheck:{cmdCheck}\n{emsg}");
                qsoState = WsjtxMessage.QsoStates.CALLING;      //in case enqueueing call manually right now
                replyCmd = null;        //invalidate last reply cmd since not replying
                replyDecode = null;
                curCmd = emsg.GenMsg;
                newDirCq = false;
                callInProg = null;
                DebugOutputStatus();
                DebugOutput($"{Time()} CheckNextXmit end");
                UpdateDebug();      //unconditional
                return;
            }
        }

        public void ProcessDecodes()
        {
            ctrl.timer2.Stop();
            DebugOutput($"\n{Time()} ProcessDecodes:");
            //check for Tx started manually during Rx
            if (txEnabled) CheckNextXmit();
            DebugOutput($"{Time()} ProcessDecodes done\n");
        }

        //check for time to log (best done at Tx start to avoid any logging/dequeueing timing problem if done at Tx end)
        private void ProcessTxStart()
        {
            string toCall = WsjtxMessage.ToCall(txMsg);
            string lastToCall = WsjtxMessage.ToCall(lastTxMsg);
            callInProg = null;
            if (toCall != "CQ") callInProg = toCall;
            DebugOutput($"\n{Time()} Tx start: toCall:'{toCall}' lastToCall:'{lastToCall}' timer2 interval:{ctrl.timer2.Interval} msec");
            DebugOutputStatus();

            //check for time to log early
            //  option enabled                   correct cur and prev    just sent RRR                and previously sent +XX
            if (ctrl.logEarlyCheckBox.Checked && !qsoLogged && toCall == lastToCall && WsjtxMessage.IsRogers(txMsg) && WsjtxMessage.IsReport(lastTxMsg))
            {
                LogQso(toCall);
                DebugOutput($"{spacer}early logging reqd: toCall:'{toCall}' qsoLogged:{qsoLogged}");
            }

            //check for QSO complete, normal logging
            // correct cur and prev     prev Tx was a RRR                 or prev Tx was a R+XX                    or prev Tx was a +XX                 and cur Tx was 73
            if (toCall == lastToCall && (WsjtxMessage.IsRogers(lastTxMsg) || WsjtxMessage.IsRogerReport(lastTxMsg) || WsjtxMessage.IsReport(lastTxMsg)) && WsjtxMessage.Is73orRR73(txMsg))
            {
                DebugOutput($"{spacer}is 73, was RRR/R+XX, qsoLogged:{qsoLogged}");
                if (!qsoLogged)
                {
                    LogQso(toCall);
                    DebugOutput($"{spacer}normal logging reqd: toCall:'{toCall}' qsoLogged:{qsoLogged}");
                }
            }
            DebugOutput($"{Time()} Tx start done: txMsg:'{txMsg}' lastTxMsg:'{lastTxMsg}' toCall:'{toCall}' lastToCall:'{lastToCall}'\n           qsoLogged:{qsoLogged}\n");
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
                callInProg = null;
                DebugOutput($"{spacer}Possible CQ button, callInProg:'{callInProg}' autoCalling:{autoCalling}");
            }
            else
            {
                callInProg = toCall;
                DebugOutput($"{spacer}callInProg:'{callInProg}'");
            }

            //save all calls a report msg was sent to
            if ((WsjtxMessage.IsReport(txMsg) || WsjtxMessage.IsRogerReport(txMsg)) && !reportList.Contains(toCall)) reportList.Add(toCall);

            //check for time to log early; NOTE: doing this at Tx end because WSJT-X may have changed Tx msgs (between Tx start and Tx end) due to late decode for the current call
            //  option enabled                   correct cur and prev    just sent RRR                and previously sent +XX
            if (ctrl.logEarlyCheckBox.Checked && !qsoLogged && toCall == lastToCall && WsjtxMessage.IsRogers(txMsg) && WsjtxMessage.IsReport(lastTxMsg))
            {
                LogQso(toCall);
                DebugOutput($"{spacer}early logging reqd: toCall:'{toCall}' qsoLogged:{qsoLogged}");
            }
            //check for QSO complete, trigger next call in the queue
            // correct cur and prev     prev Tx was a RRR                 or prev Tx was a R+XX                    or prev Tx was a +XX                 and cur Tx was 73
            if (toCall == lastToCall && (WsjtxMessage.IsRogers(lastTxMsg) || WsjtxMessage.IsRogerReport(lastTxMsg) || WsjtxMessage.IsReport(lastTxMsg)) && WsjtxMessage.Is73orRR73(txMsg))
            {
                txTimeout = true;      //timeout to Tx the next call in the queue
                xmitCycleCount = 0;
                autoCalling = true;
                callInProg = null;
                DebugOutput($"{Time()} Reset(2): (is 73, was RRR/R+XX, have queue entry) xmitCycleCount:{xmitCycleCount} txTimeout:{txTimeout} qsoLogged:{qsoLogged}\n           autoCalling:{autoCalling} callInProg:'{callInProg}'");
                //NOTE: doing this at Tx end because WSJT-X may have changed Tx msgs (between Tx start and Tx end) due to late decode for the current call
                if (!qsoLogged)
                {
                    LogQso(toCall);
                    DebugOutput($"{spacer}normal logging reqd: toCall:'{toCall}' qsoLogged:{qsoLogged}");
                }
            }

            //count tx cycles: check for changed Tx call in WSJT-X
            if (lastTxMsg != txMsg)
            {
                lastTxMsg = txMsg;                  //don't interfere with logging check
                if (xmitCycleCount >= 0)
                {
                    //check  for "to" call changed since last xmit end
                    if (toCall != lastToCall && callQueue.Contains(toCall))
                    {
                        RemoveCall(toCall);         //manually switched to Txing a call that was also in the queue
                    }
                    xmitCycleCount = 0;
                    DebugOutput($"{Time()} Reset(1) (different msg) xmitCycleCount:{xmitCycleCount} txMsg:'{txMsg}' lastTxMsg:'{lastTxMsg}'");
                }
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
                        txTimeout = true;
                        autoCalling = true;
                        callInProg = null;
                        tCall = WsjtxMessage.ToCall(lastTxMsg);        //call to remove from queue, will be null if non-std msg
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

            //check for time to process new directed CQ or WSJT-X setting changed in UI
            if (toCall == "CQ" && (settingChanged || (ctrl.directedCheckBox.Checked && ctrl.directedTextBox.Text.Trim().Length > 0)))
            {
                xmitCycleCount = 0;
                newDirCq = true;
                if (settingChanged)
                {
                    ctrl.WsjtxSettingConfirmed();
                    settingChanged = false;
                }
                DebugOutput($"{Time()} Reset(5) (new directed CQ, or setting changed) xmitCycleCount:{xmitCycleCount} newDirCq:{newDirCq}");
            }

            DebugOutputStatus();
            DebugOutput($"{Time()} Tx end done\n");
            ShowTimeout();
            UpdateDebug();      //unconditional
        }

        private void LogQso(string toCall)
        {
            emsg.NewTxMsgIdx = 5;           //force logging, no QSO phase change
            emsg.GenMsg = $"{toCall} {myCall} 73";
            emsg.SkipGrid = ctrl.skipGridCheckBox.Checked;
            emsg.UseRR73 = ctrl.useRR73CheckBox.Checked;
            emsg.CmdCheck = cmdCheck;
            ba = emsg.GetBytes();
            udpClient2.Send(ba, ba.Length);
            DebugOutput($"{Time()} >>>>>Sent 'Log Req' cmd:5 cmdCheck:{cmdCheck}\n{emsg}");
            qsoLogged = true;
        }

        private bool IsEvenPeriod(int msec)     //milliseconds past start of the current minute
        {
            if (mode == "FT4")
            {
                return !(msec == 07000 || msec == 22000 || msec == 37000 || msec == 52000);
            }
            else
            {
                return (msec / trPeriod) % 2 == 0;
            }
        }

        private string NextDirCq()
        {
            string dirCq = "";
            if (ctrl.directedCheckBox.Checked && ctrl.directedTextBox.Text.Trim().Length > 0)
            {
                string[] dirWords = ctrl.directedTextBox.Text.Trim().ToUpper().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                string s = dirWords[rnd.Next() % dirWords.Length];
                if (s != "*" && s.Length <= 3) dirCq = " " + s;          //is directed
                DebugOutput($"{Time()} dirCq:{dirCq}");
            }
            return dirCq;
        }

        private void ResetNego()
        {
            ResetOpMode();
            WsjtxMessage.Reinit();                      //NegoState = INITIAL;
            DebugOutput($"\n\n{Time()} opMode:{opMode} NegoState:{WsjtxMessage.NegoState}");
            DebugOutput($"{Time()} Waiting for heartbeat...");
            cmdCheck = RandomCheckString();
            commConfirmed = false;
            ShowStatus();
            UpdateDebug();
        }

        private void ResetOpMode()
        {
            opMode = OpModes.IDLE;
            ShowStatus();
            autoCalling = true;
            callInProg = null;
            qsoLogged = false;
            txTimeout = false;
            replyCmd = null;
            curCmd = null;
            replyDecode = null;
            qCall = null;
            tCall = null;
            newDirCq = false;
            dxCall = null;
            xmitCycleCount = 0;
            logList.Clear();        //can re-log on new mode
            ShowLogged();
            ClearCalls(true);
            UpdateDebug();
            UpdateAddCall();
            ShowStatus();
            DebugOutput($"\n\n{Time()} opMode:{opMode} NegoState:{WsjtxMessage.NegoState}");
        }

        private void ClearCalls(bool inclCqCalls)
        {
            callQueue.Clear();
            callDict.Clear();
            if (inclCqCalls) cqCallDict.Clear();
            ShowQueue();
            allCallDict.Clear();
            reportList.Clear();
            xmitCycleCount = 0;
            ShowTimeout();
            ctrl.timer2.Stop();
        }

        private void UpdateAddCall()
        {
            ctrl.addCallLabel.Visible = (advanced && opMode == OpModes.ACTIVE);
        }

        private bool UpdateCall(string call, DecodeMessage msg)
        {
            if (callDict.ContainsKey(call))
            {
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
            if (callDict.TryGetValue(call, out msg))     //dictionary contains call data for this call sign
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
            DebugOutput($"{Time()} Not removed {call}: {CallQueueString()} {CallDictString()}");
            return false;
        }

        //add call/decode to queue/dict;
        //priority decodes (to myCall) move toward the head of the queue
        //because non-priority calls are added manually to queue (i.e., not rec'd, prospective for QSO)
        //but priority calls are decoded calls to myCall (i.e., rec'd and immediately ready for QSO);
        //return false if already added
        private bool AddCall(string call, DecodeMessage msg)
        {
            tCall = null;               //prevent call removal from queue at timeout
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

        private string Time()
        {
            var dt = DateTime.UtcNow;
            return dt.ToString("HHmmss.fff");
        }

        public void Closing()
        {
            try
            {
                if (udpClient2 != null)
                {
                    //notify WSJT-X
                    emsg.NewTxMsgIdx = 0;           //de-init WSJT-X
                    emsg.GenMsg = $"";
                    emsg.SkipGrid = ctrl.skipGridCheckBox.Checked;
                    emsg.UseRR73 = ctrl.useRR73CheckBox.Checked;
                    emsg.CmdCheck = cmdCheck;
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

            if (sw != null)
            {
                DebugOutput($"{DateTime.UtcNow.ToString("yyyy-MM-dd HHmmss")} UTC ###################### Program ending....\n\n");
                sw.Flush();
                sw.Close();
                sw = null;
            }

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
               (uint)(SoundFlags.SND_NOSTOP | SoundFlags.SND_ASYNC));
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
            Color color = Color.Red;
            ctrl.statusText.ForeColor = Color.White;

            if (WsjtxMessage.NegoState == WsjtxMessage.NegoStates.FAIL || !modeSupported)
            {
                ctrl.statusText.Text = failReason;
                ctrl.statusText.BackColor = Color.Yellow;
                ctrl.statusText.ForeColor = Color.Black;
                return;
            }

            if (WsjtxMessage.NegoState == WsjtxMessage.NegoStates.INITIAL)
            {
                status = "Waiting for WSJT-X...";
            }
            else
            {
                switch ((int)opMode)
                {
                    case (int)OpModes.START:            //fall thru
                    case (int)OpModes.IDLE:
                        status = "Connecting: Select 'Monitor', wait until ready";
                        break;
                    case (int)OpModes.ACTIVE:
                        status = txEnabled ? (autoCalling ? "Automatic calling enabled" : "Automatic calling paused...") : "To start: Select 'Enable Tx'";
                        color = Color.Green;
                        break;
                }
            }
            ctrl.statusText.BackColor = color;
            ctrl.statusText.Text = status;
        }

        private void ShowLogged()
        {
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
        /*
        private void wdTimer_Tick(object sender, EventArgs e)
        {
            wdTimer.Stop();
            //DebugOutput($"{Time()} wdTimer ticked");
        }
        */

        //process a manually- or automatically-generated request to add a decode to call reply queue
        public void AddSelectedCall(EnqueueDecodeMessage emsg)
        {
            string msg = emsg.Message;
            string deCall = WsjtxMessage.DeCall(msg);
            string toCall = WsjtxMessage.ToCall(msg);
            string directedTo = WsjtxMessage.DirectedTo(msg);

            //auto-generated notification of a CQ;
            //IsDx only validw for auto-generated "CQ DX" case, 
            //not for other auto-generated or any manually-selected message
            if (emsg.AutoGen) DebugOutput($"{Time()} AddSelectedCall, AutoGen:{emsg.AutoGen} deCall:'{deCall}' IsDx:{emsg.IsDx}");

            if (emsg.AutoGen)       //automatically-generated queue request
            {
                if (advanced && ctrl.replyCqCheckBox.Checked)
                {
                    DebugOutput($"{spacer}callInProg:{callInProg} callQueue.Count:{callQueue.Count} callQueue.Contains:{callQueue.Contains(deCall)}");
                    if (myCall == null || opMode != OpModes.ACTIVE
                        || IsEvenPeriod(emsg.SinceMidnight.Seconds * 1000) == txFirst
                        || msg.Contains("...")) return;

                    if (deCall == null || callQueue.Contains(deCall) || (txEnabled && deCall == callInProg)) return;

                    bool wantedDirected = false;
                    if (directedTo != null)
                    {
                        wantedDirected = ctrl.alertCheckBox.Checked && ctrl.alertTextBox.Text.ToUpper().Contains(directedTo);
                        DebugOutput($"{spacer}directedTo:{directedTo} wantedDirected:{wantedDirected}");
                        if (!((directedTo == "DX" && emsg.IsDx) || wantedDirected)) return;
                    }

                    if (callQueue.Count < maxAutoGenEnqueue || wantedDirected)
                    {
                        int prevCqs = 0;
                        if (!cqCallDict.TryGetValue(deCall, out prevCqs) || prevCqs < maxPrevCqs)
                        {
                            if (wantedDirected) emsg.Priority = true;   //since know to be wanted, give same priority as an actual reply
                            AddCall(deCall, emsg);              //add to call queue

                            if (prevCqs > 0)                   //track how many times Controlller replied to CQ from this call sign
                            {
                                cqCallDict.Remove(deCall);
                            }
                            cqCallDict.Add(deCall, prevCqs + 1);
                            Play("blip.wav");
                        }
                    }
                }
                return;
            }

            //manually-selected queue request
            if (myCall == null || opMode != OpModes.ACTIVE)
            {
                ctrl.ShowMsg("Not ready to add calls yet", true);
                return;
            }

            if (!emsg.Modifier && IsEvenPeriod(emsg.SinceMidnight.Seconds * 1000) == txFirst)
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

                if (txEnabled)
                {
                    txTimeout = true;                   //immediate switch to other tx period
                    autoCalling = true;
                    callInProg = null;
                    DebugOutput($"{spacer}txTimeout:{txTimeout} tCall:'{tCall}' autoCalling:{autoCalling} callInProg:'{callInProg}'");
                    CheckNextXmit();
                }
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
                //get actual message to reply to
                AddCall(deCall, emsg);              //add to call queue

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

                ctrl.label9.Text = $"opMode: {opMode}";

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


                ctrl.label14.Text = $"qso: {qsoState}";
                lastQsoStateDebug = qsoState;

                ctrl.label15.Text = $"log call: {qCall}";

                if (txTimeout != lastTxTimeout)
                {
                    ctrl.label10.ForeColor = Color.Red;
                    chg = true;
                }
                ctrl.label10.Text = $"t/o: {txTimeout.ToString().Substring(0, 1)}";
                lastTxTimeout = txTimeout;

                ctrl.label11.Text = $"txFirst: {txFirst.ToString().Substring(0, 1)}";
                ctrl.label16.Text = $"Nego:{WsjtxMessage.NegoState}";

                if (txMsg != lastTxMsg && !WsjtxMessage.IsCQ(txMsg) && !WsjtxMessage.IsCQ(lastTxMsg))
                {
                    ctrl.label19.ForeColor = Color.Red;
                    chg = true;
                }
                ctrl.label19.Text = $"tx:  {txMsg}";

                ctrl.label18.Text = $"last: {lastTxMsg}";

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
                ctrl.label3.Text = $"rpts: {allCallDict.Count}/{i}/{reportList.Count}";
                ctrl.label21.Text = $"replyCmd: {replyCmd}";

                //if (curCmd != lastCurCmd) ctrl.label17.ForeColor = Color.Red;
                ctrl.label17.Text = $"curCmd: {curCmd}";
                lastCurCmd = curCmd;

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
            ResetOpMode();

            emsg.NewTxMsgIdx = 7;
            emsg.GenMsg = $"";          //no effect
            emsg.SkipGrid = false;      //no effect
            emsg.UseRR73 = false;      //no effect
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

        public string ConfigsCheckedString()
        {
            string delim = "";
            StringBuilder sb = new StringBuilder();

            foreach (string s in configList)
            {
                sb.Append(delim);
                sb.Append(s);
                delim = " ";
            }

            return sb.ToString();
        }

        public void ConfigsCheckedFromString(string s)
        {
            string[] arr = s.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string elem in arr)
            {
                configList.Add(elem);
            }

        }

        public void WsjtxSettingChanged()
        {
            settingChanged = true;
        }

        private string RandomCheckString()
        {
            string s = rnd.Next().ToString();
            if (s.Length > 8) s = s.Substring(0, 8);
            return s;
        }

        private void DebugOutput(string s)
        {
            if (logToFile)
            {
                if (sw != null) sw.WriteLine(s);
            }

            if (debug)
            {
                Console.WriteLine(s);
            }
        }

        //during decoding, check for late signof (73 or RR73) 
        //from a call sign that isn't (or won't be) the call in progress;
        //if reports have bee exchanged, log the QSO;
        //logging is done directly via log file, not via WSJT-X
        private void CheckLateLog(string call, DecodeMessage msg)
        {
            DebugOutput($"{Time()} CheckLateLog: call'{call}' callInProg:'{callInProg}' msg:{msg.Message} Is73orRR73:{WsjtxMessage.Is73orRR73(msg.Message)}");
            if (call == null || call == callInProg || !WsjtxMessage.Is73orRR73(msg.Message)) return;
            DebugOutput($"{spacer}call is not in progress and is RRR73 or 73");
            List<DecodeMessage> msgList;
            if (!allCallDict.TryGetValue(call, out msgList)) return;          //no previous call(s) from DX station
            DebugOutput($"{spacer}recd previous call(s)");
            DecodeMessage rMsg;
            if ((rMsg = msgList.Find(RogerReport)) == null && (rMsg = msgList.Find(Report)) == null) return;        //the DX station never reported a signal
            DebugOutput($"{spacer}recd previous report(s)");
            if (!reportList.Contains(call)) return;         //never reported SNR to the DX station
            DebugOutput($"{spacer}sent previous report(s)");
            LogDirect(call, rMsg, msg);              //process a "late" QSO completion
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

        //log a QSO directly to the WSJT-X .ADI log file and to WSJT-X to re-broadcast
        private void LogDirect(string call, DecodeMessage reptMsg, DecodeMessage lateMsg)
        {
            if (!File.Exists(WsjtxLogPathFilename)) return;
            if (debug) Play("dive.wav");

            //<call:4>W1AW <gridsquare:4>EM77 <mode:3>FT8 <rst_sent:3>-10 <rst_rcvd:3>+01 <qso_date:8>20201226 
            //<time_on:6>042215 <qso_date_off:8>20201226 <time_off:6>042300 <band:3>40m <freq:8>7.076439 
            //<station_callsign:4>WM8Q <my_gridsquare:6>DN61OK <eor>

            string rstSent = $"{reptMsg.Snr:+#;-#;+00}";
            string rstRecd = WsjtxMessage.RstRecd(reptMsg.Message);
            string qsoDateOn = reptMsg.RxDate.ToString("yyyyMMdd");
            string qsoDateOff = lateMsg.RxDate.ToString("yyyyMMdd");
            string qsoTimeOn = reptMsg.SinceMidnight.ToString("hhmmss");      //one of the report decodes
            string qsoTimeOff = lateMsg.SinceMidnight.ToString("hhmmss");
            string qsoMode = mode;
            string freq = (dialFrequency + txOffset / 1e6).ToString("F6");
            string band = FreqToBand(dialFrequency / 1e6);

            string adifRecord = $"<call:{call.Length}>{call}  <gridsquare:0> <mode:{mode.Length}>{mode} <rst_sent:{rstSent.Length}>{rstSent} <rst_rcvd:{rstRecd.Length}>{rstRecd} <qso_date:{qsoDateOn.Length}>{qsoDateOn} <time_on:{qsoTimeOn.Length}>{qsoTimeOn} <qso_date_off:{qsoDateOff.Length}>{qsoDateOff} <time_off:{qsoTimeOff.Length}>{qsoTimeOff} <band:{band.Length}>{band} <freq:{freq.Length}>{freq} <station_callsign:{myCall.Length}>{myCall} <my_gridsquare:{myGrid.Length}>{myGrid} <eor>";

            //send ADIF record to WSJT-X for re-broadcast to logging pgms
            LogToSecondaryUdp(adifRecord);

            //send ADIF record to the WSJT-X log file
            int retry = 3;          //file could be in use/locked
            while (true)
            {
                try
                {
                    StreamWriter lsw = File.AppendText(WsjtxLogPathFilename);
                    lsw.WriteLine(adifRecord);
                    lsw.Close();
                    break;
                }
                catch (Exception err)
                {
                    DebugOutput($"{Time()} ERROR: LogDirect {err}");
                    Console.Beep();

                    if (--retry == 0)
                    {
                        DebugOutput($"{Time()} ERROR: LogDirect failed, call'{call}'");
                        Console.Beep();
                        return;
                    }
                    Thread.Sleep(100);
                }
            }

            if (ctrl.loggedCheckBox.Checked) Play("echo.wav");
            ctrl.ShowMsg($"Logging late QSO with {call}", false);
            logList.Add(call);      //even if already logged this mode/band
            UpdateCqCall(call);     //no more CQ responses to this call
            ShowLogged();
            DebugOutput($"{Time()} QSO logged late: call'{call}'");
            RemoveAllCall(call);
            UpdateDebug();
        }

        private string FreqToBand(double freq)
        {
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

            if (allCallDict.ContainsKey(call))
            {
                allCallDict.Remove(call);
                DebugOutput($"{spacer} removed '{call}' from allCallDict");
            }

            if (reportList.Contains(call))
            {
                reportList.Remove(call);
                DebugOutput($"{spacer} removed '{call}' from reportList");
            }
        }

        private string CurrentStatus()
        {
            return $"myCall:'{myCall}' callInProg:'{callInProg}' qsoState:{qsoState} lastQsoState:{lastQsoState} txMsg:'{txMsg}'\n           lastTxMsg:'{lastTxMsg}' replyCmd:'{replyCmd}' curCmd:'{curCmd}'\n           txTimeout:{txTimeout} xmitCycleCount:{xmitCycleCount} transmitting:{transmitting} mode:{mode} txEnabled:{txEnabled} autoCalling:{autoCalling}\n           txFirst:{txFirst} dxCall:'{dxCall}' trPeriod:{trPeriod} dblClk:{dblClk}\n           newDirCq:{newDirCq} tCall:'{tCall}'  qCall:'{qCall}'  qsoLogged:{qsoLogged}  decoding:{decoding}\n           {CallQueueString()}";
        }

        private void DebugOutputStatus()
        {
            DebugOutput($"Now:       {CurrentStatus()}");
        }

        //detect supported mode
        private void CheckModeSupported()
        {
            string s = "";
            modeSupported = supportedModes.Contains(mode) && (mode != "FT8" || specOp == 0);
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

        //send ADIF record to WSJT-X for re-broadcast to logging pgms
        private void LogToSecondaryUdp(string logLine)
        {
            emsg.NewTxMsgIdx = 255;     //function code
            emsg.GenMsg = logLine;
            emsg.SkipGrid = false;      //no effect
            emsg.UseRR73 = false;      //no effect
            emsg.CmdCheck = cmdCheck;
            ba = emsg.GetBytes();
            udpClient2.Send(ba, ba.Length);
            DebugOutput($"{Time()} >>>>>Sent 'Broadcast' cmd:255 cmdCheck:{cmdCheck}\n{emsg}");
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
    }
}
