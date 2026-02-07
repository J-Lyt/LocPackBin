using System.Buffers.Binary;
using System.Text;

namespace LocPackBin
{
    public static class LocPackConverter
    {
        private static class LocPackLines
        {
            // First two lines from each .locpack
            private static readonly HashSet<(int, int)> Menus = 
            [
                (6, 30887), // Avatar: Frontiers of Pandora
                (6, 17106), // Star Wars: Outlaws
            ];
        
            private static readonly HashSet<(int, int)> Subtitles = 
            [
                (7, 65565), // Avatar: Frontiers of Pandora
                (8, 98265), // Star Wars: Outlaws
            ];
        
            public static bool IsMenus(int line1, int line2) => Menus.Contains((line1, line2));
            public static bool IsSubtitles(int line1, int line2) => Subtitles.Contains((line1, line2));
        }
        
        #region ToBin
        public static void FileToBin(string path)
        {
            try
            {
                string[] locPackArray = File.ReadAllText(path).Split(Environment.NewLine).Where(line => !string.IsNullOrWhiteSpace(line)).ToArray();

                int locPackLine1 = int.Parse(locPackArray[0].TrimEnd(','));
                int locPackLine2 = int.Parse(locPackArray[1].TrimEnd(','));
            
                bool isMenus = LocPackLines.IsMenus(locPackLine1, locPackLine2);
                bool isSubtitles = LocPackLines.IsSubtitles(locPackLine1, locPackLine2);

                if (!isMenus && !isSubtitles)
                {
                    Console.WriteLine($"Error: {Path.GetFileName(path)} is not a valid menus or subtitles .locpack file");
                    return;
                }
                
                string[] locPackArraySkipped = locPackArray.Skip(2).ToArray(); // Skip first two lines
                    
                using (var writer = new BinaryWriter(new FileStream(Path.ChangeExtension(path, ".locpackbin"), FileMode.Create)))
                {
                    writer.Write(locPackLine1);
                    writer.Write(locPackLine2);
                        
                    if (isMenus)
                    {
                        foreach (var line in locPackArraySkipped)
                        {
                            WriteLineMenus(writer, line);
                        }
                    }
                    else if (isSubtitles)
                    {
                        foreach (var line in locPackArraySkipped)
                        {
                            WriteLineSubtitles(writer, line);
                        }
                    }
                }
                
                Console.WriteLine($"Converted: {Path.GetFileName(path)} to {Path.GetFileNameWithoutExtension(path)}.locpackbin");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to convert file: {Path.GetFileName(path)} - {ex.Message}");
            }
        }
        
        private static void WriteLineMenus(BinaryWriter writer, string line)
        {
            string[] splitLine = line.Split([','], 4);
            
            string guid = splitLine[0];
            int lineVersion = int.Parse(splitLine[1]);
            int maxLength = int.Parse(splitLine[2]);
            string text = FormatTextBin(splitLine[3]);
            
            byte[] textBytes = Encoding.UTF8.GetBytes(text);
            int textLength = textBytes.Length;
            
            if (textLength == 0)
            {
                maxLength = 0; // If text is empty, zero maxLength
            }
            
            writer.Write(GetGuidBytes(guid));
            
            if (lineVersion >= 0)
            {
                writer.Write((short)lineVersion);
            }
            else
            {
                writer.Write(lineVersion);
            }
            
            writer.Write((short)maxLength);
            
            if (lineVersion < 0)
            {
                writer.Write(new byte[2]); // 2 zeroed bytes if lineVersion is negative
            }
            else
            {
                writer.Write(new byte[4]); // 4 zeroed bytes
            }
            
            writer.Write((short)textLength);
            
            if (textLength > 0)
            {
                writer.Write(textBytes);
            }
        }

