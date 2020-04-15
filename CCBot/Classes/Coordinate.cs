using System;

namespace CCBot
{
    /*
     * Coordinate
     */

    public class Coordinate
    {
        public int X { get; set; }
        public int Y { get; set; }

        public Coordinate(int _x, int _y)
        {
            X = _x;
            Y = _y;
        }

        public Coordinate()
        {
        }
    }
}
