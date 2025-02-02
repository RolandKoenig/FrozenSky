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
using SeeingSharp;
using SeeingSharp.Multimedia.Core;
using SeeingSharp.Multimedia.Drawing3D;
using SeeingSharp.Util;
using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

// Namespace mappings
using D3D11 = SharpDX.Direct3D11;
using SeeingSharp.Multimedia.Input;
using SharpDX;
using System.Collections.Generic;

namespace SeeingSharp.Multimedia.Views
{
    //For handling of staging resource see
    // http://msdn.microsoft.com/en-us/library/windows/desktop/ff476259(v=vs.85).aspx
    public class MemoryRenderTarget : IDisposable, ISeeingSharpPainter, IRenderLoopHost
    {
        #region Configuration
        private int m_pixelWidth;
        private int m_pixelHeight;
        #endregion

        #region Reference to the render loop
        private RenderLoop m_renderLoop;
        private ThreadSaveQueue<TaskCompletionSource<object>> m_renderAwaitors;
        #endregion

        #region All needed direct3d resources
        private D3D11.Device m_device;
        private D3D11.DeviceContext m_deviceContext;
        private D3D11.Texture2D m_copyHelperTextureStaging;
        private D3D11.Texture2D m_renderTarget;
        private D3D11.Texture2D m_renderTargetDepth;
        private D3D11.RenderTargetView m_renderTargetView;
        private D3D11.DepthStencilView m_renderTargetDepthView;
        #endregion

        /// <summary>
        /// Raises before the render target starts rendering.
        /// </summary>
        public event CancelEventHandler BeforeRender;

        /// <summary>
        /// Initializes a new instance of the <see cref="MemoryRenderTarget" /> class.
        /// </summary>
        /// <param name="pixelHeight">Height of the offline render target in pixels.</param>
        /// <param name="pixelWidth">Width of the offline render target in pixels.</param>
        /// <param name="syncContext">Sets the SynchronizationContext which should be used by default.</param>
        public MemoryRenderTarget(int pixelWidth, int pixelHeight, SynchronizationContext syncContext = null)
        {
            //Set confiugration
            m_pixelWidth = pixelWidth;
            m_pixelHeight = pixelHeight;

            m_renderAwaitors = new ThreadSaveQueue<TaskCompletionSource<object>>();
            if (syncContext == null) { syncContext = new SynchronizationContext(); }

            //Create the RenderLoop object
            GraphicsCore.Touch();
            m_renderLoop = new RenderLoop(syncContext, this);
            m_renderLoop.Camera.SetScreenSize(pixelWidth, pixelHeight);
            m_renderLoop.RegisterRenderLoop();
        }

        /// <summary>
        /// Awaits next render.
        /// </summary>
        public Task AwaitRenderAsync()
        {
            if (!this.IsOperational) { return Task.Delay(100); }

            TaskCompletionSource<object> result = new TaskCompletionSource<object>();
            m_renderAwaitors.Enqueue(result);

            return result.Task;
        }

        /// <summary>
        /// Führt anwendungsspezifische Aufgaben aus, die mit dem Freigeben, Zurückgeben oder Zurücksetzen von nicht verwalteten Ressourcen zusammenhängen.
        /// </summary>
        public void Dispose()
        {
            m_renderLoop.Dispose();
        }

        /// <summary>
        /// Disposes all loaded view resources.
        /// </summary>
        void IRenderLoopHost.OnRenderLoop_DisposeViewResources(EngineDevice device)
        {
            m_renderTargetDepthView = GraphicsHelper.DisposeObject(m_renderTargetDepthView);
            m_renderTargetDepth = GraphicsHelper.DisposeObject(m_renderTargetDepth);
            m_renderTargetView = GraphicsHelper.DisposeObject(m_renderTargetView);
            m_renderTarget = GraphicsHelper.DisposeObject(m_renderTarget);
            m_copyHelperTextureStaging = GraphicsHelper.DisposeObject(m_copyHelperTextureStaging);

            m_device = null;
            m_deviceContext = null;
        }

        /// <summary>
        /// Create all view resources.
        /// </summary>
        Tuple<D3D11.Texture2D, D3D11.RenderTargetView, D3D11.Texture2D, D3D11.DepthStencilView, SharpDX.Mathematics.Interop.RawViewportF, Size2, DpiScaling> IRenderLoopHost.OnRenderLoop_CreateViewResources(EngineDevice device)
        {
            int width = m_pixelWidth;
            int height = m_pixelHeight;

            //Get references to current render device
            m_device = device.DeviceD3D11_1;
            m_deviceContext = m_device.ImmediateContext;

            //Create the swap chain and the render target
            m_renderTarget = GraphicsHelper.CreateRenderTargetTexture(device, width, height, m_renderLoop.ViewConfiguration);
            m_renderTargetView = new D3D11.RenderTargetView(m_device, m_renderTarget);

            //Create the depth buffer
            m_renderTargetDepth = GraphicsHelper.CreateDepthBufferTexture(device, width, height, m_renderLoop.ViewConfiguration);
            m_renderTargetDepthView = new D3D11.DepthStencilView(m_device, m_renderTargetDepth);

            //Define the viewport for rendering
            SharpDX.Mathematics.Interop.RawViewportF viewPort = GraphicsHelper.CreateDefaultViewport(width, height);

            //Return all generated objects
            return Tuple.Create(m_renderTarget, m_renderTargetView, m_renderTargetDepth, m_renderTargetDepthView, viewPort, new Size2(width, height), DpiScaling.Default);
        }

        /// <summary>
        /// Called when RenderLoop object checks wheter it is possible to render.
        /// </summary>
        bool IRenderLoopHost.OnRenderLoop_CheckCanRender(EngineDevice device)
        {
            CancelEventArgs eventArgs = new CancelEventArgs(false);
            if (BeforeRender != null) { BeforeRender(this, eventArgs); }

            return !eventArgs.Cancel;
        }

        void IRenderLoopHost.OnRenderLoop_PrepareRendering(EngineDevice device)
        {
            
        }

        /// <summary>
        /// Called when RenderLoop wants to present its results.
        /// </summary>
        void IRenderLoopHost.OnRenderLoop_Present(EngineDevice device)
        {
            // Finish rendering of all render tasks
            m_deviceContext.Flush();
            m_deviceContext.ClearState();

            // Notify all render awaitors (callers of AwaitRenderAsync method)
            m_renderAwaitors.DequeueAll().ForEachInEnumeration(actAwaitor =>
                {
                    actAwaitor.SetResult(null);
                });
        }

        /// <summary>
        /// Called when RenderLoop has finished rendering.
        /// </summary>
        void IRenderLoopHost.OnRenderLoop_AfterRendering(EngineDevice device)
        {

        }

        /// <summary>
        /// Gets or sets the scene.
        /// </summary>
        public Scene Scene
        {
            get { return m_renderLoop.Scene; }
            set { m_renderLoop.SetScene(value); }
        }

        public Camera3DBase Camera
        {
            get { return m_renderLoop.Camera; }
            set { m_renderLoop.Camera = value; }
        }

        public Color4 ClearColor
        {
            get { return m_renderLoop.ClearColor; }
            set { m_renderLoop.ClearColor = value; }
        }

        public SynchronizationContext UISynchronizationContext
        {
            get { return m_renderLoop.UISynchronizationContext; }
        }

        /// <summary>
        /// Gets the renderloop object.
        /// </summary>
        public RenderLoop RenderLoop
        {
            get { return m_renderLoop; }
        }

        public bool IsOperational
        {
            get { return m_renderLoop.IsOperational; }
        }
    }
}
