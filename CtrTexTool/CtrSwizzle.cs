namespace CtrTex
{
    internal class CtrSwizzle
    {
        private readonly (int, int)[] _bitFieldCoords = new[] { (1, 0), (0, 1), (2, 0), (0, 2), (4, 0), (0, 4) };

        private const int MacroTileWidth = 8;
        private const int MacroTileHeight = 8;
        readonly int _widthInTiles;
        private Point _init;

        public CtrSwizzle(int imageStride)
        {
            _init = new Point();
            _widthInTiles = (imageStride + MacroTileWidth - 1) / MacroTileWidth;
        }

        public Point Get(int pointCount)
        {
            var macroTileCount = pointCount / MacroTileWidth / MacroTileHeight;
            var (macroX, macroY) = (macroTileCount % _widthInTiles, macroTileCount / _widthInTiles);

            return new[] { (macroX * MacroTileWidth, macroY * MacroTileHeight) }
                .Concat(_bitFieldCoords.Where((v, j) => (pointCount >> j) % 2 == 1))
                .Aggregate(_init, (a, b) => new Point(a.X ^ b.Item1, a.Y ^ b.Item2));
        }
    }
}
