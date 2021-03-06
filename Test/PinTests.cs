﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using FX3Api;
using System.IO;
using System.Diagnostics;
using System.Reflection;
using AdisApi;

namespace iSensor_FX3_Test
{
    class PinTests : FX3TestBase
    {

        private void SetPinResistorSetting(FX3PinResistorSetting setting)
        {
            List<IPinObject> pins = new List<IPinObject>();
            FX3.BitBangSpiConfig = new BitBangSpiConfig(true);
            pins.Add(FX3.DIO1);
            pins.Add(FX3.DIO2);
            pins.Add(FX3.DIO3);
            pins.Add(FX3.DIO4);
            pins.Add(FX3.FX3_GPIO1);
            pins.Add(FX3.FX3_GPIO2);
            pins.Add(FX3.FX3_GPIO3);
            pins.Add(FX3.FX3_GPIO4);
            pins.Add(FX3.BitBangSpiConfig.CS);
            pins.Add(FX3.BitBangSpiConfig.SCLK);
            pins.Add(FX3.BitBangSpiConfig.MISO);
            pins.Add(FX3.BitBangSpiConfig.MOSI);
            pins.Add(FX3.ResetPin);
            //uart
            pins.Add(new FX3PinObject(48));
            foreach(IPinObject pin in  pins)
            {
                /* Read each pin to set as input */
                FX3.ReadPin(pin);
                FX3.SetPinResistorSetting(pin, setting);
            }
        }

        [Test]
        public void DrPinTest()
        {
            Console.WriteLine("Starting data ready pin test...");

            Assert.AreEqual(FX3.DIO1.pinConfig, FX3.DrPin.pinConfig, "ERROR: Expected default DR pin to be DIO1");

            IPinObject drPin = FX3.DrPin;
            Assert.AreEqual(drPin.pinConfig, FX3.ReadyPin.pinConfig);

            FX3.ReadyPin = new FX3Api.FX3PinObject(20);
            Assert.AreEqual(20, FX3.ReadyPin.pinConfig);
            Assert.AreEqual(20, FX3.DrPin.pinConfig);

            FX3.DrPin = drPin;
            Assert.AreEqual(drPin.pinConfig, FX3.ReadyPin.pinConfig);
            Assert.AreEqual(drPin.pinConfig, FX3.DrPin.pinConfig);

            FX3.SensorType = DeviceType.ADcmXL;
            FX3.PartType = DUTType.ADcmXL3021;

            Assert.AreEqual(FX3.DIO2.pinConfig, FX3.DrPin.pinConfig, "ERROR: Expected default DR pin to be DIO2 for ADcmXL");

            FX3.SensorType = DeviceType.IMU;
            FX3.PartType = DUTType.IMU;

            Assert.AreEqual(FX3.DIO1.pinConfig, FX3.DrPin.pinConfig, "ERROR: Expected default DR pin to be DIO1 for IMU");
        }

        [Test]
        public void LoopPinTest()
        {
            Stopwatch timer = new Stopwatch();
            long expectedTime;

            if(FX3.ActiveFX3.BoardType < FX3BoardType.iSensorFX3Board_C)
            {
                Console.WriteLine("The connected boards do not have loop back pins");
                return;
            }

            for(double freq = 100; freq < 2000; freq+= 100)
            {
                Console.WriteLine("Setting " + freq.ToString() + "Hz PWM on loop pin 1");
                FX3.StartPWM(freq, 0.5, FX3.FX3_LOOPBACK1);
                Assert.AreEqual(freq, FX3.MeasurePinFreq(FX3.FX3_LOOPBACK2, 1, 1000, 100), 0.02 * freq, "ERROR: Invalid value measured on loop pin 2");
                FX3.StopPWM(FX3.FX3_LOOPBACK1);
                Console.WriteLine("Setting " + freq.ToString() + "Hz PWM on loop pin 2");
                FX3.StartPWM(freq, 0.5, FX3.FX3_LOOPBACK2);
                Assert.AreEqual(freq, FX3.MeasurePinFreq(FX3.FX3_LOOPBACK1, 1, 1000, 100), 0.02 * freq, "ERROR: Invalid value measured on loop pin 1");
                FX3.StopPWM(FX3.FX3_LOOPBACK2);
            }

            Console.WriteLine("Testing SPI triggering with loop back pins...");
            FX3.StartPWM(100, 0.5, FX3.FX3_LOOPBACK1);
            FX3.DrPin = FX3.FX3_LOOPBACK2;
            FX3.DrActive = true;
            expectedTime = 1000 * 1000 / 100;

            timer.Start();
            FX3.TransferArray(new uint[1], 1, 1000);
            timer.Stop();
            Console.WriteLine("Elapsed stream time " + timer.ElapsedMilliseconds.ToString() + "ms");
            Assert.AreEqual(expectedTime, timer.ElapsedMilliseconds, 0.05 * expectedTime, "ERROR: Invalid stream time");
        }


