namespace RainMeadow
{
    public class CommandExecute
    {
        public const string invalidCmdStr = "Command invalid.";
        public const string incompleteCmdStr = "Command incomplete.";
        public CommandExecute(string command)
        {
            // need to refactor this thing because it just doesn't work
            var commandSplit = command.Split(' ');
            for (var i = 0; i < commandSplit.Length; i++)
            {
                RainMeadow.Debug(commandSplit[i].ToLower());
            }
            for (var i = 0; i < ChatLogManager.playerNamesInLobby.Count; i++)
            {
                RainMeadow.Debug($"Player Name Listed: {ChatLogManager.playerNamesInLobby[i].ToString()}");
            }

            if (OnlineManager.lobby.isOwner) // host only commands
            {
                switch (commandSplit[0].ToLower())
                {
                    case "/ban":
                        RainMeadow.Debug("banning via chat command");
                        if (commandSplit.Length < 2)
                        {
                            RainMeadow.Debug("Did not execute command since it didn't fill the params");
                            ChatLogManager.LogClientSystem(incompleteCmdStr, OnlineManager.mePlayer.id.name);
                            return;
                        }

                        foreach (var player in OnlineManager.players)
                        {
                            if (commandSplit[1].Trim() == player.id.name)
                            {
                                if (player.isMe)
                                {
                                    ChatLogManager.LogClientSystem("The lobby would die if you ban yourself, we can't afford that", OnlineManager.mePlayer.id.name);
                                    return;
                                }
                                BanHammer.BanUser(player);
                                ChatLogManager.LogClientSystem($"{player.id.name} Has been banned from the current lobby.", OnlineManager.mePlayer.id.name);
                                return;
                            }
                            else
                            {
                                ChatLogManager.LogClientSystem("User is invalid.", OnlineManager.mePlayer.id.name);
                                return;
                            }
                        }
                        break;

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
                            if (commandSplit[1].Trim() == player.id.name)
                            {
                                if (player.isMe)
                                {
                                    ChatLogManager.LogClientSystem("You can't kick yourself, doofus!", OnlineManager.mePlayer.id.name);
                                    return;
                                }
                                BanHammer.KickUser(player);
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
                        if (commandSplit[1].Trim() == player.id.name)
                        {
                            ChatLogManager.LogClientSystem($"{OnlineManager.mePlayer.id.name} privately messaged: {commandSplit[2]}", player.id.name);
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