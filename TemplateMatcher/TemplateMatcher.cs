using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Drawing.Drawing2D;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.Util;

namespace TemplateMatcher
{
    public class TemplateMatcher
    {
        private Image<Bgra, byte>[] _templates; // Array of template images 

        /// <summary>
        /// Calculate the best set of template assignments to reproduce the input image
        /// </summary>
        /// <param name="frame">Image to be reproduced</param>
        /// <returns></returns>
        public TemplateAssignment[] CalculateBestAssignment(Image<Bgra, byte> frame)
        {
            var assignmentCandidates = EnumerateAssignments(frame);
            //Search through assignment candidates to obtain best reproduction
            var bestAssignmentError = double.MaxValue;
            var bestAssignment = new TemplateAssignment[assignmentCandidates.Length];
            foreach (var t in assignmentCandidates)
            {
                var error = EvaluateAssignment(t, frame);
                if (!(error < bestAssignmentError)) continue;
                bestAssignment = t;
                bestAssignmentError = error;
            }

            return bestAssignment;
        }

        /// <summary>
        /// Calculate which single template placement best fits the input image
        /// </summary>
        /// <param name="image">Image to be reproduced</param>
        /// <returns></returns>
        public TemplateAssignment SingleBestAssignment(Image<Bgra, byte> image)
        {
            var templateAssignments = new TemplateAssignment[1];
            templateAssignments[0] = new TemplateAssignment
            {
                XShift = 0,
                YShift = 0,
                Template = 0
            };
            var bestTemplateAssignment = new TemplateAssignment
            {
                XShift = 0,
                YShift = 0,
                Template = 0
            };

            var bestAssignmentValue = EvaluateAssignment(templateAssignments, image);

            for(var k=0; k<_templates.Length;k++)
            for(var i=0; i<image.Width;i+=3)
                for (var j = 0; j < image.Height; j+=3)
                {
                    templateAssignments[0].Template = k;
                    templateAssignments[0].XShift = i;
                    templateAssignments[0].YShift = j;
                    var assignmentValue = EvaluateAssignment(templateAssignments, image);

                    if (!(assignmentValue < bestAssignmentValue)) continue;
                    bestAssignmentValue = assignmentValue;
                    bestTemplateAssignment.XShift = i;
                    bestTemplateAssignment.YShift = j;
                    bestTemplateAssignment.Template = k;
                }

            return bestTemplateAssignment;
        }

        /// <summary>
        /// Read in images to be used as templates in reconstruction
        /// </summary>
        /// <param name="templatePath"></param>
        public void PopulateTemplates(string templatePath)
        {
            var paths = Directory.GetFiles(templatePath);
            _templates = new Image<Bgra, byte>[paths.Length];
            for (var i = 0; i < paths.Length; i++)
                _templates[i] = new Image<Bgra, byte>(paths[i]);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="candidate"></param>
        /// <param name="input"></param>
        /// <returns></returns>
        public double EvaluateAssignment(TemplateAssignment[] candidate, Image<Bgra, byte> input)
        {
            var image = ReconstructImage(candidate, input.Width, input.Height);
            var differenceImage = input.AbsDiff(image);
            var reconstructionErrorSum = differenceImage.GetSum().Red + differenceImage.GetSum().Green + differenceImage.GetSum().Blue;
            return reconstructionErrorSum;
        }

        /// <summary>
        /// Enumerate list of templateassignments to be tested for image reproduction
        /// </summary>
        /// <param name="frame"></param>
        /// <returns></returns>
        private TemplateAssignment[][] EnumerateAssignments(Image<Bgra, byte> frame)
        {
            const int downsampleFactor = 10;
            var assignmentIndex = 0;

            var template0Assignments = new TemplateAssignment[frame.Width * frame.Height];
            var template1Assignments = new TemplateAssignment[frame.Width * frame.Height];
            for (var i = 0; i < frame.Width; i += downsampleFactor)
            {
                for (var j = 0; j < frame.Height; j += downsampleFactor)
                {
                    template0Assignments[assignmentIndex] = new TemplateAssignment
                    {
                        Template = 0,
                        XShift = i,
                        YShift = j
                    };

                    template1Assignments[assignmentIndex] = new TemplateAssignment
                    {
                        Template = 1,
                        XShift = i,
                        YShift = j
                    };

                    assignmentIndex++;
                }
            }

            Array.Resize(ref template0Assignments, assignmentIndex);
            Array.Resize(ref template1Assignments, assignmentIndex);
            var allPairs =
                from template0 in template0Assignments
                from template1 in template1Assignments
                select new[] {template0, template1};

            var allDoubleTemplate0 =
                from template0a in template0Assignments
                from template0b in template0Assignments
                select new[] { template0a, template0b };

            var allDoubleTemplate1 =
                from template1a in template1Assignments
                from template1b in template1Assignments
                select new[] { template1a, template1b };

            Array.Resize(ref template1Assignments, assignmentIndex);
            var allTemplate0 =
                from template0 in template0Assignments
                select new[] { template0 };

            var allTemplate1 =
                from template1 in template1Assignments
                select new[] { template1 };

            var assignments = allPairs.ToArray().Concat(allTemplate0.ToArray()).ToArray();
            assignments = assignments.Concat(allTemplate1.ToArray()).ToArray();
            assignments = assignments.Concat(allDoubleTemplate0.ToArray()).ToArray();
            assignments = assignments.Concat(allDoubleTemplate1.ToArray()).ToArray();
            return assignments;
        }

        /// <summary>
        /// Render a set of image templates at various positions on a blank rectangle. 
        /// </summary>
        /// <param name="assignment"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public Image<Bgra, byte> ReconstructImage(TemplateAssignment[] assignment, int width, int height)
        {
            var image = new Image<Bgra, byte>(width, height, new Bgra(0, 0, 0, 0));
            var paddedImage = PaddedImage(image);
            var maxDimension = _templates.Select(template => Math.Max(template.Width, template.Height)).Concat(new[] { 0 }).Max();

            var bmp = paddedImage.Bitmap;
            var gra = Graphics.FromImage(bmp);
            gra.CompositingMode = CompositingMode.SourceOver;
            for (var i = 0; i < assignment.Length; i++)
            {
                var template = _templates[assignment[i].Template];
                var x = assignment[i].XShift;
                var y = assignment[i].YShift;
                paddedImage.ROI = new Rectangle(x, y, template.Width, template.Height);
                gra.DrawImage(template.ToBitmap(), new Point(x, y));
            }

            paddedImage.ROI = new Rectangle(maxDimension, maxDimension, image.Width, image.Height);
            paddedImage.CopyTo(image);
            return image;
        }

        /// <summary>
        /// Generate image padded by dimensions equal to the greatest dimension of the template set. This is done so that 
        /// templates can be placed partially outside of the original input image. 
        /// </summary>
        /// <param name="imageIn">Image to be padded.</param>
        /// <returns></returns>
        private Image<Bgra, byte> PaddedImage(Image<Bgra, byte> imageIn)
        {
            var maxDimension = _templates.Select(template => Math.Max(template.Width, template.Height)).Concat(new[] { 0 }).Max();
            var paddedIm =
                new Image<Bgra, byte>(new Size(imageIn.Width + 2*maxDimension, imageIn.Height + 2*maxDimension))
                {
                    ROI = new Rectangle(maxDimension, maxDimension, imageIn.Width, imageIn.Height)
                };
            imageIn.CopyTo(paddedIm);
            return paddedIm;
        }
    }

    public struct TemplateAssignment
    {
        public int Template;
        public int XShift;
        public int YShift;
    }
}
