using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using FX3Api;
using System.IO;

namespace iSensor_FX3_Test
{
    class SpiTests : FX3TestBase
    {

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
            FX3.ChipSelectLeadTime = SpiLagLeadTime.SPI_SSN_LAG_LEAD_ZERO_CLK;
            Assert.AreEqual(SpiLagLeadTime.SPI_SSN_LAG_LEAD_ZERO_CLK, FX3.ChipSelectLeadTime, "ERROR: CS lead time not applied");
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
            for(int freq = 10000; freq < 40000000; freq += 10000)
            {
                Console.WriteLine("Setting SCLK frequency to " + freq.ToString() + "Hz");
                FX3.SclkFrequency = freq;
                Assert.AreEqual(freq, FX3.SclkFrequency, "ERROR: FX3 frequency set failed");
                TestSpiFunctionality();
            }
        }

        private void TestSpiFunctionality()
        {
            Assert.AreEqual(1, FX3.Transfer(1), "ERROR: SPI data failed to echo back");
        }

    }
}
