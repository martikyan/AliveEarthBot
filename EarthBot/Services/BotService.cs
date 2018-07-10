using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using Tweetinvi;
using Tweetinvi.Models;
using Tweetinvi.Parameters;

namespace EarthBot.Services {
    public class BotService {
        private readonly IConfiguration _configuration;
        private readonly ImageService _imageService;
        private readonly ILogger _logger;
        private readonly string[] _symbols;

        public BotService(IConfiguration configuration, ILogger<BotService> logger) {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _imageService = new ImageService(_configuration, _logger);

            _symbols = _configuration["TweetSymbols"].Split(' ');

            var customerKey = _configuration["TwitterApiKeys:ConsumerKey"];
            var customerKeySecret = _configuration["TwitterApiKeys:ConsumerKeySecret"];
            var accessToken = _configuration["TwitterApiKeys:AccessToken"];
            var accessTokenSecret = _configuration["TwitterApiKeys:AccessTokenSecret"];

            Auth.SetUserCredentials(customerKey, customerKeySecret, accessToken, accessTokenSecret);
        }

        public void Init() {
            var hoursToSleep = int.Parse(_configuration["SleepTimeInHours"]);

            if (hoursToSleep == 0) {
                var e = new ArgumentNullException(nameof(hoursToSleep));
                _logger.LogError(e.Message);

                throw e;
            }

            while (true) {
                new Task(TryPublishImage).Start();

                _logger.LogInformation($"Main thread is sleeping for {hoursToSleep} hours");
                Thread.Sleep(new TimeSpan(0, hoursToSleep, 0, 0));
            }
        }

        private async void TryPublishImage() {
            var random = new Random();
            var stream = new MemoryStream();
            var pictureToPost = await _imageService.GetPostableImage();

            pictureToPost.GetObject().SaveAsJpeg(stream);

            var symbol = _symbols[random.Next(0, _symbols.Length - 1)];
            var coordinates = new Coordinates(pictureToPost.GetLatitude(), pictureToPost.GetLongitude());
            var parameters =
                new PublishTweetParameters(
                        $"{symbol}    ({coordinates.Latitude:0.######}, {coordinates.Longitude:0.######})")
                    {MediaBinaries = {stream.GetBuffer()}, Coordinates = coordinates};

            _logger.LogInformation($"Tweeting {parameters.Text}");
            Tweet.PublishTweet(parameters);
        }
    }
}