using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;

namespace can_hw
{
    public enum BUS_SPEED
    {
        BUS_10Kbit = 0,
        BUS_20Kbit,
        BUS_50Kbit,
        BUS_100Kbit,
        BUS_125Kbit,
        BUS_250Kbit,
        BUS_500Kbit,
        BUS_800Kbit,
        BUS_1Mbit,
    }

    public class webserial_canfd
    {
        #region members
        /*
         * Frame Format
         *   TAG        : 1 byte
         *   Length     : 2 bytes
         *   Timestamp  : 4 bytes
         *   Packet Seq : 2 bytes
         *   Payload    : N Bytes
         *   Checksum   : 1 byte
         */
        const byte TAG_SOF = 0xff;
        const byte TAG_OFFSET = 0;
        const byte LEN_OFFSET = 1;
        const byte TIMESTAMP_OFFSET = 3;
        const byte PACKET_SEQ_OFFSET = 7;
        const byte PAYLOAD_OFFSET = 9;
        const byte FRAME_OVERHEAD = 10;

        const byte COMMAND_OFFSET = PAYLOAD_OFFSET;
        const byte CMD_GET_DEVICE_ID = 0x00;
        const byte CMD_CAN_START = 0x01;
        const byte CMD_CAN_STOP = 0x02;
        const byte CMD_SEND_DOWNSTREAM = 0x10;
        const byte CMD_SEND_UPSTREAM = 0x11;
        const byte CMD_PROTOCOL_STATUS = 0x12;
        const byte CMD_GET_CAN_STATS = 0x13;
        const byte CMD_RESET_CAN_STATS = 0x14;

        const byte DEVICE_ID = 0xAC;

        private SerialPort serial_port;
        private const UInt32 RXBUFFERSIZE = 4096;
        private RingBuffer serialPort_rxBuffer;
        public UInt32 rxCount { get; private set; }
        public bool bConnected { private set; get; }
        private bool can_bus_started = false;
        public event CanMsgHandler CanRxMsgEvent;
        //public event CanTxHook CanTxHookEvent;
        public UInt16 REC { private set; get; }
        public UInt16 max_REC { private set; get; }
        public UInt16 TEC { private set; get; }
        public UInt16 max_TEC { private set; get; }
        public UInt32 total_tx_cnt { private set; get; }
        public UInt32 total_rx_cnt { private set; get; }
        public UInt16 error_passive_cnt { private set; get; }
        #endregion // members

        #region methods
        public webserial_canfd()
        {
            serial_port = new SerialPort();
            serialPort_rxBuffer = new RingBuffer(RXBUFFERSIZE);

        }

        public bool Connect(string comPort, BUS_SPEED bus_speed)
        {
            serial_port.PortName = comPort;
            serial_port.Open();
            serial_port.DataReceived += SerialPort_DataReceived;
            bConnected = true;
            GetDongleID();
            return true;
        }

        public void Disconnect()
        {
            if(can_bus_started) {
                CAN_stop();
            }
            serial_port.DataReceived -= SerialPort_DataReceived;
            serial_port.Close();
            bConnected = false;
            can_bus_started = false;
        }

        public bool SendStandard(UInt32 msgId, byte[] data)
        {
            if(!this.bConnected) {
                Console.WriteLine("webserial_canfd: Not Connected");
                return false;
            }
            Console.WriteLine("Tx: msgId: " + msgId.ToString("X4") + ", data: " + BitConverter.ToString(data));
            Int32 length = FRAME_OVERHEAD + 1 + 1 + 4 + 1; // 1 byte (cmd), 1 byte (type), 4 bytes (msgId), 1 bytes (dlc)
            byte frameType = 0x2; // CAN-CC and BRS_OFF
            byte dlc = (byte)data.Length;
            if (dlc > 8) {
                return false;
            }

            const UInt32 FRAME_TYPE_OFFSET = PAYLOAD_OFFSET + 1;
            const UInt32 MSGID_OFFSET = PAYLOAD_OFFSET + 2;
            const UInt32 DLC_OFFSET = PAYLOAD_OFFSET + 6;
            const UInt32 DATA_OFFSET = PAYLOAD_OFFSET + 7;

            byte[] packet = new byte[length + dlc];
            packet[0] = TAG_SOF;
            Array.Copy(BitConverter.GetBytes((UInt16)packet.Length), 0, packet, LEN_OFFSET, 2);
            packet[COMMAND_OFFSET] = CMD_SEND_DOWNSTREAM;
            packet[FRAME_TYPE_OFFSET] = frameType;
            Array.Copy(BitConverter.GetBytes(msgId), 0, packet, MSGID_OFFSET, 4);
            packet[DLC_OFFSET] = (byte)dlc;
            Array.Copy(data, 0, packet, DATA_OFFSET, dlc);

            Int32 checksum = CalculateChecksum(packet);
            packet[packet.Length - 1] = Convert.ToByte(( -checksum ) & 0xFF);

            if(!SendPacket(packet)) {
                Console.WriteLine("Failed to send!");
            }
            return true;
        }

