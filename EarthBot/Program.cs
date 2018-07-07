using System;
using System.Collections.Generic;
using System.IO;
using BingMapsRESTToolkit;
using EarthBot.Services;
using System.Drawing;
using System.Net.Mime;
using Microsoft.Extensions.Configuration;
using System.Linq;
using Microsoft.Extensions.FileSystemGlobbing.Internal;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Filters;
using SixLabors.ImageSharp.Processing.Transforms;

namespace EarthBot
{
    class Program
    {
        public static IConfiguration Configuration { get; set; }

        static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");

            Configuration = builder.Build();

            BotService mainService = new BotService(Configuration);
            mainService.Init();
        }
    }
}