using log4net;
using RTSP_Viewer.OnvifPtzServiceReference;
using System;

namespace SDS.Video.Onvif
{

    public class OnvifPtz
    {
        private string User;
        private string Password;
        private PTZClient PtzClient;
        private RTSP_Viewer.OnvifMediaServiceReference.MediaClient MediaClient;
        private RTSP_Viewer.OnvifMediaServiceReference.Profile MediaProfile { get; set; }

        /// <summary>
        /// Set when a Pan/Tilt/Zoom command is issued.  Reset when a Stop command is issued.
        /// </summary>
        public bool PtzMoving { get; private set; } = false;
        private PTZPreset[] Presets;

        private static readonly ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Instantiates PTZ object with known media profile
        /// </summary>
        /// <param name="mediaUri">Camera's Media URI (from device client GetServices())</param>
        /// <param name="ptzUri">Camera's PTZ URI (from device client GetServices())</param>
        /// <param name="deviceTimeOffset">Difference between device time and client time (seconds)</param>
        /// <param name="mediaProfile">Media Profile to use</param>
        /// <param name="user">Username</param>
        /// <param name="password">Password</param>
        public OnvifPtz(string mediaUri, string ptzUri, double deviceTimeOffset, RTSP_Viewer.OnvifMediaServiceReference.Profile mediaProfile, string user, string password)
        {
            User = user;
            Password = password;

            if (string.IsNullOrEmpty(mediaUri) | string.IsNullOrEmpty(ptzUri))
                throw new Exception("Media and/or PTZ URI is empty or null.  PTZ object cannot be created");

            PtzClient = OnvifServices.GetOnvifPTZClient(ptzUri, deviceTimeOffset, User, Password);
            MediaProfile = mediaProfile;

            // Should be able to remove this once all instantiates go through this constructor (only used by GetMediaProfile - which is only necessary if a mediaProfile is not provided)
            MediaClient = OnvifServices.GetOnvifMediaClient(mediaUri, deviceTimeOffset, User, Password);
        }

        /// <summary>
        /// Gets the first media profile that contains a PTZConfiguration from the the MediaClient GetProfiles command
        /// </summary>
        /// <returns>Media profile with PTZConfiguration</returns>
        private RTSP_Viewer.OnvifMediaServiceReference.Profile GetMediaProfile()
        {
            if (MediaProfile != null)
            {
                return MediaProfile;
            }
            else
            {
                log.Warn(string.Format("PTZ Media profile not assigned.  Finding first available PTZ-enabled profile - THIS MAY CAUSE ISSUES (commands sent to wrong stream) AND NEEDS TO BE CHANGED"));
                // If no profile defined, take a guess and select the first available one - THIS NEEDS TO GO AWAY EVENTUALLY
                RTSP_Viewer.OnvifMediaServiceReference.Profile[] mediaProfiles = MediaClient.GetProfiles();

                foreach (RTSP_Viewer.OnvifMediaServiceReference.Profile p in mediaProfiles)
                {
                    if (p.PTZConfiguration != null)
                    {
                        // This should eliminate the redundant GetProfiles() / GetProfile() calls that were being done on every command
                        MediaProfile = MediaClient.GetProfile(p.token);
                        return MediaProfile; // MediaClient.GetProfile(p.token);
                    }
                }
            }

            throw new Exception("No media profiles containing a PTZConfiguration on this device");
        }

        /// <summary>
        /// Pan the camera (uses the media profile provided to the constructor)
        /// </summary>
        /// <param name="speed">Percent of max speed to move the camera (-1.00 to 1.00)</param>
        private void Pan(float speed)
        {
            RTSP_Viewer.OnvifMediaServiceReference.Profile mediaProfile = GetMediaProfile();
            PTZConfigurationOptions ptzConfigurationOptions = PtzClient.GetConfigurationOptions(mediaProfile.PTZConfiguration.token);

            PTZSpeed velocity = new PTZSpeed();
            velocity.PanTilt = new Vector2D() { x = speed * ptzConfigurationOptions.Spaces.ContinuousPanTiltVelocitySpace[0].XRange.Max, y = 0 };

            PtzClient.ContinuousMove(mediaProfile.token, velocity, null);
            PtzMoving = true;
        }

        /// <summary>
        /// Tilt the camera (uses the media profile provided to the constructor)
        /// </summary>
        /// <param name="speed">Percent of max speed to move the camera (-1.00 to 1.00)</param>
        private void Tilt(float speed)
        {
            RTSP_Viewer.OnvifMediaServiceReference.Profile mediaProfile = GetMediaProfile();
            PTZConfigurationOptions ptzConfigurationOptions = PtzClient.GetConfigurationOptions(mediaProfile.PTZConfiguration.token);

            PTZSpeed velocity = new PTZSpeed();
            velocity.PanTilt = new Vector2D() { x = 0, y = speed * ptzConfigurationOptions.Spaces.ContinuousPanTiltVelocitySpace[0].YRange.Max };

            PtzClient.ContinuousMove(mediaProfile.token, velocity, null);
            PtzMoving = true;
        }

        /// <summary>
        /// Zoom the camera (uses the media profile provided to the constructor)
        /// </summary>
        /// <param name="speed">Percent of max speed to move the camera (-100 to 100). Negative zooms out, Positive zooms in</param>
        public void Zoom(int speed)
        {
            if (speed > 100)
                speed = 100;

            if (speed < -100)
                speed = -100;

            log.Debug(string.Format("Zoom @ velocity of {0}", speed));

            RTSP_Viewer.OnvifMediaServiceReference.Profile mediaProfile = GetMediaProfile();
            PTZConfigurationOptions ptzConfigurationOptions = PtzClient.GetConfigurationOptions(mediaProfile.PTZConfiguration.token);
            
            PTZSpeed velocity = new PTZSpeed();
            velocity.Zoom = new Vector1D() { x = ((float)speed / 100) * ptzConfigurationOptions.Spaces.ContinuousZoomVelocitySpace[0].XRange.Max };

            string timeout = null;  //"PT5S";
            try
            {
                PtzClient.ContinuousMove(mediaProfile.token, velocity, timeout);
                PtzMoving = true;
            }
            catch (Exception ex)
            {
                log.Error(string.Format("Exception executing Zoom command. {0}", ex.Message));
            }
        }

