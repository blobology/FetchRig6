# FetchRig6

This project is in very early development. The aim of this project is to stream, analyze, and encode (HW-accelerated) video data from (for now) two Flir Oryx 7.1 MP cameras at 100 Hz to run closed-loop behavioral neuroscience experiments with rats or mice in a large arena.

This is a C# Windows Forms Application. Video streams from both cameras are merged into a single FOV for display on the main UI thread. Each camera runs on a separate thread. Each camera passes images (either resized or full frame) to separate threads for further processing with EmguCV (C# wrapper for OpenCV). These processing threads pass raw and processed frames to another thread which merges the camera streams into a single image. This "merge" thread passes merged images back to the main UI thread for display. An XBox controller is used to issue commands from the user through concurrent queues to all threads.

The goal of this software architecture will be to allow flexibility in multi-threaded multi-camera applications for streaming, processing, and encoding video data. For example, you may wish to encode:
  - full resolution (here 3208 x 2200 pixels) video from all cameras at 20 Hz
  - resized video (e.g. 802 x 550 pixels) video from all cameras at full frame rate (here 100 Hz)
  - full resolution cropped ROIs (e.g. 600 x 600 pixels) around several moving targets (e.g. rats A and B, targets A and B)
  
This project will be developed such that 2D poses of objects and animals can be achieved in real time to facilitate experimental flow control and interaction with devices and multiple pick-and-place robots.

Acknowledgments:
Many thanks to these scientists for sharing code and experience to help with software development, hardware selection, and experimental design:
  - Andrew Bolton
  - Selmaan Chettih
  - Kyle Severson
  - Diego Aldarondo
  - Jason Keller
  - Jesse Marshall
  - Tim Dunn
  - Armin Bahl
  
