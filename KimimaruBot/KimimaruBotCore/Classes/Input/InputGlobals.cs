﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.CompilerServices;

namespace KimimaruBot
{
    /// <summary>
    /// Defines inputs.
    /// </summary>
    public static class InputGlobals
    {
        /// <summary>
        /// The consoles that inputs are supported for.
        /// </summary>
        public enum InputConsoles
        {
            SNES,
            N64,
            GC,
            Wii,
        }

        public static readonly Dictionary<string, string> INPUT_SYNONYMS = new Dictionary<string, string>()
        {
            //{ "pause", "start" }
            { "kappa", "#" }
        };

        /// <summary>
        /// The current console inputs are being sent for.
        /// </summary>
        public static InputConsoles CurrentConsole = InputConsoles.GC;

        //SNES
        private static readonly string[] SNESInputs = new string[14]
        {
            "left", "right", "up", "down",
            "a", "b", "l", "r", "x", "y",
            "start", "select",
            "#", "."
        };

        //N64        
        private static readonly string[] N64Inputs = new string[20]
        {
            "left", "right", "up", "down",
            "dleft", "dright", "dup", "ddown",
            "cleft", "cright", "cup", "cdown",
            "a", "b", "l", "r", "z",
            "start",
            "#", "."
        };

        //GC
        private static readonly string[] GCInputs = new string[22]
        {
            "left", "right", "up", "down",
            "dleft", "dright", "dup", "ddown",
            "cleft", "cright", "cup", "cdown",
            "a", "b", "l", "r", "x", "y", "z",
            "start",
            "#", "."
        };

        //Wii
        private static readonly string[] WiiInputs = new string[24]
        {
            "left", "right", "up", "down",
            "pleft", "pright", "pup", "pdown",
            "tleft", "tright", "tup", "tdown",
            "a", "b", "one", "two", "minus", "plus",
            "c", "z",
            "shake", "point",
            "#", "."
        };

        /// <summary>
        /// The default duration of an input.
        /// </summary>
        public const int DURATION_DEFAULT = 200;

        /// <summary>
        /// The max duration of a given input sequence.
        /// </summary>
        public const int DURATION_MAX = 60000;

        /// <summary>
        /// Returns the valid input names of the current console.
        /// </summary>
        public static string[] ValidInputs => GetValidInputs(CurrentConsole);

        /// <summary>
        /// Retrieves the valid input names for a given console.
        /// </summary>
        /// <param name="console">The console to retrieve inputs for.</param>
        /// <returns>An array of strings with valid input names for the console.</returns>
        public static string[] GetValidInputs(in InputConsoles console)
        {
            switch(console)
            {
                case InputConsoles.SNES:
                default: return SNESInputs;
                case InputConsoles.N64: return N64Inputs;
                case InputConsoles.GC: return GCInputs;
                case InputConsoles.Wii: return WiiInputs;
            }
        }

        /* Kimimaru: NOTE - It might be better to refactor these to be in separate classes unique to each controller
         * This way, for instance, if you call IsAbsoluteAxis on a GC controller, it'll return true if L or R aren't fully pressed, but on any
         * other console it'll return false
         * 
         * This also makes extending them to other consoles easier
         * */

        /// <summary>
        /// A more efficient version of telling whether an input is an axis.
        /// Returns the axis if found to save a dictionary lookup if one is needed afterwards.
        /// </summary>
        /// <param name="input">The input to check.</param>
        /// <param name="axis">The axis value that is assigned.</param>
        /// <returns>true if the input is an axis, otherwise false.</returns>
        public static bool GetAxis(in Parser.Input input, out HID_USAGES axis)
        {
            if (input.name == "l" || input.name == "r")
            {
                if (CurrentConsole != InputConsoles.GC || input.percent == 100)
                {
                    axis = default;
                    return false;
                }
            }

            return InputAxes.TryGetValue(input.name, out axis);
        }