        /// <summary>
        /// Combined Pan and Tilt of the camera (uses the media profile provided to the constructor)
        /// </summary>
        /// <param name="panSpeed">Percent of max speed to move the camera (-1.00 to 1.00)</param>
        /// <param name="tiltSpeed">Percent of max speed to move the camera (-1.00 to 1.00)</param>
        public void PanTilt(float panSpeed, float tiltSpeed)
        {
            RTSP_Viewer.OnvifMediaServiceReference.Profile mediaProfile = GetMediaProfile();
            PTZConfigurationOptions ptzConfigurationOptions = PtzClient.GetConfigurationOptions(mediaProfile.PTZConfiguration.token);

            PTZSpeed velocity = new PTZSpeed();
            velocity.PanTilt = new Vector2D()
            {
                x = panSpeed * ptzConfigurationOptions.Spaces.ContinuousPanTiltVelocitySpace[0].XRange.Max,
                y = tiltSpeed * ptzConfigurationOptions.Spaces.ContinuousPanTiltVelocitySpace[0].YRange.Max
            };

            try
            {
                PtzClient.ContinuousMove(mediaProfile.token, velocity, null);
                PtzMoving = true;
                log.Debug("Pan/Tilt Ptz command sent");
            }
            catch (Exception ex)
            {
                log.Error(string.Format("Exception executing Pan/Tilt command. {0}", ex.Message));
            }
        }

        /// <summary>
        /// Stop the camera (uses the media profile provided to the constructor).
        /// NOTE: may not work if not issued in conjunction with a move command
        /// </summary>
        public void Stop()
        {
            RTSP_Viewer.OnvifMediaServiceReference.Profile mediaProfile = GetMediaProfile();
            try
            {
                PtzClient.Stop(mediaProfile.token, true, true);
                PtzMoving = false;
                log.Debug("Ptz Stop command sent");
            }
            catch (Exception ex)
            {
                log.Error(string.Format("Exception executing Stop command. {0}", ex.Message));
            }
        }

        /// <summary>
        /// Move PTZ to provided preset number (uses the media profile provided to the constructor)
        /// </summary>
        /// <param name="presetNumber">Preset to use</param>
        public void ShowPreset(int presetNumber)
        {
            string presetToken = string.Empty;

            RTSP_Viewer.OnvifMediaServiceReference.Profile mediaProfile = GetMediaProfile();
            string profileToken = mediaProfile.token;

            // This could be used to cache presets to avoid unecessary HTTP requests
            // This could also cause an issue when presets are modified so it is not used currently
            //if (Presets == null)
            //    Presets = PtzClient.GetPresets(profileToken);

            Presets = PtzClient.GetPresets(profileToken);
            PTZPreset[] presets = Presets; // PtzClient.GetPresets(profileToken);
            if (presets.Length >= presetNumber)
            {
                presetToken = presets[presetNumber - 1].token;

                PTZSpeed velocity = new PTZSpeed();
                velocity.PanTilt = new Vector2D() { x = (float)-0.5, y = 0 }; ;

                try
                {
                    PtzClient.GotoPreset(profileToken, presetToken, velocity);
                }
                catch (Exception ex)
                {
                    log.Error(string.Format("Exception executing ShowPreset command. {0}", ex.Message));
                }
            }
            else
            {
                log.Warn(string.Format("Preset #{0} not defined for this camera [{1}]", presetNumber, PtzClient.Endpoint.Address.Uri.AbsoluteUri));
                throw new IndexOutOfRangeException(string.Format("Preset #{0} not defined for this camera [{1}]", presetNumber, PtzClient.Endpoint.Address.Uri.AbsoluteUri));
            }
        }

        public bool IsValidPresetToken(string profileToken, string presetToken)
        {
            Presets = PtzClient.GetPresets(profileToken);

            PTZPreset[] presets = Presets;
            foreach (PTZPreset p in presets)
            {
                if (p.token == presetToken)
                    return true;
            }

            return false;
        }

        public int GetPresetCount()
        {
            RTSP_Viewer.OnvifMediaServiceReference.Profile mediaProfile = GetMediaProfile();
            string profileToken = mediaProfile.token;

            // This could be used to cache presets to avoid unecessary HTTP requests
            // This could also cause an issue when presets are modified so it is not used currently
            //if (Presets == null)
            //    Presets = PtzClient.GetPresets(profileToken);

            Presets = PtzClient.GetPresets(profileToken);

            return Presets.Length;
        }

        /// <summary>
        /// Get the current Ptz location from the camera
        /// </summary>
        /// <returns></returns>
        public PTZStatus GetPtzLocation()
        {
            RTSP_Viewer.OnvifMediaServiceReference.Profile mediaProfile = GetMediaProfile();

            PTZStatus status = PtzClient.GetStatus(mediaProfile.token);
            return status;
        }

        // Shouldn't be necessary anymore since the constructor requires a PTZ uri 
        //public bool IsPtz()
        //{
        //    try
        //    {
        //        RTSP_Viewer.OnvifMediaServiceReference.Profile mediaProfile = GetMediaProfile();
        //        return true;
        //    }
        //    catch (Exception)
        //    {
        //        return false;
        //    }
        //}
    }
}
