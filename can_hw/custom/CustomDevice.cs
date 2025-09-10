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
        private UsbDeviceFinder MyUsbFinder;
        private UsbEndpointReader epReader;
        private UsbEndpointWriter epWriter;
        private ErrorCode ec = ErrorCode.None;
        private Thread USB_receive;
        private byte[] readBuffer;
        private int nBytesReceived;
        private Thread USB_connect;
        private byte[] writeBuffer;
        private int vid;
        private int pid;
        private string sn;
        public deviceState state { get; private set; }
        public List<string> DevSerialList { get; private set; }
        public event Events.DataReceiveEventHandler EventDataReceive;
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
                    this.ec = this.epReader.Transfer(this.readBuffer, 0, this.bufferSize, 500, out bytesRead);
                    this.nBytesReceived = bytesRead;
                    lock (USB_receive) {
                        if (this.running && ( this.ec == ErrorCode.None )) {
                            if (this.EventDataReceive != null) {
                                this.EventDataReceive(this, new Events.DataReceiveEventArgs(this.readBuffer, bytesRead));
                            }
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
        private const byte PKT_OVERHEAD = 16;
        private const byte SOF = 0xFF;
        private UInt16 length;
        private UInt16 seq;
        private UInt16 msgId;
        private byte dlc;
        private byte[] frame;
        private byte checksum;

        public Packet(deviceChannel ch, UInt16 seq, UInt16 msgId, bool bFd, byte[] payload)
        {
            DateTime timestamp = DateTime.Now;
            this.frame = new byte[PKT_OVERHEAD + payload.Length];
            this.frame[0] = 0xFF;
            this.frame[1] = (byte)( frame.Length & 0x00FF );
            this.frame[2] = (byte)( ( frame.Length >> 8 ) & 0x00FF );
            this.frame[3] = (byte)( seq & 0x00FF );
            this.frame[4] = (byte)( ( seq >> 8 ) & 0x00FF );
            UInt32 timestamp_ms = Convert.ToUInt32(( ( ( timestamp.Hour * 60.0 ) + timestamp.Minute ) * 60.0 ) + timestamp.Second + ( timestamp.Millisecond / 1000.0 ));
            BitConverter.GetBytes(timestamp_ms).CopyTo(this.frame, 5);
            if(bFd) {
                this.frame[9] = (byte)( 0x3 + ( (byte)ch << 5 ) );
            } else {
                this.frame[9] = (byte)( 0x1 + ( (byte)ch << 5 ) );
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
