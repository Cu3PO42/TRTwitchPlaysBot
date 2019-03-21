﻿using System;
using System.Collections.Generic;
using System.Text;
using vJoyInterfaceWrap;
using static vJoyInterfaceWrap.vJoy;

namespace KimimaruBot
{
    public class VJoyController : IDisposable
    {
        /// <summary>
        /// Whether we're using vJoy's robust, but less efficient, implementation or not.
        /// </summary>
        public static bool UseRobustImplementation = true;

        /// <summary>
        /// Minimum acceptable vJoy device ID.
        /// </summary>
        public const uint MIN_VJOY_DEVICE_ID = 1;

        /// <summary>
        /// Maximum acceptable vJoy device ID.
        /// </summary>
        public const uint MAX_VJOY_DEVICE_ID = 16;

        /// <summary>
        /// Tells whether the vJoy instance is initialized.
        /// </summary>
        public static bool Initialized => (VJoyInstance != null);

        public static vJoy VJoyInstance { get; private set; } = null;
        public static VJoyController Joystick { get; private set; } = null;

        /// <summary>
        /// Holds the button states for the controller, updated by the bot. true means pressed, and false means released.
        /// </summary>
        public readonly Dictionary<string, bool> ButtonStates = new Dictionary<string, bool>();

        /// <summary>
        /// The ID of the controller.
        /// </summary>
        public uint ControllerID { get; private set; } = 0;

        /// <summary>
        /// The JoystickState of the controller, used in the Efficient implementation.
        /// </summary>
        private JoystickState JSState = default;

        private Dictionary<HID_USAGES, (long AxisMin, long AxisMax)> MinMaxAxes = new Dictionary<HID_USAGES, (long, long)>();

        public VJoyController(in uint controllerID)
        {
            ControllerID = controllerID;
        }

        public void Dispose()
        {
            if (Initialized == false)
                return;

            Reset();
            VJoyInstance.RelinquishVJD(ControllerID);
        }

        public void Init()
        {
            Joystick.SetButtons(InputGlobals.CurrentConsole);

            HID_USAGES[] axes = EnumUtility.GetValues<HID_USAGES>.EnumValues;

            for (int i = 0; i < axes.Length; i++)
            {
                if (VJoyInstance.GetVJDAxisExist(ControllerID, axes[i]))
                {
                    long min = 0L;
                    long max = 0L;
                    VJoyInstance.GetVJDAxisMin(ControllerID, axes[i], ref min);
                    VJoyInstance.GetVJDAxisMax(ControllerID, axes[i], ref max);

                    MinMaxAxes.Add(axes[i], (min, max));
                }
            }
        }

        public void Reset()
        {
            if (Initialized == false)
                return;

            string[] keys = new string[ButtonStates.Keys.Count];
            ButtonStates.Keys.CopyTo(keys, 0);
            for (int i = 0; i < keys.Length; i++)
            {
                ButtonStates[keys[i]] = false;
            }

            VJoyInstance.ResetButtons(ControllerID);

            foreach (KeyValuePair<HID_USAGES, (long, long)> val in MinMaxAxes)
            {
                if (val.Key == HID_USAGES.HID_USAGE_RZ || val.Key == HID_USAGES.HID_USAGE_Z)
                {
                    ReleaseAbsoluteAxis(val.Key);
                }
                else
                {
                    ReleaseAxis(val.Key);
                }
            }

            UpdateJoystickEfficient();
        }

        public void PressInput(in Parser.Input input)
        {
            if (InputGlobals.IsAbsoluteAxis(input) == true)
            {
                PressAbsoluteAxis(InputGlobals.InputAxes[input.name], input.percent);
            }
            else if (InputGlobals.IsAxis(input) == true)
            {
                PressAxis(InputGlobals.InputAxes[input.name], InputGlobals.IsMinAxis(input.name), input.percent);
            }
            else if (InputGlobals.IsButton(input) == true)
            {
                PressButton(input.name);
            }
        }

        public void ReleaseInput(in Parser.Input input)
        {
            if (InputGlobals.IsAbsoluteAxis(input) == true)
            {
                ReleaseAbsoluteAxis(InputGlobals.InputAxes[input.name]);
            }
            else if (InputGlobals.IsAxis(input) == true)
            {
                ReleaseAxis(InputGlobals.InputAxes[input.name]);
            }
            else if (InputGlobals.IsButton(input) == true)
            {
                ReleaseButton(input.name);
            }
        }

        public void PressAxis(in HID_USAGES axis, in bool min, in int percent)
        {
            if (MinMaxAxes.TryGetValue(axis, out (long, long) axisVals) == false)
            {
                return;
            }

            long mid = (axisVals.Item2 - axisVals.Item1) / 2;

            if (min)
            {
                VJoyInstance.SetAxis((int)(mid - ((percent / 100f) * mid)), ControllerID, axis);
            }
            else
            {
                //Mid + ((percent / 100) * Mid)

                VJoyInstance.SetAxis((int)(mid + ((percent / 100f) * mid)), ControllerID, axis);
            }
        }

        public void PressAbsoluteAxis(in HID_USAGES axis, in int percent)
        {
            if (MinMaxAxes.TryGetValue(axis, out (long, long) axisVals) == false)
            {
                return;
            }

            VJoyInstance.SetAxis((int)(axisVals.Item2 * (percent / 100f)), ControllerID, axis);
        }

        public void ReleaseAbsoluteAxis(in HID_USAGES axis)
        {
            if (MinMaxAxes.ContainsKey(axis) == false)
            {
                return;
            }

            VJoyInstance.SetAxis(0, ControllerID, axis);
        }

