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
using Xunit;
using System;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Threading.Tasks;
using SeeingSharp.Multimedia.Core;
using SeeingSharp.Multimedia.Views;
using SeeingSharp.Util;
using SeeingSharp.Multimedia.Drawing3D;
using SeeingSharp.Multimedia.Drawing2D;
using SeeingSharp.Multimedia.Objects;
using SeeingSharp.Infrastructure;

// Some namespace mappings
using GDI = System.Drawing;

namespace SeeingSharp.Tests.Rendering
{
    [Collection("Rendering_SeeingSharp")]
    public class RenderToMemoryTests
    {
        public const int MANIPULATE_WAIT_TIME = 500;
        public const string TEST_CATEGORY = "SeeingSharp Multimedia Drawing 3D";

        [Fact]
        [Trait("Category", TEST_CATEGORY)]
        public async Task Render_ClearedScreen()
        {
            await UnitTestHelper.InitializeWithGrahicsAsync();

            using (MemoryRenderTarget memRenderTarget = new MemoryRenderTarget(1024, 1024))
            {
                // Perform rendering
                memRenderTarget.ClearColor = Color4.CornflowerBlue;
                await memRenderTarget.AwaitRenderAsync();

                // Take screenshot
                GDI.Bitmap screenshot = await memRenderTarget.RenderLoop.GetScreenshotGdiAsync();

                // Calculate and check difference
                bool isNearEqual = BitmapComparison.IsNearEqual(
                    screenshot, Properties.Resources.ReferenceImage_ClearedScreen);
                Assert.True(isNearEqual, "Difference to reference image is to big!");
            }

            // Finishing checks
            Assert.True(GraphicsCore.Current.MainLoop.RegisteredRenderLoopCount == 0, "RenderLoops where not disposed correctly!");
        }

        [Fact]
        [Trait("Category", TEST_CATEGORY)]
        public async Task Render_SimpleLine()
        {
            await UnitTestHelper.InitializeWithGrahicsAsync();

            using (MemoryRenderTarget memRenderTarget = new MemoryRenderTarget(1024, 1024))
            {
                memRenderTarget.ClearColor = Color4.CornflowerBlue;

                // Get and configure the camera
                PerspectiveCamera3D camera = memRenderTarget.Camera as PerspectiveCamera3D;
                camera.Position = new Vector3(0f, 5f, -7f);
                camera.Target = new Vector3(0f, 0f, 0f);
                camera.UpdateCamera();

                // Define scene
                await memRenderTarget.Scene.ManipulateSceneAsync((manipulator) =>
                {
                    WireObject wireObject = new WireObject();
                    wireObject.LineData = new Line[]{
                        new Line(
                            new Vector3(-0.5f, 0f, -0.5f),
                            new Vector3(0.5f, 0f, -0.5f)),
                        new Line(
                            new Vector3(0.5f, 0f, -0.5f),
                            new Vector3(0.5f, 0f, 0.5f)),
                        new Line(
                            new Vector3(0.5f, 0f, 0.5f),
                            new Vector3(-0.5f, 0f, 0.5f)),
                        new Line(
                            new Vector3(-0.5f, 0f, 0.5f),
                            new Vector3(-0.5f, 0f, -0.5f)),
                    };
                    wireObject.LineColor = Color4.RedColor;
                    manipulator.Add(wireObject);
                });

                // Take screenshot
                GDI.Bitmap screenshot = await memRenderTarget.RenderLoop.GetScreenshotGdiAsync();

                //screenshot.DumpToDesktop("Blub");

                // Calculate and check difference
                bool isNearEqual = BitmapComparison.IsNearEqual(
                    screenshot, Properties.Resources.ReferenceImage_SimpleLine);
                Assert.True(isNearEqual, "Difference to reference image is to big!");
            }
        }

