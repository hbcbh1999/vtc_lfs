using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Newtonsoft.Json;
using Brushes = System.Windows.Media.Brushes;
using Image = System.Windows.Controls.Image;
using Point = System.Windows.Point;
using VTC.Common;
using VTC.Kernel;
using VTC.Classifier;

namespace TrajectoryAnalyzer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public ObservableCollection<string> trajectoriesList { get; private set; }
        public ObservableCollection<string> prototypeTrajectoriesList { get; private set; }
        public ObservableCollection<string> selectedTrajectoriesList { get; private set; }

        private List<string> approaches;
        private List<string> exits;

        private string selectedApproach = "Any";
        private string selectedExit = "Any";

        private Bitmap backgroundImage;

        private YoloIntegerNameMapping _yoloNameMapping = new YoloIntegerNameMapping();

        //private List<int> _lineNumbers = new List<int>();

        public MainWindow()
        {
            InitializeComponent();
            trajectoriesList = new ObservableCollection<string>();
            selectedTrajectoriesList = new ObservableCollection<string>();
            prototypeTrajectoriesList = new ObservableCollection<string>();
            trajectoryListView.ItemsSource = selectedTrajectoriesList;
            approaches = new List<string>();
            exits = new List<string>();
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop, true))
            {
                string[] droppedFilePaths = e.Data.GetData(DataFormats.FileDrop, true) as string[];
                foreach (var path in droppedFilePaths)
                {   
                    FileInfo fi = new FileInfo(path);
                    TrajectoryFilenameBox.Text = fi.Name;

                    ProcessFileType(path,fi);

                    if(Directory.Exists(path))
                    { 
                        var children = Directory.GetFiles(path);
                        foreach(var file in children)
                        {
                            ProcessFileType(file, new FileInfo(file));
                        }
                    }

                }
            }
        }

        private void ProcessFileType(string filepath, FileInfo fi)
        { 
            if (fi.Extension == ".json")
            {
                var lines = File.ReadAllLines(filepath);

                foreach (var line in lines)
                {
                    if (fi.Name.Contains("Movements"))
                    {
                        trajectoriesList.Add(line);
                        var m = ParseAsMovement(line);
                                
                        if (!approaches.Contains(m.Approach))
                        {
                            approaches.Add(m.Approach);
                        }

                        if (!exits.Contains(m.Exit))
                        {
                            exits.Add(m.Exit);
                        }
                    }
                    else if (fi.Name.Contains("Synthetic"))
                    {
                        prototypeTrajectoriesList.Add(line);
                    }
                }

                PopulateApproachAndExitsBoxes();
            }

            if (fi.Extension == ".png")
            {
                backgroundImage = (Bitmap) System.Drawing.Image.FromFile(filepath);
            }
        }

        private string ExtractTurnSubstring(string movementString)
        {
            var t = ParseAsMovement(movementString);
            return t.ToString();
        }

        private Movement ParseAsMovement(string movementJson)
        {
            return JsonConvert.DeserializeObject<Movement>(movementJson);
        }

        private void RenderBackground(DrawingContext drawingContext)
        {
            if (backgroundImage != null)
            {
                BitmapSource backgroundBmpSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                    backgroundImage.GetHbitmap(),
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());

                drawingContext.DrawImage(backgroundBmpSource,
                    new Rect(0, 0, backgroundImage.Width, backgroundImage.Height));
            }
            else
            {
                drawingContext.DrawRectangle(Brushes.DarkGray, new System.Windows.Media.Pen(Brushes.DarkGray, 1), new Rect(new System.Windows.Size(640,480)) );
            }
        }

        private void RenderTrajectory(string trajectoryString, int width, int height, double dpi, byte[] pixelData, DrawingContext drawingContext, System.Windows.Media.Brush lineColor, double lineThickness)
        {
            Regex rgx = new Regex(@"[^\d\.]");

            var m = ParseAsMovement(trajectoryString);

            if(extrapolateCheckbox.IsChecked.Value)
            {
                var extrap = TrajectoryExtrapolator.ExtrapolatedTrajectory(m.StateEstimates, width, height);
                for (int i = 0; i < extrap.Count; i++)
                {
                    if (i + 1 < extrap.Count)
                    {
                        var thisPoint = extrap[i];
                        var nextPoint = extrap[i + 1];

                        if(thisPoint.X > width | thisPoint.X < 0)
                        { 
                            continue;
                        }

                        if(thisPoint.Y > height | thisPoint.Y < 0)
                        { 
                            continue;
                        }

                        if(nextPoint.X > width | nextPoint.X < 0)
                        { 
                            continue;
                        }

                        if(nextPoint.Y > height | nextPoint.Y < 0)
                        { 
                            continue;
                        }

                        drawingContext.DrawLine(new System.Windows.Media.Pen(Brushes.Yellow, lineThickness),
                            new Point(thisPoint.X, thisPoint.Y), new Point(nextPoint.X, nextPoint.Y));
                    }
                }           
            }

            for (int i = 0; i < m.StateEstimates.Count; i++)
            {
                if (i + 1 < m.StateEstimates.Count)
                {
                    var thisPoint = m.StateEstimates[i];
                    var nextPoint = m.StateEstimates[i + 1];

                    if(thisPoint.X > width | thisPoint.X < 0)
                    { 
                        continue;
                    }

                    if(thisPoint.Y > height | thisPoint.Y < 0)
                    { 
                        continue;
                    }

                    if(nextPoint.X > width | nextPoint.X < 0)
                    { 
                        continue;
                    }

                    if(nextPoint.Y > height | nextPoint.Y < 0)
                    { 
                        continue;
                    }

                    drawingContext.DrawLine(new System.Windows.Media.Pen(lineColor, lineThickness),
                        new Point(thisPoint.X, thisPoint.Y), new Point(nextPoint.X, nextPoint.Y));
                }
            }

            var firstMeasurement = m.StateEstimates.First();
            var lastMeasurement = m.StateEstimates.Last();

            if(showEndpointsCheckbox.IsChecked.Value)
            { 
                drawingContext.DrawText(new FormattedText("Start", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface("Arial"), 18, Brushes.Red), new Point(firstMeasurement.X, firstMeasurement.Y));
                drawingContext.DrawText(new FormattedText("End", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface("Arial"), 18, Brushes.Red), new Point(lastMeasurement.X, lastMeasurement.Y));
            }
           
        }

        private List<Movement> SyntheticPrototypes()
        {
            return prototypeTrajectoriesList.Select(str => JsonLogger<Movement>.FromJsonString(str)).ToList();
        }

        private void trajectoryListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            double dpi = 96;
            int width = 640;
            int height = 480;
            byte[] pixelData = new byte[width * height];
            Regex rgx = new Regex(@"[^\d\.]");

            trajectoryMatchListView.Items.Clear();

            foreach (var item in e.AddedItems)
            {
                var trajectoryString = item.ToString();
                var visual = new DrawingVisual();
                using (DrawingContext drawingContext = visual.RenderOpen())
                {

                    RenderBackground(drawingContext);
                    RenderTrajectory(trajectoryString, width, height, dpi, pixelData, drawingContext, System.Windows.Media.Brushes.Red, 0.5);

                    foreach (var proto in prototypeTrajectoriesList)
                    {
                        RenderTrajectory(proto, width, height, dpi, pixelData, drawingContext, System.Windows.Media.Brushes.Blue, 4);
                    }
                }
                var image = new DrawingImage(visual.Drawing);
                trajectoryRenderingImage.Source = image;

                var turnSubstring = ExtractTurnSubstring(trajectoryString);

                movementTextBox.Text = turnSubstring;

                var turnSubstringElements = turnSubstring.Split(',');
                movementNameBox.Content = turnSubstringElements[1];

                //Compare against all synthetic trajectories
                var synthetics = SyntheticPrototypes();
                var movement = JsonConvert.DeserializeObject<Movement>(trajectoryString);
                
                var mostLikelyClassType =
                        YoloIntegerNameMapping.GetObjectNameFromClassInteger(movement.StateEstimates.Last().MostFrequentClassId(),
                            _yoloNameMapping.IntegerToObjectName);
                var uppercaseClassType = CommonFunctions.FirstCharToUpper(mostLikelyClassType);

                var tracked_object = new VTC.Common.TrackedObject();
                tracked_object.ObjectType = mostLikelyClassType;
                tracked_object.StateHistory = movement.StateEstimates;
                //Populate trajectory-comparison list box with comparison statistics for each synthetic trajectory
                foreach(var synth in synthetics)
                {
                    var matchCost = TrajectorySimilarity.NearestPointsCost(tracked_object.StateHistory, synth.StateEstimates);
                    var description = synth.ToString() + " match-cost: " + matchCost + "," + TrajectorySimilarity.CostExplanation(tracked_object.StateHistory, synth.StateEstimates);
                    trajectoryMatchListView.Items.Add(description);
                }
                //var matched_movement = TrajectorySimilarity.MatchNearestTrajectory(tracked_object, mostLikelyClassType, 0, synthetics);

                
                netMovementBox.Content = tracked_object.NetMovement();
                numSamplesBox.Content = movement.StateEstimates.Count();
                missedDetectionsBox.Content = movement.StateEstimates.Sum(se => se.MissedDetections);
                var lastStateEstimate = movement.StateEstimates.Last();
                finalPositionCovarianceBox.Content = Math.Sqrt(Math.Pow(lastStateEstimate.CovX,2) + Math.Pow(lastStateEstimate.CovX,2));
                pathLengthBox.Content = tracked_object.PathLengthIntegral();
                missRatioBox.Content = Math.Round((double) movement.StateEstimates.Sum(se => se.MissedDetections) / movement.StateEstimates.Count(), 2);
            }
        }

        private void exitComboxBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedExit = exitComboxBox.SelectedItem.ToString();
            UpdateSelectedTrajectories();
            DrawSelectedTrajectories();
        }

        private void approachComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedApproach = approachComboBox.SelectedItem.ToString();
            UpdateSelectedTrajectories();
            DrawSelectedTrajectories();
        }

        private void PopulateApproachAndExitsBoxes()
        {
            approachComboBox.Items.Clear();
            exitComboxBox.Items.Clear();

            approaches.Sort();
            exits.Sort();

            foreach(var a in approaches)
            {approachComboBox.Items.Add(a);}
            approachComboBox.Items.Add("Any");

            foreach(var e in exits)
            {exitComboxBox.Items.Add(e);}
            exitComboxBox.Items.Add("Any");
        }

        private void UpdateSelectedTrajectories()
        {
            selectedTrajectoriesList.Clear();
            foreach (var t in trajectoriesList)
            {
                var m = ParseAsMovement(t);

                if (m.Approach != selectedApproach && selectedApproach != "Any")
                {
                    continue;
                }
                
                if (m.Exit != selectedExit && selectedExit != "Any")
                {
                    continue;
                }

                selectedTrajectoriesList.Add(t);
            }
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
           DrawSelectedTrajectories();
        }

        private void DrawSelectedTrajectories()
        {
            double dpi = 96;
            int width = 640;
            int height = 480;
            byte[] pixelData = new byte[width * height];
            var visual = new DrawingVisual();
            using (DrawingContext drawingContext = visual.RenderOpen())
            {
                RenderBackground(drawingContext);

                foreach (var proto in prototypeTrajectoriesList)
                {
                    RenderTrajectory(proto, width, height, dpi, pixelData, drawingContext, System.Windows.Media.Brushes.Blue, 4);
                }

                foreach (var t in selectedTrajectoriesList)
                {
                    RenderTrajectory(t, width, height, dpi, pixelData, drawingContext, System.Windows.Media.Brushes.Red, 0.5);
                }
            }
            var image = new DrawingImage(visual.Drawing);
            trajectoryRenderingImage.Source = image;
        }
    }    

    public class Measurement
    {
        public double X;
        public double Y;
        public double Vx;
        public double Vy;
        public int R;
        public int G;
        public int B;
        public int Size;
        public string Type;
    }
}
