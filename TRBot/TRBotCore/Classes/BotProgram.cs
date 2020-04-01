﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TwitchLib;
using TwitchLib.Client;
using TwitchLib.Client.Models;
using TwitchLib.Client.Events;
using TwitchLib.Communication.Events;
using TwitchLib.Communication.Interfaces;
using Newtonsoft;
using Newtonsoft.Json;

namespace TRBot
{
    public sealed class BotProgram : IDisposable
    {
        private static readonly object LockObj = new object();

        private static BotProgram instance = null;

        public bool Initialized { get; private set; } = false;

        private LoginInfo LoginInformation = null;
        public static Settings BotSettings { get; private set; } = null;
        public static BotData BotData { get; private set; } = null;

        private TwitchClient Client;
        private ConnectionCredentials Credentials = null;
        private CrashHandler crashHandler = null;

        private CommandHandler CommandHandler = null;

        public static bool TryReconnect { get; private set; } = false;
        public static bool ChannelJoined { get; private set; } = false;

        public bool IsInChannel => (Client?.IsConnected == true && ChannelJoined == true);

        private DateTime CurQueueTime;

        /// <summary>
        /// Queued messages.
        /// </summary>
        private Queue<string> ClientMessages = new Queue<string>();

        private List<BaseRoutine> BotRoutines = new List<BaseRoutine>();

        //Throttler
        private Stopwatch Throttler = new Stopwatch();

        /// <summary>
        /// Whether to ignore logging bot messages to the console based on potential console logs from the <see cref="ExecCommand"/>.
        /// </summary>
        public static bool IgnoreConsoleLog = false;

        public static string BotName
        {
            get
            {
                if (instance != null)
                {
                    if (instance.LoginInformation != null) return instance.LoginInformation.BotName;
                }

                return "N/A";
            }
        }

        public BotProgram()
        {
            crashHandler = new CrashHandler();
            
            instance = this;

            //Below normal priority
            Process thisProcess = Process.GetCurrentProcess();
            thisProcess.PriorityBoostEnabled = false;
            thisProcess.PriorityClass = ProcessPriorityClass.Idle;
        }

        public void Dispose()
        {
            if (Initialized == false)
                return;

            UnsubscribeEvents();

            for (int i = 0; i < BotRoutines.Count; i++)
            {
                BotRoutines[i].CleanUp(Client);
            }

            ClientMessages.Clear();

            if (Client.IsConnected == true)
                Client.Disconnect();

            //Clean up and relinquish the devices when we're done
            InputGlobals.ControllerMngr?.CleanUp();

            instance = null;
        }

