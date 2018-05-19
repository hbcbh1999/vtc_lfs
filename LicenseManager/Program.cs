using LicenseSpot.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LicenseManager
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            string publicKey = "<RSAKeyValue><Modulus>uHfytqHYNN+1mYDeocM6fjotTwmQgGphb4XaMtrADk3+oa03ZWMXkIFZyL7mzG/hPpd/Q+waSWiklL7QR4k1XujCbcLNngY0gz4qaKFq/LqCSHzX7zHQ3N1Lyg368XK+uLtAxX9fGF9vOgloIPnDb/4Jol6nohouKODSZc+rf43D2q6mYWApWPrBFrhGyeO9mF3khYkFiJTXnCDku8WbJBdwK963RmYkI5p+jyoDi0Uy5a2+TmU9jnzK7zyRybjd4f1o7bfFQlBouSCrwVzU0n8PmtrU5boSh45RbDuy5FRYknxBM9djQvewydLTVHztZWjeQ0Q3JxH03/6DIY0Lsw==</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new LicensedFormContext(publicKey, ProcessLicensedResult, String.Empty));
        }

        private static Form ProcessLicensedResult(bool isLicensed, string args)
        {
            return null;
        }
    }
}
