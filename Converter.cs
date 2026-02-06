using System.Text;

namespace LocPackBin
{
    public static class Converter
    {
        public static void ProcessFile(string path)
        {
            bool isMenus = Path.GetFileNameWithoutExtension(path).Contains("menus");
            bool isSubtitles = Path.GetFileNameWithoutExtension(path).Contains("subtitles");
            bool hasProcessed = false;
            
            // First two lines from each .locpack
            const string menus = "06000000A7780000"; // 6,,, // 30887,,,
            const string subtitles = "070000001D000100"; // 7,,,,, // 65565,,,,,

            string[] locPackArray = File.ReadAllText(path).Split(Environment.NewLine).Skip(2).ToArray(); // Skip first two lines
            
            List<string> hexList = [];

            if (isMenus)
            {
                hexList.Add(menus); // Add first two lines
                
                foreach (var line in locPackArray)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        string newline = GetLine(line);
                        hexList.Add(newline);
                    }
                }

                if (locPackArray.Length == hexList.Count)
                {
                    hasProcessed = true;
                }
            }
            else if (isSubtitles)
            {
                hexList.Add(subtitles); // Add first two lines
                
                foreach (var line in locPackArray)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        string newline = GetLineSubtitles(line);
                        hexList.Add(newline);
                    }
                }
                
                if (locPackArray.Length == hexList.Count)
                {
                    hasProcessed = true;
                }
            }

            if (hasProcessed)
            {
                using (var writer = new BinaryWriter(new FileStream(Path.ChangeExtension(path, ".locpackbin"), FileMode.Create)))
                {
                    foreach (var line in hexList)
                    {
                        writer.Write(Convert.FromHexString(line));
                    }
                }

                Console.WriteLine($"Converted: {Path.GetFileName(path)} to {Path.GetFileNameWithoutExtension(path)}.locpackbin");
            }
            else
            {
                Console.WriteLine($"Failed to convert file: {Path.GetFileName(path)}");
            }
        }

        private static string GetLine(string line)
        {
            string[] splitLine = line.Split([','], 4);
            
            string guid  = splitLine[0];
            int lineVersion = int.Parse(splitLine[1]);
            int maxLength = int.Parse(splitLine[2]);
            string text = splitLine[3];

            if (text.StartsWith('"'))
            {
                text = text[1..^1]; // Remove quotation marks from beginning and end of line
            }
            
            text = text.Replace("\"\"", "\""); // Any line with double quotation marks must be replaced with single

            if (text.StartsWith('/'))
            {
                text = text.TrimStart('/'); // Remove forward slash from beginning of line if present
            }

            string hexGuid = GetGuid(guid);
            string hexLineVersion = GetIntHex(lineVersion);
            string hexMaxLength = GetIntHex(maxLength);
            string hexText = GetStringHex(text);
            
            int textLength = GetHexLength(hexText);
            string hexTextLength = GetIntHex(textLength);

            if (textLength == 0)
            {
                hexMaxLength = "0000"; // If text is empty, zero hexMaxLength
            }
            
            string hexZeroed8 = "00000000"; // Zeroed
            
            if (lineVersion < 0)
            {
                hexZeroed8 = "0000"; // If lineVersion is negative, zeroed bytes reduced by two
            }
            
            string newLine = hexGuid + hexLineVersion + hexMaxLength + hexZeroed8 + hexTextLength + hexText;
            
            return newLine; 
        }

        private static string GetLineSubtitles(string line)
        {
            string[] splitLine = line.Split([','], 6);
            
            string guid  = splitLine[0];
            int unk0 = int.Parse(splitLine[1]); // Unknown
            int unk1 = int.Parse(splitLine[2]); // Unknown
            int unk2 = int.Parse(splitLine[3]); // Unknown
            int unk3 = int.Parse(splitLine[4]); // Unknown
            string text = splitLine[5];

            if (text.StartsWith('"'))
            {
                text = text[1..^1]; // Remove quotation marks from beginning and end of line
            }
            
            text = text.Replace("\"\"", "\""); // Any line with double quotation marks must be replaced with single

            if (text.StartsWith('/'))
            {
                text = text.TrimStart('/'); // Remove forward slash from line if present
            }

            string hexGuid = GetGuid(guid);
            string hexUnk0 = GetIntHex(unk0);
            string hexUnk1 = GetIntHex(unk1);
            string hexUnk2 = GetIntHex(unk2);
            string hexUnk3 = GetIntHex(unk3);
            string hexText = GetStringHex(text);
            
            int textLength = GetHexLength(hexText);
            string hexTextLength = GetIntHex(textLength);

            string hexZeroed4 = "0000"; // Zeroed

            if (unk0 < 0)
            {
                hexZeroed4 = ""; // If unk0 is negative, remove zeroed bytes
            }

            string hexZeroed2 = "";
            string hexZeroed8 = "00000000"; // Zeroed
            
            if (unk0 == 4 && textLength == 0)
            {
                hexUnk2 = "0004"; // Reverse
                hexZeroed2 = "00"; // Zeroed
                hexZeroed8 = "000000"; // Zeroed bytes reduced by one
            }
            
            string newLine = hexGuid + hexUnk0 + hexZeroed4 + hexUnk1 + hexZeroed2 + hexUnk2 + hexZeroed8 + hexUnk3 + "0000" + hexTextLength + hexText;

            if (unk0 != 4 && textLength == 0)
            {
                newLine = newLine.Replace("040000", "000004");
            }
            
            return newLine;
        }

        private static string GetGuid(string guid)
        {
            Guid groupedGuid = Guid.Parse(guid);
            string[] guidGroups = groupedGuid.ToString().Split('-');
            
            string group0 = ReverseGroup(guidGroups[0]);
            string group1 = ReverseGroup(guidGroups[1]);
            string group2 = ReverseGroup(guidGroups[2]);
            string group3 = ReverseGroup(guidGroups[3]);
            string group4 = ReverseGroup(guidGroups[4]);
            
            string newGuid = group2 + group1 + group0 + group4 + group3; // Corrected Order
            
            return newGuid.ToUpper();
        }

        private static string GetIntHex(int num)
        {
            string hex = num.ToString("X4");
            hex = string.Join("", SplitHex(hex));
            
            return hex;
        }

        private static string GetStringHex(string text)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(text);
            string hex = BitConverter.ToString(bytes);
            hex = hex.Replace("-", "");
            
            return hex;
        }

        private static int GetHexLength(string hex)
        {
            return (hex.Length / 2);
        }

        private static string ReverseGroup(string group)
        {
            return (string.Join("", SplitHex(group)));
        }

        private static string[] SplitHex(string text)
        {
            byte[] bytes = Convert.FromHexString(text);
            string hex = BitConverter.ToString(bytes);
            
            string[] hexArray = hex.Split('-');
            Array.Reverse(hexArray);
            
            return hexArray;
        }
    }
}
