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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeeingSharp.Util
{
    /// <summary>
    /// Indicates that a property is not to be cloned by CommonTools.CloneObject method.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class IgnoreGenericCloneAttribute : Attribute
    {

    }

    /// <summary>
    /// Inidicates that this field will get a reference to the original parent object after being cloned from it using CommonTools.Clone
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class AssignOriginalParentObjectAfterCloneAttribute : Attribute
    {

    }
}
