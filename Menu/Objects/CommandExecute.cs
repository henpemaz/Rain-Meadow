namespace RainMeadow
{
    public class CommandExecute
    {
        public const string invalidCmdStr = "Command invalid.";
        public const string incompleteCmdStr = "Command incomplete.";
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
                        if (commandSplit.Length < 2) 
                        { 
                            RainMeadow.Debug("Did not execute command since it didn't fill the params");
                            ChatLogManager.LogClientSystem(incompleteCmdStr, OnlineManager.mePlayer.id.name);
                            return; 
                        }

                        foreach (var player in OnlineManager.players)
                        {
                            if (commandSplit[1] == player.id.name)
                            {
                                if (player.isMe)
                                { 
                                    ChatLogManager.LogClientSystem("You can't kick yourself, doofus!", OnlineManager.mePlayer.id.name);
                                    return;
                                }
                                BanHammer.BanUser(player);
                                ChatLogManager.LogClientSystem($"{player.id.name} Has been kicked.", OnlineManager.mePlayer.id.name);
                                return;
                            }
                            else
                            {
                                ChatLogManager.LogClientSystem("User is invalid.", OnlineManager.mePlayer.id.name);
                                return;
                            }
                        }
                        break;
                }
            }
            // client commands

            switch (commandSplit[0].ToLower())
            {
                case "/msg":
                    if (commandSplit.Length < 3) 
                    { 
                        RainMeadow.Debug("Did not execute command since it didn't fill the params");
                        ChatLogManager.LogClientSystem(incompleteCmdStr, OnlineManager.mePlayer.id.name);
                        return; 
                    }

                    foreach (var player in OnlineManager.players)
                    {
                        if (commandSplit[1] == player.id.name)
                        {
                            ChatLogManager.LogClientSystem($"{OnlineManager.mePlayer.id.name} privately message: {commandSplit[2]}", player.id.name);
                            return;
                        }
                        else
                        {
                            ChatLogManager.LogClientSystem("User is invalid.", OnlineManager.mePlayer.id.name);
                            return;
                        }
                    }
                    break;
            }

            ChatLogManager.LogClientSystem(invalidCmdStr, OnlineManager.mePlayer.id.name);
        }
    }
}