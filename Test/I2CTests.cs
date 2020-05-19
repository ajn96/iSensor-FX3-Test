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
