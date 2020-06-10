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
    class I2CTests : FX3TestBase
    {

        [Test]
        public void I2CReadTest()
        {
            InitializeTestCase();
            Console.WriteLine("Starting I2C read test...");

            byte[] InitialRead, SecondRead;

            I2CPreamble pre = new I2CPreamble();
            pre.DeviceAddress = 0xA0;
            pre.PreambleData.Add(0);
            pre.PreambleData.Add(0);
            pre.StartMask = 4;

            for(uint readLen = 2; readLen < 256; readLen+=2)
            {
                Console.WriteLine("Testing " + readLen.ToString() + " byte read");
                InitialRead = FX3.I2CReadBytes(pre, readLen, 1000);
                SecondRead = FX3.I2CReadBytes(pre, readLen, 1000);
                for(int i = 0; i < InitialRead.Count(); i++)
                {
                    Assert.AreEqual(InitialRead[i], SecondRead[i], "ERROR: Expected flash read data to match");
                }
            }
        }

        [Test]
        public void I2CWriteTest()
        {
            InitializeTestCase();
            Console.WriteLine("Starting I2C write test...");
        }

        [Test]
        public void I2CRetryTest()
        {
            InitializeTestCase();
            Console.WriteLine("Starting I2C retry test...");

            Stopwatch timer = new Stopwatch();

            I2CPreamble pre = new I2CPreamble();
            pre.DeviceAddress = 0x0;
            pre.PreambleData.Add(0);
            pre.PreambleData.Add(0);
            pre.StartMask = 0;

            Console.WriteLine("Setting retry count to 0");
            FX3.I2CRetryCount = 0;
            Assert.AreEqual(0, FX3.I2CRetryCount, "ERROR: Setting not applied correctly");

            Console.WriteLine("Attempting to read non-existent device... (address 0)");
            timer.Start();
            try
            {
                FX3.I2CReadBytes(pre, 128, 1000);
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }
            timer.Stop();
            Console.WriteLine("Elapsed time: " + timer.ElapsedMilliseconds.ToString() + "ms");
            CheckFirmwareResponsiveness();
        }

        [Test]
        public void I2CBitRateTest()
        {
            InitializeTestCase();
            Console.WriteLine("Starting I2C bit rate test...");

            Console.WriteLine("Testing input validation...");
            uint startingBitRate;
            int numExpections = 0;

            startingBitRate = FX3.I2CBitRate;
            Assert.AreEqual(100000, startingBitRate, "ERROR: Invalid default I2C bit rate");
            try
            {
                FX3.I2CBitRate = 99999;
            }
            catch(Exception e)
            {
                numExpections++;
                Console.WriteLine(e.Message);
            }
            Assert.AreEqual(1, numExpections, "ERROR: Expected exception to be thrown");
            Assert.AreEqual(startingBitRate, FX3.I2CBitRate, "ERROR: Expected I2C bit rate setting to be rejected");

            try
            {
                FX3.I2CBitRate = 1000001;
            }
            catch (Exception e)
            {
                numExpections++;
                Console.WriteLine(e.Message);
            }
            Assert.AreEqual(2, numExpections, "ERROR: Expected exception to be thrown");
            Assert.AreEqual(startingBitRate, FX3.I2CBitRate, "ERROR: Expected I2C bit rate setting to be rejected");

            Console.WriteLine("Testing reads across valid bit rate range...");

            I2CPreamble pre = new I2CPreamble();
            pre.DeviceAddress = 0xA0;
            pre.PreambleData.Add(0);
            pre.PreambleData.Add(0);
            pre.StartMask = 4;

            byte[] InitialRead, SecondRead;

            Console.WriteLine("Performing initial flash read...");
            const uint READ_LEN = 1024;
            InitialRead = FX3.I2CReadBytes(pre, READ_LEN, 2000);
            for (uint bitrate = 100000; bitrate <= 1000000; bitrate += 100000)
            {
                Console.WriteLine("Testing " + bitrate.ToString() + "bits/s...");
                FX3.I2CBitRate = bitrate;
                Assert.AreEqual(bitrate, FX3.I2CBitRate, "ERROR: Setting bit rate failed");
                SecondRead = FX3.I2CReadBytes(pre, READ_LEN, 2000);
                for (int i = 0; i < InitialRead.Count(); i++)
                {
                    Assert.AreEqual(InitialRead[i], SecondRead[i], "ERROR: Expected flash read data to match");
                }
            }
        }

        [Test]
        public void I2CStreamTest()
        {
            InitializeTestCase();
            Console.WriteLine("Starting I2C data stream test...");
        }

        [Test]
        public void I2CStreamCancelTest()
        {
            InitializeTestCase();
            Console.WriteLine("Starting I2C stream cancel test...");

            long firstCount;

            I2CPreamble pre = new I2CPreamble();
            pre.DeviceAddress = 0xA0;
            pre.PreambleData.Add(0);
            pre.PreambleData.Add(0);
            pre.StartMask = 4;

            for (int trial = 0; trial < 5; trial++)
            {
                Console.WriteLine("Starting trial " + trial.ToString());

                /* Start stream */
                FX3.StartI2CStream(pre, 64, 1000000);
                firstCount = FX3.GetNumBuffersRead;
                System.Threading.Thread.Sleep(100);
                Assert.Greater(FX3.GetNumBuffersRead, firstCount, "ERROR: Expected to have read buffers");

                /* Cancel stream (stop stream) */
                FX3.StopStream();
                System.Threading.Thread.Sleep(20);

                /* Test read functionality */
                TestI2CFunctionality();

                /* Start stream */
                FX3.StartI2CStream(pre, 64, 1000000);
                firstCount = FX3.GetNumBuffersRead;
                System.Threading.Thread.Sleep(100);
                Assert.Greater(FX3.GetNumBuffersRead, firstCount, "ERROR: Expected to have read buffers");

                /* Cancel stream (cancel stream) */
                FX3.CancelStreamAsync();
                System.Threading.Thread.Sleep(20);

                /* Test read functionality */
                TestI2CFunctionality();
            }
        }

        [Test]
        public void I2CModeSwitchingTest()
        {
            InitializeTestCase();
            Console.WriteLine("Starting I2C mode switching stress test...");
        }

        private void TestI2CFunctionality()
        {
            byte[] InitialRead, SecondRead;

            I2CPreamble pre = new I2CPreamble();
            pre.DeviceAddress = 0xA0;
            pre.PreambleData.Add(0);
            pre.PreambleData.Add(0);
            pre.StartMask = 4;

            InitialRead = FX3.I2CReadBytes(pre, 64, 1000);
            SecondRead = FX3.I2CReadBytes(pre, 64, 1000);
            for (int i = 0; i < InitialRead.Count(); i++)
            {
                Assert.AreEqual(InitialRead[i], SecondRead[i], "ERROR: Expected flash read data to match");
            }
        }
    }
}
