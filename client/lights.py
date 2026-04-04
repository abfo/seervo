from machine import Pin
from neopixel import NeoPixel


class Lights:

    def __init__(self, pin=16, count=4):
        self._np = NeoPixel(Pin(pin, Pin.OUT), count)
        self._count = count

    def set_all(self, r, g, b):
        for i in range(self._count):
            self._np[i] = (r, g, b)
        self._np.write()

    def set_pixel(self, index, r, g, b):
        self._np[index] = (r, g, b)
        self._np.write()

    def update(self, colors):
        """Update pixels from a list of {r, g, b} dicts."""
        for i, color in enumerate(colors):
            self._np[i] = (color['r'], color['g'], color['b'])
        self._np.write()
