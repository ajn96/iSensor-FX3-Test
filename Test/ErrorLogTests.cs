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

            List<FX3ErrorLog> initialLog, log;

            int count;

            initialLog = FX3.GetErrorLog();
            Console.WriteLine("Initial error log count: " + initialLog.Count.ToString());

            for(int trial = 0; trial < 5; trial++)
            {
                Console.WriteLine("Reading error log...");
                log = FX3.GetErrorLog();
                Assert.AreEqual(initialLog.Count, log.Count, "ERROR: Invalid error log count");
                for(int i = 0; i<log.Count; i++)
                {
                    Assert.AreEqual(initialLog[i], log[i], "ERROR: Invalid log entry");
                }
            }

            Console.WriteLine("Adding a new item to the error log...");
            string version = FX3.GetFirmwareVersion;
            long uptime = FX3.ActiveFX3.Uptime;
            GenerateErrorLog();
            log = FX3.GetErrorLog();
            Assert.AreEqual(initialLog.Count + 1, log.Count, "ERROR: Error log count not incremented correctly");
            count = log.Count;
            for(int i = 0; i < initialLog.Count; i++)
            {
                Assert.AreEqual(initialLog[i], log[i], "ERROR: Invalid older log entries");
            }
            /* Check new log entry */
            Assert.AreEqual(uptime, log[log.Count - 1].OSUptime, 1000, "ERROR: Invalid error log uptime");
            /*Check boot time stamp */
            uint expectedTimestamp = (uint)((DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds - (uptime / 1000));
            Assert.AreEqual(log[log.Count - 1].BootTimeStamp, expectedTimestamp, 120, "ERROR: Invalid boot time stamp");
            /* Check file (should have originated in main) */
            Assert.AreEqual(log[log.Count - 1].FileIdentifier, 1, "ERROR: Invalid file identifier");

            initialLog = FX3.GetErrorLog();
            Assert.AreEqual(count, log.Count, "ERROR: Invalid log count");

            /* Reboot FX3 and check contents */
            Console.WriteLine("Rebooting FX3...");
            FX3.Disconnect();
            Connect();

            for (int trial = 0; trial < 5; trial++)
            {
                Console.WriteLine("Reading error log...");
                log = FX3.GetErrorLog();
                Assert.AreEqual(initialLog.Count, log.Count, "ERROR: Invalid error log count");
                for (int i = 0; i < log.Count; i++)
                {
                    Assert.AreEqual(initialLog[i], log[i], "ERROR: Invalid log entry");
                }
            }
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
            pre.PreambleData.Add(0);
            pre.StartMask = 0;

            Console.WriteLine("Rebooting FX3...");
            FX3.Disconnect();
            System.Threading.Thread.Sleep(1000);
            Connect();

            Console.WriteLine("Getting initial error log...");
            initialLog = FX3.GetErrorLog();

            Console.WriteLine("Starting I2C Read...");
            try
            {
                FX3.I2CReadBytes(pre, 32, 1000);
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }

            Console.WriteLine("Getting error log...");
            log = FX3.GetErrorLog();
            CheckLogEquality(initialLog, log);

            Console.WriteLine("Starting I2C write...");
            try
            {
                FX3.I2CWriteBytes(pre, new byte[] { 1, 2, 3, 4 }, 1000);
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }
            
            Console.WriteLine("Getting error log...");
            log = FX3.GetErrorLog();
            CheckLogEquality(initialLog, log);
        }

        private void CheckLogEquality(List<FX3ErrorLog> log0, List<FX3ErrorLog> log1)
        {
            Assert.AreEqual(log0.Count, log1.Count, "ERROR: Invalid log count");
            for(int i = 0; i < log0.Count; i++)
            {
                Assert.AreEqual(log0[i], log1[i], "ERROR: Mis-matching log entry");
            }
        }

        private void GenerateErrorLog()
        {
            FX3.WatchdogEnable = true;
            FX3.WatchdogEnable = false;
            FX3.WatchdogEnable = false;
        }
    }
}
