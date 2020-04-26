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

namespace TRBot
{
    /// <summary>
    /// The Nintendo Wii.
    /// </summary>
    public sealed class WiiConsole : ConsoleBase
    {
        /// <summary>
        /// The various input modes for the Wii.
        /// This is not a bit field to make defining inputs easier.
        /// </summary>
        public enum InputModes
        {
            Remote = 0,
            RemoteNunchuk = 1
        }

        /// <summary>
        /// The current input mode for the Wii.
        /// </summary>
        public InputModes InputMode = InputModes.RemoteNunchuk;

        public override string[] ValidInputs { get; protected set; } = new string[]
        {
            "left", "right", "up", "down",
            "pleft", "pright", "pup", "pdown",
            "tleft", "tright", "tup", "tdown",
            "dleft", "dright", "dup", "ddown",
            "a", "b", "one", "two", "minus", "plus",
            "c", "z",
            "shake", "point",
            "savestate1", "savestate2", "savestate3", "savestate4", "savestate5", "savestate6", "ss1", "ss2", "ss3", "ss4", "ss5", "ss6",
            "loadstate1", "loadstate2", "loadstate3", "loadstate4", "loadstate5", "loadstate6", "ls1", "ls2", "ls3", "ls4", "ls5", "ls6",
            "#", "."
        };
        
        public override Dictionary<string, int> InputAxes { get; protected set; } = new Dictionary<string, int>()
        {
            { "left",       (int)GlobalAxisVals.AXIS_X },
            { "right",      (int)GlobalAxisVals.AXIS_X },
            { "up",         (int)GlobalAxisVals.AXIS_Y },
            { "down",       (int)GlobalAxisVals.AXIS_Y },
            { "tleft",      (int)GlobalAxisVals.AXIS_RX },
            { "tright",     (int)GlobalAxisVals.AXIS_RX },
            { "tforward",   (int)GlobalAxisVals.AXIS_RY },
            { "tback",      (int)GlobalAxisVals.AXIS_RY },
            { "pleft",      (int)GlobalAxisVals.AXIS_RZ },
            { "pright",     (int)GlobalAxisVals.AXIS_RZ },
            { "pup",        (int)GlobalAxisVals.AXIS_Z },
            { "pdown",      (int)GlobalAxisVals.AXIS_Z }
        };

