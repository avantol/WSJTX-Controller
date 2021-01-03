﻿using WsjtxUdpLib.Messages.Both;
using System;
using System.Linq;
using System.Net;
using System.Text;

namespace WsjtxUdpLib.Messages.Out
{
    public abstract class WsjtxMessage
    {
        protected const int MAGIC_NUMBER_LENGTH = 4;
        protected static byte[] MagicNumber = { 0xad, 0xbc, 0xcb, 0xda };
        public static string UniqueId = "ExtCtl";
        public static string PgmRevision = "abcdef";
        public static string PgmVersion = "1.0.0";
        public static int PgmSchemaVersion = 3;

        public enum QsoStates
        {
            CALLING,
            REPLYING,
            REPORT,
            ROGER_REPORT,
            ROGERS,
            SIGNOFF,
            INVALID
        }
        public enum NegoStates
        {
            INITIAL,
            FAIL,
            SENT,
            RECD 
        }

        public static int NegotiatedSchemaVersion = 2;
        public static NegoStates NegoState = NegoStates.INITIAL;

        //return the "to" call from the msg in the form "W1AW K1JT FN60"
        //if a CQ return "CQ", if no/invalid/non-std msg, return null
        public static string ToCall(string msg)
        {
            if (msg == null) return null;
            msg = RemoveAngleBrackets(msg);
            string[] words = msg.Split(new char[]{' '}, StringSplitOptions.RemoveEmptyEntries);
            if (words.Count() < 2 || words.Count() > 4 || words[0].Contains("...")) return null;
            if (words[0] == "CQ") return "CQ";
            return words[0];
        }

        public static string DeCall(string msg)
        {
            //return the "from" call from the msg in the form "W1AW K1JT FN60" or "CQ K1JT FN60" or "CQ NA K1JT FN60"
            //if non-std or invalid msg, return null
            if (msg == null) return null;
            msg = RemoveAngleBrackets(msg);
            string[] words = msg.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Count() < 2 || words.Count() > 4) return null;
            if (words[0].Contains("CQ") && words.Count() == 4)      //directed CQ
            {
                return words[2];
            }
            if (words[1].Contains("...")) return null;
            return words[1];
        }

        public static string RemoveAngleBrackets(string s)
        {
            if (s == null) return null;
            s = s.Replace("<", "");
            s = s.Replace(">", "");
            return s;
        }

