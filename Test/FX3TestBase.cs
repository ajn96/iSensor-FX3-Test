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
    public abstract class FX3TestBase
    {
        /* FX3 object */
        public FX3Connection FX3;

        [TestFixtureSetUp(), Timeout(5000)]
        public void TestFixtureSetup()
        {
            Console.WriteLine("Starting test fixture setup...");
            string exePath = System.AppDomain.CurrentDomain.BaseDirectory;
            Console.WriteLine("Tests executing from " + exePath);
            string resoucePath = Path.GetFullPath(Path.Combine(exePath, @"..\..\..\"));
            resoucePath = Path.Combine(resoucePath, "Resources");
            Assert.True(Directory.Exists(resoucePath), "ERROR: Resource path not found. Build process may have failed!");
            FX3 = new FX3Connection(resoucePath, resoucePath, resoucePath, DeviceType.IMU);
            Connect();
            Console.WriteLine("FX3 Connected! FX3 API Info: " + Environment.NewLine + FX3.GetFX3ApiInfo.ToString());
            Console.WriteLine("FX3 Board Info: " + Environment.NewLine + FX3.ActiveFX3.ToString());
            Console.WriteLine("Test fixture setup complete");
        }

        [TestFixtureTearDown()]
        public void Teardown()
        {
            FX3.Disconnect();
            Console.WriteLine("Test fixture tear down complete");
        }

        public void Connect()
        {
            /* Return if board already connected */
            if (FX3.ActiveFX3 != null)
                return;
            FX3.WaitForBoard(5);
            if (FX3.AvailableFX3s.Count > 0)
            {
                FX3.Connect(FX3.AvailableFX3s[0]);
            }
            else if (FX3.BusyFX3s.Count > 0)
            {
                FX3.ResetAllFX3s();
                Connect();
            }
            else
            {
                Assert.True(false, "ERROR: No FX3 board connected!");
            }
        }
    }
}
