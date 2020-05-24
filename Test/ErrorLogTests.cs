using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using FX3Api;
using System.IO;
using System.Diagnostics;

namespace iSensor_FX3_Test
{
    class ErrorLogTests : FX3TestBase
    {

        [Test]
        public void ErrorLogCountTest()
        {
            InitializeTestCase();
            Console.WriteLine("Starting error log count test...");

            uint count, firstCount;

            List<FX3ErrorLog> log;

            firstCount = FX3.GetErrorLogCount();
            for(int trial = 0; trial < 5; trial++)
            {
                count = FX3.GetErrorLogCount();
                Console.WriteLine("Error log count: " + count.ToString());
                Assert.AreEqual(firstCount, count, "ERROR: Invalid error log count");
            }

            log = FX3.GetErrorLog();
            Assert.AreEqual(firstCount, log.Count(), "ERROR: Invalid error log size");

            Console.WriteLine("Rebooting FX3...");
            FX3.Disconnect();
            System.Threading.Thread.Sleep(1000);
            Connect();

            count = FX3.GetErrorLogCount();
            Console.WriteLine("Error log count: " + count.ToString());
            Assert.AreEqual(firstCount, count, "ERROR: Invalid error log count");

            log = FX3.GetErrorLog();
            Assert.AreEqual(firstCount, log.Count(), "ERROR: Invalid error log size");
        }

        [Test]
        public void ErrorLogContentsTest()
        {
            InitializeTestCase();
            Console.WriteLine("Starting error contents test...");

        }

        public void ErrorLogRobustnessTest()
        {
            InitializeTestCase();
            Console.WriteLine("Starting error log robustness test...");

            List<FX3ErrorLog> initialLog, log;

            I2CPreamble pre = new I2CPreamble();
            pre.DeviceAddress = 0xA0;
            pre.PreambleData.Add(0);
            pre.PreambleData.Add(0);
            pre.StartMask = 4;

            Console.WriteLine("Rebooting FX3...");
            FX3.Disconnect();
            System.Threading.Thread.Sleep(1000);
            Connect();

            Console.WriteLine("Getting initial error log...");
            initialLog = FX3.GetErrorLog();

            Console.WriteLine("Starting I2C Read...");
            FX3.I2CReadBytes(pre, 32, 1000);

            Console.WriteLine("Getting error log...");
            log = FX3.GetErrorLog();
            CheckLogEquality(initialLog, log);

            Console.WriteLine("Starting I2C write...");
            FX3.I2CWriteBytes(pre, new byte[] {1, 2, 3, 4}, 1000);

            Console.WriteLine("Getting error log...");
            log = FX3.GetErrorLog();
            CheckLogEquality(initialLog, log);
        }

        private void CheckLogEquality(List<FX3ErrorLog> log0, List<FX3ErrorLog> log1)
        {

        }

        private void GenerateErrorLog()
        {
            FX3.WatchdogEnable = true;
            FX3.WatchdogEnable = false;
            FX3.WatchdogEnable = false;
        }
    }
}