        [Test]
        [Explicit]
        public void LEDSpinnerTest()
        {
            long spinPeriod;
            int num, lastNum;
            bool rising = false;
            Stopwatch timer = new Stopwatch();

            if (FX3.ActiveFX3.BoardType < FX3BoardType.iSensorFX3Board_C)
            {
                Console.WriteLine("The connected boards do not have loop back pins");
                return;
            }

            //DQ13, CS, DIO4/3/2/1, 5V, 3.3V
            num = 0;
            lastNum = 0;
            spinPeriod = 1000;
            while(true)
            {
                DeactivateLED(lastNum);
                ActivateLED(num);
                System.Threading.Thread.Sleep((int) spinPeriod / 8);
                lastNum = num;
                num++;
                if (num > 7) num = 0;
                if(rising)
                {
                    spinPeriod += 1;
                    if (spinPeriod >= 400) rising = false;
                }
                else
                {
                    spinPeriod -= 1;
                    if (spinPeriod <= 200) rising = true;
                }
            }

        }

        private void ActivateLED(int index)
        {
            switch(index)
            {
                case 0:
                    FX3.SetPin(new FX3Api.FX3PinObject(13), 0);
                    break;
                case 1:
                    FX3.SetPin(new FX3Api.BitBangSpiConfig(true).CS, 0);
                    break;
                case 2:
                    FX3.SetPin(FX3.DIO4, 0);
                    break;
                case 3:
                    FX3.SetPin(FX3.DIO3, 0);
                    break;
                case 4:
                    FX3.SetPin(FX3.DIO2, 0);
                    break;
                case 5:
                    FX3.SetPin(FX3.DIO1, 0);
                    break;
                case 6:
                    FX3.DutSupplyMode = DutVoltage.On5_0Volts;
                    break;
                case 7:
                    FX3.DutSupplyMode = DutVoltage.On3_3Volts;
                    break;
            }
        }

        private void DeactivateLED(int index)
        {
            switch (index)
            {
                case 0:
                    FX3.SetPin(new FX3Api.FX3PinObject(13), 1);
                    break;
                case 1:
                    FX3.SetPin(new FX3Api.BitBangSpiConfig(true).CS, 1);
                    break;
                case 2:
                    FX3.SetPin(FX3.DIO4, 1);
                    break;
                case 3:
                    FX3.SetPin(FX3.DIO3, 1);
                    break;
                case 4:
                    FX3.SetPin(FX3.DIO2, 1);
                    break;
                case 5:
                    FX3.SetPin(FX3.DIO1, 1);
                    break;
                case 6:
                case 7:
                    FX3.DutSupplyMode = DutVoltage.Off;
                    break;
            }
        }

        [Test]
        public void SetReadPinTest()
        {
            Console.WriteLine("Starting pin read/write test...");

            const int NUM_TRIALS = 100;

            Console.WriteLine("Testing " + NUM_TRIALS.ToString() + " pin set/read trials");
            for(int trial = 0; trial < NUM_TRIALS; trial++)
            {
                Console.WriteLine("Starting trial " + trial.ToString());
                FX3.SetPin(FX3.DIO3, 1);
                Assert.AreEqual(1, FX3.ReadPin(FX3.DIO4), "ERROR: Invalid pin value read on DIO4");
                FX3.SetPin(FX3.DIO3, 0);
                Assert.AreEqual(0, FX3.ReadPin(FX3.DIO4), "ERROR: Invalid pin value read on DIO4");

                FX3.SetPin(FX3.DIO4, 1);
                Assert.AreEqual(1, FX3.ReadPin(FX3.DIO3), "ERROR: Invalid pin value read on DIO3");
                FX3.SetPin(FX3.DIO4, 0);
                Assert.AreEqual(0, FX3.ReadPin(FX3.DIO3), "ERROR: Invalid pin value read on DIO3");
            }
        }

