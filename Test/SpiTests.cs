using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using FX3Api;
using System.IO;
using System.Diagnostics;
using RegMapClasses;
using AdisApi;

namespace iSensor_FX3_Test
{
    class SpiTests : FX3TestBase
    {

        [Test]
        public void TransferArrayWithWriteTest()
        {
            InitializeTestCase();
            uint[] res;
            uint[] initialMOSI = new uint[6];
            uint[] repeatedMOSI = new uint[4];

            Stopwatch timer = new Stopwatch();
            double time;

            Console.WriteLine("Starting SPI write then read stream test...");

            FX3.WordLength = 32;
            FX3.StallTime = 3;
            FX3.SclkFrequency = 4000000;
            FX3.DrActive = false;
            FX3.DrPin = FX3.DIO3;

            for(uint i = 0; i < 4; i++)
            {
                repeatedMOSI[i] = i;
            }
            initialMOSI[0] = 0xAAAAAAAA;
            initialMOSI[5] = 0x55555555;

            /* Both false */
            Console.WriteLine("Testing both DR inactive...");
            FX3.DrActive = false;
            System.Threading.Thread.Sleep(10);
            FX3.StartPWM(10, 0.5, FX3.DIO4);
            timer.Restart();
            res = FX3.WriteReadTransferArray(initialMOSI, false, repeatedMOSI, 10);
            time = timer.ElapsedMilliseconds;
            Assert.LessOrEqual(time, 10, "Expected time of less than 10ms");
            Assert.AreEqual(repeatedMOSI.Count() * 10, res.Count(), "Invalid data size");
            CheckRxData(res);

            /* dr active */
            Console.WriteLine("Testing DR active read...");
            FX3.DrActive = true;
            System.Threading.Thread.Sleep(10);
            FX3.StartPWM(10, 0.5, FX3.DIO4);
            timer.Restart();
            res = FX3.WriteReadTransferArray(initialMOSI, false, repeatedMOSI, 10);
            time = timer.ElapsedMilliseconds;
            //10 reads at 10Hz -> approx 1 sec. Give range 1000 - 1200
            Assert.AreEqual(time, 1100, 100, "Expected time of between 1000ms and 1200ms");
            Assert.AreEqual(repeatedMOSI.Count() * 10, res.Count(), "Invalid data size");
            CheckRxData(res);

            /* initial dr active */
            Console.WriteLine("Testing DR active write...");
            FX3.DrActive = false;
            System.Threading.Thread.Sleep(10);
            FX3.StartPWM(10, 0.5, FX3.DIO4);
            timer.Restart();
            res = FX3.WriteReadTransferArray(initialMOSI, true, repeatedMOSI, 10);
            time = timer.ElapsedMilliseconds;
            //Should be between 50ms and 150ms
            Assert.AreEqual(time, 100, 50, "Expected time of between 50ms and 150ms");
            Assert.AreEqual(repeatedMOSI.Count() * 10, res.Count(), "Invalid data size");
            CheckRxData(res);

            /* Both true */
            Console.WriteLine("Testing both DR active...");
            FX3.DrActive = true;
            System.Threading.Thread.Sleep(10);
            FX3.StartPWM(10, 0.5, FX3.DIO4);
            timer.Restart();
            res = FX3.WriteReadTransferArray(initialMOSI, true, repeatedMOSI, 10);
            time = timer.ElapsedMilliseconds;
            //10 reads at 10Hz -> approx 1 sec. Give range 1000 - 1200
            Assert.AreEqual(time, 1100, 100, "Expected time of between 1000ms and 1200ms");
            Assert.AreEqual(repeatedMOSI.Count() * 10, res.Count(), "Invalid data size");
            CheckRxData(res);
        }

        private void CheckRxData(uint[] data)
        {
            //check data
            int index = 0;
            for (int i = 0; i < data.Count(); i += 4)
            {
                for (uint j = 0; j < 4; j++)
                {
                    Assert.AreEqual(j, data[index], "Expected SPI data to be echoed");
                    index += 1;
                }
            }
        }

        [Test]
        public void IRegInterfaceTest()
        {
            InitializeTestCase();
            Console.WriteLine("Starting IRegInterface test...");

            FX3.WordLength = 16;
            FX3.WriteRegByte(new AddrDataPair() { addr = 0, data = 0 });
        }

