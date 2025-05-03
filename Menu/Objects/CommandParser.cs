namespace RainMeadow
{
    public class CommandParser
    {
        public OnlinePlayer player;
        public CommandParser() { }

        public CommandParser(string command)
        {
            var commandSplit = command.Split(' ');
            for (var i = 0; i < commandSplit.Length; i++)
            {
                RainMeadow.Debug(commandSplit[i].ToLower());
            }

            switch (commandSplit[0].ToLower())
            {
                case "/kick":
                    // i do not know how to get the player via string, for now that is
                    // i'll look more into it later
                    break;
            }
        }
    }
}
