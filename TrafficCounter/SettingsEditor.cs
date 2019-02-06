using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace VTC
{
    public partial class SettingsEditor : Form
    {
        public SettingsEditor()
        {
            InitializeComponent();
            settingsPropertyGrid.SelectedObject = Properties.Settings.Default;
        }

        private void saveButton_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.Save();
        }

        private void resetToDefaultButton_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.Reset();
            Properties.Settings.Default.Save();
        }
    }
}
