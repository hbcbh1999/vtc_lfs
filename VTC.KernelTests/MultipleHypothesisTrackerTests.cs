using Microsoft.VisualStudio.TestTools.UnitTesting;
using VTC.Kernel;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using VTC.Common;
using VTC.Common.RegionConfig;

namespace VTC.KernelTests
{
    [TestClass()]
    public class MultipleHypothesisTrackerTests
    {
        [Ignore]
        [TestMethod()]
        public void UpdateTest()
        {
            var regionConfig = new RegionConfig();
            var vf = new VelocityField(640, 480);
            var mht = new MultipleHypothesisTracker(new RegionConfig(), vf);
            var rnd = new Random();
            var maxMeasurements = 50;
            var nFrames = 3000;

            Int64 totalMs = 0;
            for (int frame = 0; frame < nFrames; frame++)
            {
                var nMeasurements = rnd.Next(1, maxMeasurements);
                var measurements = new Measurement[nMeasurements];
                for (int i = 0; i < nMeasurements; i++)
                {
                    measurements[i] = new Measurement
                    {
                        Red = rnd.Next(1, 256),
                        Green = rnd.Next(1, 256),
                        Blue = rnd.Next(1, 256),
                        X = rnd.Next(0, 640),
                        Y = rnd.Next(0, 480)
                    };
                }

                //TODO: This tests the same "method" but NOT the same actual code that is used at runtime. Need to rewrite this test.
                var cts = new CancellationTokenSource();
                var ct = cts.Token;
                var watch = Stopwatch.StartNew();
                try
                {
                    var mhtTask = Task.Run(() => mht.Update(measurements, ct, 0.1), ct);
                    if (mhtTask.Wait(TimeSpan.FromMilliseconds(100)))
                    {
                    }
                    else
                    {
                        cts.Cancel();
                        watch.Stop();
                        var elapsedMs = watch.ElapsedMilliseconds;
                        totalMs += elapsedMs;
                        if (elapsedMs > 200)
                        {
                            Assert.Fail("MHT update took longer than 100ms");
                        }

                        while (!mhtTask.IsCompleted)
                        {
                        }
                    }
                }
                catch (AggregateException ex)
                {
                    ex.Handle(ce => true);
                }  
            }

            var avgMs = totalMs/nFrames;
            Debug.Write("Average execution time (ms): " + avgMs);
        }
    }
}