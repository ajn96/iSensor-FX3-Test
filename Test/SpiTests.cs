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

namespace iSensor_FX3_Test
{
    class SpiTests : FX3TestBase
    {

        [Test]
        public void IRegInterfaceTest()
        {
            InitializeTestCase();
            Console.WriteLine("Starting IRegInterface test...");
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

                /* Override DIO1/2 pins */
                FX3.BitBangSpiConfig.MOSI = (FX3PinObject) FX3.DIO1;
                FX3.BitBangSpiConfig.MISO = (FX3PinObject) FX3.DIO2;
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

            /* 900KHz SCLK */
            FX3.SetBitBangSpiFreq(900000);

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
            baseTime -= (4000000.0 / 900000) * numReads;
            Console.WriteLine("Base bitbang SPI time: " + baseTime.ToString() + "ms");

            for (uint freq = 75000; freq <= 850000; freq += 25000)
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
            FX3.DrPin = FX3.DIO1;
            FX3.StartPWM(100, 0.5, FX3.DIO2);

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
            for(int bit = 0; bit < 8; bit++)
            {
                Assert.AreEqual(1U << bit, FX3.Transfer(1U << bit), "ERROR: SPI loop back failed");
            }
            /* Bits outside of word length should not be echo'd */
            for (int bit = 8; bit < 32; bit++)
            {
                Assert.AreEqual(0, FX3.Transfer(1U << bit), "ERROR: SPI loop back failed");
            }

            /* Set word length of 16 */
            Console.WriteLine("Testing word length of 16 bits...");
            FX3.WordLength = 16;
            for (int bit = 0; bit < 16; bit++)
            {
                Assert.AreEqual(1U << bit, FX3.Transfer(1U << bit), "ERROR: SPI loop back failed");
            }
            /* Bits outside of word length should not be echo'd */
            for (int bit = 16; bit < 32; bit++)
            {
                Assert.AreEqual(0, FX3.Transfer(1U << bit), "ERROR: SPI loop back failed");
            }

            /* Set word length of 24 */
            Console.WriteLine("Testing word length of 24 bits...");
            FX3.WordLength = 24;
            for (int bit = 0; bit < 24; bit++)
            {
                Assert.AreEqual(1U << bit, FX3.Transfer(1U << bit), "ERROR: SPI loop back failed");
            }
            /* Bits outside of word length should not be echo'd */
            for (int bit = 24; bit < 32; bit++)
            {
                Assert.AreEqual(0, FX3.Transfer(1U << bit), "ERROR: SPI loop back failed");
            }

            /* Set word length of 32 */
            Console.WriteLine("Testing word length of 32 bits...");
            FX3.WordLength = 32;
            for (int bit = 0; bit < 32; bit++)
            {
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

            FX3.BurstByteCount = 16;
            FX3.TriggerReg = new RegClass() { Address = 0, NumBytes = 2, Page = 0 };
            FX3.SetupBurstMode();
            FX3.DrActive = false;

            for (int trial = 0; trial < 5; trial++)
            {
                Console.WriteLine("Starting trial " + trial.ToString());
                /* Start stream */
                FX3.StartBurstStream(1000000, FX3.BurstMOSIData);
                System.Threading.Thread.Sleep(100);
                Assert.Greater(FX3.GetNumBuffersRead, 0, "ERROR: Expected to have read buffers");

                /* Cancel stream (stop stream) */
                FX3.StopStream();

                /* Check SPI functionality */
                TestSpiFunctionality();

                /* Start stream */
                FX3.StartBurstStream(1000000, FX3.BurstMOSIData);
                System.Threading.Thread.Sleep(100);
                Assert.Greater(FX3.GetNumBuffersRead, 0, "ERROR: Expected to have read buffers");

                /* Cancel stream (cancel stream) */
                FX3.CancelStreamAsync();

                /* Check SPI functionality */
                TestSpiFunctionality();
            }
        }

        [Test]
        public void TransferStreamCancelTest()
        {
            InitializeTestCase();
            Console.WriteLine("Starting transfer stream cancel test...");

            FX3.SensorType = DeviceType.AutomotiveSpi;
            FX3.PartType = DUTType.IMU;

            for (int trial = 0; trial < 5; trial++)
            {
                Console.WriteLine("Starting trial " + trial.ToString());
                /* Start stream */
                FX3.StartBufferedStream(new[] { 0U, 1U, 2U }, 1, 1000000, 10, null);
                System.Threading.Thread.Sleep(100);
                Assert.Greater(FX3.GetNumBuffersRead, 0, "ERROR: Expected to have read buffers");

                /* Cancel stream (stop stream) */
                FX3.StopStream();

                /* Check SPI functionality */
                TestSpiFunctionality();

                /* Start stream */
                FX3.StartBufferedStream(new[] { 0U, 1U, 2U }, 1, 1000000, 10, null);
                System.Threading.Thread.Sleep(100);
                Assert.Greater(FX3.GetNumBuffersRead, 0, "ERROR: Expected to have read buffers");

                /* Cancel stream (cancel stream) */
                FX3.CancelStreamAsync();

                /* Check SPI functionality */
                TestSpiFunctionality();
            }
        }

        [Test]
        public void GenericStreamCancelTest()
        {
            InitializeTestCase();
            Console.WriteLine("Starting generic stream cancel test...");

            FX3.SensorType = DeviceType.IMU;
            FX3.PartType = DUTType.IMU;

            for (int trial = 0; trial < 5; trial++)
            {
                Console.WriteLine("Starting trial " + trial.ToString());
                /* Start stream */
                FX3.StartBufferedStream(new[] { 0U, 1U, 2U }, 1, 1000000, 10, null);
                System.Threading.Thread.Sleep(100);
                Assert.Greater(FX3.GetNumBuffersRead, 0, "ERROR: Expected to have read buffers");

                /* Cancel stream (stop stream) */
                FX3.StopStream();
                System.Threading.Thread.Sleep(100);

                /* Check SPI functionality */
                TestSpiFunctionality();

                /* Start stream */
                FX3.StartBufferedStream(new[] { 0U, 1U, 2U }, 1, 1000000, 10, null);
                System.Threading.Thread.Sleep(100);
                Assert.Greater(FX3.GetNumBuffersRead, 0, "ERROR: Expected to have read buffers");

                /* Cancel stream (cancel stream) */
                FX3.CancelStreamAsync();
                System.Threading.Thread.Sleep(100);

                /* Check SPI functionality */
                TestSpiFunctionality();
            }

        }

        [Test]
        public void ADcmXLStreamCancelTest()
        {
            InitializeTestCase();
            Console.WriteLine("Starting ADcmXL stream cancel test...");

            FX3.DrActive = true;
            FX3.SensorType = DeviceType.ADcmXL;
            FX3.PartType = DUTType.ADcmXL3021;
            /* Start 6KHz DR signal on DIO1 */
            FX3.StartPWM(6000, 0.1, FX3.DIO1);

            for (int trial = 0; trial < 5; trial++)
            {
                Console.WriteLine("Starting trial " + trial.ToString());
                /* Start stream */
                FX3.StartRealTimeStreaming(1000000);
                System.Threading.Thread.Sleep(100);
                Assert.Greater(FX3.GetNumBuffersRead, 0, "ERROR: Expected to have read buffers");

                /* Cancel stream (stop stream) */
                FX3.StopStream();

                /* Check SPI functionality */
                TestSpiFunctionality();

                /* Start stream */
                FX3.StartRealTimeStreaming(1000000);
                System.Threading.Thread.Sleep(100);
                Assert.Greater(FX3.GetNumBuffersRead, 0, "ERROR: Expected to have read buffers");

                /* Cancel stream (cancel stream) */
                FX3.CancelStreamAsync();

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
