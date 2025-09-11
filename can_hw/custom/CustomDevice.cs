using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using LibUsbDotNet;
using LibUsbDotNet.Main;

namespace can_hw.custom
{
    namespace Events
    {
        public delegate void DataReceiveEventHandler(object sender, DataReceiveEventArgs e);
        public class DataReceiveEventArgs : EventArgs
        {
            public readonly byte[] buf;
            public DataReceiveEventArgs(byte[] buf, int len)
            {
                this.buf = new byte[len];
                for (int i = 0; i < len; i++) {
                    this.buf[i] = buf[i];
                }
            }
        }

        public delegate void CanReceiveEventHandler(object sender, CanReceiveEventArgs e);
        public class CanReceiveEventArgs : EventArgs
        {
            public readonly UInt32 timestamp_10us;
            public readonly Packet.frame_type_t frame_type;
            public readonly byte channel;
            public readonly UInt32 msgId;
            public readonly byte dlc;
            public readonly byte[] buf;
            public CanReceiveEventArgs(UInt32 timestamp_10us, Packet.frame_type_t frame_type, byte channel, UInt32 msgId, byte dlc, byte[] payload)
            {
                this.timestamp_10us = timestamp_10us;
                this.frame_type = frame_type;
                this.channel = channel;
                this.msgId = msgId;
                this.dlc = dlc;
                this.buf = new byte[payload.Length];
                payload.CopyTo(this.buf, 0);
            }
        }

        public delegate void DeviceConnectEventHandler(object sender, DeviceConnectEventArgs e);
        public class DeviceConnectEventArgs : EventArgs
        {
            public readonly bool IsConnected;
            public DeviceConnectEventArgs(bool isConnected)
            {
                this.IsConnected = isConnected;
            }
        }

    }

    public enum deviceState
    {
        STOPPED = 0,
        WAITING_CONNECTION,
        CONNECTED
    }

    public enum deviceChannel
    {
        CH0 = 0,
        CH1,
        CH2
    }

    public class CustomDevice
    {
        #region Members
        private int bufferSize = 2048;
        private bool running;
        private bool interfaceClaimed = false;
        private bool device_connected;
        private UsbDevice MyUsbDevice;
        private UsbEndpointReader epReader;
        private UsbEndpointWriter epWriter;
        private ErrorCode ec = ErrorCode.None;
        private Thread USB_receive;
        private byte[] readBuffer;
        private int readBuffer_wrIdx;
        private int readBuffer_rdIdx;
        private Thread USB_connect;
        private byte[] writeBuffer;
        private int vid;
        private int pid;
        private string sn;
        public deviceState state { get; private set; }
        public List<string> DevSerialList { get; private set; }
        public event Events.DataReceiveEventHandler EventDataReceive;
        public event Events.CanReceiveEventHandler EventCanReceive;
        public event Events.DeviceConnectEventHandler EventDeviceConnect;

        #endregion // Members

