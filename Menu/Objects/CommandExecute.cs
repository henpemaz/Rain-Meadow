namespace RainMeadow
{
    public class CommandExecute
    {
        public CommandExecute(string command)
        {
            var commandSplit = command.Split(' ');
            for (var i = 0; i < commandSplit.Length; i++)
            {
                RainMeadow.Debug(commandSplit[i].ToLower());
            }

            if (OnlineManager.lobby.isOwner) // host only commands
            {
                switch (commandSplit[0].ToLower())
                {
                    case "/kick":
                        RainMeadow.Debug("kicking via chat command");
                        foreach (var player in OnlineManager.players)
                        {
                            if (commandSplit[1] == player.id.name)
                            {
                                if (!player.isMe) BanHammer.BanUser(player);
                                return;
                            }
                        }
                        break;
                }
            }
            // to do client commands (msg, etc)
        }
    }
}