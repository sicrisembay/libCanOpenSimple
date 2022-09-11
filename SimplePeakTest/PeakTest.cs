using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using libCanopenSimple;
using Peak.Can.Basic;
using TPCANHandle = System.Byte;

namespace SimplePeakTest
{
    public partial class PeakTest : Form
    {
        private libCanopenSimple.libCanopenSimple lco;
        private TPCANHandle[] m_HandleArray;
        private TPCANHandle pcanHandle;
        private TPCANBaudrate pcanBaudRate;

        public PeakTest()
        {
            InitializeComponent();

            this.lco = new libCanopenSimple.libCanopenSimple();
            this.lco.nmtevent += Lco_nmtevent;
            this.lco.nmtecevent += Lco_nmtecevent;
            this.lco.pdoevent += Lco_pdoevent;
            this.lco.sdoevent += Lco_sdoevent;

            this.m_HandleArray = new TPCANHandle[]
            {
                PCANBasic.PCAN_USBBUS1,
                PCANBasic.PCAN_USBBUS2,
                PCANBasic.PCAN_USBBUS3,
                PCANBasic.PCAN_USBBUS4,
                PCANBasic.PCAN_USBBUS5,
                PCANBasic.PCAN_USBBUS6,
                PCANBasic.PCAN_USBBUS7,
                PCANBasic.PCAN_USBBUS8,
            };

            this.btn_HwRefresh_Click(this, null);

        }

        private void Lco_nmtecevent(canpacket p, DateTime dt)
        {
            Console.WriteLine("NMTEC :" + p.ToString());
        }

        private void Lco_sdoevent(libCanopenSimple.canpacket p, DateTime dt)
        {
            Console.WriteLine("SDO :" + p.ToString());
        }

        private void Lco_pdoevent(libCanopenSimple.canpacket[] ps, DateTime dt)
        {
            foreach (canpacket p in ps)
                Console.WriteLine("PDO :" + p.ToString());
        }

        private void Lco_nmtevent(libCanopenSimple.canpacket p, DateTime dt)
        {
            Console.WriteLine("NMT :" + p.ToString());
        }

        private void btn_HwRefresh_Click(object sender, EventArgs e)
        {
            this.cbb_channel.Items.Clear();
            this.cbb_baudrates.Items.Clear();

            UInt32 iBuffer;
            TPCANStatus stsResult;
            try {
                for (int i = 0; i < this.m_HandleArray.Length; i++) {
                    // Includes all no-Plug&Play Handles
                    if (( this.m_HandleArray[i] >= PCANBasic.PCAN_USBBUS1 ) &&
                        ( this.m_HandleArray[i] <= PCANBasic.PCAN_USBBUS8 )) {
                        stsResult = PCANBasic.GetValue(this.m_HandleArray[i], TPCANParameter.PCAN_CHANNEL_CONDITION, out iBuffer, sizeof(UInt32));
                        if (( stsResult == TPCANStatus.PCAN_ERROR_OK ) && ( iBuffer == PCANBasic.PCAN_CHANNEL_AVAILABLE )) {
                            cbb_channel.Items.Add(string.Format("PCAN-USB{0}", this.m_HandleArray[i] & 0x0F));
                        }
                    }
                }
                cbb_channel.SelectedIndex = cbb_channel.Items.Count - 1;
            } catch (DllNotFoundException) {
                MessageBox.Show("Unable to find the library: PCANBasic.dll !", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(-1);
            }

            string[] baudName = Enum.GetNames(typeof(TPCANBaudrate));
            for (int i = 0; i < baudName.Length; i++) {
                cbb_baudrates.Items.Add(baudName[i]);
            }
            cbb_baudrates.SelectedIndex = 3;

        }

        private void btn_connect_Click(object sender, EventArgs e)
        {
            if (btn_connect.Text == "Connect") {
                this.lco.open(this.pcanHandle, this.pcanBaudRate);
                this.btn_connect.Text = "Disconnect";
            } else {
                this.lco.close();
                this.btn_connect.Text = "Connect";
            }
        }

        private void cbb_channel_SelectedIndexChanged(object sender, EventArgs e)
        {
            string strTemp = cbb_channel.Text;
            strTemp = strTemp.Substring(strTemp.IndexOf('B') + 1, 1);
            strTemp = "0x5" + strTemp;
            this.pcanHandle = Convert.ToByte(strTemp, 16);
        }

        private void cbb_baudrates_SelectedIndexChanged(object sender, EventArgs e)
        {
            TPCANBaudrate[] baudValue = (TPCANBaudrate[])Enum.GetValues(typeof(TPCANBaudrate));
            this.pcanBaudRate = baudValue[cbb_baudrates.SelectedIndex];
        }

        private void button_setHeartBeat_Click(object sender, EventArgs e)
        {
            if(this.lco.isopen()) {
                UInt16 hbInterval = Convert.ToUInt16(textBox_HBInterval.Text);
//                byte[] data = new byte[2];
//                data[0] = (byte)( ( hbInterval >> 8 ) & 0x00FF );
//                data[1] = (byte)( hbInterval & 0x00FF );
                this.lco.SDOwrite(10, 0x1017, 0, hbInterval, null);
            }
        }
    }
}
