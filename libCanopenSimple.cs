﻿/*
    This file is part of libCanopenSimple.
    libCanopenSimple is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.
    libCanopenSimple is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.
    You should have received a copy of the GNU General Public License
    along with libCanopenSimple.  If not, see <http://www.gnu.org/licenses/>.
 
    Copyright(c) 2017 Robin Cornelius <robin.cornelius@gmail.com>
*/

using System;
using System.Collections.Generic;
using System.Threading;
using System.Collections.Concurrent;
using Peak.Can.Basic;

namespace libCanopenSimple
{
    using TPCANHandle = System.UInt16;

    public enum BUSSPEED
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

    public enum debuglevel
    {
        DEBUG_ALL,
        DEBUG_NONE
    }

    /// <summary>
    /// C# representation of a CanPacket, containing the COB the length and the data. RTR is not supported
    /// as its prettly much not used on CanOpen, but this could be added later if necessary
    /// </summary>
    public class canpacket
    {
        public UInt16 cob;
        public byte len;
        public byte[] data;
        public bool bridge = false;

        public canpacket()
        {
        }

        /// <summary>
        /// Construct C# Canpacket from a CanFestival message
        /// </summary>
        /// <param name="msg">A CanFestival message struct</param>
        public canpacket(DriverInstance.Message msg,bool bridge=false)
        {
            cob = msg.cob_id;
            len = msg.len;
            data = new byte[len];
            this.bridge = bridge;

            byte[] temp = BitConverter.GetBytes(msg.data);
            Array.Copy(temp, data, msg.len);
        }

        /// <summary>
        /// Convert to a CanFestival message
        /// </summary>
        /// <returns>CanFestival message</returns>
        public DriverInstance.Message ToMsg()
        {
            DriverInstance.Message msg = new DriverInstance.Message();
            msg.cob_id = cob;
            msg.len = len;
            msg.rtr = 0;

            byte[] temp = new byte[8];
            Array.Copy(data, temp, len);
            msg.data = BitConverter.ToUInt64(temp, 0);

            return msg;

        }

        /// <summary>
        /// Dump current packet to string
        /// </summary>
        /// <returns>Formatted string of current packet</returns>
        public override string ToString()
        {
            string output = string.Format("{0:x3} {1:x1}", cob, len);

            for (int x = 0; x < len; x++)
            {
                output += string.Format(" {0:x2}", data[x]);
            }
            return output;
        }
    }


    /// <summary>
    /// A simple can open class providing callbacks for each of the message classes and allowing one to send messages to the bus
    /// Also supports some NMT helper functions and can act as a SDO Client
    /// It is not a CanDevice and does not respond to any message (other than the required SDO client handshakes) and it does not
    /// contain an object dictionary
    /// </summary>
    public class libCanopenSimple
    {
        private can_hw.pcan_usb pcan;
        private const int SDO_DEFAULT_TIMEOUT = 5;  // timeout in second
        public debuglevel dbglevel = debuglevel.DEBUG_NONE;
       
        DriverInstance driver;

        Dictionary<UInt16, NMTState> nmtstate = new Dictionary<ushort, NMTState>();

        private Queue<SDO> sdo_queue = new Queue<SDO>();

        DriverLoader loader = new DriverLoader();

        public bool echo = true;

        public libCanopenSimple()
        {
            //preallocate all NMT guards
            for (byte x = 0; x < 0x80; x++)
            {
                NMTState nmt = new NMTState();
                nmtstate[x] = nmt;
            }

            this.pcan = new can_hw.pcan_usb();
        }

        #region driverinterface

        /// <summary>
        /// Open the CAN hardware device via the CanFestival driver, NB this is currently a simple version that will
        /// not work with drivers that have more complex bus ids so only supports com port (inc usb serial) devices for the moment
        /// </summary>
        /// <param name="comport">COM PORT number</param>
        /// <param name="speed">CAN Bit rate</param>
        /// <param name="drivername">Driver to use</param>
#if false
        public void open(string comport, BUSSPEED speed, string drivername)
        {

            driver = loader.loaddriver(drivername);
            driver.open(string.Format("{0}", comport), speed);

            driver.rxmessage += Driver_rxmessage;

            threadrun = true;
            Thread thread = new Thread(new ThreadStart(asyncprocess));
            thread.Name = "CAN Open worker";
            thread.Start();

            if (connectionevent != null) connectionevent(this, new ConnectionChangedEventArgs(true));

        }
#endif

