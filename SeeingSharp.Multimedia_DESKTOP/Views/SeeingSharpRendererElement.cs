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
using SeeingSharp;
using SeeingSharp.Infrastructure;
using SeeingSharp.Multimedia.Core;
using SeeingSharp.Multimedia.Drawing2D;
using SeeingSharp.Multimedia.Drawing3D;
using SeeingSharp.Multimedia.Input;
using SeeingSharp.Util;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Reactive.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Threading;

//Some namespace mappings
using D3D11 = SharpDX.Direct3D11;
using DXGI = SharpDX.DXGI;
using GDI = System.Drawing;

namespace SeeingSharp.Multimedia.Views
{
    [ContentProperty("SceneComponents")]
    public partial class SeeingSharpRendererElement : Image, IInputEnabledView, ISeeingSharpPainter, IRenderLoopHost
    {
        #region Dependency properties
        public static readonly DependencyProperty SceneProperty =
            DependencyProperty.Register(nameof(Scene), typeof(Scene), typeof(SeeingSharpRendererElement), new PropertyMetadata(new Scene()));
        public static readonly DependencyProperty CameraProperty =
            DependencyProperty.Register(nameof(Camera), typeof(Camera3DBase), typeof(SeeingSharpRendererElement), new PropertyMetadata(new PerspectiveCamera3D()));
        public static readonly DependencyProperty DrawingLayer2DProperty =
            DependencyProperty.Register(nameof(DrawingLayer2D), typeof(Custom2DDrawingLayer), typeof(SeeingSharpRendererElement), new PropertyMetadata(null));
        #endregion

        private static Duration MAX_IMAGE_LOCK_DURATION = new Duration(TimeSpan.FromMilliseconds(100.0));

        #region Some members..
        private RenderLoop m_renderLoop;
        private HigherD3DImageSource m_d3dImageSource;
        private int m_lastRecreateWidth;
        private int m_lastRecreateHeight;
        private IDisposable m_controlObserver;
        #endregion

        #region All needed direct3d resources
        private D3D11.Texture2D m_backBufferForWpf;
        private D3D11.Texture2D m_backBufferD3D11;
        private D3D11.Texture2D m_depthBuffer;
        private D3D11.RenderTargetView m_renderTarget;
        private D3D11.DepthStencilView m_renderTargetDepth;
        private DXGI.Surface m_renderTarget2DDxgi;
        #endregion

        #region Some size related properties
        private int m_renderTargetHeight;
        private int m_renderTargetWidth;
        private int m_viewportHeight;
        private int m_viewportWidth;
        #endregion

        #region State members for handling rendering problems
        private int m_isDirtyCount = 0;
        private BitmapSource m_dummyBitmap;
        #endregion

        #region Change events
        public event EventHandler SceneChanged;
        public event EventHandler CameraChanged;
        public event EventHandler DrawingLayer2DChanged;
        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="SeeingSharpRendererElement"/> class.
        /// </summary>
        public SeeingSharpRendererElement()
        {
            this.Loaded += OnLoaded;
            this.Unloaded += OnUnloaded;

            // Basic configuration
            this.Focusable = true;
            this.IsHitTestVisible = true;
            this.Stretch = System.Windows.Media.Stretch.Fill;
            this.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
            this.VerticalAlignment = System.Windows.VerticalAlignment.Stretch;

            // Load ummy bitmap
            using (System.Drawing.Bitmap dummyOrig = Properties.Resources.GfxNotInitialized)
            {
                m_dummyBitmap = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                    dummyOrig.GetHbitmap(),
                    IntPtr.Zero,
                    System.Windows.Int32Rect.Empty,
                    BitmapSizeOptions.FromWidthAndHeight(500, 500));
            }
            this.Source = m_dummyBitmap;

            //Create the RenderLoop object
            GraphicsCore.Touch();
            m_renderLoop = new RenderLoop(
                SynchronizationContext.Current, this, isDesignMode: this.IsInDesignMode());

            // Break here if we are in design mode
            if (this.IsInDesignMode()) { return; }

