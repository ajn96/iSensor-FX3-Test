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
    class I2CTests : FX3TestBase
    {

        [Test]
        public void I2CReadTest()
        {
            InitializeTestCase();
            Console.WriteLine("Starting I2C read test...");

        }

        [Test]
        public void I2CWriteTest()
        {
            InitializeTestCase();
            Console.WriteLine("Starting I2C write test...");
        }

        [Test]
        public void I2CStreamTest()
        {
            InitializeTestCase();
            Console.WriteLine("Starting I2C data stream test...");
        }

        [Test]
        public void I2CModeSwitchingTest()
        {
            InitializeTestCase();
            Console.WriteLine("Starting I2C mode switching stress test...");
        }
    }
}