        public void open(TPCANHandle pcanHandle, TPCANBaudrate baudrate)
        {
            if (this.pcan.Connect(pcanHandle, baudrate)) {
                this.pcan.CanRxMsgEvent += this.Driver_pcan_rxmessage;
                this.threadrun = true;
                Thread thread = new Thread(new ThreadStart(this.asyncprocess));
                thread.Name = "CANopen worker";
                thread.Start();
                if (connectionevent != null) {
                    connectionevent(this, new ConnectionChangedEventArgs(true));
                }
            } else {
                throw new Exception("Unable to connect to CAN adapter");
            }
        }

        public Dictionary<string, List<string>> ports = new Dictionary<string, List<string>>();

        public void enumerate(string drivername)
        {

            if (!ports.ContainsKey(drivername))
                ports.Add(drivername, new List<string>());

            driver = loader.loaddriver(drivername);
            driver.enumerate();

            ports[drivername] = DriverInstance.ports;

        }

        /// <summary>
        /// Is the driver open
        /// </summary>
        /// <returns>true = driver open and ready to use</returns>
        public bool isopen()
        {
#if false
            if (driver == null)
                return false;

            return driver.isOpen();
#else
            return this.pcan.bConnected;
#endif
        }

        /// <summary>
        /// Send a Can packet on the bus
        /// </summary>
        /// <param name="p"></param>
        public void SendPacket(canpacket p, bool bridge=false)
        {
#if false
            DriverInstance.Message msg = p.ToMsg();

            driver.cansend(msg);

            if (echo == true)
            {
                Driver_rxmessage(msg,bridge);
            }
#else
            if(this.pcan.bConnected) {
                UInt32 can_id = p.cob;
                byte[] data = new byte[p.len];
                Array.Copy(p.data, data, p.len);
                if(this.pcan.SendStandard(can_id, data)) {
                    if(sendPacketEvent != null) {
                        sendPacketEvent(p, DateTime.Now);
                    }
                }
            }
#endif
        }

        /// <summary>
        /// Recieved message callback handler
        /// </summary>
        /// <param name="msg">CanOpen message recieved from the bus</param>
        private void Driver_rxmessage(DriverInstance.Message msg,bool bridge=false)
        {
            packetqueue.Enqueue(new canpacket(msg,bridge));
        }


        private void Driver_pcan_rxmessage(object sender, can_hw.CanRxMsgArgs e)
        {
            if (e.msgType == (byte)TPCANMessageType.PCAN_MESSAGE_STANDARD) {
                canpacket newPacket = new canpacket();
                newPacket.cob = Convert.ToUInt16(e.msgId & 0x0000FFFF);
                newPacket.len = e.len;
                newPacket.data = new byte[e.len];
                Array.Copy(e.data, newPacket.data, e.len);
                packetqueue.Enqueue(newPacket);
            } else {
                string strMsg = "MsgType: " + ( (TPCANMessageType)e.msgType ).ToString();
                if((e.msgType == (byte)TPCANMessageType.PCAN_MESSAGE_STATUS) ||
                   (e.msgType == (byte)TPCANMessageType.PCAN_MESSAGE_ERRFRAME)) {
                    strMsg += " Data: ";
                    for(int i = 0; i < e.len; i++) {
                        strMsg += e.data[i].ToString("X2") + " ";
                    }
                }
                Console.WriteLine(strMsg);
            }
        }

        /// <summary>
        /// Close the CanOpen CanFestival driver
        /// </summary>
        public void close()
        {
            this.pcan.CanRxMsgEvent -= this.Driver_pcan_rxmessage;
            threadrun = false;

#if false
            if (driver == null)
                return;

            driver.close();
#else
            this.pcan.Disconnect();
#endif
            if (connectionevent != null) connectionevent(this, new ConnectionChangedEventArgs(false));
        }

#endregion

        Dictionary<UInt16, Action<byte[]>> PDOcallbacks = new Dictionary<ushort, Action<byte[]>>();
        public Dictionary<UInt16, SDO> SDOcallbacks = new Dictionary<ushort, SDO>();
        ConcurrentQueue<canpacket> packetqueue = new ConcurrentQueue<canpacket>();

        public delegate void ConnectionEvent(object sender, EventArgs e);
        public event ConnectionEvent connectionevent;