            m_renderLoop.ClearColor = Color4.Transparent;
            m_renderLoop.CallPresentInUIThread = true;

            // Create new scene and camera object
            if (SeeingSharpApplication.IsInitialized)
            {
                this.Scene = new Core.Scene();
                this.Camera = new PerspectiveCamera3D();
            }

            //Attach to SizeChanged event (refresh view resources only after a specific time)
            if (SeeingSharpApplication.IsInitialized)
            {
                // Observe events and trigger rendreing as long as the control lives
                this.Loaded += (sender, eArgs) =>
                {
                    if (m_controlObserver == null)
                    {
                        m_controlObserver = CommonTools.DisposeObject(m_controlObserver);
                        m_controlObserver = Observable.FromEventPattern<EventArgs>(this, "SizeChanged")
                            .Merge(Observable.FromEventPattern<EventArgs>(m_renderLoop.ViewConfiguration, "ConfigurationChanged"))
                            .Throttle(TimeSpan.FromSeconds(0.5))
                            .ObserveOn(SynchronizationContext.Current)
                            .Subscribe((innerEArgs) => OnThrottledSizeChanged());

                        SystemEvents.SessionSwitch += OnSystemEvents_SessionSwitch;
                    }
                };
                this.Unloaded += (sender, eArgs) =>
                {
                    if (m_controlObserver != null)
                    {
                        SystemEvents.SessionSwitch -= OnSystemEvents_SessionSwitch;

                        m_controlObserver = CommonTools.DisposeObject(m_controlObserver);
                    }
                };
            }
        }

