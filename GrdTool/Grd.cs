using System.Text.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace GrdTool
{
    class Grd
    {
        class Metadata
        {
            public int ScreenWidth { get; set; }
            public int ScreenHeight { get; set; }
            public int Bpp { get; set; }
            public int Left { get; set; }
            public int Right { get; set; }
            public int Top { get; set; }
            public int Bottom { get; set; }
        }

        public static void ExtractMetadata(string filePath, string jsonPath)
        {
            using var stream = File.OpenRead(filePath);
            using var reader = new BinaryReader(stream);

            var metadata = new Metadata();

            reader.ReadUInt16(); // pack type

            metadata.ScreenWidth = reader.ReadUInt16();
            metadata.ScreenHeight = reader.ReadUInt16();
            metadata.Bpp = reader.ReadUInt16();
            metadata.Left = reader.ReadUInt16();
            metadata.Right = reader.ReadUInt16();
            metadata.Top = reader.ReadUInt16();
            metadata.Bottom = reader.ReadUInt16();

            var serializerOptions = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(metadata, serializerOptions);
            File.WriteAllText(jsonPath, json);
        }

        public static void Create(string filePath, string sourcePath, string jsonPath)
        {
            // Load source image

            var source = Image.Load(sourcePath);

            if (source.PixelType.BitsPerPixel != 24 && source.PixelType.BitsPerPixel != 32)
            {
                throw new Exception("Only 24-bit and 32-bit image are supported.");
            }

            // Copy pixel data

            var image = source.CloneAs<Argb32>();

            // Flip image

            image.Mutate(x => x.Flip(FlipMode.Vertical));

            // Unpack pixel data

            var unpackedSize = image.Width * image.Height * 4;

            var memStream = new MemoryStream(unpackedSize);
            var memWriter = new BinaryWriter(memStream);

            for (var y = 0; y < image.Height; y++)
            {
                var row = image.GetPixelRowSpan(y);

                for (var x = 0; x < image.Width; x++)
                {
                    memWriter.Write(row[x].PackedValue);
                }
            }

            var pixelData = memStream.ToArray();

            // Compress pixel data

            var a_data = Array.Empty<byte>();
            var r_data = Array.Empty<byte>();
            var g_data = Array.Empty<byte>();
            var b_data = Array.Empty<byte>();

            if (source.PixelType.BitsPerPixel == 32)
            {
                a_data = RleCompress(pixelData, 0, 4);
                r_data = RleCompress(pixelData, 1, 4);
                g_data = RleCompress(pixelData, 2, 4);
                b_data = RleCompress(pixelData, 3, 4);
            }
            else
            {
                r_data = RleCompress(pixelData, 1, 3);
                g_data = RleCompress(pixelData, 2, 3);
                b_data = RleCompress(pixelData, 3, 3);
            }

            // Read metadata file

            Metadata? metadata = null;

            try
            {
                var json = File.ReadAllText(jsonPath);
                metadata = JsonSerializer.Deserialize<Metadata>(json);
            }
            catch (Exception)
            {
                Console.WriteLine("WARNING: Metadata failed to load.");

                metadata = new Metadata();
                metadata.Right = source.Width;
                metadata.Bottom = source.Height;
            }

            if (metadata is null)
            {
                throw new Exception("ERROR: Metadata failed to load.");
            }

            metadata.Bpp = source.PixelType.BitsPerPixel;

            // Create new image file

            using var stream = File.Create(filePath);
            using var writer = new BinaryWriter(stream);

            // Write header

            writer.WriteUInt16(0x101); // pack type
            writer.WriteUInt16(Convert.ToUInt16(metadata.ScreenWidth));
            writer.WriteUInt16(Convert.ToUInt16(metadata.ScreenHeight));
            writer.WriteUInt16(Convert.ToUInt16(metadata.Bpp));
            writer.WriteUInt16(Convert.ToUInt16(metadata.Left));
            writer.WriteUInt16(Convert.ToUInt16(metadata.Right));
            writer.WriteUInt16(Convert.ToUInt16(metadata.Top));
            writer.WriteUInt16(Convert.ToUInt16(metadata.Bottom));
            writer.WriteUInt32(Convert.ToUInt32(a_data.Length));
            writer.WriteUInt32(Convert.ToUInt32(r_data.Length));
            writer.WriteUInt32(Convert.ToUInt32(g_data.Length));
            writer.WriteUInt32(Convert.ToUInt32(b_data.Length));

            // Write pixel data

            if (a_data.Length != 0)
            {
                writer.Write(a_data);
            }

            writer.Write(r_data);
            writer.Write(g_data);
            writer.Write(b_data);
        }

        public static byte[] RleCompress(byte[] input, int src_p, int step)
        {
            var output = new MemoryStream();
            var writer = new BinaryWriter(output);

            int end_p = input.Length;

            while (src_p < end_p)
            {
                int scan_p = src_p;
                int curr_p = src_p;
                int next_p = src_p + step;

                if (next_p < end_p && input[next_p] == input[curr_p])
                {
                    int count = 1;

                    while (next_p < end_p && input[next_p] == input[scan_p] && count < 127)
                    {
                        next_p += step;
                        count++;
                    }

                    src_p += count * step;

                    count |= 0x80;

                    writer.WriteByte(Convert.ToByte(count));
                    writer.WriteByte(input[curr_p]);
                }
                else
                {
                    int count = 1;

                    while (next_p < end_p && input[next_p] != input[scan_p] && count < 127)
                    {
                        scan_p += step;
                        next_p += step;
                        count++;
                    }

                    src_p += count * step;

                    writer.WriteByte(Convert.ToByte(count));

                    for (var i = 0; i < count; i++)
                    {
                        writer.WriteByte(input[curr_p]);
                        curr_p += step;
                    }
                }
            }

            return output.ToArray();
        }
    }
}
