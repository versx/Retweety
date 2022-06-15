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
            if (_timer.Enabled)
                return;

            _timer.Start();

            // Check if interested users to follow have been added,
            // if not add them to filter stream.
            AddTwitterFollowers();
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
            var userId = (ulong)tweet.CreatedBy.Id;
            _logger.Debug($"Tweet [Owner={tweet.CreatedBy.Name} ({userId}), Url={tweet.Url}]");

            // Check if tweet author is in configured followers list
            if (!_config.TwitterAccounts.ContainsKey(userId))
            {
                _logger.Error($"User '{userId}' is not in configured retweet list, skipping...");
                return;
            }

            // Parse Discord embed via user defined template
            var templateData = ParseEmbedTemplate(tweet, _config.EmbedTemplate ?? Strings.DefaultEmbedTemplate);

            // Build the embed once then loop all webhooks
            var embed = new DiscordWebhookMessage
            {
                Username = _config.Bot?.Name ?? Strings.BotName,
                AvatarUrl = _config.Bot?.IconUrl ?? Strings.BotIconUrl,
                Content = templateData,
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

        /// <summary>
        /// Parse Tweet embed message via user defined templating keys
        /// </summary>
        /// <param name="tweet">Tweet model</param>
        /// <param name="template">Template</param>
        /// <returns></returns>
        private static string ParseEmbedTemplate(ITweet tweet, string template)
        {
            var model = new
            {
                // TODO: Add more properties
                //tweet.Contributors
                //tweet.ContributorsIds
                //tweet.Coordinates
                created_at = tweet.CreatedAt,
                //tweet.CreatedBy (IUser)
                //tweet.CurrentUserRetweetIdentifier
                //tweet.Entities
                //tweet.ExtendedTweet
                favorite_count = tweet.FavoriteCount,
                favorited = tweet.Favorited,
                filter_level = tweet.FilterLevel,
                full_text = tweet.FullText,
                //tweet.Hashtags
                id = tweet.Id,
                //tweet.InReplyToScreenName
                //tweet.InReplyToStatusId
                //tweet.InReplyToUserId
                is_retweet = tweet.IsRetweet,
                language = tweet.Language,
                //tweet.Media
                //tweet.Place
                possibly_sensitive = tweet.PossiblySensitive,
                prefix = tweet.Prefix,
                quote_count = tweet.QuoteCount,
                //tweet.QuotedStatusId
                //tweet.QuotedTweet
                reply_count = tweet.ReplyCount,
                retweet_count = tweet.RetweetCount,
                retweeted = tweet.Retweeted,
                //tweet.RetweetedTweet
                //tweet.SafeDisplayTextRange
                //tweet.Scopes
                //tweet.Source
                suffix = tweet.Suffix,
                text = tweet.Text,
                truncated = tweet.Truncated,
                //tweet.TweetDTO
                tweet_mode = tweet.TweetMode,
                url = tweet.Url,
                //tweet.Urls
                //tweet.UserMentions
                //tweet.WithheldCopyright
                //tweet.WithheldInCountries
                //tweet.WithheldScope
            };
            var templateData = TemplateRenderer.Parse(template, model);
            return templateData;
        }

        #endregion
    }
}

#region ITweet Properties

