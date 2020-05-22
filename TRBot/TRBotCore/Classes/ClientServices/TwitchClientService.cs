﻿/* This file is part of TRBot.
 *
 * TRBot is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * TRBot is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Affero General Public License for more details.
 *
 * You should have received a copy of the GNU Affero General Public License
 * along with TRBot.  If not, see <https://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Text;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;

namespace TRBot
{
    /// <summary>
    /// Twitch client interaction.
    /// </summary>
    public class TwitchClientService : IClientService
    {
        private TwitchClient twitchClient = null;

        private ConnectionCredentials Credentials = null;
        private string ChannelName = string.Empty;
        private char ChatCommandIdentifier = '!';
        private char WhisperCommandIdentifier = '!';
        private bool AutoRelistenOnExceptions = true;

        /// <summary>
        /// The event handler associated with the service.
        /// </summary>
        public IEventHandler EventHandler { get; private set; } = null;

        /// <summary>
        /// Tells if the client is initialized.
        /// </summary>
        public bool IsInitialized => (twitchClient?.IsInitialized == true);

        /// <summary>
        /// Tells if the client is connected.
        /// </summary>
        public bool IsConnected => (twitchClient?.IsConnected == true);

        //Cached joined channels list to avoid generating too much garbage from TwitchClient.JoinedChannels
        //This is updated accordingly
        private IReadOnlyList<JoinedChannel> JoinedChannels = null;

        public TwitchClientService(ConnectionCredentials credentials, string channelName, in char chatCommandIdentifier,
            in char whisperCommandIdentifier, in bool autoRelistenOnExceptions)
        {
            twitchClient = new TwitchClient();

            Credentials = credentials;
            ChannelName = channelName;
            ChatCommandIdentifier = chatCommandIdentifier;
            WhisperCommandIdentifier = whisperCommandIdentifier;
            AutoRelistenOnExceptions = autoRelistenOnExceptions;
        }

        /// <summary>
        /// Initializes the client.
        /// </summary>
        public void Initialize()
        {
            twitchClient.Initialize(Credentials, ChannelName, ChatCommandIdentifier,
                WhisperCommandIdentifier, AutoRelistenOnExceptions);
            twitchClient.OverrideBeingHostedCheck = true;

            EventHandler = new TwitchEventHandler(twitchClient);
            EventHandler.Initialize();

            EventHandler.OnJoinedChannelEvent -= OnClientJoinedChannel;
            EventHandler.OnJoinedChannelEvent += OnClientJoinedChannel;
        }

        /// <summary>
        /// Connects the client.
        /// </summary>
        public void Connect()
        {
            if (twitchClient.IsConnected == false)
                twitchClient.Connect();
        }

        /// <summary>
        /// Disconnects the client.
        /// </summary>
        public void Disconnect()
        {
            if (twitchClient.IsConnected == true)
            {
                twitchClient.Disconnect();
                JoinedChannels = null;
            }
        }

        /// <summary>
        /// Reconnects the client.
        /// </summary>
        public void Reconnect()
        {
            if (twitchClient.IsConnected == true)
                twitchClient.Reconnect();
        }

        /// <summary>
        /// Send a message through the client.
        /// </summary>
        public void SendMessage(string channel, string message)
        {
            if (twitchClient.IsConnected == true && JoinedChannels?.Count > 0)
            {
                twitchClient.SendMessage(channel, message);
            }
        }

        /// <summary>
        /// Cleans up the client.
        /// </summary>
        public void CleanUp()
        {
            if (twitchClient.IsConnected == true)
                twitchClient.Disconnect();
            
            JoinedChannels = null;

            EventHandler.OnJoinedChannelEvent -= OnClientJoinedChannel;

            EventHandler.CleanUp();
        }

        private void OnClientJoinedChannel(OnJoinedChannelArgs e)
        {
            //When joining a channel, cache the joined channels list
            JoinedChannels = twitchClient.JoinedChannels;
        }
    }
}
