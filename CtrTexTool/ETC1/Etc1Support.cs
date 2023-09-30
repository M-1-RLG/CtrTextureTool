using CtrTex.ETC1.Models;
using CtrTex.MoreEnumerable;

namespace CtrTex.ETC1
{
    internal class Etc1Support
    {
        internal static Etc1Quality Quality = Etc1Quality.Medium;
        private static bool _useAlpha;

        protected static Etc1PixelData ReadNextBlock(BinaryReader br)
        {
            var alpha = _useAlpha ? br.ReadUInt64() : ulong.MaxValue;
            var colors = br.ReadUInt64();

            return new Etc1PixelData
            {
                Alpha = alpha,
                Block = new Block
                {
                    LSB = (ushort)(colors & 0xFFFF),
                    MSB = (ushort)((colors >> 16) & 0xFFFF),
                    Flags = (byte)((colors >> 32) & 0xFF),
                    B = (byte)((colors >> 40) & 0xFF),
                    G = (byte)((colors >> 48) & 0xFF),
                    R = (byte)((colors >> 56) & 0xFF)
                }
            };
        }

        protected static void WriteNextBlock(BinaryWriter bw, Etc1PixelData block)
        {
            if (_useAlpha) bw.Write(block.Alpha);
            bw.Write(block.Block.GetBlockData());
        }

        protected static IList<Color> DecodeNextBlock(Etc1PixelData block)
        {
            return Etc1Transcoder.DecodeBlocks(block).ToArray();
        }

        protected static Etc1PixelData EncodeNextBlock(IList<Color> colors)
        {
            return Etc1Transcoder.EncodeColors(colors);
        }

        private static IEnumerable<Etc1PixelData> ReadBlocks(BinaryReader br)
        {
            while (br.BaseStream.Position < br.BaseStream.Length)
                yield return ReadNextBlock(br);
        }

        public static byte[] Load(byte[] input, bool hasAlpha)
        {
            _useAlpha = hasAlpha;
            var br = new BinaryReader(new MemoryStream(input));

            return ReadBlocks(br).AsParallel().AsOrdered()
                .WithDegreeOfParallelism(Environment.ProcessorCount)
                .SelectMany(DecodeNextBlock)
                .SelectMany(o=> new byte[] {o.R, o.G, o.B, o.A}).ToArray();
        }

        public static byte[] Save(IEnumerable<Color> colors, bool hasAlpha, Etc1Quality quality)
        {
            _useAlpha = hasAlpha;
            Quality = quality;

            var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);

            var blocks = colors.Batch(16)
                .AsParallel().AsOrdered()
                .WithDegreeOfParallelism(Environment.ProcessorCount)
                .Select(c => EncodeNextBlock(c.ToArray()));

            foreach (var block in blocks)
                WriteNextBlock(bw, block);

            return ms.ToArray();
        }
    }
}
