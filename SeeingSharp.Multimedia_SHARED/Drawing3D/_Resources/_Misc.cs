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
using SeeingSharp.Multimedia.Core;

namespace SeeingSharp.Multimedia.Drawing3D
{
    public enum GradientDirection
    {
        LeftToRight,

        TopToBottom,

        Directional,
    }

    //public delegate void LoadTextureEventHandler(object sender, LoadTextureEventArgs e);
    public delegate void TextureChangedHandler(object sender, TextureChangedEventArgs e);

    ///// <summary>
    ///// EventArgs class for LoadTextureEventHandler delegate.
    ///// </summary>
    //public class LoadTextureEventArgs : EventArgs
    //{
    //    private Bitmap m_bitmap;

    //    /// <summary>
    //    /// Initializes a new instance of the <see cref="LoadTextureEventArgs"/> class.
    //    /// </summary>
    //    internal LoadTextureEventArgs()
    //    {
    //        m_bitmap = null;
    //    }

    //    /// <summary>
    //    /// Gets or sets the source.
    //    /// </summary>
    //    /// <value>The source.</value>
    //    public Bitmap Source
    //    {
    //        get { return m_bitmap; }
    //        set { m_bitmap = value; }
    //    }
    //}

    /// <summary>
    /// EventArgs class for TextureChangedHandler delegate
    /// </summary>
    public class TextureChangedEventArgs : EventArgs
    {
        private RenderState m_renderState;

        /// <summary>
        /// Initializes a new instance of the <see cref="TextureChangedEventArgs"/> class.
        /// </summary>
        /// <param name="renderState">Current render state.</param>
        internal TextureChangedEventArgs(RenderState renderState)
        {
            m_renderState = renderState;
        }

        /// <summary>
        /// Gets current renderstate object.
        /// </summary>
        public RenderState RenderState
        {
            get { return m_renderState; }
        }
    }
}
