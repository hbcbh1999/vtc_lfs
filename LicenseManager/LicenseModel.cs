using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using LicenseSpot.Framework;

namespace LicenseManager
{
    public class LicenseModel
    {
        private readonly ExtendedLicense _license;
        private string _key;

        public int? MaxKeyLength = 28;
        public Regex ValidCharacterRegex = new Regex(@"[\w\d]");
         
        public LicenseModel(Type licensedObjectType, object licensedObjectInstance, string publicKey)
        {
            try
            {
                _license = ExtendedLicenseManager.GetLicense(licensedObjectType, licensedObjectInstance, publicKey);
            }
            catch (Exception ex)
            {
                var message = $"Failed to retrieve license. type={licensedObjectType} publicKey={publicKey}";
                throw new Exception(message, ex);
            }
        }

        public bool ValidateKeyFormat(string key, out string message)
        {
            if (Regex.IsMatch(key, @"[^\w\d-]"))
            {
                message = "Key contains invalid characters." + Environment.NewLine + "Only letters, numbers, hyphens";
                return false;
            }

            if (key.Replace("-", string.Empty).Length != 28)
            {
                message = "Key must be 28 characters not including hyphens.";
                return false;
            }

            _key = key.ToUpperInvariant();
            message = String.Empty;

            return true;
        }

        public bool TryActivate(string key, out string message)
        {
            try
            {
                message = _license.Activate(key, true);
                return true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return false;
            }
        }

        // TODO: This doesnt seem to work within the IPManager
        //public void Deactivate()
        //{
        //    var result = License.Deactivate();
        //}

        public bool IsActivatedAndGenuine
        {
            get
            {
                GenuineResult result;
                try
                {
                    result = _license.IsGenuineEx();
                    if (result == GenuineResult.InternetError)
                    {
                        // TODO: Handle connection errors
                    }
                }
                catch (ExtendedLicenseException)
                {
                    return false;
                }

                return result == GenuineResult.Genuine || result == GenuineResult.InternetError;
            }
        }

    }
}
