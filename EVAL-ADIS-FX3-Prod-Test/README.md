# EVAL-ADIS-FX3 Production Test Application

This application performs first time programming and a basic functional test of the EVAL-ADIS-FX3 IMU evaluation board from Analog Devices.

![Running Application](https://raw.githubusercontent.com/ajn96/iSensor-FX3-Test/master/EVAL-ADIS-FX3-Prod-Test/app_image.JPG)

## Hardware Requirements

In order to operate correctly, this application must be used in conjunction with a EVAL-ADIS-FX3 test mating pod. The mating pod plugs into the two FX3 headers and shorts each pair of digital I/O pins together (including all SPI pins and UART Tx) to enable testing for any shorts/opens. The following table shows the pin connections used to enable testing.

| Pin 0     | Pin 1                |
| --------- | -------------------- |
| DIO1      | SPI Clock (SCLK)     |
| DIO2      | SPI Chip Select (CS) |
| DIO3      | DIO4                 |
| Reset     | UART Tx              |
| FX3_GPIO1 | FX3_GPIO2            |
| FX3_GPIO3 | FX3_GPIO4            |

## Test Sequence

The test application currently performs the following steps:

- Initial bootloader loading to NVM (if none loaded). This step will detect many potential gross failures in the board including:

- - Errors in NVM
  - Errors in USB connection
  - Any errors with the FX3 processor or supply /clock/passive circuitry for processor

- Loading application code

- NVM error log initialization (clears log)

- Reboots board and verifies it correctly re-enumerates with bootloader (bootloader loaded to NVM correctly)

- Loading application code again and checking that no initialization errors have been logged by the application code

- For each GPIO pair, drive one GPIO high/low repeatedly and verify the other shorted GPIO reads the correct logic level. This verifies the connection from the FX3 processor to the connector headers for all digital I/O.

- For each GPIO pair, apply a 1MHz clock signal to one pin and verify the signal frequency and duty cycle using a timer configured for input capture on the other pin. This will do a better job catching marginal failures or resistive opens, which might not fail at lower frequencies. 

- For each GPIO pair, set one pin logic high and verify all other GPIO can be brought to logic low via a weak pull down resistor. Repeat for opposite polarity. This test will catch any potential (unexpected) shorts between connector pins.

