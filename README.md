# (Ctr)3DS Texture Tool

## Supported formats
```
RGBA8
RGB8
RGBA5551
RGB565
RGBA4
LA8
RG8 (HiLo8)
L8
A8
LA4
L4
A4
ETC1
ETC1a4
```

## Usage
```cs
using CtrTex;

// Note: Etc1Quality is set to medium by default.
// For ETC1(a4) it's recommended to endcode in medium quality for speed reasons.

byte[] encodedBytes = CtrCodec.EncodeBuffer(rawRGBABytes, width, height, CtrTexFormat.ETC1, Etc1Quality.Medium);
byte[] rgbaBytes = CtrCodec.DecodeBuffer(encoded, width, height, CtrTexFormat.ETC1);
```

## Credits 
[Kuriimu2/Kuriimu2](https://github.com/FanTranslatorsInternational/Kuriimu2) for ETC1(a4) Encoding/Decoding.

[gdkchan and KillzXGaming](https://github.com/KillzXGaming/SPICA) for most texture Encoding/Decoding.
