using System;

namespace RainMeadow
{
    public static class UsernameGenerator
    {
        private static readonly string[] Adjectives =
        {
        "brave", "quick", "silent", "happy", "angry", "noble", "sly", "curious", "wise", "bold", "fearful", "sad"
    };

        private static readonly string[] Nouns =
        {
        "slugcat", "vulture", "lizard", "scavenger", "guardian", "noodlefly", "pearl", "rainworld", "shelter", "iterator"
    };

        private static readonly Random Random = new Random();

        public static string GenerateRandomUsername()
        {
            string adjective = Adjectives[Random.Next(Adjectives.Length)];
            string noun = Nouns[Random.Next(Nouns.Length)];

            return $"{adjective}_{noun}";
        }
    }

}