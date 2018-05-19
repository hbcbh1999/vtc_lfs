using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Emgu.CV;
using Emgu.CV.Structure;
using OptAssignTest.Framework;
using VTC.Common;
using VTC.Kernel.Video;
using VTC.Kernel.Vistas;

namespace OptAssignTest
{
    /// <summary>
    /// Base class for scripted tests.
    /// </summary>
    public class ScriptedTestBase : TestBase, ICaptureContextProvider
    {
        /// <summary>
        /// Frames to skip before validation.
        /// </summary>
        protected const int DetectionThreshold = 5;

        /// <summary>
        /// Execute script against the test action.
        /// </summary>
        protected static void RunScript(Script script, Action<Vista, uint> testAction)
        {
            var vista = CreateVista();

            // initialize background
            var background = new Image<Bgr, byte>((int) 640, (int) 480, new Bgr(Color.Black));
            vista.Update(background);

            // run the script
            for (uint frame = 0; ! script.IsDone(frame); frame++)
            {
                var image = background.Clone();

                script.Draw(frame, image);
                vista.Update(image);

                // run validation for the current frame
                testAction(vista, frame);
            }
        }

        /// <summary>
        /// Get available video sources.
        /// </summary>
        /// <returns></returns>
        public virtual IEnumerable<CaptureContext> GetCaptures()
        {
            return Enumerable.Empty<CaptureContext>();
        }
    }
}