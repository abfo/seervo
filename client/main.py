from machine import Pin
import time
import network
import machine
from sonar import Sonar
import urequests
import sys
import gc
from motors import MotorController
from lights import Lights
from camera import ArduCAM

# Valid ranges
COLOR_MIN, COLOR_MAX = 0, 255
DURATION_MIN, DURATION_MAX = 0, 5
SPEED_MIN, SPEED_MAX = -1023, 1023
MOTOR_DURATION_MIN, MOTOR_DURATION_MAX = 0, 5000

def load_env(path='.env'):
    config = {}
    with open(path) as f:
        for line in f:
            line = line.strip()
            if line and not line.startswith('#') and '=' in line:
                key, val = line.split('=', 1)
                config[key.strip()] = val.strip()
    return config

def clamp(val, lo, hi):
    return max(lo, min(hi, val))

env = load_env()
motors = MotorController()
leds = Lights()
sensor = Sonar()

leds.set_all(30, 0, 0)

# connect to WiFi
wlan = network.WLAN()
wlan.active(False)
time.sleep(1)
leds.set_pixel(0, 0, 30, 0)

wlan.active(True)
if not wlan.isconnected():
    print('connecting to network...')
    wlan.connect(env['WIFI_SSID'], env['WIFI_PASS'])
    while not wlan.isconnected():
        machine.idle()

# WiFi connected    
leds.set_pixel(1, 0, 30, 0)

# connect to camera
cam = ArduCAM.create()
leds.set_all(30, 30, 30)

# control loop
while True:
    try:
        gc.collect()
        time.sleep(0.5)
        data = cam.capture()
        distance = round(sensor.distance_m(), 2)
        url = 'http://192.168.50.2:5090/next?distance={}'.format(distance)
        response = urequests.post(url,
                                  data=data,
                                  headers={'Content-Type': 'image/jpeg'})
        del data
        result = response.json()
        response.close()

        colors = [
            {
                'r': clamp(c['r'], COLOR_MIN, COLOR_MAX),
                'g': clamp(c['g'], COLOR_MIN, COLOR_MAX),
                'b': clamp(c['b'], COLOR_MIN, COLOR_MAX),
            }
            for c in result['colors']
        ]
        duration = clamp(result['duration'], DURATION_MIN, DURATION_MAX)
        left_speed = clamp(result['leftSpeed'], SPEED_MIN, SPEED_MAX)
        right_speed = clamp(result['rightSpeed'], SPEED_MIN, SPEED_MAX)
        motor_duration = clamp(result['motorDuration'], MOTOR_DURATION_MIN, MOTOR_DURATION_MAX)

        leds.update(colors)
        time.sleep(duration)

        if motor_duration > 0 and (left_speed != 0 or right_speed != 0):
            motors.drive(left_speed, right_speed, motor_duration)
        
    except Exception as e:
        try:
            sys.print_exception(e)
            leds.set_all(60, 0, 0)
            motors.stop()
            time.sleep(5)
        except:
            pass





