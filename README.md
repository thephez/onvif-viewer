 Features
  - HMI integration (via text file)
  - Runtime resizable
  - Run as full-screen app (runtime changeable)
  - Toggle views between grid and single full view
   - Select different grid item without leaving full view via status overlay
  - Shows preset buttons on viewer for PTZs
  - Dynamically reload while running (for changing # of views, etc.)
  
  - Onvif compatible
   - Tested with a variety of Bosch and Samsung cameras (including 360)
   - Supports authentication
    - Works for both the RTSP stream and Onvif commands
	- Querys camera time and calculates offset between camera and client time to make sure Onvif commands work (Onvif spec requires the timestamp in Onvif commands to be within 5 seconds of the camera time)
   - Querys camera stream URI info directly from each camera so no guessing what the correct RTSP path is
   - Supports PTZ commands (including preset)
   - Video played via VLC control so many features available (audio, etc.)
   - Multicast capable
   