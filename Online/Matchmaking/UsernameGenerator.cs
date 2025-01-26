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

        public static string GenerateRandomUsername(int seed)
        {
            Random random = new Random(seed);
            string adjective = Adjectives[random.Next(Adjectives.Length)];
            string noun = Nouns[random.Next(Nouns.Length)];

            return $"{adjective}_{noun}";
        }
    }

}