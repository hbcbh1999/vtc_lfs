using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using VTC.Reporting.Properties;
using VTC.Common;
using NLog;

namespace VTC.Reporting
{
    public class MovementCountRow
    {
        public MovementCount MovementCt = new MovementCount();
        public DateTime Time;

        public long MovementTypeCount(Turn tt)
        {
            long count = 0;

            foreach(var mc in MovementCt)
            {
                if(mc.Key.TurnType == tt)
                {
                    count += mc.Value;
                }
            }

            return count;
        }

        public long MovementTypeApproachCount(Turn tt, string approachName)
        {
            long count = 0;

            foreach(var mc in MovementCt)
            {
                if(mc.Key.TurnType == tt && mc.Key.Approach == approachName)
                {
                    count += mc.Value;
                }
            }

            return count;
        }
    }

    public class MovementCountRowList : List<MovementCountRow>
    {

    }

    public class SummaryReportGenerator
    {

        private static readonly Logger Logger = LogManager.GetLogger("main.form");

        const int MaxNumSparklineSamples = 80;

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

        private class FlowMetrics
        {
            public long PeakFlow;
            public DateTime PeakTime;
            public long TotalFlow;
            public long TotalLeft;
            public long TotalRight;
            public long TotalStraight;
            public long TotalUTurn;
        }

