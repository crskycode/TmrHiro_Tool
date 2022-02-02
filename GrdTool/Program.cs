using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GrdTool
{
    public static class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Tmr-Hiro GRD Tool");
                Console.WriteLine("  -- Created by Crsky");
                Console.WriteLine("Usage:");
                Console.WriteLine("  Extract   : GrdTool -e [image.grd|folder]");
                Console.WriteLine("  Create    : GrdTool -c [image.png|folder]");
                Console.WriteLine();
                Console.WriteLine("Help:");
                Console.WriteLine("  This tool is only works with 'Tmr-Hiro GRD' files,");
                Console.WriteLine("    please check the engine first.");
                Console.WriteLine("  Metadata (.metadata.json) and image (.png) are required to build GRD.");
                Console.WriteLine();
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
                return;
            }

            var mode = args[0];
            var path = Path.GetFullPath(args[1]);

            switch (mode)
            {
                case "-e":
                {
                    void Extract(string filePath)
                    {
                        Console.WriteLine($"Extracting metadata from {Path.GetFileName(filePath)}");

                        try
                        {
                            var jsonPath = Path.ChangeExtension(path, ".metadata.json");
                            Grd.ExtractMetadata(path, jsonPath);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                        }
                    }

                    if (Utility.PathIsFolder(path))
                    {
                        foreach (var item in Directory.EnumerateFiles(path, "*.grd"))
                        {
                            Extract(item);
                        }
                    }
                    else
                    {
                        Extract(path);
                    }

                    break;
                }
                case "-c":
                {
                    void Create(string filePath)
                    {
                        Console.WriteLine($"Creating image from {Path.GetFileName(filePath)}");

                        try
                        {
                            var grdPath = Path.ChangeExtension(filePath, ".new.grd");
                            var jsonPath = Path.ChangeExtension(filePath, ".metadata.json");
                            Grd.Create(grdPath, filePath, jsonPath);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                        }
                    }

                    if (Utility.PathIsFolder(path))
                    {
                        foreach (var item in Directory.EnumerateFiles(path, "*.png"))
                        {
                            Create(item);
                        }
                    }
                    else
                    {
                        Create(path);
                    }

                    break;
                }
            }
        }
    }
}
