using CtrTex.ETC1;

namespace CtrTex
{
    // A lot of code taken from: https://github.com/KillzXGaming/SPICA/blob/master/SPICA/PICA/Converters/TextureConverter.cs
    // ETC1(a4) compression and more edited from: https://github.com/FanTranslatorsInternational/Kuriimu2
    // TODO: Clean all of this...
    public enum Etc1Quality
    {
        Low,
        Medium,
        High,
    };

    public enum CtrTexFormat
    {
        RGBA8,
        RGB8,
        RGBA5551,
        RGB565,
        RGBA4,
        LA8,
        RG8,
        L8,
        A8,
        LA4,
        L4,
        A4,
        ETC1,
        ETC1a4
    }

    public enum GLTexFormat : uint
    {
        RGBA8 = 0x14016752,
        RGB8 = 0x14016754,
        RGBA5551 = 0x80346752,
        RGB565 = 0x83636754,
        RGBA4 = 0x80336752,
        A8 = 0x14016756,
        L8 = 0x14016757,
        LA8 = 0x14016758,
        RG8 = 0x14016759,
        A4 = 0x67616756,
        L4 = 0x67616757,
        LA4 = 0x67606758,
        ETC1 = 0x0000675A,
        ETC1a4 = 0x0000675B,
    }

    public class CtrCodec
    {
        private static readonly int[] FmtBPP = new int[] { 32, 24, 16, 16, 16, 16, 16, 8, 8, 8, 4, 4, 4, 8 };

        private static readonly int[] SwizzleLUT =
        {
             0,  1,  8,  9,  2,  3, 10, 11,
            16, 17, 24, 25, 18, 19, 26, 27,
             4,  5, 12, 13,  6,  7, 14, 15,
            20, 21, 28, 29, 22, 23, 30, 31,
            32, 33, 40, 41, 34, 35, 42, 43,
            48, 49, 56, 57, 50, 51, 58, 59,
            36, 37, 44, 45, 38, 39, 46, 47,
            52, 53, 60, 61, 54, 55, 62, 63
        };

        #region Decoding
        public static byte[] DecodeBuffer(byte[] input, int width, int height, GLTexFormat format)
        {
            return DecodeBuffer(input, width, height, ToTextureFormat(format));
        }

