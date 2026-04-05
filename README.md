# Seervo

An LLM powered ESP32 robot. This is an experiment in embodied AI, with a vision model connected to a camera and deciding what to do next. 

![Seervo](/seervo-small.jpg)

## Architecture 

For this project I wanted a server to make it easy to see what the robot is doing and to iterate on the prompts and control logic. The server folder has a very simple ASP.NET Core web API. The robot posts an image to the API and gets JSON with the next action to take. I run this in VS Code on Windows but it should run anywhere .NET 10 Core is supported. 

The client folder contains [MicroPython](https://micropython.org/) code to run the robot. There are simple drivers for the camera, motors and LEDs and a control loop that calls the API and executes the JSON instructions. 

Lastly the chassis folder contains OpenSCAD and STL files to 3D print a basic chassis that holds the battery, controller, motors and camera. 

## Getting Started

Follow the [notes](client/notes.md) in the client folder to flash MicroPython to the ESP32 and assemble the components. Use mpremote to copy python files to the board, and a .env file containing your WiFi SSID and password, i.e.:

```
mprempte cp camera.py :camera.py
```

3D print the chassis. This has alignment holes for the motors and I use a glue gun to hold these in place. I use a cable tie to attach the camera to the mast, and another to wrap around the chassis and secure the battery. 

Start the server up and switch on the robot! You'll need an OpenAI API key as an environment variable. 

## Tips and Ideas

I'm planning to add an ultrasonic range detector to help the robot figure out how much space it has to work with. The LLM sometimes gets nervous about objects that are quite far away. 

My version is supposed to avoid pets and entertain humans. Edit Instructions.md to give the robot a different mission. 

The server currently uses GPT 5.4 with medium reasoning. I find this to be a good balance of speed and intelligence. It does chew up some money if you leave it running though. Drop down to a cheaper model and/or a lower camera resolution if you need this to be cheaper! 

It would be good to have an option for the robot to call ChatGPT directly, removing the server dependency. 

I'm also considering an image embedding model in parallel to the LLM call, so the robot can more easily detect if it's looking at the same scene and might be stuck. 