/*
//
// Summary:
//     Client used by the instance to perform any request to Twitter
ITwitterClient Client { get; set; }

//
// Summary:
//     Creation date of the Tweet
DateTimeOffset CreatedAt { get; }

//
// Summary:
//     Formatted text of the tweet.
string Text { get; }

//
// Summary:
//     Prefix of an extended tweet.
string Prefix { get; }

//
// Summary:
//     Suffix of an extended tweet.
string Suffix { get; }

//
// Summary:
//     Full text of an extended tweet.
string FullText { get; }

//
// Summary:
//     Content display text range for FullText.
int[] DisplayTextRange { get; }

//
// Summary:
//     The range of text to be displayed for any Tweet. If this is an Extended Tweet,
//     this will be the range supplied by Twitter. If this is an old-style 140 character
//     Tweet, the range will be 0 - Length.
int[] SafeDisplayTextRange { get; }

//
// Summary:
//     Extended Tweet details.
IExtendedTweet ExtendedTweet { get; }

//
// Summary:
//     Coordinates of the location from where the tweet has been sent
ICoordinates Coordinates { get; }

//
// Summary:
//     source field
string Source { get; }

//
// Summary:
//     Whether the tweet text was truncated because it was longer than 140 characters.
bool Truncated { get; }

//
// Summary:
//     Number of times this Tweet has been replied to This property is only available
//     with the Premium and Enterprise tier products.
int? ReplyCount { get; }

//
// Summary:
//     In_reply_to_status_id
long? InReplyToStatusId { get; }

//
// Summary:
//     In_reply_to_status_id_str
string InReplyToStatusIdStr { get; }

//
// Summary:
//     In_reply_to_user_id
long? InReplyToUserId { get; }

//
// Summary:
//     In_reply_to_user_id_str
string InReplyToUserIdStr { get; }

//
// Summary:
//     In_reply_to_screen_name
string InReplyToScreenName { get; }

//
// Summary:
//     User who created the Tweet
IUser CreatedBy { get; }

//
// Summary:
//     Details the Tweet ID of the user's own retweet (if existent) of this Tweet.
ITweetIdentifier CurrentUserRetweetIdentifier { get; }

//
// Summary:
//     Ids of the users who contributed in the Tweet
int[] ContributorsIds { get; }

//
// Summary:
//     Users who contributed to the authorship of the tweet, on behalf of the official
//     tweet author.
IEnumerable<long> Contributors { get; }

//
// Summary:
//     Number of retweets related with this tweet
int RetweetCount { get; }

//
// Summary:
//     Extended entities in the tweet. Used by twitter for multiple photos
ITweetEntities Entities { get; }

//
// Summary:
//     Is the tweet Favorited
bool Favorited { get; }

//
// Summary:
//     Number of time the tweet has been Favorited
int FavoriteCount { get; }

//
// Summary:
//     Has the tweet been retweeted
bool Retweeted { get; }

//
// Summary:
//     Is the tweet potentialy sensitive
bool PossiblySensitive { get; }

//
// Summary:
//     Main language used in the tweet
Language? Language { get; }

//
// Summary:
//     Geographic details concerning the location where the tweet has been published
IPlace Place { get; }

//
// Summary:
//     Informed whether a tweet is displayed or not in a specific type of scope. This
//     property is most of the time null.
Dictionary<string, object> Scopes { get; }

//
// Summary:
//     Streaming tweets requires a filter level. A tweet will be streamed if its filter
//     level is higher than the one of the stream
string FilterLevel { get; }

//
// Summary:
//     Informs that a tweet has been withheld for a copyright reason
bool WithheldCopyright { get; }

//
// Summary:
//     Countries in which the tweet will be withheld
IEnumerable<string> WithheldInCountries { get; }

//
// Summary:
//     When present, indicates whether the content being withheld is the "status" or
//     a "user."
string WithheldScope { get; }

//
// Summary:
//     Property used to store the data received from Twitter
ITweetDTO TweetDTO { get; }

//
// Summary:
//     Collection of hashtags associated with a Tweet
List<IHashtagEntity> Hashtags { get; }

//
// Summary:
//     Collection of urls associated with a tweet
List<IUrlEntity> Urls { get; }

//
// Summary:
//     Collection of medias associated with a tweet
List<IMediaEntity> Media { get; }

//
// Summary:
//     Collection of tweets mentioning this tweet
List<IUserMentionEntity> UserMentions { get; }

//
// Summary:
//     Indicates whether the current tweet is a retweet of another tweet
bool IsRetweet { get; }

//
// Summary:
//     If the tweet is a retweet this field provides the tweet that it retweeted
ITweet RetweetedTweet { get; }

//
// Summary:
//     Indicates approximately how many times this Tweet has been quoted by Twitter
//     users. This property is only available with the Premium and Enterprise tier products.
int? QuoteCount { get; }

//
// Summary:
//     Tweet Id that was retweeted with a quote
long? QuotedStatusId { get; }

//
// Summary:
//     Tweet Id that was retweeted with a quote
string QuotedStatusIdStr { get; }

//
// Summary:
//     Tweet that was retweeted with a quote
ITweet QuotedTweet { get; }

//
// Summary:
//     URL of the tweet on twitter.com
string Url { get; }

TweetMode TweetMode { get; }
*/

#endregion