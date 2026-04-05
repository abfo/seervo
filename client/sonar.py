from machine import Pin, time_pulse_us
import time


class Sonar:
    """HC-SR04 ultrasonic distance sensor driver."""

    SOUND_SPEED_M_PER_US = 0.000343

    def __init__(self, trigger_pin=25, echo_pin=26, timeout_us=30000):
        self._trigger = Pin(trigger_pin, Pin.OUT)
        self._echo = Pin(echo_pin, Pin.IN)
        self._timeout = timeout_us
        self._trigger.value(0)

    def distance_m(self):
        """Measure distance in meters. Returns -1 if out of range."""
        self._trigger.value(0)
        time.sleep_us(5)
        self._trigger.value(1)
        time.sleep_us(10)
        self._trigger.value(0)

        duration = time_pulse_us(self._echo, 1, self._timeout)
        if duration < 0:
            return -1

        return (duration * self.SOUND_SPEED_M_PER_US) / 2