        public void Initialize()
        {
            if (Initialized == true)
                return;

            //Kimimaru: Use invariant culture
            Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

            //Load all the necessary data; if something doesn't exist, save out an empty object so it can be filled in manually
            string loginText = Globals.ReadFromTextFileOrCreate(Globals.LoginInfoFilename);
            LoginInformation = JsonConvert.DeserializeObject<LoginInfo>(loginText);

            if (LoginInformation == null)
            {
                Console.WriteLine("No login information found; attempting to create file template. If created, please manually fill out the information.");

                LoginInformation = new LoginInfo();
                string text = JsonConvert.SerializeObject(LoginInformation, Formatting.Indented);
                Globals.SaveToTextFile(Globals.LoginInfoFilename, text);
            }

            LoadSettingsAndBotData();

            //Kimimaru: If the bot itself isn't in the bot data, add it as an admin!
            if (string.IsNullOrEmpty(LoginInformation.BotName) == false)
            {
                string botName = LoginInformation.BotName.ToLowerInvariant();
                User botUser = null;
                if (BotData.Users.TryGetValue(botName, out botUser) == false)
                {
                    botUser = new User();
                    botUser.Name = botName;
                    botUser.Level = (int)AccessLevels.Levels.Admin;
                    BotData.Users.Add(botName, botUser);

                    SaveBotData();
                }
            }

            try
            {
                Credentials = new ConnectionCredentials(LoginInformation.BotName, LoginInformation.Password);
            }
            catch (Exception exception)
            {
                Console.WriteLine($"Invalid credentials: {exception.Message}");
                Console.WriteLine("Cannot proceed. Please double check the login information in the data folder");
                return;
            }

            Client = new TwitchClient();
            Client.Initialize(Credentials, LoginInformation.ChannelName, Globals.CommandIdentifier, Globals.CommandIdentifier, true);
            Client.OverrideBeingHostedCheck = true;

            UnsubscribeEvents();

            Client.OnJoinedChannel += OnJoinedChannel;
            Client.OnMessageReceived += OnMessageReceived;
            Client.OnWhisperReceived += OnWhisperReceived;
            Client.OnNewSubscriber += OnNewSubscriber;
            Client.OnReSubscriber += OnReSubscriber;
            Client.OnChatCommandReceived += OnChatCommandReceived;
            Client.OnBeingHosted += OnBeingHosted;
            
            Client.OnConnected += OnConnected;
            Client.OnConnectionError += OnConnectionError;
            Client.OnReconnected += OnReconnected;
            Client.OnDisconnected += OnDisconnected;

            AddRoutine(new PeriodicMessageRoutine());
            AddRoutine(new CreditsGiveRoutine());
            AddRoutine(new ReconnectRoutine());

            //Initialize controller input - validate the controller type first
            if (InputGlobals.IsVControllerSupported((InputGlobals.VControllerTypes)BotData.LastVControllerType) == false)
            {
                BotData.LastVControllerType = (int)InputGlobals.GetDefaultSupportedVControllerType();
            }

            InputGlobals.VControllerTypes vCType = (InputGlobals.VControllerTypes)BotData.LastVControllerType;
            Console.WriteLine($"Setting up virtual controller {vCType}");
            
            InputGlobals.SetVirtualController(vCType);

            Initialized = true;
        }

        public void Run()
        {
            if (Client.IsConnected == true)
            {
                Console.WriteLine("Client is already connected and running!");
                return;
            }

            Client.Connect();

            const int CPUPercentLimit = 10;

            //Run
            while (true)
            {
                Throttler.Reset();
                Throttler.Start();

                long start = Throttler.ElapsedTicks;

                DateTime now = DateTime.Now;

                TimeSpan queueDiff = now - CurQueueTime;

                //Queued messages
                if (ClientMessages.Count > 0 && queueDiff.TotalMilliseconds >= BotSettings.MessageCooldown)
                {
                    if (IsInChannel == true)
                    {
                        string message = ClientMessages.Dequeue();

                        //There's a chance the bot could be disconnected from the channel between the conditional and now
                        try
                        {
                            //Send the message
                            Client.SendMessage(LoginInformation.ChannelName, message);
                        }
                        catch (TwitchLib.Client.Exceptions.BadStateException e)
                        {
                            Console.WriteLine($"Could not send message due to bad state: {e.Message}");
                        }

                        if (IgnoreConsoleLog == false)
                        {
                            Console.WriteLine(message);
                        }

                        CurQueueTime = now;
                    }
                }

                //Update routines
                for (int i = 0; i < BotRoutines.Count; i++)
                {
                    if (BotRoutines[i] == null)
                    {
                        Console.WriteLine($"NULL BOT ROUTINE AT {i} SOMEHOW!!");
                        continue;
                    }

                    BotRoutines[i].UpdateRoutine(Client, now);
                }

                long end = Throttler.ElapsedTicks;
                long dur = end - start;

                long relativeWaitTime = (int)((1 / (double)CPUPercentLimit) * dur);

                Thread.Sleep((int)((relativeWaitTime / (double)Stopwatch.Frequency) * 1000));
            }
        }

        public static void QueueMessage(string message)
        {
            if (string.IsNullOrEmpty(message) == false)
            {
                instance.ClientMessages.Enqueue(message);
            }
        }

        public static void AddRoutine(BaseRoutine routine)
        {
            routine.Initialize(instance.Client);
            instance.BotRoutines.Add(routine);
        }

        public static void RemoveRoutine(BaseRoutine routine)
        {
            routine.CleanUp(instance.Client);
            instance.BotRoutines.Remove(routine);
        }