        [Test]
        public void MeasureBusyPulseTest()
        {
            double period;

            double pwmPeriod = 1000;

            double measuredPeriodPin, measuredPeriodSpi;

            List<byte> SpiData = new List<byte>();
            SpiData.Add(0);
            SpiData.Add(0);

            /* Test small period measurements (0.5us - 500us) */
            for (period = 0.5; period < 500; period += 0.5) 
            {
                FX3.StartPWM(1000, (pwmPeriod - period) / pwmPeriod, FX3.DIO4);
                measuredPeriodPin = 1000 * FX3.MeasureBusyPulse(FX3.DIO2, 1, 0, FX3.DIO3, 0, 100);
                measuredPeriodSpi = 1000 * FX3.MeasureBusyPulse(SpiData.ToArray(), FX3.DIO3, 0, 100);
                Console.WriteLine("Negative PWM Period: " + period.ToString() + "us.\t Error: " + (period - measuredPeriodPin).ToString() + "us");
                Console.WriteLine("Measured period: " + measuredPeriodPin.ToString() + "us");
                /* Assert with 0.2us margin of error */
                Assert.AreEqual(period, measuredPeriodSpi, 0.2, "ERROR: Invalid period measured");
                Assert.AreEqual(period, measuredPeriodSpi, 0.2, "ERROR: Invalid period measured");

                /* Measure positive period */
                FX3.StartPWM(1000,  period / pwmPeriod, FX3.DIO4);
                measuredPeriodPin = 1000 * FX3.MeasureBusyPulse(FX3.DIO2, 1, 0, FX3.DIO3, 1, 100);
                measuredPeriodSpi = 1000 * FX3.MeasureBusyPulse(SpiData.ToArray(), FX3.DIO3, 1, 100);
                Console.WriteLine("Positive PWM Period: " + period.ToString() + "us.\t Error: " + (period - measuredPeriodPin).ToString() + "us");
                Console.WriteLine("Measured period: " + measuredPeriodPin.ToString() + "us");
                /* Assert with 0.2us margin of error */
                Assert.AreEqual(period, measuredPeriodSpi, 0.2, "ERROR: Invalid period measured");
                Assert.AreEqual(period, measuredPeriodSpi, 0.2, "ERROR: Invalid period measured");
            }

            /* Test large period measurements (2ms - 50ms) */
            pwmPeriod = 100;
            for (period = 2; period < 50; period += 2)
            {
                FX3.StartPWM(10, (pwmPeriod - period) / pwmPeriod, FX3.DIO4);
                measuredPeriodPin = FX3.MeasureBusyPulse(FX3.DIO2, 1, 0, FX3.DIO3, 0, 200);
                measuredPeriodSpi = FX3.MeasureBusyPulse(SpiData.ToArray(), FX3.DIO3, 0, 200);
                Console.WriteLine("Negative PWM Period: " + period.ToString() + "ms.\t Error: " + (period - measuredPeriodPin).ToString() + "ms");
                /* Assert with 1% margin of error */
                Assert.AreEqual(period, measuredPeriodSpi, period * 0.1, "ERROR: Invalid period measured");
                Assert.AreEqual(period, measuredPeriodSpi, period * 0.1, "ERROR: Invalid period measured");

                /* Measure positive period */
                FX3.StartPWM(10, period / pwmPeriod, FX3.DIO4);
                measuredPeriodPin = FX3.MeasureBusyPulse(FX3.DIO2, 1, 0, FX3.DIO3, 1, 200);
                measuredPeriodSpi = FX3.MeasureBusyPulse(SpiData.ToArray(), FX3.DIO3, 1, 200);
                Console.WriteLine("Positive PWM Period: " + period.ToString() + "ms.\t Error: " + (period - measuredPeriodPin).ToString() + "ms");
                /* Assert with 1% margin of error */
                Assert.AreEqual(period, measuredPeriodSpi, period * 0.1, "ERROR: Invalid period measured");
                Assert.AreEqual(period, measuredPeriodSpi, period * 0.1, "ERROR: Invalid period measured");
            }

            /* Test very large period measurements (1 sec) */
            Console.WriteLine("Testing 1 second period signal measurements...");
            period = 1000;
            FX3.StartPWM(0.5, 0.5, FX3.DIO4);
            measuredPeriodPin = FX3.MeasureBusyPulse(FX3.DIO2, 1, 0, FX3.DIO3, 0, 5000);
            measuredPeriodSpi = FX3.MeasureBusyPulse(SpiData.ToArray(), FX3.DIO3, 0, 5000);
            Assert.AreEqual(period, measuredPeriodSpi, period * 0.1, "ERROR: Invalid period measured");
            Assert.AreEqual(period, measuredPeriodSpi, period * 0.1, "ERROR: Invalid period measured");
            measuredPeriodPin = FX3.MeasureBusyPulse(FX3.DIO2, 1, 0, FX3.DIO3, 1, 5000);
            measuredPeriodSpi = FX3.MeasureBusyPulse(SpiData.ToArray(), FX3.DIO3, 1, 5000);
            Assert.AreEqual(period, measuredPeriodSpi, period * 0.1, "ERROR: Invalid period measured");
            Assert.AreEqual(period, measuredPeriodSpi, period * 0.1, "ERROR: Invalid period measured");
        }

