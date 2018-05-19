using System.Text.RegularExpressions;

namespace LicenseManager
{
    public interface ILicenseManagerView
    {
        void SetController(LicenseManagerController controller);
        void SetStatusMessage(string errorMessage);
        void SetMaxKeyLength(int? keyLength);
        void SetValidCharacterRegex(Regex validCharacterRegex);
        void ActivateLicense();
        string GetLicenseKey();
        void ShowActivationSuccess(string message);
        void Close();
    }
}