        public delegate void SendPacketEvent(canpacket p, DateTime dt);
        public event SendPacketEvent sendPacketEvent;

        public delegate void PacketEvent(canpacket p, DateTime dt);
        public event PacketEvent packetevent;

        public delegate void SDOEvent(canpacket p, DateTime dt);
        public event SDOEvent sdoevent;

        public delegate void NMTEvent(canpacket p, DateTime dt);
        public event NMTEvent nmtevent;

        public delegate void NMTECEvent(canpacket p, DateTime dt);
        public event NMTECEvent nmtecevent;

        public delegate void PDOEvent(canpacket[] p,DateTime dt);
        public event PDOEvent pdoevent;

        public delegate void EMCYEvent(canpacket p, DateTime dt);
        public event EMCYEvent emcyevent;

        public delegate void LSSEvent(canpacket p, DateTime dt);
        public event LSSEvent lssevent;

        public delegate void TIMEEvent(canpacket p, DateTime dt);
        public event TIMEEvent timeevent;

        public delegate void SYNCEvent(canpacket p, DateTime dt);
        public event SYNCEvent syncevent;

        bool threadrun = true;

        /// <summary>
        /// Register a parser handler for a PDO, if a PDO is recieved with a matching COB this function will be called
        /// so that additional messages can be added for bus decoding and monitoring
        /// </summary>
        /// <param name="cob">COB to match</param>
        /// <param name="handler">function(byte[] data]{} function to invoke</param>
        public void registerPDOhandler(UInt16 cob, Action<byte[]> handler)
        {
            PDOcallbacks[cob] = handler;
        }

        /// <summary>
        /// Main process loop, used to get latest packets from buffer and also keep the SDO events pumped
        /// When packets are recieved they will be matched to any approprate callback handlers for this specific COB type
        /// and that handler invoked.
        /// </summary>
        void asyncprocess()
        {
            while (threadrun)
            {
                canpacket cp;
                List<canpacket> pdos = new List<canpacket>();

                while (threadrun && packetqueue.IsEmpty && pdos.Count==0 && sdo_queue.Count==0 && SDO.isEmpty())
                {
                    System.Threading.Thread.Sleep(0);
                }

                while (packetqueue.TryDequeue(out cp))
                {

                    if (cp.bridge == false)
                    {
                        if(packetevent!=null)
                            packetevent(cp, DateTime.Now);
                    }

                    //PDO 0x180 -- 0x57F
                    if (cp.cob >= 0x180 && cp.cob <= 0x57F)
                    {

                        if (PDOcallbacks.ContainsKey(cp.cob))
                            PDOcallbacks[cp.cob](cp.data);

                        pdos.Add(cp);
                    }

                    //SDO replies 0x601-0x67F
                    if (cp.cob >= 0x580 && cp.cob < 0x600)
                    {
                        if (cp.len != 8)
                            return;

                        lock (sdo_queue)
                        {
                            if (SDOcallbacks.ContainsKey(cp.cob))
                            {
                                if (SDOcallbacks[cp.cob].SDOProcess(cp))
                                {
                                    SDOcallbacks.Remove(cp.cob);
                                }
                            }
                            if (sdoevent != null)
                                sdoevent(cp, DateTime.Now);
                        }
                    }

                    if (cp.cob >= 0x600 && cp.cob < 0x680)
                    {
                        if (sdoevent != null)
                            sdoevent(cp,DateTime.Now);
                    }

                    //NMT
                    if (cp.cob > 0x700 && cp.cob <= 0x77f)
                    {
                        byte node = (byte)(cp.cob & 0x07F);

                        nmtstate[node].changestate((NMTState.e_NMTState)cp.data[0]);
                        nmtstate[node].lastping = DateTime.Now;

                        if (nmtecevent != null)
                            nmtecevent(cp, DateTime.Now);
                    }

                    if (cp.cob == 000)
                    {

                        if (nmtevent != null)
                            nmtevent(cp, DateTime.Now);
                    }
                    if (cp.cob == 0x80)
                    {
                        if (syncevent != null)
                            syncevent(cp, DateTime.Now);
                    }

                    if (cp.cob > 0x080 && cp.cob <= 0xFF)
                    {
                        if (emcyevent != null)
                        {
                            emcyevent(cp, DateTime.Now);
                        }
                    }

                    if (cp.cob == 0x100)
                    {
                        if (timeevent != null)
                            timeevent(cp, DateTime.Now);
                    }

                    if (cp.cob > 0x7E4 && cp.cob <= 0x7E5)
                    {
                        if (lssevent != null)
                            lssevent(cp, DateTime.Now);
                    }
                }

                if (pdos.Count > 0)
                {
                    if (pdoevent != null)
                        pdoevent(pdos.ToArray(),DateTime.Now);
                }

                SDO.kick_SDO();

                lock (sdo_queue)
                {
                    if (sdo_queue.Count > 0)
                    {
                        SDO sdoobj = sdo_queue.Peek();

                        if (!SDOcallbacks.ContainsKey((UInt16)(sdoobj.node + 0x580)))
                        {
                            sdoobj = sdo_queue.Dequeue();
                            SDOcallbacks.Add((UInt16)(sdoobj.node + 0x580), sdoobj);
                            sdoobj.sendSDO();
                        }
                    }
                }
            }
        }


#region SDOHelpers

