namespace CtrTex
{
    internal struct Point
    {
        public int X, Y;

        public Point()
        {
            X = 0;
            Y = 0;
        }

        public Point(int x, int y)
        {
            X = x;
            Y = y;
        }
    }

    internal struct Color
    {
        public byte R, G, B, A;

        public Color()
        {
            R = 0;
            G = 0;
            B = 0;
            A = 0;
        }

        public Color(byte r, byte g, byte b, byte a = byte.MaxValue)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }
    }
}
