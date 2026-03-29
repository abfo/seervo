from machine import Pin, PWM
import time


class MotorController:
    """
    Tank-drive controller for Maker-ESP32 with 4 DC motors.
    Left:  M1 (GPIO 27, 13), M3 (GPIO 17, 12)
    Right: M2 (GPIO 4, 2),   M4 (GPIO 14, 15)
    """

    def __init__(self, freq=1000):
        self._m1a = PWM(Pin(27), freq=freq, duty=0)
        self._m1b = PWM(Pin(13), freq=freq, duty=0)
        self._m2a = PWM(Pin(4),  freq=freq, duty=0)
        self._m2b = PWM(Pin(2),  freq=freq, duty=0)
        self._m3a = PWM(Pin(17), freq=freq, duty=0)
        self._m3b = PWM(Pin(12), freq=freq, duty=0)
        self._m4a = PWM(Pin(14), freq=freq, duty=0)
        self._m4b = PWM(Pin(15), freq=freq, duty=0)

    def _set_side(self, motors, speed):
        """Set a pair of motors. Positive = forward, negative = backward."""
        forward = speed >= 0
        duty = min(abs(speed), 1023)
        for ma, mb in motors:
            if forward:
                ma.duty(duty)
                mb.duty(0)
            else:
                ma.duty(0)
                mb.duty(duty)

    def _left_motors(self):
        return [(self._m1a, self._m1b), (self._m3a, self._m3b)]

    def _right_motors(self):
        return [(self._m2a, self._m2b), (self._m4a, self._m4b)]

    def drive(self, left_speed, right_speed, duration_ms):
        """
        Drive motors for duration_ms.
        Speeds range from -1023 (full reverse) to 1023 (full forward).
        """
        self._set_side(self._left_motors(), left_speed)
        self._set_side(self._right_motors(), right_speed)
        time.sleep_ms(duration_ms)
        self.stop()

    def stop(self):
        """Stop all motors."""
        for ma, mb in self._left_motors() + self._right_motors():
            ma.duty(0)
            mb.duty(0)