        public void ClearStatistics()
        {
            total_rx_cnt = 0;
            total_tx_cnt = 0;
        }

        private void ParseValidSerialPacket(byte[] packet)
        {
            // Timestamp is 6 bytes
            UInt32 timestamp_us = BitConverter.ToUInt32(packet, TIMESTAMP_OFFSET) * 10;
            float fTimestamp = ( (float)timestamp_us ) * 0.000001f;

            UInt16 seq = BitConverter.ToUInt16(packet, PACKET_SEQ_OFFSET);

            // payload
            byte[] payload = new byte[packet.Length - FRAME_OVERHEAD];
            Array.Copy(packet, PAYLOAD_OFFSET, payload, 0, payload.Length);

            byte cmd = payload[0];
            switch(cmd) {
                case CMD_GET_DEVICE_ID: {
                    byte DevID = payload[1];
                    Console.WriteLine(fTimestamp.ToString() + ": Dongle Device ID: " + DevID.ToString("X2"));
                    if(DevID != DEVICE_ID) {
                        Console.WriteLine("Unknown Device ID");
                        break;
                    }
                    if(!can_bus_started) {
                        CAN_start();
                    }
                    break;
                }
                case CMD_CAN_START: {
                    byte sts = payload[1];
                    Console.WriteLine(fTimestamp.ToString() + ": Started: " + sts.ToString("X2"));
                    if(sts != 0) {
                        /// TODO: Handle this
                        break;
                    }
                    can_bus_started = true;
                    break;
                }
                case CMD_CAN_STOP: {
                    byte sts = payload[1];
                    Console.WriteLine(fTimestamp.ToString() + ": Stopped: " + sts.ToString("X2"));
                    if (sts != 0) {
                        /// TODO: Handle this
                        break;
                    }
                    can_bus_started = false;
                    break;
                }
                case CMD_SEND_DOWNSTREAM: {
                    byte sts = payload[1];
                    if (sts == 0) {
                        total_tx_cnt++;
                    } else {
                        Console.WriteLine(fTimestamp.ToString() + ": CMD_SEND_DOWNSTREAM: Error:" + sts.ToString("X2"));
                    }
                    break;
                }
                case CMD_SEND_UPSTREAM: {
                    byte frameType = payload[1];
                    UInt32 msgId = BitConverter.ToUInt32(payload, 2);
                    byte dlc = payload[6];
                    byte[] data = new byte[dlc];
                    Array.Copy(payload, 7, data, 0, dlc);

                    //Console.WriteLine(fTimestamp.ToString() + ": CAN-Rx: type: 0x" + frameType.ToString("X2") +
                    //    ", MsgId: 0x" + msgId.ToString("X4") +
                    //    ", dlc: " + dlc +
                    //    ", data: " + BitConverter.ToString(data));

                    this.rxCount++;

                    if(this.CanRxMsgEvent != null) {
                        // translate frameType to msgType definition
                        byte msgType = 0;
                        if((frameType & 0x01) != 0) {
                            msgType |= ( 0x04 ); // PCAN_MESSAGE_FD
                        }
                        if((frameType & 0x02) == 0) {
                            msgType |= ( 0x08 ); // PCAN_MESSAGE_BRS
                        }
                        if((frameType & 0x04) != 0) {
                            msgType |= ( 0x02 ); // PCAN_MESSAGE_EXTENDED
                        }
                        this.CanRxMsgEvent(this, new CanRxMsgArgs(msgId, msgType, data, timestamp_us));
                    }
                    total_rx_cnt++;
                    break;
                }
                case CMD_PROTOCOL_STATUS: {
                    /*
                     * Protocol Status Format:
                     * Payload[0]: CMD_PROTOCOL_STATUS (0x12)
                     * Payload[1]: LastErrorCode
                     * Payload[2]: DataLastErrorCode
                     * Payload[3]: Activity
                     * Payload[4]: Flags byte:
                     *   bit0: ErrorPassive
                     *   bit1: Warning
                     *   bit2: BusOff
                     *   bit3: RxESIflag
                     *   bit4: RxBRSflag
                     *   bit5: RxFDFflag
                     *   bit6: ProtocolException
                     *   bit7: Reserved
                     * Payload[5]: TDCvalue
                     */
                    byte lastErrorCode = payload[1];
                    byte dataLastErrorCode = payload[2];
                    byte activity = payload[3];
                    byte flags = payload[4];
                    byte tdcValue = payload[5];

                    Console.WriteLine(fTimestamp.ToString() + ": Protocol Status - LastErr: 0x" + lastErrorCode.ToString("X2") +
                        ", DataLastErr: 0x" + dataLastErrorCode.ToString("X2") +
                        ", Activity: 0x" + activity.ToString("X2") +
                        ", Flags: 0x" + flags.ToString("X2") +
                        ", TDC: 0x" + tdcValue.ToString("X2"));
                    break;
                }
                case CMD_GET_CAN_STATS: {
                    /*
                     * CAN Stats Response Format:
                     * Payload[0]: CMD_GET_CAN_STATS (0x13)
                     * Payload[1-2]: TxErrorCnt (uint16_t, little-endian)
                     * Payload[3-4]: TxErrorCntMax (uint16_t, little-endian)
                     * Payload[5-6]: RxErrorCnt (uint16_t, little-endian)
                     * Payload[7-8]: RxErrorCntMax (uint16_t, little-endian)
                     * Payload[9-10]: PassiveErrorCnt (uint16_t, little-endian)
                     * Payload[11-12]: stat_downstream_packet_loss_cnt (uint16_t, little-endian)
                     * Payload[13-14]: stat_upstream_packet_loss_cnt (uint16_t, little-endian)
                     */
                    TEC = BitConverter.ToUInt16(payload, 1);
                    max_TEC = BitConverter.ToUInt16(payload, 3);
                    REC = BitConverter.ToUInt16(payload, 5);
                    max_REC = BitConverter.ToUInt16(payload, 7);
                    error_passive_cnt = BitConverter.ToUInt16(payload, 9);
                    Console.WriteLine(fTimestamp.ToString() + ": TEC: " + TEC +
                        ", Max TEC:" + max_TEC +
                        ", REC: " + REC +
                        ", Max REC: " + max_REC +
                        ", PassiveErr: " + error_passive_cnt);
                    break;
                }
                default: {
                    break;
                }
            }
        }

