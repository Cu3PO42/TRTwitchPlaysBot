﻿using System;
using System.Collections.Generic;
using System.Text;
using TwitchLib.Client.Events;

namespace KimimaruBot
{
    /// <summary>
    /// Views a user's level.
    /// </summary>
    public sealed class LevelCommand : BaseCommand
    {
        public override void ExecuteCommand(object sender, OnChatCommandReceivedArgs e)
        {
            List<string> args = e.Command.ArgumentsAsList;

            if (args.Count != 1)
            {
                BotProgram.QueueMessage($"{Globals.CommandIdentifier}level usage: \"username\"");
                return;
            }

            string levelUsername = args[0].ToLowerInvariant();
            User levelUser = BotProgram.GetUser(levelUsername, true);

            if (levelUser == null)
            {
                BotProgram.QueueMessage($"User {levelUsername} does not exist in database!");
                return;
            }

            BotProgram.QueueMessage($"{levelUsername}'s is level {levelUser.Level}, {((AccessLevels.Levels)levelUser.Level)}!");
        }
    }
}