        [Test]
        public void SpiStallTimeTest()
        {
            InitializeTestCase();
            Console.WriteLine("Starting SPI stall time test...");

            double expectedTime;
            Stopwatch timer = new Stopwatch();
            double baseTime = 0;
            int numReads = 5;

            uint[] MOSI = new uint[200];

            /* Stall time for transfer reads (32 bits) */
            Console.WriteLine("Testing stall time for 32-bit reads (transfer stream)...");
            FX3.WordLength = 32;
            FX3.SclkFrequency = 15000000;
            FX3.ChipSelectLeadTime = SpiLagLeadTime.SPI_SSN_LAG_LEAD_ONE_HALF_CLK;
            FX3.ChipSelectLagTime = SpiLagLeadTime.SPI_SSN_LAG_LEAD_ONE_HALF_CLK;

            /* Get base stall time (5us stall) */
            for (int i = 0; i < 8; i++)
            {
                timer.Restart();
                for (int trial = 0; trial < numReads; trial++)
                {
                    FX3.TransferArray(MOSI, 5);
                }
                timer.Stop();
                baseTime += timer.ElapsedMilliseconds;
            }
            /* Average base time */
            baseTime /= 8.0;
            Console.WriteLine("Base SPI transfer time with 5us stall: " + baseTime.ToString() + "ms");

            for (ushort stallTime = 50; stallTime >= 7; stallTime--)
            {
                Console.WriteLine("Testing stall time of " + stallTime.ToString() + "us");
                FX3.StallTime = stallTime;
                /* Perform sets of 5 sets of 200 32-bit transfers (999 stalls). Expected time is in ms */
                expectedTime = (stallTime - 5) * numReads;
                /* Add base time overhead */
                expectedTime += baseTime;
                timer.Restart();
                for (int trial = 0; trial < numReads; trial++)
                {
                    FX3.TransferArray(MOSI, 5);
                }
                timer.Stop();
                Console.WriteLine("Expected time: " + expectedTime.ToString() + "ms, real time: " + timer.ElapsedMilliseconds.ToString() + "ms");
                Assert.AreEqual(expectedTime, timer.ElapsedMilliseconds, 0.5 * baseTime, "ERROR: Invalid transfer time");
                System.Threading.Thread.Sleep(100);
            }

            /* Stall time for generic reads */
            Console.WriteLine("Testing stall time for 16-bit reads (generic stream)...");
            FX3.WordLength = 16;

            /* Get base stall time (5us stall) */
            baseTime = 0;
            for (int i = 0; i < 8; i++)
            {
                timer.Restart();
                for (int trial = 0; trial < numReads; trial++)
                {
                    FX3.ReadRegArray(MOSI, 5);
                }
                timer.Stop();
                baseTime += timer.ElapsedMilliseconds;
            }
            /* Average base time */
            baseTime /= 8.0;
            Console.WriteLine("Base SPI transfer time with 5us stall: " + baseTime.ToString() + "ms");

            for (ushort stallTime = 50; stallTime >= 7; stallTime--)
            {
                Console.WriteLine("Testing stall time of " + stallTime.ToString() + "us");
                FX3.StallTime = stallTime;
                /* Perform sets of 5 sets of 200 16-bit transfers (999 stalls). Expected time is in ms */
                expectedTime = (stallTime - 5) * numReads;
                /* Add base time overhead */
                expectedTime += baseTime;
                timer.Restart();
                for (int trial = 0; trial < numReads; trial++)
                {
                    FX3.ReadRegArray(MOSI, 5);
                }
                timer.Stop();
                Console.WriteLine("Expected time: " + expectedTime.ToString() + "ms, real time: " + timer.ElapsedMilliseconds.ToString() + "ms");
                Assert.AreEqual(expectedTime, timer.ElapsedMilliseconds, 0.5 * baseTime, "ERROR: Invalid transfer time");
                System.Threading.Thread.Sleep(100);
            }
            FX3.StallTime = 5;
        }


        [Test]
        public void BitBangSpiModeTest()
        {
            InitializeTestCase();
            Console.WriteLine("Starting bit bang SPI mode test...");

            byte[] MISO;

            uint mode;

            List<byte> MOSI = new List<byte>();
            for (int i = 0; i < 1024; i++)
            {
                MOSI.Add((byte)(i & 0x7F));
            }

            FX3.BitBangSpiConfig = new BitBangSpiConfig(true);
            FX3.BitBangSpiConfig.SCLK = (FX3PinObject)FX3.DIO3;
            FX3.ReadPin(FX3.DIO3);
            FX3.ReadPin(FX3.DIO3);
            Random rnd = new Random();
            for (int trial = 0; trial < 64; trial++)
            {
                mode = (uint)(rnd.NextDouble() * 4);
                Console.WriteLine("Testing bit-bang SPI mode " + mode.ToString());
                FX3.BitBangSpiConfig.CPHA = ((mode & 0x1) != 0);
                FX3.BitBangSpiConfig.CPOL = ((mode & 0x2) != 0);

                for(uint len = 8; len < 64; len += 8)
                {
                    MISO = FX3.BitBangSpi(len, 1, MOSI.ToArray(), 1000);
                    Assert.AreEqual(len >> 3, MISO.Count(), "ERROR: Invalid data count");
                    for (int i = 0; i < MISO.Count(); i++)
                    {
                        Assert.AreEqual(MOSI[i], MISO[i], "ERROR: Invalid echo data");
                    }
                }

                /* Check SCLK pin level after transfer */
                if (FX3.BitBangSpiConfig.CPOL)
                {
                    Assert.AreEqual(1, FX3.ReadPin(FX3.DIO4), "ERROR: Expected SCLK to be idle high");
                }
                else
                {
                    Assert.AreEqual(0, FX3.ReadPin(FX3.DIO4), "ERROR: Expected SCLK to be idle low");
                }
            }
        }

        [Test]
        public void BitBangSpiTest()
        {
            InitializeTestCase();
            Console.WriteLine("Starting bit bang SPI functionality test...");

            byte[] MISO;

            List<byte> MOSI = new List<byte>();
            for(int i = 0; i < 1024; i++)
            {
                MOSI.Add((byte)(i & 0x7F));
            }

            Console.WriteLine("Testing bit bang SPI word length...");
            for(uint wordLen = 8; wordLen <= 512; wordLen+= 8)
            {
                Console.WriteLine("Testing bit length of " + wordLen.ToString());
                /* Override SPI pins */
                FX3.BitBangSpiConfig = new BitBangSpiConfig(true);
                MISO = FX3.BitBangSpi(wordLen, 1, MOSI.ToArray(), 1000);
                Assert.AreEqual(Math.Ceiling(wordLen / 8.0), MISO.Count(), "ERROR: Invalid data count");
                for(int i = 0; i < MISO.Count(); i++)
                {
                    Assert.AreEqual(MOSI[i], MISO[i], "ERROR: Invalid echo data");
                }

                /* Override DIO3/4 pins */
                FX3.BitBangSpiConfig.MOSI = (FX3PinObject) FX3.DIO3;
                FX3.BitBangSpiConfig.MISO = (FX3PinObject) FX3.DIO4;
                MISO = FX3.BitBangSpi(wordLen, 1, MOSI.ToArray(), 1000);
                Assert.AreEqual(Math.Ceiling(wordLen / 8.0), MISO.Count(), "ERROR: Invalid data count");
                for (int i = 0; i < MISO.Count(); i++)
                {
                   Assert.AreEqual(MOSI[i], MISO[i], "ERROR: Invalid echo data");
                }
            }
            
            Console.WriteLine("Testing restore hardware SPI functionality...");
            FX3.RestoreHardwareSpi();
            TestSpiFunctionality();
        }

