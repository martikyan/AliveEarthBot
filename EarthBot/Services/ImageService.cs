using System;
using System.Net;
using System.Threading.Tasks;
using BingMapsRESTToolkit;
using EarthBot.Models;
using Microsoft.Extensions.Configuration;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Filters;
using SixLabors.ImageSharp.Processing.Transforms;

namespace EarthBot.Services
{
    public class ImageService
    {
        private const double LatitudeMaxModulus = 85.0d;
        private const double LongtitudeMaxModulus = 180.0d;
        private const double Detailness = 1.0d;

        public ImageService(IConfiguration configuration)
        {
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        private IConfiguration Configuration { get; }

        private LocatedObject<string> GeneratePictureUrl()
        {
            var rnd = new Random();
            var latitude = rnd.NextDouble() * 2 * LatitudeMaxModulus - LatitudeMaxModulus;
            var longtitude = rnd.NextDouble() * 2 * LongtitudeMaxModulus - LongtitudeMaxModulus;

            var minZoom = int.Parse(Configuration["MediaOptions:MinZoom"]);
            var maxZoom = int.Parse(Configuration["MediaOptions:MaxZoom"]);
            var zoomLevel = rnd.Next(minZoom, maxZoom);
            var request = new ImageryRequest();

            request.CenterPoint = new Coordinate(latitude, longtitude);
            request.ZoomLevel = zoomLevel;
            request.BingMapsKey = Configuration["BingMapsApiKey"];
            request.Resolution = ImageResolutionType.High;
            request.MapHeight = int.Parse(Configuration["MediaOptions:Height"]);
            request.MapWidth = int.Parse(Configuration["MediaOptions:Width"]);

            return new LocatedObject<string>(request.GetPostRequestUrl(), latitude, longtitude);
        }

        private async Task<Image<Rgba32>> DownloadImage(string url)
        {
            using (var client = new WebClient())
            {
                var result = await client.DownloadDataTaskAsync(url);
                return Image.Load(result);
            }
        }

        private bool IsPostable(Image<Rgba32> image)
        {
            var bwImage = image.Clone();
            bwImage.Mutate(i => i.BlackWhite());

            var entropy = GetEntropy(bwImage.SavePixelData());
            return entropy >= Detailness;
        }

        public async Task<LocatedObject<Image<Rgba32>>> GetPostableImage()
        {
            while (true)
            {
                var url = GeneratePictureUrl();
                var picture = await DownloadImage(url.GetObject());

                if (!IsPostable(picture)) continue;
                CropImage(picture);
                return new LocatedObject<Image<Rgba32>>(picture, url.GetLatitude(), url.GetLongitude());
            }
        }

        private static unsafe double GetEntropy(byte[] data)
        {
            int* rgi = stackalloc int[0x100], pi = rgi + 0x100;

            for (var i = data.Length; --i >= 0;)
                rgi[data[i]]++;

            double H = 0.0, cb = data.Length;
            while (--pi >= rgi)
                if (*pi > 0)
                    H += *pi * Math.Log(*pi / cb, 2.0);

            return -H / cb;
        }

        private static void CropImage(Image<Rgba32> image)
        {
            image.Mutate(img => img.Crop(image.Width, image.Height - 25));
        }
    }
}