        public static BaseRoutine FindRoutine<T>()
        {
            return instance.BotRoutines.Find((routine) => routine is T);
        }

        private void UnsubscribeEvents()
        {
            Client.OnJoinedChannel -= OnJoinedChannel;
            Client.OnMessageReceived -= OnMessageReceived;
            Client.OnWhisperReceived -= OnWhisperReceived;
            Client.OnNewSubscriber -= OnNewSubscriber;
            Client.OnReSubscriber -= OnReSubscriber;
            Client.OnChatCommandReceived -= OnChatCommandReceived;
            Client.OnConnected -= OnConnected;
            Client.OnConnectionError -= OnConnectionError;
            Client.OnReconnected += OnReconnected;
            Client.OnDisconnected -= OnDisconnected;
            Client.OnBeingHosted -= OnBeingHosted;
        }

#region Events

        private void OnConnected(object sender, OnConnectedArgs e)
        {
            TryReconnect = false;
            ChannelJoined = false;

            Console.WriteLine($"{LoginInformation.BotName} connected!");
        }

        private void OnConnectionError(object sender, OnConnectionErrorArgs e)
        {
            ChannelJoined = false;

            if (TryReconnect == false)
            {
                Console.WriteLine($"Failed to connect: {e.Error.Message}");

                TryReconnect = true;
            }
        }

        private void OnJoinedChannel(object sender, OnJoinedChannelArgs e)
        {
            if (string.IsNullOrEmpty(BotSettings.ConnectMessage) == false)
            {
                string finalMsg = BotSettings.ConnectMessage.Replace("{0}", LoginInformation.BotName).Replace("{1}", Globals.CommandIdentifier.ToString());
                QueueMessage(finalMsg);
            }

            Console.WriteLine($"Joined channel \"{e.Channel}\"");

            TryReconnect = false;
            ChannelJoined = true;

            if (CommandHandler == null)
            {
                CommandHandler = new CommandHandler(Client);
            }
        }

        private void OnChatCommandReceived(object sender, OnChatCommandReceivedArgs e)
        {
            CommandHandler.HandleCommand(sender, e);
        }

