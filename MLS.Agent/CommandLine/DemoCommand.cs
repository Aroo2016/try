﻿using System;
using System.CommandLine;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace MLS.Agent.CommandLine
{
    public static class DemoCommand
    {
        public static async Task<int> Do(
            DemoOptions options,
            IConsole console)
        {
            if (!options.Output.Exists)
            {
                options.Output.Create();
            }
            else
            {
                if (options.Output.GetFiles().Any())
                {
                    console.Out.WriteLine($"Directory {options.Output} must be empty.");
                    return -1;
                }
            }

            var assembly = typeof(Program).Assembly;

            using (var disposableDirectory = DisposableDirectory.Create())
            {
                using (var resourceStream = assembly.GetManifestResourceStream("demo.zip"))
                {
                    var zipPath = Path.Combine(disposableDirectory.Directory.FullName, "demo.zip");

                    using (var fileStream = new FileStream(zipPath, FileMode.Create, FileAccess.Write))
                    {
                        resourceStream.CopyTo(fileStream);
                    }

                    ZipFile.ExtractToDirectory(zipPath, options.Output.FullName);
                }
            }

            return 0;
        }
    }
}