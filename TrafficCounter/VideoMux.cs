using Emgu.CV.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace VTC
{
    public partial class VideoMux : Form
    {
        public Dictionary<CheckBox, ImageBox> DisplayLookup = new Dictionary<CheckBox, ImageBox>();
        private readonly Timer _updateDebounceTimer;
        private int _displayedRowCount;
        private int _displayedColCount;

        public VideoMux()
        {
            InitializeComponent();

            _updateDebounceTimer = new Timer {Interval = 1500};
            _updateDebounceTimer.Tick += UpdateDebounceTimer_Tick;

            UpdateMux();

            Resize += VideoMux_Resize;
        }

        private void VideoMux_Resize(object sender, EventArgs e)
        {
            AutoscaleInputs();
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

            if (resize)
                AutoscaleInputs();

        }

        public void AutoscaleInputs()
        {
            foreach(var control in tlpVideoDisplayTable.Controls)
            {
                var box = control as ImageBox;

                if (box?.Image == null)
                    continue;

                try
                {
                    var size = box.Size;
                    var imgSize = box.Image.Size;

                    var horizontalScale = size.Width / (double)imgSize.Width;
                    var verticalScale = size.Height / (double)imgSize.Height;
                    box.SetZoomScale(Math.Min(horizontalScale, verticalScale), new Point());
                }
                catch(NullReferenceException ex)
                {
                    //TODO: Fix this - try/catch is a bandaid.
                }
            }
        }

        public void AddDisplay(ImageBox imageBox, string name)
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
    }
}