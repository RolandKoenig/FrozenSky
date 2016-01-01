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
using System;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SeeingSharp.Checking;
using SeeingSharp.Infrastructure;
using SeeingSharp.Multimedia.Core;
using SeeingSharp.Multimedia.Drawing2D;
using SeeingSharp.Multimedia.Drawing3D;
using SeeingSharp.Multimedia.Objects;
using SeeingSharp.Util;

// Define assembly attributes for type that is defined in this file
[assembly: AssemblyQueryableType(
    targetType: typeof(SeeingSharp.Samples.Base.Direct2D.Direct2DAnimatedTextureSample),
    contractType: typeof(SeeingSharp.Samples.Base.SampleBase))]

namespace SeeingSharp.Samples.Base.Direct2D
{
    [SampleInfo(
        Constants.SAMPLEGROUP_DIRECT2D, "Draw to Texture (animated)",
        Constants.SAMPLE_DIRECT2D_D2DANIMTEXTURE_ORDER,
        "https://github.com/RolandKoenig/SeeingSharp/blob/master/Samples/SeeingSharp.Samples.Base/_Samples/Direct2D/Direct2DAnimatedTextureSample.cs",
        SampleTargetPlatform.All)]
    public class Direct2DAnimatedTextureSample : SampleBase
    {
        private StandardBitmapResource m_starBitmap;
        private SolidBrushResource m_borderBrush;

        /// <summary>
        /// Called when the sample has to startup.
        /// </summary>
        /// <param name="targetRenderLoop">The target render loop.</param>
        public override async Task OnStartupAsync(RenderLoop targetRenderLoop)
        {
            targetRenderLoop.EnsureNotNull("targetRenderLoop");

            // Build dummy scene
            Scene scene = targetRenderLoop.Scene;
            Camera3DBase camera = targetRenderLoop.Camera as Camera3DBase;

            // Create all objects for animation
            List<Vector2> starLocations = new List<Vector2>();
            m_starBitmap = new StandardBitmapResource(
                new AssemblyResourceUriBuilder(
                    "SeeingSharp.Samples.Base", false,
                    "Assets/Bitmaps/StarColored_128x128.png"));
            m_borderBrush = new SolidBrushResource(Color4.SteelBlue);
            Random starCreateRandomizer = new Random();

            // 2D rendering is made here
            Custom2DDrawingLayer d2dDrawingLayer = new Custom2DDrawingLayer((graphics) =>
            {
                // Draw background
                RectangleF d2dRectangle = new RectangleF(10, 10, 236, 236);
                graphics.Clear(Color4.LightBlue);

                // Dynamically create new stars
                if((starLocations.Count < 50) &&
                   (starCreateRandomizer.Next(0, 100) <= 70))
                {
                    starLocations.Add(new Vector2(
                        (float)starCreateRandomizer.Next(0, 256),
                        -32f));
                }

                // Update and draw all stars
                for(int loopStar =0; loopStar<starLocations.Count; loopStar++)
                {
                    Vector2 actLocation = starLocations[loopStar];
                    if(actLocation.Y > 270f)
                    {
                        starLocations.RemoveAt(loopStar);
                        loopStar--;
                        continue;
                    }

                    actLocation.Y = actLocation.Y + 4f;
                    starLocations[loopStar] = actLocation;

                    graphics.DrawBitmap(
                        m_starBitmap,
                        new RectangleF(
                            actLocation.X - 16f, actLocation.Y - 16f,
                            32f, 32f),
                        0.6f,
                        BitmapInterpolationMode.Linear);
                }

                // Draw a simple border
                graphics.DrawRectangle(graphics.ScreenBounds, m_borderBrush, 2f);
            });

            // Build 3D scene
            await targetRenderLoop.Scene.ManipulateSceneAsync((manipulator) =>
            {
                // Create floor
                SampleSceneBuilder.BuildStandardConveyorFloor(
                    manipulator, Scene.DEFAULT_LAYER_NAME);

                // Define Direct2D texture resource
                var resD2DTexture = manipulator.AddResource<Direct2DTextureResource>(
                    () => new Direct2DTextureResource(d2dDrawingLayer, 256, 256));
                var resD2DMaterial = manipulator.AddSimpleColoredMaterial(resD2DTexture);

                // Create pallet geometry resource
                PalletType pType = new PalletType();
                pType.PalletMaterial = NamedOrGenericKey.Empty;
                pType.ContentMaterial = resD2DMaterial;
                var resPalletGeometry = manipulator.AddResource<GeometryResource>(
                    () => new GeometryResource(pType));

                // Create pallet object
                GenericObject palletObject = manipulator.AddGeneric(resPalletGeometry);
                palletObject.Color = Color4.GreenColor;
                palletObject.EnableShaderGeneratedBorder();
                palletObject.BuildAnimationSequence()
                    .RotateEulerAnglesTo(new Vector3(0f, EngineMath.RAD_180DEG, 0f), TimeSpan.FromSeconds(2.0))
                    .WaitFinished()
                    .RotateEulerAnglesTo(new Vector3(0f, EngineMath.RAD_360DEG, 0f), TimeSpan.FromSeconds(2.0))
                    .WaitFinished()
                    .CallAction(() => palletObject.RotationEuler = Vector3.Zero)
                    .ApplyAndRewind();
            });

            // Configure camera
            camera.Position = new Vector3(2f, 2f, 2f);
            camera.Target = new Vector3(0f, 0.5f, 0f);
            camera.UpdateCamera();
        }

        public override void OnClosed()
        {
            base.OnClosed();

            CommonTools.SafeDispose(ref m_starBitmap);
            CommonTools.SafeDispose(ref m_borderBrush);
        }
    }
}