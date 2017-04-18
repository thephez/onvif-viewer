using System;
using System.Drawing;
using System.Text.RegularExpressions;
using SDS.Video.Onvif;
using System.Windows.Forms;

namespace RTSP_Viewer.Classes
{
    class Utilities
    {

        /// <summary>
        /// Returns x,y coordinates for evenly distributing a quantiy in a given size (height/width).
        /// Forces quantiy to numbers that provide an integer square root (i.e. 1, 4, 9, 15, etc.)
        /// </summary>
        /// <param name="quantity">Number of items</param>
        /// <param name="width">Width to distribute over</param>
        /// <param name="height">Height to distribute over</param>
        /// <returns>Array of point locations</returns>
        public static Point[] CalculatePointLocations(int quantity, int width, int height)
        {
            Point[] displayPoint = new Point[quantity];

            // Make sure an even grid (2x2, 3x3, etc.) is provided by rounding down the square root
            int dim = (int)Math.Round(Math.Sqrt(quantity));
            width = width / dim;
            height = height / dim;

            for (int j = 0; j < dim; j++)
            {
                for (int i = 0; i < dim; i++)
                {
                    displayPoint[dim * j + i] = new Point(width * i, height * j);
                }
            }

            return displayPoint;
        }

        /// <summary>
        /// Provides the size to make items in order to evenly distribute in a given space (height/width).
        /// </summary>
        /// <param name="quantity">Number of items</param>
        /// <param name="width">Width to distribute over</param>
        /// <param name="height">Height to distribute over</param>
        /// <returns>Size of items to use</returns>
        public static Size CalculateItemSizes(int quantity, int width, int height, int padding)
        {
            // Make sure an even grid (2x2, 3x3, etc.) is provided by rounding down the square root
            int dim = (int)Math.Round(Math.Sqrt(quantity));
            width = width / dim;
            height = height / dim;

            return new Size(width - padding, height - padding);
        }

        /// <summary>
        /// Gets the that cursor points the approximate direction of PTZ motion
        /// </summary>
        /// <param name="angle">The angle of the mouse from the center of the screen (based on the center being x=0, y=0)</param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public static Cursor GetPtzCursor(double angle)
        {
            if (angle >= -22.5 && angle < 22.5)
                return Cursors.PanEast;
            else if (angle >= 22.5 && angle < 67.5)
                return Cursors.PanNE;
            else if (angle >= 67.5 && angle < 112.5)
                return Cursors.PanNorth;
            else if (angle >= 112.5 && angle < 157.5)
                return Cursors.PanNW;

            else if (angle >= 157.5 || angle < -157.5)
                return Cursors.PanWest;
            else if (angle >= -157.5 && angle < -112.5)
                return Cursors.PanSW;
            else if (angle >= -112.5 && angle < -67.5)
                return Cursors.PanSouth;
            else if (angle >= -67.5 && angle < -22.5)
                return Cursors.PanSE;
            else
                return Cursors.Default;
        }

        /// <summary>
        /// Check if a string contains an IP address
        /// </summary>
        /// <param name="ipString">String to check for an IP Address</param>
        /// <returns>The first IP address found</returns>
        public static string GetIpAddressFromString(string ipString)
        {
            string ValidIpAddressRegex = @"((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)";
            Regex ip = new Regex(ValidIpAddressRegex);
            MatchCollection ipAddr = ip.Matches(ipString);
            if (ipAddr.Count > 0)
                return ipAddr[0].ToString();
            else
                throw new Exception(string.Format("No IP address found in provided string ({0})", ipString));

        }

        /// <summary>
        /// Inserts username/password into a URI after the '://' (i.e. rtsp://192.168.1.1/stream -> rtsp://user:password@192.168.1.1/stream)
        /// Returns original URI if the user field is an empty string
        /// </summary>
        /// <param name="uri">URI to add credentials to</param>
        /// <param name="user">Username</param>
        /// <param name="password">Password</param>
        /// <returns>Uri with login info included</returns>
        public static Uri InsertUriCredentials(Uri uri, string user, string password)
        {
            if (uri != null)
            {
                UriBuilder u = new UriBuilder(uri);
                u.UserName = user;
                u.Password = password;
                return u.Uri;
            }

            return uri;
        }
    }
}
