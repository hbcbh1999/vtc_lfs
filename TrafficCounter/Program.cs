using System;
using System.Reflection;
using System.Windows.Forms;
using System.Management;
using NLog;
using LicenseManager;
using VTC.Common;

namespace VTC
{
    static class Program
    {
        private static readonly Logger Logger = LogManager.GetLogger("app.global");

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.ThreadException += (_, e) => Logger.Error(e.Exception, "Thread exception");
            AppDomain.CurrentDomain.UnhandledException += (_, e) => Logger.Error((Exception)e.ExceptionObject, "Unhandled exception");

            int delayMs = 2000;
            var ss = new SplashScreen(delayMs);
            ss.Show();

            Logger.Info("***** Start. v." + Assembly.GetExecutingAssembly().GetName().Version);
            if (args.Length > 0)
            {
                Logger.Info("Arguments: " + string.Join(";", args));
            }

            string appArgument = null;
            if (args.Length == 1) appArgument = args[0];


            string publicKey = "<RSAKeyValue><Modulus>uHfytqHYNN+1mYDeocM6fjotTwmQgGphb4XaMtrADk3+oa03ZWMXkIFZyL7mzG/hPpd/Q+waSWiklL7QR4k1XujCbcLNngY0gz4qaKFq/LqCSHzX7zHQ3N1Lyg368XK+uLtAxX9fGF9vOgloIPnDb/4Jol6nohouKODSZc+rf43D2q6mYWApWPrBFrhGyeO9mF3khYkFiJTXnCDku8WbJBdwK963RmYkI5p+jyoDi0Uy5a2+TmU9jnzK7zyRybjd4f1o7bfFQlBouSCrwVzU0n8PmtrU5boSh45RbDuy5FRYknxBM9djQvewydLTVHztZWjeQ0Q3JxH03/6DIY0Lsw==</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";

            var context = new LicensedFormContext(publicKey, CreateTrafficCounterForm, appArgument);

            if (null != context.MainForm)
            try{
            Application.Run(context); 
            }
            catch(AccessViolationException ex)
            { 
                Logger.Log(LogLevel.Error, ex.Message);       
            }
        }

        private static Form CreateTrafficCounterForm(bool isLicensed, string args)
        {
            return new TrafficCounter(isLicensed, args);
        }
    }
}
