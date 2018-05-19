using System;
using System.Windows.Forms;

namespace LicenseManager
{
    public class LicensedFormContext : ApplicationContext
    {
        public LicensedFormContext(string publicKey, Func<bool, string, Form> licensedFormCreator, string licensedFormArguments)
        {
            var model = new LicenseModel(typeof(LicensedFormContext), this, publicKey);

            if (!model.IsActivatedAndGenuine)
            {
                var view = new LicenseManagerView();
                var controller = new LicenseManagerController(model, view);
                view.SetController(controller);
                view.Show();
            }

            MainForm = licensedFormCreator(model.IsActivatedAndGenuine, licensedFormArguments);
        }
    }
}