        [Test]
        public void PinFreqMeasureTest()
        {
            Console.WriteLine("Starting pin frequency measure test...");

            const double MIN_FREQ = 5;
            const double MAX_FREQ = 10000;

            double measuredFreq, expectedFreq;

            Console.WriteLine("Sweeping PWM duty cycle with fixed frequency...");
            expectedFreq = 4000;
            for (double dutyCycle = 0.02; dutyCycle < 0.98; dutyCycle += 0.01)
            {
                Console.WriteLine("Testing duty cycle of " + dutyCycle.ToString());
                FX3.StartPWM(4000, dutyCycle, FX3.DIO3);
                measuredFreq = FX3.MeasurePinFreq(FX3.DIO4, 1, 500, 5);
                Console.WriteLine("Pin freq measured on DIO2: " + measuredFreq.ToString() + "Hz");
                Assert.AreEqual(expectedFreq, measuredFreq, 0.02 * expectedFreq, "ERROR: Invalid freq read back on DIO2");
            }
            FX3.StopPWM(FX3.DIO3);

            Console.WriteLine("Sweeping freq range...");
            for (expectedFreq = MIN_FREQ; expectedFreq < MAX_FREQ; expectedFreq = expectedFreq * 1.08)
            {
                Console.WriteLine("Testing PWM freq of " + expectedFreq.ToString() + "Hz");
                FX3.StartPWM(expectedFreq, 0.5, FX3.DIO3);
                measuredFreq = FX3.MeasurePinFreq(FX3.DIO4, 1, 2000, 3);
                Console.WriteLine("Pin freq measured on DIO4: " + measuredFreq.ToString() + "Hz");
                Assert.AreEqual(expectedFreq, measuredFreq, 0.02 * expectedFreq, "ERROR: Invalid freq read back on DIO4");
                FX3.StopPWM(FX3.DIO3);
                FX3.StartPWM(expectedFreq, 0.5, FX3.DIO4);
                measuredFreq = FX3.MeasurePinFreq(FX3.DIO3, 1, 2000, 3);
                Console.WriteLine("Pin freq measured on DIO3: " + measuredFreq.ToString() + "Hz");
                Assert.AreEqual(expectedFreq, measuredFreq, 0.02 * expectedFreq, "ERROR: Invalid freq read back on DIO3");
                FX3.StopPWM(FX3.DIO4);
            }

            Console.WriteLine("Testing averaging...");
            expectedFreq = 4000;
            FX3.StartPWM(expectedFreq, 0.5, FX3.DIO3);
            for (ushort average = 1; average < 1000; average += 2)
            {
                Console.WriteLine("Testing pin freq measure with " + average.ToString() + " averages...");
                measuredFreq = FX3.MeasurePinFreq(FX3.DIO4, 1, 1000, average);
                Console.WriteLine("Pin freq measured on DIO4: " + measuredFreq.ToString() + "Hz");
                Assert.AreEqual(expectedFreq, measuredFreq, 0.02 * expectedFreq, "ERROR: Invalid freq read back on DIO4");
            }
        }

