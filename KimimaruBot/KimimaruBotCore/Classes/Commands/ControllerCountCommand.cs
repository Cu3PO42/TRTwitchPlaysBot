﻿using System;
using System.Collections.Generic;
using System.Text;
using TwitchLib.Client.Events;

namespace KimimaruBot
{
    /// <summary>
    /// Displays how many vJoy controllers are intended to be available.
    /// </summary>
    public sealed class ControllerCountCommand : BaseCommand
    {
        public override void ExecuteCommand(object sender, OnChatCommandReceivedArgs e)
        {
            BotProgram.QueueMessage($"There are {BotProgram.BotData.JoystickCount} controller(s) plugged in!");
        }
    }
}
