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
    class PinTests : FX3TestBase
    {
        [Test]
        public void SetReadPinTest()
        {
            InitializeTestCase();
            Console.WriteLine("Starting pin read/write test...");

            const int NUM_TRIALS = 100;

            Console.WriteLine("Testing " + NUM_TRIALS.ToString() + " pin set/read trials");
            for(int trial = 0; trial < NUM_TRIALS; trial++)
            {
                Console.WriteLine("Starting trial " + trial.ToString());
                FX3.SetPin(FX3.DIO1, 1);
                Assert.AreEqual(1, FX3.ReadPin(FX3.DIO2), "ERROR: Invalid pin value read on DIO2");
                FX3.SetPin(FX3.DIO1, 0);
                Assert.AreEqual(0, FX3.ReadPin(FX3.DIO2), "ERROR: Invalid pin value read on DIO2");

                FX3.SetPin(FX3.DIO2, 1);
                Assert.AreEqual(1, FX3.ReadPin(FX3.DIO1), "ERROR: Invalid pin value read on DIO1");
                FX3.SetPin(FX3.DIO2, 0);
                Assert.AreEqual(0, FX3.ReadPin(FX3.DIO1), "ERROR: Invalid pin value read on DIO1");
            }
        }

        [Test]
        public void PinFreqMeasurePWMTest()
        {
            InitializeTestCase();
            Console.WriteLine("Starting pin frequency measure test...");

            const double MIN_FREQ = 5;
            const double MAX_FREQ = 10000;

            double measuredFreq, expectedFreq;

            Console.WriteLine("Sweeping PWM duty cycle with fixed frequency...");
            expectedFreq = 4000;
            for (double dutyCycle = 0.02; dutyCycle < 0.98; dutyCycle += 0.01)
            {
                Console.WriteLine("Testing duty cycle of " + dutyCycle.ToString());
                FX3.StartPWM(4000, dutyCycle, FX3.DIO1);
                measuredFreq = FX3.MeasurePinFreq(FX3.DIO2, 1, 500, 5);
                Console.WriteLine("Pin freq measured on DIO2: " + measuredFreq.ToString() + "Hz");
                Assert.AreEqual(expectedFreq, measuredFreq, 0.02 * expectedFreq, "ERROR: Invalid freq read back on DIO2");
            }
            FX3.StopPWM(FX3.DIO1);

            Console.WriteLine("Sweeping freq range...");
            for (expectedFreq = MIN_FREQ; expectedFreq < MAX_FREQ; expectedFreq = expectedFreq * 1.05)
            {
                Console.WriteLine("Testing PWM freq of " + expectedFreq.ToString() + "Hz");
                FX3.StartPWM(expectedFreq, 0.5, FX3.DIO1);
                measuredFreq = FX3.MeasurePinFreq(FX3.DIO2, 1, 2000, 2);
                Console.WriteLine("Pin freq measured on DIO2: " + measuredFreq.ToString() + "Hz");
                Assert.AreEqual(expectedFreq, measuredFreq, 0.02 * expectedFreq, "ERROR: Invalid freq read back on DIO2");
                FX3.StopPWM(FX3.DIO1);
                FX3.StartPWM(expectedFreq, 0.5, FX3.DIO2);
                measuredFreq = FX3.MeasurePinFreq(FX3.DIO1, 1, 2000, 2);
                Console.WriteLine("Pin freq measured on DIO1: " + measuredFreq.ToString() + "Hz");
                Assert.AreEqual(expectedFreq, measuredFreq, 0.02 * expectedFreq, "ERROR: Invalid freq read back on DIO1");
                FX3.StopPWM(FX3.DIO2);
            }

            Console.WriteLine("Testing averaging...");
            expectedFreq = 4000;
            FX3.StartPWM(expectedFreq, 0.5, FX3.DIO1);
            for (ushort average = 1; average < 1000; average += 2)
            {
                Console.WriteLine("Testing pin freq measure with " + average.ToString() + " averages...");
                measuredFreq = FX3.MeasurePinFreq(FX3.DIO2, 1, 1000, average);
                Console.WriteLine("Pin freq measured on DIO2: " + measuredFreq.ToString() + "Hz");
                Assert.AreEqual(expectedFreq, measuredFreq, 0.02 * expectedFreq, "ERROR: Invalid freq read back on DIO2");
            }
        }

        [Test]
        public void PinFreqMeasureTimeoutTest()
        {
            InitializeTestCase();
            Console.WriteLine("Starting pin frequency measure timeout functionality test...");
        }

        [Test]
        public void ResistorConfigurationTest()
        {
            InitializeTestCase();
            Console.WriteLine("Starting GPIO resistor configuration test...");
        }

    }
}