        [Test]
        public void BitBangSpiFreqTest()
        {
            InitializeTestCase();
            Console.WriteLine("Testing bit bang SPI SCLK freq...");

            byte[] MOSI = new byte[1024];
            double expectedTime;
            Stopwatch timer = new Stopwatch();
            double baseTime = 0;
            int numReads = 8;

            /* 1MHz SCLK */
            FX3.SetBitBangSpiFreq(1000000);

            /* Get base time (max SCLK with 0.5 microsecond stall) */
            FX3.SetBitBangStallTime(0.5);
            for (int i = 0; i < 4; i++)
            {
                timer.Restart();
                for (int trial = 0; trial < numReads; trial++)
                {
                    FX3.BitBangSpi(4000, 1, MOSI, 2000);
                }
                timer.Stop();
                baseTime += timer.ElapsedMilliseconds;
            }
            /* Average base time */
            baseTime /= 4.0;
            /* Subtract time for clocks in basetime */
            baseTime -= (4000000.0 / 1000000) * numReads;
            Console.WriteLine("Base bitbang SPI time: " + baseTime.ToString() + "ms");

            for (uint freq = 75000; freq <= 900000; freq += 25000)
            {
                Console.WriteLine("Testing freq of " + freq.ToString() + "Hz");
                FX3.SetBitBangSpiFreq(freq);
                /* Perform sets of 1 4000-bit transfer. Expected time is in ms */
                expectedTime = (4000000.0 / freq) * numReads;
                /* Add base time overhead */
                expectedTime += baseTime;
                timer.Restart();
                for (int trial = 0; trial < numReads; trial++)
                {
                    FX3.BitBangSpi(4000, 1, MOSI, 2000);
                }
                timer.Stop();
                Console.WriteLine("Expected time: " + expectedTime.ToString() + "ms, real time: " + timer.ElapsedMilliseconds.ToString() + "ms");
                Assert.AreEqual(expectedTime, timer.ElapsedMilliseconds, Math.Max(25, 0.1 * expectedTime), "ERROR: Invalid transfer time");
            }
        }

        [Test]
        public void BitBangSpiStallTimeTest()
        {
            InitializeTestCase();
            Console.WriteLine("Testing bit bang SPI stall time...");

            byte[] MOSI = new byte[1024];
            double expectedTime;
            Stopwatch timer = new Stopwatch();
            double baseTime = 0;
            int numReads = 5;

            FX3.SetBitBangSpiFreq(500000);

            /* Get base time (with half microsecond stall) */
            FX3.SetBitBangStallTime(0.5);
            for (int i = 0; i < 4; i++)
            {
                timer.Restart();
                for (int trial = 0; trial < numReads; trial++)
                {
                    FX3.BitBangSpi(4, 1001, MOSI, 2000);
                }
                timer.Stop();
                baseTime += timer.ElapsedMilliseconds;
            }
            /* Average base time */
            baseTime /= 4.0;
            Console.WriteLine("Base bitbang SPI time: " + baseTime.ToString() + "ms");

            for (double stallTime = 50; stallTime >= 5; stallTime--)
            {
                Console.WriteLine("Testing stall time of " + stallTime.ToString() + "us");
                FX3.SetBitBangStallTime(stallTime);
                /* Perform sets of 1001 4-bit transfers (1000 stalls). Expected time is in ms */
                expectedTime = stallTime * numReads;
                /* Add base time overhead */
                expectedTime += baseTime;
                timer.Restart();
                for (int trial = 0; trial < numReads; trial++)
                {
                    FX3.BitBangSpi(4, 1001, MOSI, 2000);
                }
                timer.Stop();
                Console.WriteLine("Expected time: " + expectedTime.ToString() + "ms, real time: " + timer.ElapsedMilliseconds.ToString() + "ms");
                Assert.AreEqual(expectedTime, timer.ElapsedMilliseconds, 0.5 * baseTime, "ERROR: Invalid transfer time");
            }
        }

