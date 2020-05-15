# Overview

This repository allows automated testing of the iSensor FX3 demonstration platform firmware and API on target hardware

FX3 API Repository: https://github.com/juchong/iSensor-FX3-API
FX3 Firmware Repository: https://github.com/juchong/iSensor-FX3-Firmware

## Hardware Setup

* Must have an iSensor FX3 board connected to USB
* Connected FX3 must have the SPI MISO pin connected to MOSI
* Connected FX3 must have the DIO1 pin connected to DIO2

## Software Setup

* Requires Microsoft Windows 7 or newer
* Must have MSBuild installed and added to the path
* Must have git installed and added to the path
* Must have iSensor FX3 Driver installed

## Test Flow

Running "build_script.bat" will trigger the build and test sequence:
* Get latest version of the FX3 API from GitHub
* Compile FX3 API from source
* Copy pre-built FX3 Firmware binaries from FX3 API repo - at some point will write a makefile for the FX3 Firmware to allow automated build
* Compile iSensor-FX3-Test program
* Run test program using NUnit 2.6.4, with the output piped to "test_result.txt". These tests are run on target hardware.
* Compiled resources and test results are pushed to remote 