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
using System.Numerics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SeeingSharp;
using SeeingSharp.Util;
using SeeingSharp.Multimedia.Core;
using SeeingSharp.Multimedia.Drawing3D;

//Some namespace mappings
using D3D11 = SharpDX.Direct3D11;

namespace SeeingSharp.Multimedia.Objects
{
    /// <summary>
    /// This class is responsible for rendering simple lines into the 3d scene.
    /// Use the LineData property to define all points of the line.
    /// </summary>
    public class WireObject : SceneObject
    {
        #region Configuration
        private bool m_forceReloadLineData;
        private Line[] m_lineData;
        private Color4 m_lineColor;
        #endregion

        #region Direct3D resources
        private IndexBasedDynamicCollection<LocalResourceData> m_localResources;
        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="WireObject" /> class.
        /// </summary>
        public WireObject()
        {
            m_lineColor = Color4.Black;
            m_localResources = new IndexBasedDynamicCollection<LocalResourceData>();
        }

        /// <summary>
        /// Loads all resources of the object.
        /// </summary>
        /// <param name="device">Current graphics device.</param>
        /// <param name="resourceDictionary">Current resource dicionary.</param>
        public override void LoadResources(EngineDevice device, ResourceDictionary resourceDictionary)
        {
            m_localResources.AddObject(
                new LocalResourceData()
                {
                    LineRenderResources = resourceDictionary.GetResourceAndEnsureLoaded<LineRenderResources>(
                        LineRenderResources.RESOURCE_KEY,
                        () => new LineRenderResources()),
                    LineVertexBuffer = null
                },
                device.DeviceIndex);
        }

        /// <summary>
        /// Are resources loaded for the given device?
        /// </summary>
        /// <param name="device">The device for which to check.</param>
        public override bool IsLoaded(EngineDevice device)
        {
            return m_localResources.HasObjectAt(device.DeviceIndex);
        }

        /// <summary>
        /// Unloads all resources of the object.
        /// </summary>
        public override void UnloadResources()
        {
            base.UnloadResources();

            m_localResources.Clear();
        }

        /// <summary>
        /// Updates the object.
        /// </summary>
        /// <param name="updateState">Current update state.</param>
        protected override void UpdateInternal(SceneRelatedUpdateState updateState)
        {
            // Handle line data reloading flag
            if(m_forceReloadLineData)
            {
                m_localResources.ForEachInEnumeration((actItem) => actItem.LineDataLoaded = false);
                m_forceReloadLineData = false;
            }
        }

        /// <summary>
        /// Updates this object for the given view.
        /// </summary>
        /// <param name="updateState">Current state of the update pass.</param>
        /// <param name="layerViewSubset">The layer view subset wich called this update method.</param>
        protected override void UpdateForViewInternal(SceneRelatedUpdateState updateState, ViewRelatedSceneLayerSubset layerViewSubset)
        {
            if (base.CountRenderPassSubscriptions(layerViewSubset) == 0)
            {
                base.SubscribeToPass(RenderPassInfo.PASS_LINE_RENDER, layerViewSubset, RenderLines);
            }
        }

        /// <summary>
        /// Main render method for the wire object.
        /// </summary>
        /// <param name="renderState">Current render state.</param>
        private void RenderLines(RenderState renderState)
        {
            LocalResourceData resourceData = m_localResources[renderState.DeviceIndex];

            // Load line data to memory if needed
            if (!resourceData.LineDataLoaded)
            {
                if ((m_lineData == null) ||
                    (m_lineData.Length == 0))
                {
                    return;
                }

                // Loading of line data
                GraphicsHelper.SafeDispose(ref resourceData.LineVertexBuffer);
                resourceData.LineVertexBuffer = GraphicsHelper.CreateImmutableVertexBuffer(renderState.Device, m_lineData);
                resourceData.LineDataLoaded = true;
            }

            // Calculate transform matrix
            Matrix4x4 viewProj = Matrix4x4.Transpose(renderState.ViewProj);

            // Render all lines finally
            resourceData.LineRenderResources.RenderLines(
                renderState, viewProj, m_lineColor, 
                resourceData.LineVertexBuffer, m_lineData.Length * 2);
        }

        /// <summary>
        /// Gets or sets current line data.
        /// </summary>
        public Line[] LineData
        {
            get { return m_lineData; }
            set
            {
                if (m_lineData != value)
                {
                    m_lineData = value;
                    m_forceReloadLineData = true;
                }
            }
        }

        /// <summary>
        /// Gets or sets the line's color.
        /// </summary>
        public Color4 LineColor
        {
            get { return m_lineColor; }
            set { m_lineColor = value; }
        }

        //*********************************************************************
        //*********************************************************************
        //*********************************************************************
        private class LocalResourceData
        {
            public LineRenderResources LineRenderResources;
            public D3D11.Buffer LineVertexBuffer;
            public bool LineDataLoaded;
        }
    }
}