        private void OnMessageReceived(object sender, OnMessageReceivedArgs e)
        {
            User userData = GetOrAddUser(e.ChatMessage.Username, false);

            userData.TotalMessages++;

            string possibleMeme = e.ChatMessage.Message.ToLower();
            if (BotProgram.BotData.Memes.TryGetValue(possibleMeme, out string meme) == true)
            {
                BotProgram.QueueMessage(meme);
            }
            else
            {
                //Ignore commands as inputs
                if (possibleMeme.StartsWith(Globals.CommandIdentifier) == true)
                {
                    return;
                }

                //Parser.InputSequence inputSequence = default;
                //(bool, List<List<Parser.Input>>, bool, int) parsedVal = default;
                Parser.InputSequence inputSequence = default;

                try
                {
                    string parse_message = Parser.Expandify(Parser.PopulateMacros(e.ChatMessage.Message));

                    inputSequence = Parser.ParseInputs(parse_message);

                    //parsedVal = Parser.Parse(parse_message);
                    //Console.WriteLine(inputSequence.ToString());
                }
                catch (Exception exception)
                {
                    //Kimimaru: Sanitize parsing exceptions
                    //Most of these are currently caused by differences in how C# and Python handle slicing strings (Substring() vs string[:])
                    //One example that throws this that shouldn't is "#mash(w234"
                    //BotProgram.QueueMessage($"ERROR: {exception.Message}");
                    inputSequence.InputValidationType = Parser.InputValidationTypes.Invalid;
                    //parsedVal.Item1 = false;
                }

                //Check for non-valid messages
                if (inputSequence.InputValidationType != Parser.InputValidationTypes.Valid)
                {
                    //Display error message for invalid inputs
                    if (inputSequence.InputValidationType == Parser.InputValidationTypes.Invalid)
                    {
                        BotProgram.QueueMessage(inputSequence.Error);
                    }
                }
                //It's a valid message, so process it
                else
                {
                    //Ignore if user is silenced
                    if (userData.Silenced == true)
                    {
                        return;
                    }

                    if (InputGlobals.IsValidPauseInputDuration(inputSequence.Inputs, "start", BotData.MaxPauseHoldDuration) == false)
                    {
                        BotProgram.QueueMessage($"Invalid input: Pause button held for longer than the max duration of {BotData.MaxPauseHoldDuration} milliseconds!");
                        return;
                    }

                    //Check if the user has permission to perform all the inputs they attempted
                    ParserPostProcess.InputValidation inputValidation = ParserPostProcess.CheckInputPermissions(userData.Level, inputSequence.Inputs, BotData.InputAccess.InputAccessDict);

                    //If the input isn't valid, exit
                    if (inputValidation.IsValid == false)
                    {
                        if (string.IsNullOrEmpty(inputValidation.Message) == false)
                        {
                            QueueMessage(inputValidation.Message);
                        }

                        return;
                    }

                    if (InputHandler.StopRunningInputs == false)
                    {
                        //Mark this as a valid input
                        userData.ValidInputs++;

                        bool shouldPerformInput = true;

                        //Check the team the user is on for the controller they should be using
                        //Validate that the controller is acquired and exists
                        int controllerNum = userData.Team;

                        if (controllerNum < 0 || controllerNum >= InputGlobals.ControllerMngr.ControllerCount)
                        {
                            BotProgram.QueueMessage($"ERROR: Invalid joystick number {controllerNum + 1}. # of joysticks: {InputGlobals.ControllerMngr.ControllerCount}. Please change your controller port to a valid number to perform inputs.");
                            shouldPerformInput = false;
                        }
                        //Now verify that the controller has been acquired
                        else if (InputGlobals.ControllerMngr.GetController(controllerNum).IsAcquired == false)
                        {
                            BotProgram.QueueMessage($"ERROR: Joystick number {controllerNum + 1} with controller ID of {InputGlobals.ControllerMngr.GetController(controllerNum).ControllerID} has not been acquired! Ensure you (the streamer) have a virtual device set up at this ID.");
                            shouldPerformInput = false;
                        }

                        //We're okay to perform the input
                        if (shouldPerformInput == true)
                        {
                            InputHandler.CarryOutInput(inputSequence.Inputs, controllerNum);

                            //If auto whitelist is enabled, the user reached the whitelist message threshold,
                            //the user isn't whitelisted, and the user hasn't ever been whitelisted, whitelist them
                            if (BotSettings.AutoWhitelistEnabled == true && userData.Level < (int)AccessLevels.Levels.Whitelisted
                                && userData.AutoWhitelisted == false && userData.ValidInputs >= BotSettings.AutoWhitelistInputCount)
                            {
                                userData.Level = (int)AccessLevels.Levels.Whitelisted;
                                userData.AutoWhitelisted = true;

                                if (string.IsNullOrEmpty(BotSettings.AutoWhitelistMsg) == false)
                                {
                                    //Replace the user's name with the message
                                    string msg = BotSettings.AutoWhitelistMsg.Replace("{0}", e.ChatMessage.Username);
                                    QueueMessage(msg);
                                }
                            }
                        }
                    }
                    else
                    {
                        QueueMessage("New inputs cannot be processed until all other inputs have stopped.");
                    }
                }
            }

            //Kimimaru: For testing this will work, but we shouldn't save after each message
            SaveBotData();
        }

        private void OnWhisperReceived(object sender, OnWhisperReceivedArgs e)
        {
            
        }

        private void OnBeingHosted(object sender, OnBeingHostedArgs e)
        {
            QueueMessage($"Thank you for hosting, {e.BeingHostedNotification.HostedByChannel}!!");
        }

        private void OnNewSubscriber(object sender, OnNewSubscriberArgs e)
        {
            QueueMessage($"Thank you for subscribing, {e.Subscriber.DisplayName} :D !!");
        }

        private void OnReSubscriber(object sender, OnReSubscriberArgs e)
        {
            QueueMessage($"Thank you for subscribing for {e.ReSubscriber.Months} months, {e.ReSubscriber.DisplayName} :D !!");
        }