        /// <summary>
        /// Write to a node via SDO
        /// </summary>
        /// <param name="node">Node ID</param>
        /// <param name="index">Object Dictionary Index</param>
        /// <param name="subindex">Object Dictionary sub index</param>
        /// <param name="udata">UInt32 data to send</param>
        /// <param name="completedcallback">Call back on finished/error event</param>
        /// <returns>SDO class that is used to perform the packet handshake, contains error/status codes</returns>
        public SDO SDOwrite(byte node, UInt16 index, byte subindex, UInt32 udata, Action<SDO> completedcallback)
        {
            byte[] bytes = BitConverter.GetBytes(udata);
            return SDOwrite(node, index, subindex, bytes, completedcallback);
        }


        /// <summary>
        /// Write to a node via SDO
        /// </summary>
        /// <param name="node">Node ID</param>
        /// <param name="index">Object Dictionary Index</param>
        /// <param name="subindex">Object Dictionary sub index</param>
        /// <param name="udata">Int64 data to send</param>
        /// <param name="completedcallback">Call back on finished/error event</param>
        /// <returns>SDO class that is used to perform the packet handshake, contains error/status codes</returns>
        public SDO SDOwrite(byte node, UInt16 index, byte subindex, Int64 udata, Action<SDO> completedcallback)
        {
            byte[] bytes = BitConverter.GetBytes(udata);
            return SDOwrite(node, index, subindex, bytes, completedcallback);
        }

        /// <summary>
        /// Write to a node via SDO
        /// </summary>
        /// <param name="node">Node ID</param>
        /// <param name="index">Object Dictionary Index</param>
        /// <param name="subindex">Object Dictionary sub index</param>
        /// <param name="udata">UInt64 data to send</param>
        /// <param name="completedcallback">Call back on finished/error event</param>
        /// <returns>SDO class that is used to perform the packet handshake, contains error/status codes</returns>
        public SDO SDOwrite(byte node, UInt16 index, byte subindex, UInt64 udata, Action<SDO> completedcallback)
        {
            byte[] bytes = BitConverter.GetBytes(udata);
            return SDOwrite(node, index, subindex, bytes, completedcallback);
        }

        /// <summary>
        /// Write to a node via SDO
        /// </summary>
        /// <param name="node">Node ID</param>
        /// <param name="index">Object Dictionary Index</param>
        /// <param name="subindex">Object Dictionary sub index</param>
        /// <param name="udata">Int32 data to send</param>
        /// <param name="completedcallback">Call back on finished/error event</param>
        /// <returns>SDO class that is used to perform the packet handshake, contains error/status codes</returns>
        public SDO SDOwrite(byte node, UInt16 index, byte subindex, Int32 udata, Action<SDO> completedcallback)
        {
            byte[] bytes = BitConverter.GetBytes(udata);
            return SDOwrite(node, index, subindex, bytes, completedcallback);
        }

        /// <summary>
        /// Write to a node via SDO
        /// </summary>
        /// <param name="node">Node ID</param>
        /// <param name="index">Object Dictionary Index</param>
        /// <param name="subindex">Object Dictionary sub index</param>
        /// <param name="udata">UInt16 data to send</param>
        /// <param name="completedcallback">Call back on finished/error event</param>
        /// <returns>SDO class that is used to perform the packet handshake, contains error/status codes</returns>
        public SDO SDOwrite(byte node, UInt16 index, byte subindex, Int16 udata, Action<SDO> completedcallback)
        {
            byte[] bytes = BitConverter.GetBytes(udata);
            return SDOwrite(node, index, subindex, bytes, completedcallback);
        }