        [Test]
        public void BurstSpiTest()
        {
            InitializeTestCase();
            Console.WriteLine("Starting SPI burst read test...");

            List<byte> BurstTxData = new List<byte>();
            ushort[] BurstData;
            int index;
            RegMapClasses.RegClass triggerReg = new RegMapClasses.RegClass();
            triggerReg.NumBytes = 2;
            triggerReg.Address = 0x12;

            FX3.DrActive = false;

            for(int byteCount = 4; byteCount<400; byteCount+= 2)
            {
                Console.WriteLine("Testing burst read of " + byteCount.ToString() + " bytes...");
                FX3.BurstByteCount = byteCount;
                Assert.AreEqual(byteCount, FX3.BurstByteCount, "ERROR; Byte count not applied correctly");
                Assert.AreEqual((byteCount - 2) / 2, FX3.WordCount, "ERROR: FX3 burst word count not set correctly");

                /* Burst trigger reg */
                FX3.TriggerReg = triggerReg;

                /* strip header */
                Console.WriteLine("Testing burst read with trigger reg and header stripped...");
                FX3.StripBurstTriggerWord = true;
                FX3.SetupBurstMode();
                FX3.StartBurstStream(1, FX3.BurstMOSIData);
                FX3.WaitForStreamCompletion(50);
                BurstData = FX3.GetBuffer();
                Assert.AreEqual((byteCount / 2) - 1, BurstData.Count(), "ERROR: Invalid burst data count");
                for (int i = 0; i < BurstData.Count(); i++)
                {
                    Assert.AreEqual(0, BurstData[i], "ERROR: Expected all burst data to be 0");
                }

                /* No strip header */
                Console.WriteLine("Testing burst read with trigger reg and header not stripped...");
                FX3.StripBurstTriggerWord = false;
                FX3.SetupBurstMode();
                FX3.StartBurstStream(1, FX3.BurstMOSIData);
                FX3.WaitForStreamCompletion(50);
                BurstData = FX3.GetBuffer();
                Assert.AreEqual((byteCount / 2), BurstData.Count(), "ERROR: Invalid burst data count");
                Assert.AreEqual(0x1200, BurstData[0], "ERROR: Invalid burst data echoed");
                for(int i = 1; i < BurstData.Count(); i++)
                {
                    Assert.AreEqual(0, BurstData[i], "ERROR: Expected remainder of burst data to be 0");
                }

                /* Burst transmit data */
                BurstTxData.Clear();
                for(int i = 0; i<byteCount; i++)
                {
                    BurstTxData.Add((byte) (i & 0xFFU));
                }
                FX3.BurstMOSIData = BurstTxData.ToArray();
                for(int i = 0; i<BurstTxData.Count; i++)
                {
                    Assert.AreEqual(BurstTxData[i], FX3.BurstMOSIData[i], "ERROR: Burst MOSI data not applied correctly");
                }

                /* strip header */
                Console.WriteLine("Testing burst read with MOSI byte array and header stripped...");
                FX3.StripBurstTriggerWord = true;
                FX3.SetupBurstMode();
                FX3.StartBurstStream(1, FX3.BurstMOSIData);
                FX3.WaitForStreamCompletion(50);
                BurstData = FX3.GetBuffer();
                Assert.AreEqual((byteCount / 2) - 1, BurstData.Count(), "ERROR: Invalid burst data count");
                index = 2;
                for (int i = 0; i < BurstData.Count(); i++)
                {
                    Assert.AreEqual(BurstTxData[index], BurstData[i] >> 8, "ERROR: Invalid burst data echoed");
                    Assert.AreEqual(BurstTxData[index + 1], BurstData[i] & 0xFF, "ERROR: Invalid burst data echoed");
                    index += 2;
                }

                /* No strip header */
                Console.WriteLine("Testing burst read with MOSI byte array and header not stripped...");
                FX3.StripBurstTriggerWord = false;
                FX3.SetupBurstMode();
                FX3.StartBurstStream(1, FX3.BurstMOSIData);
                FX3.WaitForStreamCompletion(50);
                BurstData = FX3.GetBuffer();
                Assert.AreEqual((byteCount / 2), BurstData.Count(), "ERROR: Invalid burst data count");
                index = 0;
                for (int i = 0; i < BurstData.Count(); i++)
                {
                    Assert.AreEqual(BurstTxData[index], BurstData[i] >> 8, "ERROR: Invalid burst data echoed");
                    Assert.AreEqual(BurstTxData[index + 1], BurstData[i] & 0xFF, "ERROR: Invalid burst data echoed");
                    index += 2;
                }
            }

            Console.WriteLine("Testing Dr Active triggering for burst...");
            FX3.DrPin = FX3.DIO3;
            FX3.StartPWM(100, 0.5, FX3.DIO4);

            FX3.BurstByteCount = 64;
            BurstTxData.Clear();
            for (int i = 0; i < 64; i++)
            {
                BurstTxData.Add((byte)(i & 0xFFU));
            }
            FX3.BurstMOSIData = BurstTxData.ToArray();
            FX3.StripBurstTriggerWord = false;
            FX3.SetupBurstMode();
            FX3.DrActive = true;

            double expectedTime;
            long drActiveTime, baseTime;
            Stopwatch timer = new Stopwatch();

            for(uint numBuffers = 100; numBuffers <= 300; numBuffers+= 100)
            {
                Console.WriteLine("Capturing " + numBuffers.ToString() + " buffers with DrActive set to false...");
                FX3.DrActive = false;
                FX3.StartBurstStream(numBuffers, FX3.BurstMOSIData);
                timer.Restart();
                while(FX3.GetNumBuffersRead < numBuffers)
                {
                    System.Threading.Thread.Sleep(1);
                }
                baseTime = timer.ElapsedMilliseconds;
                Console.WriteLine("Stream time: " + baseTime.ToString() + "ms");
                CheckBurstBuffers(BurstTxData.ToArray(), numBuffers);

                Console.WriteLine("Capturing " + numBuffers.ToString() + " buffers with DrActive set to true...");
                expectedTime = 10 * numBuffers; //100Hz DR
                FX3.DrActive = true;
                FX3.StartBurstStream(numBuffers, FX3.BurstMOSIData);
                timer.Restart();
                while (FX3.GetNumBuffersRead < numBuffers)
                {
                    System.Threading.Thread.Sleep(1);
                }
                drActiveTime = timer.ElapsedMilliseconds;
                Console.WriteLine("Stream time: " + drActiveTime.ToString() + "ms");
                Assert.AreEqual(expectedTime, drActiveTime, 0.25 * expectedTime, "ERROR: Invalid stream time");
                CheckBurstBuffers(BurstTxData.ToArray(), numBuffers);

                Assert.Less(baseTime, drActiveTime, "ERROR: Base stream time greater than DrActive stream time");
            }
        }