        [Test]
        public void PWMPinInfoTest()
        {
            Console.WriteLine("Starting pin PWM info test...");

            int exCount;
            PinPWMInfo info;

            Console.WriteLine("Testing default PWM pin info for each pin...");
            foreach (PropertyInfo prop in FX3.GetType().GetProperties())
            {
                if (prop.PropertyType == typeof(AdisApi.IPinObject))
                {
                    Assert.AreEqual(0, FX3.GetPinPWMInfo((AdisApi.IPinObject)prop.GetValue(FX3)).IdealFrequency, "ERROR: Expected PWM pins to be disabled for all pins initially");
                }
            }
            Console.WriteLine("Testing PWM pin info for each pin...");
            foreach (PropertyInfo prop in FX3.GetType().GetProperties())
            {
                if (prop.PropertyType == typeof(AdisApi.IPinObject))
                {
                    if(((AdisApi.IPinObject)prop.GetValue(FX3)).pinConfig % 8 != 0)
                    {
                        FX3.StartPWM(2000, 0.5, (AdisApi.IPinObject)prop.GetValue(FX3));
                        info = FX3.GetPinPWMInfo((AdisApi.IPinObject)prop.GetValue(FX3));
                        Assert.AreEqual(((AdisApi.IPinObject)prop.GetValue(FX3)).pinConfig, info.FX3GPIONumber, "ERROR: Invalid GPIO Number");
                        Assert.AreEqual(info.FX3GPIONumber % 8, info.FX3TimerBlock, "ERROR: Invalid FX3 timer block");
                        Assert.AreEqual(2000, info.IdealFrequency, "ERROR: Invalid frequency");
                        Assert.AreEqual(0.5, info.IdealDutyCycle, "ERROR: Invalid duty cycle");
                        FX3.StopPWM((AdisApi.IPinObject)prop.GetValue(FX3));
                    }
                    else
                    {
                        exCount = 0;
                        try
                        {
                            FX3.StartPWM(2000, 0.5, (AdisApi.IPinObject)prop.GetValue(FX3));
                        }
                        catch(Exception e)
                        {
                            Console.WriteLine(e.Message);
                            exCount = 1;
                        }
                        Assert.AreEqual(1, exCount, "ERROR: Expected exception to be thrown for pins which cannot run PWM");
                    }
                }
            }
            Console.WriteLine("Sweeping PWM freq...");
            {
                for(double freq = 0.5; freq < 100000; freq = freq * 1.1)
                {
                    FX3.StartPWM(freq, 0.5, FX3.DIO1);
                    Assert.AreEqual(freq, FX3.GetPinPWMInfo(FX3.DIO1).IdealFrequency, "ERROR: Invalid IdealFrequency");
                    Assert.AreEqual(freq, FX3.GetPinPWMInfo(FX3.DIO1).RealFrequency, 0.001 * freq, "ERROR: Invalid RealFrequency");
                }
            }

            Console.WriteLine("Sweeping PWM duty cycle at 1KHz...");
            for(double dutyCycle = 0.01; dutyCycle < 1.0; dutyCycle += 0.01)
            {
                FX3.StartPWM(1000, dutyCycle, FX3.DIO1);
                Assert.AreEqual(dutyCycle, FX3.GetPinPWMInfo(FX3.DIO1).IdealDutyCycle, "ERROR: Invalid ideal duty cycle");
                Assert.AreEqual(dutyCycle, FX3.GetPinPWMInfo(FX3.DIO1).RealDutyCycle, 0.001 * dutyCycle, "ERROR: Invalid real duty cycle");
            }
        }

