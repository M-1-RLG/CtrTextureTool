﻿using CtrTex.ETC1.Helper;
using CtrTex.ETC1.Models;

namespace CtrTex.ETC1
{
    class Etc1Transcoder
    {
        public static IEnumerable<Color> DecodeBlocks(Etc1PixelData data)
        {
            var basec0 = data.Block.Color0.Scale(data.Block.ColorDepth);
            var basec1 = data.Block.Color1.Scale(data.Block.ColorDepth);

            var flipbitmask = data.Block.FlipBit ? 2 : 8;
            foreach (var i in Constants.ZOrder)
            {
                var basec = (i & flipbitmask) == 0 ? basec0 : basec1;
                var mod = Constants.Modifiers[(i & flipbitmask) == 0 ? data.Block.Table0 : data.Block.Table1];
                var c = basec + mod[data.Block[i]];

                yield return new Color(c.R, c.G, c.B, (byte)((data.Alpha >> (4 * i)) % 16 * 17));
            }
        }

        public static Etc1PixelData EncodeColors(IList<Color> colorBatch)
        {
            var colorsWindows = Enumerable.Range(0, 16).Select(j => colorBatch[Constants.ZOrder[Constants.ZOrder[Constants.ZOrder[j]]]]);

            var alpha = colorsWindows.Reverse().Aggregate(0ul, (a, b) => (a * 16) | (byte)(b.A / 16));
            var colors = colorsWindows.Select(c2 => new RGB(c2.R, c2.G, c2.B)).ToList();

            Block block;
            // special case 1: this block has all 16 pixels exactly the same color
            if (colors.All(color => color == colors[0]))
            {
                block = PackSolidColor(colors[0]);
            }
            // special case 2: this block was previously etc1-compressed
            else if (!Optimizer.RepackEtc1CompressedBlock(colors, out block))
            {
                block = Optimizer.Encode(colors);
            }

            return new Etc1PixelData { Alpha = alpha, Block = block };
        }

        private static Block PackSolidColor(RGB c)
        {
            return (from i in Enumerable.Range(0, 64)
                    let r = Constants.StaticColorLookup[i * 256 + c.R]
                    let g = Constants.StaticColorLookup[i * 256 + c.G]
                    let b = Constants.StaticColorLookup[i * 256 + c.B]
                    orderby ErrorRGB(r >> 8, g >> 8, b >> 8)
                    let soln = new Solution
                    {
                        BlockColor = new RGB(r, g, b),
                        IntenTable = Constants.Modifiers[(i >> 2) & 7],
                        SelectorMSB = (i & 2) == 2 ? 0xFF : 0,
                        SelectorLSB = (i & 1) == 1 ? 0xFF : 0
                    }
                    select new SolutionSet(false, (i & 32) == 32, soln, soln).ToBlock())
                .First();
        }

        private static int ErrorRGB(int r, int g, int b) => 2 * r * r + 4 * g * g + 3 * b * b; // human perception
    }
}