        public void ReleaseAxis(in HID_USAGES axis)
        {
            if (MinMaxAxes.TryGetValue(axis, out (long, long) axisVals) == false)
            {
                return;
            }

            VJoyInstance.SetAxis((int)((axisVals.Item2 - axisVals.Item1) / 2), ControllerID, axis);
        }

        public void PressButton(in string buttonName)
        {
            if (ButtonStates[buttonName] == true) return;

            ButtonStates[buttonName] = true;
            VJoyInstance.SetBtn(true, ControllerID, InputGlobals.InputMap[buttonName]);
        }

        public void ReleaseButton(in string buttonName)
        {
            if (ButtonStates[buttonName] == false) return;

            ButtonStates[buttonName] = false;
            VJoyInstance.SetBtn(false, ControllerID, InputGlobals.InputMap[buttonName]);
        }

        public void SetButtons(InputGlobals.InputConsoles console)
        {
            ButtonStates.Clear();

            string[] inputs = InputGlobals.GetValidInputs(console);
            for (int i = 0; i < inputs.Length; i++)
            {
                ButtonStates.Add(inputs[i], false);
            }

            Console.WriteLine($"Set controller {ControllerID} buttons to {console}");

            //Reset the controller when we set buttons
            //If we switch to a different console, this will clear all other inputs, including ones not supported by this console
            Reset();
        }

        /// <summary>
        /// Updates the joystick when using the efficient implementation.
        /// </summary>
        public void UpdateJoystickEfficient()
        {
            if (UseRobustImplementation == false)
            {
                VJoyInstance.UpdateVJD(ControllerID, ref JSState);
            }
        }

        public static void Initialize()
        {
            if (Initialized == true) return;

            VJoyInstance = new vJoy();

            if (VJoyInstance.vJoyEnabled() == false)
            {
                Console.WriteLine("vJoy driver not enabled!");
                return;
            }

            Joystick = new VJoyController(1);

            string vendor = VJoyInstance.GetvJoyManufacturerString();
            string product = VJoyInstance.GetvJoyProductString();
            string serialNum = VJoyInstance.GetvJoySerialNumberString();
            Console.WriteLine($"vJoy driver found - Vendor: {vendor} | Product: {product} | Version: {serialNum}");

            uint dllver = 0;
            uint drvver = 0;
            bool match = VJoyInstance.DriverMatch(ref dllver, ref drvver);

            Console.WriteLine($"Using vJoy DLL version {dllver} and vJoy driver version {drvver} | Version Match: {match}");

            //Acquire the device ID
            VjdStat controllerStatus = VJoyInstance.GetVJDStatus(Joystick.ControllerID);
            switch (controllerStatus)
            {
                case VjdStat.VJD_STAT_FREE:
                    bool acquire = VJoyInstance.AcquireVJD(Joystick.ControllerID);
                    if (acquire == false) goto default;

                    Console.WriteLine($"Acquired vJoy device ID {Joystick.ControllerID}!");

                    //Initialize the joystick
                    Joystick.Init();

                    //Reset the joystick
                    Joystick.Reset();
                    break;
                default:
                    Console.WriteLine($"Unable to acquire vJoy device ID {Joystick.ControllerID}");
                    break;
            }
        }

        public static void CleanUp()
        {
            if (Initialized == false)
            {
                Console.WriteLine("Not initialized; cannot clean up");
                return;
            }

            Joystick?.Dispose();
        }

        public static void CheckDeviceIDState(in uint deviceID)
        {
            VjdStat status = VJoyInstance.GetVJDStatus(deviceID);

            switch (status)
            {
                case VjdStat.VJD_STAT_OWN:
                    Console.WriteLine($"vJoy Device {deviceID} is already owned by this feeder");
                    break;
                case VjdStat.VJD_STAT_FREE:
                    Console.WriteLine($"vJoy Device {deviceID} is free!");
                    break;
                case VjdStat.VJD_STAT_BUSY:
                    Console.WriteLine($"vJoy Device {deviceID} is already owned by another feeder - cannot continue");
                    break;
                case VjdStat.VJD_STAT_MISS:
                    Console.WriteLine($"vJoy Device {deviceID} is not installed or disabled - cannot continue");
                    break;
                default:
                    Console.WriteLine($"vJoy Device {deviceID} general error - cannot continue");
                    break;
            }
        }

        public static void CheckButtonCount(in uint deviceID)
        {
            int nBtn = VJoyInstance.GetVJDButtonNumber(deviceID);
            int nDPov = VJoyInstance.GetVJDDiscPovNumber(deviceID);
            int nCPov = VJoyInstance.GetVJDContPovNumber(deviceID);
            bool hasX = VJoyInstance.GetVJDAxisExist(deviceID, HID_USAGES.HID_USAGE_X);
            bool hasY = VJoyInstance.GetVJDAxisExist(deviceID, HID_USAGES.HID_USAGE_Y);
            bool hasZ = VJoyInstance.GetVJDAxisExist(deviceID, HID_USAGES.HID_USAGE_Z);
            bool hasRX = VJoyInstance.GetVJDAxisExist(deviceID, HID_USAGES.HID_USAGE_RX);

            Console.WriteLine($"Device[{deviceID}]: Buttons: {nBtn} | DiscPOVs: {nDPov} | ContPOVs: {nCPov} | Axes - X:{hasX} Y:{hasY} Z: {hasZ} RX: {hasRX}");
        }
    }
}
