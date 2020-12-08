using System;
using System.Text;

namespace WsjtxUdpLib.Messages.Out
{

    /// <summary>
    /// A .NET type which parses the format of UDP datagrams emitted from WSJT-X on UDP port 2237,
    /// for the Decode message type (the type emitted when WSJT-X decodes an FT8 frame)
    /// </summary>
    public class DecodeMessage : WsjtxMessage
    {
        /*
         * Excerpt from NetworkMessage.hpp in WSJT-X source code:
         * 
         * WSJT-X Message Formats
         * ======================
         *
         * All messages are written or  read using the QDataStream derivatives
         * defined below, note that we are using the default for floating
         * point precision which means all are double precision i.e. 64-bit
         * IEEE format.
         *
         *  Message is big endian format
         *
         *   Header format:
         *
         *      32-bit unsigned integer magic number 0xadbccbda
         *      32-bit unsigned integer schema number
         *
         *   Payload format:
         *
         *      As per  the QDataStream format,  see below for version  used and
         *      here:
         *
         *        http://doc.qt.io/qt-5/datastreamformat.html
         *
         *      for the serialization details for each type, at the time of
         *      writing the above document is for Qt_5_0 format which is buggy
         *      so we use Qt_5_4 format, differences are:
         *
         *      QDateTime:
         *           QDate      qint64    Julian day number
         *           QTime      quint32   Milli-seconds since midnight
         *           timespec   quint8    0=local, 1=UTC, 2=Offset from UTC
         *                                                 (seconds)
         *                                3=time zone
         *           offset     qint32    only present if timespec=2
         *           timezone   several-fields only present if timespec=3
         *
         *      we will avoid using QDateTime fields with time zones for simplicity.
         *
         * Type utf8  is a  utf-8 byte  string formatted  as a  QByteArray for
         * serialization purposes  (currently a quint32 size  followed by size
         * bytes, no terminator is present or counted).
         *
         * The QDataStream format document linked above is not complete for
         * the QByteArray serialization format, it is similar to the QString
         * serialization format in that it differentiates between empty
         * strings and null strings. Empty strings have a length of zero
         * whereas null strings have a length field of 0xffffffff.
         * 
         * Decode        Out       2                      quint32      4 bytes?
         *                         Id (unique key)        utf8         4 bytes, that number of chars, no terminator
         *                         New                    bool         1 byte or bit?
         *                         Time                   QTime        quint32   Milliseconds since midnight (4 bytes?)
         *                         snr                    qint32       4 bytes?
         *                         Delta time (S)         float (serialized as double) 8 bytes
         *                         Delta frequency (Hz)   quint32      4 bytes
         *                         Mode                   utf8         4 bytes, that number of chars, no terminator
         *                         Message                utf8         4 bytes, that number of chars, no terminator
         *                         Low confidence         bool         1 byte or bit?
         *                         Off air                bool         1 byte or bit?
         *
         *      The decode message is sent when  a new decode is completed, in
         *      this case the 'New' field is true. It is also used in response
         *      to  a "Replay"  message where  each  old decode  in the  "Band
         *      activity" window, that  has not been erased, is  sent in order
         *      as a one of these messages  with the 'New' field set to false.
         *      See  the "Replay"  message below  for details  of usage.   Low
         *      confidence decodes are flagged  in protocols where the decoder
         *      has knows that  a decode has a higher  than normal probability
         *      of  being  false, they  should  not  be reported  on  publicly
         *      accessible services  without some attached warning  or further
         *      validation. Off air decodes are those that result from playing
         *      back a .WAV file.
         *      
         * From MessageServer.cpp:

                 case NetworkMessage::Decode:
                 {
                     // unpack message
                     bool is_new {true};
                     QTime time;
                     qint32 snr;
                     float delta_time;
                     quint32 delta_frequency;
                     QByteArray mode;
                     QByteArray message;
                     bool low_confidence {false};
                     bool off_air {false};
                     in >> is_new >> time >> snr >> delta_time >> delta_frequency >> mode >> message >> low_confidence >> off_air;
                     if (check_status (in) != Fail)
                     {
                         Q_EMIT self_->decode (is_new, id, time, snr, delta_time, delta_frequency
                                             , QString::fromUtf8 (mode), QString::fromUtf8 (message)
                                             , low_confidence, off_air);
                     }
                 }
                 break;
         *      
         */

        public int SchemaVersion { get; set; }
        public string Id { get; set; }
        /// <summary>
        /// True when the decode is off-air, false when the decode is from a replay requested from a third-party pplication
        /// </summary>

        private string[] messageWords;
        public bool New { get; set; }
        public TimeSpan SinceMidnight { get; set; }
        public int Snr { get; set; }
        public double DeltaTime { get; set; }
        public int DeltaFrequency { get; set; }
        public string Mode { get; set; }
        //  0     1    2      idx
        //K9AVT K4SV R-03
        //01234567890123456789012345678901234567890 posn
        //          1         2         3         4  
        public string Message { get; set; }
        public bool LowConfidence { get; set; }
        /// <summary>
        ///  True to indicate the decode was derived from a .WAV file playback, false when decoded from an on air reception.
        /// </summary>
        public bool OffAir { get; set; }

        public bool IsCallTo(string myCall)
        {
            return messageWords.Length > 0 && messageWords[0].Contains(myCall);
        }
        public bool Is73()
        {
            return Message.Contains("73");
        }
        public bool IsCQ()
        {
            return messageWords.Length > 1 && messageWords[0].Contains("CQ");
        }
        public string DeCall()
        {
            string s = WsjtxMessage.DeCall(Message);
            if (s == null) return "";
            return s;
        }
        public string Call()
        {
            return messageWords.Length > 0 ? messageWords[0] : "";
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("Decode    ");
            sb.Append($"{Col(SinceMidnight, 8, Align.Left)} ");
            sb.Append($"{Col(Snr, 3, Align.Right)} ");
            sb.Append($"{Col(DeltaFrequency, 4, Align.Right)} ");
            sb.Append($"{Col(DeltaTime, 4, Align.Right)} ");
            sb.Append($"{Col(Mode, 1, Align.Left)} ");
            sb.Append($"{(LowConfidence ? "LC" : "  ")} ");
            sb.Append($"{Col(Message, 20, Align.Left)} ");

            return sb.ToString();
        }

        public static new WsjtxMessage Parse(byte[] message)
        {
            if (!CheckMagicNumber(message))
            {
                return null;
            }

            var decodeMessage = new DecodeMessage();

            int cur = MAGIC_NUMBER_LENGTH;
            decodeMessage.SchemaVersion = DecodeQInt32(message, ref cur);

            var messageType = (MessageType)DecodeQInt32(message, ref cur);

            if (messageType != MessageType.DECODE_MESSAGE_TYPE)
            {
                return null;
            }

            decodeMessage.Id = DecodeString(message, ref cur);
            decodeMessage.New = DecodeBool(message, ref cur);
            decodeMessage.SinceMidnight = DecodeQTime(message, ref cur);
            decodeMessage.Snr = DecodeQInt32(message, ref cur);
            decodeMessage.DeltaTime = DecodeDouble(message, ref cur);
            decodeMessage.DeltaFrequency = DecodeQInt32(message, ref cur);
            decodeMessage.Mode = DecodeString(message, ref cur);
            decodeMessage.Message = DecodeString(message, ref cur);
            decodeMessage.LowConfidence = DecodeBool(message, ref cur);
            decodeMessage.OffAir = DecodeBool(message, ref cur);

            decodeMessage.messageWords = decodeMessage.Message.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            return decodeMessage;
        }
    }
}
