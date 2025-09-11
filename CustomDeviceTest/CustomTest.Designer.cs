
namespace CustomDeviceTest
{
    partial class CustomTest
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && ( components != null )) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.gbUsbComm = new System.Windows.Forms.GroupBox();
            this.lblSerialNumber = new System.Windows.Forms.Label();
            this.btnRefresh = new System.Windows.Forms.Button();
            this.cboDongleSN = new System.Windows.Forms.ComboBox();
            this.btnConnect = new System.Windows.Forms.Button();
            this.btnSendTest = new System.Windows.Forms.Button();
            this.btnCanFdSend = new System.Windows.Forms.Button();
            this.rtb_log = new System.Windows.Forms.RichTextBox();
            this.gbUsbComm.SuspendLayout();
            this.SuspendLayout();
            // 
            // gbUsbComm
            // 
            this.gbUsbComm.Controls.Add(this.lblSerialNumber);
            this.gbUsbComm.Controls.Add(this.btnRefresh);
            this.gbUsbComm.Controls.Add(this.cboDongleSN);
            this.gbUsbComm.Location = new System.Drawing.Point(22, 18);
            this.gbUsbComm.Name = "gbUsbComm";
            this.gbUsbComm.Size = new System.Drawing.Size(285, 66);
            this.gbUsbComm.TabIndex = 7;
            this.gbUsbComm.TabStop = false;
            this.gbUsbComm.Text = "USB Dongle";
            // 
            // lblSerialNumber
            // 
            this.lblSerialNumber.AutoSize = true;
            this.lblSerialNumber.Location = new System.Drawing.Point(13, 31);
            this.lblSerialNumber.Name = "lblSerialNumber";
            this.lblSerialNumber.Size = new System.Drawing.Size(25, 13);
            this.lblSerialNumber.TabIndex = 3;
            this.lblSerialNumber.Text = "SN:";
            // 
            // btnRefresh
            // 
            this.btnRefresh.Location = new System.Drawing.Point(190, 27);
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.Size = new System.Drawing.Size(73, 21);
            this.btnRefresh.TabIndex = 1;
            this.btnRefresh.Text = "Refresh";
            this.btnRefresh.UseVisualStyleBackColor = true;
            this.btnRefresh.Click += new System.EventHandler(this.btnRefresh_Click);
            // 
            // cboDongleSN
            // 
            this.cboDongleSN.FormattingEnabled = true;
            this.cboDongleSN.Location = new System.Drawing.Point(45, 27);
            this.cboDongleSN.Name = "cboDongleSN";
            this.cboDongleSN.Size = new System.Drawing.Size(139, 21);
            this.cboDongleSN.TabIndex = 2;
            // 
            // btnConnect
            // 
            this.btnConnect.Location = new System.Drawing.Point(313, 36);
            this.btnConnect.Name = "btnConnect";
            this.btnConnect.Size = new System.Drawing.Size(74, 38);
            this.btnConnect.TabIndex = 8;
            this.btnConnect.Text = "Connect";
            this.btnConnect.UseVisualStyleBackColor = true;
            this.btnConnect.Click += new System.EventHandler(this.btnConnect_Click);
            // 
            // btnSendTest
            // 
            this.btnSendTest.Location = new System.Drawing.Point(470, 34);
            this.btnSendTest.Name = "btnSendTest";
            this.btnSendTest.Size = new System.Drawing.Size(121, 41);
            this.btnSendTest.TabIndex = 9;
            this.btnSendTest.Text = "Test CAN-CC Send";
            this.btnSendTest.UseVisualStyleBackColor = true;
            this.btnSendTest.Click += new System.EventHandler(this.btnSendTest_Click);
            // 
            // btnCanFdSend
            // 
            this.btnCanFdSend.Location = new System.Drawing.Point(597, 33);
            this.btnCanFdSend.Name = "btnCanFdSend";
            this.btnCanFdSend.Size = new System.Drawing.Size(121, 41);
            this.btnCanFdSend.TabIndex = 10;
            this.btnCanFdSend.Text = "Test CAN-FD Send";
            this.btnCanFdSend.UseVisualStyleBackColor = true;
            this.btnCanFdSend.Click += new System.EventHandler(this.btnCanFdSend_Click);
            // 
            // rtb_log
            // 
            this.rtb_log.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.rtb_log.Location = new System.Drawing.Point(22, 99);
            this.rtb_log.Name = "rtb_log";
            this.rtb_log.Size = new System.Drawing.Size(758, 339);
            this.rtb_log.TabIndex = 11;
            this.rtb_log.Text = "";
            this.rtb_log.WordWrap = false;
            this.rtb_log.DoubleClick += new System.EventHandler(this.rtb_log_DoubleClick);
            // 
            // CustomTest
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(792, 450);
            this.Controls.Add(this.rtb_log);
            this.Controls.Add(this.btnCanFdSend);
            this.Controls.Add(this.btnSendTest);
            this.Controls.Add(this.btnConnect);
            this.Controls.Add(this.gbUsbComm);
            this.Name = "CustomTest";
            this.Text = "Custom CAN-CC/FD Adapter Test";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.CustomTest_FormClosing);
            this.gbUsbComm.ResumeLayout(false);
            this.gbUsbComm.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox gbUsbComm;
        private System.Windows.Forms.Label lblSerialNumber;
        private System.Windows.Forms.Button btnRefresh;
        private System.Windows.Forms.ComboBox cboDongleSN;
        private System.Windows.Forms.Button btnConnect;
        private System.Windows.Forms.Button btnSendTest;
        private System.Windows.Forms.Button btnCanFdSend;
        private System.Windows.Forms.RichTextBox rtb_log;
    }
}