        private void OnReconnected(object sender, OnReconnectedEventArgs e)
        {
            QueueMessage("Successfully reconnected to chat!");

            TryReconnect = false;
        }

        private void OnDisconnected(object sender, OnDisconnectedEventArgs e)
        {
            Console.WriteLine("Disconnected!");

            TryReconnect = true;
        }

        public static User GetUser(string username, bool isLower = true)
        {
            if (isLower == false)
            {
                username = username.ToLowerInvariant();
            }

            User userData = null;

            BotData.Users.TryGetValue(username, out userData);

            return userData;
        }

        /// <summary>
        /// Gets a user object by username and adds the user object if the username isn't found.
        /// </summary>
        /// <param name="username">The name of the user.</param>
        /// <param name="isLower">Whether the username is all lower-case or not.
        /// If false, will make the username lowercase before checking the name.</param>
        /// <returns>A User object associated with <paramref name="username"/>.</returns>
        public static User GetOrAddUser(string username, bool isLower = true)
        {
            string origName = username;
            if (isLower == false)
            {
                username = username.ToLowerInvariant();
            }

            User userData = null;

            //Check to add a user that doesn't exist
            if (BotData.Users.TryGetValue(username, out userData) == false)
            {
                userData = new User();
                userData.Name = username;
                BotData.Users.Add(username, userData);

                BotProgram.QueueMessage($"Welcome to the stream, {origName} :D ! We hope you enjoy your stay!");
            }

            return userData;
        }

        public static void SaveBotData()
        {
            //Make sure more than one thread doesn't try to save at the same time to prevent potential loss of data and access violations
            lock (LockObj)
            {
                string text = JsonConvert.SerializeObject(BotData, Formatting.Indented);
                if (string.IsNullOrEmpty(text) == false)
                {
                    if (Globals.SaveToTextFile("BotData.txt", text) == false)
                    {
                        QueueMessage($"CRITICAL - Unable to save bot data");
                    }
                }
            }
        }

        public static void LoadSettingsAndBotData()
        {
            string settingsText = Globals.ReadFromTextFileOrCreate(Globals.SettingsFilename);
            BotSettings = JsonConvert.DeserializeObject<Settings>(settingsText);

            if (BotSettings == null)
            {
                Console.WriteLine("No settings found; attempting to create file template. If created, please manually fill out the information.");

                BotSettings = new Settings();
                string text = JsonConvert.SerializeObject(BotSettings, Formatting.Indented);
                Globals.SaveToTextFile(Globals.SettingsFilename, text);
            }

            string dataText = Globals.ReadFromTextFile(Globals.BotDataFilename);
            BotData = JsonConvert.DeserializeObject<BotData>(dataText);

            if (BotData == null)
            {
                Console.WriteLine("Not bot data found; initializing new bot data.");

                BotData = new BotData();
                SaveBotData();
            }
        }

#endregion

        private class LoginInfo
        {
            public string BotName = string.Empty;
            public string Password = string.Empty;
            public string ChannelName = string.Empty;
        }

        public class Settings
        {
            public int MessageTime = 30;
            public double MessageCooldown = 1000d;
            public double CreditsTime = 2d;
            public long CreditsAmount = 100L;

            /// <summary>
            /// The message to send when the bot connects to a channel. "{0}" is replaced with the name of the bot and "{1}" is replaced with the command identifier.
            /// </summary>
            public string ConnectMessage = "{0} has connected :D ! Use {1}help to display a list of commands and {1}tutorial to see how to play! Original input parser by Jdog, aka TwitchPlays_Everything, converted to C# & improved by the community.";

            /// <summary>
            /// If true, automatically whitelists users if conditions are met, including the command count.
            /// </summary>
            public bool AutoWhitelistEnabled = false;

            /// <summary>
            /// The number of valid inputs required to whitelist a user if they're not whitelisted and auto whitelist is enabled.
            /// </summary>
            public int AutoWhitelistInputCount = 20;

            /// <summary>
            /// The message to send when a user is auto whitelisted. "{0}" is replaced with the name of the user whitelisted.
            /// </summary>
            public string AutoWhitelistMsg = "{0} has been whitelisted! New commands are available.";
        }
    }
}
