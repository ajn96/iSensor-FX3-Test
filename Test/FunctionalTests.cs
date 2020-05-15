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
    class FunctionalTests : FX3TestBase
    {
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
            Console.WriteLine("Starting firmware image path validation test...");
        }

        [Test]
        public void BootStatusTest()
        {
            InitializeTestCase();
            Console.WriteLine("Starting boot status retrieval test...");
        }

        [Test]
        public void FirmwareVersionTest()
        {
            InitializeTestCase();
            Console.WriteLine("Starting firmware version test...");
        }
    }
}
