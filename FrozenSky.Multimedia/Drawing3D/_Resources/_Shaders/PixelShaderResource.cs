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

using FrozenSky.Multimedia.Core;
using FrozenSky.Util;

//Some namespace mappings
using D3D11 = SharpDX.Direct3D11;

namespace FrozenSky.Multimedia.Drawing3D
{
    public class PixelShaderResource : ShaderResource
    {
        //Resources for Direct3D 11 rendering
        private D3D11.PixelShader m_pixelShader;

        /// <summary>
        /// Initializes a new instance of the <see cref="VertexShaderResource" /> class.
        /// </summary>
        /// <param name="shaderProfile">Shader profile used for compiling.</param>
        /// <param name="resourceSource">The resource source.</param>
        public PixelShaderResource(string shaderProfile, ResourceSource resourceSource)
            : base(shaderProfile, resourceSource)
        {

        }

        /// <summary>
        /// Loads the resource.
        /// </summary>
        protected internal override void LoadShader(EngineDevice device, byte[] shaderBytecode)
        {
            if (m_pixelShader == null)
            {
                m_pixelShader = new D3D11.PixelShader(device.DeviceD3D11, shaderBytecode);
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