        #region Method
        #region Constructor
        public CustomDevice(int vid, int pid)
        {
            this.running = false;
            this.device_connected = false;
            this.vid = vid;
            this.pid = pid;
            this.MyUsbDevice = null;
            this.readBuffer = new byte[this.bufferSize];
            this.readBuffer_rdIdx = 0;
            this.readBuffer_wrIdx = 0;
            this.writeBuffer = new byte[this.bufferSize];
            this.state = deviceState.STOPPED;
            this.ListDevice();
        }
        #endregion // Constructor
        public void Connect(string serialNumber)
        {
            if (!this.running) {
                try {
                    this.sn = serialNumber;
                    USB_connect = new Thread(USB_connection);
                    this.running = true;
                    this.readBuffer_rdIdx = 0;
                    this.readBuffer_wrIdx = 0;
                    USB_connect.Start();
                } catch (Exception ex) {
                    Console.WriteLine(ex.Message);
                    this.running = false;
                }
            }
        }
        public void Disconnect()
        {
            this.running = false;
            this.device_connected = false;
            Thread.Sleep(100);
            this.Close();
            this.state = deviceState.STOPPED;
        }
        public void Exit()
        {
            this.Disconnect();
            UsbDevice.Exit();
        }
        public void ListDevice()
        {
            UsbRegDeviceList usbRegistryDevices = UsbDevice.AllDevices.FindAll(new UsbDeviceFinder(this.vid, this.pid));
            this.DevSerialList = new List<string>();
            if (usbRegistryDevices.Count == 0) {
                Console.WriteLine("No Device Found!");
                return;
            }
            for (int i = 0; i < usbRegistryDevices.Count; i++) {
                if (usbRegistryDevices[i].Device != null) {
                    Console.WriteLine("Dev" + i + " : " + usbRegistryDevices[i].Device.UsbRegistryInfo.Device.Info.SerialString);
                    this.DevSerialList.Add(usbRegistryDevices[i].Device?.UsbRegistryInfo.Device.Info.SerialString);
                }
            }
            Console.WriteLine(this.DevSerialList.Count + " Device found.");
        }
        public bool Send(deviceChannel ch, UInt16 msgId, bool bFd, byte[] payload)
        {
            int bytesWritten = 0;
            if (this.interfaceClaimed != true) {
                throw new InvalidOperationException("Error: Cannot send using unclaimed interface!");
            }
            if(msgId > 0x7FF) {
                throw new ArgumentException("Error: supports only 11-bit ID!");
            }
            if (bFd) {
                if(payload.Length > 64) {
                    throw new ArgumentException("Error: Greater than CAN-FD max payload length of 64!");
                }
            } else {
                if(payload.Length > 8) {
                    throw new ArgumentException("Error: Greater than CAN-CC max payload length of 8!");
                }
            }

            Packet pkt = new Packet(ch, 0, msgId, bFd, payload);
            pkt.GetFrame().CopyTo(writeBuffer, 0);
            ErrorCode errCode = this.epWriter.Transfer(this.writeBuffer, 0, pkt.GetFrame().Length, 5000, out bytesWritten);
            if (errCode == ErrorCode.None) {
                return true;
            } else {
                Console.WriteLine("Send Failed! " + errCode);
                return false;
            }
        }
        private void USB_connection()
        {
            this.state = deviceState.WAITING_CONNECTION;
            Console.WriteLine("Waiting for device connection");

            while (this.running == true) {
                if (this.device_connected == false) {
                    while (( this.MyUsbDevice == null ) && ( this.running == true )) {
                        Thread.Sleep(200);
                        UsbRegDeviceList usbRegistryDevices = UsbDevice.AllDevices.FindAll(new UsbDeviceFinder(this.vid, this.pid));
                        foreach(UsbRegistry regDevice in usbRegistryDevices) {
                            if(regDevice.Device != null) {
                                if((regDevice.Vid == this.vid) && (regDevice.Pid == this.pid) &&
                                   (regDevice.Device.Info.SerialString == this.sn)) {
                                    this.MyUsbDevice = regDevice.Device;
                                    Console.WriteLine("Name: " + this.MyUsbDevice.UsbRegistryInfo.Name);
                                    foreach(var usbConfig in regDevice.Device.Configs) {
                                        Console.WriteLine("****************************************************");
                                        Console.WriteLine("ConfigID: " + usbConfig.Descriptor.ConfigID + ", Attributes: 0x" + usbConfig.Descriptor.Attributes.ToString("X2"));
                                        foreach(var usbInterface in usbConfig.InterfaceInfoList) {
                                            Console.WriteLine("InterfaceID: " + usbInterface.Descriptor.InterfaceID);
                                            Console.WriteLine("Interface: " + usbInterface.InterfaceString);
                                            foreach(var usbEndpoint in usbInterface.EndpointInfoList) {
                                                Console.WriteLine("EP ID: 0x" + usbEndpoint.Descriptor.EndpointID.ToString("X2"));
                                            }
                                        }
                                    }
                                    break;
                                }
                            }
                        }
                    }
                    if (this.running == true) {
                        try {
                            this.USB_init();
                            this.device_connected = true;
                            this.state = deviceState.CONNECTED;
                            Console.WriteLine("Device Connected");
                        } catch (Exception ex) {
                            Console.WriteLine(ex.Message);
                        }
                    }
                }
                Thread.Sleep(100);
            }

            this.state = deviceState.STOPPED;
        }
        private void USB_init()
        {
            IUsbDevice wholeUsbDevice = this.MyUsbDevice as IUsbDevice;
            if (!ReferenceEquals(wholeUsbDevice, null)) {
                // Select Config #1
                if(!wholeUsbDevice.SetConfiguration(1)) {
                    Console.WriteLine("Failed to set USB configuration");
                    return;
                }
                // Claim Interface #0
                if (wholeUsbDevice.ClaimInterface(3)) {
                    Console.WriteLine("Interface(3) claimed.");
                    this.interfaceClaimed = true;
                } else {
                    Console.WriteLine("Failed to claim USB interface");
                    return;
                }
                this.epReader = this.MyUsbDevice.OpenEndpointReader(ReadEndpointID.Ep04);
                this.epWriter = this.MyUsbDevice.OpenEndpointWriter(WriteEndpointID.Ep04);
                this.USB_receive = new Thread(USBReceive);
                this.USB_receive.Start();
            }
        }
        private void USBReceive()
        {
            try {
                while (this.device_connected && ( ( this.ec == ErrorCode.None ) || ( this.ec == ErrorCode.IoTimedOut ) )) {
                    int bytesRead = 0;
                    byte[] usbReadBuf = new byte[512];
                    this.ec = this.epReader.Transfer(usbReadBuf, 0, usbReadBuf.Length, 500, out bytesRead);
                    if (bytesRead == 0) {
                        continue;
                    }
                    lock (USB_receive) {
                        int availableBytes = 0;
                        UInt16 length = 0;
                        int idx = 0;
                        int sum = 0;

                        for (int i = 0; i < bytesRead; i++) {
                            this.readBuffer[readBuffer_wrIdx] = usbReadBuf[i];
                            readBuffer_wrIdx = ( readBuffer_wrIdx + 1 ) % this.readBuffer.Length;
                        }

                        if (this.running && ( this.ec == ErrorCode.None )) {
                            if (this.EventDataReceive != null) {
                                this.EventDataReceive(this, new Events.DataReceiveEventArgs(usbReadBuf, bytesRead));
                            }
                        }

                        /* Parse */
                        while(readBuffer_rdIdx != readBuffer_wrIdx) {
                            if(readBuffer[readBuffer_rdIdx] != Packet.SOF) {
                                /* Not SOF, skip byte */
                                readBuffer_rdIdx = ( readBuffer_rdIdx + 1 ) % readBuffer.Length;
                                continue;
                            }

                            /* Get available bytes in buffer */
                            if(readBuffer_wrIdx >= readBuffer_rdIdx) {
                                availableBytes = readBuffer_wrIdx - readBuffer_rdIdx;
                            } else {
                                availableBytes = readBuffer.Length - ( readBuffer_rdIdx - readBuffer_wrIdx );
                            }
                            if(availableBytes < Packet.MIN_PACKET_SIZE) {
                                /* Less than minimum frame size */
                                break;
                            }

                            /* Get packet length */
                            length = Convert.ToUInt16( readBuffer[(readBuffer_rdIdx + 1) % readBuffer.Length] +
                                                (readBuffer[(readBuffer_rdIdx + 2) % readBuffer.Length] << 8));
                            if(availableBytes < length) {
                                /* entire packet is not in the receive buffer */
                                break;
                            }
                            /* Compute checksum */
                            sum = 0;
                            for(int i = 0; i < length; i++) {
                                sum = sum + readBuffer[(readBuffer_rdIdx + i) % readBuffer.Length];
                            }
                            if((sum & 0x00FF) != 0) {
                                /*
                                 * Invalid checksum
                                 * It is probably not really the start of a frame
                                 */
                                readBuffer_rdIdx = ( readBuffer_rdIdx + 1 ) % readBuffer.Length;
                                continue;
                            }
                            /* Valid Frame */
                            byte[] validFrameBuf = new byte[length];
                            for(int i = 0; i < length; i++) {
                                validFrameBuf[i] = readBuffer[( readBuffer_rdIdx + i ) % readBuffer.Length];
                            }
                            processValidFrame(validFrameBuf);
                            /* Done */
                            readBuffer_rdIdx = ( readBuffer_rdIdx + length ) % readBuffer.Length;
                        }
                    }
                }
            } catch (Exception ex) {
                Console.WriteLine(( this.ec != ErrorCode.None ? ec + ":" : String.Empty ) + ex.Message);
            } finally {
                if (this.running) {
                    Thread.Sleep(200);
                    this.device_connected = false;
                    this.state = deviceState.WAITING_CONNECTION;
                    Console.WriteLine("Waiting for device connection");
                    this.Close();
                    this.ec = ErrorCode.None;
                }
            }
        }
        private void processValidFrame(byte[] buf)
        {
            UInt32 timestamp_10us = BitConverter.ToUInt32(buf, Packet.OFFSET_TIMESTAMP);
            Packet.frame_type_t type = (Packet.frame_type_t)(buf[Packet.OFFSET_TYPE] & 0x1F);
            byte channel = (byte)( buf[Packet.OFFSET_TYPE] >> 5 );
            UInt32 msgId = BitConverter.ToUInt32(buf, Packet.OFFSET_MSGID);
            byte dlc = buf[Packet.OFFSET_DLC];
            byte[] payload = new byte[dlc];
            Array.Copy(buf, Packet.OFFSET_PAYLOAD, payload, 0, dlc);
            if(this.EventCanReceive != null) {
                this.EventCanReceive(this, new Events.CanReceiveEventArgs(timestamp_10us, type, channel, msgId, dlc, payload));
            }
        }
        private void Close()
        {
            if (this.MyUsbDevice != null) {
                IUsbDevice wholeUsbDevice = this.MyUsbDevice as IUsbDevice;
                if (!ReferenceEquals(wholeUsbDevice, null)) {
                    if(wholeUsbDevice.ReleaseInterface(3)) {
                        this.interfaceClaimed = false;
                    } else {
                        Console.WriteLine("Failed to release Interface!");
                    }
                }
                this.MyUsbDevice.Close();
            }
            this.MyUsbDevice = null;
        }

