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
using SeeingSharp.Multimedia.Core;
using SeeingSharp.Multimedia.Drawing3D;
using SeeingSharp.Multimedia.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace SeeingSharp.Multimedia.Components
{
    public class FreeMovingCameraComponent : SceneComponent
    {
        #region Constants
        private const float MOVEMENT = 0.3f;
        private const float ROTATION = 0.01f;
        #endregion

        /// <summary>
        /// Attaches this component to a scene.
        /// Be careful, this method gets called from a background thread of seeing#!
        /// It may also be called from multiple scenes in parallel or simply withoud previous Detach call.
        /// </summary>
        /// <param name="manipulator">The manipulator of the scene we attach to.</param>
        /// <param name="correspondingView">The view which attached this component.</param>
        protected override void Attach(SceneManipulator manipulator, ViewInformation correspondingView)
        {

        }

        /// <summary>
        /// Detaches this component from a scene.
        /// Be careful, this method gets called from a background thread of seeing#!
        /// It may also be called from multiple scenes in parallel.
        /// </summary>
        /// <param name="manipulator">The manipulator of the scene we attach to.</param>
        /// <param name="correspondingView">The view which attached this component.</param>
        protected override void Detach(SceneManipulator manipulator, ViewInformation correspondingView)
        {
            // nothing to be detached here
        }

        /// <summary>
        /// This update method gets called on each update pass for each scenes
        /// this component is attached to.
        /// </summary>
        /// <param name="updateState">Current update state.</param>
        /// <param name="correspondingView">The view which attached this component (may be null).</param>
        protected override void Update(SceneRelatedUpdateState updateState, ViewInformation correspondingView)
        {
            Camera3DBase actCamera = correspondingView.Camera;
            if(actCamera == null) { return; }

            foreach (InputFrame actInputFrame in updateState.InputFrames)
            {
                foreach (var actInputState in actInputFrame.GetInputStates(correspondingView))
                {
                    // Handle keyboard
                    KeyboardState actKeyboardState = actInputState as KeyboardState;
                    bool isControlKeyDown = false;
                    if (actKeyboardState != null)
                    {
                        UpdateForKeyboard(actCamera, actKeyboardState, out isControlKeyDown);
                        continue;
                    }

                    // Handle mouse (or pointer)
                    MouseOrPointerState mouseState = actInputState as MouseOrPointerState;
                    if (mouseState != null)
                    {
                        UpdateForMouse(actCamera, isControlKeyDown, mouseState);
                    }
                }
            }
        }

        /// <summary>
        /// Update camera for keyboard input.
        /// </summary>
        private static void UpdateForKeyboard(
            Camera3DBase actCamera, KeyboardState actKeyboardState, 
            out bool isControlKeyDown)
        {
            // Define multiplyer
            float multiplyer = 1f;
            isControlKeyDown = false;
            if (actKeyboardState.IsKeyDown(WinVirtualKey.ControlKey) ||
                actKeyboardState.IsKeyDown(WinVirtualKey.LControlKey) ||
                actKeyboardState.IsKeyDown(WinVirtualKey.RControlKey))
            {
                multiplyer = 2f;
                isControlKeyDown = true;
            }

            foreach (WinVirtualKey actKey in actKeyboardState.KeysDown)
            {
                switch (actKey)
                {
                    case WinVirtualKey.Up:
                    case WinVirtualKey.W:
                        actCamera.Zoom(MOVEMENT * multiplyer);
                        break;

                    case WinVirtualKey.Down:
                    case WinVirtualKey.S:
                        actCamera.Zoom(-MOVEMENT * multiplyer);
                        break;

                    case WinVirtualKey.Left:
                    case WinVirtualKey.A:
                        actCamera.Strave(-MOVEMENT * multiplyer);
                        break;

                    case WinVirtualKey.Right:
                    case WinVirtualKey.D:
                        actCamera.Strave(MOVEMENT * multiplyer);
                        break;

                    case WinVirtualKey.Q:
                    case WinVirtualKey.NumPad3:
                        actCamera.Move(new Vector3(0f, -MOVEMENT * multiplyer, 0f));
                        break;

                    case WinVirtualKey.E:
                    case WinVirtualKey.NumPad9:
                        actCamera.Move(new Vector3(0f, MOVEMENT * multiplyer, 0f));
                        break;

                    case WinVirtualKey.NumPad4:
                        actCamera.Rotate(ROTATION, 0f);
                        break;

                    case WinVirtualKey.NumPad2:
                        actCamera.Rotate(0f, -ROTATION);
                        break;

                    case WinVirtualKey.NumPad6:
                        actCamera.Rotate(-ROTATION, 0f);
                        break;

                    case WinVirtualKey.NumPad8:
                        actCamera.Rotate(0f, ROTATION);
                        break;
                }
            }
        }

        /// <summary>
        /// Update camera for mouse input.
        /// </summary>
        private static void UpdateForMouse(Camera3DBase actCamera, bool isControlKeyDown, MouseOrPointerState mouseState)
        {
            // Handle mouse move
            if (mouseState.MoveDistanceDip != Vector2.Zero)
            {
                Vector2 moving = mouseState.MoveDistanceDip;
                if (mouseState.IsButtonDown(MouseButton.Left) &&
                    mouseState.IsButtonDown(MouseButton.Right))
                {
                    actCamera.Zoom(moving.Y / -50f);
                }
                else if (mouseState.IsButtonDown(MouseButton.Left))
                {
                    actCamera.Strave(moving.X / 50f);
                    actCamera.UpDown(-moving.Y / 50f);
                }
                else if (mouseState.IsButtonDown(MouseButton.Right))
                {
                    actCamera.Rotate(-moving.X / 200f, -moving.Y / 200f);
                }
            }

            // Handle mouse wheel
            if (mouseState.WheelDelta != 0)
            {
                float multiplyer = 1f;
                if (isControlKeyDown) { multiplyer = 2f; }
                actCamera.Zoom((mouseState.WheelDelta / 100f) * multiplyer);
            }
        }

        public override string ComponentGroup
        {
            get { return Constants.COMPONENT_GROUP_CAMERA; }
        }

        public override bool IsViewSpecific
        {
            get { return true; }
        }
    }
}
