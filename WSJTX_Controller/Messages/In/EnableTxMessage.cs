//NOTE CAREFULLY: This message class requires the use of a slightly modified WSJT-X program.
//Further information is in the README file.

using WsjtxUdpLib.Messages.Out;
using System;
using System.IO;
using System.Text;

namespace WsjtxUdpLib.Messages
{
    /*
     * Enable Tx     In        16
     *                         Id (unique key)        utf8
     *                         Next Tx Msg Index      UInt32
     *                         Generated Message      utf8
     *
     *      The server requests the client to 
     *      transmit the specfied Tx message
     *      at the start of the next transmission period
     *      using this message.
     */

    public class EnableTxMessage : WsjtxMessage, IWsjtxCommandMessageGenerator
    {
        public UInt32 SchemaVersion { get; set; }
        public string Id { get; set; }
        public int NewTxMsgIdx { get; set; }
        public string GenMsg { get; set; }
        public bool SkipGrid;
        public bool UseRR73;

        /*Public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("EnableTx  ");
            sb.Append($"{Col(newTxMsgIdx, 1, Align.Left)} ");

            return sb.ToString();
        }*/
        public byte[] GetBytes()
        {
            using (MemoryStream m = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(m))
                {
                    writer.Write(WsjtxMessage.MagicNumber);
                    writer.Write(EncodeQUInt32(SchemaVersion));
                    writer.Write(EncodeQUInt32(16));    //msg type
                    writer.Write(EncodeString(Id));
                    writer.Write(EncodeQUInt32((UInt32)NewTxMsgIdx));
                    writer.Write(EncodeString(GenMsg));
                    writer.Write(EncodeBoolean(SkipGrid));
                    writer.Write(EncodeBoolean(UseRR73));
                }
                return m.ToArray();
            }
        }
        public override string ToString() => $"EnableTx  {this.ToCompactLine(nameof(Id))}";
    }
}