        #endregion // Method
    }

    public class Packet
    {
        // SOF - LEN - SEQ - TIMESTAMP - TYPE - MSGID - DLC - PAYLOAD - CHECKSUM
        // 1B    2B    2B    4B          1B     4B      1B    0-64B     1B
        public const byte MIN_PACKET_SIZE = 16;
        public const byte SOF = 0xFF;
        public const int OFFSET_TIMESTAMP = 5;
        public const int OFFSET_TYPE = 9;
        public const int OFFSET_MSGID = 10;
        public const int OFFSET_DLC = 14;
        public const int OFFSET_PAYLOAD = 15;
        private byte[] frame;
        public enum frame_type_t
        {
            FRAME_TYPE_CAN_CC_RX = 0,
            FRAME_TYPE_CAN_CC_TX,
            FRAME_TYPE_CAN_FD_RX,
            FRAME_TYPE_CAN_FD_TX
        };

        public Packet(deviceChannel ch, UInt16 seq, UInt16 msgId, bool bFd, byte[] payload)
        {
            DateTime timestamp = DateTime.Now;
            this.frame = new byte[MIN_PACKET_SIZE + payload.Length];
            this.frame[0] = SOF;
            this.frame[1] = (byte)( frame.Length & 0x00FF );
            this.frame[2] = (byte)( ( frame.Length >> 8 ) & 0x00FF );
            this.frame[3] = (byte)( seq & 0x00FF );
            this.frame[4] = (byte)( ( seq >> 8 ) & 0x00FF );
            UInt32 timestamp_ms = Convert.ToUInt32(( ( ( timestamp.Hour * 60.0 ) + timestamp.Minute ) * 60.0 ) + timestamp.Second + ( timestamp.Millisecond / 1000.0 ));
            BitConverter.GetBytes(timestamp_ms).CopyTo(this.frame, 5);
            if(bFd) {
                this.frame[9] = (byte)( (byte)(frame_type_t.FRAME_TYPE_CAN_FD_TX) + ( (byte)ch << 5 ) );
            } else {
                this.frame[9] = (byte)( (byte)(frame_type_t.FRAME_TYPE_CAN_CC_TX) + ( (byte)ch << 5 ) );
            }
            BitConverter.GetBytes(Convert.ToUInt32(msgId)).CopyTo(this.frame, 10);
            this.frame[14] = (byte)( payload.Length );
            if (payload.Length > 0) {
                payload.CopyTo(this.frame, 15);
            }
            Int32 chksum = 0;
            for(int i = 0; i < (this.frame.Length - 1); i++) {
                chksum = chksum + this.frame[i];
            }
            chksum = -chksum;
            this.frame[this.frame.Length - 1] = (byte)( chksum & 0xFF );
        }

        public byte[] GetFrame()
        {
            return this.frame;
        }
    }
}
