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
        "slugcat", "vulture", "lizard", "scavenger", "templar", "noodlefly", "pearl", "rainworld", "shelter", "iterator"
    };

        private static readonly Random Random = new Random();

        public static string GenerateRandomUsername()
        {
            // Randomly select an adjective and a noun
            string adjective = Adjectives[Random.Next(Adjectives.Length)];
            string noun = Nouns[Random.Next(Nouns.Length)];

            // Combine them into a username
            return $"{adjective}_{noun}";
        }
    }

}