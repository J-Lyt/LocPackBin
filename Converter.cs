using System.Text;

namespace LocPackBin
{
    public static class Converter
    {
        public static void ProcessFile(string path)
        {
            bool isMenus = path.Contains("menus");
            bool isSubtitles = path.Contains("subtitles");
            
            // First two lines from each .locpack
            const string menus = "06000000A7780000";
            const string subtitles = "070000001D000100";

            List<string> origList = File.ReadAllText(path).Split("\r\n").ToList();
            
            origList.RemoveRange(0, 2); // Remove first two lines when processing file

            bool hasProcessed = false;
            List<string> newList = [];

            if (isMenus)
            {
                newList.Add(menus);
                
                foreach (var line in origList)
                {
                    if (line != "")
                    {
                        string newline = GetLine(line);
                        newList.Add(newline);
                    }
                }

                if (origList.Count == newList.Count)
                {
                    hasProcessed = true;
                }
            }
            else if (isSubtitles)
            {
                newList.Add(subtitles);
                
                foreach (var line in origList)
                {
                    if (line != "")
                    {
                        string newline = GetLineSubtitles(line);
                        newList.Add(newline);
                    }
                }
                
                if (origList.Count == newList.Count)
                {
                    hasProcessed = true;
                }
            }

            if (hasProcessed)
            {
                File.WriteAllBytes(path.Replace("locpack", "locpackbin"), Convert.FromHexString(string.Join("", newList)));
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
            
            string uid  = splitLine[0];
            int lineVersion = int.Parse(splitLine[1]);
            int maxLength = int.Parse(splitLine[2]);
            string text = splitLine[3];

            if (text.StartsWith('"'))
            {
                text = text.Substring(1, text.Length - 2); // Remove quotation marks from beginning and end of line
            }
            
            text = text.Replace("\"\"", "\""); // Any line with double quotation marks must be replaced with single

            if (text.StartsWith('/'))
            {
                text = text.TrimStart('/'); // Remove forward slash from beginning of line if present
            }

            string hexGuid = GetGuid(uid);
            string hexLineVersion = GetIntHex(lineVersion);
            string hexMaxLength = GetIntHex(maxLength);
            string hexText = GetStringHex(text);
            
            int textLength = GetHexLength(hexText);
            string hexTextLength = GetIntHex(textLength);

            if (textLength == 0)
            {
                hexMaxLength = "0000"; // If text is empty, zero out hexMaxLength
            }
            
            string hexZeroed8 = "00000000";
            
            if (lineVersion.ToString().StartsWith('-'))
            {
                hexZeroed8 = "0000";
            }
            
            string newLine = hexGuid + hexLineVersion + hexMaxLength + hexZeroed8 + hexTextLength + hexText;
            
            return newLine; 
        }

        private static string GetLineSubtitles(string line)
        {
            string[] splitLine = line.Split([','], 6);
            
            string uid  = splitLine[0];
            int unk0 = int.Parse(splitLine[1]);
            int unk1 = int.Parse(splitLine[2]);
            int unk2 = int.Parse(splitLine[3]);
            int unk3 = int.Parse(splitLine[4]);
            string text = splitLine[5];

            if (text.StartsWith('"'))
            {
                text = text.Substring(1, text.Length - 2);  // Remove quotation marks from start and end of line
            }
            
            text = text.Replace("\"\"", "\""); // Any line with double quotation marks must be replaced with single

            if (text.StartsWith('/'))
            {
                text = text.TrimStart('/'); // Remove forward slash from line if present
            }

            string hexGuid = GetGuid(uid);
            string hexUnk0 = GetIntHex(unk0);
            string hexUnk1 = GetIntHex(unk1);
            string hexUnk2 = GetIntHex(unk2);
            string hexUnk3 = GetIntHex(unk3);
            string hexText = GetStringHex(text);
            
            int textLength = GetHexLength(hexText);
            string hexTextLength = GetIntHex(textLength);

            string hexZeroed4 = "0000"; 

            if (unk0.ToString().StartsWith('-'))
            {
                hexZeroed4 = "";
            }

            string hexZeroed2 = "";
            string hexZeroed8 = "00000000";
            
            if (unk0 == 4 & textLength == 0)
            {
                hexUnk2 = "0004";
                hexZeroed2 = "00";
                hexZeroed8 = "000000";
            }
            
            string newLine = hexGuid + hexUnk0 + hexZeroed4 + hexUnk1 + hexZeroed2 + hexUnk2 + hexZeroed8 + hexUnk3 + "0000" + hexTextLength + hexText;

            if (unk0 != 4 & textLength == 0)
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
            
            string newGuid = group2 + group1 + group0 + group4 + group3; // Correct Order
            
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