        public static byte[] DecodeBuffer(byte[] input, int width, int height, CtrTexFormat format)
        {
            byte[] output = new byte[width * height * 4];
            byte[] etcInput = Array.Empty<byte>();

            int IOffs = 0;
            int Increment = FmtBPP[(int)format] / 8;

            if (Increment == 0)
                Increment = 1;

            if (format >= CtrTexFormat.ETC1)
            {
                Increment = 4;
                etcInput = Etc1Support.Load(input, format == CtrTexFormat.ETC1a4);
            }

            for (int TY = 0; TY < height; TY += 8)
            {
                for (int TX = 0; TX < width; TX += 8)
                {
                    for (int Px = 0; Px < 64; Px++)
                    {
                        int X = SwizzleLUT[Px] & 7;
                        int Y = (SwizzleLUT[Px] - X) >> 3;

                        int OOffs = (TX + X + (((TY + Y)) * width)) * 4;

                        switch (format)
                        {
                            case CtrTexFormat.ETC1a4:
                            case CtrTexFormat.ETC1:
                                output[OOffs + 0] = etcInput[IOffs + 0];
                                output[OOffs + 1] = etcInput[IOffs + 1];
                                output[OOffs + 2] = etcInput[IOffs + 2];
                                output[OOffs + 3] = etcInput[IOffs + 3];
                                break;
                            case CtrTexFormat.RGBA8:
                                output[OOffs + 0] = input[IOffs + 3];
                                output[OOffs + 1] = input[IOffs + 2];
                                output[OOffs + 2] = input[IOffs + 1];
                                output[OOffs + 3] = input[IOffs + 0];
                                break;
                            case CtrTexFormat.RGB8:
                                output[OOffs + 0] = input[IOffs + 2];
                                output[OOffs + 1] = input[IOffs + 1];
                                output[OOffs + 2] = input[IOffs + 0];
                                output[OOffs + 3] = 0xff;
                                break;
                            case CtrTexFormat.RGBA5551: DecodeRGBA5551(output, OOffs, GetUShort(input, IOffs)); break;
                            case CtrTexFormat.RGB565: DecodeRGB565(output, OOffs, GetUShort(input, IOffs)); break;
                            case CtrTexFormat.RGBA4: DecodeRGBA4(output, OOffs, GetUShort(input, IOffs)); break;
                            case CtrTexFormat.LA8:
                                output[OOffs + 0] = input[IOffs + 1];
                                output[OOffs + 1] = input[IOffs + 1];
                                output[OOffs + 2] = input[IOffs + 1];
                                output[OOffs + 3] = input[IOffs + 0];
                                break;
                            case CtrTexFormat.RG8:
                                output[OOffs + 0] = input[IOffs + 1];
                                output[OOffs + 1] = input[IOffs + 0];
                                output[OOffs + 2] = 0xff;
                                output[OOffs + 3] = 0xff;
                                break;
                            case CtrTexFormat.L8:
                                output[OOffs + 0] = input[IOffs];
                                output[OOffs + 1] = input[IOffs];
                                output[OOffs + 2] = input[IOffs];
                                output[OOffs + 3] = 0xff;
                                break;
                            case CtrTexFormat.A8:
                                output[OOffs + 0] = 0xff;
                                output[OOffs + 1] = 0xff;
                                output[OOffs + 2] = 0xff;
                                output[OOffs + 3] = input[IOffs];
                                break;
                            case CtrTexFormat.LA4:
                                output[OOffs + 0] = (byte)((input[IOffs] >> 4) | (input[IOffs] & 0xf0));
                                output[OOffs + 1] = (byte)((input[IOffs] >> 4) | (input[IOffs] & 0xf0));
                                output[OOffs + 2] = (byte)((input[IOffs] >> 4) | (input[IOffs] & 0xf0));
                                output[OOffs + 3] = (byte)((input[IOffs] << 4) | (input[IOffs] & 0x0f));
                                break;
                            case CtrTexFormat.L4:
                                int L = (input[IOffs >> 1] >> ((IOffs & 1) << 2)) & 0xf;

                                output[OOffs + 0] = (byte)((L << 4) | L);
                                output[OOffs + 1] = (byte)((L << 4) | L);
                                output[OOffs + 2] = (byte)((L << 4) | L);
                                output[OOffs + 3] = 0xff;
                                break;
                            case CtrTexFormat.A4:
                                int A = (input[IOffs >> 1] >> ((IOffs & 1) << 2)) & 0xf;

                                output[OOffs + 0] = 0xff;
                                output[OOffs + 1] = 0xff;
                                output[OOffs + 2] = 0xff;
                                output[OOffs + 3] = (byte)((A << 4) | A);
                                break;
                        }

                        IOffs += Increment;
                    }
                }
            }

            return output;
        }

        private static void DecodeRGBA5551(byte[] Buffer, int Address, ushort Value)
        {
            int B = ((Value >> 1) & 0x1f) << 3;
            int G = ((Value >> 6) & 0x1f) << 3;
            int R = ((Value >> 11) & 0x1f) << 3;

            SetColor(Buffer, Address, (Value & 1) * 0xff,
                B | (B >> 5),
                G | (G >> 5),
                R | (R >> 5));
        }

        private static void DecodeRGB565(byte[] Buffer, int Address, ushort Value)
        {
            int B = ((Value >> 0) & 0x1f) << 3;
            int G = ((Value >> 5) & 0x3f) << 2;
            int R = ((Value >> 11) & 0x1f) << 3;

            SetColor(Buffer, Address, 0xff,
                B | (B >> 5),
                G | (G >> 6),
                R | (R >> 5));
        }

        private static void DecodeRGBA4(byte[] Buffer, int Address, ushort Value)
        {
            int B = (Value >> 4) & 0xf;
            int G = (Value >> 8) & 0xf;
            int R = (Value >> 12) & 0xf;

            SetColor(Buffer, Address, (Value & 0xf) | (Value << 4),
                B | (B << 4),
                G | (G << 4),
                R | (R << 4));
        }

        private static void SetColor(byte[] Buffer, int Address, int A, int B, int G, int R)
        {
            Buffer[Address + 0] = (byte)R;
            Buffer[Address + 1] = (byte)G;
            Buffer[Address + 2] = (byte)B;
            Buffer[Address + 3] = (byte)A;
        }

        private static ushort GetUShort(byte[] Buffer, int Address)
        {
            return (ushort)(
                Buffer[Address + 0] << 0 |
                Buffer[Address + 1] << 8);
        }
        #endregion

        #region Encoding
        public static byte[] EncodeBuffer(byte[] rawRgba, int width, int height, GLTexFormat format, Etc1Quality quality = Etc1Quality.Medium)
        {
            return EncodeBuffer(rawRgba, width, height, ToTextureFormat(format));
        }

