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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrozenSky
{
    public partial struct Matrix
    {
        /// <summary>
        /// Gets a matrix for given horizontal and vertical rotation.
        /// </summary>
        /// <param name="hRotation">Horizontal rotation angle.</param>
        /// <param name="vRotation">Vertical rotation angle.</param>
        public static Matrix RotationHV(float hRotation, float vRotation)
        {
            return Matrix.RotationYawPitchRoll(hRotation, vRotation, 0f);//Matrix4.RotationZ(vRotation) * Matrix4.RotationY(hRotation);
        }

        /// <summary>
        /// Gets a matrix for given horizontal and vertical rotation.
        /// </summary>
        /// <param name="rotation">Vector containing horizontal and vertical rotation angles.</param>
        public static Matrix RotationHV(Vector2 rotation)
        {
            return RotationHV(rotation.X, rotation.Y);
        }

        /// <summary>
        /// Gets a rotation matrix for the given direction vectors.
        /// </summary>
        /// <param name="upVector">The up vector (standard: y-axis).</param>
        /// <param name="forwardVector">The forward vector (standard: x-axis)</param>
        public static Matrix RotationDirection(Vector3 upVector, Vector3 forwardVector)
        {
            Vector3 right = Vector3.Cross(upVector, forwardVector);
            return new Matrix(
                right.X, right.Y, right.Z, 0f,
                upVector.X, upVector.Y, upVector.Z, 0f,
                forwardVector.X, forwardVector.Y, forwardVector.Z, 0f,
                0f, 0f, 0f, 1f);
        }
    }
}
