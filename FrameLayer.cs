using System;
using System.IO;
using System.IO.Ports;
//using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace EnOcean
{
    public enum PacketType { RESERVED = 0, RADIO_ERP1 = 1, RESPONSE, RADIO_SUB_TEL, EVENT, COMMON_COMMAND, SMART_ACK_COMMAND, REMOTE_MAN_COMMAND, RESERVED_ENOCEAN, RADIO_MESSAGE, RADIO_ERP2 };
    public enum TelegramType { UNKNOWN=0, TT_4BS = 0xA5, TT_ADT = 0xA6, TT_VLD = 0xC2, TT_1BS = 0xD5}
    public class EnOceanOptionalData
    {
        private IList<byte> list;

        public int getSize()
        {
            return list.Count;
        }
        public EnOceanOptionalData(IList<byte> list)
        {
            this.list = list;
        }
        public PacketType getType()
        {
            if (list == null || list.Count == 0)
                return PacketType.RESERVED;
            return (PacketType)list[0];
        }
        public UInt32 getDestination()
        {
            UInt32 dest = list[1];
            dest = (dest * 256) + list[2];
            dest = (dest * 256) + list[3];
            dest = (dest * 256) + list[4];

            return dest;
        }

    }
    public class EnOceanPacket
    {
        public DateTime recieved = DateTime.UtcNow;
        private IList<byte> data;
        private IList<byte> optData;
        private PacketType type;
        private Byte[] rawPacket;
        public Byte[] GetData()
        {
            var f = new Byte[this.data.Count];
            this.data.CopyTo(f, 0);
            return f;
        }
        public EnOceanOptionalData Get_OptionalData()
        {
            return new EnOceanOptionalData(this.optData);
        }
        public TelegramType getTelegramType()
        {
            if (data == null || data.Count == 0)
                return TelegramType.UNKNOWN;
            return (TelegramType)data[0];
        }
        public PacketType getType()
        {
            return type;

        }
        static public EnOceanPacket MakePacket_CO_RD_VERSION()
        {
            var pkt = new EnOceanPacket(PacketType.COMMON_COMMAND, new byte[] { 0x03 }, null);
            pkt.BuildPacket();
            return pkt;
        }
        public Byte[] BuildPacket()
        {
            if (optData == null)
                optData = new byte[0];
            var ms = new MemoryStream();
            var bw = new BinaryWriter(ms);
            bw.Write((byte)0x55); // Sync byte
            bw.Write((byte)(data.Count >> 8));
            bw.Write((byte)(data.Count & 0xFF));
            bw.Write((byte)optData.Count); // optData length
            bw.Write((byte)type); // Packet Type
            bw.Write(EnOceanChecksum.CalcCRC8(ms.GetBuffer(), 4, 1));
            foreach (var b in data)
                bw.Write(b);
            foreach (var b in optData)
                bw.Write(b);
            bw.Write(EnOceanChecksum.CalcCRC8(ms.GetBuffer(), (int)(ms.Length - 6), 6));
            this.rawPacket = ms.GetBuffer();
            Array.Resize<byte>(ref rawPacket, (int)ms.Length);
            return this.rawPacket;
        }
        public EnOceanPacket(byte pkt_type, IList<byte> data, IList<byte> optData)
        {
            this.data = data;
            this.optData = optData;
            this.type = (PacketType)pkt_type;
        }
        public EnOceanPacket(PacketType pkt_type, IList<byte> data, IList<byte> optData)
        {
            this.data = data;
            this.optData = optData;
            this.type = pkt_type;
        }
        static public EnOceanPacket Parse(byte pkt_type, IList<byte> data, IList<byte> optData)
        {
            return new EnOceanPacket(pkt_type, data, optData);
        }

        internal UInt32 getSource()
        {
            int srcPos = 1;
            if (getTelegramType() == TelegramType.TT_4BS)
                srcPos = 5;
            UInt32 src = data[srcPos];
            src = (src * 256) + data[srcPos+1];
            src = (src * 256) + data[srcPos+2];
            src = (src * 256) + data[srcPos+3];

            return src;
        }
    }
    public class EnOceanChecksum
    {
        static byte[] u8CRC8Table = {
0x00, 0x07, 0x0e, 0x09, 0x1c, 0x1b, 0x12, 0x15,
0x38, 0x3f, 0x36, 0x31, 0x24, 0x23, 0x2a, 0x2d,
0x70, 0x77, 0x7e, 0x79, 0x6c, 0x6b, 0x62, 0x65,
0x48, 0x4f, 0x46, 0x41, 0x54, 0x53, 0x5a, 0x5d,
0xe0, 0xe7, 0xee, 0xe9, 0xfc, 0xfb, 0xf2, 0xf5,
0xd8, 0xdf, 0xd6, 0xd1, 0xc4, 0xc3, 0xca, 0xcd,
0x90, 0x97, 0x9e, 0x99, 0x8c, 0x8b, 0x82, 0x85,
0xa8, 0xaf, 0xa6, 0xa1, 0xb4, 0xb3, 0xba, 0xbd,
0xc7, 0xc0, 0xc9, 0xce, 0xdb, 0xdc, 0xd5, 0xd2,
0xff, 0xf8, 0xf1, 0xf6, 0xe3, 0xe4, 0xed, 0xea,
0xb7, 0xb0, 0xb9, 0xbe, 0xab, 0xac, 0xa5, 0xa2,
0x8f, 0x88, 0x81, 0x86, 0x93, 0x94, 0x9d, 0x9a,
0x27, 0x20, 0x29, 0x2e, 0x3b, 0x3c, 0x35, 0x32,
0x1f, 0x18, 0x11, 0x16, 0x03, 0x04, 0x0d, 0x0a,
0x57, 0x50, 0x59, 0x5e, 0x4b, 0x4c, 0x45, 0x42,
0x6f, 0x68, 0x61, 0x66, 0x73, 0x74, 0x7d, 0x7a,
0x89, 0x8e, 0x87, 0x80, 0x95, 0x92, 0x9b, 0x9c,
0xb1, 0xb6, 0xbf, 0xb8, 0xad, 0xaa, 0xa3, 0xa4,
0xf9, 0xfe, 0xf7, 0xf0, 0xe5, 0xe2, 0xeb, 0xec,
0xc1, 0xc6, 0xcf, 0xc8, 0xdd, 0xda, 0xd3, 0xd4,
0x69, 0x6e, 0x67, 0x60, 0x75, 0x72, 0x7b, 0x7c,
0x51, 0x56, 0x5f, 0x58, 0x4d, 0x4a, 0x43, 0x44,
0x19, 0x1e, 0x17, 0x10, 0x05, 0x02, 0x0b, 0x0c,
0x21, 0x26, 0x2f, 0x28, 0x3d, 0x3a, 0x33, 0x34,
0x4e, 0x49, 0x40, 0x47, 0x52, 0x55, 0x5c, 0x5b,
0x76, 0x71, 0x78, 0x7f, 0x6A, 0x6d, 0x64, 0x63,
0x3e, 0x39, 0x30, 0x37, 0x22, 0x25, 0x2c, 0x2b,
0x06, 0x01, 0x08, 0x0f, 0x1a, 0x1d, 0x14, 0x13,
0xae, 0xa9, 0xa0, 0xa7, 0xb2, 0xb5, 0xbc, 0xbb,
0x96, 0x91, 0x98, 0x9f, 0x8a, 0x8D, 0x84, 0x83,
0xde, 0xd9, 0xd0, 0xd7, 0xc2, 0xc5, 0xcc, 0xcb,
0xe6, 0xe1, 0xe8, 0xef, 0xfa, 0xfd, 0xf4, 0xf3
};
        static byte proccrc8(byte u8CRC, byte u8Data)
        {
            return u8CRC8Table[u8CRC ^ u8Data];
        }
        static public byte CalcCRC8(IList<byte> data, int len = 0, int offset = 0)
        {
            if (len == 0)
                len = data.Count - offset;
            byte u8CRC = 0;
            for (int i = offset; i < offset + len; i++)
                u8CRC = proccrc8(u8CRC, data[i]);
            //Console.WriteLine("CRC8 = 0x{0:x}", u8CRC);
            return u8CRC;
        }

    }
    public class EnOceanFrameLayer : IDisposable
    {
        SerialPort serialPort;
        Thread commThreadHandle;
        Boolean commActive;
        ConcurrentQueue<EnOceanPacket> rxPacketQueue = new ConcurrentQueue<EnOceanPacket>();
        EventWaitHandle rxCommThreadWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);
        object commLock = new object();

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // dispose managed resources
                serialPort.Close();
                serialPort = null;
            }
            // free native resources
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public EnOceanFrameLayer()
        {
//                Queue.Synchronized()
//            Queue<PacketEvent>
//            PacketEventHandler += (EnOceanPacket p) => { Console.WriteLine(" PKT HANDLER"); }; // TESTING
            PacketEventHandler += (EnOceanPacket p) =>
            {
//                Console.WriteLine(" PKT HANDLER 2");
                foreach (var listener in PacketListeners)
                {
                    if (listener.handler(p))
                    {
                        listener.succeeded = true;
                        listener.waitHandle.Set();
                    }
                }
            };


        }
        void commThread()
        {
            Console.WriteLine("Starting communications thread");
            while (commActive)
            {
                if (rxCommThreadWaitHandle.WaitOne(250))
                {
                AGAIN:
                    EnOceanPacket qp;
                    if (rxPacketQueue.TryDequeue(out qp))
                    {
                        if (PacketEventHandler != null)
                            PacketEventHandler(qp);
                        goto AGAIN;
                    }

                }
                //				SendFrame(verGet);
            }
            Console.WriteLine("Ending comm thread");
        }
        public class PacketListener : IDisposable
        {
            public PacketListener(IReceiveHandler pHandler)
            {
                this.handler = pHandler;
                succeeded = false;
                packet = null;
                waitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);
            }
            protected virtual void Dispose(bool disposing)
            {
                if (disposing)
                {
                    // dispose managed resources
                    waitHandle.Close();
                    waitHandle = null;
                }
                // free native resources
            }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
            public IReceiveHandler handler;
            public Boolean succeeded;
            public EnOceanPacket packet;
            public EventWaitHandle waitHandle;
        }
        List<PacketListener> PacketListeners = new List<PacketListener>();
        public delegate bool IReceiveHandler(EnOceanPacket packet);
        public bool Send(EnOceanPacket packet, IReceiveHandler handler, int retries = 3, int timeout = 1000)
        {
            var rawPacket = packet.BuildPacket();
            var pl = new PacketListener(handler);
            PacketListeners.Add(pl);
        AGAIN:
            // TESTING
            SendFrame(rawPacket);
            pl.waitHandle.WaitOne(timeout);
            if (pl.succeeded)
            {
                PacketListeners.Remove(pl);
                return true;
            }
            else
            {
                if (retries-- > 0)
                    goto AGAIN;
                PacketListeners.Remove(pl);
                return false;
                // Packet not what he wanted.. wait for next!
            }
        }
        public bool SendFrame(byte[] frame)
        {
            serialPort.Write(frame, 0, frame.Length);
            return false;
        }
        byte CalculateFrameChecksum(byte[] frameData)
        {
            byte chksum = 0xff;
            for (int i = 1; i < frameData.Length - 1; i++)
            {
                chksum ^= frameData[i];
            }
            return chksum;
        }
        public bool Open(string portName)
        {
            serialPort = new SerialPort(portName, 57600);
            serialPort.DataReceived += new SerialDataReceivedEventHandler(onCommDataReceived);
            try
            {
                serialPort.Open();
                commThreadHandle = new Thread(new ThreadStart(commThread));
                commThreadHandle.Start();
            }
            catch (Exception e)
            {
                Console.WriteLine("Error opening port: {0}", e);
                commActive = false;
                return false;

            }

            commActive = true;
            return serialPort.IsOpen;
        }
        public bool Close()
        {
            if (commThreadHandle != null)
            {
                commActive = false;
                commThreadHandle.Join();
                commThreadHandle = null;
            }
            if (serialPort != null)
            {
                serialPort.Close();
                serialPort = null;
                return true;
            }
            return false;
        }
        List<byte> receiveBuffer = new List<byte>();
        int receiveIdx = 0;
        void onCommDataReceived(
            object sender,
            SerialDataReceivedEventArgs args)
        {
            SerialPort sp = (SerialPort)sender;
            byte[] rBuf = new byte[sp.BytesToRead];
            int bytesRead = sp.Read(rBuf, 0, rBuf.Length);
            receiveBuffer.InsertRange(receiveBuffer.Count, rBuf);
            receiveIdx += bytesRead;
/*            Console.WriteLine("Data Received: {0} bytes", bytesRead);
            foreach (var b in receiveBuffer)
            {
                Console.Write("0x{0:X2} ", b);
            }
            Console.WriteLine();
 */
        AGAIN:
            while (receiveBuffer.Count > 0 && receiveBuffer[0] != 0x55)
                receiveBuffer.RemoveAt(0);
            if (receiveBuffer.Count < 6)
            {
                return;
            }
            receiveBuffer.RemoveAt(0); // Remove SYNC byte 0x55
            byte hdrCrc8 = EnOceanChecksum.CalcCRC8(receiveBuffer, 4);
            if (hdrCrc8 != receiveBuffer[4])
            {
                Console.WriteLine("CRC ERROR FOR PACKET HDR - or not a sync start\n");
                goto AGAIN;
            }
            UInt16 pktLen = receiveBuffer[0];
            pktLen *= 256;
            pktLen += receiveBuffer[1];
            Byte optLen = receiveBuffer[2];
            Byte pktType = receiveBuffer[3];
            if ((pktLen + optLen + 6) > receiveBuffer.Count)
            {
                // Not enough data yet.. push back header..
                Console.WriteLine(" ABANDON FOR LATER - NOT ENOUGH DATA");
                receiveBuffer.Insert(0, 0x55);
                return;
            }
            List<byte> pktHdr = receiveBuffer.GetRange(0, 5);
            receiveBuffer.RemoveRange(0, 5); // Remove hdr
            Byte dtaCrc = EnOceanChecksum.CalcCRC8(receiveBuffer, pktLen + optLen);
            if (dtaCrc == receiveBuffer[optLen + pktLen])
            {
                // Console.WriteLine(" ----- MATCH DATA CRC OK");
                receiveBuffer.RemoveAt(receiveBuffer.Count - 1); // Remove checksum - we have checked it already
                List<byte> payload = receiveBuffer.GetRange(0, pktLen);
                List<byte> optPayload = receiveBuffer.GetRange(pktLen, optLen);
                Console.WriteLine("Dispatching validated packet of {0} bytes and {1} bytes", payload.Count, optPayload.Count);
                EnOceanPacket parsedPkt = EnOceanPacket.Parse(pktHdr[3], payload, optPayload);
                rxPacketQueue.Enqueue(parsedPkt);
                rxCommThreadWaitHandle.Set(); // Notify rx thread
                receiveBuffer.RemoveRange(0, optLen + pktLen);
                goto AGAIN;
            }
        }
        public delegate void PacketEvent(EnOceanPacket pkt);
        public PacketEvent PacketEventHandler;

    }
}