        //there are grid codes that *contain* "73", so test for *exactly* "73" or "RR73";
        //msgs in the form "W1AW K1JT 73" or "W1AW K1JT RR73";
        //custom 73 msgs are not acceptable
        public static bool Is73orRR73(string msg)
        {
            if (msg == null) return false;
            string[] words = msg.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Count() != 3) return false;
            return (words[2] == "73" || words[2] == "RR73");
        }

        //there are grid codes that *contain* "73", so test for *exactly* "73";
        //msgs in the form "W1AW K1JT 73";
        //custom 73 msgs are not acceptable
        public static bool Is73(string msg)
        {
            if (msg == null) return false;
            string[] words = msg.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Count() != 3) return false;
            return (words[2] == "73");
        }

        public static bool IsCQ(string msg)
        {
            return ToCall(msg) == "CQ";
        }

        //msg in the form "W1AW K1JT RRR"
        public static bool IsRogers(string msg)
        {
            if (msg == null) return false;
            string[] words = msg.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Count() != 3) return false;
            return (words[2] == "RRR");
        }

        //msg in the form "W1AW K1JT R-03" or "W1AW K1JT R+12"
        public static bool IsRogerReport(string msg)
        {
            if (msg == null) return false;
            string[] words = msg.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Count() != 3) return false;
            if (!words[2].Contains("R+") && !words[2].Contains("R-")) return false;
            return (int.TryParse(words[2].Substring(2, 2), out int i));
        }

        //msg in the form "W1AW K1JT -03" or "W1AW K1JT +12"
        public static bool IsReport(string msg)
        {
            if (msg == null) return false;
            string[] words = msg.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Count() != 3) return false;
            if (!words[2].Contains("+") && !words[2].Contains("-")) return false;
            if (words[2].Length != 3) return false;
            return (int.TryParse(words[2].Substring(1, 2), out int i));
        }

        //msg in the form "W1AW K1JT FN62"
        public static bool IsReply(string msg)
        {
            if (msg == null) return false;
            string[] words = msg.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Count() != 3) return false;
            if (IsRogerReport(msg) || IsRogers(msg) || IsCQ(msg) || Is73orRR73(msg)) return false;
            if (words[2].Length != 4) return false;
            int i;
            if (!int.TryParse(words[2].Substring(2, 2), out i)) return false;
            if (int.TryParse(words[2].Substring(0, 1), out i)) return false;
            if (int.TryParse(words[2].Substring(1, 1), out i)) return false;
            return true;
        }

        public static string DirectedTo(string msg)
        {
            //return the "directed to" part of the call from the CQ msg in the form "CQ NA K1JT FN60"
            //if not a directed CQ msg msg, return null
            if (msg == null) return null;
            string[] words = msg.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Count() != 4) return null;
            if (words[0] != "CQ") return null;
            return words[1];
        }

        //msg in the form "WIAW K2JT +03" or "W1AW K1JT R-04"
        //return RST received from DX station as string (without "R");
        //return null if neither a Report or a RogerReport
        public static string RstRecd(string msg)
        {
            if (msg == null) return null;
            if (!IsReport(msg) && !IsRogerReport(msg)) return null;
            string[] words = msg.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            //already know words.Length and validity of numeric value
            return words[2].Replace("R", "");
        }

        public static void Reinit()
        {
            NegotiatedSchemaVersion = 2;
            NegoState = NegoStates.INITIAL;
        }
        
        public static WsjtxMessage Parse(byte[] datagram)
        {
            if (!CheckMagicNumber(datagram))
            {
                return null;
            }

            int cur = MAGIC_NUMBER_LENGTH;

            int schemaVersion = DecodeQInt32(datagram, ref cur);
            var messageType = (MessageType)DecodeQInt32(datagram, ref cur);

            try
            {
                if (schemaVersion == 2 || schemaVersion == 3)
                {
                    WsjtxMessage result;

                    if (messageType == MessageType.HEARTBEAT_MESSAGE_TYPE)
                    {
                        result = HeartbeatMessage.Parse(datagram);
                    }
                    else if (messageType == MessageType.STATUS_MESSAGE_TYPE)
                    {
                        result = StatusMessage.Parse(datagram);
                    }
                    else if (messageType == MessageType.DECODE_MESSAGE_TYPE)
                    {
                        result = DecodeMessage.Parse(datagram);
                    }
                    else if (messageType == MessageType.ENQUEUE_DECODE_MESSAGE_TYPE)
                    {
                        result = EnqueueDecodeMessage.Parse(datagram);
                    }
                    else if (messageType == MessageType.CLEAR_MESSAGE_TYPE)
                    {
                        result = ClearMessage.Parse(datagram);
                    }
                    else if (messageType == MessageType.QSO_LOGGED_MESSAGE_TYPE)
                    {
                        result = QsoLoggedMessage.Parse(datagram);
                    }
                    else if (messageType == MessageType.CLOSE_MESSAGE_TYPE)
                    {
                        result = CloseMessage.Parse(datagram);
                    }
                    else if (messageType == MessageType.WSPR_DECODE_MESSAGE_TYPE)
                    {
                        result = WsprDecodeMessage.Parse(datagram);
                    }
                    else if (messageType == MessageType.LOGGED_ADIF_MESSAGE_TYPE)
                    {
                        result = LoggedAdifMessage.Parse(datagram);
                    }
                    else
                    {
                        result = new UnknownMessage();
                    }
                    return result;
                }
            }
            catch (Exception ex)
            {
                throw new ParseFailureException(messageType, datagram, ex);
            }

            throw new NotImplementedException($"Schema version {schemaVersion}, message type {messageType}");
        }

        private static double RoundToSignificantDigits(double d, int digits)
        {
            if (d == 0)
                return 0;

            double scale = Math.Pow(10, Math.Floor(Math.Log10(Math.Abs(d))) + 1);
            return scale * Math.Round(d / scale, digits);
        }
        protected enum Align
        {
            Left, Right
        }

        protected static string Col(object o, int chars, Align alignment)
        {
            if (o == null)
            {
                return new string(' ', chars);
            }

            if (o is double d)
            {
                string str = RoundToSignificantDigits(d, chars - 1).ToString();
                if (!str.Contains("."))
                {
                    str += ".0";
                }
                return Col(str, chars, alignment);
            }

            string output = o.ToString();

            if (output.Length > chars)
            {
                if (alignment == Align.Left)
                {
                    return output.Substring(0, chars);
                }
                else
                {
                    return output.Substring(output.Length - chars, chars);
                }
            }
            else if (output.Length == chars)
            {
                return output;
            }
            else
            {
                if (alignment == Align.Left)
                {
                    return output + new string(' ', chars - output.Length);
                }
                else
                {
                    return new string(' ', chars - output.Length) + output;
                }
            }
        }

        protected static int DecodeQInt32(byte[] message, ref int cur)
        {
            var result = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(message, cur));
            cur += sizeof(int);
            return result;
        }

        protected static long DecodeQInt64(byte[] message, ref int cur)
        {
            var result = IPAddress.NetworkToHostOrder(BitConverter.ToInt64(message, cur));
            cur += sizeof(long);
            return result;
        }

        protected static byte DecodeQUInt8(byte[] message, ref int cur)
        {
            var result = (byte)message[cur];
            cur += sizeof(byte);
            return result;
        }

        static bool reverseByteOrder = BitConverter.IsLittleEndian;

        protected static uint DecodeQUInt32(byte[] message, ref int cur)
        {
            byte[] digits = message.Skip(cur).Take(sizeof(uint)).ToArray();

            uint result = reverseByteOrder
                ? BitConverter.ToUInt32(message.Skip(cur).Take(sizeof(uint)).Reverse().ToArray(), 0)
                : BitConverter.ToUInt32(message, cur);

            cur += sizeof(uint);
            return result;
        }

        protected static uint? DecodeNullableQUInt32(byte[] message, ref int cur)
        {
            try
            {
                if (IsQUInt32MaxValue(message, cur))
                {
                    return null;
                }

                uint result = reverseByteOrder
                    ? BitConverter.ToUInt32(message.Skip(cur).Take(sizeof(uint)).Reverse().ToArray(), 0)
                    : BitConverter.ToUInt32(message, cur);

                return result;
            }
            finally
            {
                cur += sizeof(uint);
            }
        }

        protected static UInt64 DecodeQUInt64(byte[] message, ref int cur)
        {
            var result = reverseByteOrder
                ? BitConverter.ToUInt64(message.Skip(cur).Take(sizeof(UInt64)).Reverse().ToArray(), 0)
                : BitConverter.ToUInt64(message, cur);

            cur += sizeof(UInt64);
            return result;
        }

        protected static double DecodeDouble(byte[] message, ref int cur)
        {
            double result;
            if (reverseByteOrder)
            {
                // x64
                result = BitConverter.ToDouble(message.Skip(cur).Take(sizeof(double)).Reverse().ToArray(), 0);
            }
            else
            {
                // who knows what platform
                result = BitConverter.ToDouble(message, cur);
            }

            cur += sizeof(double);
            return result;
        }

        protected static bool DecodeBool(byte[] message, ref int cur)
        {
            bool result = message[cur] != 0;
            cur += sizeof(bool);
            return result;
        }

        protected static TimeSpan DecodeQTime(byte[] message, ref int cur)
        {
            return TimeSpan.FromMilliseconds(DecodeQUInt32(message, ref cur));
        }

        protected static string DecodeString(byte[] message, ref int cur)
        {
            if (IsQUInt32MaxValue(message, cur))
            {
                cur += sizeof(uint);
                return null;
            }

            var numBytesInField = DecodeQUInt32(message, ref cur);

            char[] letters = new char[numBytesInField];
            for (int i = 0; i < numBytesInField; i++)
            {
                letters[i] = (char)message[cur + i];
            }

            cur += (int)numBytesInField;

            var str = new string(letters);

            if (string.IsNullOrWhiteSpace(str))
            {
                return null;
            }

            return str;
        }

        protected static DateTime DecodeQDateTimeWithoutTimezone(byte[] message, ref int cur)
        {
            /*
             *       QDateTime:
             *           QDate      qint64    Julian day number
             *           QTime      quint32   Milli-seconds since midnight
             *           timespec   quint8    0=local, 1=UTC, 2=Offset from UTC
             *                                                 (seconds)
             *                                3=time zone
             *           offset     qint32    only present if timespec=2
             *           timezone   several-fields only present if timespec=3
             *
             *      we will avoid using QDateTime fields with time zones for simplicity.
             */

            long julianDay = DecodeQInt64(message, ref cur);
            var sinceMidnightUtc = DecodeQTime(message, ref cur);
            byte timespec = DecodeQUInt8(message, ref cur);

            int offset = 0;
            DateTimeKind kind;

            if (timespec == 0)
            {
                kind = DateTimeKind.Local;
            }
            else if (timespec == 1)
            {
                kind = DateTimeKind.Utc;
            }
            else if (timespec == 2)
            {
                kind = DateTimeKind.Utc;
                offset = DecodeQInt32(message, ref cur);
            }
            else if (timespec == 3)
            {
                throw new NotSupportedException("timespec=3");
            }
            else throw new NotImplementedException($"timespec={timespec}");

            var date = DateTime.SpecifyKind(JulianDayNumberToDateMeeus(julianDay), kind);

            var result = date.Add(sinceMidnightUtc).AddSeconds(offset);

            return result;
        }

        /// <summary>
        /// Converts a Julian day to a calendar DateTime.
        /// https://stackoverflow.com/a/52340035/17971
        /// </summary>
        static DateTime JulianDayNumberToDateMeeus(double jDNum) 
        {
            int alpha, A, B, C, D, E, Z;
            double F;

            jDNum += 0.5;
            Z = (int)jDNum;  // Z == int so I = int part
            F = jDNum - Z;   // F =  fractional part
            
            if (Z < 2299161)
            { 
                //Julian?
                A = Z;
            }
            else
            {  
                //Gregorian
                alpha = (int)Math.Floor((Z - 1867216.25) / 36524.25);
                A = Z + 1 + alpha - (int)Math.Floor(alpha / 4.0);
            }

            B = A + 1524;
            C = (int)Math.Floor((B - 122.1) / 365.25);
            D = (int)Math.Floor(365.25 * C);
            E = (int)Math.Floor((B - D) / 30.6001);

            int day = (int)(B - D - (int)Math.Floor(30.6001 * E) + F);
            int month = E < 14 ? E - 1 : E - 13;
            int year = month > 2 ? C - 4716 : C - 4715;

            return new DateTime(year, month, day);
        }
        
        protected static bool IsQUInt32MaxValue(byte[] message, int cur)
            => message[cur] == 0xff && message[cur + 1] == 0xff && message[cur + 2] == 0xff && message[cur + 3] == 0xff;

        protected static bool CheckMagicNumber(byte[] message) => message.Take(4).SequenceEqual(MagicNumber);

        //public byte[] Datagram { get; set; }

        protected static byte[] EncodeQUInt32(UInt32 i)
        {
            return BitConverter.GetBytes(i).Reverse().ToArray();
        }

        protected static byte[] EncodeQInt32(Int32 i)
        {
            return BitConverter.GetBytes(i).Reverse().ToArray();
        }

        protected static byte[] EncodeString(String s)
        {
            byte[] ba = new byte[s.Length + 4];
            EncodeQUInt32((UInt32)s.Length).CopyTo(ba, 0);
            Encoding.UTF8.GetBytes(s).CopyTo(ba, 4);
            return ba;
        }

        protected static byte EncodeBoolean(bool b)
        {
            return b ? (byte)1 : (byte)0;
        }

        protected static byte[] EncodeQTime(TimeSpan ts)
        {
            return EncodeQUInt32((UInt32)ts.TotalMilliseconds);
        }
        protected static byte[] EncodeDouble(double d)
        {
            return BitConverter.GetBytes(d).Reverse().ToArray();
        }
        
    }
}
