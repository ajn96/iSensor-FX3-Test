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
    class FunctionalTests : FX3TestBase
    {
        [Test]
        public void CompileDateTest()
        {
            InitializeTestCase();
            Console.WriteLine("Starting build date/time test...");

            DateTime fwDate, apiDate;

            double secondsDiff;

            Console.WriteLine("FX3 API build date: " + FX3.GetFX3ApiInfo.BuildDateTime);
            Console.WriteLine("FX3 firmware build date: " + FX3.ActiveFX3.BuildDateTime);

            apiDate = DateTime.Parse(FX3.GetFX3ApiInfo.BuildDateTime);
            fwDate = DateTime.Parse(FX3.ActiveFX3.BuildDateTime);

            Console.WriteLine("Parsed API Date: " + apiDate.ToString());
            Console.WriteLine("Parsed firmware Date: " + fwDate.ToString());

            secondsDiff = Math.Abs((apiDate - fwDate).TotalSeconds);
            /* Just assert compiled within 90 days of each other. Probably will always be the case */
            Assert.LessOrEqual(secondsDiff, 90 * 24 * 60 * 60, "ERROR: Expected firmware and API to be compiled within 90 days of each other");
        }

        [Test]
        public void BoardDetectionTest()
        {
            InitializeTestCase();
            Console.WriteLine("Starting FX3 board detection test...");

            string sn = FX3.ActiveFX3SerialNumber;
            Assert.AreEqual(0, FX3.AvailableFX3s.Count(), "ERROR: Expected no available FX3s");
            Assert.AreEqual(1, FX3.BusyFX3s.Count(), "ERROR: Expected 1 busy FX3");
            Assert.AreEqual(sn, FX3.BusyFX3s[0], "Invalid busy FX3 SN");

            Console.WriteLine("Rebooting FX3...");
            FX3.Disconnect();
            FX3.WaitForBoard(10);
            Assert.AreEqual(1, FX3.AvailableFX3s.Count(), "ERROR: Expected 1 available FX3");
            Assert.AreEqual(sn, FX3.AvailableFX3s[0], "Invalid available FX3 SN");
            Assert.AreEqual(0, FX3.BusyFX3s.Count(), "ERROR: Expected 0 busy FX3s");

            FX3.Connect(sn);
            Assert.AreEqual(0, FX3.AvailableFX3s.Count(), "ERROR: Expected no available FX3s");
            Assert.AreEqual(1, FX3.BusyFX3s.Count(), "ERROR: Expected 1 busy FX3");
            Assert.AreEqual(sn, FX3.BusyFX3s[0], "Invalid busy FX3 SN");
        }

        [Test]
        public void SerialNumberTest()
        {
            InitializeTestCase();
            Console.WriteLine("Starting FX3 serial number test...");

            int exCount = 0;

            string sn = FX3.ActiveFX3SerialNumber;
            Assert.AreEqual(sn, FX3.GetTargetSerialNumber, "ERROR: Invalid target serial number");
            Assert.AreEqual(sn, FX3.ActiveFX3.SerialNumber, "ERRRO: Invalid active FX3 serial number");

            FX3.Disconnect();
            Assert.AreEqual(null, FX3.ActiveFX3SerialNumber, "ERROR: Expected null SN when board disconnected");
            try
            {
                /* This one communicates to board, should throw exception if board not connected */
                Console.WriteLine(FX3.GetTargetSerialNumber);
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
                exCount = 1;
            }
            Assert.AreEqual(1, exCount, "ERROR: Expected exception to be thrown");
            
            Assert.AreEqual(null, FX3.ActiveFX3, "ERROR: Expected null SN when board disconnected");

            FX3.WaitForBoard(5);

            FX3.Connect(sn);
            Assert.AreEqual(sn, FX3.ActiveFX3SerialNumber, "ERROR: serial number");
            Assert.AreEqual(sn, FX3.GetTargetSerialNumber, "ERROR: Invalid target serial number");
            Assert.AreEqual(sn, FX3.ActiveFX3.SerialNumber, "ERRRO: Invalid active FX3 serial number");
        }

        [Test]
        public void BoardInfoTest()
        {
            InitializeTestCase();
            Console.WriteLine("Starting FX3 board info test...");

            FX3Board startInfo = FX3.ActiveFX3;
            Console.WriteLine("Board: " + startInfo.ToString());
            Assert.IsFalse(startInfo.VerboseMode, "ERROR: FX3 firmware should not be in verbose mode");
            Console.WriteLine("Disconnecting FX3...");
            FX3.Disconnect();
            Assert.IsNull(FX3.ActiveFX3, "ERROR: Expected FX3 object to be null after disconnect");
            FX3.WaitForBoard(10);
            Console.WriteLine("Reconnecting...");
            FX3.Connect(FX3.AvailableFX3s[0]);
            Console.WriteLine("Board: " + FX3.ActiveFX3.ToString());
            Assert.AreEqual(startInfo.SerialNumber, FX3.ActiveFX3.SerialNumber, "ERROR: Invalid serial number");
            Assert.AreEqual(startInfo.FirmwareVersionNumber, FX3.ActiveFX3.FirmwareVersionNumber, "ERROR: Invalid firmware version number");
            Assert.AreEqual(startInfo.VerboseMode, FX3.ActiveFX3.VerboseMode, "ERROR: Invalid verbose mode setting");
            Assert.AreEqual(FX3.GetFX3ApiInfo.VersionNumber, FX3.ActiveFX3.FirmwareVersionNumber, "ERROR: FX3 Firmware version does not match API");

            Console.WriteLine("Testing board up-time setting...");
            Stopwatch timer = new Stopwatch();

            long startTime = FX3.ActiveFX3.Uptime;
            timer.Start();
            while (timer.ElapsedMilliseconds < 3000)
            {
                Assert.AreEqual(timer.ElapsedMilliseconds + startTime, FX3.ActiveFX3.Uptime, 50, "ERROR: Invalid FX3 Uptime");
                System.Threading.Thread.Sleep(10);
            }
        }

        [Test]
        public void FirmwareLoadTest()
        {
            InitializeTestCase();
            Console.WriteLine("Starting firmware load test...");

            const int RESET_TRIALS = 4;

            string sn = FX3.ActiveFX3.SerialNumber;

            int exCount = 0;

            for(int trial = 0; trial < RESET_TRIALS; trial++)
            {
                Console.WriteLine("Disconnecting FX3...");
                FX3.Disconnect();
                Assert.IsNull(FX3.ActiveFX3, "ERROR: Active board should be null after disconnect");
                FX3.WaitForBoard(5);
                Assert.AreEqual(1, FX3.AvailableFX3s.Count, "ERROR: Expected only 1 FX3 to be available...");
                Console.WriteLine("Connecting to FX3...");
                FX3.Connect(sn);
            }

            try
            {
                FX3.Disconnect();
                exCount = 0;
                FX3.Connect("Bad SN");
            }
            catch(Exception e)
            {
                Assert.True(e is FX3ProgrammingException, "ERROR: Expected FX3 programming exception to be thrown");
                exCount++;
            }
            Assert.AreEqual(1, exCount, "ERROR: No exception throw for connecting to invalid board");
        }

        [Test]
        public void ImagePathsTest()
        {
            InitializeTestCase();

            int exCount;

            Console.WriteLine("Starting firmware image path validation test...");

            Assert.AreEqual(ResourcePath + @"\FX3_Firmware.img", FX3.FirmwarePath, "ERROR: Invalid firmware path");
            Assert.AreEqual(ResourcePath + @"\boot_fw.img", FX3.BootloaderPath, "ERROR: Invalid firmware path");
            Assert.AreEqual(ResourcePath + @"\USBFlashProg.img", FX3.FlashProgrammerPath, "ERROR: Invalid firmware path");

            try
            {
                exCount = 0;
                FX3Connection temp = new FX3Connection("bad.img", ResourcePath, ResourcePath, DeviceType.IMU);
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
                Assert.True(e is FX3ConfigurationException, "ERROR: Expected FX3 configuration exception to be thrown");
                exCount = 1;
            }
            Assert.AreEqual(1, exCount, "ERROR: Expected exception to be thrown for bad firmware image path");

            try
            {
                exCount = 0;
                FX3Connection temp = new FX3Connection(ResourcePath, "bad.img", ResourcePath, DeviceType.IMU);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Assert.True(e is FX3ConfigurationException, "ERROR: Expected FX3 configuration exception to be thrown");
                exCount = 1;
            }
            Assert.AreEqual(1, exCount, "ERROR: Expected exception to be thrown for bad bootloader image path");

            try
            {
                exCount = 0;
                FX3Connection temp = new FX3Connection(ResourcePath, ResourcePath, "bad.img", DeviceType.IMU);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Assert.True(e is FX3ConfigurationException, "ERROR: Expected FX3 configuration exception to be thrown");
                exCount = 1;
            }
            Assert.AreEqual(1, exCount, "ERROR: Expected exception to be thrown for bad flash programmer image path");
        }

        [Test]
        public void BootStatusTest()
        {
            InitializeTestCase();
            Console.WriteLine("Starting boot status retrieval test...");

            string sn = FX3.ActiveFX3.SerialNumber;
            string status;

            status = FX3.GetBootStatus;
            Console.WriteLine(status);
            Assert.False(status.Contains("Not"), "ERROR: Expected firmware to be running");
            Assert.True(FX3.FX3BoardAttached, "ERROR: Expected FX3 board to be attached");

            Console.WriteLine("Disconnecting FX3...");
            FX3.Disconnect();
            Assert.IsFalse(FX3.FX3BoardAttached, "ERROR: Expected FX3 board not to be attached");
            status = FX3.GetBootStatus;
            Console.WriteLine(status);
            Assert.True(status.Contains("Not"), "ERROR: Expected firmware to not be running");

            Console.WriteLine("Re-connecting FX3...");
            FX3.WaitForBoard(5);
            FX3.Connect(sn);
            status = FX3.GetBootStatus;
            Console.WriteLine(status);
            Assert.False(status.Contains("Not"), "ERROR: Expected firmware to be running");
        }

        [Test]
        public void FirmwareVersionTest()
        {
            InitializeTestCase();
            Console.WriteLine("Starting firmware version test...");

            Assert.AreEqual(FX3.GetFirmwareVersion, FX3.ActiveFX3.FirmwareVersion, "ERROR: Invalid firmware version");
            Assert.True(FX3.GetFirmwareVersion.Contains(FX3.ActiveFX3.FirmwareVersionNumber), "ERROR: Expected firmware version to contain version number");
            Assert.True(FX3.GetFirmwareVersion.Contains("PUB"), "ERROR: Expected firmware version to contain PUB");
            Assert.AreEqual(FX3.GetFX3ApiInfo.VersionNumber, FX3.ActiveFX3.FirmwareVersionNumber, "ERROR: Firmware version must match API");
        }

        [Test]
        public void USBTimingTest()
        {
            InitializeTestCase();
            Console.WriteLine("Starting USB command execution timing test...");

            const int NUM_TRIALS = 5000;

            /* Timer for measuring elapsed time */
            Stopwatch timer = new Stopwatch();

            //Set pin
            timer.Restart();
            for(int trial = 0; trial < NUM_TRIALS; trial++)
            {
                FX3.SetPin(FX3.DIO1, 0);
            }
            timer.Stop();
            Console.WriteLine("Average SetPin execution time: " + ((timer.ElapsedMilliseconds * 1000) / NUM_TRIALS).ToString() + "us");

            timer.Restart();
            for (int trial = 0; trial < NUM_TRIALS; trial++)
            {
                FX3.ReadPin(FX3.DIO1);
            }
            timer.Stop();
            Console.WriteLine("Average ReadPin execution time: " + ((timer.ElapsedMilliseconds * 1000) / NUM_TRIALS).ToString() + "us");

            timer.Restart();
            for (int trial = 0; trial < NUM_TRIALS; trial++)
            {
                FX3.StallTime = 10;
            }
            timer.Stop();
            Console.WriteLine("Average SPI parameter configuration time: " + ((timer.ElapsedMilliseconds * 1000) / NUM_TRIALS).ToString() + "us");
        }
    }
}
