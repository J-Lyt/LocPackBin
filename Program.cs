// See https://aka.ms/new-console-template for more information
using LocPackBin;

if (args.Length > 0 && File.Exists(args[0]))
{
    string[] allowedFiles = ["menus", "subtitles"];
    
    var path = args[0];

    if (allowedFiles.Any(path.Contains) & Path.GetExtension(path) == ".locpack")
    {
        Console.WriteLine($"Converting: {Path.GetFileName(path)}");
        Console.WriteLine();
        Converter.ProcessFile(path);
        Console.WriteLine();
        Console.WriteLine("Press any key to exit...");
        Console.ReadLine();
    }
    else if (allowedFiles.Any(path.Contains) & Path.GetExtension(path) == ".locpackbin")
    {
        Console.WriteLine($"Error: {Path.GetFileName(path)} has already been converted.");
        Console.WriteLine();
        Console.WriteLine("Press any key to exit...");
        Console.ReadLine();
    }
    else
    {
        Console.WriteLine($"Error: {Path.GetFileName(path)} is not an accepted file");
        Console.WriteLine();
        Console.WriteLine("Press any key to exit...");
        Console.ReadLine();
    }
}
