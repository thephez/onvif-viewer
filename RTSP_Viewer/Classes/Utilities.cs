using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
