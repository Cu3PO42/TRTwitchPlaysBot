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
using System.Collections.Concurrent;
using System.Text;
using vJoyInterfaceWrap;
using System.Runtime.CompilerServices;
using static vJoyInterfaceWrap.vJoy;

namespace TRBot
{
    public class VJoyController : IVirtualController
    {
        /// <summary>
        /// The mapping from axis number to axis code.
        /// </summary>
        private static readonly Dictionary<int, int> AxisCodeMap = new Dictionary<int, int>(8)
        {
            { (int)GlobalAxisVals.AXIS_X,  (int)HID_USAGES.HID_USAGE_X },
            { (int)GlobalAxisVals.AXIS_Y,  (int)HID_USAGES.HID_USAGE_Y },
            { (int)GlobalAxisVals.AXIS_Z,  (int)HID_USAGES.HID_USAGE_Z },
            { (int)GlobalAxisVals.AXIS_RX, (int)HID_USAGES.HID_USAGE_RX },
            { (int)GlobalAxisVals.AXIS_RY, (int)HID_USAGES.HID_USAGE_RY },
            { (int)GlobalAxisVals.AXIS_RZ, (int)HID_USAGES.HID_USAGE_RZ },
            { (int)GlobalAxisVals.AXIS_M1, (int)HID_USAGES.HID_USAGE_SL0 },
            { (int)GlobalAxisVals.AXIS_M2, (int)HID_USAGES.HID_USAGE_SL1 }
        };

        /// <summary>
        /// The mapping from button number to button code.
        /// </summary>
        private static readonly Dictionary<int, int> ButtonCodeMap = new Dictionary<int, int>(32)
        {
            { (int)GlobalButtonVals.BTN1,       0 },
            { (int)GlobalButtonVals.BTN2,       1 },
            { (int)GlobalButtonVals.BTN3,       2 },
            { (int)GlobalButtonVals.BTN4,       3 },
            { (int)GlobalButtonVals.BTN5,       4 },
            { (int)GlobalButtonVals.BTN6,       5 },
            { (int)GlobalButtonVals.BTN7,       6 },
            { (int)GlobalButtonVals.BTN8,       7 },
            { (int)GlobalButtonVals.BTN9,       8 },
            { (int)GlobalButtonVals.BTN10,      9 },
            { (int)GlobalButtonVals.BTN11,      10 },
            { (int)GlobalButtonVals.BTN12,      11 },
            { (int)GlobalButtonVals.BTN13,      12 },
            { (int)GlobalButtonVals.BTN14,      13 },
            { (int)GlobalButtonVals.BTN15,      14 },
            { (int)GlobalButtonVals.BTN16,      15 },
            { (int)GlobalButtonVals.BTN17,      16 },
            { (int)GlobalButtonVals.BTN18,      17 },
            { (int)GlobalButtonVals.BTN19,      18 },
            { (int)GlobalButtonVals.BTN20,      19 },
            { (int)GlobalButtonVals.BTN21,      20 },
            { (int)GlobalButtonVals.BTN22,      21 },
            { (int)GlobalButtonVals.BTN23,      22 },
            { (int)GlobalButtonVals.BTN24,      23 },
            { (int)GlobalButtonVals.BTN25,      24 },
            { (int)GlobalButtonVals.BTN26,      25 },
            { (int)GlobalButtonVals.BTN27,      26 },
            { (int)GlobalButtonVals.BTN28,      27 },
            { (int)GlobalButtonVals.BTN29,      28 },
            { (int)GlobalButtonVals.BTN30,      29 },
            { (int)GlobalButtonVals.BTN31,      30 },
            { (int)GlobalButtonVals.BTN32,      31 }
        };

        /// <summary>
        /// The ID of the controller.
        /// </summary>
        public uint ControllerID { get; private set; } = 0;

        public int ControllerIndex => (int)ControllerID;

        /// <summary>
        /// Tells whether the controller device was successfully acquired through vJoy.
        /// If this is false, don't use this controller instance to make inputs.
        /// </summary>
        public bool IsAcquired { get; private set; } = false;

