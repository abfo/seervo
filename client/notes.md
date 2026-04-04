# Seervo Client

## Getting Started

I'm using this [Maker-ESP32](https://github.com/nulllaborg/maker-esp32/tree/master?tab=readme-ov-file) board and working with micropython from VS Code. 

Create a virtual environment and install esptool and mpremote:

```python
pip install esptool mpremote
```

Grab the firmware ([this seems to work](https://micropython.org/resources/firmware/ESP32_GENERIC-20251209-v1.27.0.bin)) and install (board mounted as COM5 for me, there is a driver listed but hope not to have to install...):

```python
python -m esptool --port COM5 erase_flash
python -m esptool --port COM5 --baud 460800 write_flash 0x1000 ESP32_GENERIC-20251209-v1.27.0.bin
```

Run a quick test script to blink the LEDs:

```python
from machine import Pin
from neopixel import NeoPixel
import time

np = NeoPixel(Pin(16, Pin.OUT), 4)  # 4 onboard RGB LEDs on GPIO16

while True:
    for i in range(4):
        np[i] = (30, 0, 0)   # red
    np.write()
    time.sleep_ms(300)

    for i in range(4):
        np[i] = (0, 0, 0)    # off
    np.write()
    time.sleep_ms(300)
```

```python
mpremote run main.py
```

LEDs should now be blinking. 

## Camera

For this project I got the Arduino Mega256 SPI camera board (B0067). This wires into SPI/I2C and 3.3v power. There isn't a driver for the ESP32, camera.py handles photo capture. 

## Motors

The Maker-ESP32 has four motor drivers. I'm using [these](https://www.amazon.com/dp/B0D8H89XDY?ref_=ppx_hzsearch_conn_dt_b_fed_asin_title_1) generic motors and wheels. 

## Power

I'm using [this](https://www.amazon.com/dp/B077Y9HNTF?th=1) battery and [these](https://www.amazon.com/dp/B0CPHJMW47) connectors for the charger and to plug into the Maker board. 

## Chassis 

See the Chassis folder for an OpenSCAD design and STL file. 








