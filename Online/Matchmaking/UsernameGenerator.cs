using Steamworks;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using static RainMeadow.SteamMatchmakingManager;

namespace RainMeadow
{
    public static class UsernameGenerator
    {
        public static long Timestamp;
        public static Dictionary<string, string> cache = new();

        private static readonly string[] Adjectives =
        {
            "brave", 
            "quick", 
            "silent", 
            "happy", 
            "angry", 
            "noble", 
            "sly", 
            "curious", 
            "wise", 
            "bold", 
            "fearful", 
            "sad", 
            "aloof", 
            "red", 
            "cyan",
            "big",
            "small",
            "sky"
        };

        private static readonly string[] Nouns =
        {
            "slugcat", 
            "vulture", 
            "lizard", 
            "scavenger", 
            "guardian", 
            "noodlefly", 
            "pearl", 
            "rainworld", 
            "shelter", 
            "iterator", 
            "centipede",
            "spider",
            "yeek",
            "barnacle",
            "moth",
            "whale",
            "snail"
        };

        public static string GenerateRandomUsername(int seed)
        {
            Random random = new Random(seed);
            string adjective = Adjectives[random.Next(Adjectives.Length)];
            string noun = Nouns[random.Next(Nouns.Length)];

            return $"{adjective}_{noun}";
        }

        /// <summary>
        /// Automatically returns a randomized username based on the input and the time you launched the game so you get the same name every time.
        /// </summary>
        /// <param name="id"></param>
        /// <returns>A randomized username if you have StreamerMode enabled and are playing via Steam, otherwise MeadowPlayerId.name</returns>
        public static string StreamerModeName(string name)
        {
            if (RainMeadow.rainMeadowOptions.StreamerMode.Value == RainMeadowOptions.StreamMode.None) 
                return name;
            if (RainMeadow.rainMeadowOptions.StreamerMode.Value == RainMeadowOptions.StreamMode.Me && name != OnlineManager.mePlayer.id.name)
                return name;
            if (cache.ContainsKey(name)) 
                return cache[name];
            using (SHA1 sha1  = SHA1.Create())
            {
                var censored = GenerateRandomUsername(BitConverter.ToInt32(sha1.ComputeHash(Encoding.UTF8.GetBytes(name)), 0));
                cache.Add(name, censored);
                return censored;
            }
        }
    }

}