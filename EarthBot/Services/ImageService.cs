using System;
using System.Net;
using System.Threading.Tasks;
using BingMapsRESTToolkit;
using EarthBot.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Transforms;

namespace EarthBot.Services {
    public class ImageService {
        private const double LatitudeMaxModulus = 85.0d;
        private const double LongitudeMaxModulus = 180.0d;
        private const long MinImageSize = 128000;
        private readonly IConfiguration _configuration;
        private readonly ILogger _logger;

        public ImageService(IConfiguration configuration, ILogger logger) {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private LocatedObject<string> GeneratePictureUrl() {
            var rnd = new Random();
            var latitude = rnd.NextDouble() * 2 * LatitudeMaxModulus - LatitudeMaxModulus;
            var longitude = rnd.NextDouble() * 2 * LongitudeMaxModulus - LongitudeMaxModulus;

            var minZoom = int.Parse(_configuration["MediaOptions:MinZoom"]);
            var maxZoom = int.Parse(_configuration["MediaOptions:MaxZoom"]);
            var zoomLevel = Math.Sqrt(rnd.Next(minZoom * minZoom, maxZoom * maxZoom));
            var request = new ImageryRequest();

            request.CenterPoint = new Coordinate(latitude, longitude);
            request.ZoomLevel = (int) Math.Ceiling(zoomLevel);
            request.BingMapsKey = _configuration["BingMapsApiKey"];
            request.Resolution = ImageResolutionType.High;
            request.MapHeight = int.Parse(_configuration["MediaOptions:Height"]);
            request.MapWidth = int.Parse(_configuration["MediaOptions:Width"]);

            _logger.LogInformation(
                $"Generated a picture info\nZoom - {request.ZoomLevel}\nLatitude - {latitude}\nLongitude - {longitude}");
            return new LocatedObject<string>(request.GetPostRequestUrl(), latitude, longitude);
        }

        private async Task<Image<Rgba32>> DownloadImage(string url) {
            var client = new WebClient();

            client.OpenRead(url);
            var bytesTotal = Convert.ToInt64(client.ResponseHeaders["Content-Length"]);

            _logger.LogInformation($"Got an image with size {bytesTotal} from url: {url}");

            if (bytesTotal < MinImageSize) {
                client.Dispose();
                return null;
            }


            _logger.LogInformation($"Downloading an image from {url}");
            var result = await client.DownloadDataTaskAsync(url);
            client.Dispose();
            return Image.Load(result);
        }

        public async Task<LocatedObject<Image<Rgba32>>> GetPostableImage() {
            while (true) {
                LocatedObject<string> url;
                Image<Rgba32> picture;

                do {
                    url = GeneratePictureUrl();
                    picture = await DownloadImage(url.GetObject());
                } while (picture == null);

                CropImage(picture);
                return new LocatedObject<Image<Rgba32>>(picture, url.GetLatitude(), url.GetLongitude());
            }
        }

        private static void CropImage(Image<Rgba32> image) {
            image.Mutate(img => img.Crop(image.Width, image.Height - 25));
        }
    }
}