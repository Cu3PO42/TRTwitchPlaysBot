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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TwitchLib.Client.Events;

namespace TRBot
{
    public sealed class AcceptCommand : BaseCommand
    {
        private Random Rand = new Random();

        public AcceptCommand()
        {

        }

        public override void ExecuteCommand(EvtChatCommandArgs e)
        {
            string name = e.Command.ChatMessage.DisplayName;
            string nameToLower = name.ToLower();

            if (DuelCommand.DuelRequests.ContainsKey(nameToLower) == true)
            {
                DuelCommand.DuelData data = DuelCommand.DuelRequests[nameToLower];
                DuelCommand.DuelRequests.Remove(nameToLower);

                TimeSpan diff = DateTime.Now - data.CurDuelTime;

                if (diff.TotalMinutes >= DuelCommand.DUEL_MINUTES)
                {
                    BotProgram.MsgHandler.QueueMessage("You are not in a duel or your duel has expired!");
                    return;
                }

                long betAmount = data.BetAmount;
                string dueled = data.UserDueling;
                string dueledToLower = dueled.ToLower();

                User duelerUser = BotProgram.GetUser(nameToLower);
                User dueledUser = BotProgram.GetUser(dueledToLower);

                //First confirm both users have enough credits for the duel, as they could've lost some in that time
                if (duelerUser.Credits < betAmount || dueledUser.Credits < betAmount)
                {
                    BotProgram.MsgHandler.QueueMessage("At least one user involved in the duel no longer has enough points for the duel! The duel is off!");
                    return;
                }

                //50/50 chance of either user winning
                int val = Rand.Next(0, 2);

                string message = string.Empty;

                if (val == 0)
                {
                    duelerUser.AddCredits(betAmount);
                    dueledUser.SubtractCredits(betAmount);

                    message = $"{name} won the bet against {dueled} for {betAmount} credit(s)!";
                }
                else
                {
                    duelerUser.SubtractCredits(betAmount);
                    dueledUser.AddCredits(betAmount);

                    message = $"{dueled} won the bet against {name} for {betAmount} credit(s)!";
                }

                BotProgram.SaveBotData();

                BotProgram.MsgHandler.QueueMessage(message);
            }
            else
            {
                BotProgram.MsgHandler.QueueMessage("You are not in a duel or your duel has expired!");
            }
        }
    }
}