        private void ParseSerialData()
        {
            UInt16 length = 0;
            while (serialPort_rxBuffer.IsEmpty() == false) {
                /* Check start of command TAG */
                if (TAG_SOF != serialPort_rxBuffer.buffer[serialPort_rxBuffer.rdIdx]) {
                    // Skip character
                    serialPort_rxBuffer.Remove(1);
                    continue;
                }

                /* Get available bytes in the buffer */
                if (serialPort_rxBuffer.AvailableBytes() < 3) {
                    /*
                     * Minimum of five bytes to proceed
                     * 1byte(TAG) + 3bytes(Length)
                     */
                    break;
                }
                // See if the packet size byte is valid.  A command packet must be at
                // least four bytes and can not be larger than the receive buffer size.
                length = BitConverter.ToUInt16(serialPort_rxBuffer.Peek(LEN_OFFSET, 2), 0);

                if (( length < 4 ) || ( length > ( serialPort_rxBuffer.buffer.Length - 1 ) )) {
                    // The packet size is too small. Minimum packet size is 4 bytes
                    // 1byte(TAG) + 2bytes(Length) + 1byte(Checksum)

                    // The packet size is too large, so either this is not the start of
                    // a packet or an invalid packet was received.  Skip this start of
                    // command packet tag.
                    serialPort_rxBuffer.Remove(1);
                    // Keep scanning for a start of command packet tag.
                    continue;
                }

                // If the entire command packet is not in the receive buffer then stop
                if (serialPort_rxBuffer.AvailableBytes() < length) {
                    break;
                }

                // The entire command packet is in the receive buffer, so compute its
                // checksum.
                byte[] packet = serialPort_rxBuffer.Peek(0, length);
                UInt32 sum = 0;
                for (int i = 0; i < length; i++) {
                    sum += packet[i];
                }

                // Skip this packet if the checksum is not correct (that is, it is
                // probably not really the start of a packet).
                if (( sum & 0xFF ) != 0) {
                    // Skip this character
                    serialPort_rxBuffer.Remove(1);
                    // Keep scanning for a start of command packet tag.
                    continue;
                }

                // Process Valid Packet
                ParseValidSerialPacket(packet);

                // Done with processing this command packet.
                serialPort_rxBuffer.Remove(length);
            }
        }

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            int bytesToRead = serial_port.BytesToRead;
            byte[] readBuffer = new byte[bytesToRead];
            if (serial_port.BytesToRead > 0) {
                int actualBytesRead = serial_port.Read(readBuffer, 0, bytesToRead);
                byte[] actualReadBuffer = new byte[actualBytesRead];
                Array.Copy(readBuffer, 0, actualReadBuffer, 0, actualBytesRead);
                serialPort_rxBuffer.Write(actualReadBuffer);
                this.ParseSerialData();
            }
        }

