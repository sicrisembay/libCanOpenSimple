
namespace SimplePeakTest
{
    partial class PeakTest
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
            this.cbb_channel = new System.Windows.Forms.ComboBox();
            this.cbb_baudrates = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.btn_HwRefresh = new System.Windows.Forms.Button();
            this.btn_connect = new System.Windows.Forms.Button();
            this.textBox_HBInterval = new System.Windows.Forms.TextBox();
            this.button_setHeartBeat = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // cbb_channel
            // 
            this.cbb_channel.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbb_channel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cbb_channel.Location = new System.Drawing.Point(15, 27);
            this.cbb_channel.Name = "cbb_channel";
            this.cbb_channel.Size = new System.Drawing.Size(119, 21);
            this.cbb_channel.TabIndex = 42;
            this.cbb_channel.SelectedIndexChanged += new System.EventHandler(this.cbb_channel_SelectedIndexChanged);
            // 
            // cbb_baudrates
            // 
            this.cbb_baudrates.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbb_baudrates.Location = new System.Drawing.Point(143, 27);
            this.cbb_baudrates.Name = "cbb_baudrates";
            this.cbb_baudrates.Size = new System.Drawing.Size(152, 21);
            this.cbb_baudrates.TabIndex = 43;
            this.cbb_baudrates.SelectedIndexChanged += new System.EventHandler(this.cbb_baudrates_SelectedIndexChanged);
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(56, 15);
            this.label1.TabIndex = 44;
            this.label1.Text = "Channel:";
            // 
            // label2
            // 
            this.label2.Location = new System.Drawing.Point(140, 9);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(56, 15);
            this.label2.TabIndex = 45;
            this.label2.Text = "Baudrate:";
            // 
            // btn_HwRefresh
            // 
            this.btn_HwRefresh.Cursor = System.Windows.Forms.Cursors.Default;
            this.btn_HwRefresh.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.btn_HwRefresh.Location = new System.Drawing.Point(311, 16);
            this.btn_HwRefresh.Name = "btn_HwRefresh";
            this.btn_HwRefresh.Size = new System.Drawing.Size(65, 40);
            this.btn_HwRefresh.TabIndex = 46;
            this.btn_HwRefresh.Text = "Refresh";
            this.btn_HwRefresh.Click += new System.EventHandler(this.btn_HwRefresh_Click);
            // 
            // btn_connect
            // 
            this.btn_connect.Cursor = System.Windows.Forms.Cursors.Default;
            this.btn_connect.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.btn_connect.Location = new System.Drawing.Point(382, 16);
            this.btn_connect.Name = "btn_connect";
            this.btn_connect.Size = new System.Drawing.Size(65, 41);
            this.btn_connect.TabIndex = 47;
            this.btn_connect.Text = "Connect";
            this.btn_connect.Click += new System.EventHandler(this.btn_connect_Click);
            // 
            // textBox_HBInterval
            // 
            this.textBox_HBInterval.Location = new System.Drawing.Point(129, 82);
            this.textBox_HBInterval.Name = "textBox_HBInterval";
            this.textBox_HBInterval.Size = new System.Drawing.Size(100, 20);
            this.textBox_HBInterval.TabIndex = 48;
            this.textBox_HBInterval.Text = "1000";
            // 
            // button_setHeartBeat
            // 
            this.button_setHeartBeat.Location = new System.Drawing.Point(38, 80);
            this.button_setHeartBeat.Name = "button_setHeartBeat";
            this.button_setHeartBeat.Size = new System.Drawing.Size(75, 23);
            this.button_setHeartBeat.TabIndex = 49;
            this.button_setHeartBeat.Text = "Heartbeat";
            this.button_setHeartBeat.UseVisualStyleBackColor = true;
            this.button_setHeartBeat.Click += new System.EventHandler(this.button_setHeartBeat_Click);
            // 
            // PeakTest
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.button_setHeartBeat);
            this.Controls.Add(this.textBox_HBInterval);
            this.Controls.Add(this.btn_connect);
            this.Controls.Add(this.btn_HwRefresh);
            this.Controls.Add(this.cbb_channel);
            this.Controls.Add(this.cbb_baudrates);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.label2);
            this.Name = "PeakTest";
            this.Text = "Test";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox cbb_channel;
        private System.Windows.Forms.ComboBox cbb_baudrates;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button btn_HwRefresh;
        private System.Windows.Forms.Button btn_connect;
        private System.Windows.Forms.TextBox textBox_HBInterval;
        private System.Windows.Forms.Button button_setHeartBeat;
    }
}

