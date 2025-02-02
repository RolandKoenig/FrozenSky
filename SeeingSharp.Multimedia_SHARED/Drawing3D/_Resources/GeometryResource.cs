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
using SeeingSharp.Multimedia.Objects;
using SeeingSharp.Util;
using System;
using System.Numerics;
using System.Linq;
using System.Collections.Generic;

// Some namespace mappings
using D3D = SharpDX.Direct3D;
using D3D11 = SharpDX.Direct3D11;
using DXGI = SharpDX.DXGI;

namespace SeeingSharp.Multimedia.Drawing3D
{
    public class GeometryResource : Resource
    {
        private const int MAX_VERTEX_COUNT_PER_BUFFER = 1000000000;
        private const int MAX_INDEX_COUNT_PER_BUFFER = 1000000000;

        #region  Resources for Direct3D 11 rendering
        private LoadedStructureInfo[] m_loadedStructures;
        #endregion

        #region Generic resources
        private BoundingBox m_boundingBox;
        private ObjectType m_objectType;
        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="GeometryResource"/> class.
        /// </summary>
        public GeometryResource(ObjectType objectType)
        {
            m_objectType = objectType;

            m_loadedStructures = new LoadedStructureInfo[0];
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GeometryResource"/> class.
        /// </summary>
        /// <param name="vertexStructure">The vertex structures.</param>
        public GeometryResource(VertexStructure vertexStructure)
            : this(new GenericObjectType(vertexStructure))
        {

        }

        /// <summary>
        /// Stores all required data into a new <see cref="ExportGeometryInfo"/>. 
        /// </summary>
        public ExportGeometryInfo PrepareForExport()
        {
            return new ExportGeometryInfo(
                this.Key,
                this.ObjectType);
        }

        /// <summary>
        /// Gets a collection containing all referenced materials.
        /// </summary>
        public IEnumerable<MaterialResource> GetReferencedMaterials()
        {
            var loadedStructures = m_loadedStructures;
            for(int loop=0; loop<loadedStructures.Length; loop++)
            {
                var actLoadedStructure = loadedStructures[loop];
                if(actLoadedStructure == null) { continue; }

                if(actLoadedStructure.Material != null) { yield return actLoadedStructure.Material; }
            }
        }

        /// <summary>
        /// Performs an intersection test using given picking ray and picking options.
        /// </summary>
        /// <param name="pickingRay">The given picking ray.</param>
        /// <param name="pickingOptions">The picking options.</param>
        /// <param name="distance">The distance from origin to the picking point.</param>
        public bool Intersects(Ray pickingRay, PickingOptions pickingOptions, out float distance)
        {
            distance = float.MaxValue;
            bool result = false;

            for (int loop = 0; loop < m_loadedStructures.Length; loop++)
            {
                VertexStructure actLoadedStructure = m_loadedStructures[loop].VertexStructure;

                float currentDistance = float.NaN;
                if (actLoadedStructure.Intersects(pickingRay, pickingOptions, out currentDistance))
                {
                    result = true;
                    if (currentDistance < distance) { distance = currentDistance; }
                }
            }

            return result;
        }

        /// <summary>
        /// Redefines the content of this geometry resource.
        /// </summary>
        public void Redefine(ResourceDictionary resources, ObjectType objectType)
        {
            //Unload resource first if it was loaded
            bool wasLoaded = this.IsLoaded;
            if (wasLoaded)
            {
                this.UnloadResource();
            }

            //Update members
            m_objectType = objectType;
            m_boundingBox = new BoundingBox();

            //Reload resources again if they where loaded before
            if (wasLoaded)
            {
                this.LoadResource();
            }
        }

        /// <summary>
        /// Redefines the content of this geometry resource.
        /// </summary>
        public void Redefine(ResourceDictionary resources, VertexStructure vertexStructures)
        {
            this.Redefine(resources, new GenericObjectType(vertexStructures));
        }

        /// <summary>
        /// Performs a simple picking-test.
        /// </summary>
        /// <param name="pickInformation">The that gathers picking information.</param>
        /// <param name="pickingRay">The picking ray.</param>
        public void Pick(PickingInformation pickInformation, Ray pickingRay)
        {
            if (pickingRay.Intersects(m_boundingBox))
            {

            }
        }

        /// <summary>
        /// Renders this GeometryResource.
        /// </summary>
        /// <param name="renderState">Current render state.</param>
        public void Render(RenderState renderState)
        {
            D3D11.DeviceContext deviceContext = renderState.Device.DeviceImmediateContextD3D11;
            DXGI.Format indexBufferFormat = renderState.Device.SupportsOnly16BitIndexBuffer ? DXGI.Format.R16_UInt : DXGI.Format.R32_UInt;

            int lastVertexBufferID = -1;
            int lastIndexBufferID = -1;
            for (int loop = 0; loop < m_loadedStructures.Length; loop++)
            {
                LoadedStructureInfo structureToDraw = m_loadedStructures[loop];

                // Apply VertexBuffer 
                if (lastVertexBufferID != structureToDraw.VertexBufferID)
                {
                    lastVertexBufferID = structureToDraw.VertexBufferID;
                    deviceContext.InputAssembler.InputLayout = structureToDraw.InputLayout;
                    deviceContext.InputAssembler.SetVertexBuffers(0, new D3D11.VertexBufferBinding(structureToDraw.VertexBuffer, structureToDraw.SizePerVertex, 0));
                }

                // Apply IndexBuffer
                if (lastIndexBufferID != structureToDraw.IndexBufferID)
                {
                    deviceContext.InputAssembler.SetIndexBuffer(structureToDraw.IndexBuffer, indexBufferFormat, 0);
                }

                // Apply material
                renderState.ApplyMaterial(structureToDraw.Material);
                D3D11.InputLayout newInputLayout = null;
                if(renderState.ForcedMaterial != null)
                {
                    newInputLayout = renderState.ForcedMaterial.GenerateInputLayout(
                        renderState.Device,
                        StandardVertex.InputElements,
                        MaterialApplyInstancingMode.SingleObject);
                    deviceContext.InputAssembler.InputLayout = newInputLayout;
                }
                try
                {
                    // Draw current rener block
                    deviceContext.DrawIndexed(
                        structureToDraw.IndexCount,
                        structureToDraw.StartIndex,
                        0);
                }
                finally
                {
                    if (newInputLayout != null)
                    {
                        deviceContext.InputAssembler.InputLayout = null;
                        GraphicsHelper.SafeDispose(ref newInputLayout);
                    }
                }
            }
        }

        /// <summary>
        /// Loads the resource.
        /// </summary>
        protected override void LoadResourceInternal(EngineDevice device, ResourceDictionary resources)
        {
            // Build structures
            VertexStructure[] structures = new VertexStructure[]
            {
                m_objectType.BuildStructure(new StructureBuildOptions(device.SupportedDetailLevel))
            };

            // Build BoundingBox around all vertices
            List<Vector3> vertexLocations = new List<Vector3>();
            for (int loop = 0; loop < structures.Length; loop++)
            {
                foreach (Vertex actVertex in structures[loop].Vertices)
                {
                    vertexLocations.Add(actVertex.Position);
                }
            }
            m_boundingBox = new BoundingBox(vertexLocations);

            // Build geometry
            m_loadedStructures = BuildBuffers(device, structures, resources);
        }

        /// <summary>
        /// Unloads the resource.
        /// </summary>
        protected override void UnloadResourceInternal(EngineDevice device, ResourceDictionary resources)
        {
            for (int loop = 0; loop < m_loadedStructures.Length; loop++)
            {
                m_loadedStructures[loop].InputLayout = GraphicsHelper.DisposeObject(m_loadedStructures[loop].InputLayout);
                m_loadedStructures[loop].VertexBuffer = GraphicsHelper.DisposeObject(m_loadedStructures[loop].VertexBuffer);
                m_loadedStructures[loop].IndexBuffer = GraphicsHelper.DisposeObject(m_loadedStructures[loop].IndexBuffer);
            }
            m_loadedStructures = new LoadedStructureInfo[0];

            device = null;
        }

        /// <summary>
        /// Builds vertex and index buffers.
        /// </summary>
        /// <param name="device">The device on which to build all buffers.</param>
        /// <param name="structures">All structure to be loaded.</param>
        /// <param name="resources">The current resource dictionary</param>
        protected virtual LoadedStructureInfo[] BuildBuffers(EngineDevice device, VertexStructure[] structures, ResourceDictionary resources)
        {
            List<LoadedStructureInfo> result = new List<LoadedStructureInfo>(structures.Length * 2);
            List<StandardVertex[]> cachedVertices = new List<StandardVertex[]>(2);
            List<int[]> cachedIndices = new List<int[]>(6);

            int structuresCount = structures.Length;
            int actVertexCount = 0;
            int actIndexCount = 0;
            int lastFinishedVertexBufferResultIndex = -1;
            int lastFinishedIndexBufferResultIndex = -1;
            int vertexBufferID = 0;
            int indexBufferID = 0;

            // Define the action which finishes current index buffer
            Action actionFinishIndexBuffer = () =>
            {
                // Create the vertex buffer
                D3D11.Buffer indexBuffer = GraphicsHelper.CreateImmutableIndexBuffer(
                    device,
                    cachedIndices.ToArray());
                cachedIndices.Clear();
                actIndexCount = 0;
                indexBufferID++;

                // Do also create index buffer
                for (int loop = lastFinishedIndexBufferResultIndex + 1; loop < result.Count; loop++)
                {
                    result[loop].IndexBuffer = indexBuffer;
                }
                lastFinishedIndexBufferResultIndex = result.Count - 1;
            };

            // Define the action which finishes current vertex buffer
            Action actionFinishVertexBuffer = () =>
            {
                // Create the vertex buffer
                D3D11.Buffer vertexBuffer = GraphicsHelper.CreateImmutableVertexBuffer(
                    device,
                    cachedVertices.ToArray());
                cachedVertices.Clear();
                actVertexCount = 0;
                vertexBufferID++;

                // Do also finish index buffers in this case
                actionFinishIndexBuffer();

                // Do also create index buffer
                for (int loop = lastFinishedVertexBufferResultIndex + 1; loop < result.Count; loop++)
                {
                    result[loop].VertexBuffer = vertexBuffer;
                }
                lastFinishedVertexBufferResultIndex = result.Count - 1;
            };

            // Load all structures into memory within a loop
            for(int loopStruct = 0; loopStruct<structuresCount; loopStruct++)
            {
                VertexStructure actStructure = structures[loopStruct];
                if(actStructure.CountVertices == 0) { continue; }
                if(actStructure.CountSurfaces == 0) { continue; }

                // Handle vertex data
                StandardVertex[] vertexArray = StandardVertex.FromVertexStructure(actStructure);
                if(actVertexCount + vertexArray.Length > MAX_VERTEX_COUNT_PER_BUFFER)
                {
                    actionFinishVertexBuffer();
                }
                cachedVertices.Add(vertexArray);
                int actBaseVertex = actVertexCount;
                actVertexCount += vertexArray.Length;

                // Sort all surfaces by material
                List<VertexStructureSurface> surfaceList = new List<VertexStructureSurface>(actStructure.Surfaces);
                surfaceList.Sort((left, right) => left.MaterialProperties.GetHashCode().CompareTo(right.MaterialProperties.GetHashCode()));

                int surfaceCount = surfaceList.Count;
                for(int loopSurface =0; loopSurface<surfaceCount; loopSurface++)
                {
                    VertexStructureSurface actSurface = surfaceList[loopSurface];
                    if(actSurface.CountTriangles == 0) { continue; }

                    // Handle index data
                    int[] indexArray = actSurface.GetIndexArray();
                    if(actBaseVertex > 0)
                    {
                        for(int loopIndex=0; loopIndex<indexArray.Length; loopIndex++)
                        {
                            indexArray[loopIndex] = indexArray[loopIndex] + actBaseVertex;
                        }
                    }
                    if(actIndexCount + indexArray.Length > MAX_INDEX_COUNT_PER_BUFFER)
                    {
                        actionFinishIndexBuffer();
                    }
                    cachedIndices.Add(indexArray);
                    actIndexCount += indexArray.Length;

                    // Get or create the material
                    LoadedStructureInfo lastStructureInfo = result.Count > 0 ? result[result.Count - 1] : null;
                    if ((lastStructureInfo != null) &&
                        (lastStructureInfo.IndexBuffer == null) &&
                        (actSurface.MaterialProperties.Equals(lastStructureInfo.MaterialProperties)))
                    {
                        LoadedStructureInfo actStructureInfo = result[result.Count - 1];
                        actStructureInfo.IndexCount = actStructureInfo.IndexCount + indexArray.Length;
                    }
                    else
                    {
                        MaterialResource actMaterialResource = resources.GetOrCreateMaterialResourceAndEnsureLoaded(actSurface);

                        // Create some information about the loaded structures
                        LoadedStructureInfo newStructureInfo = new LoadedStructureInfo();
                        newStructureInfo.VertexBufferID = vertexBufferID;
                        newStructureInfo.IndexBufferID = indexBufferID;
                        newStructureInfo.SizePerVertex = StandardVertex.Size;
                        newStructureInfo.VertexStructure = actStructure;
                        newStructureInfo.IndexCount = indexArray.Length;
                        newStructureInfo.StartIndex = actIndexCount - indexArray.Length;
                        newStructureInfo.Material = actMaterialResource;
                        newStructureInfo.MaterialProperties = actSurface.MaterialProperties;
                        newStructureInfo.VertexBuffer = null;
                        newStructureInfo.IndexBuffer = null;
                        newStructureInfo.InputLayout = newStructureInfo.Material.GenerateInputLayout(
                            device, StandardVertex.InputElements, MaterialApplyInstancingMode.SingleObject);
                        result.Add(newStructureInfo);
                    }
                }
            }

            // Finish all remaining buffers finally
            if(cachedVertices.Count > 0) { actionFinishVertexBuffer(); }
            if(cachedIndices.Count > 0) { actionFinishIndexBuffer(); }

            return result.ToArray();
        }

        /// <summary>
        /// Is the resource loaded?
        /// </summary>
        public override bool IsLoaded
        {
            get { return m_loadedStructures.Length > 0; }
        }

        /// <summary>
        /// Gets the source of geometry data.
        /// </summary>
        public ObjectType ObjectType
        {
            get { return m_objectType; }
        }

        /// <summary>
        /// Gets the bounding box sourounding this object.
        /// </summary>
        public BoundingBox BoundingBox
        {
            get { return m_boundingBox; }
        }

        //*********************************************************************
        //*********************************************************************
        //*********************************************************************
        protected class LoadedStructureInfo
        {
            public int VertexBufferID;
            public int IndexBufferID;
            public VertexStructure VertexStructure;
            public D3D11.Buffer VertexBuffer;
            public D3D11.Buffer IndexBuffer;
            public int SizePerVertex;
            public int IndexCount;
            public int StartIndex;
            public MaterialResource Material;
            public MaterialProperties MaterialProperties;
            public D3D11.InputLayout InputLayout;
        }
    }
}