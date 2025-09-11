using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;

namespace CustomDeviceTest
{
    public partial class CustomTest : Form
    {
        #region Members
        const int VID = 0xcafe;
        const int PID = 0x4013;
        can_hw.custom.CustomDevice device;
        #endregion // Members

        #region Methods
        public CustomTest()
        {
            InitializeComponent();
            device = new can_hw.custom.CustomDevice(VID, PID);
            GetDongleSerialNumber();
        }
        private void GetDongleSerialNumber()
        {
            cboDongleSN.Items.Clear();
            cboDongleSN.Text = null;
            this.device.ListDevice();
            List<string> DevSerialList = this.device.DevSerialList;
            if (DevSerialList.Count != 0) {
                for (int i = 0; i < DevSerialList.Count; i++) {
                    if (DevSerialList[i] != null) {
                        cboDongleSN.Items.Add(DevSerialList[i]);
                    }
                }
                cboDongleSN.Text = DevSerialList[0];
            }
        }
        #region Form Events
        private void CustomTest_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.device.Exit();
        }
        #endregion // Form Events
        #region Button Events
        private void btnRefresh_Click(object sender, EventArgs e)
        {
            GetDongleSerialNumber();
        }
        private void btnConnect_Click(object sender, EventArgs e)
        {
            if (btnConnect.Text == "Connect") {
                try {
                    this.device.Connect(cboDongleSN.Text);
                    this.device.EventDataReceive += DataReceive;
                    this.device.EventCanReceive += CanReceive;
                    btnConnect.Text = "Disconnect";
                    gbUsbComm.Enabled = false;
                    rtb_log.Clear();
                    Thread.Sleep(2000); /* Add delay to give enough time for the dongle to be in CONNECTED state */
                } catch (Exception ex) {
                    MessageBox.Show(ex.Message);
                }
            } else {
                btnConnect.Text = "Connect";
                gbUsbComm.Enabled = true;
                this.device.EventDataReceive -= DataReceive;
                this.device.Disconnect();
            }
        }
        #endregion // Button Events

        #endregion // Methods

        private void btnSendTest_Click(object sender, EventArgs e)
        {
            // Test CAN-CC transmit
            byte[] buffer = new byte[8];
            for(int i = 0; i < buffer.Length; i++) {
                buffer[i] = (byte)i;
            }
            try {
                if (!this.device.Send(can_hw.custom.deviceChannel.CH0, 0x701, false, buffer)) {
                    Console.WriteLine("CAN-CC Send Failed!");
                }
            } catch(Exception ex) {
                MessageBox.Show(ex.Message);
            }
        }

        private void btnCanFdSend_Click(object sender, EventArgs e)
        {
            // Test CAN-FD transmit
            byte[] buffer = new byte[64];
            for(int i = 0; i < buffer.Length; i++) {
                buffer[i] = (byte)i;
            }
            try {
                if(!this.device.Send(can_hw.custom.deviceChannel.CH1, 0x702, true, buffer)) {
                    Console.WriteLine("CAN-FD Send Failed!");
                }
            } catch(Exception ex) {
                MessageBox.Show(ex.Message);
            }
        }

        void DataReceive(object sender, can_hw.custom.Events.DataReceiveEventArgs e)
        {
            Console.WriteLine("DataReceive(): " + BitConverter.ToString(e.buf));
        }

        void CanReceive(object sender, can_hw.custom.Events.CanReceiveEventArgs e)
        {
            string strType = "UNKNOWN";
            if(e.frame_type == can_hw.custom.Packet.frame_type_t.FRAME_TYPE_CAN_CC_RX) {
                strType = "CAN-CC";
            } else if(e.frame_type == can_hw.custom.Packet.frame_type_t.FRAME_TYPE_CAN_FD_RX) {
                strType = "CAN-FD";
            }
            string strLog = "(" + (e.timestamp_10us * 0.00001 ).ToString("F3") + ")   CAN" + e.channel + ", type: " + strType + ", MsgId: 0x" + e.msgId.ToString("X4") + ", dlc: " + e.dlc +
                            ", payload: " + BitConverter.ToString(e.buf);
            this.rtb_log.BeginInvoke(new Action(() => {
                rtb_log.Text += strLog + Environment.NewLine;
                rtb_log.Select(rtb_log.Text.Length, 0);
                rtb_log.ScrollToCaret();
            }));
        }

        private void rtb_log_DoubleClick(object sender, EventArgs e)
        {
            rtb_log.Clear();
        }
    }
}
