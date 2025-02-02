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
using SeeingSharp.Util;

//Some namespace mappings
using D3D11 = SharpDX.Direct3D11;

namespace SeeingSharp.Multimedia.Drawing3D
{
    public class PixelShaderResource : ShaderResource
    {
        #region Resources for Direct3D 11 rendering
        private D3D11.PixelShader m_pixelShader;
        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="VertexShaderResource" /> class.
        /// </summary>
        /// <param name="shaderProfile">Shader profile used for compiling.</param>
        /// <param name="resourceLink">The resourceLink.</param>
        public PixelShaderResource(string shaderProfile, ResourceLink resourceLink)
            : base(shaderProfile, resourceLink)
        {

        }

        /// <summary>
        /// Loads the resource.
        /// </summary>
        protected internal override void LoadShader(EngineDevice device, byte[] shaderBytecode)
        {
            if (m_pixelShader == null)
            {
                m_pixelShader = new D3D11.PixelShader(device.DeviceD3D11_1, shaderBytecode);
            }
        }

        /// <summary>
        /// Unloads the resource.
        /// </summary>
        protected internal override void UnloadShader()
        {
            m_pixelShader = GraphicsHelper.DisposeObject(m_pixelShader);
        }

        /// <summary>
        /// Is the resource loaded?
        /// </summary>
        public override bool IsLoaded
        {
            get { return m_pixelShader != null; }
        }

        /// <summary>
        /// Gets the loaded VertexShader object.
        /// </summary>
        internal D3D11.PixelShader PixelShader
        {
            get { return m_pixelShader; }
        }
    }
}