        /// <summary>
        /// Write to a node via SDO
        /// </summary>
        /// <param name="node">Node ID</param>
        /// <param name="index">Object Dictionary Index</param>
        /// <param name="subindex">Object Dictionary sub index</param>
        /// <param name="udata">UInt16 data to send</param>
        /// <param name="completedcallback">Call back on finished/error event</param>
        /// <returns>SDO class that is used to perform the packet handshake, contains error/status codes</returns>
        public SDO SDOwrite(byte node, UInt16 index, byte subindex, UInt16 udata, Action<SDO> completedcallback)
        {
            byte[] bytes = BitConverter.GetBytes(udata);
            return SDOwrite(node, index, subindex, bytes, completedcallback);
        }

        /// <summary>
        /// Write to a node via SDO
        /// </summary>
        /// <param name="node">Node ID</param>
        /// <param name="index">Object Dictionary Index</param>
        /// <param name="subindex">Object Dictionary sub index</param>
        /// <param name="udata">float data to send</param>
        /// <param name="completedcallback">Call back on finished/error event</param>
        /// <returns>SDO class that is used to perform the packet handshake, contains error/status codes</returns>
        public SDO SDOwrite(byte node, UInt16 index, byte subindex, float ddata, Action<SDO> completedcallback)
        {
            byte[] bytes = BitConverter.GetBytes(ddata);
            return SDOwrite(node, index, subindex, bytes, completedcallback);
        }

        /// <summary>
        /// Write to a node via SDO
        /// </summary>
        /// <param name="node">Node ID</param>
        /// <param name="index">Object Dictionary Index</param>
        /// <param name="subindex">Object Dictionary sub index</param>
        /// <param name="udata">a byte of data to send</param>
        /// <param name="completedcallback">Call back on finished/error event</param>
        /// <returns>SDO class that is used to perform the packet handshake, contains error/status codes</returns>
        public SDO SDOwrite(byte node, UInt16 index, byte subindex, byte udata, Action<SDO> completedcallback)
        {
            byte[] bytes = new byte[1];
            bytes[0] = udata;
            return SDOwrite(node, index, subindex, bytes, completedcallback);
        }

        /// <summary>
        /// Write to a node via SDO
        /// </summary>
        /// <param name="node">Node ID</param>
        /// <param name="index">Object Dictionary Index</param>
        /// <param name="subindex">Object Dictionary sub index</param>
        /// <param name="udata">a byte of unsigned data to send</param>
        /// <param name="completedcallback">Call back on finished/error event</param>
        /// <returns>SDO class that is used to perform the packet handshake, contains error/status codes</returns>
        public SDO SDOwrite(byte node, UInt16 index, byte subindex, sbyte udata, Action<SDO> completedcallback)
        {
            byte[] bytes = new byte[1];
            bytes[0] = (byte)udata;
            return SDOwrite(node, index, subindex, bytes, completedcallback);
        }

        /// <summary>
        /// Write to a node via SDO
        /// </summary>
        /// <param name="node">Node ID</param>
        /// <param name="index">Object Dictionary Index</param>
        /// <param name="subindex">Object Dictionary sub index</param>
        /// <param name="udata">byte[] of data (1-8 bytes) to send</param>
        /// <param name="completedcallback">Call back on finished/error event</param>
        /// <returns>SDO class that is used to perform the packet handshake, contains error/status codes</returns>
        public SDO SDOwrite(byte node, UInt16 index, byte subindex, byte[] data, Action<SDO> completedcallback, int timeout = SDO_DEFAULT_TIMEOUT)
        {

            SDO sdo = new SDO(this, node, index, subindex, SDO.direction.SDO_WRITE, completedcallback, data, timeout: timeout);
            lock(sdo_queue)
                sdo_queue.Enqueue(sdo);
            return sdo;
        }

        /// <summary>
        /// Read from a remote node via SDO
        /// </summary>
        /// <param name="node">Node ID to read from</param>
        /// <param name="index">Object Dictionary Index</param>
        /// <param name="subindex">Object Dictionary sub index</param>
        /// <param name="completedcallback">Call back on finished/error event</param>
        /// <returns>SDO class that is used to perform the packet handshake, contains returned data and error/status codes</returns>
        public SDO SDOread(byte node, UInt16 index, byte subindex, Action<SDO> completedcallback, int timeout = SDO_DEFAULT_TIMEOUT)
        {
            SDO sdo = new SDO(this, node, index, subindex, SDO.direction.SDO_READ, completedcallback, null, timeout: timeout);
            lock (sdo_queue)
                sdo_queue.Enqueue(sdo);
            return sdo;
        }

