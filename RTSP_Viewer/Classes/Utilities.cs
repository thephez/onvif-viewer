using System;
using System.Drawing;
using System.Text.RegularExpressions;

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

        public static string GetPtzCommandFromMouse(int mouseX, int mouseY, int width, int height)
        {
            int x = width / 2;
            int y = height / 2;

            int deltaX = mouseX - x;
            int deltaY = y - mouseY;

            float radius = (float)Math.Sqrt((deltaX * deltaX) + (deltaY * deltaY));
            double angle = Math.Atan2(deltaY, deltaX) * (180 / Math.PI);

            string function = "";
            if (angle >= -45 && angle < 45)
            {
                function = "Pan Right";
            }
            else if (angle >= 45 && angle < 135)
            {
                function = "Tilt Up";
            }
            else if (angle >= 135 || angle < -135)
            {
                function = "Pan Left";
            }
            else if (angle >= -135 && angle < -45)
            {
                function = "Tilt Down";
            }

            return function;
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
    }
}
