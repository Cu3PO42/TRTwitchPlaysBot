﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TwitchLib;
using TwitchLib.Client.Events;

namespace KimimaruBot
{
    public sealed class FeedCommand : BaseCommand
    {
        public FeedCommand()
        {

        }

        public override void ExecuteCommand(object sender, OnChatCommandReceivedArgs e)
        {
            string[] stuff = e.Command.ChatMessage.Message.Split(' ');
            if (stuff.Length > 1)
            {
                string food = string.Empty;
                for (int i = 1; i < stuff.Length; i++)
                {
                    food += stuff[i];

                    if (i < (stuff.Length - 1))
                    {
                        food += " ";
                    }
                }

                if (food.Length > 40)
                {
                    BotProgram.QueueMessage("That's too long; I don't know what kind of food that is!");
                    return;
                }

                string foodToLower = food.ToLower();

                foreach (KeyValuePair<string, List<string>> item in FeedReactions)
                {
                    if (item.Value.Contains(foodToLower) == true)
                    {
                        BotProgram.QueueMessage(item.Key);
                        return;
                    }
                }

                BotProgram.QueueMessage($"I'm indifferent on {food}. Give me something else to eat!");
            }
            else
            {
                BotProgram.QueueMessage("Sorry, I don't recognize that! Feed me something with {Globals.CommandIdentifier}feed");
            }
        }

        private readonly Dictionary<string, List<string>> FeedReactions = new Dictionary<string, List<string>>()
        {
            { "I'm vegetarian, but good choice!", new List<string>() { "steak", "sausage", "boar", "beef", "pork" } },
            { "Hmm...not bad!", new List<string>() { "bread", "butter" } },
            { "Salty, eh?", new List<string>() { "salt" } },
            { "Hot!", new List<string>() { "pepper" } },
            { "Awesome!", new List<string>() { "spaghetti", "fettuccine", "yogurt", "eggplant" } },
            { "If it's vegetarian, yummie!", new List<string>() { "burger", "burrito" } },
            { "Mmm!", new List<string>() { "tofu", "pretzel", "banana", "blueberry" } },
            { "Great choice; I like you!", new List<string>() { "pizza", "popsicle" } },
            { "Amazing!", new List<string>() { "pineapple", "eggplant parmesan", "eggplant parmigiana", "baklava" } },
            { "My taste buds are jumping!", new List<string>() { "cookie", "brownie" } },
            { "EWWW!!", new List<string>() { "poo", "poop", "feces" } },
            { "Yum, yum, yum!", new List<string>() { "fettuccine alfredo", "mushroom", "super mushroom" } },
            { "I've never had donkey before!", new List<string>() { "ass" } },

            { "Great! All toasters, toast toast!", new List<string>() { "toast" } },

            { "That could mean a lot of things!", new List<string>() { "shit", "crap", "fuck" } },

            { "Great!", new List<string>() { "apple", "red delicious apple", "granny smith apple", "berry" } },
            { "Delicious!", new List<string>() { "coinarrhea", "orange", "strawberry" } },
            { "Ouch!", new List<string>() { "lava", "fire" } },

            { "I'm not a cannibal!", new List<string>() { "robot", "robots" } },
            { "I can't eat the world!", new List<string>() { "globe" } },
            { "How will you know where to go then?", new List<string>() { "map" } },
            { "*Drinks* Thanks, I was parched!", new List<string>() { "water" } },
            { "That's not nice; I'm still hungry :(", new List<string>() { "air", "nothing" } },
            { "Ooh, awesome! Thank you!", new List<string>() { "ice cream" } },
            { "Kappa", new List<string>() { "kappa" } },
            { "The boy who cried wolf?", new List<string>() { "boy" } },
            { "I don't eat everything.", new List<string>() { "tpe", "twitchplays_everything" } },
            { "That's not edible for me!", new List<string>() { "nail", "hammer", "cardboard" } },

            { "My mouth is still cleaner than yours!", new List<string>() { "soap" } },
            { "*Absorbs* Ok, I know more now; thanks!", new List<string>() { "knowledge" } },
            { "I don't feel good about doing that :/", new List<string>() { "kimi" } },
            { "I don't think that's a great idea!", new List<string>() { "kimibot", "kimimarubot" } },
            { "Exquisite!", new List<string>() { "durian" } }
        };
    }
}