        //Kimimaru: NOTE - Though vJoy supports up to 128 buttons, Dolphin supports only 32 max
        //The Wii Remote + Nunchuk has more than 32 inputs, so since we can't fit them all, we'll need some modes to toggle
        //and change the input map based on the input scheme
        //We might have to make some sacrifices, such as fewer savestates or disallowing use of the D-pad if a Nunchuk is being used
        public override Dictionary<string, uint> ButtonInputMap { get; protected set; } = new Dictionary<string, uint>() {
            { "left",       (int)GlobalButtonVals.BTN1 }, { "c",        (int)GlobalButtonVals.BTN1 },
            { "right",      (int)GlobalButtonVals.BTN2 }, { "z",        (int)GlobalButtonVals.BTN2 },
            { "up",         (int)GlobalButtonVals.BTN3 }, { "tleft",    (int)GlobalButtonVals.BTN3 },
            { "down",       (int)GlobalButtonVals.BTN4 }, { "tright",   (int)GlobalButtonVals.BTN4 },
            { "a",          (int)GlobalButtonVals.BTN5 },
            { "b",          (int)GlobalButtonVals.BTN6 },
            { "one",        (int)GlobalButtonVals.BTN7 },
            { "two",        (int)GlobalButtonVals.BTN8 },
            { "minus",      (int)GlobalButtonVals.BTN9 },
            { "plus",       (int)GlobalButtonVals.BTN10 },
            { "pleft",      (int)GlobalButtonVals.BTN11 },
            { "pright",     (int)GlobalButtonVals.BTN12 },
            { "pup",        (int)GlobalButtonVals.BTN13 },
            { "pdown",      (int)GlobalButtonVals.BTN14 },
            { "dleft",      (int)GlobalButtonVals.BTN15 },
            { "dright",     (int)GlobalButtonVals.BTN16 },
            { "dup",        (int)GlobalButtonVals.BTN17 },
            { "ddown",      (int)GlobalButtonVals.BTN18 },
            { "savestate1", (int)GlobalButtonVals.BTN19 }, { "ss1",     (int)GlobalButtonVals.BTN19 }, { "tforward",    (int)GlobalButtonVals.BTN19 },
            { "savestate2", (int)GlobalButtonVals.BTN20 }, { "ss2",     (int)GlobalButtonVals.BTN20 }, { "tback",       (int)GlobalButtonVals.BTN20 },
            { "savestate3", (int)GlobalButtonVals.BTN21 }, { "ss3",     (int)GlobalButtonVals.BTN21 },
            { "savestate4", (int)GlobalButtonVals.BTN22 }, { "ss4",     (int)GlobalButtonVals.BTN22 },
            { "savestate5", (int)GlobalButtonVals.BTN23 }, { "ss5",     (int)GlobalButtonVals.BTN23 },
            { "savestate6", (int)GlobalButtonVals.BTN24 }, { "ss6",     (int)GlobalButtonVals.BTN24 },
            { "loadstate1", (int)GlobalButtonVals.BTN25 }, { "ls1",     (int)GlobalButtonVals.BTN25 },
            { "loadstate2", (int)GlobalButtonVals.BTN26 }, { "ls2",     (int)GlobalButtonVals.BTN26 },
            { "loadstate3", (int)GlobalButtonVals.BTN27 }, { "ls3",     (int)GlobalButtonVals.BTN27 },
            { "loadstate4", (int)GlobalButtonVals.BTN28 }, { "ls4",     (int)GlobalButtonVals.BTN28 },
            { "loadstate5", (int)GlobalButtonVals.BTN29 }, { "ls5",     (int)GlobalButtonVals.BTN29 },
            { "loadstate6", (int)GlobalButtonVals.BTN30 }, { "ls6",     (int)GlobalButtonVals.BTN30 },
            { "shake",      (int)GlobalButtonVals.BTN31 },
            { "point",      (int)GlobalButtonVals.BTN32 }
        };
        
        public override void HandleArgsOnConsoleChange(List<string> arguments)
        {
            if (arguments?.Count == 0) return;

            for (int i = 0; i < arguments.Count; i++)
            {
                //Check for enum mode
                if (Enum.TryParse(arguments[i], true, out InputModes mode) == true)
                {
                    InputMode = mode;
                    BotProgram.QueueMessage($"Changed Wii input mode to {mode}!");
                }

                //Kimimaru: I'm debating including the ability to set it by number, so we'll keep it commented until a decision is made
                //if (int.TryParse(arguments[i], out int inputMode) == true)
                //{
                //    InputModes newInputMode = (InputModes)inputMode;
                //    InputMode = (InputModes)inputMode;
                //    Console.WriteLine($"Changed Wii input mode to {newInputMode}");
                //}
            }
        }

        public override bool GetAxis(in Parser.Input input, out int axis)
        {
            return InputAxes.TryGetValue(input.name, out axis);
        }

        public override bool IsAbsoluteAxis(in Parser.Input input)
        {
            return false;
        }

        public override bool IsAxis(in Parser.Input input)
        {
            return (InputAxes.ContainsKey(input.name) == true);
        }

        public override bool IsMinAxis(in Parser.Input input)
        {
            return (input.name == "left" || input.name == "up" || input.name == "tleft" || input.name == "tforward"
                || input.name == "pleft" || input.name == "pup");
        }

        public override bool IsButton(in Parser.Input input)
        {
            return (IsWait(input) == false && IsAxis(input) == false);
        }
    }
}
