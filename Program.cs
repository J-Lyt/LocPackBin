// See https://aka.ms/new-console-template for more information
using LocPackBin;

Console.WriteLine("Snowdrop LocPack Converter");
Console.WriteLine();

if (args.Length == 0)
{
    Console.WriteLine("No file specified");
    Console.WriteLine("Drag and drop a .locpack or .locpackbin file onto the executable");
}
else if (File.Exists(args[0]))
{
    var path = args[0];
    var ext = Path.GetExtension(path);
    
    if (ext is ".locpack" or ".locpackbin")
    {
        Console.WriteLine($"Converting: {Path.GetFileName(path)}");
        Console.WriteLine();
    }
    
    if (ext == ".locpack")
    {
        LocPackConverter.FileToBin(path);
    }
    else if (ext == ".locpackbin")
    {
        LocPackConverter.FileFromBin(path);
    }
    else
    {
        Console.WriteLine($"Error: {Path.GetFileName(path)} is not an accepted file");
    }
}
else
{
    Console.WriteLine($"Error: {args[0]} could not be found");
}

Console.WriteLine();
Console.WriteLine("Press any key to exit...");
Console.ReadLine();
