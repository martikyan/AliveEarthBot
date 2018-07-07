using System;
using System.Net;
using System.Threading.Tasks;
using BingMapsRESTToolkit;
using EarthBot.Models;
using Microsoft.Extensions.Configuration;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Transforms;

namespace EarthBot.Services
{
    public class ImageService
    {
        private const double LatitudeMaxModulus = 85.0d;
        private const double LongtitudeMaxModulus = 180.0d;
        private const int Averageability = 50;
        private const int Similarity = 45;

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
            var zoomLevel = rnd.Next(8, 18);
            var request = new ImageryRequest();

            request.CenterPoint = new Coordinate(latitude, longtitude);
            request.ZoomLevel = zoomLevel;
            request.BingMapsKey = Configuration["BingMapsApiKey"];
            request.Resolution = ImageResolutionType.High;
            request.MapHeight = int.Parse(Configuration["MediaOptions:Height"]);
            request.MapWidth = int.Parse(Configuration["MediaOptions:Width"]);

            return new LocatedObject<string>(request.GetPostRequestUrl(), latitude, longtitude);
        }

        private Rgba32 GetAveragePixel(Image<Rgba32> image)
        {
            var result = new Rgba32();
            int sumR = 0, sumG = 0, sumB = 0;
            var counter = 0;

            for (var i = 0; i < image.Height; i += image.Height / Averageability)
            for (var j = 0; j < image.Width; j += image.Width / Averageability)
            {
                counter++;
                sumR += image[j, i].R;
                sumG += image[j, i].G;
                sumB += image[j, i].B;
            }

            result.R = Convert.ToByte(sumR / counter);
            result.G = Convert.ToByte(sumG / counter);
            result.B = Convert.ToByte(sumB / counter);

            return result;
        }

        private int GetDifference(Image<Rgba32> a, Image<Rgba32> b)
        {
            var aMain = GetAveragePixel(a);
            var bMain = GetAveragePixel(b);
            var diff = 0;

            diff += Math.Abs(bMain.R - aMain.R);
            diff += Math.Abs(bMain.G - aMain.G);
            diff += Math.Abs(bMain.B - aMain.B);

            return diff;
        }

        private async Task<Image<Rgba32>> DownloadImage(string url)
        {
            using (var client = new WebClient())
            {
                var result = await client.DownloadDataTaskAsync(url);
                return Image.Load(result);
            }
        }

        private Image<Rgba32> ReadImage(string path)
        {
            return Image.Load(path);
        }

        private bool IsPostable(Image<Rgba32> image)
        {
            var sea = ReadImage("UnwantedImages/sea.jpeg");

            if (GetDifference(image, sea) < Similarity) return false;

            var badImage = ReadImage("UnwantedImages/badImage.jpeg");

            if (GetDifference(image, badImage) < Similarity) return false;

            return true;
        }

        public async Task<LocatedObject<Image<Rgba32>>> GetPostableImage()
        {
            while (true)
            {
                var url = GeneratePictureUrl();
                var picture = await DownloadImage(url.GetObject());

                if (IsPostable(picture))
                {
                    CropImage(picture);
                    return new LocatedObject<Image<Rgba32>>(picture, url.GetLatitude(), url.GetLongitude());
                }
            }
        }

        private void CropImage(Image<Rgba32> image)
        {
            image.Mutate(img => img.Crop(image.Width, image.Height - 25));
        }
    }
}