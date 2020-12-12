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

        private string myCall = null, myGrid = null;
        private Dictionary<string, DecodeMessage> callDict = new Dictionary<string, DecodeMessage>();
        private Queue<string> callQueue = new Queue<string>();
        private List<string> altCallList = new List<string>();
        private List<string> reportList = new List<string>();
        private List<string> rogerReportList = new List<string>();
        private bool txEnabled = false;
        private bool transmitting = false;
        private bool decoding = false;
        private bool qsoLogged = false;
        private WsjtxMessage.QsoStates qsoState = WsjtxMessage.QsoStates.CALLING;
        private string deCall = "";
        private string mode = "";
        private string rawMode = "";
        private bool txFirst = false;
        private bool cQonly = false;
        private int? trPeriod = null;       //msec
        private string replyCmd = null;     //no "reply to" cmd sent to WSJT-X yet
        private DecodeMessage replyDecode = null;

        private WsjtxMessage.QsoStates lastQsoState = WsjtxMessage.QsoStates.INVALID;
        private UdpClient udpClient2;
        private IPEndPoint endPoint;
        private List<string> supportedModes = new List<string>() { "FT8", "FT4" };
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
        private List<string> acceptableWsjtxVersions = new List<string> { "2.2.2" };
        private string failReason = "Failure reason: Unknown";

        private const int maxQueueLines = 6, maxQueueWidth = 19, maxLogWidth = 9;
        private byte[] ba;
        private EnableTxMessage emsg;
        //HaltTxMessage amsg;
        private WsjtxMessage msg = new UnknownMessage();
        private string errorDesc = null;
        private Random rnd = new Random();
        TimeSpan latestDecodeTime;

        private struct UdpState
        {
            public UdpClient u;
            public IPEndPoint e;
        }

        private enum OpModes
        {
            READY,
            START,
            ACTIVE
        }
        private OpModes opMode = OpModes.READY;

        public WsjtxClient(Controller c, IPAddress reqIpAddress, int reqPort, bool reqMulticast, bool reqDebug)
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

            asyncCallback = new AsyncCallback(ReceiveCallback);
            s = new UdpState();
            s.e = endPoint;
            s.u = udpClient;

            WsjtxMessage.NegoState = WsjtxMessage.NegoStates.INITIAL;
            Console.WriteLine($"{Time()} NegoState: INITIAL");
            Console.WriteLine($"{Time()} opMode: READY");
            Console.WriteLine($"{Time()} Waiting for heartbeat...");

            ShowStatus();
            ShowQueue();
            ShowLogged();
            UpdateAltCallListStatus();
            messageRecd = false;
            recvStarted = false;

            ctrl.altListBox.DataSource = altCallList;

            string cast = multicast ? "(multicast)" : "(unicast)";
            ctrl.verLabel.Text = $"by WM8Q v{fileVer} IP addr: {ipAddress}:{port} {cast}";
            ctrl.verLabel2.Text = $"Want more features? more.avantol@xoxy.net";

            ctrl.alertTextBox.Enabled = false;
            ctrl.directedTextBox.Enabled = false;
            ctrl.timeoutLabel.Visible = false;

            emsg = new EnableTxMessage();
            emsg.Id = WsjtxMessage.UniqueId;

            //amsg = new HaltTxMessage();
            //amsg.Id = WsjtxMessage.UniqueId;
            //amsg.AutoOnly = true;

            UpdateDebug();          //last before starting loop
        }

        public void UdpLoop()
        {
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
                udpClient.BeginReceive(asyncCallback, s);
                recvStarted = true;
            }
        }

        private void Update()
        {

            try
            {
                msg = WsjtxMessage.Parse(datagram);
                Console.WriteLine($"{Time()} msg:{msg.GetType().Name}");            //tempOnly
            }
            catch (ParseFailureException ex)
            {
                File.WriteAllBytes($"{ex.MessageType}.couldnotparse.bin", ex.Datagram);
                Console.WriteLine($"{Time()} Parse failure for {ex.MessageType}: {ex.InnerException.Message}");
                errorDesc = "Parse fail {ex.MessageType}";
                UpdateDebug();
                return;
            }

            //first HeartbeatMessage
            if (msg.GetType().Name == "HeartbeatMessage" && (WsjtxMessage.NegoState == WsjtxMessage.NegoStates.INITIAL || WsjtxMessage.NegoState == WsjtxMessage.NegoStates.FAIL))
            {
                Console.WriteLine(msg);
                HeartbeatMessage imsg = (HeartbeatMessage)msg;
                if (!acceptableWsjtxVersions.Contains(imsg.Version) || imsg.Version == "2.2.2" && imsg.Revision == "0d9b96")
                {
                    WsjtxMessage.NegoState = WsjtxMessage.NegoStates.FAIL;
                    Console.WriteLine($"{Time()} NegoState: FAIL");
                    Console.Beep();
                    failReason = $"WSJT-X v{imsg.Version} {imsg.Revision} not supported";
                    Console.WriteLine($"{Time()} {failReason}");
                    ShowStatus();
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
                    Console.WriteLine($"{Time()} NegoState: SENT");
                    Console.WriteLine($"{Time()} >>>>>Sent'Heartbeat' msg:\n{tmsg}");
                    ShowStatus();
                }
                UpdateDebug();
                return;
            }

            //negotiation HeartbeatMessage
            if (WsjtxMessage.NegoState == WsjtxMessage.NegoStates.SENT && msg.GetType().Name == "HeartbeatMessage")
            {
                Console.WriteLine(msg);
                Console.WriteLine($"{Time()} NegoState: RECD");

                HeartbeatMessage hmsg = (HeartbeatMessage)msg;
                WsjtxMessage.NegotiatedSchemaVersion = hmsg.SchemaVersion;
                WsjtxMessage.NegoState = WsjtxMessage.NegoStates.RECD;
                Console.WriteLine($"{Time()} Negotiated schema version:{WsjtxMessage.NegotiatedSchemaVersion}");
                UpdateDebug();
            }

            //get minimal info from StatusMessage needed for faster startup
            if (WsjtxMessage.NegoState != WsjtxMessage.NegoStates.RECD && msg.GetType().Name == "StatusMessage")
            {
                StatusMessage smsg = (StatusMessage)msg;
                mode = smsg.Mode;
                specOp = (int)smsg.SpecialOperationMode;
                Console.WriteLine($"{Time()} Status    mode: {mode} specOp:{specOp}");
            }


            //************
            //CloseMessage
            //************
            if (msg.GetType().Name == "CloseMessage")
            {
                Console.WriteLine(msg);

                if (udpClient2 != null) udpClient2.Close();
                WsjtxMessage.NegoState = WsjtxMessage.NegoStates.INITIAL;
                ResetOpMode();     //wait for (new) WSJT-X mode
                //Console.WriteLine(msg);
                Console.WriteLine($"");
                Console.WriteLine($"{Time()} opMode: READY (close)");
                Console.WriteLine($"{Time()} Waiting for heartbeat...");
                UpdateDebug();
                return;
            }

            //****************
            //HeartbeatMessage
            //****************
            if (msg.GetType().Name == "HeartbeatMessage")
            {
            }

            if (WsjtxMessage.NegoState == WsjtxMessage.NegoStates.RECD)
            {
                if (supportedModes.Contains(mode) && (mode != "FT8" || specOp == 0))
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
                            if (dmsg.IsCallTo(myCall)) Console.WriteLine(dmsg);

                            deCall = dmsg.DeCall();
                            //do some processing not directly related to replying immediately
                            if (deCall != null)
                            {
                                //add the decodes having the opposite time from the Tx 1st checkbox in WSJT-X
                                if (((dmsg.SinceMidnight.Seconds % (trPeriod / 500)) == 0) != txFirst) addAltCall(dmsg);

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
                                Console.WriteLine($"     *'{deCall}' is to {myCall}");  //tempOnly
                                // not xmitting yet    
                                if (/*!transmitting*/ true)      //todo: can process decodes after Tx starts?
                                {
                                    if (!dmsg.Is73())       //not a 73 or RR73
                                    {
                                        Console.WriteLine($"     *Not a 73");  //tempOnly
                                        if (!callQueue.Contains(deCall))        //call not in queue, possibly enqueue the call data
                                        {
                                            Console.WriteLine($"     *'{deCall}' not already in queue");  //tempOnly
                                            if (qsoState == WsjtxMessage.QsoStates.CALLING && !decodedMsgReplied)
                                            {
                                                Console.WriteLine($"     *CALLING, no decode replied yet this cycle, reply now txTimeout:{txTimeout}");  //tempOnly
                                                //WSJT-X CQing, is ready to process this call
                                                //set WSJT-X call enable with Reply message
                                                txTimeout = false;   //cancel any pending timeout (like for directed CQ)
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
                                                Console.WriteLine($"{Time()} >>>>>Sent 'Reply To Msg', txTimeout:{txTimeout} cmd:\n{rmsg}\nreplyToReq:'{replyCmd}'");
                                                decodedMsgReplied = true;       //no more replies during rest of pass(es) in current decoding phases
                                           }
                                            else   //not CALLING or a decode already replied to this cycle
                                            {
                                                Console.WriteLine($"     *not CALLING (is {qsoState}) or a decode already replied to this cycle {decodedMsgReplied}");     //tempOnly
                                                Console.WriteLine($"     *last Tx 'to':({WsjtxMessage.ToCall(txMsg)}), last cmd 'from':({WsjtxMessage.DeCall(replyCmd)})");     //tempOnly
                                                if (deCall == CallInProgress())                //call currently being processed by WSJT-X
                                                {
                                                    Console.WriteLine($"     *'{deCall}' currently being processed by WSJT-X:");    //tempOnly
                                                }
                                                else
                                                {
                                                    Console.WriteLine($"     *'{deCall}' not currently being processed by WSJT-X");     //tempOnly
                                                    AddCall(deCall, dmsg);          //known to not be in queue
                                                }
                                            }
                                        }
                                        else       //call is already in queue, possibly update the call data
                                        {
                                            Console.WriteLine($"     *'{deCall}' already in queue");     //tempOnly
                                            if (deCall == CallInProgress())                //call currently being processed by WSJT-X
                                            {
                                                Console.WriteLine($"     *'{deCall}' currently being processed by WSJT-X");     //tempOnly
                                                RemoveCall(deCall);             //may have been queued previously
                                            }
                                            else        //update the call in queue
                                            {
                                                Console.WriteLine($"     *'{deCall}' not currently being processed by WSJT-X, update queue");     //tempOnly
                                                UpdateCall(deCall, dmsg);
                                            }
                                        }
                                    }
                                    else        //decode is 73 msg
                                    {
                                        Console.WriteLine($"     *decode is 73 msg, tCall:{tCall}, cur Tx to:'{WsjtxMessage.DeCall(txMsg)}' cur Tx IsRogers:{WsjtxMessage.IsRogers(txMsg)}");     //tempOnly
                                        /* to-do: need to set up WSJT-X with valid QSO data first
                                        //check for last-chance logging (73 after current QSO just ended)
                                        //  was not logged early at RRR sent  same call timed out  prev Tx msg was to myCall           prev Tx msg was a RRR
                                        if (!ctrl.logEarlyCheckBox.Checked && tCall == deCall && WsjtxMessage.ToCall(lastTxMsg) == deCall && WsjtxMessage.IsRogers(lastTxMsg))       //this decode is a late 73, log it
                                        {
                                            Console.WriteLine($"     *{deCall} timed out, prev Tx msg was to {myCall}, current Tx msg was a RRR");     //tempOnly
                                            Console.WriteLine($"{Time()} Logging late 73 msg");
                                            LogQso(deCall);
                                        }
                                        */
                                    }
                                }
                                //else
                                //{
                                //    Console.WriteLine($"     *discarded decode, already transmitting");
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
                        Console.WriteLine(lmsg);         //tempOnly
                        qCall = lmsg.DxCall;
                        if (ctrl.loggedCheckBox.Checked) Play("echo.wav");
                        logList.Add(qCall);    //even if already logged this mode/band
                        ShowLogged();
                        Console.WriteLine($"{Time()} QSO logging ackd: {qCall}");
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
                    if (lastTxEnabled == null) lastTxEnabled = txEnabled;     //initialize
                    if (lastDecoding == null) lastDecoding = decoding;     //initialize
                    if (lastTxWatchdog == null) lastTxWatchdog = smsg.TxWatchdog;   //initialize
                    if (lastMode == null) lastMode = mode;                     //initialize
                    if (lastTxFirst == null) lastTxFirst = txFirst;                     //initialize
                    if (lastDialFrequency == null) lastDialFrequency = smsg.DialFrequency; //initialize
                    if (lastSpecOp == null) lastSpecOp = (int)smsg.SpecialOperationMode; //initialize
                    if (lastTxMsg == null) lastTxMsg = txMsg;   //initialize
                    if (smsg.TRPeriod != null) trPeriod = (int)smsg.TRPeriod;

                    //detect xmit start/end ASAP
                    if (trPeriod != null && transmitting != lastXmitting)
                    {
                        if (transmitting)
                        {
                            StartTimer2();
                            decodedMsgReplied = false;          //decode passes finished
                            processTxStart();
                        }
                        else                //end of transmit
                        {
                            processTxEnd();
                        }
                        lastXmitting = transmitting;
                        ShowStatus();
                    }

                    //minimize console clutter
                    string curStatus = $"Status    qsoState:{qsoState} txMsg:'{txMsg}' replyCmd:'{replyCmd}' \n          txTimeout:{txTimeout} Transmitting: {transmitting} Mode: {mode} txEnabled:{txEnabled}\n          txFirst:{txFirst} dxCall:{dxCall} trPeriod: {trPeriod}";
                    if (curStatus != lastStatus)
                    {
                        Console.WriteLine(curStatus);
                        lastStatus = curStatus;
                    }

                    //detect WSJT-X mode change
                    if (mode != lastMode)
                    {
                        Console.WriteLine($"{Time()} mode: {mode} (was {lastMode})");

                        ResetOpMode();
                        Console.WriteLine($"{Time()} opMode: READY, waiting for mode status...");
                        lastMode = mode;
                    }

                    //detect WSJT-X special operating mode change
                    if (specOp != lastSpecOp)
                    {
                        Console.WriteLine($"{Time()} specOp: {specOp} (was {lastSpecOp})");

                        ResetOpMode();
                        Console.WriteLine($"{Time()} opMode: READY, waiting for mode status...");
                        lastSpecOp = specOp;
                    }

                    //check for time to flag starting first xmit
                    if (supportedModes.Contains(mode) && (mode != "FT8" || specOp == 0) && opMode == OpModes.READY)
                    {
                        opMode = OpModes.START;
                        ShowStatus();
                        ClearAltCallList();
                        Console.WriteLine($"{Time()} opMode: START");
                    }

                    //detect decoding start/end
                    if (smsg.Decoding != lastDecoding)
                    {
                        if (smsg.Decoding)
                        {
                            Console.WriteLine($"{Time()} Decode start");
                        }
                        else
                        {
                            Console.WriteLine($"{Time()} Decode end");
                        }
                        lastDecoding = smsg.Decoding;
                    }

                    //check for changed QSO state in WSJT-X
                    if (lastQsoState != qsoState)
                    {
                        Console.WriteLine($"{Time()} qsoState: {qsoState} (was {lastQsoState})");
                        lastQsoState = qsoState;
                    }

                    //check for changed Tx enabled
                    if (lastTxEnabled != txEnabled)
                    {
                        Console.WriteLine($"{Time()} txEnabled: {txEnabled} (was {lastTxEnabled})");
                        if (!txEnabled) ctrl.timer2.Stop();       //no xmit cycle now
                        if (txEnabled && altCallList.Count > 0 /*&& WsjtxMessage.IsCQ(txMsg)*/ && qsoState == WsjtxMessage.QsoStates.CALLING)
                        {
                            Console.WriteLine($"{Time()} Tx enabled: starting queue processing");
                            txTimeout = true;                   //triggers next in queue
                            tCall = null;                       //prevent call removal from queue
                            DateTime dtNow = DateTime.Now;
                            if (txFirst == IsEvenPeriod((dtNow.Second * 1000) + dtNow.Millisecond))
                            {
                                xmitCycleCount = -1;        //Tx enabled during the same period Tx will happen, add one more tx cycle before timeout
                                Console.WriteLine($"     &Tx enabled during Tx period");
                            }
                            else
                            {
                                xmitCycleCount = 0;
                                Console.WriteLine($"     &Tx enabled during Rx period");
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
                            string hStatus = $"Status    qsoState:{qsoState} lastTxMsg: {smsg.LastTxMsg} txEnabled:{txEnabled} tCall:{tCall} TxWatchdog:{smsg.TxWatchdog} Transmitting: {transmitting} Mode: {mode}";
                            Console.WriteLine(hStatus);
                            Play("beepbeep.wav");
                            /*  done in WSJT-X
                            //disable xmit
                            ba = amsg.GetBytes();
                            udpClient2.Send(ba, ba.Length);
                            Console.WriteLine($"{Time()} >>>>>Sent 'Halt Tx' cmd:\n{amsg}");
                            */
                        }
                        lastTxWatchdog = smsg.TxWatchdog;
                    }

                    if (lastDialFrequency != null && (Math.Abs((Int64)lastDialFrequency - (Int64)smsg.DialFrequency) > 500000))
                    {
                        ClearCalls();
                        logList.Clear();            //can re-log on new band
                        Console.WriteLine($"{Time()} Cleared queued calls: DialFrequency");
                        lastDialFrequency = smsg.DialFrequency;
                    }

                    //detect WSJT-X Tx First change
                    if (txFirst != lastTxFirst)
                    {
                        ClearCalls();
                        Console.WriteLine($"{Time()} Cleared queued calls: TxFirst");
                        lastTxFirst = txFirst;
                    }

                    if (myCall == null)
                    {
                        myCall = smsg.DeCall;
                        myGrid = smsg.DeGrid;
                        if (myGrid.Length > 4)
                        {
                            myGrid = myGrid.Substring(0, 4);
                        }
                        Console.WriteLine($"{Time()} myCall: {myCall} myGrid: {myGrid}");
                    }

                    //check for setup for CQ
                    if (supportedModes.Contains(mode) && (mode != "FT8" || specOp == 0) && opMode == OpModes.START)
                    {
                        opMode = OpModes.ACTIVE;
                        ShowStatus();
                        Console.WriteLine($"{Time()} opMode: ACTIVE");
                        ClearAltCallList();
                        //string curStatus = $"Status    qsoState:{qsoState} lastTxMsg: {smsg.LastTxMsg} txEnabled:{txEnabled} txMsg:'{txMsg}' txTimeout:{txTimeout} Transmitting: {transmitting} Mode: {mode}";
                        //Console.WriteLine(curStatus);
                        //setup for CQ
                        emsg.NewTxMsgIdx = 6;
                        emsg.GenMsg = $"CQ{NextDirCq()} {myCall} {myGrid}";
                        emsg.SkipGrid = ctrl.skipGridCheckBox.Checked;
                        emsg.UseRR73 = ctrl.useRR73CheckBox.Checked;
                        ba = emsg.GetBytes();           //re-enable Tx for CQ
                        udpClient2.Send(ba, ba.Length);
                        Console.WriteLine($"{Time()} >>>>>Sent 'Setup CQ' cmd:\n{emsg}");
                        qsoState = WsjtxMessage.QsoStates.CALLING;      //in case enqueueing call manually right now
                        Console.WriteLine($"{Time()} qsoState: {qsoState} (was {lastQsoState})");
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
            Console.WriteLine($"\n{Time()} StartTimer2: interval:{ctrl.timer2.Interval} msec");
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
                Console.WriteLine($"{Time()} CheckNextXmit start, txTimeout:{txTimeout} replyCmd:'{replyCmd}' tCall:{tCall}");   //tempOnly
                //check for call sign in process timed out and must be removed;
                //dictionary won't contain data for this call sign if QSO handled only by WSJT-X
                if (txTimeout && tCall != null)     //null if call added manually, and must be processed below
                {
                    Console.WriteLine($"     @{tCall} might be in process in WSJT-X but timed out");       //tempOnly
                    RemoveCall(tCall);  //tempOnly
                }

                //process the next call in the queue. if any present
                if (callQueue.Count > 0)            //have queued call signs
                {
                    DecodeMessage dmsg = new DecodeMessage();
                    string nCall = GetNextCall(out dmsg);
                    Console.WriteLine($"     @Have entries in queue, got {nCall}");       //tempOnly

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
                    Console.WriteLine($"{Time()} >>>>>Sent 'Reply To Msg' cmd:\n{rmsg} lastTxMsg:'{lastTxMsg}'\nreplyCmd:'{replyCmd}'");
                }
                else            //no queued call signs, start CQing
                {
                    Console.WriteLine($"     @No entries in queue, start CQing");       //tempOnly
                    emsg.NewTxMsgIdx = 6;
                    emsg.GenMsg = $"CQ{NextDirCq()} {myCall} {myGrid}";
                    emsg.SkipGrid = ctrl.skipGridCheckBox.Checked;
                    emsg.UseRR73 = ctrl.useRR73CheckBox.Checked;
                    ba = emsg.GetBytes();           //set up for CQ, auto, call 1st
                    udpClient2.Send(ba, ba.Length);
                    Console.WriteLine($"{Time()} >>>>>Sent 'Setup CQ' cmd:\n{emsg}");
                    qsoState = WsjtxMessage.QsoStates.CALLING;      //in case enqueueing call manually right now
                    replyCmd = null;        //invalidate last reply cmd since not replying
                    replyDecode = null;
                    Console.WriteLine($"{Time()} qsoState: {qsoState} (was {lastQsoState} replyCmd:'{replyCmd}')");
                    lastQsoState = qsoState;
                }
                txTimeout = false;              //ready for next timeout
                qsoLogged = false;              //clear "logged" status display
                Console.WriteLine($"{Time()} CheckNextXmit end, txTimeout:{txTimeout} replyCmd:'{replyCmd}' qsoLogged:{qsoLogged}");
                ShowStatus();
            }
            UpdateDebug();
        }

        public void processDecodes()
        {
            Console.WriteLine($"\n{Time()} Timer2 tick: txEnabled: {txEnabled} txTimeout:{txTimeout}");
            if (ctrl == null) Console.WriteLine($"ERROR: ctrl is null");            //tempOnly
            if (ctrl.timer2 == null) Console.WriteLine($"ERROR: timer2 is null");   //tempOnly
            ctrl.timer2.Stop();
            Console.WriteLine($"     +timer2 stopped");                             //tempOnly
            if (txEnabled) CheckNextXmit();
            Console.WriteLine($"{Time()} Timer2 tick done\n");
        }

        //check for time to log (best done at Tx start to avoid any logging/dequeueing timing problem if done at Tx end)
        private void processTxStart()
        {
            string toCall = WsjtxMessage.ToCall(txMsg);
            string lastToCall = WsjtxMessage.ToCall(lastTxMsg);
            Console.WriteLine($"{Time()} Tx start: txMsg:'{txMsg}' lastTxMsg:'{lastTxMsg}' toCall:'{toCall}' lastToCall:'{lastToCall} qsoLogged:{qsoLogged}'");
            //check for WSJT-X ready to respond to a 73/RR73 that is not from the last Tx 'to'
            // msg to be sent is 73               msg to be sent 'to'      is not  replyCmd "from' (i.e.: WSJT-X ignored last reply cmd)
            if (WsjtxMessage.Is73orRR73(txMsg) && replyCmd != null && WsjtxMessage.ToCall(txMsg) != WsjtxMessage.DeCall(replyCmd))
            {
                //log the unexpected call sign
                LogQso(WsjtxMessage.ToCall(txMsg));
                qsoLogged = true;
                ShowStatus();
                Console.WriteLine($"     ~unexpected logging reqd: toCall:{WsjtxMessage.ToCall(txMsg)} qsoLogged:{qsoLogged}");
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
                Console.WriteLine($"     ~early logging reqd: toCall:{toCall} qsoLogged:{qsoLogged}");
            }

            //check for QSO complete, normal logging
            // correct cur and prev     prev Tx was a RRR                 or prev Tx was a R+XX                    or prev Tx was a +XX                 and cur Tx was 73
            if (toCall == lastToCall && (WsjtxMessage.IsRogers(lastTxMsg) || WsjtxMessage.IsRogerReport(lastTxMsg) || WsjtxMessage.IsReport(lastTxMsg)) && WsjtxMessage.Is73orRR73(txMsg))
            {
                Console.WriteLine($"     ~is 73, was RRR/R+XX, qsoLogged:{qsoLogged}");
                if (!qsoLogged)
                {
                    LogQso(toCall);
                    qsoLogged = true;
                    ShowStatus();
                    Console.WriteLine($"     ~normal logging reqd: toCall:{toCall} qsoLogged:{qsoLogged}");
                }
            }
            Console.WriteLine($"{Time()} Tx start done: txMsg:'{txMsg}' lastTxMsg:'{lastTxMsg}' toCall:'{toCall}' lastToCall:'{lastToCall} qsoLogged:{qsoLogged}'\n");
        }

        //check for QSO end or timeout (and possibly logging (if txMsg changed between TX start and Tx end)
        private void processTxEnd()
        {
            string toCall = WsjtxMessage.ToCall(txMsg);
            string lastToCall = WsjtxMessage.ToCall(lastTxMsg);
            Console.WriteLine($"\n{Time()} Tx end: xmitCycleCount: {xmitCycleCount} txMsg:'{txMsg}' lastTxMsg:'{lastTxMsg}' tCall: {tCall}\n          toCall:'{toCall}' lastToCall:'{lastToCall} qsoLogged:{qsoLogged}'");

            //check for WSJT-X processing a call other than last cmd
            string deCall = WsjtxMessage.DeCall(replyCmd);
            if (replyCmd != null && txMsg != null && toCall != deCall)
            {
                replyCmd = null;        //last reply cmd sent is no longer in effect
                replyDecode = null;
                xmitCycleCount = 0;     //stop any timeout, since new call
                Console.WriteLine($"     #Call selected manually in WSJT-X: invalidated replyCmd:'{replyCmd}' reset xmitCycleCount:{xmitCycleCount} txTimeout:{txTimeout}");
            }

            //check for time to log early; NOTE: doing this at Tx end because WSJT-X may have changed Tx msgs (between Tx start and Tx end) due to late decode for the current call
            //  option enabled                   correct cur and prev    just sent RRR                and previously sent +XX
            if (ctrl.logEarlyCheckBox.Checked && !qsoLogged && toCall == lastToCall && WsjtxMessage.IsRogers(txMsg) && WsjtxMessage.IsReport(lastTxMsg))
            {
                LogQso(toCall);
                qsoLogged = true;
                ShowStatus();
                Console.WriteLine($"     #early logging reqd: toCall:{toCall} qsoLogged:{qsoLogged}");
            }
            //check for QSO complete, trigger next call in the queue
            // correct cur and prev     prev Tx was a RRR                 or prev Tx was a R+XX                    or prev Tx was a +XX                 and cur Tx was 73
            if (toCall == lastToCall && (WsjtxMessage.IsRogers(lastTxMsg) || WsjtxMessage.IsRogerReport(lastTxMsg) || WsjtxMessage.IsReport(lastTxMsg)) && WsjtxMessage.Is73orRR73(txMsg))
            {
                txTimeout = true;      //timeout to Tx the next call in the queue
                xmitCycleCount = 0;
                Console.WriteLine($"{Time()} Reset(2): (is 73, was RRR/R+XX, have queue entry) xmitCycleCount: {xmitCycleCount} txTimeout:{txTimeout} qsoLogged:{qsoLogged}");
                //NOTE: doing this at Tx end because WSJT-X may have changed Tx msgs (between Tx start and Tx end) due to late decode for the current call
                if (!qsoLogged)
                {
                    LogQso(toCall);
                    qsoLogged = true;
                    ShowStatus();
                    Console.WriteLine($"     #normal logging reqd: toCall:{toCall} qsoLogged:{qsoLogged}");
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
                Console.WriteLine($"{Time()} Reset(1) (different msg) xmitCycleCount: {xmitCycleCount} txMsg:'{txMsg}' lastTxMsg:'{lastTxMsg}'");
            }
            else        //same "to" call as last xmit, count xmit cycles
            {
                if (toCall != "CQ")        //don't count CQ (or non-std) calls
                {
                    xmitCycleCount++;           //count xmits to same call sign at end of xmit cycle
                    Console.WriteLine($"{Time()} (same msg) xmitCycleCount: {xmitCycleCount} txMsg:'{txMsg}' lastTxMsg:'{lastTxMsg}'");
                    if (xmitCycleCount >= (int)ctrl.timeoutNumUpDown.Value - 1)  //n msgs = n-1 diffs
                    {
                        xmitCycleCount = 0;
                        txTimeout = true;
                        tCall = WsjtxMessage.ToCall(lastTxMsg);        //will be null if non-std msg
                        Console.WriteLine($"{Time()} Reset(3) (timeout) xmitCycleCount: {xmitCycleCount} txTimeout:{txTimeout} tCall:{tCall}");
                    }
                }
                else
                {
                    //same CQ or non-std call
                    xmitCycleCount = 0;
                    Console.WriteLine($"{Time()} Reset(4) (no action, CQ or non-std) xmitCycleCount: {xmitCycleCount}");
                }
            }
            
            //check for time to process new directed CQ
            if (toCall == "CQ" && ctrl.directedCheckBox.Checked && ctrl.directedTextBox.Text.Trim().Length > 0)
            {
                xmitCycleCount = 0;
                txTimeout = true;
                Console.WriteLine($"{Time()} Reset(5) (new directed CQ) xmitCycleCount: {xmitCycleCount}");
            }

            Console.WriteLine($"{Time()} Tx end done: xmitCycleCount: {xmitCycleCount} txMsg:'{txMsg}' lastTxMsg:'{lastTxMsg}' tCall: {tCall} qsoLogged:{qsoLogged}\n");
            ShowTimeout();
        }

        private void LogQso(string toCall)
        {
            emsg.NewTxMsgIdx = 5;           //force logging, no QSO phase change
            emsg.GenMsg = $"{toCall} {myCall} 73";
            emsg.SkipGrid = ctrl.skipGridCheckBox.Checked;
            emsg.UseRR73 = ctrl.useRR73CheckBox.Checked;
            ba = emsg.GetBytes();
            udpClient2.Send(ba, ba.Length);
            Console.WriteLine($"{Time()} >>>>>Sent 'Setup logging' cmd:\n{emsg}");
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
                Console.WriteLine($"{Time()} dirCq:{dirCq}");
            }
            return dirCq;
        }
        private void ResetOpMode()
        {
            opMode = OpModes.READY;
            ShowStatus();
            lastMode = null;
            lastXmitting = false;
            lastTxWatchdog = null;
            lastDialFrequency = null;
            trPeriod = null;
            logList.Clear();        //can re-log on new mode
            ShowLogged();
            ClearCalls();
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
                Console.WriteLine($"{Time()} Updated {deCall}: {callQueueString()} {callDictString()}");
                return true;
            }
            Console.WriteLine($"{Time()} Not updated {deCall}: {callQueueString()} {callDictString()}");
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
                    Console.WriteLine("ERROR: queueDict and callDict out of sync");
                    errorDesc = " queueDict out of sync";
                    UpdateDebug();
                    return false;
                }

                ShowQueue();
                Console.WriteLine($"{Time()} Removed {call}: {callQueueString()} {callDictString()}");
                return true;
            }
            Console.WriteLine($"{Time()} Not removed {call}: {callQueueString()} {callDictString()}");
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
                Console.WriteLine($"{Time()} Enqueued {call}: {callQueueString()} {callDictString()}");
                return true;
            }
            Console.WriteLine($"{Time()} Not enqueued {call}: {callQueueString()} {callDictString()}");
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
                Console.WriteLine($"{Time()} Not dequeued: {callQueueString()} {callDictString()}");
                return null;
            }

            string call = callQueue.Dequeue();

            if (!callDict.TryGetValue(call, out dmsg))
            {
                Play("dive.wav");
                Console.WriteLine("ERROR: {nCall} not found");
                errorDesc = "{nCall} not found";
                UpdateDebug();
                return null;
            }

            if (callDict.ContainsKey(call)) callDict.Remove(call);

            if (callDict.Count != callQueue.Count)
            {
                Play("dive.wav");
                Console.WriteLine("ERROR: callDict and queueDict out of sync");
                errorDesc = " callDict out of sync";
                UpdateDebug();
                return null;
            }

            ShowQueue();
            dmsg.Message = dmsg.Message.Replace("73", "  ");            //important, otherwise WSJT-X will not respond
            Console.WriteLine($"{Time()} Dequeued {call}: msg:'{dmsg.Message}' {callQueueString()} {callDictString()}");
            return call;
        }

        private void WriteToDisk(WsjtxMessage msg)
        {
            try
            {
                //File.WriteAllBytes(msg.GetType().Name, msg.Datagram);
            }
            catch (Exception) { }
        }

        private string callQueueString()
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

        private string callDictString()
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
                    ba = emsg.GetBytes();
                    udpClient2.Send(ba, ba.Length);
                    Console.WriteLine($"{Time()} >>>>>Sent 'Setup de-init' cmd:\n{emsg}");
                    Thread.Sleep(500);
                    udpClient2.Close();
                }
                if (udpClient != null) udpClient.Close();     //causes unresolvable "disposed object" problem at EndReceive
            }
            catch (Exception e)         //udpClient might be disposed already
            {
                Console.WriteLine($"{Time()} Error at Closing, udpClient:{udpClient} udpClient2:{udpClient2}");
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

        public static void ReceiveCallback(IAsyncResult ar)
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
                Console.WriteLine($"Error: ReceiveCallback() {err}");
                return;
            }

            //Console.WriteLine($"Received: {receiveString}");
            messageRecd = true;
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

            if (WsjtxMessage.NegoState == WsjtxMessage.NegoStates.FAIL)
            {
                ctrl.statusText.Text = failReason;
                color = System.Drawing.Color.Green;
                return;
            }

            switch ((int)opMode)
            {
                case (int)OpModes.START:            //fall thru
                case (int)OpModes.READY:
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
            //Console.WriteLine($"{Time()} wdTimer ticked");
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

        private void addAltCall(DecodeMessage dmsg)
        {
            if (altListPaused) return;
            if (dmsg.Message.Contains("...")) return;
            if (cQonly && !dmsg.IsCQ()) return;
            if (altCallList.Count > 0 && altCallList[0].Contains("..."))
            {
                ctrl.altListBox.Enabled = true;
                altCallList.Clear();
            }

 
            if (altCallList.Contains(dmsg.Message))         //don't need duplicates, but show latest, in sequence
            {
                //Console.WriteLine($"Before remove1:{dmsg.Message}\n{AltCallListString()}");
                altCallList.Remove(dmsg.Message);     //remove from somewhere in the list
                //Console.WriteLine($"After remove1:{dmsg.Message}\n{AltCallListString()}");
            }
            latestDecodeTime = dmsg.SinceMidnight;

            //Console.WriteLine($"Before add:{dmsg.Message}\n{AltCallListString()}");
            altCallList.Add(dmsg.Message);
            //Console.WriteLine($"After add:{dmsg.Message} \n{AltCallListString()}");

            //shorten list
            if (altCallList.Count > 128)
            {
                altCallList.RemoveAt(0);
                //Console.WriteLine($"After remove2 {dmsg.Message}: \n{AltCallListString()}");
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
            DecodeMessage nmsg;
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

            nmsg = new DecodeMessage();
            nmsg.Mode = rawMode;
            nmsg.SchemaVersion = WsjtxMessage.NegotiatedSchemaVersion;
            nmsg.New = true;
            nmsg.OffAir = false;
            nmsg.Snr = 0;
            nmsg.DeltaTime = 0.0;
            nmsg.DeltaFrequency = 1000;
            nmsg.Id = WsjtxMessage.UniqueId;

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

                //Console.WriteLine($"%Shift/dbl-click on {toCall}");

                nmsg.Message = $"CQ {toCall}";
                nmsg.SinceMidnight = latestDecodeTime + new TimeSpan(0, 0, 0, 0, (int)trPeriod);
                ClearCalls();                       //nothing left to do this tx period
                AddCall(toCall, nmsg);

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

                //Console.WriteLine($"%Dbl-click on {toCall}");
                nmsg.Message = msg;
                nmsg.SinceMidnight = latestDecodeTime + new TimeSpan(0, 0, 0, 0, 0);
                AddCall(deCall, nmsg);

                if (txEnabled && callQueue.Count == 1 && qsoState == WsjtxMessage.QsoStates.CALLING)  //stops CQing, starts the first xmit at next decode/status/heartbeat msg
                {
                    txTimeout = true;
                    tCall = null;                       //prevent call removal from queue
                }
                //heartbeat msg handles starting processing of any queued calls after the first one
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
                ctrl.label9.Text = $"dxCall: {dxCall}";

                string txTo = WsjtxMessage.ToCall(txMsg);
                s = (txTo == "CQ" ? null : txTo);
                ctrl.label12.Text = $"tx to: {s}";
                string inPr = CallInProgress();
                s = (inPr == "CQ" ? null : txTo);
                ctrl.label13.Text = $"in-prog: {s}";

                ctrl.label14.Text = $"qsoState: {qsoState}";
                ctrl.label15.Text = $"log call: {qCall}";

                //ctrl.label9.Text = $"replyTo: {replyCmd}";
                //ctrl.label13.Text = $"txMsg: {txMsg}";

                ctrl.label10.Text = $"t/o: {txTimeout.ToString().Substring(0, 1)}";
                ctrl.label11.Text = $"txFirst: {txFirst.ToString().Substring(0, 1)}";
                ctrl.label16.Text = $"t/o call:{tCall}";
                ctrl.label17.Text = $"Err: {errorDesc}";
            }
            catch (Exception err)
            {
                Console.WriteLine($"ERROR: UpdateDebug: err:{err}");
            }
        }
    }
}