        public static byte[] EncodeBuffer(byte[] rawRgba, int width, int height, CtrTexFormat format, Etc1Quality quality = Etc1Quality.Medium)
        {
            if (rawRgba.Length != width * height * 4)
                throw new Exception("Not enough data in rgba array!");

            if (format >= CtrTexFormat.ETC1)
            {
                return Etc1Support.Save(GetETC1Colors(rawRgba, width, height), format == CtrTexFormat.ETC1a4, quality);
            }
            else
            {
                byte[] Output = new byte[CalculateLength(width, height, format)];

                int OOffs = 0;
                int BPP = FmtBPP[(int)format] / 8;
                if (BPP == 0) BPP = 1;

                for (int TY = 0; TY < height; TY += 8)
                {
                    for (int TX = 0; TX < width; TX += 8)
                    {
                        for (int Px = 0; Px < 64; Px++)
                        {
                            int X = SwizzleLUT[Px] & 7;
                            int Y = (SwizzleLUT[Px] - X) >> 3;

                            int IOffs = (TX + X + ((TY + Y) * width)) * 4;

                            switch (format)
                            {
                                case CtrTexFormat.RGB8:
                                    Output[OOffs + 2] = rawRgba[IOffs + 0];
                                    Output[OOffs + 1] = rawRgba[IOffs + 1];
                                    Output[OOffs + 0] = rawRgba[IOffs + 2];
                                    break;
                                case CtrTexFormat.RGBA8:
                                    Output[OOffs + 3] = rawRgba[IOffs + 0];
                                    Output[OOffs + 2] = rawRgba[IOffs + 1];
                                    Output[OOffs + 1] = rawRgba[IOffs + 2];
                                    Output[OOffs + 0] = rawRgba[IOffs + 3];
                                    break;
                                case CtrTexFormat.RGB565:
                                    {
                                        ushort R = (ushort)(Convert8To5(rawRgba[IOffs + 0]) << 11);
                                        ushort G = (ushort)(Convert8To6(rawRgba[IOffs + 1]) << 5);
                                        ushort B = (ushort)(Convert8To5(rawRgba[IOffs + 2]));
                                        ushort result = (ushort)(R | G | B);

                                        Output[OOffs + 0] = (byte)(result & 0xFF);
                                        Output[OOffs + 1] = (byte)((result >> 8) & 0xFF);
                                    } break;
                                case CtrTexFormat.RGBA4:
                                    {
                                        ushort R = (ushort)(Convert8To4(rawRgba[IOffs + 0]) << 12);
                                        ushort G = (ushort)(Convert8To4(rawRgba[IOffs + 1]) << 8);
                                        ushort B = (ushort)(Convert8To4(rawRgba[IOffs + 2]) << 4);
                                        ushort A = (ushort)(Convert8To4(rawRgba[IOffs + 3]));
                                        ushort result = (ushort)(R | G | B | A);

                                        Output[OOffs + 0] = (byte)(result & 0xFF);
                                        Output[OOffs + 1] = (byte)((result >> 8) & 0xFF);
                                    } break;
                                case CtrTexFormat.RGBA5551:
                                    {
                                        ushort R = (ushort)(Convert8To5(rawRgba[IOffs + 0]) << 11);
                                        ushort G = (ushort)(Convert8To5(rawRgba[IOffs + 1]) << 6);
                                        ushort B = (ushort)(Convert8To5(rawRgba[IOffs + 2]) << 1);
                                        ushort A = (ushort)(Convert8To1(rawRgba[IOffs + 3]));
                                        ushort result = (ushort)(R | G | B | A);

                                        Output[OOffs + 0] = (byte)(result & 0xFF);
                                        Output[OOffs + 1] = (byte)((result >> 8) & 0xFF);
                                    } break;
                                case CtrTexFormat.A8:
                                    Output[OOffs + 0] = rawRgba[IOffs + 3];
                                    break;
                                case CtrTexFormat.L4:
                                    {
                                        int ActualOOffs = OOffs / 2;
                                        int Shift = (OOffs & 1) * 4;
                                        Output[ActualOOffs] |= (byte)((GetLuminosity(rawRgba, IOffs) >> 4 & 0xF) << Shift);
                                    }
                                    break;
                                case CtrTexFormat.LA4:
                                    {
                                        byte A = rawRgba[IOffs + 3];
                                        byte L = ConvertRGB8ToL(new byte[]{ rawRgba[IOffs + 0], rawRgba[IOffs + 1], rawRgba[IOffs + 2]});
                                        Output[OOffs + 0] = (byte)(Convert8To4(L) << 4 | Convert8To4(A));
                                    }
                                    break;
                                case CtrTexFormat.A4:
                                    {
                                        int ActualOOffs = OOffs / 2;
                                        int Shift = (OOffs & 1) * 4;
                                        Output[ActualOOffs] |= (byte)((rawRgba[IOffs + 3] >> 4 & 0xF) << Shift);
                                    }
                                    break;
                                case CtrTexFormat.L8:
                                    Output[OOffs + 0] = ConvertRGB8ToL(new byte[]{ rawRgba[IOffs + 0], rawRgba[IOffs + 1], rawRgba[IOffs + 2]});
                                    break;
                                case CtrTexFormat.LA8:
                                    Output[OOffs + 0] = rawRgba[IOffs + 3];
                                    Output[OOffs + 1] = ConvertRGB8ToL(new byte[]{ rawRgba[IOffs + 0], rawRgba[IOffs + 1], rawRgba[IOffs + 2]});
                                    break;
                                case CtrTexFormat.RG8:
                                    Output[OOffs + 0] = rawRgba[IOffs + 1];
                                    Output[OOffs + 1] = rawRgba[IOffs + 0];
                                    break;
                                default: throw new NotImplementedException();
                            }
                            OOffs += BPP;
                        }
                    }
                }

                return Output;
            }
        }

