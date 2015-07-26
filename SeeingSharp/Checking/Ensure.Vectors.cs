﻿#region License information (SeeingSharp and all based games/applications)
/*
    Seeing# and all games/applications distributed together with it. 
    More info at 
     - https://github.com/RolandKoenig/SeeingSharp (sourcecode)
     - http://www.rolandk.de/wp (the autors homepage, german)
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
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace SeeingSharp.Checking
{
    public static partial class Ensure
    {
        [Conditional("DEBUG")]
        public static void EnsureNormalized(
            this Vector3 vectorValue, string checkedVariableName,
            [CallerMemberName]
            string callerMethod = "")
        {
            if (string.IsNullOrEmpty(callerMethod)) { callerMethod = "Unknown"; }

            if (!EngineMath.EqualsWithTolerance(vectorValue.Length(), 1f))
            {
                throw new SeeingSharpCheckException(string.Format(
                    "Vector {0} within method {1} must be normalized!",
                    checkedVariableName, callerMethod, vectorValue));
            }
        }

        [Conditional("DEBUG")]
        public static void EnsureNormalized(
            this Vector2 vectorValue, string checkedVariableName,
            [CallerMemberName]
            string callerMethod = "")
        {
            if (string.IsNullOrEmpty(callerMethod)) { callerMethod = "Unknown"; }

            if (!EngineMath.EqualsWithTolerance(vectorValue.Length(), 1f))
            {
                throw new SeeingSharpCheckException(string.Format(
                    "Vector {0} within method {1} must be normalized!",
                    checkedVariableName, callerMethod, vectorValue));
            }
        }
    }
}
