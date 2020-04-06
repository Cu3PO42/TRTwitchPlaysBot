﻿using System;
using System.Collections.Generic;
using System.Text;
using TwitchLib.Client.Events;

namespace TRBot
{
    /// <summary>
    /// Sets the sleep time on the main thread. Useful only for the user running the bot.
    /// </summary>
    public class SetSleepCommand : BaseCommand
    {
        public override void Initialize(CommandHandler commandHandler)
        {
            base.Initialize(commandHandler);
            AccessLevel = (int)AccessLevels.Levels.Admin;
        }
        
        public override void ExecuteCommand(object sender, OnChatCommandReceivedArgs e)
        {
            List<string> args = e.Command.ArgumentsAsList;

            if (args.Count == 0)
            {
                BotProgram.QueueMessage($"The sleep time of the main thread is {BotProgram.BotSettings.MainThreadSleep}ms.");
                return;
            }
            
            if (args.Count > 1)
            {
                BotProgram.QueueMessage("Usage: sleep time in ms");
                return;
            }

            string sleepStr = args[0];

            if (int.TryParse(sleepStr, out int sleepNum) == false)
            {
                BotProgram.QueueMessage("Invalid number.");
                return;
            }

            if (sleepNum < Globals.MinSleepTime)
            {
                BotProgram.QueueMessage($"The sleep time must be greater than or equal to the minimum of {Globals.MinSleepTime}ms!");
                return;
            }
            else if (sleepNum > Globals.MaxSleepTime)
            {
                BotProgram.QueueMessage($"The sleep time must be less than or equal to the maximum of {Globals.MaxSleepTime}ms!");
                return;
            }

            BotProgram.BotSettings.MainThreadSleep = sleepNum;
            BotProgram.SaveSettings();

            BotProgram.QueueMessage($"Set sleep time to {sleepNum}ms!");
        }
    }
}
