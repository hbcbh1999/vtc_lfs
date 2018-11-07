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
            public long TotalCrossing;
        }

        public static MovementCountRowList Add(MovementCountRowList mcrl1, MovementCountRowList mcrl2)
        {
            if(mcrl1.Count != mcrl2.Count)
            {
                throw new ArgumentException("Movement-count row lists must contain the same number of elements to be added.");
            }

            var mcrl_sum = new MovementCountRowList();
            for(int i=0;i<mcrl1.Count;i++)
            {
                var mcr_new = new MovementCountRow();
                mcr_new.Time = mcrl1[i].Time;
                foreach(var k in mcrl1[i].MovementCt.Keys)
                {
                    foreach(var v in mcrl2[i].MovementCt.Keys)
                    {
                        if(k.Approach == v.Approach && k.Exit == v.Exit)
                        {
                            var movement_new = new Movement(k.Approach, k.Exit, k.TurnType, ObjectType.Unknown, new List<StateEstimate>(), 0);
                            mcr_new.MovementCt[movement_new] = mcrl1[i].MovementCt[k] + mcrl2[i].MovementCt[v];
                            break;
                        }
                    }
                }
                mcrl_sum.Add(mcr_new);
            }
            return mcrl_sum;
        }

        public static void GenerateAllVehiclesSummaryReportHTML(string exportPath, string location, DateTime videoTime)
        { 
            try
            {
                //parse CSV files
                var filename5minCar = "5-minute binned counts [car].csv";
                var filename5minTruck = "5-minute binned counts [truck].csv";
                var filename5minBus = "5-minute binned counts [bus].csv";
                var filename5minMotorcycle = "5-minute binned counts [motorcycle].csv";
                var filepath5MinCar = Path.Combine(exportPath, filename5minCar);
                var filepath5MinTruck = Path.Combine(exportPath, filename5minTruck);
                var filepath5MinBus = Path.Combine(exportPath, filename5minBus);
                var filepath5MinMotorcycle = Path.Combine(exportPath, filename5minMotorcycle);

                var filename15minCar = "15-minute binned counts [car].csv";
                var filename15minTruck = "15-minute binned counts [truck].csv";
                var filename15minBus = "15-minute binned counts [bus].csv";
                var filename15minMotorcycle = "15-minute binned counts [motorcycle].csv";
                var filepath15MinCar = Path.Combine(exportPath, filename15minCar);
                var filepath15MinTruck = Path.Combine(exportPath, filename15minTruck);
                var filepath15MinBus = Path.Combine(exportPath, filename15minBus);
                var filepath15MinMotorcycle = Path.Combine(exportPath, filename15minMotorcycle);

                var filename60minCar = "60-minute binned counts [car].csv";
                var filename60minTruck = "60-minute binned counts [truck].csv";
                var filename60minBus = "60-minute binned counts [bus].csv";
                var filename60minMotorcycle = "60-minute binned counts [motorcycle].csv";
                var filepath60MinCar = Path.Combine(exportPath, filename60minCar);
                var filepath60MinTruck = Path.Combine(exportPath, filename60minTruck);
                var filepath60MinBus = Path.Combine(exportPath, filename60minBus);
                var filepath60MinMotorcycle = Path.Combine(exportPath, filename60minMotorcycle);                

                var mcrl5minCar = ParseCSVToListCounts(filepath5MinCar);
                var mcrl15minCar = ParseCSVToListCounts(filepath15MinCar);
                var mcrl60minCar = ParseCSVToListCounts(filepath60MinCar);

                var mcrl5minTruck = ParseCSVToListCounts(filepath5MinTruck);
                var mcrl15minTruck = ParseCSVToListCounts(filepath15MinTruck);
                var mcrl60minTruck = ParseCSVToListCounts(filepath60MinTruck);

                var mcrl5minBus = ParseCSVToListCounts(filepath5MinBus);
                var mcrl15minBus = ParseCSVToListCounts(filepath15MinBus);
                var mcrl60minBus = ParseCSVToListCounts(filepath60MinBus);

                var mcrl5minMotorcycle = ParseCSVToListCounts(filepath5MinMotorcycle);
                var mcrl15minMotorcycle = ParseCSVToListCounts(filepath15MinMotorcycle);
                var mcrl60minMotorcycle = ParseCSVToListCounts(filepath60MinMotorcycle);

                var mcrl5minTotal = Add(Add(Add(mcrl5minCar, mcrl5minBus), mcrl5minTruck), mcrl5minMotorcycle);
                var mcrl15minTotal = Add(Add(Add(mcrl15minCar, mcrl15minBus), mcrl15minTruck), mcrl15minMotorcycle);
                var mcrl60minTotal = Add(Add(Add(mcrl60minCar, mcrl60minBus), mcrl60minTruck), mcrl60minMotorcycle);

                //Populate tables
                //Write summary statistics
                //Generate HTML footer

                var reportPath = Path.Combine(exportPath, "report [All vehicles].html");
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

                var a1Metrics = CalculateFlowMetrics(mcrl5minTotal,"Approach 1");
                var a2Metrics = CalculateFlowMetrics(mcrl5minTotal,"Approach 2");
                var a3Metrics = CalculateFlowMetrics(mcrl5minTotal,"Approach 3");
                var a4Metrics = CalculateFlowMetrics(mcrl5minTotal,"Approach 4");
                var summaryReportA1 = SummaryReportForApproach(a1Metrics, "Approach 1", mcrl5minTotal);
                var summaryReportA2 = SummaryReportForApproach(a2Metrics, "Approach 2", mcrl5minTotal);
                var summaryReportA3 = SummaryReportForApproach(a3Metrics, "Approach 3", mcrl5minTotal);
                var summaryReportA4 = SummaryReportForApproach(a4Metrics, "Approach 4", mcrl5minTotal);

                if (a1Metrics != null)
                    summaryReport += summaryReportA1;

                if (a2Metrics != null)
                    summaryReport += summaryReportA2;

                if (a3Metrics != null)
                    summaryReport += summaryReportA3;

                if (a4Metrics != null)
                    summaryReport += summaryReportA4;

                summaryReport += "</div>"; //summary statistics row close

                if (mcrl60minTotal.Count > 0)
                {
                    summaryReport += AddRowOfApproachTables(mcrl60minTotal, "60");
                }

                if (mcrl15minTotal.Count > 0)
                {
                    summaryReport += AddRowOfApproachTables(mcrl15minTotal, "15");
                }

                if (mcrl5minTotal.Count > 0)
                {
                    summaryReport += AddRowOfApproachTables(mcrl5minTotal, "5");
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

        public static void GenerateSummaryReportHTML(string exportPath, string location, DateTime videoTime, string objectType)
        {
            try
            {
                //parse CSV files
                var filename5min = "5-minute binned counts ["+objectType+"].csv";
                var filepath5Min = Path.Combine(exportPath, filename5min);

                var filename15min = "15-minute binned counts ["+objectType+"].csv";
                var filepath15Min = Path.Combine(exportPath, filename15min);

                var filename60min = "60-minute binned counts ["+objectType+"].csv";
                var filepath60Min = Path.Combine(exportPath, filename60min);

                var mcrl5min = ParseCSVToListCounts(filepath5Min);
                var mcrl15min = ParseCSVToListCounts(filepath15Min);
                var mcrl60min = ParseCSVToListCounts(filepath60Min);

                //Populate tables
                //Write summary statistics
                //Generate HTML footer

                var reportPath = Path.Combine(exportPath, "report ["+objectType+"].html");
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

                if(objectType == "person")
                {
                    var s1Metrics = CalculateFlowMetrics(mcrl5min,"Sidewalk 1");
                    var s2Metrics = CalculateFlowMetrics(mcrl5min,"Sidewalk 2");
                    var s3Metrics = CalculateFlowMetrics(mcrl5min,"Sidewalk 3");
                    var s4Metrics = CalculateFlowMetrics(mcrl5min,"Sidewalk 4");
                    var summaryReportS1 = SummaryReportForSidewalk(s1Metrics, "Sidewalk 1", mcrl5min);
                    var summaryReportS2 = SummaryReportForSidewalk(s2Metrics, "Sidewalk 2", mcrl5min);
                    var summaryReportS3 = SummaryReportForSidewalk(s3Metrics, "Sidewalk 3", mcrl5min);
                    var summaryReportS4 = SummaryReportForSidewalk(s4Metrics, "Sidewalk 4", mcrl5min);

                    if (s1Metrics != null)
                        summaryReport += summaryReportS1;

                    if (s2Metrics != null)
                        summaryReport += summaryReportS2;

                    if (s3Metrics != null)
                        summaryReport += summaryReportS3;

                    if (s4Metrics != null)
                        summaryReport += summaryReportS4;

                    summaryReport += "</div>"; //summary statistics row close

                    if (mcrl60min.Count > 0)
                    {
                        summaryReport += AddRowOfSidewalkTables(mcrl60min, "60");
                    }

                    if (mcrl15min.Count > 0)
                    {
                        summaryReport += AddRowOfSidewalkTables(mcrl15min, "15");
                    }

                    if (mcrl5min.Count > 0)
                    {
                        summaryReport += AddRowOfSidewalkTables(mcrl5min, "5");
                    }
                }
                else
                {
                    var a1Metrics = CalculateFlowMetrics(mcrl5min,"Approach 1");
                    var a2Metrics = CalculateFlowMetrics(mcrl5min,"Approach 2");
                    var a3Metrics = CalculateFlowMetrics(mcrl5min,"Approach 3");
                    var a4Metrics = CalculateFlowMetrics(mcrl5min,"Approach 4");
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

            var summaryReport =
                Resources.summaryStatistics.Replace("@approach", name)
                    .Replace("@peakflow", fmetrics.PeakFlow.ToString())
                    .Replace("@peaktime", fmetrics.PeakTime.ToString("hh:mm"));
            summaryReport = summaryReport.Replace("@total", fmetrics.TotalFlow.ToString())
                .Replace("@left", fmetrics.TotalLeft.ToString())
                .Replace("@right", fmetrics.TotalRight.ToString())
                .Replace("@thru", fmetrics.TotalStraight.ToString())
                .Replace("@uturn", fmetrics.TotalUTurn.ToString());
            summaryReport += "<br><br>";
            summaryReport += GenerateSparkline(approachCountRows, name);
            summaryReport += "</div>";
            return summaryReport;
        }

        private static string SummaryReportForSidewalk(FlowMetrics fmetrics, string name, List<MovementCountRow> approachCountRows)
        {
            if (fmetrics == null)
                return "";

            var summaryReport =
                Resources.summaryStatisticsSidewalk.Replace("@approach", name)
                    .Replace("@peakflow", fmetrics.PeakFlow.ToString())
                    .Replace("@peaktime", fmetrics.PeakTime.ToString("hh:mm"));
            summaryReport = summaryReport.Replace("@total", fmetrics.TotalFlow.ToString())
                .Replace("@crossing", fmetrics.TotalCrossing.ToString());
            summaryReport += "<br><br>";
            summaryReport += GenerateSparkline(approachCountRows, name);
            summaryReport += "</div>";
            return summaryReport;
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
            metrics.TotalCrossing =  countRows.Sum(mc => mc.MovementTypeApproachCount(Turn.Crossing, approachName));
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

                        var movement = new Movement(approach, exit, turnType, objectType, null,0);
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

        private static string AddRowOfSidewalkTables(MovementCountRowList mcrl, string minutes)
        {
            string rowid = "row" + minutes;
            string rowOfTables = "";
            rowOfTables += "<h3>" + minutes + "-minute counts</h3>";
            rowOfTables += "<button data-toggle=\"collapse\" class=\"btn\" data-target=\"#" + rowid + "\">";
            rowOfTables += "<span class=\"glyphicon glyphicon-collapse-down\"></span>";
            rowOfTables += "</button>";
            rowOfTables += Resources.rowTopBufferDivOpenTagCollapse.Replace("@rowid", rowid); // counts
            rowOfTables += GenerateSingleTableSidewalk(mcrl, "Sidewalk 1");
            rowOfTables += GenerateSingleTableSidewalk(mcrl, "Sidewalk 2");
            rowOfTables += GenerateSingleTableSidewalk(mcrl, "Sidewalk 3");
            rowOfTables += GenerateSingleTableSidewalk(mcrl, "Sidewalk 4");

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

        private static string GenerateSingleTableSidewalk(MovementCountRowList mcrl, string approachName)
        {
            string table = "";
            table += Resources.colSm3DivOpenTag; // Approach 1
            table += "<h4>" + approachName + "</h4>";
            table += "<table>";
            table += Resources.tableHeaderRowSidewalk;
            table += LayoutRowsSidewalk(mcrl, approachName);
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

        private static string LayoutRowsSidewalk(MovementCountRowList movementCountRows, string approachName)
        {
            string rowsString = "";
            foreach (var mcr in movementCountRows)
            {
                var tableRow = (string) Resources.tableCountRowSidewalk.Clone();
                tableRow = tableRow.Replace("@time", mcr.Time.ToShortTimeString());
                tableRow = tableRow.Replace("@crossing", mcr.MovementTypeApproachCount(Turn.Crossing,approachName).ToString());
                rowsString += tableRow;
            }
            return rowsString;
        }
    }
}
