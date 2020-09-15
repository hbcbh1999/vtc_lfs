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
    public class ReportGenerator
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

        public static List<string> GetAllUniqueExitNames(List<Movement> movements)
        {
            var names = new List<string>();
            foreach (var m in movements)
            {
                if (!names.Contains(m.Exit))
                {
                    names.Add(m.Exit);
                }
            }

            return names;
        }

        public static List<(string Approach,string Exit)> GetAllUniqueApproachExitPairs(List<Movement> movements)
        {
            List<(string Approach, string Exit)> pairs = new List<(string Approach, string Exit)>();
            foreach (var m in movements)
            {
                if (!pairs.Contains((m.Approach, m.Exit)))
                {
                    pairs.Add((m.Approach,m.Exit));
                }
            }
            return pairs;
        }

        public static void GenerateSummaryReportHtml(string exportPath, string location, DateTime videoTime, List<Movement> movements)
        {
            try
            {
                var reportPath = Path.Combine(exportPath, "Report.html");
                var approachNames = GetAllUniqueApproachNames(movements).OrderByDescending(name => name).Reverse().ToArray();
                var exitNames = GetAllUniqueExitNames(movements).OrderByDescending(name => name).Reverse().ToArray();
                var binnedMovements15 = BinnedMovements.BinMovementsByTime(movements, 15);
                var summaryReport = new SummaryReportTemplate {Movements = movements, Location = location, VideoTime = videoTime, ApproachNames = approachNames, BinnedMovements15 = binnedMovements15 , ExitNames = exitNames}.TransformText();
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

        public static void GenerateCSVReportHtml(string exportPath, string location, DateTime videoTime, List<Movement> movements)
        {
            try
            {
                var reportPath = Path.Combine(exportPath, "15-minute binned counts.csv");
                var pairs = GetAllUniqueApproachExitPairs(movements);
                
                var binnedMovements15 = BinnedMovements.BinMovementsByTime(movements, 15);
                var csvReport = new CSVReportTemplate { BinnedMovements15 = binnedMovements15, ApproachExitPairs = pairs }.TransformText();
                File.WriteAllText(reportPath, csvReport); //Save
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
