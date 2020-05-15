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
        }

        [Test]
        public void SpiParametersTest()
        {
            InitializeTestCase();
            Console.WriteLine("Starting SPI configurable parameters test...");
        }

    }
}
