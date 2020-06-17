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

            if(firstCount > 1500)
            {
                Console.WriteLine("ERROR: Error log full. Clearing...");
                FX3.ClearErrorLog();
                Assert.AreEqual(0, FX3.GetErrorLogCount(), "ERROR: Error log count clear failed");
                firstCount = 0;
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

            uint count;

            count = FX3.GetErrorLogCount();
            if (count > 1500)
            {
                Console.WriteLine("Error log count of " + count.ToString() + " exceeds log capacity. Clearing log...");
                FX3.ClearErrorLog();
                Assert.AreEqual(0, FX3.GetErrorLogCount(), "ERROR: Error log count clear failed");
            }

            initialLog = FX3.GetErrorLog();
            Console.WriteLine("Initial error log count: " + initialLog.Count.ToString());
            foreach(FX3ErrorLog logEntry in initialLog)
            {
                Console.WriteLine(logEntry.ToString());
            }

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
            Console.WriteLine(log[log.Count - 1].ToString());
            Assert.AreEqual(initialLog.Count + 1, log.Count, "ERROR: Error log count not incremented correctly");
            count = (uint) log.Count;
            for(int i = 0; i < initialLog.Count; i++)
            {
                Assert.AreEqual(initialLog[i], log[i], "ERROR: Invalid older log entries");
            }
            /* Check new log entry */
            Assert.AreEqual(uptime, log[log.Count - 1].OSUptime, 1000, "ERROR: Invalid error log uptime");
            /*Check boot time stamp */
            uint expectedTimestamp = (uint)((DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds - (uptime / 1000));
            Assert.AreEqual(log[log.Count - 1].BootTimeStamp, expectedTimestamp, 120, "ERROR: Invalid boot time stamp");
            /* Check file (should have originated in HelperFunctions (10)) */
            Assert.AreEqual(10, log[log.Count - 1].FileIdentifier, "ERROR: Invalid file identifier. Expected error to have originated in HelperFunctions.c");

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

        [Test]
        public void ErrorLogRobustnessTest()
        {
            InitializeTestCase();
            Console.WriteLine("Starting error log robustness test...");

            List<FX3ErrorLog> initialLog, log;

            I2CPreamble preRead = new I2CPreamble();
            preRead.DeviceAddress = 0xA0;
            preRead.PreambleData.Add(0);
            preRead.PreambleData.Add(0);
            preRead.PreambleData.Add(0xA1);
            preRead.StartMask = 4;

            I2CPreamble preWrite = new I2CPreamble();
            preWrite.DeviceAddress = 0;
            preWrite.PreambleData.Add(0);
            preWrite.StartMask = 0;

            Console.WriteLine("Rebooting FX3...");
            FX3.Disconnect();
            System.Threading.Thread.Sleep(500);
            Connect();

            Console.WriteLine("Getting initial error log...");
            initialLog = FX3.GetErrorLog();
            foreach (FX3ErrorLog entry in initialLog)
                Console.WriteLine(entry.ToString());

            Console.WriteLine("Starting I2C Read...");
            try
            {
                FX3.I2CReadBytes(preRead, 32, 1000);
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }

            Console.WriteLine("Getting error log...");
            log = FX3.GetErrorLog();
            CheckLogEquality(initialLog, log, false);

            Console.WriteLine("Starting invalid I2C write...");
            try
            {
                FX3.I2CWriteBytes(preWrite, new byte[] { 1, 2, 3, 4 }, 1000);
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }
            
            Console.WriteLine("Getting error log...");
            log = FX3.GetErrorLog();
            CheckLogEquality(initialLog, log, true);

            initialLog.Clear();
            foreach(FX3ErrorLog entry in log)
            {
                initialLog.Add(entry);
            }

            Console.WriteLine("Starting I2C stream...");
            FX3.StartI2CStream(preRead, 64, 10);
            System.Threading.Thread.Sleep(2000);

            Console.WriteLine("Getting error log...");
            log = FX3.GetErrorLog();
            CheckLogEquality(initialLog, log, false);

            Console.WriteLine("Rebooting FX3...");
            FX3.Disconnect();
            System.Threading.Thread.Sleep(500);
            Connect();

            Console.WriteLine("Getting error log...");
            log = FX3.GetErrorLog();
            CheckLogEquality(initialLog, log, false);

            Console.WriteLine("Starting I2C Read...");
            try
            {
                FX3.I2CReadBytes(preRead, 32, 1000);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            Console.WriteLine("Generating new error log...");
            GenerateErrorLog();

            Console.WriteLine("Getting error log...");
            log = FX3.GetErrorLog();
            Assert.AreEqual(initialLog.Count + 1, log.Count, "ERROR: Invalid log count");
            CheckLogEquality(initialLog, log, true);
        }

        [Test]
        public void ErrorLogClearTest()
        {
            Console.WriteLine("Starting error log clear test...");

            List<FX3ErrorLog> initialLog, log;
            initialLog = FX3.GetErrorLog();
            Console.WriteLine("Initial error log count: " + initialLog.Count.ToString());
            foreach (FX3ErrorLog logEntry in initialLog)
            {
                Console.WriteLine(logEntry.ToString());
            }
            Console.WriteLine("Clearing error log...");
            FX3.ClearErrorLog();
            log = FX3.GetErrorLog();
            Console.WriteLine("Error log count: " + log.Count.ToString());
            foreach (FX3ErrorLog logEntry in log)
            {
                Console.WriteLine(logEntry.ToString());
            }
            Assert.AreEqual(0, log.Count, "ERROR: Error log failed to clear");
            Assert.AreEqual(0, FX3.GetErrorLogCount(), "ERROR: Error log failed to clear");

            Console.WriteLine("Rebooting FX3...");
            FX3.Disconnect();
            System.Threading.Thread.Sleep(1000);
            FX3.WaitForBoard(5);
            FX3.Connect(FX3.AvailableFX3s[0]);

            Assert.AreEqual(0, log.Count, "ERROR: Error log failed to clear");
            Assert.AreEqual(0, FX3.GetErrorLogCount(), "ERROR: Error log failed to clear");
        }

        private void CheckLogEquality(List<FX3ErrorLog> log0, List<FX3ErrorLog> log1, bool isExtraEntryAllowed)
        {
            if(isExtraEntryAllowed)
                Assert.GreaterOrEqual(log1.Count, log0.Count, "ERROR: Invalid log count");
            else
                Assert.AreEqual(log1.Count, log0.Count, "ERROR: Invalid log count");

            for (int i = 0; i < log0.Count; i++)
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
