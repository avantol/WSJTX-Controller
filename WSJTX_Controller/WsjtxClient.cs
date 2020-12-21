//NOTE CAREFULLY: Several message classes require the use of a slightly modified WSJT-X program.
//Further information is in the README file.

using WsjtxUdpLib.Messages;
using WsjtxUdpLib.Messages.Out;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
//using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

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

        private List<string> acceptableWsjtxVersions = new List<string> { "2.2.2/215", "2.3.0-rc2/100", "2.3.0-rc2/101", "2.3.0-rc2/102", "2.3.0-rc2/103", "2.3.0-rc2/104", "2.3.0-rc2/105" };
        private List<string> supportedModes = new List<string>() { "FT8", "FT4", "FST4" };

        private bool logToFile = false;
        private StreamWriter sw;
        private bool suspendComm = false;
        private bool settingChanged = false;
        private string cmdCheck = "";
        private bool commConfirmed = false;
        private string myCall = null, myGrid = null;
        private Dictionary<string, DecodeMessage> callDict = new Dictionary<string, DecodeMessage>();
        private Queue<string> callQueue = new Queue<string>();
        private List<string> altCallList = new List<string>();
        private Dictionary<string, DecodeMessage> altCallDict = new Dictionary<string, DecodeMessage>();
        private List<string> reportList = new List<string>();
        private List<string> rogerReportList = new List<string>();
        private List<string> configList = new List<string>();
        private bool txEnabled = false;
        private bool transmitting = false;
        private bool decoding = false;
        private bool qsoLogged = false;
        private WsjtxMessage.QsoStates qsoState = WsjtxMessage.QsoStates.CALLING;
        private string deCall = "";
        private string mode = "";
        private bool modeSupported = true;
        private bool? lastModeSupported = null;
        private string rawMode = "";
        private bool txFirst = false;
        private bool cQonly = false;
        private int? trPeriod = null;       //msec
        private string replyCmd = null;     //no "reply to" cmd sent to WSJT-X yet
        private DecodeMessage replyDecode = null;
        private string configuration = null;

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

        //private string lastDxCall = null;
        private string lastStatus = null;
        private int xmitCycleCount = 0;
        private bool txTimeout = false;
        private int specOp = 0;
        private string qCall = null;            //call sign for last QSO logged
        private string tCall = null;            //call sign being processed at timeout
        private string txMsg = null;            //msg for the most-recent Tx
        private bool decodedMsgReplied = false;  //no decoded msgs replied yet in current decoding phase
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
        TimeSpan latestDecodeTime;
        DateTime firstDecodeTime;

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
            //opMode = OpModes.IDLE;
            //WsjtxMessage.NegoState = WsjtxMessage.NegoStates.INITIAL;
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
                    DebugOutput($"\n\n{DateTime.UtcNow.ToString("yyyy-MM-dd HHmmss")} UTC ###################### Program starting....");
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
                MessageBox.Show($"Unable to connect with WSJT-X using the provided IP address ({ipAddress}) and port ({port}).\n\nEnter a different IP address/port in the dialog that follows.", pgmName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                ctrl.wsjtxClient = this;
                ctrl.setupButton_Click(null, null);
                return;
            }

            asyncCallback = new AsyncCallback(ReceiveCallback);
            s = new UdpState();
            s.e = endPoint;
            s.u = udpClient;

            DebugOutput($"{Time()} NegoState:{WsjtxMessage.NegoState}");
            DebugOutput($"{Time()} opMode:{opMode}");

            DebugOutput($"{Time()} Waiting for heartbeat...");

            ShowStatus();
            ShowQueue();
            ShowLogged();
            UpdateAltCallListStatus();
            messageRecd = false;
            recvStarted = false;

            ctrl.altListBox.DataSource = altCallList;

            string cast = multicast ? "(multicast)" : "(unicast)";
            ctrl.verLabel.Text = $"by WM8Q v{fileVer}";
            ctrl.verLabel2.Text = $"More features? more.avantol@xoxy.net";

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
                //DebugOutput($"{Time()} msg:{msg}");            //tempOnly
            }
            catch (ParseFailureException ex)
            {
                File.WriteAllBytes($"{ex.MessageType}.couldnotparse.bin", ex.Datagram);
                DebugOutput($"{Time()} Parse failure for {ex.MessageType}: {ex.InnerException.Message}");
                errorDesc = "Parse fail {ex.MessageType}";
                UpdateDebug();
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
                    DebugOutput($"           *NegoState:{WsjtxMessage.NegoState}");
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
                DebugOutput($"           *NegoState:{WsjtxMessage.NegoState}");
                DebugOutput($"           *negotiated schema version:{WsjtxMessage.NegotiatedSchemaVersion}");
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
                DebugOutput($"           *check cmd timer started");
            }

            //while in INIT or SENT state:
            //get minimal info from StatusMessage needed for faster startup
            //and for special case of ack msg returned by WSJT-X after req for StatusMessage
            //check for no call sign or grid, exit if so
            if (WsjtxMessage.NegoState != WsjtxMessage.NegoStates.RECD && msg.GetType().Name == "StatusMessage")
            {
                StatusMessage smsg = (StatusMessage)msg;
                //DebugOutput($"\n{Time()}\n{smsg}");
                mode = smsg.Mode;
                specOp = (int)smsg.SpecialOperationMode;
                configuration = smsg.ConfigurationName.Trim(). Replace(' ', '-');
                if (!CheckMyCall(smsg)) return;
                DebugOutput($"{Time()}\nStatus     myCall:{myCall} myGrid:{myGrid} mode:{mode} specOp:{specOp} configuration:{configuration} check:{smsg.Check}");
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
                        rawMode = dmsg.Mode;    //different from mode string in status msg
                        if (dmsg.New)           //important to reject replays requested by other pgms
                        { 
                            if (dmsg.IsCallTo(myCall)) DebugOutput($"{dmsg}\n           *msg:'{dmsg.Message}'");

                            deCall = dmsg.DeCall();
                            //do some processing not directly related to replying immediately
                            if (deCall != null)
                            {
                                //add the decodes having the opposite time from the Tx 1st checkbox in WSJT-X
                                if (((dmsg.SinceMidnight.Seconds % (trPeriod / 500)) == 0) != txFirst) AddAltCall(dmsg);

                                //check for directed CQ
                                if (ctrl.alertCheckBox.Checked)
                                {
                                    string d = WsjtxMessage.DirectedTo(dmsg.Message);
                                    if (d != null && ctrl.alertTextBox.Text.ToUpper().Contains(d))
                                        Play("beepbeep.wav");   //directed CQ
                                }

                                //check for decode being a call to myCall
                                if (deCall != qCall && myCall != null && dmsg.IsCallTo(myCall) && ctrl.mycallCheckBox.Checked) Play("trumpet.wav");   //not the call just logged
                            }

                            //decode processing of calls to myCall requires txEnabled
                            if (txEnabled && deCall != null && myCall != null && dmsg.IsCallTo(myCall))
                            {
                                DebugOutput($"           *'{deCall}' is to {myCall}");  //tempOnly
                                // not xmitting yet    
                                if (/*!transmitting*/ true)      //todo: can process decodes after Tx starts?
                                {
                                    if (!dmsg.Is73orRR73())       //not a 73 or RR73
                                    {
                                        DebugOutput($"           *Not a 73 or RR73");  //tempOnly
                                        if (!callQueue.Contains(deCall))        //call not in queue, possibly enqueue the call data
                                        {
                                            DebugOutput($"           *'{deCall}' not already in queue");  //tempOnly
                                            if (qsoState == WsjtxMessage.QsoStates.CALLING && !decodedMsgReplied)
                                            {
                                                DebugOutput($"           *CALLING, no decode replied yet this cycle, reply now txTimeout:{txTimeout}");  //tempOnly
                                                //WSJT-X CQing, is ready to process this call
                                                //set WSJT-X call enable with Reply message
                                                txTimeout = false;   //cancel any pending timeout (like for directed CQ or WSJT-X setting changed)
                                                var rmsg = new ReplyMessage();
                                                rmsg.SchemaVersion = WsjtxMessage.NegotiatedSchemaVersion;
                                                rmsg.Id = WsjtxMessage.UniqueId;
                                                rmsg.SinceMidnight = dmsg.SinceMidnight;
                                                rmsg.Snr = dmsg.Snr;
                                                rmsg.DeltaTime = dmsg.DeltaTime;
                                                rmsg.DeltaFrequency = dmsg.DeltaFrequency;
                                                rmsg.Mode = dmsg.Mode;
                                                rmsg.Message = dmsg.Message;
                                                rmsg.LowConfidence = dmsg.LowConfidence;
                                                ba = rmsg.GetBytes();
                                                udpClient2.Send(ba, ba.Length);
                                                replyCmd = dmsg.Message;        //save the last reply cmd to determine which call is in progress
                                                replyDecode = dmsg;             //save the decode the reply cmd came from
                                                DebugOutput($"{Time()} >>>>>Sent 'Reply To Msg', txTimeout:{txTimeout} cmd:\n{rmsg}\nreplyCmd:'{replyCmd}'");
                                                decodedMsgReplied = true;       //no more replies during rest of pass(es) in current decoding phases
                                           }
                                            else   //not CALLING or a decode already replied to this cycle
                                            {
                                                DebugOutput($"           *not CALLING (is {qsoState}) or a decode already replied to this cycle {decodedMsgReplied}");     //tempOnly
                                                DebugOutput($"           *last Tx 'to':({WsjtxMessage.ToCall(txMsg)}), last cmd 'from':({WsjtxMessage.DeCall(replyCmd)})");     //tempOnly
                                                if (deCall == CallInProgress())                //call currently being processed by WSJT-X
                                                {
                                                    DebugOutput($"           *'{deCall}' currently being processed by WSJT-X:");    //tempOnly
                                                }
                                                else
                                                {
                                                    DebugOutput($"           *'{deCall}' not currently being processed by WSJT-X");     //tempOnly
                                                    AddCall(deCall, dmsg);          //known to not be in queue
                                                }
                                            }
                                        }
                                        else       //call is already in queue, possibly update the call data
                                        {
                                            DebugOutput($"           *'{deCall}' already in queue");     //tempOnly
                                            if (deCall == CallInProgress())                //call currently being processed by WSJT-X
                                            {
                                                DebugOutput($"           *'{deCall}' currently being processed by WSJT-X");     //tempOnly
                                                RemoveCall(deCall);             //may have been queued previously
                                            }
                                            else        //update the call in queue
                                            {
                                                DebugOutput($"           *'{deCall}' not currently being processed by WSJT-X, update queue");     //tempOnly
                                                UpdateCall(deCall, dmsg);
                                            }
                                        }
                                    }
                                    else        //decode is 73 or RR73 msg
                                    {
                                        DebugOutput($"           *decode is 73 or RR73 msg, tCall:{tCall}, cur Tx to:'{WsjtxMessage.DeCall(txMsg)}' cur Tx IsRogers:{WsjtxMessage.IsRogers(txMsg)}");     //tempOnly
                                        /* to-do: need to set up WSJT-X with valid QSO data first
                                        //check for last-chance logging (73 after current QSO just ended)
                                        //  was not logged early at RRR sent  same call timed out  prev Tx msg was to myCall           prev Tx msg was a RRR
                                        if (!ctrl.logEarlyCheckBox.Checked && tCall == deCall && WsjtxMessage.ToCall(lastTxMsg) == deCall && WsjtxMessage.IsRogers(lastTxMsg))       //this decode is a late 73, log it
                                        {
                                            DebugOutput($"           *{deCall} timed out, prev Tx msg was to {myCall}, current Tx msg was a RRR");     //tempOnly
                                            DebugOutput($"{Time()} Logging late 73 msg");
                                            LogQso(deCall);
                                        }
                                        */
                                    }
                                }
                                //else
                                //{
                                //    DebugOutput($"           *discarded decode, already transmitting");
                                //}
                            }
                            UpdateDebug();
                        }
                        return;
                    }

                    //****************
                    //QsoLoggedMessage
                    //****************
                    if (txEnabled && msg.GetType().Name == "QsoLoggedMessage")
                    {
                        //ack'ing either early or normal logging
                        //WSJT-X's auto-logging is disabled
                        QsoLoggedMessage lmsg = (QsoLoggedMessage)msg;
                        DebugOutput($"{lmsg}");         //tempOnly
                        qCall = lmsg.DxCall;
                        if (ctrl.loggedCheckBox.Checked) Play("echo.wav");
                        logList.Add(qCall);    //even if already logged this mode/band
                        ShowLogged();
                        DebugOutput($"{Time()} QSO logging ackd: qCall{qCall}");
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
                    //DebugOutput($"\n{Time()}\n{smsg}");                        
                    qsoState = smsg.CurQsoState();
                    txEnabled = smsg.TxEnabled;
                    dxCall = smsg.DxCall;                               //unreliable info, can be edited manually
                    mode = smsg.Mode;
                    specOp = (int)smsg.SpecialOperationMode;
                    txMsg = smsg.LastTxMsg;        //msg from last Tx
                    txFirst = smsg.TxFirst;
                    cQonly = smsg.CqOnly;
                    decoding = smsg.Decoding;
                    transmitting = smsg.Transmitting;

                    if (lastXmitting == null) lastXmitting = transmitting;     //initialize
                    if (lastQsoState == WsjtxMessage.QsoStates.INVALID) lastQsoState = qsoState;    //initialize WSJT-X user QSO state change detection
                    //if (lastDxCall == null) lastDxCall = dxCall;    //initialize WSJT-X user potential "to" call
                    if (lastTxEnabled == null) lastTxEnabled = txEnabled;     //initializlastGenMsge
                    if (lastDecoding == null) lastDecoding = decoding;     //initialize
                    if (lastTxWatchdog == null) lastTxWatchdog = smsg.TxWatchdog;   //initialize
                    if (lastMode == null) lastMode = mode;                     //initialize
                    if (lastTxFirst == null) lastTxFirst = txFirst;                     //initialize
                    if (lastDialFrequency == null) lastDialFrequency = smsg.DialFrequency; //initialize
                    if (lastSpecOp == null) lastSpecOp = (int)smsg.SpecialOperationMode; //initialize
                    if (lastTxMsg == null) lastTxMsg = txMsg;   //initialize
                    if (smsg.TRPeriod != null) trPeriod = (int)smsg.TRPeriod;

                    //minimize console clutter
                    string curStatus = $"Status     myCall:{myCall} myGrid:{myGrid} qsoState:{qsoState} txMsg:'{txMsg}' replyCmd:'{replyCmd}' \n           txTimeout:{txTimeout} Transmitting:{transmitting} Mode:{mode} txEnabled:{txEnabled}\n           txFirst:{txFirst} dxCall:{dxCall} trPeriod:{trPeriod} check:{smsg.Check}";
                    if (curStatus != lastStatus)
                    {
                        DebugOutput($"{Time()}\n{curStatus}");
                        lastStatus = curStatus;
                    }

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
                            decodedMsgReplied = false;          //decode passes finished
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

                    //detect WSJT-X mode change
                    if (mode != lastMode)
                    {
                        DebugOutput($"{Time()} mode:{mode} (was {lastMode})");

                        ResetOpMode();
                        DebugOutput($"{Time()} opMode:{opMode}, waiting for mode status...");
                        lastMode = mode;
                    }

                    //detect WSJT-X special operating mode change
                    if (specOp != lastSpecOp)
                    {
                        DebugOutput($"{Time()} specOp:{specOp} (was {lastSpecOp})");

                        ResetOpMode();
                        DebugOutput($"{Time()} opMode:{opMode}, waiting for mode status...");
                        lastSpecOp = specOp;
                    }

                    //detect supported mode change
                    modeSupported = supportedModes.Contains(mode) && (mode != "FT8" || specOp == 0);
                    if (modeSupported != lastModeSupported)
                    {
                        if (!modeSupported) failReason = $"{mode} mode not supported";
                        lastModeSupported = modeSupported;
                        ShowStatus();
                    }

                    //check for time to flag starting first xmit
                    if (supportedModes.Contains(mode) && (mode != "FT8" || specOp == 0) && opMode == OpModes.IDLE)
                    {
                        opMode = OpModes.START;
                        ShowStatus();
                        ClearAltCallList();
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
                    }

                    //check for changed Tx enabled
                    if (lastTxEnabled != txEnabled)
                    {
                        DebugOutput($"{Time()} txEnabled:{txEnabled} (was {lastTxEnabled})");
                        if (!txEnabled) ctrl.timer2.Stop();       //no xmit cycle now
                        if (txEnabled && altCallList.Count > 0 /*&& WsjtxMessage.IsCQ(txMsg)*/ && qsoState == WsjtxMessage.QsoStates.CALLING)
                        {
                            DebugOutput($"{Time()} Tx enabled: starting queue processing");
                            txTimeout = true;                   //triggers next in queue
                            tCall = null;                       //prevent call removal from queue
                            DateTime dtNow = DateTime.Now;
                            if (txFirst == IsEvenPeriod((dtNow.Second * 1000) + dtNow.Millisecond))
                            {
                                xmitCycleCount = -1;        //Tx enabled during the same period Tx will happen, add one more tx cycle before timeout
                                DebugOutput($"           *Tx enabled during Tx period");
                            }
                            else
                            {
                                xmitCycleCount = 0;
                                DebugOutput($"           *Tx enabled during Rx period");
                            }
                            CheckNextXmit();            //process the timeout
                        }
                        ShowStatus();
                        lastTxEnabled = txEnabled;
                    }

                    //check for watchdog timer status changed
                    if (lastTxWatchdog != smsg.TxWatchdog)
                    {
                        if (smsg.TxWatchdog && opMode == OpModes.ACTIVE)        //only need this event if in valid mode
                        {
                            string hStatus = $"Status    qsoState:{qsoState} lastTxMsg:{smsg.LastTxMsg} txEnabled:{txEnabled} tCall:{tCall} TxWatchdog:{smsg.TxWatchdog} Transmitting:{transmitting} Mode:{mode}";
                            DebugOutput(hStatus);
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

                    if (lastDialFrequency != null && (Math.Abs((Int64)lastDialFrequency - (Int64)smsg.DialFrequency) > 500000))
                    {
                        ClearCalls();
                        logList.Clear();            //can re-log on new band
                        DebugOutput($"{Time()} Cleared queued calls:DialFrequency");
                        lastDialFrequency = smsg.DialFrequency;
                    }

                    //detect WSJT-X Tx First change
                    if (txFirst != lastTxFirst)
                    {
                        ClearCalls();
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
                        ClearAltCallList();
                        //string curStatus = $"Status    qsoState:{qsoState} lastTxMsg:{smsg.LastTxMsg} txEnabled:{txEnabled} txMsg:'{txMsg}' txTimeout:{txTimeout} transmitting:{transmitting} mode:{mode}";
                        //DebugOutput(curStatus);
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
                        DebugOutput($"{Time()} qsoState:{qsoState} (was {lastQsoState})");
                        lastQsoState = qsoState;
                    }
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
            ctrl.timer2.Interval = (2 * (int)trPeriod) - diffMsec - cycleTimerAdj;     //tempOnly
            ctrl.timer2.Start();
            DebugOutput($"\n{Time()} StartTimer2: interval:{ctrl.timer2.Interval} msec");
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
            return true;
        }
        private void CheckNextXmit()
        {
            //******************
            //Timeout processing
            //******************
            //check for time to initiate next xmit from queued calls
            if (txTimeout)        //important to sync qso logged to end of xmit
            {
                replyCmd = null;        //last reply cmd sent is no longer in effect
                replyDecode = null;
                DebugOutput($"{Time()} CheckNextXmit start, txTimeout:{txTimeout} replyCmd:'{replyCmd}' tCall:{tCall}");   //tempOnly
                //check for call sign in process timed out and must be removed;
                //dictionary won't contain data for this call sign if QSO handled only by WSJT-X
                if (txTimeout && tCall != null)     //null if call added manually, and must be processed below
                {
                    DebugOutput($"           *{tCall} might be in process in WSJT-X but timed out");       //tempOnly
                    RemoveCall(tCall);  //tempOnly
                }

                //process the next call in the queue. if any present
                if (callQueue.Count > 0)            //have queued call signs
                {
                    DecodeMessage dmsg = new DecodeMessage();
                    string nCall = GetNextCall(out dmsg);
                    DebugOutput($"           *Have entries in queue, got {nCall}");       //tempOnly

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
                    rmsg.LowConfidence = dmsg.LowConfidence;
                    ba = rmsg.GetBytes();
                    udpClient2.Send(ba, ba.Length);
                    replyCmd = dmsg.Message;            //save the last reply cmd to determine which call is in progress
                    replyDecode = dmsg;                 //save the decode the reply cmd came from
                    DebugOutput($"{Time()} >>>>>Sent 'Reply To Msg' cmd:\n{rmsg} lastTxMsg:'{lastTxMsg}'\nreplyCmd:'{replyCmd}'");
                }
                else            //no queued call signs, start CQing
                {
                    DebugOutput($"           *No entries in queue, start CQing");       //tempOnly
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
                    DebugOutput($"{Time()} qsoState:{qsoState} (was {lastQsoState} replyCmd:'{replyCmd}')");
                    lastQsoState = qsoState;
                }
                txTimeout = false;              //ready for next timeout
                qsoLogged = false;              //clear "logged" status display
                DebugOutput($"{Time()} CheckNextXmit end, txTimeout:{txTimeout} replyCmd:'{replyCmd}' qsoLogged:{qsoLogged}");
                ShowStatus();
            }
            UpdateDebug();
        }

        public void ProcessDecodes()
        {
            DebugOutput($"\n{Time()} Timer2 tick: txEnabled:{txEnabled} txTimeout:{txTimeout}");
            if (ctrl == null) DebugOutput($"ERROR: ctrl is null");            //tempOnly
            if (ctrl.timer2 == null) DebugOutput($"ERROR: timer2 is null");   //tempOnly
            ctrl.timer2.Stop();
            DebugOutput($"           *timer2 stopped");                             //tempOnly
            if (txEnabled) CheckNextXmit();
            DebugOutput($"{Time()} Timer2 tick done\n");
        }

        //check for time to log (best done at Tx start to avoid any logging/dequeueing timing problem if done at Tx end)
        private void ProcessTxStart()
        {
            string toCall = WsjtxMessage.ToCall(txMsg);
            string lastToCall = WsjtxMessage.ToCall(lastTxMsg);
            DebugOutput($"{Time()} Tx start: txMsg:'{txMsg}' lastTxMsg:'{lastTxMsg}' toCall:'{toCall}' lastToCall:'{lastToCall}' replyCmd:'{replyCmd}' qsoLogged:{qsoLogged}");
            //check for WSJT-X ready to respond to a 73/RR73 that is not from the last Tx 'to'
            // msg to be sent is 73               msg to be sent 'to'      is not  replyCmd 'from' (i.e.: WSJT-X ignored last reply cmd)
            if (WsjtxMessage.Is73orRR73(txMsg) && replyCmd != null && WsjtxMessage.ToCall(txMsg) != null 
                && WsjtxMessage.DeCall(replyCmd) != null && WsjtxMessage.ToCall(txMsg) != WsjtxMessage.DeCall(replyCmd))
            {
                //log the unexpected call sign
                LogQso(WsjtxMessage.ToCall(txMsg));
                qsoLogged = true;
                ShowStatus();
                DebugOutput($"           *unexpected logging reqd: toCall:{WsjtxMessage.ToCall(txMsg)} qsoLogged:{qsoLogged}");
                //put the call sign previously in-progress back in the call queue
                AddCall(replyCmd, replyDecode);
            }


            //check for time to log early
            //  option enabled                   correct cur and prev    just sent RRR                and previously sent +XX
            if (ctrl.logEarlyCheckBox.Checked && !qsoLogged && toCall == lastToCall && WsjtxMessage.IsRogers(txMsg) && WsjtxMessage.IsReport(lastTxMsg))
            {
                LogQso(toCall);
                qsoLogged = true;
                ShowStatus();
                DebugOutput($"           *early logging reqd: toCall:{toCall} qsoLogged:{qsoLogged}");
            }

            //check for QSO complete, normal logging
            // correct cur and prev     prev Tx was a RRR                 or prev Tx was a R+XX                    or prev Tx was a +XX                 and cur Tx was 73
            if (toCall == lastToCall && (WsjtxMessage.IsRogers(lastTxMsg) || WsjtxMessage.IsRogerReport(lastTxMsg) || WsjtxMessage.IsReport(lastTxMsg)) && WsjtxMessage.Is73orRR73(txMsg))
            {
                DebugOutput($"           *is 73, was RRR/R+XX, qsoLogged:{qsoLogged}");
                if (!qsoLogged)
                {
                    LogQso(toCall);
                    qsoLogged = true;
                    ShowStatus();
                    DebugOutput($"           *normal logging reqd: toCall:{toCall} qsoLogged:{qsoLogged}");
                }
            }
            DebugOutput($"{Time()} Tx start done: txMsg:'{txMsg}' lastTxMsg:'{lastTxMsg}' toCall:'{toCall}' lastToCall:'{lastToCall} qsoLogged:{qsoLogged}'\n");
        }

        //check for QSO end or timeout (and possibly logging (if txMsg changed between TX start and Tx end)
        private void ProcessTxEnd()
        {
            string toCall = WsjtxMessage.ToCall(txMsg);
            string lastToCall = WsjtxMessage.ToCall(lastTxMsg);
            DebugOutput($"\n{Time()} Tx end: xmitCycleCount:{xmitCycleCount} txMsg:'{txMsg}' lastTxMsg:'{lastTxMsg}' tCall: {tCall}\n           toCall:'{toCall}' lastToCall:'{lastToCall} qsoLogged:{qsoLogged}'");

            //check for WSJT-X processing a call other than last cmd
            string deCall = WsjtxMessage.DeCall(replyCmd);
            if (replyCmd != null && txMsg != null && toCall != deCall)
            {
                replyCmd = null;        //last reply cmd sent is no longer in effect
                replyDecode = null;
                xmitCycleCount = 0;     //stop any timeout, since new call
                DebugOutput($"           *Call selected manually in WSJT-X: invalidated replyCmd:'{replyCmd}' reset xmitCycleCount:{xmitCycleCount} txTimeout:{txTimeout}");
            }

            //check for time to log early; NOTE: doing this at Tx end because WSJT-X may have changed Tx msgs (between Tx start and Tx end) due to late decode for the current call
            //  option enabled                   correct cur and prev    just sent RRR                and previously sent +XX
            if (ctrl.logEarlyCheckBox.Checked && !qsoLogged && toCall == lastToCall && WsjtxMessage.IsRogers(txMsg) && WsjtxMessage.IsReport(lastTxMsg))
            {
                LogQso(toCall);
                qsoLogged = true;
                ShowStatus();
                DebugOutput($"           *early logging reqd: toCall:{toCall} qsoLogged:{qsoLogged}");
            }
            //check for QSO complete, trigger next call in the queue
            // correct cur and prev     prev Tx was a RRR                 or prev Tx was a R+XX                    or prev Tx was a +XX                 and cur Tx was 73
            if (toCall == lastToCall && (WsjtxMessage.IsRogers(lastTxMsg) || WsjtxMessage.IsRogerReport(lastTxMsg) || WsjtxMessage.IsReport(lastTxMsg)) && WsjtxMessage.Is73orRR73(txMsg))
            {
                txTimeout = true;      //timeout to Tx the next call in the queue
                xmitCycleCount = 0;
                DebugOutput($"{Time()} Reset(2): (is 73, was RRR/R+XX, have queue entry) xmitCycleCount:{xmitCycleCount} txTimeout:{txTimeout} qsoLogged:{qsoLogged}");
                //NOTE: doing this at Tx end because WSJT-X may have changed Tx msgs (between Tx start and Tx end) due to late decode for the current call
                if (!qsoLogged)
                {
                    LogQso(toCall);
                    qsoLogged = true;
                    ShowStatus();
                    DebugOutput($"           *normal logging reqd: toCall:{toCall} qsoLogged:{qsoLogged}");
                }
            }

            //count tx cycles: check for changed "to" call in WSJT-X
            if (lastTxMsg != txMsg && xmitCycleCount >= 0)
            {
                //"to" call has changed since last xmit end
                if (callQueue.Contains(toCall))
                {
                    RemoveCall(toCall);         //manually switched to Txing a call that was also in the queue
                }
                lastTxMsg = txMsg;
                xmitCycleCount = 0;
                DebugOutput($"{Time()} Reset(1) (different msg) xmitCycleCount:{xmitCycleCount} txMsg:'{txMsg}' lastTxMsg:'{lastTxMsg}'");
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
                        tCall = WsjtxMessage.ToCall(lastTxMsg);        //will be null if non-std msg
                        DebugOutput($"{Time()} Reset(3) (timeout) xmitCycleCount:{xmitCycleCount} txTimeout:{txTimeout} tCall:{tCall}");
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
                txTimeout = true;
                if (settingChanged)
                {
                    ctrl.WsjtxSettingConfirmed();
                    settingChanged = false;
                }
                DebugOutput($"{Time()} Reset(5) (new directed CQ, or setting changed) xmitCycleCount:{xmitCycleCount}");
            }

            DebugOutput($"{Time()} Tx end done: xmitCycleCount:{xmitCycleCount} txMsg:'{txMsg}' lastTxMsg:'{lastTxMsg}' tCall:{tCall} qsoLogged:{qsoLogged}\n");
            ShowTimeout();
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
        }

        private string CallInProgress()
        {
            if (replyCmd != null) return WsjtxMessage.DeCall(replyCmd);     //last cmd sent determines call in-progress
            return WsjtxMessage.ToCall(txMsg);                              //no call-related cmd sent, so last Tx determines call in-progress
        }

        private bool IsEvenPeriod(int msec)     //milliseconds past start of the current minute
        {
            return (msec / trPeriod) % 2 == 0;
        }

        private string NextDirCq()
        {
            string dirCq = "";
            if (ctrl.directedCheckBox.Checked && ctrl.directedTextBox.Text.Trim().Length > 0)
            {
                string[] dirWords = ctrl.directedTextBox.Text.Trim().ToUpper().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                string s = dirWords[rnd.Next() % dirWords.Length];
                if (s == "*")           //not directed
                {
                    dirCq = "";
                }
                else
                {
                    dirCq = " " + s;
                }
                DebugOutput($"{Time()} dirCq:{dirCq}");
            }
            return dirCq;
        }

        private void ResetNego()
        {
            ResetOpMode();
            WsjtxMessage.NegoState = WsjtxMessage.NegoStates.INITIAL;
            DebugOutput($"\n\n{Time()} opMode:{opMode} NegoState:{WsjtxMessage.NegoState}");
            DebugOutput($"{Time()} Waiting for heartbeat...");
            cmdCheck = RandomCheckString();
            commConfirmed = false;
            UpdateDebug();
        }

        private void ResetOpMode()
        {
            opMode = OpModes.IDLE;
            modeSupported = true;           //until otherwise detected
            lastModeSupported = null;
            ShowStatus();
            lastMode = null;
            lastXmitting = false;
            lastTxWatchdog = null;
            lastDialFrequency = null;
            trPeriod = null;
            logList.Clear();        //can re-log on new mode
            ShowLogged();
            ClearCalls();
            UpdateDebug();
            DebugOutput($"\n\n{Time()} opMode:{opMode} NegoState:{WsjtxMessage.NegoState}");
        }

        private void ClearCalls()
        {
            callQueue.Clear();
            callDict.Clear();
            ShowQueue();
            ClearAltCallList();
            reportList.Clear();
            rogerReportList.Clear();
            xmitCycleCount = 0;
            ShowTimeout();
            ctrl.timer2.Stop();
        }

        private bool UpdateCall(string call, DecodeMessage msg)
        {
            if (callDict.ContainsKey(call))
            {
                callDict.Remove(call);
                callDict.Add(deCall, msg);
                ShowQueue();
                DebugOutput($"{Time()} Updated {deCall}:{CallQueueString()} {CallDictString()}");
                return true;
            }
            DebugOutput($"{Time()} Not updated {deCall}:{CallQueueString()} {CallDictString()}");
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
                    Play("dive.wav");
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

        //add call to queue/dict
        //return false if already added
        private bool AddCall(string call, DecodeMessage msg)
        {
            DecodeMessage dmsg;
            if (!callDict.TryGetValue(call, out dmsg))     //dictionary does not contain call data for this call sign
            {
                callQueue.Enqueue(call);
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
                Play("dive.wav");
                DebugOutput("ERROR: {nCall} not found");
                errorDesc = "{nCall} not found";
                UpdateDebug();
                return null;
            }

            if (callDict.ContainsKey(call)) callDict.Remove(call);

            if (callDict.Count != callQueue.Count)
            {
                Play("dive.wav");
                DebugOutput("ERROR: callDict and queueDict out of sync");
                errorDesc = " callDict out of sync";
                UpdateDebug();
                return null;
            }

            ShowQueue();
            dmsg.Message = dmsg.Message.Replace("73", "  ");            //important, otherwise WSJT-X will not respond
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

        private string AltCallListString()
        {
            string delim = "";
            StringBuilder sb = new StringBuilder();
            sb.Append("altCallList [");
            foreach (string call in altCallList)
            {
                sb.Append(delim + call);
                delim = ", ";
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

        private string AltCallDictString()
        {
            string delim = "";
            StringBuilder sb = new StringBuilder();
            sb.Append("altCallDict [");
            foreach (var entry in altCallDict)
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

        /*
        [Flags]
        private enum SoundFlags
        {
            /// <summary>play synchronously (default)</summary>
            SND_SYNC = 0×0000,
            /// <summary>play asynchronously</summary>
            SND_ASYNC = 0×0001,
            /// <summary>silence (!default) if sound not found</summary>
            SND_NODEFAULT = 0×0002,
            /// <summary>pszSound points to a memory file</summary>
            SND_MEMORY = 0×0004,
            /// <summary>loop the sound until next sndPlaySound</summary>
            SND_LOOP = 0×0008,
            /// <summary>don’t stop any currently playing sound</summary>
            SND_NOSTOP = 0×0010,
            /// <summary>Stop Playing Wave</summary>
            SND_PURGE = 0×40,
            /// <summary>don’t wait if the driver is busy</summary>
            SND_NOWAIT = 0×00002000,
            /// <summary>name is a registry alias</summary>
            SND_ALIAS = 0×00010000,
            /// <summary>alias is a predefined id</summary>
            SND_ALIAS_ID = 0×00110000,
            /// <summary>name is file name</summary>
            SND_FILENAME = 0×00020000,
            /// <summary>name is resource name or atom</summary>
            SND_RESOURCE = 0×00040004
        }
        */
        public void Play(string strFileName)
        {
            PlaySound(strFileName, UIntPtr.Zero,
               0x00020001);
        }

        private void ShowQueue()
        {
            if (callQueue.Count == 0)
            {
                ctrl.callText.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
                ctrl.callText.ForeColor = System.Drawing.Color.Gray;
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
            ctrl.callText.Font = new System.Drawing.Font("Consolas", 10.0F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            ctrl.callText.ForeColor = System.Drawing.Color.Black;
            ctrl.callText.Text = sb.ToString();
        }

        private void ShowStatus()
        {
            string status = "";
            System.Drawing.Color color = System.Drawing.Color.Red;
            ctrl.statusText.ForeColor = System.Drawing.Color.White;

            if (WsjtxMessage.NegoState == WsjtxMessage.NegoStates.FAIL || !modeSupported)
            {
                ctrl.statusText.Text = failReason;
                ctrl.statusText.BackColor = System.Drawing.Color.Yellow;
                ctrl.statusText.ForeColor = System.Drawing.Color.Black;
                return;
            }

            switch ((int)opMode)
            {
                case (int)OpModes.START:            //fall thru
                case (int)OpModes.IDLE:
                    status = "Connecting: Select 'Monitor', wait until ready";
                    break;
                case (int)OpModes.ACTIVE:
                    status = txEnabled ? (qsoLogged ? "QSO logged" : "Automatic calling enabled") : "To start: Select 'Enable Tx'";
                    color = System.Drawing.Color.Green;
                    break;
            }
            ctrl.statusText.BackColor = color;
            ctrl.statusText.Text = status;
        }

        private void ShowLogged()
        {
            if (logList.Count == 0)
            {
                ctrl.loggedText.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
                ctrl.loggedText.ForeColor = System.Drawing.Color.Gray;
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
            ctrl.loggedText.Font = new System.Drawing.Font("Consolas", 10.0F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            ctrl.loggedText.ForeColor = System.Drawing.Color.Black;
            ctrl.loggedText.Text = sb.ToString();
        }
        /*
        private void wdTimer_Tick(object sender, EventArgs e)
        {
            wdTimer.Stop();
            //DebugOutput($"{Time()} wdTimer ticked");
        }
        */

        public void AltPauseButtonToggeled()
        {
            if (altListPaused)
            {
                ctrl.altPauseButton.Text = "Pause list";
            }
            else
            {
                ctrl.altPauseButton.Text = "Resume list";
            }
            altListPaused = !altListPaused;
            UpdateAltCallListStatus();
        }

        private void AddAltCall(DecodeMessage dmsg)
        {
            if (altListPaused) return;
            if (dmsg.Message.Contains("...")) return;
            if (cQonly && !dmsg.IsCQ()) return;
            string deCall = WsjtxMessage.DeCall(dmsg.Message);
            if (deCall == null) return;

            if (altCallList.Count > 0 && altCallList[0].Contains("..."))
            {
                ctrl.altListBox.Enabled = true;
                altCallList.Clear();
            }

            //DebugOutput($"\n{Time()} AddAltCall '{dmsg.Message}'\n{AltCallListString()}");
            if (altCallList.Contains(dmsg.Message))         //don't need duplicates, but show latest, in sequence
            {
                altCallList.Remove(dmsg.Message);     //remove from somewhere in the list
                //DebugOutput($"           after remove:'{dmsg.Message}'\n{AltCallListString()}");
            }
            latestDecodeTime = dmsg.SinceMidnight;
            altCallList.Add(dmsg.Message);      //save the message body
            //DebugOutput($"           after add:'{dmsg.Message}'\n{AltCallListString()}");

            //update altCallDict by removing existing decode first
            if (altCallDict.ContainsKey(deCall))        //deCall known to not be null
            {
                DecodeMessage rmsg;
                altCallDict.TryGetValue(deCall, out rmsg);
                //DebugOutput($"           before remove deCall:'{deCall}' msg:'{rmsg.Message}' raw:'{rmsg}'\n{AltCallDictString()}");
                altCallDict.Remove(deCall);
                //DebugOutput($"           after  remove deCall:'{deCall}' msg:'{rmsg.Message}' raw:'{rmsg}'\n{AltCallDictString()}");
            }
            altCallDict.Add(deCall, dmsg);      //save the message for SNR
            //DebugOutput($"           after add deCall:'{deCall}' msg:'{dmsg.Message}' raw:'{dmsg}'\n{AltCallDictString()}");

            //shorten list
            if (altCallList.Count > 64)
            {
                string remDeCall = WsjtxMessage.DeCall(altCallList[0]);          //the 'from' call being removed (might be a separator)
                //DebugOutput($"           shorten altCallList, remDeCall:'{remDeCall}' msg:{altCallList[0]}");
                altCallList.RemoveAt(0);
                //DebugOutput($"           after remove remDeCall:'{remDeCall}'\n{AltCallListString()}");

                //remove remDeCall from call dictionary if no longer present in altCallList in any form 
                //(different forms of msgs from the same call are allowed in altCallList)
                if (remDeCall != null)              //not a separator
                {
                    string aDeCall = null;
                    foreach (string aMsg in altCallList)    //search thru altCallList
                    {
                        aDeCall = WsjtxMessage.DeCall(aMsg);    //might be null if msg invalid
                        if (aDeCall == remDeCall)           //the call removed from altCallList still exists in altCallList in a different form
                        {
                            break;
                        }
                    }
                    if (aDeCall != remDeCall)               //the call is not in altCallList in any form
                    {
                        //DebugOutput($"           shorten altCallDict, remDeCall:'{remDeCall}'");
                        if (altCallDict.ContainsKey(remDeCall)) altCallDict.Remove(remDeCall);
                        //DebugOutput($"           after remove remDeCall:'{remDeCall}'\n{AltCallDictString()}");
                    }
                }
            }

            ctrl.altListBox.Font = new System.Drawing.Font("Consolas", 10.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            ctrl.altListBox.ForeColor = System.Drawing.Color.Black;
            ctrl.altListBox.DataSource = null;
            ctrl.altListBox.DataSource = altCallList;
            ctrl.altListBox.ClearSelected();
            ctrl.altListBox.TopIndex = ctrl.altListBox.Items.Count - 1;

            ctrl.timer3.Stop();         //set timer for after last decode
            ctrl.timer3.Interval = 3000;
            ctrl.timer3.Start();
        }

        public void AddAltCallSeparator()
        {
            if (altListPaused) return;

            altCallList.Add("-------------------------");
            ctrl.altListBox.DataSource = null;
            ctrl.altListBox.DataSource = altCallList;
            ctrl.altListBox.ClearSelected();
            ctrl.altListBox.TopIndex = ctrl.altListBox.Items.Count - 1;
        }

        public void ClearAltCallList()
        {
            ctrl.timer3.Stop();     //stop adding separators
            ctrl.altListBox.DataSource = null;
            altCallList.Clear();
            altCallDict.Clear();
            ctrl.altListBox.DataSource = altCallList;
            UpdateAltCallListStatus();
        }

        private void UpdateAltCallListStatus()
        {
            if (altCallList.Count == 0 || altCallList[0].Contains("..."))
            {
                ctrl.altListBox.Enabled = false;
                altCallList.Clear();
                if (altListPaused)
                {
                    altCallList.Add("List paused...");
                }
                else if (opMode == OpModes.START || opMode == OpModes.ACTIVE)
                {
                    string s = txFirst ? "odd" : "even";
                    altCallList.Add($"Waiting for calls in '{s}' cycle...");
                }
                else
                {
                    altCallList.Add("Connecting...");
                }
                ctrl.altListBox.DataSource = null;
                ctrl.altListBox.DataSource = altCallList;
                ctrl.altListBox.ClearSelected();
                ctrl.altListBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
                ctrl.altListBox.ForeColor = System.Drawing.Color.Gray;
            }
            else
            {
                ctrl.altListBox.Enabled = true;
                if (altListPaused)
                {
                    if (!altCallList[altCallList.Count - 1].Contains("..."))
                    {
                        altCallList.Add("...list paused...");
                        ctrl.altListBox.DataSource = null;
                        ctrl.altListBox.DataSource = altCallList;
                        ctrl.altListBox.ClearSelected();
                        ctrl.altListBox.TopIndex = ctrl.altListBox.Items.Count - 1;
                    }
                }
                else
                {
                    if (altCallList[altCallList.Count - 1].Contains("..."))
                    {
                        altCallList.RemoveAt(altCallList.Count - 1);
                        ctrl.altListBox.DataSource = null;
                        ctrl.altListBox.DataSource = altCallList;
                        ctrl.altListBox.ClearSelected();
                        ctrl.altListBox.TopIndex = ctrl.altListBox.Items.Count - 1;
                    } 
                }
            }
        }

        public void AltListBoxClicked()
        {
            if (ctrl.altListBox.SelectedItem == null)
            {
                ctrl.altListBox.ClearSelected();
                return;
            }
            string msgBody = ctrl.altListBox.SelectedItem.ToString();

            if (msgBody.Contains("...") || msgBody.Contains("---"))            //status msg selected
            {
                ctrl.altListBox.ClearSelected();
                return;
            }
        }


        public void AltCallSelected(bool shiftKey)
        {
            if (ctrl.altListBox.SelectedItem == null)
            {
                ctrl.altListBox.ClearSelected();
                return;
            }
            string msg = ctrl.altListBox.SelectedItem.ToString();

            if (msg.Contains("...") || msg.Contains("---"))            //status msg selected
            {
                ctrl.altListBox.ClearSelected();
                return;
            }

            string deCall = WsjtxMessage.DeCall(msg);
            string toCall = WsjtxMessage.ToCall(msg);

            if (deCall == null)
            {
                errorDesc = "no 'DE' part";
                Console.Beep();
                UpdateDebug();
                return;
            }

            if (shiftKey)
            {
                if (msg.Contains("CQ"))
                {
                    errorDesc = "msg contains CQ";
                    Console.Beep();
                    UpdateDebug();
                    return;
                }
                if (toCall == null)
                {
                    errorDesc = "no 'to' call";
                    Console.Beep();
                    UpdateDebug();
                    return;
                }
                if (toCall == myCall)
                {
                    errorDesc = $"{toCall} is to me";
                    Console.Beep();
                    UpdateDebug();
                    return;
                }

                DebugOutput($"%Shift/dbl-click on {toCall}");
                //build a CQ message to reply to
                DecodeMessage nmsg; 
                nmsg = new DecodeMessage();
                nmsg.Mode = rawMode;
                nmsg.SchemaVersion = WsjtxMessage.NegotiatedSchemaVersion;
                nmsg.New = true;
                nmsg.OffAir = false;
                nmsg.Id = WsjtxMessage.UniqueId;
                nmsg.Snr = 0;               //not used
                nmsg.DeltaTime = 0.0;       //not used
                nmsg.DeltaFrequency = 1000; //not used
                nmsg.Message = $"CQ {toCall}";
                nmsg.SinceMidnight = latestDecodeTime + new TimeSpan(0, 0, 0, 0, (int)trPeriod);
                ClearCalls();                       //nothing left to do this tx period
                AddCall(toCall, nmsg);              //add to call queue

                if (txEnabled)
                {
                    txTimeout = true;                   //immediate switch to other tx period
                    tCall = null;                       //prevent call removal from queue
                    CheckNextXmit();
                }
            }
            else   //not shift key
            {
                if (callQueue.Contains(deCall))
                {
                    errorDesc = $"{deCall} already in queue";
                    Console.Beep();
                    UpdateDebug();
                    return;
                }
                if (txEnabled && deCall == CallInProgress())
                {
                    errorDesc = $"{deCall} in progress";
                    Console.Beep();
                    UpdateDebug();
                    return;
                }

                if (!altCallDict.ContainsKey(deCall))
                {
                    errorDesc = $"{deCall} not stored";
                    Console.Beep();
                    UpdateDebug();
                    return;
                }

                DebugOutput($"%Dbl-click on {toCall}");
                //get actual message to reply to
                DecodeMessage dmsg;
                altCallDict.TryGetValue(deCall, out dmsg);  //need SNR if skipping grid msg
                AddCall(deCall, dmsg);              //add to call queue

                if (txEnabled && callQueue.Count == 1 && qsoState == WsjtxMessage.QsoStates.CALLING)  //stops CQing, starts the first xmit at next decode/status/heartbeat msg
                {
                    txTimeout = true;
                    tCall = null;                       //prevent call removal from queue
                }
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
                        ctrl.timeoutLabel.ForeColor = System.Drawing.Color.Red;
                        break;

                    case 2:
                        ctrl.timeoutLabel.ForeColor = System.Drawing.Color.Orange;
                        break;

                    default:
                        ctrl.timeoutLabel.ForeColor = System.Drawing.Color.Green;
                        break;
                }
                
                ctrl.timeoutLabel.Text = $"(now: {xmitCycleCount + 1})";
            }
        }

        private void UpdateDebug()
        {
            if (!debug) return;
            string s;

            try             //tempOnly
            {
                ctrl.label5.Text = $"xmit: {transmitting.ToString().Substring(0, 1)}";
                ctrl.label6.Text = $"UDP: {msg.GetType().Name.Substring(0, 6)}";
                ctrl.label7.Text = $"txEnable: {txEnabled.ToString().Substring(0, 1)}";

                ctrl.label8.Text = $"cmd from: {WsjtxMessage.DeCall(replyCmd)}";
                ctrl.label9.Text = $"opMode: {opMode}";

                string txTo = (txMsg ==  null ? "" : WsjtxMessage.ToCall(txMsg));
                s = (txTo == "CQ" ? null : txTo);
                ctrl.label12.Text = $"tx to: {s}";
                string inPr = (CallInProgress() == null ? "" : CallInProgress());
                s = (inPr == "CQ" ? null : txTo);
                ctrl.label13.Text = $"in-prog: {s}";

                ctrl.label14.Text = $"qsoState: {qsoState}";
                ctrl.label15.Text = $"log call: {qCall}";

                //ctrl.label9.Text = $"replyTo: {replyCmd}";
                //ctrl.label13.Text = $"txMsg: {txMsg}";

                ctrl.label10.Text = $"t/o: {txTimeout.ToString().Substring(0, 1)}";
                ctrl.label11.Text = $"txFirst: {txFirst.ToString().Substring(0, 1)}";
                ctrl.label16.Text = $"Nego:{WsjtxMessage.NegoState}";
                ctrl.label17.Text = $"Err: {errorDesc}";
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
    }   
}