        [Fact]
        [Trait("Category", TEST_CATEGORY)]
        public async Task Render_SimpleObject()
        {
            await UnitTestHelper.InitializeWithGrahicsAsync();

            using (MemoryRenderTarget memRenderTarget = new MemoryRenderTarget(1024, 1024))
            {
                memRenderTarget.ClearColor = Color4.CornflowerBlue;

                // Get and configure the camera
                PerspectiveCamera3D camera = memRenderTarget.Camera as PerspectiveCamera3D;
                camera.Position = new Vector3(0f, 5f, -7f);
                camera.Target = new Vector3(0f, 0f, 0f);
                camera.UpdateCamera();

                await memRenderTarget.AwaitRenderAsync();

                // Define scene
                await memRenderTarget.Scene.ManipulateSceneAsync((manipulator) =>
                {
                    NamedOrGenericKey geoResource = manipulator.AddResource<GeometryResource>(
                        () => new GeometryResource(new PalletType()));

                    GenericObject newObject = manipulator.AddGeneric(geoResource);
                    newObject.RotationEuler = new Vector3(0f, EngineMath.RAD_90DEG / 2f, 0f);
                    newObject.Scaling = new Vector3(2f, 2f, 2f);
                    newObject.TrySetInitialVisibility(memRenderTarget.RenderLoop.ViewInformation, true);
                });

                // Take screenshot
                GDI.Bitmap screenshot = await memRenderTarget.RenderLoop.GetScreenshotGdiAsync();

                //screenshot.DumpToDesktop("Blub.png");

                // Calculate and check difference
                bool isNearEqual = BitmapComparison.IsNearEqual(
                    screenshot, Properties.Resources.ReferenceImage_SimpleObject);
                Assert.True(isNearEqual, "Difference to reference image is to big!");
            }

            // Finishing checks
            Assert.True(GraphicsCore.Current.MainLoop.RegisteredRenderLoopCount == 0, "RenderLoops where not disposed correctly!");
        }

        [Fact]
        [Trait("Category", TEST_CATEGORY)]
        public async Task Render_SimpleObject_StackedGeometry()
        {
            await UnitTestHelper.InitializeWithGrahicsAsync();

            using (MemoryRenderTarget memRenderTarget = new MemoryRenderTarget(1024, 1024))
            {
                memRenderTarget.ClearColor = Color4.CornflowerBlue;

                // Get and configure the camera
                PerspectiveCamera3D camera = memRenderTarget.Camera as PerspectiveCamera3D;
                camera.Position = new Vector3(0f, 5f, -7f);
                camera.Target = new Vector3(0f, 1f, 0f);
                camera.UpdateCamera();

                await memRenderTarget.AwaitRenderAsync();

                // Define scene
                await memRenderTarget.Scene.ManipulateSceneAsync((manipulator) =>
                {
                    PalletType palType = new PalletType();
                    palType.ContentHeight = 0f;
                    StackedObjectType stackedType = new StackedObjectType(palType, 10);

                    NamedOrGenericKey geoResource = manipulator.AddResource<GeometryResource>(
                        () => new GeometryResource(stackedType));

                    GenericObject newObject = manipulator.AddGeneric(geoResource);
                    newObject.RotationEuler = new Vector3(0f, EngineMath.RAD_90DEG / 2f, 0f);
                    newObject.Scaling = new Vector3(2f, 2f, 2f);
                    newObject.TrySetInitialVisibility(memRenderTarget.RenderLoop.ViewInformation, true);
                });

                // Take screenshot
                GDI.Bitmap screenshot = await memRenderTarget.RenderLoop.GetScreenshotGdiAsync();

                //screenshot.DumpToDesktop("Blub.png");

                // Calculate and check difference
                bool isNearEqual = BitmapComparison.IsNearEqual(
                    screenshot, Properties.Resources.ReferenceImage_SimpleObject_StackedGeometry);
                Assert.True(isNearEqual, "Difference to reference image is to big!");
            }

            // Finishing checks
            Assert.True(GraphicsCore.Current.MainLoop.RegisteredRenderLoopCount == 0, "RenderLoops where not disposed correctly!");
        }

        [Fact]
        [Trait("Category", TEST_CATEGORY)]
        public async Task Render_SimpleObject_Transparent()
        {
            await UnitTestHelper.InitializeWithGrahicsAsync();

            using (MemoryRenderTarget memRenderTarget = new MemoryRenderTarget(1024, 1024))
            {
                memRenderTarget.ClearColor = Color4.CornflowerBlue;

                // Get and configure the camera
                PerspectiveCamera3D camera = memRenderTarget.Camera as PerspectiveCamera3D;
                camera.Position = new Vector3(0f, 5f, -7f);
                camera.Target = new Vector3(0f, 0f, 0f);
                camera.UpdateCamera();

                // Define scene
                await memRenderTarget.Scene.ManipulateSceneAsync((manipulator) =>
                {
                    NamedOrGenericKey geoResource = manipulator.AddResource<GeometryResource>(
                        () => new GeometryResource(new PalletType()));

                    GenericObject newObject = manipulator.AddGeneric(geoResource);
                    newObject.RotationEuler = new Vector3(0f, EngineMath.RAD_90DEG / 2f, 0f);
                    newObject.Scaling = new Vector3(2f, 2f, 2f);
                    newObject.Opacity = 0.5f;
                });

                // Take screenshot
                GDI.Bitmap screenshot = await memRenderTarget.RenderLoop.GetScreenshotGdiAsync();
                screenshot = await memRenderTarget.RenderLoop.GetScreenshotGdiAsync();

                //screenshot.DumpToDesktop("Blub.png");

                // Calculate and check difference
                bool isNearEqual = BitmapComparison.IsNearEqual(
                    screenshot, Properties.Resources.ReferenceImage_SimpleObject_Transparent);
                Assert.True(isNearEqual, "Difference to reference image is to big!");
            }

            // Finishing checks
            Assert.True(GraphicsCore.Current.MainLoop.RegisteredRenderLoopCount == 0, "RenderLoops where not disposed correctly!");
        }