        [Test]
        public void PWMGenerationTest()
        {
            Console.WriteLine("Starting PWM generation test...");
            double posWidth, negWidth;
            double measuredFreq, measuredDutyCycle;

            for(double freq = 20; freq < 40000; freq *= 1.2)
            {
                for(double dutyCycle = 0.02; dutyCycle <= 0.98; dutyCycle += 0.02)
                {
                    Console.WriteLine("Starting PWM with freq " + freq.ToString("f2") + "Hz, " + dutyCycle.ToString("f2") + " duty cycle...");
                    FX3.StartPWM(freq, dutyCycle, FX3.DIO3);
                    posWidth = FX3.MeasureBusyPulse(FX3.DIO2, 1, 0, FX3.DIO4, 1, 1000);
                    negWidth = FX3.MeasureBusyPulse(FX3.DIO2, 1, 0, FX3.DIO4, 0, 1000);
                    measuredFreq = 1000 / (posWidth + negWidth);
                    measuredDutyCycle = posWidth / (posWidth + negWidth);
                    Console.WriteLine("Measured freq: " + measuredFreq.ToString("f2") + "Hz, measured duty cycle: " + measuredDutyCycle.ToString("f2"));
                    Assert.AreEqual(freq, measuredFreq, 0.01 * freq, "ERROR: Invalid freq measured");
                    Assert.AreEqual(dutyCycle, measuredDutyCycle, 0.01, "ERROR: Invalid duty cycle measured");
                }
            }
        }

        [Test]
        public void PinFunctionTimeoutTest()
        {
            Console.WriteLine("Starting pin functions timeout functionality test...");

            /* Timer for measuring elapsed time */
            Stopwatch timer = new Stopwatch();

            for(uint timeout = 100; timeout <= 800; timeout += 100)
            {
                Console.WriteLine("Testing timeout of " + timeout.ToString() + "ms...");
                FX3.SetPin(FX3.DIO4, 1);

                /* Pulse wait */
                timer.Restart();
                FX3.PulseWait(FX3.DIO3, 0, 0, timeout);
                timer.Stop();
                Console.WriteLine("Pulse wait time: " + timer.ElapsedMilliseconds.ToString() + "ms");
                Assert.GreaterOrEqual(timer.ElapsedMilliseconds, timeout, "ERROR: Function returned in less than timeout period");
                Assert.LessOrEqual(timer.ElapsedMilliseconds, timeout + 100, "ERROR: Function returned in over 100ms more than timeout period");
                CheckFirmwareResponsiveness();

                /* Measure pin freq */
                timer.Restart();
                Assert.AreEqual(double.PositiveInfinity, FX3.MeasurePinFreq(FX3.DIO4, 0, timeout, 1), "ERROR: Invalid pin freq. Expected timeout");
                timer.Stop();
                Console.WriteLine("Measure pin freq time: " + timer.ElapsedMilliseconds.ToString() + "ms");
                Assert.GreaterOrEqual(timer.ElapsedMilliseconds, timeout, "ERROR: Function returned in less than timeout period");
                Assert.LessOrEqual(timer.ElapsedMilliseconds, timeout + 100, "ERROR: Function returned in over 100ms more than timeout period");
                CheckFirmwareResponsiveness();

                /* Measure pin delay */
                timer.Restart();
                FX3.MeasurePinDelay(FX3.DIO4, 0, FX3.DIO1, timeout);
                timer.Stop();
                Console.WriteLine("Measure pin delay time: " + timer.ElapsedMilliseconds.ToString() + "ms");
                Assert.GreaterOrEqual(timer.ElapsedMilliseconds, timeout, "ERROR: Function returned in less than timeout period");
                Assert.LessOrEqual(timer.ElapsedMilliseconds, timeout + 100, "ERROR: Function returned in over 100ms more than timeout period");
                CheckFirmwareResponsiveness();

                /* Measure busy pulse */
                timer.Restart();
                Assert.AreEqual(double.PositiveInfinity, FX3.MeasureBusyPulse(FX3.DIO4, 1, 1, FX3.DIO1, 0, timeout), "ERROR: Expected measure busy pulse to return timeout");
                timer.Stop();
                Console.WriteLine("Measure busy pulse time: " + timer.ElapsedMilliseconds.ToString() + "ms");
                Assert.GreaterOrEqual(timer.ElapsedMilliseconds, timeout, "ERROR: Function returned in less than timeout period");
                Assert.LessOrEqual(timer.ElapsedMilliseconds, timeout + 100, "ERROR: Function returned in over 100ms more than timeout period");
                CheckFirmwareResponsiveness();
            }
        }

