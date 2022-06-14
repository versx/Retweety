[![Build](https://github.com/versx/Retweety/workflows/.NET%205.0/badge.svg)](https://github.com/versx/Retweety/actions)
[![GitHub Release](https://img.shields.io/github/release/versx/Retweety.svg)](https://github.com/versx/Retweety/releases/)
[![Discord](https://img.shields.io/discord/552003258000998401.svg?label=&logo=discord&logoColor=ffffff&color=7389D8&labelColor=6A7EC2)](https://discord.gg/zZ9h9Xa)  


# Retweety  
Repost tweeted messages from interested Twitter users via Discord webhooks.  

## Prerequisites  
- [.NET 5 SDK or higher](https://dotnet.microsoft.com/en-us/download/dotnet/5.0)  

## Getting Started  

1. Run automated install script:  
```
curl https://dotnet.microsoft.com/download/dotnet/scripts/v1/dotnet-install.sh > dotnet-install.sh && chmod +x install.sh && ./install.sh && rm install.sh
```
2. Head to [Twitter's Developer Portal](https://developer.twitter.com/en/portal/dashboard)  
3. Create a new Twitter App, set name, description, and website, ignore callback url.  
4. Click `Keys and Access Tokens` tab to get your Twitter App credentials.  
5. Input consumer key, consumer secret, access token, and access token secret in `bin/config.json` config file.  
6. Set interested user ID(s) as property key(s) under `accounts` config section.  
7. Set user ID key to take a list of webhook urls that will receive the tweeted message.  
8. Set bot properties under `bot` config section.  
9. Build executable file `dotnet build`.  
10. Start Retweety from `bin` folder: `dotnet Retweety.dll`.  


## Configuration  
```json
{
    // Twitter API consumer key
    "consumerKey": "<TWITTER_ACCOUNT_CONSUMER_KEY>",
    // Twitter API consumer secret
    "consumerSecret": "<TWITTER_ACCOUNT_CONSUMER_SECRET>",
    // Twitter API access token
    "accessToken": "<TWITTER_ACCOUNT_ACCESS_TOKEN>",
    // Twitter API access token secret
    "accessTokenSecret": "<TWITTER_ACCOUNT_TOKEN_SECRET>",
    // Dictionary of interested users to repost tweeted messages
    "accounts": {
        // User ID
        "2839430431": [
            // List of webhooks tweets will be sent to
            "https://discordapp.com/...."
        ]
    },
    // Bot display settings for embed post
    "bot": {
        // Bot name
        "name": "Retweety",
        // Bot icon url
        "iconUrl": ""
    },
}
```


## Twitter Handle Converter  
https://tweeterid.com/  
