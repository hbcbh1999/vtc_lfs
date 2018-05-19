using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using VTC.Reporting.Properties;
using NLog;

namespace VTC.Reporting
{
    public class SummaryReportGenerator
    {

        private static readonly Logger Logger = LogManager.GetLogger("main.form");

        const int MaxNumSparklineSamples = 80;

        private class MovementCountRow
        {
            public int LeftCount;
            public int RightCount;
            public int ThruCount;
            public int UTurnCount;
            public DateTime Time;
        }

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
            public int PeakFlow;
            public DateTime PeakTime;
            public int TotalFlow;
            public int TotalLeft;
            public int TotalRight;
            public int TotalStraight;
            public int TotalUTurn;
        }

        public static void GenerateSummaryReportHTML(string exportPath, string location, DateTime videoTime)
        {
            try
            {
                //parse CSV files
                var filename5min = "5-minute binned counts.csv";
                var filepath5Min = Path.Combine(exportPath, filename5min);

                var filename15min = "15-minute binned counts.csv";
                var filepath15Min = Path.Combine(exportPath, filename15min);

                var filename60min = "60-minute binned counts.csv";
                var filepath60Min = Path.Combine(exportPath, filename60min);


                List<MovementCountRow> a1C5M = new List<MovementCountRow>();
                List<MovementCountRow> a2C5M = new List<MovementCountRow>();
                List<MovementCountRow> a3C5M = new List<MovementCountRow>();
                List<MovementCountRow> a4C5M = new List<MovementCountRow>();

                List<MovementCountRow> a1C15M = new List<MovementCountRow>();
                List<MovementCountRow> a2C15M = new List<MovementCountRow>();
                List<MovementCountRow> a3C15M = new List<MovementCountRow>();
                List<MovementCountRow> a4C15M = new List<MovementCountRow>();

                List<MovementCountRow> a1C60M = new List<MovementCountRow>();
                List<MovementCountRow> a2C60M = new List<MovementCountRow>();
                List<MovementCountRow> a3C60M = new List<MovementCountRow>();
                List<MovementCountRow> a4C60M = new List<MovementCountRow>();

                ParseCSVToListCounts(filepath5Min, a1C5M, a2C5M, a3C5M, a4C5M);
                ParseCSVToListCounts(filepath15Min, a1C15M, a2C15M, a3C15M, a4C15M);
                ParseCSVToListCounts(filepath60Min, a1C60M, a2C60M, a3C60M, a4C60M);

                var a1Metrics = CalculateFlowMetrics(a1C5M);
                var a2Metrics = CalculateFlowMetrics(a2C5M);
                var a3Metrics = CalculateFlowMetrics(a3C5M);
                var a4Metrics = CalculateFlowMetrics(a4C5M);

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

                var summaryReportA1 = SummaryReportForApproach(a1Metrics, "Approach 1", a1C5M);
                var summaryReportA2 = SummaryReportForApproach(a2Metrics, "Approach 2", a2C5M);
                var summaryReportA3 = SummaryReportForApproach(a3Metrics, "Approach 3", a3C5M);
                var summaryReportA4 = SummaryReportForApproach(a4Metrics, "Approach 4", a4C5M);

                if (a1Metrics != null)
                    summaryReport += summaryReportA1;

                if (a2Metrics != null)
                    summaryReport += summaryReportA2;

                if (a3Metrics != null)
                    summaryReport += summaryReportA3;

                if (a4Metrics != null)
                    summaryReport += summaryReportA4;

                summaryReport += "</div>"; //summary statistics row close

                if (a1C60M.Count + a2C60M.Count + a3C60M.Count + a4C60M.Count > 0)
                {
                    summaryReport += AddRowOfApproachTables("60", a1C60M, a2C60M, a3C60M, a4C60M);
                }

                if (a1C15M.Count + a2C15M.Count + a3C15M.Count + a4C15M.Count > 0)
                {
                    summaryReport += AddRowOfApproachTables("15", a1C15M, a2C15M, a3C15M, a4C15M);
                }

                if (a1C5M.Count + a2C5M.Count + a3C5M.Count + a4C5M.Count > 0)
                {
                    summaryReport += AddRowOfApproachTables("5", a1C5M, a2C5M, a3C5M, a4C5M);
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
            summaryReportA1 += GenerateSparkline(approachCountRows);
            summaryReportA1 += "</div>";
            return summaryReportA1;
        }

        private static FlowMetrics CalculateFlowMetrics(List<MovementCountRow> countRows)
        {
            var metrics = new FlowMetrics();

            if (countRows.Count == 0)
                return null;
            
            var a1PeakSample =
                countRows.OrderByDescending(m => m.LeftCount + m.RightCount + m.ThruCount + m.UTurnCount).FirstOrDefault();

            if (a1PeakSample != null)
            {
                metrics.PeakFlow = a1PeakSample.LeftCount + a1PeakSample.RightCount + a1PeakSample.ThruCount +
                                     a1PeakSample.UTurnCount;
                metrics.PeakTime = a1PeakSample.Time;
            }
            metrics.TotalFlow = countRows.Sum(mc => mc.LeftCount + mc.RightCount + mc.ThruCount + mc.UTurnCount);
            metrics.TotalLeft = countRows.Sum(mc => mc.LeftCount);
            metrics.TotalRight = countRows.Sum(mc => mc.RightCount);
            metrics.TotalStraight = countRows.Sum(mc => mc.ThruCount);
            metrics.TotalUTurn = countRows.Sum(mc => mc.UTurnCount);
            return metrics;
        }

        private static void ParseCSVToListCounts(string filepath5Min, List<MovementCountRow> a1C, List<MovementCountRow> a2C, List<MovementCountRow> a3C, List<MovementCountRow> a4C)
        {
            if (File.Exists(filepath5Min))
            {
                foreach (var l in File.ReadLines(filepath5Min))
                {
                    var mcr1 = new MovementCountRow(); // One for each approach 
                    var mcr2 = new MovementCountRow();
                    var mcr3 = new MovementCountRow();
                    var mcr4 = new MovementCountRow();

                    var elements = l.Split(',');
                    mcr1.Time = DateTime.Parse(elements[0]);
                    mcr2.Time = DateTime.Parse(elements[0]);
                    mcr3.Time = DateTime.Parse(elements[0]);
                    mcr4.Time = DateTime.Parse(elements[0]);

                    for (int i = 1; i < elements.Length; i += 4)
                    {
                        if (i >= elements.Length - 1) break;

                        var movementName = elements[i];
                        var movementCount = Convert.ToInt32(elements[i + 3]);
                        var movementType = elements[i + 1].ToLower();

                        if (movementName.Contains("Approach 1"))
                        {
                            if (movementType.Contains("left")) mcr1.LeftCount += movementCount;
                            if (movementType.Contains("right")) mcr1.RightCount += movementCount;
                            if (movementType.Contains("straight")) mcr1.ThruCount += movementCount;
                            if (movementType.Contains("uturn")) mcr1.UTurnCount += movementCount;
                        }

                        if (movementName.Contains("Approach 2"))
                        {
                            if (movementType.Contains("left")) mcr2.LeftCount += movementCount;
                            if (movementType.Contains("right")) mcr2.RightCount += movementCount;
                            if (movementType.Contains("straight")) mcr2.ThruCount += movementCount;
                            if (movementType.Contains("uturn")) mcr2.UTurnCount += movementCount;
                        }

                        if (movementName.Contains("Approach 3"))
                        {
                            if (movementType.Contains("left")) mcr3.LeftCount += movementCount;
                            if (movementType.Contains("right")) mcr3.RightCount += movementCount;
                            if (movementType.Contains("straight")) mcr3.ThruCount += movementCount;
                            if (movementType.Contains("uturn")) mcr3.UTurnCount += movementCount;
                        }

                        if (movementName.Contains("Approach 4"))
                        {
                            if (movementType.Contains("left")) mcr4.LeftCount += movementCount;
                            if (movementType.Contains("right")) mcr4.RightCount += movementCount;
                            if (movementType.Contains("straight")) mcr4.ThruCount += movementCount;
                            if (movementType.Contains("uturn")) mcr4.UTurnCount += movementCount;
                        }
                    }

                    a1C.Add(mcr1);
                    a2C.Add(mcr2);
                    a3C.Add(mcr3);
                    a4C.Add(mcr4);
                }
            }
        }

        private static string AddRowOfApproachTables(string minutes, List<MovementCountRow> approach1CountRows, List<MovementCountRow> approach2CountRows, List<MovementCountRow> approach3CountRows,
            List<MovementCountRow> approach4CountRows)
        {
            string rowid = "row" + minutes;
            string rowOfTables = "";
            rowOfTables += "<h3>" + minutes + "-minute counts</h3>";
            rowOfTables += "<button data-toggle=\"collapse\" class=\"btn\" data-target=\"#" + rowid + "\">";
            rowOfTables += "<span class=\"glyphicon glyphicon-collapse-down\"></span>";
            rowOfTables += "</button>";
            rowOfTables += Resources.rowTopBufferDivOpenTagCollapse.Replace("@rowid", rowid); // counts
            rowOfTables += GenerateSingleTable(approach1CountRows, "Approach 1");
            rowOfTables += GenerateSingleTable(approach2CountRows, "Approach 2");
            rowOfTables += GenerateSingleTable(approach3CountRows, "Approach 3");
            rowOfTables += GenerateSingleTable(approach4CountRows, "Approach 4");

            rowOfTables += "</div>"; // row close
            return rowOfTables;
        }

        private static string GenerateSingleTable(List<MovementCountRow> approachCountRows, string name)
        {
            string table = "";
            table += Resources.colSm3DivOpenTag; // Approach 1
            table += "<h4>" + name + "</h4>";
            table += "<table>";
            table += Resources.tableHeaderRow;
            table += LayoutRows(approachCountRows);
            table += "</table>";
            table += "</div>"; //Approach 1 column close
            return table;
        }

        private static string GenerateSparkline(List<MovementCountRow> countRows)
        {
            if (!countRows.Any())
            {
                return "";
            }

            var counts =
                countRows.Select(c => (c.LeftCount + c.RightCount + c.ThruCount + c.UTurnCount).ToString());

            while (counts.Count() > MaxNumSparklineSamples)
            {
                counts = counts.Where((x, i) => i%2 == 0);
            }

            var countsString = counts.Aggregate((data, next) => data + "," + next);
            var rowOfTables = Resources.rtSparkline.Replace("@data", countsString);
            return rowOfTables;
        }

        private static string LayoutRows(List<MovementCountRow> movementCountRows)
        {
            string rowsString = "";
            foreach (var mcr in movementCountRows)
            {
                var tableRow = (string) Resources.tableCountRow.Clone();
                tableRow = tableRow.Replace("@time", mcr.Time.ToShortTimeString());
                tableRow = tableRow.Replace("@left", mcr.LeftCount.ToString());
                tableRow = tableRow.Replace("@right", mcr.RightCount.ToString());
                tableRow = tableRow.Replace("@thru", mcr.ThruCount.ToString());
                tableRow = tableRow.Replace("@uturn", mcr.UTurnCount.ToString());
                rowsString += tableRow;
            }
            return rowsString;
        }
    }
}
