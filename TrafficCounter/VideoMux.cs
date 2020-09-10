using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using VTC.Common;
using VTC.UserConfiguration;

namespace VTC
{
    public partial class VideoMux : Form
    {
        public Dictionary<CheckBox, PictureBox> DisplayLookup = new Dictionary<CheckBox, PictureBox>();
        private readonly Timer _updateDebounceTimer;
        private int _displayedRowCount;
        private int _displayedColCount;

        private UserConfig _userConfig = new UserConfig();

        public VideoMux()
        {
            InitializeComponent();

            LoadUserConfig();

            AddLogo(_userConfig.Logopath);
            AddUserText(_userConfig.Organization);

            _updateDebounceTimer = new Timer {Interval = 1500};
            _updateDebounceTimer.Tick += UpdateDebounceTimer_Tick;

            UpdateMux();
        }

        private void UpdateDebounceTimer_Tick(object sender, EventArgs e)
        {
            var timer = sender as Timer;
            timer?.Stop();
            UpdateMux();
        }

        private void UpdateMux()
        {
            var enabled = DisplayLookup.Where(kvp => kvp.Key.Checked).Select(kvp => kvp.Value).ToList();

            tlpVideoDisplayTable.Controls.Clear();

            if (enabled.Count <= 0)
                return;

            var cols = (int)Math.Ceiling(Math.Sqrt(enabled.Count));

            var rows = enabled.Count / cols; 
            while(cols * rows < enabled.Count)
            {
                rows++;
            }

            tlpVideoDisplayTable.ColumnStyles.Clear();
            tlpVideoDisplayTable.RowStyles.Clear();
            tlpVideoDisplayTable.ColumnCount = cols;
            tlpVideoDisplayTable.RowCount = rows;
            for (var i = 0; i < cols; i++)
            {
                tlpVideoDisplayTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, (tlpVideoDisplayTable.Width/cols)));
            }
            for (int i = 0; i < rows; i++)
            {
                tlpVideoDisplayTable.RowStyles.Add(new RowStyle(SizeType.Percent, (tlpVideoDisplayTable.Height / rows)));
            }


            foreach (var input in enabled)
            {
                tlpVideoDisplayTable.Controls.Add(input);
            }

            bool resize = false;
            if (rows != _displayedRowCount)
            {
                _displayedRowCount = rows;
                resize = true;
            }
            if (cols != _displayedColCount)
            {
                _displayedColCount = cols;
                resize = true;
            }
        }

        public void AddLogo(string logoPath)
        { 
            if(System.IO.File.Exists(logoPath))
            {
                var pictureBox = new PictureBox();
                pictureBox.Image = Image.FromFile(logoPath);
                pictureBox.SizeMode = PictureBoxSizeMode.StretchImage;
                pictureBox.Size = new Size(50,50);
                pictureBox.Margin = new Padding(20);

                tableLayoutPanel1.RowCount += 1;
                tableLayoutPanel1.Controls.Add(pictureBox);        
            }
        }

        public void AddUserText(string userText)
        {
            if (userText == null)
            {
                return;
            }

            if(userText.Any())
            {
                var userTextLabel = new Label();
                userTextLabel.AutoSize = true;
                userTextLabel.Text = userText;
                userTextLabel.Font = new Font(FontFamily.Families.First(ff => ff.Name == "Raleway"), (float) 20.0, FontStyle.Regular);
                userTextLabel.Margin = new Padding(20);
                userTextLabel.MinimumSize = new Size(300, 20);

                tableLayoutPanel1.RowCount += 1;
                tableLayoutPanel1.Controls.Add(userTextLabel);        
            }
        }

        public void AddDisplay(PictureBox imageBox, string name)
        {

            if (DisplayLookup.Values.Any(d => d == imageBox))
                return;

            var radioButton = new CheckBox()
            {
                Text = name,
                Appearance = Appearance.Button,
                AutoSize = false,
                Checked = true,
                Anchor = AnchorStyles.Right | AnchorStyles.Left,
                Width = 100,
                MaximumSize = new Size(100,40),
            };

            radioButton.CheckedChanged += CameraSelectButton_CheckedChanged;

            DisplayLookup[radioButton] = imageBox;

            tableLayoutPanel1.RowCount += 1;
            tableLayoutPanel1.Controls.Add(radioButton);

            DebouncedUpdate();
        }

        private void CameraSelectButton_CheckedChanged(object sender, EventArgs e)
        {
            DebouncedUpdate();
        }

        private void DebouncedUpdate()
        {
            _updateDebounceTimer.Stop();
            _updateDebounceTimer.Start();
        }

        private void LoadUserConfig()
        {
            string UserConfigSavePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) +
                                        "\\VTC\\userConfig.xml";
            IUserConfigDataAccessLayer _userConfigDataAccessLayer = new FileUserConfigDal(UserConfigSavePath);

            _userConfig = _userConfigDataAccessLayer.LoadUserConfig();
        }
    }
}