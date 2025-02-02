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
using SeeingSharp.Checking;
using System;
using System.Collections.Generic;
using System.IO;

//Some namespace mappings
using SDXTK = SeeingSharp.Multimedia.Util.SdxTK;
using D3D11 = SharpDX.Direct3D11;
using DXGI = SharpDX.DXGI;
using WIC = SharpDX.WIC;

namespace SeeingSharp.Multimedia.Drawing3D
{
    public class StandardTextureResource : TextureResource
    {
        #region configuration
        private ResourceLink m_resourceLinkHighQuality;
        private ResourceLink m_resourceLinkLowQuality;
        private MemoryMappedTexture32bpp m_inMemoryTexture;
        #endregion

        #region Loaded resources
        private D3D11.Texture2D m_texture;
        private D3D11.ShaderResourceView m_textureView;
        #endregion

        #region Runtime
        private bool m_isCubeTexture;
        private bool m_isRenderTarget;
        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="StandardTextureResource" /> class.
        /// </summary>
        public StandardTextureResource(ResourceLink textureSource)
        {
            m_resourceLinkHighQuality = textureSource;
            m_resourceLinkLowQuality = textureSource;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StandardTextureResource" /> class.
        /// </summary>
        internal StandardTextureResource(MemoryMappedTexture32bpp inMemoryTexture)
        {
            inMemoryTexture.EnsureNotNull(nameof(inMemoryTexture));

            m_inMemoryTexture = inMemoryTexture;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StandardTextureResource" /> class.
        /// </summary>
        /// <param name="highQualityTextureSource">High quality version of the texture.</param>
        /// <param name="lowQualityTextureSource">Low quality version of the texture.</param>
        public StandardTextureResource(ResourceLink highQualityTextureSource, ResourceLink lowQualityTextureSource)
        {
            m_resourceLinkHighQuality = highQualityTextureSource;
            m_resourceLinkLowQuality = lowQualityTextureSource;
        }

        /// <summary>
        /// Loads the resource.
        /// </summary>
        protected override void LoadResourceInternal(EngineDevice device, ResourceDictionary resources)
        {
            // Select source texture
            ResourceLink source = m_resourceLinkLowQuality;
            if (device.Configuration.TextureQuality == TextureQuality.Hight) { source = m_resourceLinkHighQuality; }

            // Load the texture
            try
            {
                if (source != null)
                {
                    using (Stream inStream = source.OpenInputStream())
                    using (SDXTK.Image rawImage = SDXTK.Image.Load(inStream))
                    {
                        m_texture = GraphicsHelper.CreateTexture(device, rawImage); 
                    }
                }
                else if (m_inMemoryTexture != null)
                {
                    m_texture = GraphicsHelper.LoadTexture2DFromMappedTexture(device, m_inMemoryTexture);
                }
            }
            catch(Exception)
            {
#if DESKTOP
                // Load default texture from a bitmap
                m_texture = GraphicsHelper.LoadTextureFromBitmap(device, Properties.Resources.Blank_16x16);
#else
                throw;
#endif
            }

            // Create view for shaders
            m_textureView = new D3D11.ShaderResourceView(device.DeviceD3D11_1, m_texture);

            // Some checking..
            m_isCubeTexture =
                (m_texture.Description.ArraySize == 6) &&
                ((m_texture.Description.OptionFlags & D3D11.ResourceOptionFlags.TextureCube) == D3D11.ResourceOptionFlags.TextureCube);
            m_isRenderTarget =
                (m_texture.Description.BindFlags & D3D11.BindFlags.RenderTarget) == D3D11.BindFlags.RenderTarget;
        }

        /// <summary>
        /// Unloads the resource.
        /// </summary>
        protected override void UnloadResourceInternal(EngineDevice device, ResourceDictionary resources)
        {
            m_textureView = GraphicsHelper.DisposeObject(m_textureView);
            m_texture = GraphicsHelper.DisposeObject(m_texture);

            m_isCubeTexture = false;
            m_isRenderTarget = false;
        }

        /// <summary>
        /// Gets the texture object.
        /// </summary>
        /// <exception cref="System.NotImplementedException"></exception>
        public override D3D11.Texture2D Texture
        {
            get { return m_texture; }
        }

        /// <summary>
        /// Gets a ShaderResourceView targeting the texture.
        /// </summary>
        public override D3D11.ShaderResourceView TextureView
        {
            get { return m_textureView; }
        }



        /// <summary>
        /// Is the object loaded correctly?
        /// </summary>
        public override bool IsLoaded
        {
            get { return m_textureView != null; }
        }

        /// <summary>
        /// Is this texture a cube texture?
        /// </summary>
        public bool IsCubeTexture
        {
            get { return m_isCubeTexture; }
        }

        /// <summary>
        /// Is this texture a render target texture?
        /// </summary>
        public bool IsRenderTargetTexture
        {
            get { return m_isRenderTarget; }
        }

        /// <summary>
        /// Gets the size of the texture array.
        /// 1 for normal textures.
        /// 6 for cubemap textures.
        /// </summary>
        public override int ArraySize
        {
            get { return m_texture.Description.ArraySize; }
        }
    }
}
