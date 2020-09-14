using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using VTC.Reporting.Properties;
using VTC.Common;
using NLog;

namespace VTC.Reporting
{
    public class SummaryReportGenerator
    {

        private static readonly Logger Logger = LogManager.GetLogger("main.form");

        public static void CopyAssetsToExportFolder(string exportPath)
        {
            try
            {
                if (!Directory.Exists(exportPath + "/ReportAssets"))
                {
                    Microsoft.VisualBasic.FileIO.FileSystem.CopyDirectory("./ReportAssets", exportPath + "/ReportAssets");
                }
            }
            catch (IOException e)
            {
                Logger.Log(LogLevel.Error, e, e.Message);
#if DEBUG
                throw;
#endif          
            }
        }

        public static List<string> GetAllUniqueApproachNames(List<Movement> movements)
        {
            var approachNames = new List<string>();
            foreach (var m in movements)
            {
                if (!approachNames.Contains(m.Approach))
                {
                    approachNames.Add(m.Approach);
                }
            }

            return approachNames;
        }

        public static void GenerateSummaryReportHtml(string exportPath, string location, DateTime videoTime, List<Movement> movements)
        {
            try
            {
                var reportPath = Path.Combine(exportPath, "Report.html");
                var approachNames = GetAllUniqueApproachNames(movements);
                var summaryReport = new SummaryReportTemplate {Movements = movements, Location = location, VideoTime = videoTime, ApproachNames = approachNames }.TransformText();
                File.WriteAllText(reportPath, summaryReport); //Save
            }
            catch (IOException e)
            {
                Logger.Log(LogLevel.Error, e, e.Message);
#if DEBUG
                throw;
#endif          
            }

        }

    }
}
