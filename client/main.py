from machine import Pin
from machine import SPI, I2C
from neopixel import NeoPixel
import time
import network
import machine
import urequests

def load_env(path='.env'):
    config = {}
    with open(path) as f:
        for line in f:
            line = line.strip()
            if line and not line.startswith('#') and '=' in line:
                key, val = line.split('=', 1)
                config[key.strip()] = val.strip()
    return config

env = load_env()

np = NeoPixel(Pin(16, Pin.OUT), 4)  # 4 onboard RGB LEDs on GPIO16

np[0] = (30, 0, 0)   
np[1] = (30, 0, 0)  
np[2] = (30, 0, 0)  
np[3] = (30, 0, 0)  
np.write()

wlan = network.WLAN()
wlan.active(False)
time.sleep(1)
np[0] = (0, 30, 0)   
np.write()

wlan.active(True)
if not wlan.isconnected():
    print('connecting to network...')
    wlan.connect(env['WIFI_SSID'], env['WIFI_PASS'])
    while not wlan.isconnected():
        machine.idle()
    print('network config:', wlan.ipconfig('addr4'))
    print('mac:', wlan.config('mac'))
    

np[1] = (0, 30, 0)   
np.write()

# --- Camera setup ---
from camera import ArduCAM, RES_800x600

cs = Pin(5, Pin.OUT)
spi = SPI(2, baudrate=4000000, polarity=0, phase=0,
          sck=Pin(18), mosi=Pin(23), miso=Pin(19))
i2c = I2C(0, sda=Pin(21), scl=Pin(22), freq=100000)

cam = ArduCAM(spi, cs, i2c, resolution=RES_800x600)

if cam.test_spi():
    print('SPI: OK')
    np[2] = (0, 30, 0)
else:
    print('SPI: FAIL')
    np[2] = (30, 0, 0)
np.write()

if cam.test_i2c():
    print('I2C: OV2640 found')
    np[3] = (0, 30, 0)
else:
    print('I2C: OV2640 not found')
    np[3] = (30, 0, 0)
np.write()

# Initialize sensor and take a photo
cam.init()
print('Camera initialized')


data = cam.capture()
print(f'Captured: {len(data)} bytes')

response = urequests.post('http://192.168.50.2:5090/next',
                          data=data,
                          headers={'Content-Type': 'image/jpeg'})
result = response.json()
response.close()
print(result)

for i, color in enumerate(result['colors']):
    np[i] = (color['r'], color['g'], color['b'])
np.write()
time.sleep(result['duration'])



