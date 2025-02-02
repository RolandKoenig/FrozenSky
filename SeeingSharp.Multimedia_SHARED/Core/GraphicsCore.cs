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
using System.Linq;
using System.ComponentModel;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.Reflection;
using SeeingSharp.Util;
using SeeingSharp.Checking;
using SeeingSharp.Multimedia.Input;
using SeeingSharp.Multimedia.Objects;
using SeeingSharp.Multimedia.PlayingSound;
using SeeingSharp.Infrastructure;

//Some namespace mappings
using D2D = SharpDX.Direct2D1;
using D3D = SharpDX.Direct3D;
using D3D11 = SharpDX.Direct3D11;
using DWrite = SharpDX.DirectWrite;
using WIC = SharpDX.WIC;
using XA = SharpDX.XAudio2;

//Some type mappings
#if WINRT
using DxgiFactory = SharpDX.DXGI.Factory2;
#endif
#if DESKTOP
using DxgiFactory = SharpDX.DXGI.Factory1;
#endif

namespace SeeingSharp.Multimedia.Core
{
    public class GraphicsCore
    {
        #region Members for Unittesting
        private static bool s_throwDeviceInitError;
        private static bool s_throwD2DInitDeviceError;
        #endregion

        #region Members for exception handling
        private static List<EventHandler<InternalCatchedExceptionEventArgs>> s_internalExListeners;
        private static object s_internalExListenersLock;
        #endregion

        #region Singleton instance
        private static GraphicsCore s_current;
        #endregion

        #region Hardware 
        private EngineHardwareInfo m_hardwareInfo;
        private List<EngineDevice> m_devices;
        private EngineDevice m_defaultDevice;
        #endregion

        #region Some helpers
        private GraphicsCoreConfiguration m_configuration;
        private UniqueGenericKeyGenerator m_resourceKeyGenerator;
        private PerformanceAnalyzer m_performanceCalculator;
        private ImporterExporterRepository m_importExporters;
        #endregion

        #region Members for input
        private InputHandlerFactory m_inputHandlerFactory;
        private InputGathererThread m_inputGatherer;
        #endregion

        #region Global device handlers
        private Exception m_initException;
        private FactoryHandlerWIC m_factoryHandlerWIC;
        private FactoryHandlerD2D m_factoryHandlerD2D;
        private FactoryHandlerDWrite m_factoryHandlerDWrite;
        private FactoryHandlerMF m_factoryHandlerMF;
        private FactoryHandlerXAudio2 m_factoryHandlerXAudio2;
        #endregion

        #region Members for sound
        private SoundManager m_soundManager;
        #endregion

        #region Helper for Wpf performance measure
#if DESKTOP
        private IDisposable m_wpfRenderWaiter;
        private bool m_wpfRenderWaitreCreated;
#endif
        #endregion

        #region Members for threading
        private static Task m_defaultInitTask;
        private EngineMainLoop m_mainLoop;
        private Task m_mainLoopTask;
        private CancellationTokenSource m_mainLoopCancelTokenSource;
        #endregion

        #region Configurations
        private bool m_debugEnabled;
        private bool m_force2DFallback;
        #endregion

        /// <summary>
        /// Occurs when [internal cached exception].
        /// </summary>
        public static event EventHandler<InternalCatchedExceptionEventArgs> InternalCachedException
        {
            add
            {
                if(value == null) { return; }
                lock(s_internalExListenersLock)
                {
                    s_internalExListeners.Add(value);
                }
            }
            remove
            {
                if(value == null) { return; }
                lock(s_internalExListenersLock)
                {
                    s_internalExListeners.Remove(value);
                }
            }
        }