        void CheckBurstBuffers(byte[] data, uint numBuffers)
        {
            Assert.AreEqual(numBuffers, FX3.GetNumBuffersRead, "ERROR: Number of buffers read parameter is incorrect");

            ushort[] buf;

            buf = FX3.GetBuffer();
            while(buf != null)
            {
                Assert.AreEqual(data.Count(), buf.Count() * 2, 1, "ERROR: Invalid buffer size");
                int index = 0;
                for (int i = 0; i < buf.Count(); i++)
                {
                    Assert.AreEqual(data[index], buf[i] >> 8, "ERROR: Invalid burst data echoed");
                    Assert.AreEqual(data[index + 1], buf[i] & 0xFF, "ERROR: Invalid burst data echoed");
                    index += 2;
                }
                buf = FX3.GetBuffer();
            }
        }

        [Test]
        public void SpiTransferTest()
        {
            InitializeTestCase();
            Console.WriteLine("Starting SPI transfer test...");

            uint writeVal;

            uint[] readArray;

            List<uint> writeData = new List<uint>();

            /* Set word length of 8 */
            Console.WriteLine("Testing word length of 8 bits...");
            FX3.WordLength = 8;
            TestSpiFunctionality();
            for(int bit = 0; bit < 8; bit++)
            {
                Console.WriteLine("Testing bit " + bit.ToString());
                Assert.AreEqual(1U << bit, FX3.Transfer(1U << bit), "ERROR: SPI loop back failed");
            }
            /* Bits outside of word length should not be echo'd */
            for (int bit = 8; bit < 32; bit++)
            {
                Console.WriteLine("Testing bit " + bit.ToString());
                Assert.AreEqual(0, FX3.Transfer(1U << bit), "ERROR: SPI loop back failed");
            }

            /* Set word length of 16 */
            Console.WriteLine("Testing word length of 16 bits...");
            FX3.WordLength = 16;
            for (int bit = 0; bit < 16; bit++)
            {
                Console.WriteLine("Testing bit " + bit.ToString());
                Assert.AreEqual(1U << bit, FX3.Transfer(1U << bit), "ERROR: SPI loop back failed");
            }
            /* Bits outside of word length should not be echo'd */
            for (int bit = 16; bit < 32; bit++)
            {
                Console.WriteLine("Testing bit " + bit.ToString());
                Assert.AreEqual(0, FX3.Transfer(1U << bit), "ERROR: SPI loop back failed");
            }

            /* Set word length of 24 */
            Console.WriteLine("Testing word length of 24 bits...");
            FX3.WordLength = 24;
            for (int bit = 0; bit < 24; bit++)
            {
                Console.WriteLine("Testing bit " + bit.ToString());
                Assert.AreEqual(1U << bit, FX3.Transfer(1U << bit), "ERROR: SPI loop back failed");
            }
            /* Bits outside of word length should not be echo'd */
            for (int bit = 24; bit < 32; bit++)
            {
                Console.WriteLine("Testing bit " + bit.ToString());
                Assert.AreEqual(0, FX3.Transfer(1U << bit), "ERROR: SPI loop back failed");
            }

            /* Set word length of 32 */
            Console.WriteLine("Testing word length of 32 bits...");
            FX3.WordLength = 32;
            for (int bit = 0; bit < 32; bit++)
            {
                Console.WriteLine("Testing bit " + bit.ToString());
                Assert.AreEqual(1U << bit, FX3.Transfer(1U << bit), "ERROR: SPI loop back failed");
            }

            Console.WriteLine("Testing random 32-bit values...");
            var rnd = new Random();
            for(int trial = 0; trial < 256; trial++)
            {
                writeVal = (uint)(rnd.NextDouble() * uint.MaxValue);
                Console.WriteLine("Writing 0x" + writeVal.ToString("X8"));
                Assert.AreEqual(writeVal, FX3.Transfer(writeVal), "ERROR: SPI loop back failed");
            }

            Console.WriteLine("Testing array based SPI transfers...");
            for(int writeSize = 1; writeSize < 10; writeSize++)
            {
                writeData.Clear();
                for(uint i = 0; i<writeSize; i++)
                {
                    writeData.Add(i);
                }
                for(uint numBuffers = 1; numBuffers < 10; numBuffers++)
                {
                    for (uint numCaptures = 1; numCaptures < 10; numCaptures++)
                    {
                        Console.WriteLine("Testing write data array " + writeSize.ToString() + " words long, with " + numCaptures.ToString() + " numcaptures and " + numBuffers.ToString() + " numbuffers");
                        readArray = FX3.TransferArray(writeData, numCaptures, numBuffers);

                        /* Size should be write data count * numbuffers * numCaptures */
                        Assert.AreEqual(writeSize * numBuffers * numCaptures, readArray.Count(), "ERROR: Invalid data size received");
                        int i = 0;
                        /* Check echo data */
                        for(int index = 0; index < readArray.Count(); index++)
                        {
                            i = index % writeSize;
                            Assert.AreEqual(writeData[i], readArray[i], "ERROR: Invalid SPI data at index " + index.ToString());
                        }
                    }
                }
            }
        }

