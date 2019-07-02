using System;
using System.Windows.Forms;

namespace VTC.RegionConfiguration
{
    public sealed partial class ExamplePathCreatePrompt : Form
    {
        public string Approach => approachTextbox.Text;

        public string Exit => exitTextbox.Text;

        public bool Ignored => ignoredCheckbox.Checked;

        public bool PedestrianOnly => pedestrianCheckbox.Checked;

        public ExamplePathCreatePrompt()
        {
            InitializeComponent();

            approachTextbox.TextChanged += (sender, args) =>
            {
                btnOK.Enabled = !(string.IsNullOrWhiteSpace(approachTextbox.Text) || string.IsNullOrWhiteSpace(exitTextbox.Text));
            };

            exitTextbox.TextChanged += (sender, args) =>
            {
                btnOK.Enabled = !(string.IsNullOrWhiteSpace(approachTextbox.Text) || string.IsNullOrWhiteSpace(exitTextbox.Text));
            };
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