        /// <summary>
        /// The JoystickState of the controller, used in the Efficient implementation.
        /// </summary>
        private JoystickState JSState = default;

        private Dictionary<int, (long AxisMin, long AxisMax)> MinMaxAxes = new Dictionary<int, (long, long)>(8);

        //Kimimaru: Ideally we get the input's state from the driver, but this should work well enough, for now at least
        private ConcurrentDictionary<uint, ButtonStates> ButtonPressStates = new ConcurrentDictionary<uint, ButtonStates>(Environment.ProcessorCount * 2, 32);
        private ConcurrentDictionary<uint, ButtonStates> TempBtnStates = new ConcurrentDictionary<uint, ButtonStates>(Environment.ProcessorCount * 2, 32);

        private vJoy VJoyInstance = null;

        public VJoyController(in uint controllerID, vJoy vjoyInstance)
        {
            ControllerID = controllerID;
            VJoyInstance = vjoyInstance;
        }

        public void Dispose()
        {
            if (IsAcquired == false)
                return;

            Reset();
            Close();
        }

        public void Acquire()
        {
            IsAcquired = VJoyInstance.AcquireVJD(ControllerID);
        }

        public void Close()
        {
            VJoyInstance.RelinquishVJD(ControllerID);
            IsAcquired = false;
        }

        public void Init()
        {
            Reset();

            //Initialize axes
            //Use the global axes values, which will be converted to vJoy ones when needing to carry out the inputs
            GlobalAxisVals[] axes = EnumUtility.GetValues<GlobalAxisVals>.EnumValues;

            for (int i = 0; i < axes.Length; i++)
            {
                int globalAxisVal = (int)axes[i];

                if (AxisCodeMap.TryGetValue(globalAxisVal, out int axisVal) == false)
                {
                    continue;
                }

                HID_USAGES vJoyAxis = (HID_USAGES)axisVal;

                if (VJoyInstance.GetVJDAxisExist(ControllerID, vJoyAxis))
                {
                    long min = 0L;
                    long max = 0L;
                    VJoyInstance.GetVJDAxisMin(ControllerID, vJoyAxis, ref min);
                    VJoyInstance.GetVJDAxisMax(ControllerID, vJoyAxis, ref max);

                    MinMaxAxes.Add(globalAxisVal, (min, max));
                }
            }
        }

        public void Reset()
        {
            if (IsAcquired == false)
                return;

            JSState.Buttons = JSState.ButtonsEx1 = JSState.ButtonsEx2 = JSState.ButtonsEx3 = 0;

            foreach (KeyValuePair<int, (long, long)> val in MinMaxAxes)
            {
                if (val.Key == (int)GlobalAxisVals.AXIS_Z || val.Key == (int)GlobalAxisVals.AXIS_RZ)
                {
                    ReleaseAbsoluteAxis(val.Key);
                }
                else
                {
                    ReleaseAxis(val.Key);
                }
            }
            
            foreach (KeyValuePair<uint, ButtonStates> btnKV in TempBtnStates)
            {
                TempBtnStates[btnKV.Key] = ButtonStates.Released;
            }

            UpdateController();
        }

