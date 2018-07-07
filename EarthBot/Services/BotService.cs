﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Microsoft.Extensions.Configuration;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using Tweetinvi;
using Tweetinvi.Logic.Model;
using Tweetinvi.Models;
using Tweetinvi.Parameters;

namespace EarthBot.Services
{
    public class BotService
    {
        private readonly IConfiguration _configuration;
        private readonly ImageService _imageService;
        private readonly string[] _symbols;

        public BotService(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _imageService = new ImageService(_configuration);

            _symbols = _configuration["TweetSymbols"].Split(' ');

            string customerKey = _configuration["TwitterApiKeys:ConsumerKey"];
            string customerKeySecret = _configuration["TwitterApiKeys:ConsumerKeySecret"];
            string AccessToken = _configuration["TwitterApiKeys:AccessToken"];
            string AccessTokenSecret = _configuration["TwitterApiKeys:AccessTokenSecret"];

            Auth.SetUserCredentials(customerKey, customerKeySecret, AccessToken, AccessTokenSecret);
        }

        public void Init()
        {
            while (true)
            {
                TryPublishImage();
                Thread.Sleep(new TimeSpan(0, 5, 0));
            }
        }

        private async void TryPublishImage()
        {
            var random = new Random();
            var pictureToPost = await _imageService.GetPostableImage();
            var stream = new MemoryStream();

            pictureToPost.GetObject().SaveAsJpeg(stream);

            string symbol = _symbols[random.Next(0, _symbols.Length - 1)];
            var coordinates = new Coordinates(pictureToPost.GetLatitude(), pictureToPost.GetLongitude());
            var parameters =
                new PublishTweetParameters(
                        $"{symbol}    ({coordinates.Latitude:0.#####}, {coordinates.Longitude:0.#####})")
                    {MediaBinaries = {stream.GetBuffer()}, Coordinates = coordinates};

            Tweet.PublishTweet(parameters);
        }
    }
}