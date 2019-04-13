﻿using System;
using System.Collections.Generic;
using System.Text;

namespace KimimaruBot
{
    /// <summary>
    /// The SNES, or Super Famicom.
    /// </summary>
    public sealed class SNESConsole : ConsoleBase
    {
        public override Dictionary<string, HID_USAGES> InputAxes { get; protected set; } = new Dictionary<string, HID_USAGES>();

        public override Dictionary<string, uint> ButtonInputMap { get; protected set; } = new Dictionary<string, uint>()
        {
            { "left", 1 },
            { "right", 2 },
            { "up", 3 },
            { "down", 4 },
            { "a", 5 },
            { "b", 6 },
            { "l", 7 },
            { "r", 8 },
            { "select", 9 },
            { "start", 10 },
            { "savestate1", 19 },
            { "savestate2", 20 },
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
            { "x", 31 },
            { "y", 32 }
        };

        public override string[] ValidInputs { get; protected set; } = new string[14]
        {
            "up", "down", "left", "right", "a", "b", "x", "y", "l", "r", "select", "start",
            "#", "."
        };

        public override bool GetAxis(in Parser.Input input, out HID_USAGES axis)
        {
            axis = default;
            return false;
        }

        public override bool IsAbsoluteAxis(in Parser.Input input) => false;

        public override bool IsAxis(in Parser.Input input) => false;

        public override bool IsMinAxis(in Parser.Input input) => false;

        public override bool IsButton(in Parser.Input input)
        {
            return (IsWait(input) == false);
        }
    }
}