        public void PressInput(in Parser.Input input)
        {
            if (InputGlobals.CurrentConsole.IsAbsoluteAxis(input) == true)
            {
                PressAbsoluteAxis(InputGlobals.CurrentConsole.InputAxes[input.name], input.percent);

                //Kimimaru: In the case of L and R buttons on GCN, when the axes are pressed, the buttons should be released
                ReleaseButton(InputGlobals.CurrentConsole.ButtonInputMap[input.name]);
            }
            else if (InputGlobals.CurrentConsole.GetAxis(input, out int axis) == true)
            {
                PressAxis(axis, InputGlobals.CurrentConsole.IsMinAxis(input), input.percent);
            }
            else if (InputGlobals.CurrentConsole.IsButton(input) == true)
            {
                PressButton(InputGlobals.CurrentConsole.ButtonInputMap[input.name]);

                //Kimimaru: In the case of L and R buttons on GCN, when the buttons are pressed, the axes should be released
                if (InputGlobals.CurrentConsole.InputAxes.TryGetValue(input.name, out int value) == true)
                {
                    ReleaseAbsoluteAxis(value);
                }
            }
            
            //Invoke input callbacks
            if (BotProgram.InputCBData.Callbacks.TryGetValue(input.name, out InputCallback cbData) == true)
            {
                long invocation = (long)cbData.InvocationType;

                //Invoke on press
                if (input.hold == false && EnumUtility.HasEnumVal(invocation, (long)InputCBInvocation.Press) == true)
                {
                    cbData.Callback?.Invoke(cbData.CBValue);
                }
                //Invoke on hold
                else if (input.hold == true && EnumUtility.HasEnumVal(invocation,(long)InputCBInvocation.Hold) == true)
                {
                    cbData.Callback?.Invoke(cbData.CBValue);
                }
            }
        }

        public void ReleaseInput(in Parser.Input input)
        {
            if (InputGlobals.CurrentConsole.IsAbsoluteAxis(input) == true)
            {
                ReleaseAbsoluteAxis(InputGlobals.CurrentConsole.InputAxes[input.name]);

                //Kimimaru: In the case of L and R buttons on GCN, when the axes are released, the buttons should be too
                ReleaseButton(InputGlobals.CurrentConsole.ButtonInputMap[input.name]);
            }
            else if (InputGlobals.CurrentConsole.GetAxis(input, out int axis) == true)
            {
                ReleaseAxis(axis);
            }
            else if (InputGlobals.CurrentConsole.IsButton(input) == true)
            {
                ReleaseButton(InputGlobals.CurrentConsole.ButtonInputMap[input.name]);

                //Kimimaru: In the case of L and R buttons on GCN, when the buttons are released, the axes should be too
                if (InputGlobals.CurrentConsole.InputAxes.TryGetValue(input.name, out int value) == true)
                {
                    ReleaseAbsoluteAxis(value);
                }
            }

            //Invoke input callbacks
            if (BotProgram.InputCBData.Callbacks.TryGetValue(input.name, out InputCallback cbData) == true)
            {
                //Invoke on release
                if (EnumUtility.HasEnumVal((long)cbData.InvocationType, (long)InputCBInvocation.Release) == true)
                {
                    cbData.Callback?.Invoke(cbData.CBValue);
                }
            }
        }

        public void PressAxis(in int axis, in bool min, in int percent)
        {
            //Not a valid axis - defaulting to 0 results in the wrong axis being set
            if (AxisCodeMap.TryGetValue(axis, out int vJoyAxis) == false)
            {
                return;
            }

            if (MinMaxAxes.TryGetValue(axis, out (long, long) axisVals) == false)
            {
                return;
            }

            //Neutral is halfway between the min and max axes 
            long half = (axisVals.Item2 - axisVals.Item1) / 2L;
            int mid = (int)(axisVals.Item1 + half);
            int val = 0;

            if (min)
            {
                val = (int)(mid - ((percent / 100f) * half));
            }
            else
            {
                val = (int)(mid + ((percent / 100f) * half));
            }

            SetAxisEfficient(vJoyAxis, val);
        }

        public void ReleaseAxis(in int axis)
        {
            //Not a valid axis - defaulting to 0 results in the wrong axis being set
            if (AxisCodeMap.TryGetValue(axis, out int vJoyAxis) == false)
            {
                return;
            }

            if (MinMaxAxes.TryGetValue(axis, out (long, long) axisVals) == false)
            {
                return;
            }

            //Neutral is halfway between the min and max axes
            long half = (axisVals.Item2 - axisVals.Item1) / 2L;
            int val = (int)(axisVals.Item1 + half);

            SetAxisEfficient(vJoyAxis, val);
        }