        /// <summary>
        /// Tells whether an input is an axis or not.
        /// </summary>
        /// <param name="input">The input to check.</param>
        /// <returns>true if the input is an axis, otherwise false.</returns>
        public static bool IsAxis(in Parser.Input input)
        {
            if (input.name == "l" || input.name == "r")
            {
                return (CurrentConsole == InputConsoles.GC && input.percent < 100);
            }

            return (InputAxes.ContainsKey(input.name) == true);
        }

        /// <summary>
        /// Tells whether the input is a wait input.
        /// </summary>
        /// <param name="input">The input to check.</param>
        /// <returns>true if the input is one of the wait characters, otherwise false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsWait(in Parser.Input input) => (input.name == "#" || input.name == ".");

        /// <summary>
        /// Tells whether the input is a button.
        /// </summary>
        /// <param name="input">The input to check.</param>
        /// <returns>true if the input is a button, otherwise false.</returns>
        public static bool IsButton(in Parser.Input input)
        {
            if (CurrentConsole == InputConsoles.GC && (input.name == "l" || input.name == "r"))
            {
                return (input.percent == 100);
            }

            return (IsAxis(input) == false && IsWait(input) == false);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsMinAxis(in string input)
        {
            return (input == "left" || input == "up" || input == "cleft" || input == "cup");
        }

        /// <summary>
        /// Tells whether the input is an absolute axis - one that starts at 0 and goes up to a value.
        /// <para>This is usually true only for triggers, such as the GameCube's L and R buttons.</para>
        /// </summary>
        /// <param name="input">The input to check.</param>
        /// <returns>true if the input is an absolute axis, otherwise false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsAbsoluteAxis(in Parser.Input input)
        {
            return (CurrentConsole == InputConsoles.GC && ((input.name == "l" || input.name == "r") && input.percent != 100));
        }

        #region Input Definitions

        public static readonly Dictionary<string, uint> InputMap = new Dictionary<string, uint>() {
            { "left", 1 }, { "c", 1 },
            { "right", 2 }, { "z", 2 },
            { "up", 3 }, { "tleft", 3 },
            { "down", 4 }, { "tright", 4 },
            { "a", 5 },
            { "b", 6 },
            { "l", 7 }, { "one", 7 },
            { "r", 8 }, { "two", 8 },
            { "select", 9 }, { "minus", 9 },
            { "start", 10 }, { "plus", 10 },
            { "cleft", 11 }, { "pleft", 11 },
            { "cright", 12 }, { "pright", 12 },
            { "cup", 13 }, { "pup", 13 },
            { "cdown", 14 }, { "pdown", 14 },
            { "dleft", 15 },
            { "dright", 16 },
            { "dup", 17 },
            { "ddown", 18 },
            { "savestate1", 19 }, { "tforward", 19 },
            { "savestate2", 20 }, { "tback", 20 },
            { "savestate3", 21 },
            { "savestate4", 22 },
            { "savestate5", 23 },
            { "savestate6", 24 },
            { "loadstate1", 25 },
            { "loadstate2", 26 },
            { "loadstate3", 27 },
            { "loadstate4", 28 },
            { "loadstate5", 29 },
            { "loadstate6", 30 },
            { "x", 31 }, { "shake", 31 },
            { "y", 32 }, { "point", 32 },
        };

        public static readonly Dictionary<string, HID_USAGES> InputAxes = new Dictionary<string, HID_USAGES>()
        {
            { "left", HID_USAGES.HID_USAGE_X },
            { "right", HID_USAGES.HID_USAGE_X },
            { "up", HID_USAGES.HID_USAGE_Y },
            { "down", HID_USAGES.HID_USAGE_Y },
            { "cleft", HID_USAGES.HID_USAGE_RX },
            { "cright", HID_USAGES.HID_USAGE_RX },
            { "cup", HID_USAGES.HID_USAGE_RY },
            { "cdown", HID_USAGES.HID_USAGE_RY },
            { "l", HID_USAGES.HID_USAGE_RZ },
            { "r", HID_USAGES.HID_USAGE_Z }
        };

        #endregion
    }
}
