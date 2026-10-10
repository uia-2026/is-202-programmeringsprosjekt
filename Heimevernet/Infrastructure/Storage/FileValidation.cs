namespace Heimevernet.Infrastructure.Storage;

public static class FileValidation
{
    /// <summary>Validates uploaded images by checking their file signatures (magic bytes).</summary>
    public static bool IsValidImage(Stream stream)
    {
        Span<byte> header = stackalloc byte[12];

        stream.Position = 0;
        var read = stream.ReadAtLeast(
            header, header.Length, throwOnEndOfStream: false);
        stream.Position = 0;

        var isJpeg = read >= 3
            && header[0] == 0xFF
            && header[1] == 0xD8
            && header[2] == 0xFF;

        var isPng = read >= 8
            && header[..8].SequenceEqual(
                new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });

        var isWebp = read >= 12
            && header[..4].SequenceEqual("RIFF"u8)
            && header[8..12].SequenceEqual("WEBP"u8);

        return isJpeg || isPng || isWebp;
    }
}