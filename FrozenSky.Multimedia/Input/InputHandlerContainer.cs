﻿#region License information (FrozenSky and all based games/applications)
/*
    FrozenSky and all games/applications based on it (more info at http://www.rolandk.de/wp)
    Copyright (C) 2014 Roland König (RolandK)

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see http://www.gnu.org/licenses/.
*/
#endregion

using FrozenSky.Infrastructure;
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FrozenSky.Multimedia.Core;

namespace FrozenSky.Multimedia.Input
{
    public class InputHandlerContainer
    {
        private List<IFrozenSkyFreeCameraInputHandler> m_inputHandlers;

        /// <summary>
        /// Initializes a new instance of the <see cref="InputHandlerContainer"/> class.
        /// </summary>
        internal InputHandlerContainer()
        {
            m_inputHandlers = FrozenSkyApplication.Current.TypeQuery
                .GetAndInstanciateByContract<IFrozenSkyFreeCameraInputHandler>();
        }

        /// <summary>
        /// Updates all currently active input handlers for the given view.
        /// </summary>
        /// <param name="viewObject">The object of the view control.</param>
        /// <param name="inputHandlers">The collection of input handlers managed by the view object.</param>
        /// <param name="renderLoop">The renderloop used by the view object.</param>
        /// <param name="currentlyDispsoing">Is the view currently disposing?</param>
        internal static void UpdateInputHandlerList(
            object viewObject,
            List<IFrozenSkyFreeCameraInputHandler> inputHandlers,
            RenderLoop renderLoop,
            bool currentlyDispsoing)
        {
            // Clear previous input handlers
            if(inputHandlers.Count > 0)
            {
                foreach (var actHandler in inputHandlers)
                {
                    actHandler.Stop();
                }
                inputHandlers.Clear();
            }

            // Check whether this object is disposed
            if (currentlyDispsoing) { return; }

            // Check for other dependencies
            if ((renderLoop == null) ||
                (renderLoop.Camera == null))
            {
                return;
            }

            // Get all possible input handlers
            inputHandlers.AddRange(GraphicsCore.Current.InputHandlers.GetInputHandler(
                viewObject.GetType(),
                renderLoop.Camera.GetType()));
            
            // Start them all
            foreach(var actInputHandler in inputHandlers)
            {
                actInputHandler.Start(viewObject, renderLoop.Camera);
            }
        }

        /// <summary>
        /// Gets all possible GraphicsInputHandlers for the given view and camera types.
        /// </summary>
        /// <typeparam name="ViewType">Gets the type of the view.</typeparam>
        /// <typeparam name="CameraType">Gets the type of the camera.</typeparam>
        public List<IFrozenSkyFreeCameraInputHandler> GetInputHandler<ViewType, CameraType>()
            where ViewType : class
            where CameraType : class
        {
            Type givenViewType = typeof(ViewType);
            Type givenCameraType = typeof(CameraType);

            return GetInputHandler(givenViewType, givenCameraType);
        }

        /// <summary>
        /// Gets all possible GraphicsInputHandlers for the given view and camera types.
        /// </summary>
        /// <param name="givenCameraType">The type of the view.</param>
        /// <param name="givenViewType">The type of the camera.</param>
        public List<IFrozenSkyFreeCameraInputHandler> GetInputHandler(Type givenViewType, Type givenCameraType)
        {
            List<IFrozenSkyFreeCameraInputHandler> result = new List<IFrozenSkyFreeCameraInputHandler>();
            foreach(var actInputHandler in m_inputHandlers)
            {
                // Query for the input handler's information
                Type[] actSupportedViewTypes = actInputHandler.GetSupportedViewTypes();
                Type[] actSupportedCameraTypes = actInputHandler.GetSupportedCameraTypes();
                bool viewTypeSupported = false;
                bool cameraTypeSupported = false;
               
                // Check for type support
                foreach(Type actViewType in actSupportedViewTypes)
                {
                    if(actViewType.GetTypeInfo().IsAssignableFrom(givenViewType.GetTypeInfo()))
                    {
                        viewTypeSupported = true;
                        break;
                    }
                }
                foreach(Type actCameraType in actSupportedCameraTypes)
                {
                    if(actCameraType.GetTypeInfo().IsAssignableFrom(givenCameraType.GetTypeInfo()))
                    {
                        cameraTypeSupported = true;
                        break;
                    }
                }

                // Cancel if given types are not supported
                if ((!cameraTypeSupported) || (!viewTypeSupported)) { continue; }

                // Create a new input handler 
                result.Add(Activator.CreateInstance(actInputHandler.GetType()) as IFrozenSkyFreeCameraInputHandler);
            }
            return result;
        }

        /// <summary>
        /// Gets the total count of loaded input handlers
        /// </summary>
        public int Count
        {
            get { return m_inputHandlers.Count; }
        }
    }
}