        private static void WriteLineSubtitles(BinaryWriter writer, string line)
        {
            string[] splitLine = line.Split([','], 6);
            
            string guid = splitLine[0];
            int unk0 = int.Parse(splitLine[1]); // Unknown
            int unk1 = int.Parse(splitLine[2]); // Unknown
            int unk2 = int.Parse(splitLine[3]); // Unknown
            int unk3 = int.Parse(splitLine[4]); // Unknown
            string text = FormatTextBin(splitLine[5]);
            
            byte[] textBytes = Encoding.UTF8.GetBytes(text);
            int textLength = textBytes.Length;
            
            writer.Write(GetGuidBytes(guid));
            
            if (unk0 < 0)
            {
                writer.Write(unk0);
            }
            else
            {
                writer.Write((short)unk0);
            }
            
            if (unk0 >= 0)
            {
                writer.Write(new byte[2]); // 2 zeroed bytes if unk0 is positive
            }
            
            writer.Write((short)unk1);
            
            writer.Write((byte)0x00); // 1 zeroed byte
            
            Span<byte> buffer = stackalloc byte[2];
            BinaryPrimitives.WriteInt16BigEndian(buffer, (short)unk2); // Write unk2 as big-endian
            writer.Write(buffer);
            
            writer.Write(new byte[3]); // 3 zeroed bytes
            
            writer.Write((short)unk3);
            writer.Write(new byte[2]); // 2 zeroed bytes
            writer.Write((short)textLength);
            
            if (textLength > 0)
            {
                writer.Write(textBytes);
            }
        }

        private static byte[] GetGuidBytes(string guid)
        {
            byte[] bytes = Guid.Parse(guid).ToByteArray();
            byte[] newBytes = new byte[16];
            
            Array.Copy(bytes, 6, newBytes, 0, 2); // Group 3
            Array.Copy(bytes, 4, newBytes, 2, 2); // Group 2
            Array.Copy(bytes, 0, newBytes, 4, 4); // Group 1
            // Group 5 - Reversed
            Array.Copy(bytes, 10, newBytes, 8, 6);
            Array.Reverse(newBytes, 8, 6);
            // Group 4 - Reversed
            Array.Copy(bytes, 8, newBytes, 14, 2);
            Array.Reverse(newBytes, 14, 2);
            
            return newBytes;
        }

        private static string FormatTextBin(string text)
        {
            if (text.StartsWith('"'))
            {
                text = text[1..^1]; // Remove quotation marks from beginning and end of line
            }
            
            text = text.Replace("\"\"", "\""); // Line with double quotation marks must be replaced with single
            
            if (text.StartsWith('/'))
            {
                text = text[1..]; // Remove forward slash from line if present
            }
            
            return text;
        }
        #endregion

        #region FromBin
        public static void FileFromBin(string path)
        {
            try
            {
                using var reader = new BinaryReader(new FileStream(path, FileMode.Open));
                var lines = new List<string>();

                // Read and store the first two lines (8 bytes)
                int locPackLine1 = reader.ReadInt32();
                int locPackLine2 = reader.ReadInt32();
                
                bool isMenus = LocPackLines.IsMenus(locPackLine1, locPackLine2);
                bool isSubtitles = LocPackLines.IsSubtitles(locPackLine1, locPackLine2);

                if (!isMenus && !isSubtitles)
                {
                    Console.WriteLine($"Error: {Path.GetFileName(path)} is not a valid menus or subtitles .locpackbin file");
                    return;
                }
                
                // Menus have 3 trailing commas, subtitles have 5
                int commaCount = isMenus ? 3 : 5;
                string commas = new string(',', commaCount);
                
                lines.Add(locPackLine1 + commas);
                lines.Add(locPackLine2 + commas);

                while (reader.BaseStream.Position < reader.BaseStream.Length)
                {
                    if (isMenus)
                    {
                        lines.Add(ReadLineMenus(reader));
                    }
                    else if (isSubtitles)
                    {
                        lines.Add(ReadLineSubtitles(reader));
                    }
                }
                
                File.WriteAllText(Path.ChangeExtension(path, ".locpack"), string.Join(Environment.NewLine, lines));

                Console.WriteLine($"Converted: {Path.GetFileName(path)} to {Path.GetFileNameWithoutExtension(path)}.locpack");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to convert file: {Path.GetFileName(path)} - {ex.Message}");
            }
        }

