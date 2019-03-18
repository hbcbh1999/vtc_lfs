using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VTC.Common;
using System.Collections.Generic;
using System.Drawing;
using System.Net;
using MathNet.Numerics.Properties;
using VTC.Remote;


namespace VTC
{
    [TestClass]
    public class RemoteServerTest
    {
        private const string TestServerUrl = "http://192.168.238.129:3000";

        [TestMethod]
        public void SendMovementTest()
        {
            var rs = new RemoteServer();

            var stateEstimates = new List<StateEstimate>();

            var m = new Movement("Approach 1", "Exit 1", Turn.Left, ObjectType.Car, stateEstimates, 0);

            try
            {
                var rsr = rs.SendMovement(m, "njwwirqmnwkJMPgtnsYXTGY4",TestServerUrl).Result;

                if (rsr != HttpStatusCode.OK)
                {
                    Assert.Fail("Not accepted by remote server.");
                }
            }
            catch (Exception ex)
            {
                Assert.Fail("Exception in SendMovement.");
            }            
        }

        [TestMethod]
        public void SendImageTest()
        {
            var rs = new RemoteServer();

            try
            {
                var rsr = rs.SendImage(Image.FromFile(".\\TestFiles\\cars.png"),"njwwirqmnwkJMPgtnsYXTGY4",TestServerUrl).Result;

                if (rsr != HttpStatusCode.OK)
                {
                    Assert.Fail("Not accepted by remote server.");
                }
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.Message);
            }            
        }

        [TestMethod]
        public void SendImageTest2()
        {
            var rs = new RemoteServer();

            try
            {
                var rsr = rs.SendImage(Image.FromFile(".\\TestFiles\\placeholder.png"),"njwwirqmnwkJMPgtnsYXTGY4",TestServerUrl).Result;

                if (rsr != HttpStatusCode.OK)
                {
                    Assert.Fail("Not accepted by remote server.");
                }
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.Message);
            }            
        }
    }
}
