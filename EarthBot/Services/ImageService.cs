using System.Configuration;
using System;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using BingMapsRESTToolkit;
using Microsoft.Extensions.Configuration;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Filters;
using SixLabors.ImageSharp.Processing.Transforms;


namespace EarthBot.Services
{
    public class ImageService
    {
        private IConfiguration _configuration { get; set; }
        private const double _latitudeMaxModulus = 85.0d;
        private const double _longtitudeMaxModulus = 180.0d;
        private const int _averageability = 50;
        private const int _similarity = 45;

        public ImageService(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public string GeneratePictureURL()
        {
            var rnd = new Random();
            var latitude = rnd.NextDouble() * 2 * _latitudeMaxModulus - _latitudeMaxModulus;
            var longtitude = rnd.NextDouble() * 2 * _longtitudeMaxModulus - _longtitudeMaxModulus;

            var zoomLevel = rnd.Next(8, 16);
            var request = new ImageryRequest();

            request.CenterPoint = new Coordinate(latitude, longtitude);
            request.ZoomLevel = zoomLevel;
            request.BingMapsKey = _configuration["BingMapsApiKey"];
            request.Resolution = ImageResolutionType.High;
            request.MapHeight = 2000;
            request.MapWidth = 2000;

            return request.GetPostRequestUrl();
        }

        public Rgba32 GetAveragePixel(Image<Rgba32> image)
        {
            var result = new Rgba32();
            int sumR = 0, sumG = 0, sumB = 0;
            int counter = 0;

            for (int i = 0; i < image.Height; i += image.Height / _averageability)
            {
                for (int j = 0; j < image.Width; j += image.Width / _averageability)
                {
                    counter++;
                    sumR += image[j, i].R;
                    sumG += image[j, i].G;
                    sumB += image[j, i].B;
                }
            }

            result.R = Convert.ToByte(sumR / counter);
            result.G = Convert.ToByte(sumG / counter);
            result.B = Convert.ToByte(sumB / counter);

            return result;
        }

        public int GetDifference(Image<Rgba32> a, Image<Rgba32> b)
        {
            var a_main = GetAveragePixel(a);
            var b_main = GetAveragePixel(b);
            var diff = 0;

            diff += Math.Abs(((int) b_main.R) - ((int) a_main.R));
            diff += Math.Abs(((int) b_main.G) - ((int) a_main.G));
            diff += Math.Abs(((int) b_main.B) - ((int) a_main.B));
            
            return diff;
        }

        public async Task<Image<Rgba32>> DownloadImage(string url)
        {
            using (WebClient client = new WebClient())
            {
                var result = await client.DownloadDataTaskAsync(url);
                return Image.Load(result);
            }
        }
        
        public Image<Rgba32> ReadImage(string path)
        {
            return Image.Load(path);
        }

        public bool IsPostable(Image<Rgba32> image)
        {
            var sea = ReadImage(_configuration["ImagesPaths:Sea"]);

            if (GetDifference(image, sea) < _similarity)
            {
                return false;
            }

            var badImage = ReadImage(_configuration["ImagesPaths:BadImage"]);
            
            if (GetDifference(image, badImage) < _similarity)
            {
                return false;
            }

            return true;
        }

        public async Task<Image<Rgba32>> GetPostableImage()
        {
            while (true)
            {
                string url = GeneratePictureURL();
                var picture = await DownloadImage(url);

                if (IsPostable(picture))
                {
                    CropImage(picture);
                    Console.WriteLine("got postable image!");
                    return picture;
                }
            }
        }

        public void CropImage(Image<Rgba32> image)
        {
            image.Mutate(img => img.Crop(image.Width ,image.Height - 25));
        }
    }
}