        [Test]
        public void SpiParametersTest()
        {
            InitializeTestCase();
            Console.WriteLine("Starting SPI configurable parameters test...");

            TestSpiFunctionality();

            Console.WriteLine("Testing setting all SPI parameters");
            FX3.Cpha = true;
            Assert.AreEqual(true, FX3.Cpha, "ERROR: Cpha not applied");
            TestSpiFunctionality();
            FX3.Cpha = false;
            Assert.AreEqual(false, FX3.Cpha, "ERROR: Cpha not applied");
            TestSpiFunctionality();
            FX3.Cpol = true;
            Assert.AreEqual(true, FX3.Cpol, "ERROR: Cpol not applied");
            TestSpiFunctionality();
            FX3.Cpol = false;
            Assert.AreEqual(false, FX3.Cpol, "ERROR: Cpol not applied");
            TestSpiFunctionality();
            FX3.IsLSBFirst = true;
            Assert.AreEqual(true, FX3.IsLSBFirst, "ERROR: lsbfirst not applied");
            TestSpiFunctionality();
            FX3.IsLSBFirst = false;
            Assert.AreEqual(false, FX3.IsLSBFirst, "ERROR: lsbfirst not applied");
            TestSpiFunctionality();
            FX3.ChipSelectPolarity = true;
            Assert.AreEqual(true, FX3.ChipSelectPolarity, "ERROR: CS Polarity not applied");
            TestSpiFunctionality();
            FX3.ChipSelectPolarity = false;
            Assert.AreEqual(false, FX3.ChipSelectPolarity, "ERROR: CS Polarity not applied");
            TestSpiFunctionality();
            FX3.ChipSelectLagTime = SpiLagLeadTime.SPI_SSN_LAG_LEAD_ZERO_CLK;
            Assert.AreEqual(SpiLagLeadTime.SPI_SSN_LAG_LEAD_ZERO_CLK, FX3.ChipSelectLagTime, "ERROR: CS lag time not applied");
            TestSpiFunctionality();
            FX3.ChipSelectLagTime = SpiLagLeadTime.SPI_SSN_LAG_LEAD_ONE_HALF_CLK;
            Assert.AreEqual(SpiLagLeadTime.SPI_SSN_LAG_LEAD_ONE_HALF_CLK, FX3.ChipSelectLagTime, "ERROR: CS lag time not applied");
            TestSpiFunctionality();
            FX3.ChipSelectLagTime = SpiLagLeadTime.SPI_SSN_LAG_LEAD_ONE_CLK;
            Assert.AreEqual(SpiLagLeadTime.SPI_SSN_LAG_LEAD_ONE_CLK, FX3.ChipSelectLagTime, "ERROR: CS lag time not applied");
            TestSpiFunctionality();
            FX3.ChipSelectLagTime = SpiLagLeadTime.SPI_SSN_LAG_LEAD_HALF_CLK;
            Assert.AreEqual(SpiLagLeadTime.SPI_SSN_LAG_LEAD_HALF_CLK, FX3.ChipSelectLagTime, "ERROR: CS lag time not applied");
            TestSpiFunctionality();
            int exCount = 0;
            try
            {
                FX3.ChipSelectLeadTime = SpiLagLeadTime.SPI_SSN_LAG_LEAD_ZERO_CLK;
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
                exCount = 1;
            }
            Assert.AreEqual(1, exCount, "ERROR: Exception not thrown for invalid setting");
            Assert.AreNotEqual(SpiLagLeadTime.SPI_SSN_LAG_LEAD_ZERO_CLK, FX3.ChipSelectLeadTime, "ERROR: Invalid CS lead time applied");
            TestSpiFunctionality();
            FX3.ChipSelectLeadTime = SpiLagLeadTime.SPI_SSN_LAG_LEAD_ONE_HALF_CLK;
            Assert.AreEqual(SpiLagLeadTime.SPI_SSN_LAG_LEAD_ONE_HALF_CLK, FX3.ChipSelectLeadTime, "ERROR: CS lead time not applied");
            TestSpiFunctionality();
            FX3.ChipSelectLeadTime = SpiLagLeadTime.SPI_SSN_LAG_LEAD_ONE_CLK;
            Assert.AreEqual(SpiLagLeadTime.SPI_SSN_LAG_LEAD_ONE_CLK, FX3.ChipSelectLeadTime, "ERROR: CS lead time not applied");
            TestSpiFunctionality();
            FX3.ChipSelectLeadTime = SpiLagLeadTime.SPI_SSN_LAG_LEAD_HALF_CLK;
            Assert.AreEqual(SpiLagLeadTime.SPI_SSN_LAG_LEAD_HALF_CLK, FX3.ChipSelectLeadTime, "ERROR: CS lead time not applied");
            TestSpiFunctionality();

            Console.WriteLine("Verifying settings are maintained...");
            Assert.AreEqual(SpiLagLeadTime.SPI_SSN_LAG_LEAD_HALF_CLK, FX3.ChipSelectLeadTime, "ERROR: CS lead time not applied");
            Assert.AreEqual(SpiLagLeadTime.SPI_SSN_LAG_LEAD_HALF_CLK, FX3.ChipSelectLagTime, "ERROR: CS lag time not applied");
            Assert.AreEqual(false, FX3.ChipSelectPolarity, "ERROR: CS Polarity not applied");
            Assert.AreEqual(false, FX3.IsLSBFirst, "ERROR: lsbfirst not applied");
            Assert.AreEqual(false, FX3.Cpol, "ERROR: Cpol not applied");
            Assert.AreEqual(false, FX3.Cpha, "ERROR: Cpha not applied");
        }

        [Test]
        public void SclkFrequencyTest()
        {
            InitializeTestCase();
            Console.WriteLine("Starting SPI clock frequency test...");
            FX3.WordLength = 8;
            FX3.ChipSelectLeadTime = SpiLagLeadTime.SPI_SSN_LAG_LEAD_ONE_HALF_CLK;
            FX3.ChipSelectLagTime = SpiLagLeadTime.SPI_SSN_LAG_LEAD_ONE_HALF_CLK;

            for (int freq = 100000; freq <= 20000000; freq += 10000)
            {
                Console.WriteLine("Setting SCLK frequency to " + freq.ToString() + "Hz");
                FX3.SclkFrequency = freq;
                Assert.AreEqual(freq, FX3.SclkFrequency, "ERROR: FX3 frequency set failed");
                TestSpiFunctionality();
            }
        }

