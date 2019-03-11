using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VTC.Common;
using System.Collections.Generic;
using System.Drawing;
using System.Net;
using MathNet.Numerics.Properties;


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
                var rsr = rs.SendMovement(m, "1",TestServerUrl).Result;

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
                var rsr = rs.SendImage(Image.FromFile(".\\TestFiles\\cars.png"),"1",TestServerUrl).Result;

                if (rsr != HttpStatusCode.Found)
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
        public void SendImageTest2()
        {
            var rs = new RemoteServer();

            try
            {
                var rsr = rs.SendImage(Image.FromFile(".\\TestFiles\\placeholder.png"),"1",TestServerUrl).Result;

                if (rsr != HttpStatusCode.Found)
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
