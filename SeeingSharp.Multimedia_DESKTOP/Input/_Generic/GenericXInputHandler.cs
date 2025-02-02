﻿#region License information (SeeingSharp and all based games/applications)
/*
    Seeing# and all games/applications distributed together with it. 
	Exception are projects where it is noted otherwhise.
    More info at 
     - https://github.com/RolandKoenig/SeeingSharp (sourcecode)
     - http://www.rolandk.de/wp (the autors homepage, german)
    Copyright (C) 2016 Roland König (RolandK)
    
    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU Lesser General Public License as published
    by the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.
    
    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU Lesser General Public License for more details.

    You should have received a copy of the GNU Lesser General Public License
    along with this program.  If not, see http://www.gnu.org/licenses/.
*/
#endregion
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SeeingSharp.Infrastructure;
using SeeingSharp.Multimedia.Core;

// Namespace mappings
using XI = SharpDX.XInput;

// Define assembly attributes for type that is defined in this file
[assembly: AssemblyQueryableType(
    targetType: typeof(SeeingSharp.Multimedia.Input.GenericXInputHandler),
    contractType: typeof(SeeingSharp.Multimedia.Input.IInputHandler))]

namespace SeeingSharp.Multimedia.Input
{
    internal class GenericXInputHandler : IInputHandler
    {
        #region Resources
        private XI.Controller[] m_controllers;
        private GamepadState[] m_states;
        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="GenericXInputHandler"/> class.
        /// </summary>
        public GenericXInputHandler()
        {
            m_controllers = new XI.Controller[4];
            m_controllers[0] = new XI.Controller(XI.UserIndex.One);
            m_controllers[1] = new XI.Controller(XI.UserIndex.Two);
            m_controllers[2] = new XI.Controller(XI.UserIndex.Three);
            m_controllers[3] = new XI.Controller(XI.UserIndex.Four);

            m_states = new GamepadState[m_controllers.Length];
            for(int loop=0; loop<m_controllers.Length; loop++)
            {
                m_states[loop] = new GamepadState(loop);
            }
        }

        /// <summary>
        /// Gets a list containing all supported view types.
        /// Null means that this handler is not bound to a view.
        /// </summary>
        public Type[] GetSupportedViewTypes()
        {
            return null;
        }

        /// <summary>
        /// Starts input handling.
        /// </summary>
        /// <param name="viewObject">The view object (e. g. Direct3D11Canvas).</param>
        public void Start(IInputEnabledView viewObject)
        {

        }

        public void Stop()
        {

        }

        /// <summary>
        /// Querries all current input states.
        /// </summary>
        public IEnumerable<InputStateBase> GetInputStates()
        {
            // Update connected states first
            for (int loop = 0; loop < m_controllers.Length; loop++)
            {
                bool isConnected = m_controllers[loop].IsConnected;

                if (!isConnected)
                {
                    m_states[loop].NotifyConnected(false);
                    continue;
                }
                m_states[loop].NotifyConnected(true);

                // Query all state structures
                XI.State xiState = m_controllers[loop].GetState();
                XI.Gamepad xiGamepad = xiState.Gamepad;

                // Convert float values 
                GamepadReportedState repState = new GamepadReportedState()
                {
                    LeftThumbstickX = EngineMath.Clamp((float)xiGamepad.LeftThumbX / (float)short.MaxValue, -1f, 1f),
                    LeftThumbstickY = EngineMath.Clamp((float)xiGamepad.LeftThumbY / (float)short.MaxValue, -1f, 1f),
                    LeftTrigger = EngineMath.Clamp((float)xiGamepad.LeftTrigger / 255f, 0f, 1f),
                    RightThumbstickX = EngineMath.Clamp((float)xiGamepad.RightThumbX / (float)short.MaxValue, -1f, 1f),
                    RightThumbstickY = EngineMath.Clamp((float)xiGamepad.RightThumbY / (float)short.MaxValue, -1f, 1f),
                    RightTrigger = EngineMath.Clamp((float)xiGamepad.RightTrigger / 255f, 0, 1f)
                };

                // Convert button states
                GamepadButton pressedButtons = GamepadButton.None;
                if (xiGamepad.Buttons.HasFlag(XI.GamepadButtonFlags.A)) { pressedButtons |= GamepadButton.A; }
                if (xiGamepad.Buttons.HasFlag(XI.GamepadButtonFlags.B)) { pressedButtons |= GamepadButton.B; }
                if (xiGamepad.Buttons.HasFlag(XI.GamepadButtonFlags.Back)) { pressedButtons |= GamepadButton.View; }
                if (xiGamepad.Buttons.HasFlag(XI.GamepadButtonFlags.DPadDown)) { pressedButtons |= GamepadButton.DPadDown; }
                if (xiGamepad.Buttons.HasFlag(XI.GamepadButtonFlags.DPadLeft)) { pressedButtons |= GamepadButton.DPadLeft; }
                if (xiGamepad.Buttons.HasFlag(XI.GamepadButtonFlags.DPadRight)) { pressedButtons |= GamepadButton.DPadRight; }
                if (xiGamepad.Buttons.HasFlag(XI.GamepadButtonFlags.DPadUp)) { pressedButtons |= GamepadButton.DPadUp; }
                if (xiGamepad.Buttons.HasFlag(XI.GamepadButtonFlags.LeftShoulder)) { pressedButtons |= GamepadButton.LeftShoulder; }
                if (xiGamepad.Buttons.HasFlag(XI.GamepadButtonFlags.LeftThumb)) { pressedButtons |= GamepadButton.LeftThumbstick; }
                if (xiGamepad.Buttons.HasFlag(XI.GamepadButtonFlags.RightShoulder)) { pressedButtons |= GamepadButton.RightShoulder; }
                if (xiGamepad.Buttons.HasFlag(XI.GamepadButtonFlags.RightThumb)) { pressedButtons |= GamepadButton.RightThumbstick; }
                if (xiGamepad.Buttons.HasFlag(XI.GamepadButtonFlags.Start)) { pressedButtons |= GamepadButton.Menu; }
                if (xiGamepad.Buttons.HasFlag(XI.GamepadButtonFlags.X)) { pressedButtons |= GamepadButton.X; }
                if (xiGamepad.Buttons.HasFlag(XI.GamepadButtonFlags.Y)) { pressedButtons |= GamepadButton.Y; }
                repState.Buttons = pressedButtons;

                // Report controller state to the system
                m_states[loop].NotifyState(repState);
            }

            // Now return all input states
            for (int loop=0; loop<m_states.Length; loop++)
            {
                yield return m_states[loop];
            }
        }
    }
}