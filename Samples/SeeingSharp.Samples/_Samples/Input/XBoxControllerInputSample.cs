﻿#region License information (SeeingSharp and all based games/applications)
/*
    Seeing# and all games/applications distributed together with it. 
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
using SeeingSharp.Multimedia.Objects;
using SeeingSharp.Checking;
using SeeingSharp.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using SeeingSharp.Multimedia.Input;
using SeeingSharp.Infrastructure;
using SeeingSharp.Samples.Base;
using SeeingSharp.Samples.Resources;

// Define assembly attributes for type that is defined in this file
[assembly: AssemblyQueryableType(
    targetType: typeof(SeeingSharp.Samples.Base.Input.XBoxControllerInputSample),
    contractType: typeof(SeeingSharp.Samples.Base.SampleBase))]

namespace SeeingSharp.Samples.Base.Input
{
    [SampleInfo(
        Constants.SAMPLEGROUP_INPUT, "XBox Controller",
        Constants.SAMPLE_INPUT_XBOX_CONTROLLER,
        "https://github.com/RolandKoenig/SeeingSharp/blob/master/Samples/SeeingSharp.Samples/_Samples/Input/XBoxControllerInputSample.cs")]
    public class XBoxControllerInputSample : SampleBase
    {
        /// <summary>
        /// Called when the sample has to startup.
        /// </summary>
        /// <param name="targetRenderLoop">The target render loop.</param>
        public override async Task OnStartupAsync(RenderLoop targetRenderLoop)
        {
            targetRenderLoop.EnsureNotNull(nameof(targetRenderLoop));

            // Build dummy scene
            Scene scene = targetRenderLoop.Scene;
            Camera3DBase camera = targetRenderLoop.Camera as Camera3DBase;

            await targetRenderLoop.Scene.ManipulateSceneAsync((manipulator) =>
            {
                // Create floor
                SampleSceneBuilder.BuildStandardFloor(
                    manipulator, Scene.DEFAULT_LAYER_NAME);

                // Define texture and material resource
                var resTexture = manipulator.AddTexture(
                    new AssemblyResourceLink(
                        typeof(SeeingSharpSampleResources),
                        "Textures.SimpleTexture.png"));
                var resMaterial = manipulator.AddSimpleColoredMaterial(resTexture);

                // Create pallet geometry resource
                PalletType pType = new PalletType();
                pType.ContentMaterial = resMaterial;
                var resPalletGeometry = manipulator.AddResource<GeometryResource>(
                    () => new GeometryResource(pType));

                // Create pallet object
                CustomPalletObject palletObject = new CustomPalletObject(resPalletGeometry);
                palletObject.Color = Color4.GreenColor;
                palletObject.EnableShaderGeneratedBorder();
                manipulator.Add(palletObject);
            });

            // Configure camera
            camera.Position = new Vector3(0f, 4f, -4f);
            camera.Target = new Vector3(0f, 0.5f, 0f);
            camera.UpdateCamera();
        }

        //*********************************************************************
        //*********************************************************************
        //*********************************************************************        
        /// <summary>
        /// Custom object which handles controller input within its update method.
        /// </summary>
        private class CustomPalletObject : GenericObject
        {
            public CustomPalletObject(NamedOrGenericKey geoResource)
                : base(geoResource)
            {

            }

            protected override void UpdateInternal(SceneRelatedUpdateState updateState)
            {
                foreach (InputFrame actInputFrame in updateState.InputFrames)
                {
                    GamepadState gamepadState = actInputFrame.DefaultGamepad;
                    if (gamepadState == GamepadState.Dummy) { continue; }

                    Vector3 moveVector = Vector3.Zero;

                    // Handle left/right movement
                    Vector3 moveX = new Vector3(0.1f, 0f, 0f);
                    if (gamepadState.IsButtonDown(GamepadButton.DPadLeft)) { moveVector += -moveX; }
                    else if (gamepadState.IsButtonDown(GamepadButton.DPadRight)) { moveVector += moveX; }
                    else if (Math.Abs(gamepadState.LeftThumbX) > 0.5f)
                    {
                        moveVector += gamepadState.LeftThumbX * moveX;
                    }
                    else if (Math.Abs(gamepadState.RightThumbX) > 0.5f)
                    {
                        moveVector += gamepadState.RightThumbX * moveX;
                    }

                    // Handle up/down movement
                    Vector3 moveZ = new Vector3(0f, 0f, 0.1f);
                    if (gamepadState.IsButtonDown(GamepadButton.DPadDown)) { moveVector += -moveZ; }
                    else if (gamepadState.IsButtonDown(GamepadButton.DPadUp)) { moveVector += moveZ; }
                    else if (Math.Abs(gamepadState.LeftThumbY) > 0.5f)
                    {
                        moveVector += gamepadState.LeftThumbY * moveZ;
                    }
                    else if (Math.Abs(gamepadState.RightThumbY) > 0.5f)
                    {
                        moveVector += gamepadState.RightThumbY * moveZ;
                    }

                    this.Position = this.Position + moveVector;
                }

                base.UpdateInternal(updateState);
            }
        }
    }
}
