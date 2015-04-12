﻿#region License information (FrozenSky and all based games/applications)
/*
    FrozenSky and all games/applications based on it (more info at http://www.rolandk.de/wp)
    Copyright (C) 2015 Roland König (RolandK)

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

#if DESKTOP
using FrozenSky.Multimedia.Core;
using System;
using System.Drawing;
using System.IO;

//Some namespace mappings
using D3D11 = SharpDX.Direct3D11;
using DXGI = SharpDX.DXGI;

namespace FrozenSky.Multimedia.Drawing3D
{
    public class BitmapTextureResource : TextureResource
    {
        //Member for Direct3D 11 rendering
        private D3D11.Texture2D m_texture;
        private D3D11.ShaderResourceView m_textureView;

        //Generic members
        private Bitmap m_bitmap;

        /// <summary>
        /// Initializes a new instance of the <see cref="BitmapTextureResource"/> class.
        /// </summary>
        /// <param name="name">The name of the resource.</param>
        /// <param name="bitmap">The bitmap source object.</param>
        public BitmapTextureResource(Bitmap bitmap)
        {
            m_bitmap = bitmap;
        }

        /// <summary>
        /// Loads the resource.
        /// </summary>
        /// <param name="resources">Parent ResourceDictionary.</param>
        protected override void LoadResourceInternal(EngineDevice device, ResourceDictionary resources)
        {
            if (m_texture == null)
            {
                //Get source bitmap
                Bitmap bitmap = m_bitmap;
                m_texture = GraphicsHelper.LoadTextureFromBitmap(device, bitmap, 0);

                //Create the view targeting the texture
                m_textureView = new D3D11.ShaderResourceView(device.DeviceD3D11, m_texture);
            }
        }

        /// <summary>
        /// Unloads the resource.
        /// </summary>
        /// <param name="resources">Parent ResourceDictionary.</param>
        protected override void UnloadResourceInternal(EngineDevice device, ResourceDictionary resources)
        {
            if (m_texture != null)
            {
                m_textureView = GraphicsHelper.DisposeObject(m_textureView);
                m_texture = GraphicsHelper.DisposeObject(m_texture);
            }
        }

        /// <summary>
        /// Sets the bitmap to be displayed.
        /// </summary>
        protected void SetBitmap(Bitmap bitmap)
        {
            if (bitmap == null) { throw new ArgumentNullException("bitmap"); }

            m_bitmap = bitmap;

            base.ReloadResource();
        }

        /// <summary>
        /// Is the resource loaded?
        /// </summary>
        public override bool IsLoaded
        {
            get { return m_texture != null; }
        }

        /// <summary>
        /// Gets the texture.
        /// </summary>
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
#endif