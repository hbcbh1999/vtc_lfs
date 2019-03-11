using System.Windows.Forms;
using VTC.UI;

namespace VTC
{
   partial class TrafficCounter
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
         if (disposing && (components != null))
         {
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TrafficCounter));
            this.pushStateTimer = new System.Windows.Forms.Timer(this.components);
            this.timeActiveTextBox = new System.Windows.Forms.TextBox();
            this.label11 = new System.Windows.Forms.Label();
            this.tbVistaStats = new System.Windows.Forms.TextBox();
            this.CameraComboBox = new System.Windows.Forms.ComboBox();
            this.btnConfigureRegions = new System.Windows.Forms.Button();
            this.heartbeatTimer = new System.Windows.Forms.Timer(this.components);
            this.SelectVideosButton = new System.Windows.Forms.Button();
            this.selectVideoFilesDialog = new System.Windows.Forms.OpenFileDialog();
            this.infoBox = new System.Windows.Forms.TextBox();
            this.generateReportButton = new System.Windows.Forms.Button();
            this.timer5minute = new System.Windows.Forms.Timer(this.components);
            this.timer15minute = new System.Windows.Forms.Timer(this.components);
            this.timer60minute = new System.Windows.Forms.Timer(this.components);
            this.trackedObjectsTextbox = new System.Windows.Forms.TextBox();
            this.licenseCheckTimer = new System.Windows.Forms.Timer(this.components);
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.loggingIndicator = new Indicators.LEDIndicator.LEDIndicator();
            this.framegrabbingIndicator = new Indicators.LEDIndicator.LEDIndicator();
            this.processingIndicator = new Indicators.LEDIndicator.LEDIndicator();
            this.sequencingIndicator = new Indicators.LEDIndicator.LEDIndicator();
            this.configurationIndicator = new Indicators.LEDIndicator.LEDIndicator();
            this.remainingTimeBox = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.frameTextbox = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.fpsTextbox = new System.Windows.Forms.TextBox();
            this.label8 = new System.Windows.Forms.Label();
            this.debugTextbox = new System.Windows.Forms.TextBox();
            this.userSettingsButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // pushStateTimer
            // 
            this.pushStateTimer.Enabled = true;
            this.pushStateTimer.Interval = 10000;
            this.pushStateTimer.Tick += new System.EventHandler(this.PushStateProcess);
            // 
            // timeActiveTextBox
            // 
            this.timeActiveTextBox.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.timeActiveTextBox.Location = new System.Drawing.Point(362, 557);
            this.timeActiveTextBox.Name = "timeActiveTextBox";
            this.timeActiveTextBox.ReadOnly = true;
            this.timeActiveTextBox.Size = new System.Drawing.Size(96, 21);
            this.timeActiveTextBox.TabIndex = 8;
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label11.Location = new System.Drawing.Point(287, 560);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(69, 15);
            this.label11.TabIndex = 64;
            this.label11.Text = "Time active";
            // 
            // tbVistaStats
            // 
            this.tbVistaStats.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tbVistaStats.Location = new System.Drawing.Point(11, 177);
            this.tbVistaStats.Multiline = true;
            this.tbVistaStats.Name = "tbVistaStats";
            this.tbVistaStats.ReadOnly = true;
            this.tbVistaStats.Size = new System.Drawing.Size(447, 320);
            this.tbVistaStats.TabIndex = 6;
            // 
            // CameraComboBox
            // 
            this.CameraComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.CameraComboBox.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.CameraComboBox.FormattingEnabled = true;
            this.CameraComboBox.Location = new System.Drawing.Point(12, 35);
            this.CameraComboBox.Name = "CameraComboBox";
            this.CameraComboBox.Size = new System.Drawing.Size(240, 23);
            this.CameraComboBox.TabIndex = 1;
            this.CameraComboBox.SelectedIndexChanged += new System.EventHandler(this.CameraComboBox_SelectedIndexChanged);
            // 
            // btnConfigureRegions
            // 
            this.btnConfigureRegions.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnConfigureRegions.Location = new System.Drawing.Point(12, 146);
            this.btnConfigureRegions.Name = "btnConfigureRegions";
            this.btnConfigureRegions.Size = new System.Drawing.Size(115, 22);
            this.btnConfigureRegions.TabIndex = 4;
            this.btnConfigureRegions.Text = "Configure regions";
            this.btnConfigureRegions.UseVisualStyleBackColor = true;
            this.btnConfigureRegions.Click += new System.EventHandler(this.btnConfigureRegions_Click);
            // 
            // heartbeatTimer
            // 
            this.heartbeatTimer.Enabled = true;
            this.heartbeatTimer.Interval = 5000;
            this.heartbeatTimer.Tick += new System.EventHandler(this.heartbeatTimer_Tick);
            // 
            // SelectVideosButton
            // 
            this.SelectVideosButton.BackColor = System.Drawing.Color.DodgerBlue;
            this.SelectVideosButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.SelectVideosButton.Font = new System.Drawing.Font("Arial", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.SelectVideosButton.ForeColor = System.Drawing.Color.White;
            this.SelectVideosButton.Location = new System.Drawing.Point(12, 63);
            this.SelectVideosButton.Name = "SelectVideosButton";
            this.SelectVideosButton.Size = new System.Drawing.Size(240, 44);
            this.SelectVideosButton.TabIndex = 2;
            this.SelectVideosButton.Text = "Load video";
            this.SelectVideosButton.UseVisualStyleBackColor = false;
            this.SelectVideosButton.Click += new System.EventHandler(this.SelectVideosButton_Click);
            // 
            // selectVideoFilesDialog
            // 
            this.selectVideoFilesDialog.Filter = "Video files|*.mp4;*.avi;*.wmv;*.3gp;*.asf;*.h264;*.mkv;*.ts;*.MOV;*.AVI;*.3GP;*.H" +
    "264;*.TS;*.ASF;*.MP4;*.WMV|All files|*.*";
            this.selectVideoFilesDialog.Multiselect = true;
            this.selectVideoFilesDialog.Title = "Select videos to process";
            // 
            // infoBox
            // 
            this.infoBox.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.infoBox.Location = new System.Drawing.Point(12, 547);
            this.infoBox.Multiline = true;
            this.infoBox.Name = "infoBox";
            this.infoBox.ReadOnly = true;
            this.infoBox.Size = new System.Drawing.Size(240, 58);
            this.infoBox.TabIndex = 7;
            // 
            // generateReportButton
            // 
            this.generateReportButton.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.generateReportButton.Location = new System.Drawing.Point(133, 146);
            this.generateReportButton.Name = "generateReportButton";
            this.generateReportButton.Size = new System.Drawing.Size(115, 22);
            this.generateReportButton.TabIndex = 65;
            this.generateReportButton.Text = "Generate Report";
            this.generateReportButton.UseVisualStyleBackColor = true;
            this.generateReportButton.Click += new System.EventHandler(this.generateReportButton_Click);
            // 
            // timer5minute
            // 
            this.timer5minute.Interval = 300000;
            this.timer5minute.Tick += new System.EventHandler(this.timer5minute_Tick);
            // 
            // timer15minute
            // 
            this.timer15minute.Interval = 900000;
            this.timer15minute.Tick += new System.EventHandler(this.timer15minute_Tick);
            // 
            // timer60minute
            // 
            this.timer60minute.Interval = 3600000;
            this.timer60minute.Tick += new System.EventHandler(this.timer60minute_Tick);
            // 
            // trackedObjectsTextbox
            // 
            this.trackedObjectsTextbox.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.trackedObjectsTextbox.Location = new System.Drawing.Point(11, 503);
            this.trackedObjectsTextbox.Multiline = true;
            this.trackedObjectsTextbox.Name = "trackedObjectsTextbox";
            this.trackedObjectsTextbox.ReadOnly = true;
            this.trackedObjectsTextbox.Size = new System.Drawing.Size(240, 38);
            this.trackedObjectsTextbox.TabIndex = 67;
            // 
            // licenseCheckTimer
            // 
            this.licenseCheckTimer.Enabled = true;
            this.licenseCheckTimer.Interval = 3600000;
            this.licenseCheckTimer.Tick += new System.EventHandler(this.licenseCheckTimer_Tick);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(309, 45);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(45, 13);
            this.label1.TabIndex = 74;
            this.label1.Text = "Logging";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(309, 71);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(82, 13);
            this.label2.TabIndex = 75;
            this.label2.Text = "Frame Grabbing";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(309, 97);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(59, 13);
            this.label3.TabIndex = 76;
            this.label3.Text = "Processing";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(309, 123);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(64, 13);
            this.label4.TabIndex = 77;
            this.label4.Text = "Sequencing";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(309, 149);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(69, 13);
            this.label5.TabIndex = 78;
            this.label5.Text = "Configuration";
            // 
            // loggingIndicator
            // 
            this.loggingIndicator.LEDColor = Indicators.LEDIndicator.LEDIndicator.LEDColorOptions.Green;
            this.loggingIndicator.Location = new System.Drawing.Point(286, 40);
            this.loggingIndicator.Margin = new System.Windows.Forms.Padding(0);
            this.loggingIndicator.Name = "loggingIndicator";
            this.loggingIndicator.On = false;
            this.loggingIndicator.Size = new System.Drawing.Size(20, 20);
            this.loggingIndicator.TabIndex = 80;
            // 
            // framegrabbingIndicator
            // 
            this.framegrabbingIndicator.LEDColor = Indicators.LEDIndicator.LEDIndicator.LEDColorOptions.Green;
            this.framegrabbingIndicator.Location = new System.Drawing.Point(286, 64);
            this.framegrabbingIndicator.Margin = new System.Windows.Forms.Padding(0);
            this.framegrabbingIndicator.Name = "framegrabbingIndicator";
            this.framegrabbingIndicator.On = false;
            this.framegrabbingIndicator.Size = new System.Drawing.Size(20, 20);
            this.framegrabbingIndicator.TabIndex = 81;
            // 
            // processingIndicator
            // 
            this.processingIndicator.LEDColor = Indicators.LEDIndicator.LEDIndicator.LEDColorOptions.Green;
            this.processingIndicator.Location = new System.Drawing.Point(286, 90);
            this.processingIndicator.Margin = new System.Windows.Forms.Padding(0);
            this.processingIndicator.Name = "processingIndicator";
            this.processingIndicator.On = false;
            this.processingIndicator.Size = new System.Drawing.Size(20, 20);
            this.processingIndicator.TabIndex = 82;
            // 
            // sequencingIndicator
            // 
            this.sequencingIndicator.LEDColor = Indicators.LEDIndicator.LEDIndicator.LEDColorOptions.Green;
            this.sequencingIndicator.Location = new System.Drawing.Point(286, 116);
            this.sequencingIndicator.Margin = new System.Windows.Forms.Padding(0);
            this.sequencingIndicator.Name = "sequencingIndicator";
            this.sequencingIndicator.On = false;
            this.sequencingIndicator.Size = new System.Drawing.Size(20, 20);
            this.sequencingIndicator.TabIndex = 83;
            // 
            // configurationIndicator
            // 
            this.configurationIndicator.LEDColor = Indicators.LEDIndicator.LEDIndicator.LEDColorOptions.Green;
            this.configurationIndicator.Location = new System.Drawing.Point(286, 142);
            this.configurationIndicator.Margin = new System.Windows.Forms.Padding(0);
            this.configurationIndicator.Name = "configurationIndicator";
            this.configurationIndicator.On = false;
            this.configurationIndicator.Size = new System.Drawing.Size(20, 20);
            this.configurationIndicator.TabIndex = 84;
            // 
            // remainingTimeBox
            // 
            this.remainingTimeBox.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.remainingTimeBox.Location = new System.Drawing.Point(362, 530);
            this.remainingTimeBox.Name = "remainingTimeBox";
            this.remainingTimeBox.ReadOnly = true;
            this.remainingTimeBox.Size = new System.Drawing.Size(96, 21);
            this.remainingTimeBox.TabIndex = 85;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label6.Location = new System.Drawing.Point(257, 533);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(99, 15);
            this.label6.TabIndex = 86;
            this.label6.Text = "Remaining Time";
            // 
            // frameTextbox
            // 
            this.frameTextbox.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.frameTextbox.Location = new System.Drawing.Point(362, 503);
            this.frameTextbox.Name = "frameTextbox";
            this.frameTextbox.ReadOnly = true;
            this.frameTextbox.Size = new System.Drawing.Size(96, 21);
            this.frameTextbox.TabIndex = 87;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label7.Location = new System.Drawing.Point(313, 506);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(43, 15);
            this.label7.TabIndex = 88;
            this.label7.Text = "Frame";
            // 
            // fpsTextbox
            // 
            this.fpsTextbox.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.fpsTextbox.Location = new System.Drawing.Point(362, 584);
            this.fpsTextbox.Name = "fpsTextbox";
            this.fpsTextbox.ReadOnly = true;
            this.fpsTextbox.Size = new System.Drawing.Size(96, 21);
            this.fpsTextbox.TabIndex = 89;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label8.Location = new System.Drawing.Point(326, 587);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(30, 15);
            this.label8.TabIndex = 90;
            this.label8.Text = "FPS";
            // 
            // debugTextbox
            // 
            this.debugTextbox.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.debugTextbox.Location = new System.Drawing.Point(12, 611);
            this.debugTextbox.Multiline = true;
            this.debugTextbox.Name = "debugTextbox";
            this.debugTextbox.ReadOnly = true;
            this.debugTextbox.Size = new System.Drawing.Size(446, 58);
            this.debugTextbox.TabIndex = 91;
            // 
            // userSettingsButton
            // 
            this.userSettingsButton.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.userSettingsButton.Location = new System.Drawing.Point(12, 120);
            this.userSettingsButton.Name = "userSettingsButton";
            this.userSettingsButton.Size = new System.Drawing.Size(115, 22);
            this.userSettingsButton.TabIndex = 92;
            this.userSettingsButton.Text = "User settings";
            this.userSettingsButton.UseVisualStyleBackColor = true;
            this.userSettingsButton.Click += new System.EventHandler(this.userSettingsButton_Click);
            // 
            // TrafficCounter
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.ClientSize = new System.Drawing.Size(468, 676);
            this.Controls.Add(this.userSettingsButton);
            this.Controls.Add(this.debugTextbox);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.fpsTextbox);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.frameTextbox);
            this.Controls.Add(this.remainingTimeBox);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.configurationIndicator);
            this.Controls.Add(this.sequencingIndicator);
            this.Controls.Add(this.processingIndicator);
            this.Controls.Add(this.framegrabbingIndicator);
            this.Controls.Add(this.loggingIndicator);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.trackedObjectsTextbox);
            this.Controls.Add(this.generateReportButton);
            this.Controls.Add(this.infoBox);
            this.Controls.Add(this.SelectVideosButton);
            this.Controls.Add(this.timeActiveTextBox);
            this.Controls.Add(this.label11);
            this.Controls.Add(this.tbVistaStats);
            this.Controls.Add(this.CameraComboBox);
            this.Controls.Add(this.btnConfigureRegions);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimumSize = new System.Drawing.Size(291, 543);
            this.Name = "TrafficCounter";
            this.Text = "VTC";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.TrafficCounter_FormClosed);
            this.ResumeLayout(false);
            this.PerformLayout();

      }

      #endregion

      private Timer pushStateTimer;
      private TextBox timeActiveTextBox;
      private Label label11;
      private TextBox tbVistaStats;
      private ComboBox CameraComboBox;
      private Button btnConfigureRegions;
      private Timer heartbeatTimer;
        private Button SelectVideosButton;
        private OpenFileDialog selectVideoFilesDialog;
        private TextBox infoBox;
        private Button generateReportButton;
        private Timer timer5minute;
        private Timer timer15minute;
        private Timer timer60minute;
        private TextBox trackedObjectsTextbox;
        private Timer licenseCheckTimer;
        private Label label1;
        private Label label2;
        private Label label3;
        private Label label4;
        private Label label5;
        private Indicators.LEDIndicator.LEDIndicator loggingIndicator;
        private Indicators.LEDIndicator.LEDIndicator framegrabbingIndicator;
        private Indicators.LEDIndicator.LEDIndicator processingIndicator;
        private Indicators.LEDIndicator.LEDIndicator sequencingIndicator;
        private Indicators.LEDIndicator.LEDIndicator configurationIndicator;
        private TextBox remainingTimeBox;
        private Label label6;
        private TextBox frameTextbox;
        private Label label7;
        private TextBox fpsTextbox;
        private Label label8;
        private TextBox debugTextbox;
        private Button userSettingsButton;
    }
}