        public void PressAbsoluteAxis(in int axis, in int percent)
        {
            //Not a valid axis - defaulting to 0 results in the wrong axis being set
            if (AxisCodeMap.TryGetValue(axis, out int vJoyAxis) == false)
            {
                return;
            }

            if (MinMaxAxes.TryGetValue(axis, out (long, long) axisVals) == false)
            {
                return;
            }

            int val = (int)(axisVals.Item2 * (percent / 100f));

            SetAxisEfficient(vJoyAxis, val);
        }

        public void ReleaseAbsoluteAxis(in int axis)
        {
            //Not a valid axis - defaulting to 0 results in the wrong axis being set
            if (AxisCodeMap.TryGetValue(axis, out int vJoyAxis) == false)
            {
                return;
            }

            if (MinMaxAxes.ContainsKey(axis) == false)
            {
                return;
            }

            SetAxisEfficient(vJoyAxis, 0);
        }

        public void PressButton(in uint buttonVal)
        {
            //Not a valid button - defaulting to 0 results in the wrong button being pressed/released
            if (ButtonCodeMap.TryGetValue((int)buttonVal, out int button) == false)
            {
                return;
            }

            TempBtnStates[buttonVal] = ButtonStates.Pressed;

            //Kimimaru: Handle button counts greater than 32
            //Each buttons value contains 32 bits, so choose the appropriate one based on the value of the button pressed
            //Note that not all emulators (such as Dolphin) support more than 32 buttons
            int divVal = button / 32;
            int realVal = button - (32 * divVal);
            uint addition = (uint)(1 << realVal);

            switch (divVal)
            {
                case 0: JSState.Buttons |= addition; break;
                case 1: JSState.ButtonsEx1 |= addition; break;
                case 2: JSState.ButtonsEx2 |= addition; break;
                case 3: JSState.ButtonsEx3 |= addition; break;
            }
        }

        public void ReleaseButton(in uint buttonVal)
        {
            //Not a valid button - defaulting to 0 results in the wrong button being pressed/released
            if (ButtonCodeMap.TryGetValue((int)buttonVal, out int button) == false)
            {
                return;
            }

            TempBtnStates[buttonVal] = ButtonStates.Released;

            //Kimimaru: Handle button counts greater than 32
            //Each buttons value contains 32 bits, so choose the appropriate one based on the value of the button pressed
            //Note that not all emulators (such as Dolphin) support more than 32 buttons
            int divVal = button / 32;
            int realVal = button - (32 * divVal);
            uint inverse = ~(uint)(1 << realVal);

            switch (divVal)
            {
                case 0: JSState.Buttons &= inverse; break;
                case 1: JSState.ButtonsEx1 &= inverse; break;
                case 2: JSState.ButtonsEx2 &= inverse; break;
                case 3: JSState.ButtonsEx3 &= inverse; break;
            }
        }

        public ButtonStates GetButtonState(in uint buttonVal)
        {
            if (ButtonPressStates.TryGetValue(buttonVal, out ButtonStates btnState) == true)
            {
                return btnState;
            }
            
            return ButtonStates.Released;
        }

        public void UpdateController()
        {
            //Copy button states over
            ButtonPressStates.CopyDictionaryData(TempBtnStates);
            
            VJoyInstance.UpdateVJD(ControllerID, ref JSState);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetAxisEfficient(in int axis, in int value)
        {
            switch (axis)
            {
                case (int)HID_USAGES.HID_USAGE_X: JSState.AxisX = value; break;
                case (int)HID_USAGES.HID_USAGE_Y: JSState.AxisY = value; break;
                case (int)HID_USAGES.HID_USAGE_Z: JSState.AxisZ = value; break;
                case (int)HID_USAGES.HID_USAGE_RX: JSState.AxisXRot = value; break;
                case (int)HID_USAGES.HID_USAGE_RY: JSState.AxisYRot = value; break;
                case (int)HID_USAGES.HID_USAGE_RZ: JSState.AxisZRot = value; break;
            }
        }
    }
}