        /// <summary>
        /// Initializes the <see cref="GraphicsCore"/> class.
        /// </summary>
        static GraphicsCore()
        {
            s_internalExListeners = new List<EventHandler<InternalCatchedExceptionEventArgs>>();
            s_internalExListenersLock = new object();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphicsCore"/> class.
        /// </summary>
        protected GraphicsCore(bool debugEnabled, bool force2DFallback)
        {
            try
            {
                // Upate RK.Common members
                m_debugEnabled = debugEnabled;
                m_force2DFallback = force2DFallback;
                m_devices = new List<EngineDevice>();
                m_performanceCalculator = new PerformanceAnalyzer(TimeSpan.FromSeconds(1.0), TimeSpan.FromSeconds(2.0));
                m_performanceCalculator.SyncContext = SynchronizationContext.Current; // <-- TODO
                m_performanceCalculator.RunAsync(CancellationToken.None)
                    .FireAndForget();

                m_configuration = new GraphicsCoreConfiguration();
                m_configuration.DebugEnabled = debugEnabled;

                // Create container object for all input handlers
                m_inputHandlerFactory = new InputHandlerFactory();
                m_importExporters = new ImporterExporterRepository();

                // Create the key generator for resource keys
                m_resourceKeyGenerator = new UniqueGenericKeyGenerator();

                // Try to initialize global api factories (mostly for 2D rendering / operations)
                try
                {
                    if(s_throwDeviceInitError)
                    {
                        throw new SeeingSharpException("Simulated device initialization exception");
                    }

                    m_factoryHandlerWIC = new FactoryHandlerWIC();
                    m_factoryHandlerD2D = new FactoryHandlerD2D(this);
                    m_factoryHandlerDWrite = new FactoryHandlerDWrite(this);
                    m_factoryHandlerXAudio2 = new FactoryHandlerXAudio2();
                }
                catch (Exception ex)
                {
                    m_initException = ex;

                    m_devices.Clear();
                    m_factoryHandlerWIC = null;
                    m_factoryHandlerD2D = null;
                    m_factoryHandlerDWrite = null;
                    m_factoryHandlerXAudio2 = null;
                    return;
                }
                this.FactoryD2D = m_factoryHandlerD2D.Factory;
                this.FactoryD2D_2 = m_factoryHandlerD2D.Factory2;
                this.FactoryDWrite = m_factoryHandlerDWrite.Factory;
                this.FactoryWIC = m_factoryHandlerWIC.Factory;
                this.XAudioDevice = m_factoryHandlerXAudio2.Device;

                // Create the SoundManager
                m_soundManager = new SoundManager(m_factoryHandlerXAudio2);

                // Try to initialize Media Foundation interface
                // (This is a separated init step because MF may not be available on some systems, e. g. Servers)
                try
                {
                    m_factoryHandlerMF = new FactoryHandlerMF();
                }
                catch(Exception)
                {
                    m_factoryHandlerMF = null;
                }

                // Create the object containing all hardware information
                m_hardwareInfo = new EngineHardwareInfo();
                int actIndex = 0;
                foreach(var actAdapterInfo in m_hardwareInfo.Adapters)
                {
                    EngineDevice actEngineDevice = new EngineDevice(
                        this,
                        m_configuration,
                        actAdapterInfo.Adapter,
                        actAdapterInfo.IsSoftwareAdapter,
                        debugEnabled);
                    if(actEngineDevice.IsLoadedSuccessfully)
                    {
                        actEngineDevice.DeviceIndex = actIndex;
                        actIndex++;

                        m_devices.Add(actEngineDevice);
                    }
                }
                m_defaultDevice = m_devices.FirstOrDefault();

                // Start input gathering
                m_inputGatherer = new InputGathererThread();
                m_inputGatherer.Start();

                // Start main loop
                m_mainLoop = new EngineMainLoop(this);
                if (m_devices.Count > 0)
                {
                    m_mainLoopCancelTokenSource = new CancellationTokenSource();
                    m_mainLoopTask = m_mainLoop.Start(m_mainLoopCancelTokenSource.Token);
                }
            }
            catch (Exception ex2)
            {
                m_initException = ex2;

                m_hardwareInfo = null;
                m_devices.Clear();
                m_configuration = null;
                m_resourceKeyGenerator = null;
            }
        }

        internal static void PublishInternalExceptionInfo(
            Exception ex,
            InternalExceptionLocation location)
        {
            List<EventHandler<InternalCatchedExceptionEventArgs>> handlers = null;
            lock(s_internalExListenersLock)
            {
                if(s_internalExListeners.Count > 0)
                {
                    handlers = new List<EventHandler<InternalCatchedExceptionEventArgs>>(
                        s_internalExListeners);
                }
            } 

            foreach(var actEventHandler in handlers)
            {
                try
                {
                    actEventHandler(null, new InternalCatchedExceptionEventArgs(
                        ex, location));
                }
                catch { }
            }
        }

        /// <summary>
        /// This method is implemented for automated tests only!
        /// Is sets <see cref="GraphicsCore.Current"/> to null to enable a separate instance inside a using block. 
        /// </summary>
        public static IDisposable AutomatedTest_NewTestEnviornment()
        {
            GraphicsCore lastCurrent = s_current;
            if(lastCurrent?.MainLoop?.RegisteredRenderLoopCount > 0) { throw new SeeingSharpException("Current environment still active!"); }

            s_current = null;

            return new DummyDisposable(() => s_current = lastCurrent);
        }

        /// <summary>
        /// This method is implemented for automated tests only!
        /// It simulates a device initialization exception all next times GraphicsCore.Initialize is called.
        /// </summary>
        public static IDisposable AutomatedTest_ForceDeviceInitError()
        {
            if (s_current != null) { throw new SeeingSharpException("This call is only valid before Initialize was called!"); }

            s_throwDeviceInitError = true;

            return new DummyDisposable(() => s_throwDeviceInitError = false);
        }

        /// <summary>
        /// This method is implemented for automated tests only!
        /// It simulates a device initialization exception all next times GraphicsCore.Initialize is called.
        /// </summary>
        public static IDisposable AutomatedTest_ForceD2DInitError()
        {
            if (s_current != null) { throw new SeeingSharpException("This call is only valid before Initialize was called!"); }

            s_throwD2DInitDeviceError = true;

            return new DummyDisposable(() => s_throwD2DInitDeviceError = false);
        }

        /// <summary>
        /// Gets the device with the given luid.
        /// Null is returned if no device found.
        /// </summary>
        public EngineDevice TryGetDeviceByLuid(long luid)
        {
            return m_devices.FirstOrDefault(
                (actDevice) => actDevice.Luid == luid);
        }

        /// <summary>
        /// Gets a collection containing all available font familily names.
        /// </summary>
        public IEnumerable<string> GetFontFamilyNames(string localeName = "en-us")
        {
            localeName.EnsureNotNullOrEmpty(nameof(localeName));
            localeName = localeName.ToLower();

            // Query for all available FontFamilies installed on the system
            List<string> result = null;
            using (DWrite.FontCollection fontCollection = this.FactoryDWrite.GetSystemFontCollection(false))
            {
                int fontFamilyCount = fontCollection.FontFamilyCount;
                result = new List<string>(fontFamilyCount);

                for(int loop=0; loop< fontFamilyCount; loop++)
                {
                    using (DWrite.FontFamily actFamily = fontCollection.GetFontFamily(loop))
                    using (DWrite.LocalizedStrings actLocalizedStrings = actFamily.FamilyNames)
                    {
                        int localeIndex = -1;
                        if ((bool)actLocalizedStrings.FindLocaleName(localeName, out localeIndex))
                        {
                            string actName = actLocalizedStrings.GetString(0);
                            if(!string.IsNullOrWhiteSpace(actName))
                            {
                                result.Add(actName);
                            }
                        }
                        else if((bool)actLocalizedStrings.FindLocaleName("en-us", out localeIndex))
                        {
                            string actName = actLocalizedStrings.GetString(0);
                            if (!string.IsNullOrWhiteSpace(actName))
                            {
                                result.Add(actName);
                            }
                        }
                    }
                }
            }

            // Sort the list finally
            result.Sort();

            return result;
        }

#if DESKTOP
        /// <summary>
        /// Initializes performance measure for Wpf render system.
        /// </summary>
        internal void InitializeWpfRenderPerformanceMeasure()
        {
            if (m_wpfRenderWaitreCreated) { return; }

            m_wpfRenderWaitreCreated = true;
            System.Windows.Media.CompositionTarget.Rendering += (sender, eArgs) =>
            {
                CommonTools.DisposeObject(m_wpfRenderWaiter);
                m_wpfRenderWaiter = GraphicsCore.Current.PerformanceCalculator.BeginMeasureActivityDuration(
                    Constants.PERF_WPF_RENDER);
            };
        }
#endif

        /// <summary>
        /// Resumes rendering when in supendet state.
        /// </summary>
        public void Resume()
        {
            if (m_mainLoop == null) { throw new SeeingSharpGraphicsException("GraphicsCore not initialized!"); }

            m_mainLoop.Resume();
        }

        /// <summary>
        /// Suspends rendering completely.
        /// </summary>
        public Task SuspendAsync()
        {
            if(m_mainLoop == null) { throw new SeeingSharpGraphicsException("GraphicsCore not initialized!"); }

            return m_mainLoop.SuspendAsync();
        }

        public static void Touch()
        {
            if(s_current != null) { return; }

            if(!GraphicsCore.IsInitialized)
            {
                GraphicsCore.InitializeDefaultAsync()
                    .Wait();
            }
        }

        /// <summary>
        /// Performs default initialization logic.
        /// </summary>
        /// <returns></returns>
        public static async Task InitializeDefaultAsync()
        {
            if (s_current != null) { return; }

#if DESKTOP
            if(LicenseManager.UsageMode == LicenseUsageMode.Designtime) { return; }
#endif

            if (m_defaultInitTask != null)
            {
                await m_defaultInitTask;
                return;
            }
            else
            {
                // Initialize application object
                if (!SeeingSharpApplication.IsInitialized)
                {  
                    m_defaultInitTask = SeeingSharpApplication.InitializeAsync(
                        typeof(GraphicsCore).GetTypeInfo().Assembly,
                        new Assembly[]{
                            typeof(ResourceLink).GetTypeInfo().Assembly,
                        },
                        new string[0]);
                    await m_defaultInitTask.ConfigureAwait(false);
                }

                // Initialize graphics core object
                if (!GraphicsCore.IsInitialized)
                {
                    GraphicsCore.Initialize(false, false);
                }
            }
        }

        /// <summary>
        /// Initializes the graphics engine.
        /// </summary>
        public static void Initialize()
        {
            if (s_current != null) { return; }

            //Call initialization methods
            Initialize(false, false);
        }

        /// <summary>
        /// Initializes the GraphicsCore object.
        /// </summary>
        public static void Initialize(bool enableDebug)
        {
            if (s_current != null) { return; }

            s_current = new GraphicsCore(enableDebug, false);
        }

        /// <summary>
        /// Initializes the GraphicsCore object.
        /// </summary>
        public static void Initialize(bool enableDebug, bool force2DFallback)
        {
            if (s_current != null) { return; }

            s_current = new GraphicsCore(enableDebug, force2DFallback);
        }

        /// <summary>
        /// Checks if there is any hardware device loaded.
        /// </summary>
        public bool IsAnyHardwareDeviceLoaded()
        {
            return m_devices.Any((actDevice) => !actDevice.IsSoftware);
        }

        /// <summary>
        /// Sets the default device to a software device.
        /// </summary>
        public void SetDefaultDeviceToSoftware()
        {
            m_defaultDevice = m_devices.FirstOrDefault((actDevice) => actDevice.IsSoftware);
        }

        /// <summary>
        /// Sets the default device to a hardware device.
        /// </summary>
        public void SetDefaultDeviceToHardware()
        {
            m_defaultDevice = m_devices.FirstOrDefault((actDevice) => actDevice.IsSoftware);
        }

        /// <summary>
        /// Starts measuring the duration of the given activity.
        /// </summary>
        /// <param name="activityName">The name of the activity.</param>
        internal IDisposable BeginMeasureActivityDuration(string activityName)
        {
            return this.PerformanceCalculator.BeginMeasureActivityDuration(activityName);
        }

        /// <summary>
        /// Executes the given action and measures the time it takes.
        /// </summary>
        /// <param name="activityName">Name of the activity.</param>
        /// <param name="activityAction">The activity action.</param>
        internal void ExecuteAndMeasureActivityDuration(string activityName, Action activityAction)
        {
            this.PerformanceCalculator.ExecuteAndMeasureActivityDuration(activityName, activityAction);
        }

        /// <summary>
        /// Notifies the duration of the given activity.
        /// </summary>
        /// <param name="activityName"></param>
        /// <param name="durationTicks"></param>
        internal void NotifyActivityDuration(string activityName, long durationTicks)
        {
            this.PerformanceCalculator.NotifyActivityDuration(activityName, durationTicks);
        }

        /// <summary>
        /// Gets the next generic resource key.
        /// </summary>
        public static NamedOrGenericKey GetNextGenericResourceKey()
        {
            if (s_current == null) { return new NamedOrGenericKey(); }
            return s_current.ResourceKeyGenerator.GetNextGeneric();
        }

        /// <summary>
        /// Gets current singleton instance.
        /// </summary>
        public static GraphicsCore Current
        {
            get
            {
#if DESKTOP
                // Do nothing if we are within the Wpf designer
                if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
                {
                    return null;
                }
#endif

                if (s_current == null)
                {
                    throw new SeeingSharpException($"Unable to access {nameof(GraphicsCore)}.{nameof(GraphicsCore.Current)} before Initialize was called or a rendering control created!");
                    //GraphicsCore.InitializeDefaultAsync()
                    //    .Wait();
                }

                return s_current;
            }
        }

        /// <summary>
        /// Gets the dxgi factory object.
        /// </summary>
        public EngineHardwareInfo HardwareInfo
        {
            get { return m_hardwareInfo; }
        }

        /// <summary>
        /// Gets the first output monitor.
        /// </summary>
        public EngineOutputInfo DefaultOutput
        {
            get
            {
                return m_hardwareInfo.Adapters
                    .FirstOrDefault()?
                    .Outputs.FirstOrDefault();
            }
        }

        /// <summary>
        /// Is GraphicsCore initialized?
        /// </summary>
        public static bool IsInitialized
        {
            get
            {
                return 
                    (s_current != null) && 
                    (s_current.m_devices.Count > 0) &&
                    (s_current.m_initException == null);
            }
        }

        public Exception InitException
        {
            get
            {
                return s_current?.m_initException;
            }
        }

        internal static bool ThrowD2DInitDeviceError
        {
            get { return s_throwD2DInitDeviceError; }
        }

        /// <summary>
        /// Is debug enabled?
        /// </summary>
        public bool IsDebugEnabled
        {
            get { return m_debugEnabled; }
        }

        public bool IsMediaFoundationEnabled
        {
            get { return m_factoryHandlerMF != null; }
        }

        /// <summary>
        /// Gets the current resource key generator.
        /// </summary>
        public UniqueGenericKeyGenerator ResourceKeyGenerator
        {
            get { return m_resourceKeyGenerator; }
        }

        /// <summary>
        /// Gets current graphics configuration.
        /// </summary>
        public GraphicsCoreConfiguration Configuration
        {
            get { return m_configuration; }
        }

        /// <summary>
        /// Gets the default device.
        /// </summary>
        public EngineDevice DefaultDevice
        {
            get{ return m_defaultDevice; }
            set
            {
                if (value == null) { throw new ArgumentNullException("DefaultDevice"); }
                if (!m_devices.Contains(value)) { throw new ArgumentException("Device is not available on this GraphicsCore!"); }

                m_defaultDevice = value;
            }
        }

        /// <summary>
        /// Gets a collection containing all loaded devices.
        /// </summary>
        public IEnumerable<EngineDevice> Devices
        {
            get { return m_devices; }
        }

        /// <summary>
        /// Gets the total count of loaded devices.
        /// </summary>
        public int DeviceCount
        {
            get { return m_devices.Count; }
        }

        /// <summary>
        /// Gets the total count of registered RenderLoop objects.
        /// </summary>
        public int RegisteredRenderLoopCount
        {
            get
            {
                if(m_mainLoop == null) { return 0; }
                return m_mainLoop.RegisteredRenderLoopCount;
            }
        }

        /// <summary>
        /// Gets the default software device.
        /// </summary>
        public EngineDevice DefaultSoftwareDevice
        {
            get
            {
                if (m_devices == null) { return null; }
                return m_devices.FirstOrDefault((actDevice) => actDevice.IsSoftware);
            }
        }

        /// <summary>
        /// Gets a collection containing all devices.
        /// </summary>
        public IEnumerable<EngineDevice> LoadedDevices
        {
            get { return m_devices; }
        }

        /// <summary>
        /// Gets a list containing all input handlers.
        /// </summary>
        public InputHandlerFactory InputHandlers
        {
            get { return m_inputHandlerFactory; }
        }

        /// <summary>
        /// Gets an object which manages all importers and exporters.
        /// </summary>
        public ImporterExporterRepository ImportersAndExporters
        {
            get { return m_importExporters; }
        }

        /// <summary>
        /// Gets the current main loop object of the graphics engine.
        /// </summary>
        public EngineMainLoop MainLoop
        {
            get { return m_mainLoop; }
        }

        public InputGathererThread InputGatherer
        {
            get { return m_inputGatherer; }
        }

        /// <summary>
        /// Gets the currently running SoundManager object.
        /// </summary>
        public SoundManager SoundManager
        {
            get { return m_soundManager; }
        }

        /// <summary>
        /// Gets the current performance calculator.
        /// </summary>
        public PerformanceAnalyzer PerformanceCalculator
        {
            get { return m_performanceCalculator; }
        }

        /// <summary>
        /// Gets the WIC factory object.
        /// </summary>
        internal WIC.ImagingFactory FactoryWIC;

        /// <summary>
        /// Gets the Direct2D factory object.
        /// </summary>
        internal D2D.Factory FactoryD2D;

        /// <summary>
        /// Gets the Direct2D factory object.
        /// </summary>
        internal D2D.Factory2 FactoryD2D_2;

        /// <summary>
        /// Gets the DirectWrite factory object.
        /// </summary>
        internal DWrite.Factory FactoryDWrite;

        /// <summary>
        /// The global XAudio2 device
        /// </summary>
        internal XA.XAudio2 XAudioDevice;

        internal bool Force2DFallbackMethod
        {
            get { return m_force2DFallback; }
        }
    }
}