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

        public string ResourcePath;

        [TestFixtureSetUp(), Timeout(5000)]
        public void TestFixtureSetup()
        {
            Console.WriteLine("Starting test fixture setup...");
            string exePath = System.AppDomain.CurrentDomain.BaseDirectory;
            Console.WriteLine("Tests executing from " + exePath);
            ResourcePath = Path.GetFullPath(Path.Combine(exePath, @"..\..\..\"));
            ResourcePath = Path.Combine(ResourcePath, "Resources");
            Assert.True(Directory.Exists(ResourcePath), "ERROR: Resource path not found. Build process may have failed!");
            FX3 = new FX3Connection(ResourcePath, ResourcePath, ResourcePath, DeviceType.IMU);
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
            FX3.WaitForBoard(10);
            if (FX3.AvailableFX3s.Count > 0)
            {
                FX3.Connect(FX3.AvailableFX3s[0]);
            }
            else if (FX3.BusyFX3s.Count > 0)
            {
                FX3.Connect(FX3.BusyFX3s[0]);
            }
            else
            {
                Assert.True(false, "ERROR: No FX3 board connected!");
            }
        }

        public void InitializeTestCase()
        {
            Connect();
            FX3.StopPWM(FX3.DIO1);
            FX3.StopPWM(FX3.DIO2);
            FX3.SclkFrequency = 4000000;
            FX3.DrActive = false;
            FX3.SetPinResistorSetting(FX3.DIO1, FX3PinResistorSetting.None);
            FX3.SetPinResistorSetting(FX3.DIO2, FX3PinResistorSetting.None);
            FX3.SetPinResistorSetting(FX3.DIO3, FX3PinResistorSetting.None);
            FX3.SetPinResistorSetting(FX3.DIO4, FX3PinResistorSetting.None);
            FX3.SetPinResistorSetting(FX3.FX3_GPIO1, FX3PinResistorSetting.None);
            FX3.SetPinResistorSetting(FX3.FX3_GPIO2, FX3PinResistorSetting.None);
            FX3.SetPinResistorSetting(FX3.FX3_GPIO3, FX3PinResistorSetting.None);
            FX3.SetPinResistorSetting(FX3.FX3_GPIO4, FX3PinResistorSetting.None);
        }

        public void CheckFirmwareResponsiveness()
        {
            uint time0, time1;
            time0 = FX3.GetTimerValue();
            time1 = FX3.GetTimerValue();
            Assert.AreNotEqual(time0, time1, "ERROR: Received two identical back-to-back timestamp values");
        }
    }
}