        [Test]
        public void BurstStreamCancelTest()
        {
            InitializeTestCase();
            Console.WriteLine("Starting burst stream cancel test...");

            long firstCount;

            FX3.BurstByteCount = 16;
            FX3.TriggerReg = new RegClass() { Address = 0, NumBytes = 2, Page = 0 };
            FX3.SetupBurstMode();
            FX3.DrActive = false;

            for (int trial = 0; trial < 5; trial++)
            {
                Console.WriteLine("Starting trial " + trial.ToString());
                /* Start stream */
                FX3.StartBurstStream(1000000, FX3.BurstMOSIData);
                firstCount = FX3.GetNumBuffersRead;
                System.Threading.Thread.Sleep(100);
                Assert.Greater(FX3.GetNumBuffersRead, firstCount, "ERROR: Expected to have read buffers");

                /* Cancel stream (stop stream) */
                FX3.StopStream();
                System.Threading.Thread.Sleep(20);

                /* Check SPI functionality */
                TestSpiFunctionality();

                /* Start stream */
                FX3.StartBurstStream(1000000, FX3.BurstMOSIData);
                firstCount = FX3.GetNumBuffersRead;
                System.Threading.Thread.Sleep(100);
                Assert.Greater(FX3.GetNumBuffersRead, firstCount, "ERROR: Expected to have read buffers");

                /* Cancel stream (cancel stream) */
                FX3.CancelStreamAsync();
                System.Threading.Thread.Sleep(20);

                /* Check SPI functionality */
                TestSpiFunctionality();
            }
        }

        [Test]
        public void TransferStreamCancelTest()
        {
            InitializeTestCase();
            Console.WriteLine("Starting transfer stream cancel test...");

            long firstCount;

            FX3.SensorType = DeviceType.AutomotiveSpi;
            FX3.PartType = DUTType.IMU;

            for (int trial = 0; trial < 5; trial++)
            {
                Console.WriteLine("Starting trial " + trial.ToString());
                /* Start stream */
                FX3.StartBufferedStream(new[] { 0U, 1U, 2U }, 1, 1000000, 10, null);
                firstCount = FX3.GetNumBuffersRead;
                System.Threading.Thread.Sleep(100);
                Assert.Greater(FX3.GetNumBuffersRead, firstCount, "ERROR: Expected to have read buffers");

                /* Cancel stream (stop stream) */
                FX3.StopStream();
                System.Threading.Thread.Sleep(20);

                /* Check SPI functionality */
                TestSpiFunctionality();

                /* Start stream */
                FX3.StartBufferedStream(new[] { 0U, 1U, 2U }, 1, 1000000, 10, null);
                firstCount = FX3.GetNumBuffersRead;
                System.Threading.Thread.Sleep(100);
                Assert.Greater(FX3.GetNumBuffersRead, firstCount, "ERROR: Expected to have read buffers");

                /* Cancel stream (cancel stream) */
                FX3.CancelStreamAsync();
                System.Threading.Thread.Sleep(20);

                /* Check SPI functionality */
                TestSpiFunctionality();
            }
        }

        [Test]
        public void GenericStreamCancelTest()
        {
            InitializeTestCase();
            Console.WriteLine("Starting generic stream cancel test...");

            long firstCount;

            FX3.SensorType = DeviceType.IMU;
            FX3.PartType = DUTType.IMU;

            for (int trial = 0; trial < 5; trial++)
            {
                Console.WriteLine("Starting trial " + trial.ToString());
                /* Start stream */
                FX3.StartBufferedStream(new[] { 0U, 1U, 2U }, 1, 1000000, 10, null);
                firstCount = FX3.GetNumBuffersRead;
                System.Threading.Thread.Sleep(100);
                Assert.Greater(FX3.GetNumBuffersRead, firstCount, "ERROR: Expected to have read buffers");

                /* Cancel stream (stop stream) */
                FX3.StopStream();
                System.Threading.Thread.Sleep(20);

                /* Check SPI functionality */
                TestSpiFunctionality();

                /* Start stream */
                FX3.StartBufferedStream(new[] { 0U, 1U, 2U }, 1, 1000000, 10, null);
                firstCount = FX3.GetNumBuffersRead;
                System.Threading.Thread.Sleep(100);
                Assert.Greater(FX3.GetNumBuffersRead, firstCount, "ERROR: Expected to have read buffers");

                /* Cancel stream (cancel stream) */
                FX3.CancelStreamAsync();
                System.Threading.Thread.Sleep(20);

                /* Check SPI functionality */
                TestSpiFunctionality();
            }

        }