        static int CalculateLength(int width, int height, CtrTexFormat format)
        {
            int Length = (width * height * FmtBPP[(int)format]) / 8;

            if ((Length & 0x7f) != 0)
            {
                Length = (Length & ~0x7f) + 0x80;
            }

            return Length;
        }

        // Convert helpers from Citra Emulator (citra/src/common/color.h)
        private static byte Convert8To1(byte val) { return (byte)(val == 0 ? 0 : 1); }
        private static byte Convert8To4(byte val) { return (byte)(val >> 4); }
        private static byte Convert8To5(byte val) { return (byte)(val >> 3); }
        private static byte Convert8To6(byte val) { return (byte)(val >> 2); }
        private static int Clamp(int value, int min, int max) => Math.Min(Math.Max(value, min), max - 1);

        private static byte ConvertRGB8ToL(byte[] bytes)
        {
            byte L = (byte)(bytes[0] * 0.212655f);
            L += (byte)(bytes[1] * 0.715158f);
            L += (byte)(bytes[2] * 0.072187f);
            return L;
        }

        private static byte GetLuminosity(byte[] RGBA, int IOffs)
        {
            return (byte)((RGBA[IOffs] + RGBA[IOffs + 1] + RGBA[IOffs + 2]) / 3);
        }

        private static IEnumerable<Point> GetPointSequence(int width, int height)
        {
            var zorder = new CtrSwizzle(width);
            for (int i = 0; i < width * height; i++)
            {
                var point = new Point(i % width, i / width);
                point = zorder.Get(point.Y * width + point.X);

                yield return point;
            }
        }

        private static IEnumerable<Color> GetETC1Colors(byte[] rawRgba, int width, int height)
        {
            var points = GetPointSequence(width, height);

            foreach (var point in points)
            {
                int x = Clamp(point.X, 0, width);
                int y = Clamp(point.Y, 0, height);

                int p = (x + y * width) * 4;

                yield return new Color(rawRgba[p + 0], rawRgba[p + 1], rawRgba[p + 2], rawRgba[p + 3]);
            }
        }

        #endregion

        private static CtrTexFormat ToTextureFormat(GLTexFormat format)
        {

            if (((uint)format & 0xFFFF) == 0x675A) return CtrTexFormat.ETC1;
            else if (((uint)format & 0xFFFF) == 0x675B) return CtrTexFormat.ETC1a4;

            return format switch
            {
                GLTexFormat.RGBA8 => CtrTexFormat.RGBA8,
                GLTexFormat.RGB8 => CtrTexFormat.RGB8,
                GLTexFormat.RGBA5551 => CtrTexFormat.RGBA5551,
                GLTexFormat.RGB565 => CtrTexFormat.RGB565,
                GLTexFormat.RGBA4 => CtrTexFormat.RGBA4,
                GLTexFormat.LA8 => CtrTexFormat.LA8,
                GLTexFormat.RG8 => CtrTexFormat.RG8,
                GLTexFormat.L8 => CtrTexFormat.L8,
                GLTexFormat.A8 => CtrTexFormat.A8,
                GLTexFormat.LA4 => CtrTexFormat.LA4,
                GLTexFormat.L4 => CtrTexFormat.L4,
                GLTexFormat.A4 => CtrTexFormat.A4,
                _ => throw new Exception($"Unsupported texture format {format}"),
            };
        }
    }
}
