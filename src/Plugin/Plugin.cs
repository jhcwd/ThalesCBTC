﻿using System;
using OpenBveApi.Runtime;

namespace Plugin {
	/// <summary>The interface to be implemented by the plugin.</summary>
	public class Plugin : IRuntime {

        internal Train train = null;

        [System.Runtime.InteropServices.DllImport("kernel32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        public static extern void OutputDebugString(string message);

        /// <summary>Is called when the plugin is loaded.</summary>
        /// <param name="properties">The properties supplied to the plugin on loading.</param>
        /// <returns>Whether the plugin was loaded successfully.</returns>
        public bool Load(LoadProperties properties)
        {
            properties.Panel = new int[256];
            properties.AISupport = AISupport.Basic;
            train = new Train(properties.Panel, properties.PlaySound);
            OutputDebugString("loaded");
            return true;
        }
		
		/// <summary>Is called when the plugin is unloaded.</summary>
		public void Unload() {
        }

        /// <summary>Is called after loading to inform the plugin about the specifications of the train.</summary>
        /// <param name="specs">The specifications of the train.</param>
        public void SetVehicleSpecs(VehicleSpecs specs)
        {
            this.train.specs = specs;
        }

        /// <summary>Is called when the plugin should initialize or reinitialize.</summary>
        /// <param name="mode">The mode of initialization.</param>
        public void Initialize(InitializationModes mode)
        {
            this.train.Initialize(mode);
        }

        /// <summary>Is called every frame.</summary>
        /// <param name="data">The data passed to the plugin.</param>
        public void Elapse(ElapseData data)
        {
            this.train.Elapse(data);
        }

        /// <summary>Is called when the driver changes the reverser.</summary>
        /// <param name="reverser">The new reverser position.</param>
        public void SetReverser(int reverser)
        {
            this.train.SetReverser(reverser);
        }

        /// <summary>Is called when the driver changes the power notch.</summary>
        /// <param name="powerNotch">The new power notch.</param>
        public void SetPower(int powerNotch)
        {
            this.train.SetPower(powerNotch);
        }

        /// <summary>Is called when the driver changes the brake notch.</summary>
        /// <param name="brakeNotch">The new brake notch.</param>
        public void SetBrake(int brakeNotch)
        {
            this.train.SetBrake(brakeNotch);
        }

        /// <summary>Is called when a virtual key is pressed.</summary>
        /// <param name="key">The virtual key that was pressed.</param>
        public void KeyDown(VirtualKeys key)
        {
            this.train.KeyDown(key);
        }

        /// <summary>Is called when a virtual key is released.</summary>
        /// <param name="key">The virtual key that was released.</param>
        public void KeyUp(VirtualKeys key)
        {
            this.train.KeyUp(key);
        }

        /// <summary>Is called when a horn is played or when the music horn is stopped.</summary>
        /// <param name="type">The type of horn.</param>
        public void HornBlow(HornTypes type)
        {
            this.train.HornBlow(type);
        }

        /// <summary>Is called when the state of the doors changes.</summary>
        /// <param name="oldState">The old state of the doors.</param>
        /// <param name="newState">The new state of the doors.</param>
        public void DoorChange(DoorStates oldState, DoorStates newState)
        {
            this.train.doorState = newState;
            this.train.DoorChange(oldState, newState);
        }

        /// <summary>Is called when the aspect in the current or in any of the upcoming sections changes, or when passing section boundaries.</summary>
        /// <param name="data">Signal information per section. In the array, index 0 is the current section, index 1 the upcoming section, and so on.</param>
        /// <remarks>The signal array is guaranteed to have at least one element. When accessing elements other than index 0, you must check the bounds of the array first.</remarks>
        public void SetSignal(SignalData[] signal)
        {
            this.train.SetSignal(signal);
        }

        /// <summary>Is called when the train passes a beacon.</summary>
        /// <param name="beacon">The beacon data.</param>
        public void SetBeacon(BeaconData beacon)
        {
            this.train.SetBeacon(beacon);
        }

        /// <summary>Is called when the plugin should perform the AI.</summary>
        /// <param name="data">The AI data.</param>
        public void PerformAI(AIData data)
        {
            //this.train.AI.Perform(data);
        }

    }
}