        /// <summary>
        /// Called when the render size has changed.
        /// </summary>
        /// <param name="sizeInfo">New size information.</param>
        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);

            if (!GraphicsCore.IsInitialized) { return; }

            // Break here if we are in design mode
            if (this.IsInDesignMode()) { return; }

            // Update render size
            m_renderLoop.Camera.SetScreenSize((int)this.RenderSize.Width, (int)this.RenderSize.Height);

            //Resize render target only on greater size changes
            double resizeFactorWidth = sizeInfo.NewSize.Width > m_renderTargetWidth ? sizeInfo.NewSize.Width / m_renderTargetWidth : m_renderTargetWidth / sizeInfo.NewSize.Width;
            double resizeFactorHeight = sizeInfo.NewSize.Height > m_renderTargetHeight ? sizeInfo.NewSize.Height / m_renderTargetHeight : m_renderTargetHeight / sizeInfo.NewSize.Height;
            if ((resizeFactorWidth > 1.3) || (resizeFactorHeight > 1.3))
            {
                m_renderLoop.SetCurrentViewSize((int)this.RenderSize.Width, (int)this.RenderSize.Height);
            }
        }

        /// <summary>
        /// Is this object in design mode?
        /// </summary>
        private bool IsInDesignMode()
        {
            return DesignerProperties.GetIsInDesignMode(this);
        }

        /// <summary>
        /// Gets the current DpiScaling mode.
        /// </summary>
        public DpiScaling GetDpiScaling()
        {
            PresentationSource source = PresentationSource.FromVisual(this);
            double dpiScaleFactorX = 1.0;
            double dpiScaleFactorY = 1.0;
            if (source != null)
            {
                dpiScaleFactorX = source.CompositionTarget.TransformToDevice.M11;
                dpiScaleFactorY = source.CompositionTarget.TransformToDevice.M22;
            }

            DpiScaling result = DpiScaling.Default;
            result.DpiX = (float)(result.DpiX * dpiScaleFactorX);
            result.DpiY = (float)(result.DpiY * dpiScaleFactorY);
            return result;
        }

        /// <summary>
        /// Disposes all loaded view resources.
        /// </summary>
        void IRenderLoopHost.OnRenderLoop_DisposeViewResources(EngineDevice engineDevice)
        {
            if (m_d3dImageSource != null)
            {
                this.Source = m_dummyBitmap;

                // Dispose the render target
                m_d3dImageSource.SetRenderTarget(null);
                m_d3dImageSource.Dispose();
                m_d3dImageSource = null;
            }

            // Dispose all other resources
            m_renderTarget2DDxgi = GraphicsHelper.DisposeObject(m_renderTarget2DDxgi);
            m_renderTargetDepth = GraphicsHelper.DisposeObject(m_renderTargetDepth);
            m_depthBuffer = GraphicsHelper.DisposeObject(m_depthBuffer);
            m_renderTarget = GraphicsHelper.DisposeObject(m_renderTarget);
            m_backBufferForWpf = GraphicsHelper.DisposeObject(m_backBufferForWpf);
            m_backBufferD3D11 = GraphicsHelper.DisposeObject(m_backBufferD3D11);
        }

        /// <summary>
        /// Create all view resources.
        /// </summary>
        Tuple<D3D11.Texture2D, D3D11.RenderTargetView, D3D11.Texture2D, D3D11.DepthStencilView, SharpDX.Mathematics.Interop.RawViewportF, Size2, DpiScaling> IRenderLoopHost.OnRenderLoop_CreateViewResources(EngineDevice engineDevice)
        {
            // Calculate pixel with and high of this visual
            Size pixelSize = this.GetPixelSize(new Size(100.0, 100.0));
            int width = (int)pixelSize.Width;
            int height = (int)pixelSize.Height;

            //Get references to current render device
            D3D11.Device renderDevice = engineDevice.DeviceD3D11_1;
            D3D11.DeviceContext renderDeviceContext = renderDevice.ImmediateContext;

            //Create the swap chain and the render target
            m_backBufferD3D11 = GraphicsHelper.CreateRenderTargetTexture(engineDevice, width, height, m_renderLoop.ViewConfiguration);
            m_backBufferForWpf = GraphicsHelper.CreateSharedTexture(engineDevice, width, height);
            m_renderTarget = new D3D11.RenderTargetView(renderDevice, m_backBufferD3D11);

            //Create the depth buffer
            m_depthBuffer = GraphicsHelper.CreateDepthBufferTexture(engineDevice, width, height, m_renderLoop.ViewConfiguration);
            m_renderTargetDepth = new D3D11.DepthStencilView(renderDevice, m_depthBuffer);

            //Apply render target size values
            m_renderTargetWidth = width;
            m_renderTargetHeight = height;

            //Define the viewport for rendering
            SharpDX.Mathematics.Interop.RawViewportF viewPort = GraphicsHelper.CreateDefaultViewport(width, height);

            //Apply new width and height values of the viewport
            m_viewportWidth = width;
            m_viewportHeight = height;

            //Create and apply the image source object
            m_d3dImageSource = new HigherD3DImageSource(engineDevice);
            m_d3dImageSource.SetRenderTarget(m_backBufferForWpf);
            if (this.Source == m_dummyBitmap) 
            { 
                this.Source = m_d3dImageSource; 
            }

            m_lastRecreateWidth = width;
            m_lastRecreateHeight = height;

            //Return all generated objects
            return Tuple.Create(m_backBufferD3D11, m_renderTarget, m_depthBuffer, m_renderTargetDepth, viewPort, new Size2(width, height), GetDpiScaling());
        }

        /// <summary>
        /// Called when RenderLoop object checks wheter it is possible to render.
        /// </summary>
        bool IRenderLoopHost.OnRenderLoop_CheckCanRender(EngineDevice engineDevice)
        {
            if (m_d3dImageSource == null) { return false; }
            if (!m_d3dImageSource.IsFrontBufferAvailable) { return false; }
            if (!m_d3dImageSource.HasRenderTarget) { return false; }
            if (this.Width <= 0) { return false; }
            if (this.Height <= 0) { return false; }
            if ((CommonTools.ReadPrivateMember<bool, D3DImage>(m_d3dImageSource, "_isDirty")) ||
                (CommonTools.ReadPrivateMember<IntPtr, D3DImage>(m_d3dImageSource, "_pUserSurfaceUnsafe") == IntPtr.Zero))
            {
                m_isDirtyCount++;
                if (m_isDirtyCount > 20)
                {
                    m_renderLoop.ViewConfiguration.ViewNeedsRefresh = true;
                    return true;
                }
                return false; 
            }
            else
            {
                m_isDirtyCount = 0;
            }

            return true;
        }

        /// <summary>
        /// Called when the render loop prepares rendering.
        /// </summary>
        /// <param name="engineDevice">The engine device.</param>
        void IRenderLoopHost.OnRenderLoop_PrepareRendering(EngineDevice engineDevice)
        {

        }

        /// <summary>
        /// Called when RenderLoop wants to present its results.
        /// </summary>
        void IRenderLoopHost.OnRenderLoop_Present(EngineDevice engineDevice)
        {
            if (m_d3dImageSource == null) { return; }
            if (!this.IsLoaded) { return; }

            bool isLocked = false;
            GraphicsCore.Current.PerformanceCalculator.ExecuteAndMeasureActivityDuration(
                "Render.Lock", 
                () => isLocked = m_d3dImageSource.TryLock(MAX_IMAGE_LOCK_DURATION));
            if (!isLocked) { return; }
            try
            {
                // Draw current 3d scene to wpf
                D3D11.DeviceContext deviceContext = engineDevice.DeviceImmediateContextD3D11;
                deviceContext.ResolveSubresource(m_backBufferD3D11, 0, m_backBufferForWpf, 0, DXGI.Format.B8G8R8A8_UNorm);
                deviceContext.Flush();
                deviceContext.ClearState();

                // Apply true background texture if a cached bitmap was applied before
                if(this.Source != m_d3dImageSource)
                {
                    this.Source = m_d3dImageSource; 
                }

                // Invalidate the D3D image
                m_d3dImageSource.InvalidateD3DImage();
            }
            finally
            {
                m_d3dImageSource.Unlock();
            }
        }

        /// <summary>
        /// Called when RenderLoop has finished rendering.
        /// </summary>
        void IRenderLoopHost.OnRenderLoop_AfterRendering(EngineDevice engineDevice)
        {

        }

        /// <summary>
        /// Called when the image is loaded.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (!GraphicsCore.IsInitialized) { return; }
            if (this.IsInDesignMode()) { return; }

            // Update render size
            m_renderLoop.Camera.SetScreenSize((int)this.RenderSize.Width, (int)this.RenderSize.Height);

            // Now connect this view with the main renderloop
            m_renderLoop.RegisterRenderLoop();

            // Initialize Wpf performance meausre 
            //  (first call performs initialization, all following are ignored)
            GraphicsCore.Current.InitializeWpfRenderPerformanceMeasure();

            //m_refreshTimer = new DispatcherTimer(DispatcherPriority.Render);
            //m_refreshTimer.Tick += M_refreshTimer_Tick;
            //m_refreshTimer.Interval = TimeSpan.FromMilliseconds(7.0);
            //m_refreshTimer.Start();
        }

        //private void M_refreshTimer_Tick(object sender, EventArgs e)
        //{
        //    this.InvalidateVisual();
        //}

        /// <summary>
        /// Called when the current session was switched.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="SessionSwitchEventArgs"/> instance containing the event data.</param>
        private void OnSystemEvents_SessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            if (m_renderLoop == null) { return; }
            if (this.IsInDesignMode()) { return; }

            switch (e.Reason)
            {
                    // Handle session lock/unload events
                    //  => Force recreation of view resources in that case
                case SessionSwitchReason.SessionUnlock:
                case SessionSwitchReason.SessionLock:
                case SessionSwitchReason.SessionLogon:
                    if (m_renderLoop.IsRegisteredOnMainLoop)
                    {
                        m_renderLoop.ViewConfiguration.ViewNeedsRefresh = true;
                    }
                    break;
            }
        }

        /// <summary>
        /// Called when one of the dependency properties has changed.
        /// </summary>
        protected override async void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (!GraphicsCore.IsInitialized) { return; }
            if (this.IsInDesignMode()) { return; }

            if (e.Property == SeeingSharpRendererElement.SceneProperty)
            {
                m_renderLoop.SetScene(this.Scene);
                SceneChanged.Raise(this, EventArgs.Empty);
            }
            else if (e.Property == SeeingSharpRendererElement.CameraProperty)
            {
                m_renderLoop.Camera = this.Camera;
                CameraChanged.Raise(this, EventArgs.Empty);
            }
            else if (e.Property == SeeingSharpRendererElement.DrawingLayer2DProperty)
            {
                if (e.OldValue != null) { await m_renderLoop.Deregister2DDrawingLayerAsync(e.OldValue as Custom2DDrawingLayer); }
                if (e.NewValue != null) { await m_renderLoop.Register2DDrawingLayerAsync(e.NewValue as Custom2DDrawingLayer); }
                DrawingLayer2DChanged.Raise(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Called when size changed event occurred.
        /// </summary>
        private void OnThrottledSizeChanged()
        {
            if (!GraphicsCore.IsInitialized) { return; }
            if (this.IsInDesignMode()) { return; }

            Size pixelSize = this.GetPixelSize(new Size(100.0, 100.0));

            // Refresh view resources if resolution has changed
            if ((pixelSize.Width != m_lastRecreateWidth) ||
                (pixelSize.Height != m_lastRecreateHeight) ||
                (RenderLoop.ViewConfiguration.ViewNeedsRefresh))
            {
                m_renderLoop.SetCurrentViewSize((int)pixelSize.Width, (int)pixelSize.Height);
            }
        }

        /// <summary>
        /// Called when the image is unloaded.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (!GraphicsCore.IsInitialized) { return; }
            if (this.IsInDesignMode()) { return; }

            m_renderLoop.DeregisterRenderLoop();
        }

        /// <summary>
        /// Discard rendering?
        /// </summary>
        public bool DiscardRendering
        {
            get { return m_renderLoop.DiscardRendering; }
            set { m_renderLoop.DiscardRendering = value; }
        }

        /// <summary>
        /// Gets a collection containing all SceneComponents associated to this view.
        /// </summary>
        public ObservableCollection<SceneComponentBase> SceneComponents
        {
            get { return m_renderLoop.SceneComponents; }
        }

        /// <summary>
        /// Gets or sets the currently applied scene.
        /// </summary>
        public Scene Scene
        {
            get { return (Scene)GetValue(SceneProperty); }
            set { SetValue(SceneProperty, value); }
        }

        /// <summary>
        /// Gets or sets the custom layer for 2D rendering.
        /// </summary>
        public Custom2DDrawingLayer DrawingLayer2D
        {
            get { return (Custom2DDrawingLayer)GetValue(DrawingLayer2DProperty); }
            set { SetValue(DrawingLayer2DProperty, value); }
        }

        /// <summary>
        /// Gets or sets the camera.
        /// </summary>
        public Camera3DBase Camera
        {
            get { return (Camera3DBase)GetValue(CameraProperty); }
            set { SetValue(CameraProperty, value); }
        }

        /// <summary>
        /// Gets the RenderLoop that is currently in use.
        /// </summary>
        [Browsable(false)]
        public RenderLoop RenderLoop
        {
            get { return m_renderLoop; }
        }

        /// <summary>
        /// Gets or sets the clear color of this 3D view.
        /// </summary>
        public System.Windows.Media.Color ClearColor
        {
            get { return m_renderLoop.ClearColor.ToWpfColor(); }
            set { m_renderLoop.ClearColor = new Color4(value); }
        }

        /// <summary>
        /// Does the target control have focus?
        /// </summary>
        public bool Focused
        {
            get { return this.IsFocused; }
        }

        /// <summary>
        /// True if the control is connected with the main rendering loop.
        /// False if something went wrong.
        /// </summary>
        [Browsable(false)]
        public bool IsOperational
        {
            get
            {
                return m_renderLoop.IsOperational;
            }
        }
    }
}