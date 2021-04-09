using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using FX3Api;
using System.IO;
using System.Reflection;

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
            FX3.WaitForBoard(5);
            if (FX3.AvailableFX3s.Count > 0)
            {
                FX3.Connect(FX3.AvailableFX3s[0]);
            }
            else if (FX3.BusyFX3s.Count > 0)
            {
                FX3.ResetAllFX3s();
                FX3.WaitForBoard(5);
                Connect();
            }
            else
            {
                Assert.True(false, "ERROR: No FX3 board connected!");
            }
        }

        [SetUp()]
        public void InitializeTestCase()
        {
            Connect();
            foreach (PropertyInfo prop in FX3.GetType().GetProperties())
            {
                if(prop.PropertyType == typeof(AdisApi.IPinObject))
                {
                    /* Disable PWM if running */
                    if (FX3.isPWMPin((AdisApi.IPinObject)prop.GetValue(FX3)))
                        FX3.StopPWM((AdisApi.IPinObject)prop.GetValue(FX3));
                    /* Disable resistor */
                    FX3.SetPinResistorSetting((AdisApi.IPinObject)prop.GetValue(FX3), FX3PinResistorSetting.None);
                }
            }
            FX3.RestoreHardwareSpi();
            FX3.SclkFrequency = 1000000;
            FX3.StallTime = 5;
            FX3.WordLength = 16;
            FX3.DrActive = false;
            FX3.DrPin = FX3.DIO1;
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