        #region Commands
        private byte CalculateChecksum(byte[] packet)
        {
            UInt32 sum = 0;
            for (int i = 0; i < packet.Length; i++) {
                sum += packet[i];
            }
            return Convert.ToByte(sum & 0xFF);
        }

        private bool SendPacket(byte[] packet)
        {
            if (serial_port.IsOpen == false) {
                return false;
            }
            serial_port.Write(packet, 0, packet.Length);
            return true;
        }

        private bool GetDongleID()
        {
            bool ret = true;
            byte[] packet = new byte[FRAME_OVERHEAD + 1];
            packet[0] = TAG_SOF;
            Array.Copy(BitConverter.GetBytes((UInt16)packet.Length), 0, packet, LEN_OFFSET, 2);
            packet[COMMAND_OFFSET] = CMD_GET_DEVICE_ID;
            Int32 checksum = CalculateChecksum(packet);
            packet[packet.Length - 1] = Convert.ToByte(( -checksum ) & 0xFF);

            ret = SendPacket(packet);

            return ret;
        }

        private bool CAN_start()
        {
            bool ret = true;
            byte[] packet = new byte[FRAME_OVERHEAD + 1];
            packet[0] = TAG_SOF;
            Array.Copy(BitConverter.GetBytes((UInt16)packet.Length), 0, packet, LEN_OFFSET, 2);
            packet[COMMAND_OFFSET] = CMD_CAN_START;
            Int32 checksum = CalculateChecksum(packet);
            packet[packet.Length - 1] = Convert.ToByte(( -checksum ) & 0xFF);

            ret = SendPacket(packet);

            return ret;
        }

        private bool CAN_stop()
        {
            bool ret = true;
            byte[] packet = new byte[FRAME_OVERHEAD + 1];
            packet[0] = TAG_SOF;
            Array.Copy(BitConverter.GetBytes((UInt16)packet.Length), 0, packet, LEN_OFFSET, 2);
            packet[COMMAND_OFFSET] = CMD_CAN_STOP;
            Int32 checksum = CalculateChecksum(packet);
            packet[packet.Length - 1] = Convert.ToByte(( -checksum ) & 0xFF);

            ret = SendPacket(packet);

            return ret;
        }

        #endregion // Commands

        #endregion // methods
    }

    class RingBuffer
    {
        public byte[] buffer;
        public Int32 rdIdx;
        public Int32 wrIdx;

        public RingBuffer(UInt32 size)
        {
            this.buffer = new byte[size];
            this.rdIdx = 0;
            this.wrIdx = 0;
        }

        public bool IsEmpty()
        {
            return ( rdIdx == wrIdx );
        }

        public bool IsFull()
        {
            Int32 tmpWrIdx = ( this.wrIdx + 1 ) % buffer.Length;
            return ( rdIdx == tmpWrIdx );
        }

        public Int32 AvailableBytes()
        {
            Int32 availableBytes;
            if (wrIdx >= rdIdx) {
                availableBytes = wrIdx - rdIdx;
            } else {
                availableBytes = ( wrIdx + buffer.Length ) - rdIdx;
            }
            return availableBytes;
        }

        public byte[] Peek(Int32 offset, UInt32 count)
        {
            byte[] peekBuffer = new byte[count];
            Int32 tmpRdIdx = ( rdIdx + offset ) % buffer.Length;
            for (int i = 0; i < count; i++) {
                peekBuffer[i] = buffer[tmpRdIdx];
                tmpRdIdx = ( tmpRdIdx + 1 ) % buffer.Length;
            }
            return peekBuffer;
        }


        public void Remove(UInt32 count)
        {
            for (int i = 0; i < count; i++) {
                if (IsEmpty()) {
                    return;
                } else {
                    rdIdx = ( rdIdx + 1 ) % buffer.Length;
                }
            }
        }
        public void WriteOne(byte b)
        {
            if (IsFull()) {
                Remove(1); // discard oldest data
            }
            buffer[wrIdx] = b;
            wrIdx = ( wrIdx + 1 ) % buffer.Length;
        }

        public void Write(byte[] buffer)
        {
            for (int i = 0; i < buffer.Length; i++) {
                WriteOne(buffer[i]);
            }
        }
    }

}
