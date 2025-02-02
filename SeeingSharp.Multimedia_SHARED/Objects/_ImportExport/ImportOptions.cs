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
using System.ComponentModel;
using System.Numerics;

namespace SeeingSharp.Multimedia.Objects
{
    public class ImportOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ImportOptions"/> class.
        /// </summary>
        public ImportOptions()
        {
            this.ResourceCoordinateSystem = CoordinateSystem.LeftHanded_UpY;
            this.ResizeFactor = 1f;
            this.TwoSidedSurfaces = false;
        }

        /// <summary>
        /// Gets the transform matrix for coordinate system.
        /// </summary>
        /// <returns></returns>
        public Matrix4x4 GetTransformMatrixForCoordinateSystem()
        {
            switch (this.ResourceCoordinateSystem)
            {
                case CoordinateSystem.LeftHanded_UpY:
                    return Matrix4x4.Identity;

                case CoordinateSystem.LeftHanded_UpZ:
                    return
                        Matrix4x4.CreateScale(1f, -1f, 1f) *
                        Matrix4x4.CreateRotationX(-EngineMath.RAD_90DEG); ;

                case CoordinateSystem.RightHanded_UpY:
                    return Matrix4x4.CreateScale(new Vector3(1f, 1f, -1f));

                case CoordinateSystem.RightHanded_UpZ:
                    return
                        Matrix4x4.CreateRotationX(-EngineMath.RAD_90DEG);
            }

            return Matrix4x4.Identity;
        }

        /// <summary>
        /// Should triangle order be changes by the import logic?
        /// </summary>
        public bool IsChangeTriangleOrderNeeded()
        {
            switch (this.ResourceCoordinateSystem)
            {
                case CoordinateSystem.LeftHanded_UpY:
                case CoordinateSystem.RightHanded_UpZ:
                    return false;

                case CoordinateSystem.LeftHanded_UpZ:
                case CoordinateSystem.RightHanded_UpY:
                    return true;

                default:
                    throw new SeeingSharpGraphicsException(string.Format(
                        "Unknown coordinate system {0}!",
                        this.ResourceCoordinateSystem));
            }
        }

        /// <summary>
        /// Gets or sets the resize factor.
        /// This is needed to transform coordinate from one measure unit to another.
        /// Default is 1.
        /// </summary>
#if DESKTOP
        [Category(Translatables.IMPORT_OPTIONS_CAT_BASE_CONST)]
#endif
        public float ResizeFactor
        {
            get;
            set;
        }

        /// <summary>
        /// The resource may have a different coordinate system.
        /// This property ensures that the coordinate system is mapped correctly to the one that 
        /// Seeing# uses. Default is LeftHanded_UpY.
        /// </summary>
#if DESKTOP
        [Category(Translatables.IMPORT_OPTIONS_CAT_BASE_CONST)]
#endif
        public CoordinateSystem ResourceCoordinateSystem
        {
            get;
            set;
        }

        /// <summary>
        /// Needed some times. This generates a front and a back side for each loaded surface.
        /// Default is false.
        /// </summary>
#if DESKTOP
        [Category(Translatables.IMPORT_OPTIONS_CAT_BASE_CONST)]
#endif
        public bool TwoSidedSurfaces
        {
            get;
            set;
        }

#if DESKTOP
        [Category(Translatables.IMPORT_OPTIONS_CAT_BASE_CONST)]
#endif
        public bool ToggleTriangleIndexOrder
        {
            get;
            set;
        }
    }
}
