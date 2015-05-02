﻿#region License information (SeeingSharp and all based games/applications)
/*
    SeeingSharp and all games/applications based on it (more info at http://www.rolandk.de/wp)
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

using SeeingSharp.Multimedia.Core;
using SeeingSharp.Util;
using SeeingSharp.Checking;
using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Namespace mappings
using MF = SharpDX.MediaFoundation;

namespace SeeingSharp.Multimedia.DrawingVideo
{
    /// <summary>
    /// This object reads video streams from a file source using the MediaFoundation.
    /// See https://msdn.microsoft.com/de-de/library/windows/desktop/dd389281(v=vs.85).aspx
    /// </summary>
    public class MediaFoundationVideoReader : IDisposable, ICheckDisposed
    {
        #region Configuration
        private ResourceLink m_videoSource;
        #endregion

        #region Media foundation resources
        private Stream m_videoSourceStreamNet;
        private MF.ByteStream m_videoSourceStream;
        private MF.SourceReader m_sourceReader;
        private bool m_endReached;
        #endregion

        #region Video properties
        private Size2 m_frameSize;
        private long m_durationLong;
        private long m_currentPositionLong;
        private MediaSourceCharacteristics_Internal m_characteristics;
        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaFoundationVideoReader"/> class.
        /// </summary>
        /// <param name="videoSource">The source video file.</param>
        public MediaFoundationVideoReader(ResourceLink videoSource)
        {
            videoSource.EnsureNotNull("videoSource");

            try
            {
                m_videoSource = videoSource;

                // Create the source reader
                using (MF.MediaAttributes mediaAttributes = new MF.MediaAttributes(1))
                {
                    // We need the 'EnableVideoProcessing' attribute because of the RGB32 format
                    // see (lowest post): http://msdn.developer-works.com/article/11388495/How+to+use+SourceReader+(for+H.264+to+RGB+conversion)%3F
                    mediaAttributes.Set(MF.SourceReaderAttributeKeys.EnableVideoProcessing, 1);
                    mediaAttributes.Set(MF.SourceReaderAttributeKeys.DisableDxva, 1);

                    // Wrap the .net stream to a MF Bytestream
                    m_videoSourceStreamNet = m_videoSource.OpenInputStream();
                    m_videoSourceStream = new MF.ByteStream(m_videoSourceStreamNet);
                    using(MF.MediaAttributes byteStreamAttributes = m_videoSourceStream.QueryInterface<MF.MediaAttributes>())
                    {
                        byteStreamAttributes.Set(MF.ByteStreamAttributeKeys.OriginName, "Dummy." + videoSource.FileExtension);
                    }

                    // Create the sourcereader by custom native method (needed because of the ByteStream arg)
                    IntPtr sourceReaderPointer = IntPtr.Zero;
                    SharpDX.Result sdxResult = NativeMethods.MFCreateSourceReaderFromByteStream_Native(
                        m_videoSourceStream.NativePointer,
                        mediaAttributes.NativePointer,
                        out sourceReaderPointer);
                    sdxResult.CheckError();

                    m_sourceReader = new MF.SourceReader(sourceReaderPointer);
                }

                // Apply source configuration 
                using (MF.MediaType mediaType = new MF.MediaType())
                {
                    mediaType.Set(MF.MediaTypeAttributeKeys.MajorType, MF.MediaTypeGuids.Video);
                    mediaType.Set(MF.MediaTypeAttributeKeys.Subtype, MF.VideoFormatGuids.Rgb32);
                    m_sourceReader.SetCurrentMediaType(
                        MF.SourceReaderIndex.FirstVideoStream, 
                        mediaType);
                    m_sourceReader.SetStreamSelection(MF.SourceReaderIndex.FirstVideoStream, new SharpDX.Bool(true));
                }

                // Read some information about the source
                using (MF.MediaType mediaType = m_sourceReader.GetCurrentMediaType(MF.SourceReaderIndex.FirstVideoStream))
                {
                    long frameSizeLong = mediaType.Get(MF.MediaTypeAttributeKeys.FrameSize);
                    m_frameSize = new Size2(MFHelper.GetValuesByMFEncodedInts(frameSizeLong));
                }

                // Get additional propertie3s
                m_durationLong = m_sourceReader.GetPresentationAttribute(
                    MF.SourceReaderIndex.MediaSource, MF.PresentationDescriptionAttributeKeys.Duration);
                m_characteristics = (MediaSourceCharacteristics_Internal)m_sourceReader.GetPresentationAttribute(
                    MF.SourceReaderIndex.MediaSource, MF.SourceReaderAttributeKeys.MediaSourceCharacteristics);
            }
            catch(Exception)
            {
                this.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Reads the next frame and puts it into a newly generated buffer.
        /// </summary>
        public MemoryMappedTexture32bpp ReadFrame()
        {
            this.EnsureNotNullOrDisposed("this");

            MemoryMappedTexture32bpp result = new MemoryMappedTexture32bpp(m_frameSize);
            try
            {
                if (this.ReadFrame(result)) { return result; }
                else 
                {
                    result.Dispose();
                    return null;
                }
            }
            catch(Exception)
            {
                result.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Reads the next frame and puts it into the provided buffer.
        /// </summary>
        /// <param name="targetBuffer">The target buffer to write to.</param>
        public bool ReadFrame(MemoryMappedTexture32bpp targetBuffer)
        {
            this.EnsureNotNullOrDisposed("this");
            targetBuffer.EnsureNotNull("targetBuffer");
            if((targetBuffer.Width != m_frameSize.Width) ||
               (targetBuffer.Height != m_frameSize.Height))
            {
                throw new SeeingSharpGraphicsException("Size of the given buffer does not match the video size!");
            }

            MF.SourceReaderFlags readerFlags;
            int dummyStreamIndex;
            using (MF.Sample nextSample = m_sourceReader.ReadSample(
                MF.SourceReaderIndex.FirstVideoStream,
                MF.SourceReaderControlFlags.None,
                out dummyStreamIndex,
                out readerFlags,
                out m_currentPositionLong))
            {
                // Check for end-of-stream
                if (readerFlags == MF.SourceReaderFlags.Endofstream)
                {
                    m_endReached = true;
                    return false;
                }

                // No sample received
                if(nextSample == null)
                {
                    return false;
                }

                // Reset end-reached flag (maybe the user called SetPosition again..)
                m_endReached = false;

                // Copy pixel data into target buffer
                if (nextSample.BufferCount > 0)
                {
                    using (MF.MediaBuffer mediaBuffer = nextSample.GetBufferByIndex(0))
                    {
                        int cbMaxLength;
                        int cbCurrentLenght;
                        IntPtr mediaBufferPointer = mediaBuffer.Lock(out cbMaxLength, out cbCurrentLenght);
                        try
                        {
#if DESKTOP
                            // Performance optimization using MemCopy
                            //  see http://code4k.blogspot.de/2010/10/high-performance-memcpy-gotchas-in-c.html
                            NativeMethods.MemCopy(
                                targetBuffer.Pointer,
                                mediaBufferPointer,
                                new UIntPtr((uint)(m_frameSize.Width * m_frameSize.Height * 4)));
#else
                            unsafe
                            {
                                int* mediaBufferPointerNative = (int*)mediaBufferPointer.ToPointer();
                                int* targetBufferPointerNative = (int*)targetBuffer.Pointer.ToPointer();
                                for (int loopY = 0; loopY < m_frameSize.Height; loopY++)
                                {
                                    for (int loopX = 0; loopX < m_frameSize.Width; loopX++)
                                    {
                                        int actIndex = loopX + (loopY * m_frameSize.Width);
                                        targetBufferPointerNative[actIndex] = mediaBufferPointerNative[actIndex];
                                    }
                                }
                            }
#endif
                        }
                        finally
                        {
                            mediaBuffer.Unlock();
                        }

                        // Apply 1 to all alpha values (these are not changed by the video frame!)
                        targetBuffer.SetAllAlphaValuesToOne();

                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Sets the current position of this video reader.
        /// </summary>
        /// <param name="position">The position to be set.</param>
        public void SetCurrentPosition(TimeSpan position)
        {
            position.EnsureLongerOrEqualZero("position");
            position.EnsureShorterOrEqualThan(this.Duration, "position");
            this.EnsureSeekable("self");

            m_sourceReader.SetCurrentPosition(position.Ticks);
        }

        /// <summary>
        /// Disposes all native resources.
        /// </summary>
        public void Dispose()
        {
            GraphicsHelper.SafeDispose(ref m_sourceReader);
            GraphicsHelper.SafeDispose(ref m_videoSourceStream);
            GraphicsHelper.SafeDispose(ref m_videoSourceStreamNet);
        }

        /// <summary>
        /// Gets a value indicating whether this instance is disposed.
        /// </summary>
        public bool IsDisposed
        {
            get { return m_sourceReader == null; }
        }

        /// <summary>
        /// Did we reach the end of the video stream?
        /// </summary>
        public bool EndReached
        {
            get { return m_endReached; }
        }

        /// <summary>
        /// Gets the pixel size per frame.
        /// </summary>
        public Size2 FrameSize
        {
            get { return m_frameSize; }
        }

        /// <summary>
        /// Gets the total duration of the video.
        /// </summary>
        public TimeSpan Duration
        {
            get { return TimeSpan.FromMilliseconds((double)(m_durationLong / 10000)); }
        }

        /// <summary>
        /// Gets the current time position within the video.
        /// </summary>
        public TimeSpan CurrentPosition
        {
            get { return TimeSpan.FromMilliseconds((double)(m_currentPositionLong / 10000)); }
        }

        public bool IsSeekable
        {
            get 
            {
                return m_characteristics.HasFlag(MediaSourceCharacteristics_Internal.CanSeek) &&
                       (!m_characteristics.HasFlag(MediaSourceCharacteristics_Internal.HasSlowSeek));

            }
        }
    }
}