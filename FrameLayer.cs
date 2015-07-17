using System;
using System.IO;
using System.IO.Ports;
using System.Collections.Generic;
using System.Threading;

	namespace EnOcean {
        public class EnOceanPacket
        {
            public DateTime recieved = DateTime.UtcNow;
            static public EnOceanPacket Parse(IList<byte> data, IList<byte> optData)
            {
                
                return new EnOceanPacket();
            }
        }
		public class EnOceanFrameLayer {
            byte[] u8CRC8Table = {
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
//#define proccrc8(u8CRC, u8Data) (u8CRC8Table[u8CRC ^ u8Data])
            byte proccrc8(byte u8CRC, byte u8Data) {
                return u8CRC8Table[u8CRC ^ u8Data];
            }
            public byte CalcCRC8(IList<byte> data, int len) {
                byte u8CRC = 0;
                for (int i = 0 ; i < len ; i++)
                    u8CRC = proccrc8(u8CRC, data[i]);
                //Console.WriteLine("CRC8 = 0x{0:x}", u8CRC);
                return u8CRC;
            }
			SerialPort serialPort;
			Thread commThreadHandle;
			Boolean commActive;
			object commLock = new object();
			public EnOceanFrameLayer() {
            //                public delegate int Dispatch(EnOceanPacket pkt);
                                    PacketEvent += (EnOceanPacket p) => { Console.WriteLine(" PKT HANDLER"); };


            }
			void commThread() {
				Console.WriteLine("Starting communications thread");
//				byte[] verGet = new byte[] { ZConstants.Z_REQUEST, 0x15};
//				SendFrame(verGet);
				//serialPort.Write(verGet, 0, verGet.Length);
				//serialPort.Write(new byte[] { CalculateFrameChecksum(verGet)},0,1);
				while (commActive) {
					Thread.Sleep(500);
	//				SendFrame(verGet);
				}
				//Console.WriteLine("Bytes avail is : {0}", serialPort.BytesToRead);
				Console.WriteLine("Ending comm thread");
			}
			EventWaitHandle waitSendEvent = new EventWaitHandle(false, EventResetMode.AutoReset);
			int waitSendResult = 0;


			bool SendFrame(byte[] frame) {
/*				lock (commLock) {
					byte[] completeFrame = new byte[frame.Length+3];
					completeFrame[0] = ZConstants.Z_SOF;
					completeFrame[1] = (byte)(frame.Length+1);
					frame.CopyTo(completeFrame, 2);
					completeFrame[completeFrame.Length-1] = CalculateFrameChecksum(completeFrame);
					//= new byte[] { ZConstants.Z_SOF, 3, ZConstants.Z_REQUEST, 0x15};
					serialPort.Write(completeFrame, 0, completeFrame.Length);
					//serialPort.Write(new byte[] { CalculateFrameChecksum(verGet)},0,1);
//					while (1) {
//						ackCount = 
//					}
					if (waitSendEvent.WaitOne(2000) && waitSendResult==1)
						return true;
					return false;
				}
 */
                return false;
			}
			byte CalculateFrameChecksum(byte[] frameData) {
				byte chksum = 0xff;
				for (int i=1; i<frameData.Length-1;i++) {
					chksum ^= frameData[i];
				}
				return chksum;
			}
			public bool Open(string portName) {
				serialPort = new SerialPort(portName, 57600);
				serialPort.DataReceived += new SerialDataReceivedEventHandler(onCommDataReceived);
				serialPort.Open();

				commActive = true;
//				commThreadHandle = new Thread(new ThreadStart(commThread));
//				commThreadHandle.Start();
				return serialPort.IsOpen;
			}
			public bool Close() {
				if (commThreadHandle != null) {
					commActive = false;
					commThreadHandle.Join();
					commThreadHandle = null;
				}
				if (serialPort != null) {
					serialPort.Close();
					serialPort = null;
					return true;
				}
				return false;
			}
//			byte[] receiveBuffer = new byte[256]; // FIXME: Should be higher .. maybe
			List<byte> receiveBuffer = new List<byte>();
			int receiveIdx = 0;
			void onCommDataReceived(
				object sender,
				SerialDataReceivedEventArgs args)
			{
				//try {
					SerialPort sp = (SerialPort)sender;
					//Console.WriteLine("Data ready: {0} bytes", sp.BytesToRead);
					byte[] rBuf = new byte[sp.BytesToRead];
					int bytesRead = sp.Read(rBuf, 0, rBuf.Length);
					receiveBuffer.InsertRange(receiveBuffer.Count, rBuf);
					receiveIdx += bytesRead;
                    Console.WriteLine("Data Received: {0} bytes", bytesRead);
                    foreach (var b in receiveBuffer)
                    {
                        Console.Write("0x{0:X2} ",b);
                    }
                    Console.WriteLine();
                    AGAIN:
                    while (receiveBuffer.Count > 0 && receiveBuffer[0] != 0x55)
                        receiveBuffer.RemoveAt(0);
//                    receiveBuffer.RemoveAt(0);
                    if (receiveBuffer.Count < 6)
                    {
                       // Console.WriteLine(" --- return - not enough data in buffer for a complete frame. : {0}", receiveBuffer.Count);
                        return;
                    }
                    receiveBuffer.RemoveAt(0); // Remove SYNC byte 0x55
                    byte hdrCrc8 = CalcCRC8(receiveBuffer, 4);
                    if (hdrCrc8 != receiveBuffer[4])
                    {
                        Console.WriteLine("CRC ERROR FOR PACKET HDR - or not a sync start\n");
                        goto AGAIN;
                    }
                    UInt16 pktLen = receiveBuffer[0];
                    pktLen *= 256;
                    pktLen += receiveBuffer[1];
                    //Console.WriteLine(" Packet len is {0}", pktLen);
                    Byte optLen = receiveBuffer[2];
                    Byte pktType = receiveBuffer[3];
                    if ((pktLen + optLen + 6) > receiveBuffer.Count)
                    {
                        // Not enough data yet.. push back header..
                        Console.WriteLine(" ABANDON FOR LATER - NOT ENOUGH DATA");
                        receiveBuffer.Insert(0, 0x55);
                        return; 
                    }
                    receiveBuffer.RemoveRange(0, 5); // Remove hdr
                    Byte dtaCrc = CalcCRC8(receiveBuffer, pktLen + optLen);
                    if (dtaCrc == receiveBuffer[optLen + pktLen])
                    {
                       // Console.WriteLine(" ----- MATCH DATA CRC OK");
                        receiveBuffer.RemoveAt(receiveBuffer.Count - 1); // Remove checksum - we have checked it already
                        List<byte> payload = receiveBuffer.GetRange(0, pktLen);
                        List<byte> optPayload =  receiveBuffer.GetRange(pktLen, optLen);
//                        receiveBuffer.GetRange
  //                      receiveBuffer.CopyTo(0, payload, 0, pktLen);
    //                    List<byte> buf = new List<byte>(receiveBuffer);
                        Console.WriteLine("Dispatching validated packet of {0} bytes and {1} bytes", payload.Count, optPayload.Count);
                        EnOceanPacket parsedPkt = EnOceanPacket.Parse(payload, optPayload);
//                        PacketEvent += (EnOceanPacket p) => { Console.WriteLine(" PKT HANDLER"); };
                        if (PacketEvent != null)
                            PacketEvent(parsedPkt);
                        //Dispatch(parsedPkt);
                        //                        receiveBuffer.
                        
                        receiveBuffer.RemoveRange(0, optLen + pktLen );
                        goto AGAIN;
                    }
                    //Console.WriteLine("Data + optData crc8 is 0x{0:x}", dtaCrc);
					//Console.WriteLine("Read : {0}", String.Join(" ", receiveBuffer));
//					if (receiveIdx > 0) {
	//					ProcessReceiveBuffer();
					//}
				//} catch (Exception e) {
				//	Console.WriteLine("Error while processing comm packet: {0}", e.Message);
				//}
			}
            public delegate void PacketEventHandler(EnOceanPacket pkt);
            public event PacketEventHandler PacketEvent;

		}
/*		class ZWavePlugin 
            //: iYesPlugin 
        {
			Object Engine;
			EnOceanFrameLayer zProto;
			Boolean PluginRunning;
			public ZWavePlugin(object engine) {
				Engine = engine;
				Console.WriteLine("ZWave loaded");	
				Name="Experimental Z-Wave plugin";
				PluginId="YesZWavePlugin";
				Version = 0.1m;
				Build = 1;
				Priority = 100;
			}
			public String Name { get; private set; }
			public String PluginId { get; private set; }
			public Decimal Version { get;  private set; }
			public UInt64 Build { get;  private set; }
			public UInt64 Priority { get; private set; }
			public Boolean PreInitialize()
			{
				Console.WriteLine("{0}: PreInitialize", PluginId);
				return true;
			}
			public Boolean PostInitialize()
			{
				Console.WriteLine("{0}: PostInitialize", PluginId);
				return true;
			}
			public void Run()
			{
				PluginRunning = true;
				zProto = new EnOceanFrameLayer();
				zProto.Open("/dev/ttyU0");
				while (PluginRunning) {
					Thread.Sleep(1000);
				}
				zProto.Close();
			}
			public void Stop() {
				PluginRunning = false;
			}
		}
 */
//	}
}
