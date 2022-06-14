namespace Retweety.Services
{
    using System;
    using System.Timers;
    using System.Threading.Tasks;

    using Tweetinvi;
    using Tweetinvi.Models;
    using Tweetinvi.Parameters;
    using Tweetinvi.Streaming;
    using Tweetinvi.Streaming.Parameters;

    using Retweety.Configuration;
    using Retweety.Diagnostics;
    using Retweety.Net;
    using Retweety.Net.Webhooks.Models;

    public class TweetService
    {
        #region Variables

        private readonly Config _config;
        private readonly IEventLogger _logger;
        private IFilteredStream _twitterStream;
        private ITwitterClient _client;
        private readonly Timer _timer = new();

        #endregion

        #region Constructor(s)

        /// <summary>
        /// 
        /// </summary>
        /// <param name="config"></param>
        public TweetService(Config config)
            : this(config, new EventLogger(Program.OnLogEvent))
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="config"></param>
        /// <param name="logger"></param>
        public TweetService(Config config, IEventLogger logger)
        {
            _config = config;
            _logger = logger;

            // Timer to check and validate filter stream is still active,
            // check every 5 minutes.
            var interval = 1000 * 60 * Strings.ValidationIntervalM;
            _timer.Interval = interval;
            _timer.Elapsed += async (sender, e) => await TweetUpdateHandler();

            Init();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Start retweet filter stream to check for new tweets
        /// </summary>
        public void Start()
        {
            _logger.Trace("TweetService::Start");

            // Enable timer if not already enabled
            if (!_timer.Enabled)
            {
                _timer.Start();

                // Check if interested users to follow have been added,
                // if not add them to filter stream.
                AddTwitterFollowers();
            }
        }

        /// <summary>
        /// Stop retweet filter stream
        /// </summary>
        public void Stop()
        {
            _logger.Trace("TweetService::Stop");

            // Stop tweet stream if set
            if (_twitterStream != null)
            {
                _twitterStream.Stop();
            }

            // Stop timer if enabled
            if (_timer.Enabled)
            {
                _timer.Stop();
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Initialize and authenticate the <see cref="TweetService"/>
        /// class.
        /// </summary>
        private void Init()
        {
            // Create twitter credentials object and pass to twitter client
            // for initialization
            var credentials = CreateCredentials();
            _client = new TwitterClient(credentials);

            // Subscribe to client events
            TweetinviEvents.SubscribeToClientEvents(_client);

            // Create and initialize filter stream
            _twitterStream = _client.Streams.CreateFilteredStream(new CreateFilteredTweetStreamParameters
            {
                TweetMode = TweetMode.Extended,
            });
            //_twitterStream.Credentials = credentials;
            _twitterStream.StallWarnings = true;
            _twitterStream.FilterLevel = StreamFilterLevel.None;
            _twitterStream.StreamStarted += (sender, e) => _logger.Debug("Stream started successfully.");
            _twitterStream.StreamStopped += (sender, e) => _logger.Debug($"Stream stopped.\r\n{e.Exception}\r\n{e.DisconnectMessage}");
            _twitterStream.DisconnectMessageReceived += (sender, e) => _logger.Warn($"Stream disconnected.\r\n{e.DisconnectMessage}");
            _twitterStream.WarningFallingBehindDetected += (sender, e) => _logger.Warn($"Stream warning falling behind detected: {e.WarningMessage}");
        }

        /// <summary>
        /// Create a twitter credentials object from the config
        /// options.
        /// </summary>
        /// <returns></returns>
        private TwitterCredentials CreateCredentials()
        {
            _logger.Trace("TweetService::SetCredentials");

            // Validate config credentials are set
            ValidateCredentials();

            var credentials = new TwitterCredentials(
                _config.ConsumerKey,
                _config.ConsumerSecret,
                _config.AccessToken,
                _config.AccessTokenSecret
            );
            if (credentials == null)
            {
                _logger.Error($"Failed to create TwitterCredentials object.");
                return null;
            }

            //Auth.SetCredentials(credentials);
            return credentials;
        }

        /// <summary>
        /// Validate all twitter credential properties are set.
        /// </summary>
        private void ValidateCredentials()
        {
            var failed = false;
            if (string.IsNullOrEmpty(_config.ConsumerKey))
            {
                _logger.Error(new NullReferenceException($"'ConsumerKey' must be set"));
                failed = true;
            }
            if (string.IsNullOrEmpty(_config.ConsumerSecret))
            {
                _logger.Error(new NullReferenceException($"'ConsumerSecret' must be set"));
                failed = true;
            }
            if (string.IsNullOrEmpty(_config.AccessToken))
            {
                _logger.Error(new NullReferenceException($"'AccessToken' must be set"));
                failed = true;
            }
            if (string.IsNullOrEmpty(_config.AccessTokenSecret))
            {
                _logger.Error(new NullReferenceException($"'AccessTokenSecret' must be set"));
                failed = true;
            }
            if ((_config.TwitterAccounts?.Count ?? 0) == 0)
            {
                _logger.Error(new Exception($"'Accounts' must be set"));
                failed = true;
            }
            if (failed)
            {
                Environment.FailFast($"Invalid twitter credentials provided or no user specified to follow, exiting...");
            }
        }

        /// <summary>
        /// Add twitter followers to filter stream follow list.
        /// </summary>
        private void AddTwitterFollowers()
        {
            // Check if filter stream set
            if (_twitterStream == null)
                return;

            // Loop through all configured retweet followers and start
            // following via filter stream
            foreach (var (userId, webhooks) in _config.TwitterAccounts)
            {
                // Check if we're already following user, if so skip to next...
                if (_twitterStream.ContainsFollow((long)userId))
                    continue;

                // Add user to follow stream
                _logger.Info($"Adding user '{userId}' to retweet follow list...");
                _twitterStream.AddFollow((long)userId, tweet =>
                {
                    // Skip user if post not created by them
                    if (userId != (ulong)tweet.CreatedBy.Id)
                        return;

                    // Skip user retweets of other users
                    // if (tweet.IsRetweet)
                    //     return;

                    SendTwitterWebhook(tweet);
                });
            }
        }

        /// <summary>
        /// Send webhook for user tweeted message.
        /// </summary>
        /// <param name="tweet">Tweet to send.</param>
        private void SendTwitterWebhook(ITweet tweet)
        {
            // Skip retweets if not enabled in config
            if (!_config.Enabled)
                return;

            var userId = (ulong)tweet.CreatedBy.Id;
            _logger.Debug($"Tweet [Owner={tweet.CreatedBy.Name} ({userId}), Url={tweet.Url}]");

            // Check if tweet author is in configured followers list
            if (!_config.TwitterAccounts.ContainsKey(userId))
            {
                _logger.Error($"User '{userId}' is not in configured retweet list, skipping...");
                return;
            }

            // Build the embed once then loop all webhooks
            var embed = new DiscordWebhookMessage
            {
                Username = _config.Bot?.Name ?? Strings.BotName,
                AvatarUrl = _config.Bot?.IconUrl ?? Strings.BotIconUrl,
                Content = tweet.Url
            };
            var json = embed.Build();

            // Get webhooks to send to for followed user
            var webhooks = _config.TwitterAccounts[userId];

            // Loop through all configured retweet follower webhooks
            foreach (var webhook in webhooks)
            {
                _logger.Debug($"Sending webhook for twitter url {tweet.Url} to webhook address {webhook}");
                // Send retweet to webhook
                NetUtils.SendWebhook(webhook, json);
            }
        }

        /// <summary>
        /// Validation method to ensure our filtered stream is
        /// always active.
        /// </summary>
        /// <returns></returns>
        private async Task TweetUpdateHandler()
        {
            // Check if filter stream set
            if (_twitterStream == null)
                return;

            // Check stream state, if stopped then restart filter stream, if paused resume.
            switch (_twitterStream.StreamState)
            {
                case StreamState.Running:
                    // Stream running, ignore it
                    break;
                case StreamState.Pause:
                    // Stream paused, resume it
                    _twitterStream.Resume();
                    break;
                case StreamState.Stop:
                    // Stream stopped, start it
                    await _twitterStream.StartMatchingAllConditionsAsync();
                    break;
            }
        }

        #endregion
    }
}