        [Test]
        public void ADcmXLRealTimeStreamTest()
        {
            InitializeTestCase();
            Console.WriteLine("Starting ADcmXL data stream test...");

            double expectedTime;

            double realTime;

            double baseTime;

            uint numBuffers = 13600;

            Stopwatch timer = new Stopwatch();

            FX3.DrActive = true;
            FX3.SensorType = DeviceType.ADcmXL;
            FX3.PartType = DUTType.ADcmXL3021;
            FX3.DrPin = FX3.DIO3;
            /* Start 6.8KHz, 80% duty cycle DR signal on DIO4 */
            FX3.StartPWM(6800, 0.8, FX3.DIO4);

            Console.WriteLine("Measuring base stream time...");
            baseTime = 0;
            for(int trial = 0; trial < 5; trial++)
            {
                timer.Restart();
                FX3.StartRealTimeStreaming(1000);
                System.Threading.Thread.Sleep(5);
                FX3.WaitForStreamCompletion(1000);
                baseTime += (timer.ElapsedMilliseconds - (1000.0 / 6.8));
            }
            baseTime /= 5;
            Console.WriteLine("Base stream overhead time: " + baseTime.ToString() + "ms");

            for (int trial = 0; trial < 4; trial++)
            {
                Console.WriteLine("Starting trial " + trial.ToString());
                /* Start stream */
                timer.Restart();
                FX3.StartRealTimeStreaming(numBuffers);
                System.Threading.Thread.Sleep(100);
                FX3.WaitForStreamCompletion((int) (numBuffers / 6.0) + 1000);
                timer.Stop();
                Assert.AreEqual(FX3.GetNumBuffersRead, numBuffers, "ERROR: Invalid number of buffers read");
                realTime = timer.ElapsedMilliseconds;
                /* Take off a base time */
                realTime -= baseTime;

                expectedTime = numBuffers / 6.8;
                Console.WriteLine("Expected time: " + expectedTime.ToString() + "ms, Real time: " + realTime.ToString() + "ms");
                Assert.AreEqual(expectedTime, realTime, 0.01 * expectedTime, "ERROR: Invalid stream time");

                /* Check SPI functionality */
                FX3.WordLength = 16;
                TestSpiFunctionality();
            }

            Console.WriteLine("Verifying that when the DR edge is missed the stream waits until the following edge to start...");
            FX3.SclkFrequency = 8000000;
            timer.Restart();
            FX3.StartRealTimeStreaming(numBuffers);
            System.Threading.Thread.Sleep(100);
            FX3.WaitForStreamCompletion((int)(2 * numBuffers / 6.0) + 1000);
            timer.Stop();
            Assert.AreEqual(FX3.GetNumBuffersRead, numBuffers, "ERROR: Invalid number of buffers read");
            realTime = timer.ElapsedMilliseconds;
            /* Take off a base time */
            realTime -= baseTime;

            /* Twice the time because we read every other data ready */
            expectedTime = 2 * numBuffers / 6.8;
            Console.WriteLine("Expected time: " + expectedTime.ToString() + "ms, Real time: " + realTime.ToString() + "ms");
            Assert.AreEqual(expectedTime, realTime, 0.01 * expectedTime, "ERROR: Invalid stream time");
        }

        [Test]
        public void ADcmXLStreamCancelTest()
        {
            InitializeTestCase();
            Console.WriteLine("Starting ADcmXL stream cancel test...");

            long firstCount;

            FX3.DrActive = true;
            FX3.SensorType = DeviceType.ADcmXL;
            FX3.PartType = DUTType.ADcmXL3021;
            FX3.DrPin = FX3.DIO3;
            /* Start 6KHz DR signal on DIO4 */
            FX3.StartPWM(6000, 0.1, FX3.DIO4);

            for (int trial = 0; trial < 4; trial++)
            {
                Console.WriteLine("Starting trial " + trial.ToString());
                /* Start stream */
                FX3.StartRealTimeStreaming(1000000);
                firstCount = FX3.GetNumBuffersRead;
                System.Threading.Thread.Sleep(50);
                Assert.Greater(FX3.GetNumBuffersRead, firstCount, "ERROR: Expected to have read buffers");

                /* Cancel stream (stop stream) */
                FX3.StopStream();
                System.Threading.Thread.Sleep(50);

                /* Check SPI functionality */
                TestSpiFunctionality();

                /* Start stream */
                FX3.StartRealTimeStreaming(1000000);
                firstCount = FX3.GetNumBuffersRead;
                System.Threading.Thread.Sleep(50);
                Assert.Greater(FX3.GetNumBuffersRead, firstCount, "ERROR: Expected to have read buffers");

                /* Cancel stream (cancel stream) */
                FX3.CancelStreamAsync();
                System.Threading.Thread.Sleep(50);

                /* Check SPI functionality */
                TestSpiFunctionality();
            }
        }

        private void TestSpiFunctionality()
        {
            switch(FX3.WordLength)
            {
                case 8:
                    Assert.AreEqual(0x55, FX3.Transfer(0x55) & 0xFF, "ERROR: SPI data failed to echo back. SCLK Freq " + FX3.SclkFrequency.ToString() + "Hz");
                    Assert.AreEqual(0xAA, FX3.Transfer(0xAA) & 0xFF, "ERROR: SPI data failed to echo back. SCLK Freq " + FX3.SclkFrequency.ToString() + "Hz");
                    break;
                case 16:
                    Assert.AreEqual(0x5555, FX3.Transfer(0x5555) & 0xFFFF, "ERROR: SPI data failed to echo back. SCLK Freq " + FX3.SclkFrequency.ToString() + "Hz");
                    Assert.AreEqual(0xAAAA, FX3.Transfer(0xAAAA) & 0xFFFF, "ERROR: SPI data failed to echo back. SCLK Freq " + FX3.SclkFrequency.ToString() + "Hz");
                    break;
                case 32:
                default:
                    Assert.AreEqual(0x55555555, FX3.Transfer(0x55555555), "ERROR: SPI data failed to echo back. SCLK Freq " + FX3.SclkFrequency.ToString() + "Hz");
                    Assert.AreEqual(0xAAAAAAAA, FX3.Transfer(0xAAAAAAAA), "ERROR: SPI data failed to echo back. SCLK Freq " + FX3.SclkFrequency.ToString() + "Hz");
                    break;
            }
        }

    }
}
