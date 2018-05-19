using System;
using System.Net;

namespace VTC.Reporting.ReportItems
{
    public class FtpSendFileReportItem : ReportItem
    {
        private readonly Func<Byte[]> _getData;
        private readonly Uri _destinationFilePath;
        private readonly ICredentials _credentials;

        public FtpSendFileReportItem(int reportIntervalMinutes, Uri destPath, ICredentials credentials, Func<Byte[]> getData)
            : base(reportIntervalMinutes)
        {
            _getData = getData;
            _destinationFilePath = destPath;
            _credentials = credentials;
        }

        protected override void Report()
        {
            if (null == _getData) return;

            var data = _getData();

            var wc = new WebClient {Credentials = _credentials};
            wc.UploadDataCompleted += wc_UploadDataCompleted;

            wc.UploadDataAsync(_destinationFilePath, data);

        }

        void wc_UploadDataCompleted(object sender, UploadDataCompletedEventArgs e)
        {
        }
    }
}
