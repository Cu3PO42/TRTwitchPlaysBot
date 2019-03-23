﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TwitchLib;
using TwitchLib.Client.Models;
using TwitchLib.Client.Events;
using Newtonsoft.Json;

namespace KimimaruBot
{
    /// <summary>
    /// Base class for a command
    /// </summary>
    public abstract class BaseCommand
    {
        public bool HiddenFromHelp { get; protected set; } = false;
        public int AccessLevel { get; protected set; } = 0;

        public BaseCommand()
        {
            
        }

        public virtual void Initialize(CommandHandler commandHandler)
        {

        }

        public abstract void ExecuteCommand(object sender, OnChatCommandReceivedArgs e);
    }
}