        [Test]
        public void GetTimerValueTest()
        {
            Console.WriteLine("Starting timer value test...");

            uint time0, time1;

            double scaledTime;

            time0 = FX3.GetTimerValue();
            System.Threading.Thread.Sleep(3000);
            time1 = FX3.GetTimerValue();
            if(time1 < time0)
            {
                /* Timer value just rolled over */
                time0 = time1;
                time1 = FX3.GetTimerValue();
            }
            scaledTime = (time1 - time0) / 10078;
            Assert.AreEqual(3000, scaledTime, 0.02 * 3000, "ERROR: Invalid timestamp");

            /* Timer for measuring elapsed time */
            Stopwatch timer = new Stopwatch();

            time0 = (uint) FX3.GetTimerValue() / 10078;
            timer.Start();
            while(timer.ElapsedMilliseconds < 5000)
            {
                time1 = FX3.GetTimerValue();
                Console.WriteLine("FX3 Time: " + (time1 / 10078).ToString() + "ms, system time: " + (timer.ElapsedMilliseconds + time0).ToString() + "ms");
                Assert.AreEqual((time1 / 10078), (timer.ElapsedMilliseconds + time0), (timer.ElapsedMilliseconds + time0) * 0.02, "ERROR: Invalid FX3 time");
                System.Threading.Thread.Sleep(100);
            }
        }

        [Test]
        public void ResistorConfigurationTest()
        {
            Console.WriteLine("Starting GPIO resistor configuration test...");

            for(int trial = 0; trial < 10; trial++)
            {
                Console.WriteLine("Enabling internal pull up on all user accessible GPIO...");
                SetPinResistorSetting(FX3PinResistorSetting.PullUp);
                System.Threading.Thread.Sleep(50);

                Console.WriteLine("Checking input stage values...");
                Assert.AreEqual(1, FX3.ReadPin(FX3.DIO1), "ERROR: Expected pin to be high");
                Assert.AreEqual(1, FX3.ReadPin(FX3.DIO2), "ERROR: Expected pin to be high");
                Assert.AreEqual(1, FX3.ReadPin(FX3.DIO3), "ERROR: Expected pin to be high");
                Assert.AreEqual(1, FX3.ReadPin(FX3.DIO4), "ERROR: Expected pin to be high");
                Assert.AreEqual(1, FX3.ReadPin(FX3.FX3_GPIO1), "ERROR: Expected pin to be high");
                Assert.AreEqual(1, FX3.ReadPin(FX3.FX3_GPIO2), "ERROR: Expected pin to be high");
                Assert.AreEqual(1, FX3.ReadPin(FX3.FX3_GPIO3), "ERROR: Expected pin to be high");
                Assert.AreEqual(1, FX3.ReadPin(FX3.FX3_GPIO4), "ERROR: Expected pin to be high");

                Console.WriteLine("Enabling internal pull down on all user accessible GPIO...");
                SetPinResistorSetting(FX3PinResistorSetting.PullDown);
                System.Threading.Thread.Sleep(50);

                Console.WriteLine("Checking input stage values...");
                Assert.AreEqual(0, FX3.ReadPin(FX3.DIO1), "ERROR: Expected pin to be low");
                Assert.AreEqual(0, FX3.ReadPin(FX3.DIO2), "ERROR: Expected pin to be low");
                Assert.AreEqual(0, FX3.ReadPin(FX3.DIO3), "ERROR: Expected pin to be low");
                Assert.AreEqual(0, FX3.ReadPin(FX3.DIO4), "ERROR: Expected pin to be low");
                Assert.AreEqual(0, FX3.ReadPin(FX3.FX3_GPIO1), "ERROR: Expected pin to be low");
                Assert.AreEqual(0, FX3.ReadPin(FX3.FX3_GPIO2), "ERROR: Expected pin to be low");
                Assert.AreEqual(0, FX3.ReadPin(FX3.FX3_GPIO3), "ERROR: Expected pin to be low");
                Assert.AreEqual(0, FX3.ReadPin(FX3.FX3_GPIO4), "ERROR: Expected pin to be low");

                Console.WriteLine("Disabling resistors...");
                SetPinResistorSetting(FX3PinResistorSetting.None);
                System.Threading.Thread.Sleep(50);
            }

        }
    }
}