        private static string ReadLineMenus(BinaryReader reader)
        {
            string guid = ReadGuid(reader);
            
            long pos = reader.BaseStream.Position;
            
            short shortLineVersion = reader.ReadInt16(); // Read lineVersion as 2 bytes first
            short maxLength = reader.ReadInt16();
            byte[] padding = reader.ReadBytes(4);

            int lineVersion;

            if (padding.All(b => b == 0) && shortLineVersion >= 0) // Check if padding is zeroed and lineVersion is positive
            {
                lineVersion = shortLineVersion;
            }
            else
            {
                // Return to position and read lineVersion as negative
                reader.BaseStream.Position = pos;
                lineVersion = reader.ReadInt32();
                maxLength = reader.ReadInt16();
                reader.ReadBytes(2); // Skip 2 zeroed bytes
            }

            short textLength = reader.ReadInt16();

            string text = "";
            if (textLength > 0)
            {
                byte[] textBytes = reader.ReadBytes(textLength);
                text = Encoding.UTF8.GetString(textBytes);
            }

            text = FormatTextCSV(text);

            return $"{guid},{lineVersion},{maxLength},{text}";
        }

        private static string ReadLineSubtitles(BinaryReader reader)
        {
            string guid = ReadGuid(reader);
            
            long pos = reader.BaseStream.Position;
            
            short shortUnk0 = reader.ReadInt16(); // Read unk0 as 2 bytes first
            byte[] padding = reader.ReadBytes(2);

            int unk0;

            if (padding.All(b => b == 0) && shortUnk0 >= 0) // Check if padding is zeroed and unk0 is positive
            {
                unk0 = shortUnk0;
            }
            else
            {
                // Return to position and read unk0 as negative
                reader.BaseStream.Position = pos;
                unk0 = reader.ReadInt32();
            }

            short unk1 = reader.ReadInt16();

            reader.ReadByte(); // Skip 1 zeroed byte

            // Read unk2 as big-endian
            byte[] unk2Bytes = reader.ReadBytes(2);
            short unk2 = BinaryPrimitives.ReadInt16BigEndian(unk2Bytes);

            reader.ReadBytes(3); // Skip 3 zeroed bytes

            short unk3 = reader.ReadInt16();
            reader.ReadBytes(2); // Skip 2 zeroed bytes

            short textLength = reader.ReadInt16();

            string text = "";
            if (textLength > 0)
            {
                byte[] textBytes = reader.ReadBytes(textLength);
                text = Encoding.UTF8.GetString(textBytes);
            }

            text = FormatTextCSV(text);

            return $"{guid},{unk0},{unk1},{unk2},{unk3},{text}";
        }

        private static string ReadGuid(BinaryReader reader)
        {
            byte[] bytes = reader.ReadBytes(16);
            byte[] newBytes = new byte[16];
            
            Array.Copy(bytes, 4, newBytes, 0, 4); // Group 1
            Array.Copy(bytes, 2, newBytes, 4, 2); // Group 2
            Array.Copy(bytes, 0, newBytes, 6, 2); // Group 3
            // Group 4 - Reversed
            Array.Copy(bytes, 14, newBytes, 8, 2);
            Array.Reverse(newBytes, 8, 2);
            // Group 5 - Reversed
            Array.Copy(bytes, 8, newBytes, 10, 6);
            Array.Reverse(newBytes, 10, 6);

            return new Guid(newBytes).ToString("N").ToUpper();
        }

        private static string FormatTextCSV(string text)
        {
            // Add slash to beginning of text if it starts with '--'
            if (text.StartsWith("--"))
            {
                text = "/" + text;
            }
            
            bool needsQuotes = text.Contains(',') || text.Contains('"');
            text = text.Replace("\"", "\"\"");  // Line with single quotation marks must be replaced with double

            if (needsQuotes)
            {
                text = $"\"{text}\""; // Add quotation marks to beginning and end of line
            }

            return text;
        }
        #endregion
    }
}