        [Fact]
        [Trait("Category", TEST_CATEGORY)]
        public async Task Render_SimpleObject_Orthographic()
        {
            await UnitTestHelper.InitializeWithGrahicsAsync();

            using (MemoryRenderTarget memRenderTarget = new MemoryRenderTarget(1024, 1024))
            {
                memRenderTarget.ClearColor = Color4.CornflowerBlue;

                // Get and configure the camera
                OrthographicCamera3D camera = new OrthographicCamera3D();
                camera.Position = new Vector3(0f, 5f, -7f);
                camera.Target = new Vector3(0f, 1f, 0f);
                camera.ZoomFactor = 200f;
                camera.UpdateCamera();
                memRenderTarget.RenderLoop.Camera = camera;

                await memRenderTarget.AwaitRenderAsync();

                // Define scene
                await memRenderTarget.Scene.ManipulateSceneAsync((manipulator) =>
                {
                    NamedOrGenericKey geoResource = manipulator.AddResource<GeometryResource>(
                        () => new GeometryResource(new PalletType()));

                    GenericObject newObject = manipulator.AddGeneric(geoResource);
                    newObject.RotationEuler = new Vector3(0f, EngineMath.RAD_90DEG / 2f, 0f);
                    newObject.Scaling = new Vector3(2f, 2f, 2f);
                    newObject.TrySetInitialVisibility(memRenderTarget.RenderLoop.ViewInformation, true);
                });

                // Take screenshot
                GDI.Bitmap screenshot = await memRenderTarget.RenderLoop.GetScreenshotGdiAsync();

                // screenshot.DumpToDesktop("Blub.png");

                // Calculate and check difference
                bool isNearEqual = BitmapComparison.IsNearEqual(
                    screenshot, Properties.Resources.ReferenceImage_SimpleObject_Ortho);
                Assert.True(isNearEqual, "Difference to reference image is to big!");
            }

            // Finishing checks
            Assert.True(GraphicsCore.Current.MainLoop.RegisteredRenderLoopCount == 0, "RenderLoops where not disposed correctly!");
        }

        [Fact]
        [Trait("Category", TEST_CATEGORY)]
        public async Task Render_Skybox()
        {
            await UnitTestHelper.InitializeWithGrahicsAsync();

            using (MemoryRenderTarget memRenderTarget = new MemoryRenderTarget(1024, 1024))
            {
                memRenderTarget.ClearColor = Color4.CornflowerBlue;

                // Get and configure the camera
                PerspectiveCamera3D camera = memRenderTarget.Camera as PerspectiveCamera3D;
                camera.Position = new Vector3(-3f, -3f, -7f);
                camera.Target = new Vector3(0f, 0f, 0f);
                camera.UpdateCamera();

                // Define scene
                await memRenderTarget.Scene.ManipulateSceneAsync((manipulator) =>
                {
                    // Create pallet geometry resource
                    PalletType pType = new PalletType();
                    pType.ContentColor = Color4.Transparent;
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

                    var resSkyboxTexture = manipulator.AddTexture(new Uri("/SeeingSharp.Tests.Rendering;component/Ressources/Textures/Skybox.dds", UriKind.Relative));

                    // Create the skybox on a new layer
                    manipulator.AddLayer("Skybox");
                    SkyboxObject skyboxObject = new SkyboxObject(resSkyboxTexture);
                    manipulator.Add(skyboxObject, "Skybox");
                });

                // Take screenshot
                GDI.Bitmap screenshot = await memRenderTarget.RenderLoop.GetScreenshotGdiAsync();
                screenshot = await memRenderTarget.RenderLoop.GetScreenshotGdiAsync();

                //screenshot.DumpToDesktop("Blub.png");

                // Calculate and check difference
                bool isNearEqual = BitmapComparison.IsNearEqual(
                    screenshot, Properties.Resources.ReferenceImage_Skybox);
                Assert.True(isNearEqual, "Difference to reference image is to big!");
            }

            // Finishing checks
            Assert.True(GraphicsCore.Current.MainLoop.RegisteredRenderLoopCount == 0, "RenderLoops where not disposed correctly!");
        }
    }
}
