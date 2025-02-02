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
using SeeingSharp.Util;
using SeeingSharp.Multimedia.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Some namespace mappings
using D2D = SharpDX.Direct2D1;
using DWrite = SharpDX.DirectWrite;

namespace SeeingSharp.Multimedia.Drawing2D
{
    public class TextFormatResource : Drawing2DResourceBase
    {
        #region Fixed resource parameters (passed on constructor)
        private DWrite.TextFormat[] m_loadedTextFormats;
        private string m_fontFamilyName;
        private float m_fontSize;
        private DWrite.FontWeight m_fontWeight;
        private DWrite.FontStyle m_fontStyle;
        private DWrite.FontStretch m_fontStretch;
        #endregion

        // Dynamic runtime parameters (possible to pass on each render call)
        # region
        private bool[] m_runtimeDataChangedFlags;
        private DWrite.ParagraphAlignment m_paragraphAlignment;
        private DWrite.TextAlignment m_textAlignment;
        private DWrite.WordWrapping m_wordWrapping;
        private DWrite.ReadingDirection m_readingDirection;
        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="TextFormatResource"/> class.
        /// </summary>
        /// <param name="fontFamilyName">Name of the font family.</param>
        /// <param name="fontSize">Size of the font.</param>
        /// <param name="fontWeight">The weight of the font.</param>
        /// <param name="fontStretch">The stretch parameter for the font.</param>
        /// <param name="fontStyle">The style parameter for the font.</param>
        public TextFormatResource(
            string fontFamilyName, float fontSize, 
            FontWeight fontWeight = FontWeight.Normal,
            FontStyle fontStyle = FontStyle.Normal,
            FontStretch fontStretch = FontStretch.Normal)
        {
            m_loadedTextFormats = new DWrite.TextFormat[GraphicsCore.Current.DeviceCount];
            m_runtimeDataChangedFlags = new bool[GraphicsCore.Current.DeviceCount];
            m_fontFamilyName = fontFamilyName;
            m_fontSize = fontSize;
            m_fontWeight = (DWrite.FontWeight)fontWeight;
            m_fontStyle = (DWrite.FontStyle)fontStyle;
            m_fontStretch = (DWrite.FontStretch)fontStretch;

            m_paragraphAlignment = DWrite.ParagraphAlignment.Near;
            m_textAlignment = DWrite.TextAlignment.Leading;
            m_wordWrapping = DWrite.WordWrapping.Wrap;
            m_readingDirection = DWrite.ReadingDirection.LeftToRight;
        }

        /// <summary>
        /// Unloads all resources loaded on the given device.
        /// </summary>
        /// <param name="engineDevice">The device for which to unload the resource.</param>
        internal override void UnloadResources(EngineDevice engineDevice)
        {
            DWrite.TextFormat textFormat = m_loadedTextFormats[engineDevice.DeviceIndex];
            if (textFormat != null)
            {
                GraphicsHelper.DisposeObject(textFormat);
                m_loadedTextFormats[engineDevice.DeviceIndex] = null;
            }
        }

        /// <summary>
        /// Gets the TextFormat object for the given device.
        /// </summary>
        /// <param name="engineDevice">The device for which to get the brush.</param>
        internal DWrite.TextFormat GetTextFormat(EngineDevice engineDevice)
        {
            // Check for disposed state
            if (base.IsDisposed) { throw new ObjectDisposedException(this.GetType().Name); }


            DWrite.TextFormat result = m_loadedTextFormats[engineDevice.DeviceIndex];
            if (result == null)
            {
                // Load the TextFormat object
                result = new DWrite.TextFormat(
                    GraphicsCore.Current.FactoryDWrite,
                    m_fontFamilyName,
                    m_fontWeight, m_fontStyle, m_fontStretch, m_fontSize);
                m_loadedTextFormats[engineDevice.DeviceIndex] = result;
            }

            // Update runtime values on demand
            if(m_runtimeDataChangedFlags[engineDevice.DeviceIndex])
            {
                m_runtimeDataChangedFlags[engineDevice.DeviceIndex] = false;
                result.ParagraphAlignment = m_paragraphAlignment;
                result.TextAlignment = m_textAlignment;
                result.WordWrapping = m_wordWrapping;
                result.ReadingDirection = m_readingDirection;
            }

            return result;
        }

        /// <summary>
        /// Gets or sets the alignment of the paragraph.
        /// </summary>
        public ParagraphAlignment ParagraphAlignment
        {
            get { return (ParagraphAlignment)m_paragraphAlignment; }
            set
            {
                DWrite.ParagraphAlignment castedValue = (DWrite.ParagraphAlignment)value;
                if(castedValue != m_paragraphAlignment)
                {
                    m_paragraphAlignment = castedValue;
                    m_runtimeDataChangedFlags.SetAllValuesTo(true);
                }
            }
        }

        /// <summary>
        /// Gets or sets the alignment of the paragraph.
        /// </summary>
        public TextAlignment TextAlignment
        {
            get { return (TextAlignment)m_textAlignment; }
            set
            {
                DWrite.TextAlignment castedValue = (DWrite.TextAlignment)value;
                if(castedValue != m_textAlignment)
                {
                    m_textAlignment = castedValue;
                    m_runtimeDataChangedFlags.SetAllValuesTo(true);
                }
            }
        }

        /// <summary>
        /// Gets or sets the WordWrapping mode.
        /// </summary>
        public WordWrapping WordWrapping
        {
            get { return (WordWrapping)m_wordWrapping; }
            set
            {
                DWrite.WordWrapping castedValue = (DWrite.WordWrapping)value;
                if(castedValue != m_wordWrapping)
                {
                    m_wordWrapping = castedValue;
                    m_runtimeDataChangedFlags.SetAllValuesTo(true);
                }
            }
        }

        /// <summary>
        /// Gets or sets the reading direction.
        /// </summary>
        public ReadingDirection ReadingDirection
        {
            get { return (ReadingDirection)m_readingDirection; }
            set
            {
                DWrite.ReadingDirection castedValue = (DWrite.ReadingDirection)value;
                if(castedValue != m_readingDirection)
                {
                    m_readingDirection = castedValue;
                    m_runtimeDataChangedFlags.SetAllValuesTo(true);
                }
            }
        }
    }
}