        public static void GenerateSummaryReportHTML(string exportPath, string location, DateTime videoTime)
        {
            try
            {
                //parse CSV files
                var filename5min = "5-minute binned counts [car].csv";
                var filepath5Min = Path.Combine(exportPath, filename5min);

                var filename15min = "15-minute binned counts [car].csv";
                var filepath15Min = Path.Combine(exportPath, filename15min);

                var filename60min = "60-minute binned counts [car].csv";
                var filepath60Min = Path.Combine(exportPath, filename60min);

                var mcrl5min = ParseCSVToListCounts(filepath5Min);
                var mcrl15min = ParseCSVToListCounts(filepath15Min);
                var mcrl60min = ParseCSVToListCounts(filepath60Min);

                var a1Metrics = CalculateFlowMetrics(mcrl5min,"Approach 1");
                var a2Metrics = CalculateFlowMetrics(mcrl5min,"Approach 2");
                var a3Metrics = CalculateFlowMetrics(mcrl5min,"Approach 3");
                var a4Metrics = CalculateFlowMetrics(mcrl5min,"Approach 4");

                //Populate tables
                //Write summary statistics
                //Generate HTML footer

                var reportPath = Path.Combine(exportPath, "report.html");
                string summaryReport = "";
                summaryReport += "<!DOCTYPE html>";
                summaryReport += "<html>";
                summaryReport += "<head>";
                summaryReport += Resources.headerString;
                summaryReport += "</head>";
                summaryReport += "<body>";

                summaryReport += Resources.containerDivOpenTag;

                summaryReport += Resources.docHeader.Replace("@date", videoTime.Date.ToString("yyyy'-'MM'-'dd")).Replace("@location", location); 
                summaryReport += Resources.legendDiv;

                summaryReport += Resources.rowTopBufferDivOpenTag; //summary statistics
                summaryReport += "<h3>Summary statistics</h3>";

                var summaryReportA1 = SummaryReportForApproach(a1Metrics, "Approach 1", mcrl5min);
                var summaryReportA2 = SummaryReportForApproach(a2Metrics, "Approach 2", mcrl5min);
                var summaryReportA3 = SummaryReportForApproach(a3Metrics, "Approach 3", mcrl5min);
                var summaryReportA4 = SummaryReportForApproach(a4Metrics, "Approach 4", mcrl5min);

                if (a1Metrics != null)
                    summaryReport += summaryReportA1;

                if (a2Metrics != null)
                    summaryReport += summaryReportA2;

                if (a3Metrics != null)
                    summaryReport += summaryReportA3;

                if (a4Metrics != null)
                    summaryReport += summaryReportA4;

                summaryReport += "</div>"; //summary statistics row close

                if (mcrl60min.Count > 0)
                {
                    summaryReport += AddRowOfApproachTables(mcrl60min, "60");
                }

                if (mcrl15min.Count > 0)
                {
                    summaryReport += AddRowOfApproachTables(mcrl15min, "15");
                }

                if (mcrl5min.Count > 0)
                {
                    summaryReport += AddRowOfApproachTables(mcrl5min, "5");
                }

                summaryReport += "</div>"; //Container close
                summaryReport += Resources.footer.Replace("@date", DateTime.Now.Date.ToString("yyyy'-'MM'-'dd"));

                summaryReport += "</body>";
                summaryReport += "</html>";

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

        private static string SummaryReportForApproach(FlowMetrics fmetrics, string name, List<MovementCountRow> approachCountRows)
        {
            if (fmetrics == null)
                return "";

            var summaryReportA1 =
                Resources.summaryStatistics.Replace("@approach", name)
                    .Replace("@peakflow", fmetrics.PeakFlow.ToString())
                    .Replace("@peaktime", fmetrics.PeakTime.ToString("hh:mm"));
            summaryReportA1 = summaryReportA1.Replace("@total", fmetrics.TotalFlow.ToString())
                .Replace("@left", fmetrics.TotalLeft.ToString())
                .Replace("@right", fmetrics.TotalRight.ToString())
                .Replace("@thru", fmetrics.TotalStraight.ToString())
                .Replace("@uturn", fmetrics.TotalUTurn.ToString());
            summaryReportA1 += "<br><br>";
            summaryReportA1 += GenerateSparkline(approachCountRows, name);
            summaryReportA1 += "</div>";
            return summaryReportA1;
        }

        private static FlowMetrics CalculateFlowMetrics(List<MovementCountRow> countRows, string approachName)
        {
            var metrics = new FlowMetrics();

            if (countRows.Count == 0)
                return null;
            
            var a1PeakSample =
                countRows.OrderByDescending(m => m.MovementTypeApproachCount(Turn.Left, approachName) + m.MovementTypeApproachCount(Turn.Right, approachName) + m.MovementTypeApproachCount(Turn.Straight, approachName) + m.MovementTypeApproachCount(Turn.UTurn, approachName)).FirstOrDefault();

            if (a1PeakSample != null)
            {
                metrics.PeakFlow = a1PeakSample.MovementTypeApproachCount(Turn.Left, approachName) + a1PeakSample.MovementTypeApproachCount(Turn.Right, approachName) + a1PeakSample.MovementTypeApproachCount(Turn.Straight, approachName) +
                                     a1PeakSample.MovementTypeApproachCount(Turn.UTurn, approachName);
                metrics.PeakTime = a1PeakSample.Time;
            }
            metrics.TotalFlow = countRows.Sum(mc => mc.MovementTypeApproachCount(Turn.Left, approachName) + mc.MovementTypeApproachCount(Turn.Right, approachName) + mc.MovementTypeApproachCount(Turn.Straight, approachName) + mc.MovementTypeApproachCount(Turn.UTurn, approachName));
            metrics.TotalLeft = countRows.Sum(mc => mc.MovementTypeApproachCount(Turn.Left, approachName));
            metrics.TotalRight = countRows.Sum(mc => mc.MovementTypeApproachCount(Turn.Right, approachName));
            metrics.TotalStraight = countRows.Sum(mc => mc.MovementTypeApproachCount(Turn.Straight, approachName));
            metrics.TotalUTurn = countRows.Sum(mc => mc.MovementTypeApproachCount(Turn.UTurn, approachName));
            return metrics;
        }

        private static MovementCountRowList ParseCSVToListCounts(string filepath)
        {
            var mcrl = new MovementCountRowList();
            var filenameSubstrings = filepath.Split(new char[]{'[',']'});
            var objectTypeString = CommonFunctions.FirstCharToUpper(filenameSubstrings[1]);
            var objectType = (ObjectType) Enum.Parse(typeof(ObjectType),objectTypeString);
            
            if (File.Exists(filepath))
            {
                //For each line in the CSV file
                foreach (var l in File.ReadLines(filepath))
                {
                    //Split the line into individual movement-counts
                    var mcr = new MovementCountRow();
                    var elements = l.Split(',');
                    var timestamp = DateTime.Parse(elements[0]);
                    mcr.Time = timestamp;

                    for (int i = 1; i < elements.Length; i += 3)
                    {
                        if (i >= elements.Length - 1) break;
                        if (i + 2 >= elements.Length - 1) break;

                        var movementName = elements[i];
                        var movementCount = Convert.ToInt32(elements[i + 2]);
                        var turnType = (Turn) Enum.Parse(typeof(Turn), elements[i + 1]);

                        var splitIndex = movementName.IndexOf("to");
                        var strings = movementName.Split(new string[]{" to "}, StringSplitOptions.RemoveEmptyEntries);
                        var approach = strings[0];
                        var exit = strings[1];

                        var movement = new Movement(approach, exit, turnType, objectType, null);
                        mcr.MovementCt.Add(movement,movementCount);
                    }

                    mcrl.Add(mcr);
                }
            }

            return mcrl;
        }

        private static string AddRowOfApproachTables(MovementCountRowList mcrl, string minutes)
        {
            string rowid = "row" + minutes;
            string rowOfTables = "";
            rowOfTables += "<h3>" + minutes + "-minute counts</h3>";
            rowOfTables += "<button data-toggle=\"collapse\" class=\"btn\" data-target=\"#" + rowid + "\">";
            rowOfTables += "<span class=\"glyphicon glyphicon-collapse-down\"></span>";
            rowOfTables += "</button>";
            rowOfTables += Resources.rowTopBufferDivOpenTagCollapse.Replace("@rowid", rowid); // counts
            rowOfTables += GenerateSingleTable(mcrl, "Approach 1");
            rowOfTables += GenerateSingleTable(mcrl, "Approach 2");
            rowOfTables += GenerateSingleTable(mcrl, "Approach 3");
            rowOfTables += GenerateSingleTable(mcrl, "Approach 4");

            rowOfTables += "</div>"; // row close
            return rowOfTables;
        }

        private static string GenerateSingleTable(MovementCountRowList mcrl, string approachName)
        {
            string table = "";
            table += Resources.colSm3DivOpenTag; // Approach 1
            table += "<h4>" + approachName + "</h4>";
            table += "<table>";
            table += Resources.tableHeaderRow;
            table += LayoutRows(mcrl, approachName);
            table += "</table>";
            table += "</div>"; //Approach 1 column close
            return table;
        }

        private static string GenerateSparkline(List<MovementCountRow> countRows, string approachName)
        {
            if (!countRows.Any())
            {
                return "";
            }

            var counts =
                countRows.Select(c => (c.MovementTypeApproachCount(Turn.Left, approachName) + c.MovementTypeApproachCount(Turn.Right, approachName) + c.MovementTypeApproachCount(Turn.Straight, approachName) + c.MovementTypeApproachCount(Turn.UTurn, approachName)).ToString());

            while (counts.Count() > MaxNumSparklineSamples)
            {
                counts = counts.Where((x, i) => i%2 == 0);
            }

            var countsString = counts.Aggregate((data, next) => data + "," + next);
            var rowOfTables = Resources.rtSparkline.Replace("@data", countsString);
            return rowOfTables;
        }

        private static string LayoutRows(MovementCountRowList movementCountRows, string approachName)
        {
            string rowsString = "";
            foreach (var mcr in movementCountRows)
            {
                var tableRow = (string) Resources.tableCountRow.Clone();
                tableRow = tableRow.Replace("@time", mcr.Time.ToShortTimeString());
                tableRow = tableRow.Replace("@left", mcr.MovementTypeApproachCount(Turn.Left,approachName).ToString());
                tableRow = tableRow.Replace("@right", mcr.MovementTypeApproachCount(Turn.Right,approachName).ToString());
                tableRow = tableRow.Replace("@thru", mcr.MovementTypeApproachCount(Turn.Straight,approachName).ToString());
                tableRow = tableRow.Replace("@uturn", mcr.MovementTypeApproachCount(Turn.UTurn,approachName).ToString());
                rowsString += tableRow;
            }
            return rowsString;
        }
    }
}