        /// <summary>
        /// Get the current length of Enqueued items
        /// </summary>
        /// <returns></returns>
        public int getSDOQueueSize()
        {
            return sdo_queue.Count;
        }

        /// <summary>
        /// Flush the SDO queue
        /// </summary>
        public void flushSDOqueue()
        {
            lock (sdo_queue)
                sdo_queue.Clear();
        }

#endregion

#region NMTHelpers

        public void NMT_start(byte nodeid = 0)
        {
            canpacket p = new canpacket();
            p.cob = 000;
            p.len = 2;
            p.data = new byte[2];
            p.data[0] = 0x01;
            p.data[1] = nodeid;
            SendPacket(p);
        }

        public void NMT_preop(byte nodeid = 0)
        {
            canpacket p = new canpacket();
            p.cob = 000;
            p.len = 2;
            p.data = new byte[2];
            p.data[0] = 0x80;
            p.data[1] = nodeid;
            SendPacket(p);
        }

        public void NMT_stop(byte nodeid = 0)
        {
            canpacket p = new canpacket();
            p.cob = 000;
            p.len = 2;
            p.data = new byte[2];
            p.data[0] = 0x02;
            p.data[1] = nodeid;
            SendPacket(p);
        }

        public void NMT_ResetNode(byte nodeid = 0)
        {
            canpacket p = new canpacket();
            p.cob = 000;
            p.len = 2;
            p.data = new byte[2];
            p.data[0] = 0x81;
            p.data[1] = nodeid;

            SendPacket(p);
        }

        public void NMT_ResetComms(byte nodeid = 0)
        {
            canpacket p = new canpacket();
            p.cob = 000;
            p.len = 2;
            p.data = new byte[2];
            p.data[0] = 0x82;
            p.data[1] = nodeid;

            SendPacket(p);
        }

        public void NMT_SetStateTransitionCallback(byte node, Action<NMTState.e_NMTState> callback)
        {
            nmtstate[node].NMT_boot = callback;
        }

        public bool NMT_isNodeFound(byte node)
        {
            return nmtstate[node].state != NMTState.e_NMTState.INVALID;
        }

        public void NMT_ReseCommunication(byte nodeid = 0)
        {
            canpacket p = new canpacket();
            p.cob = 000;
            p.len = 2;
            p.data = new byte[2];
            p.data[0] = 0x81;
            p.data[1] = nodeid;

            SendPacket(p);
        }

        public bool checkguard(int node, TimeSpan maxspan)
        {
            if (DateTime.Now - nmtstate[(ushort)node].lastping > maxspan)
                return false;

            return true;
        }

#endregion

#region PDOhelpers

        public void writePDO(UInt16 cob, byte[] payload)
        {
            canpacket p = new canpacket();
            p.cob = cob;
            p.len = (byte)payload.Length;
            p.data = new byte[p.len];
            for (int x = 0; x < payload.Length; x++)
                p.data[x] = payload[x];

            SendPacket(p);
        }

        #endregion

        #region CAN Statistics
        public can_hw.BusState CanStat_get_state()
        {
            return this.pcan.busState;
        }
        public byte CanStat_get_REC()
        {
            return this.pcan.REC;
        }
        public byte CanStat_get_TEC()
        {
            return this.pcan.TEC;
        }
        public byte CanStat_get_max_REC()
        {
            return this.pcan.max_REC;
        }
        public byte CanStat_get_max_TEC()
        {
            return this.pcan.max_TEC;
        }
        public UInt32 CanStat_get_total_tx_cnt()
        {
            return this.pcan.total_tx_cnt;
        }
        public UInt32 CanStat_get_total_rx_cnt()
        {
            return this.pcan.total_rx_cnt;
        }
        public UInt16 CanStat_get_warning_cnt()
        {
            return this.pcan.warning_cnt;
        }
        public UInt16 CanStat_get_error_passive_cnt()
        {
            return this.pcan.error_passive_cnt;
        }
        public UInt16 CanStat_get_busoff_cnt()
        {
            return this.pcan.bus_off_cnt;
        }
        #endregion
    }
}
