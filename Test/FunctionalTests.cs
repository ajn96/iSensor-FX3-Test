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
            Connect();
        }

        [Test]
        public void ImagePathsTest()
        {
            Connect();
        }

        [Test]
        public void BootStatusTest()
        {
            Connect();
        }

        [Test]
        public void FirmwareVersionTest()
        {
            Connect();
        }
    }
}
