using LicenseSpot.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;

namespace LicenseManager
{
    public partial class LicenseManagerView : Form, ILicenseManagerView
    {
        private LicenseManagerController _controller;
        private int? _maxKeyLength;
        private Regex _validCharacterRegex;
        
        public LicenseManagerView()
        {
            InitializeComponent();
        }

        public void SetMaxKeyLength(int? maxKeyLength)
        {
            _maxKeyLength = maxKeyLength;
        }

        public void SetValidCharacterRegex(Regex validCharacterRegex)
        {
            _validCharacterRegex = validCharacterRegex;
        }

        public void SetController(LicenseManagerController controller)
        {
            _controller = controller;
        }

        public void SetStatusMessage(string errorMessage)
        {
            lblKeyError.Text = errorMessage;
        }

        public string GetLicenseKey()
        {
            return tbKey.Text;
        }

        public void ActivateLicense()
        {
            _controller?.Activate();
        }

        public void ShowActivationSuccess(string message)
        {
            MessageBox.Show(message, "Activation Successful");
            Close();
        }

        private void tbKey_KeyPress(object sender, KeyPressEventArgs e)
        {
            //TODO: Handle pasting invalid characters

            var tb = sender as TextBox;
            string text = null == tb ? "" : tb.Text;

            if (char.IsControl(e.KeyChar))
                return;

            if (_maxKeyLength.HasValue && text.Replace("-", String.Empty).Length >= _maxKeyLength.Value)
            {
                e.Handled = true;
                return;
            }

            if (null != _validCharacterRegex && !_validCharacterRegex.IsMatch(e.KeyChar.ToString()))
            {
                e.Handled = true;
                return;
            }

            e.KeyChar = char.ToUpperInvariant(e.KeyChar);
        }

        private void btnActivate_Click(object sender, EventArgs e)
        {
            ActivateLicense();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
