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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace RKVideoMemory.Data
{
    [XmlType("LevelSettings")]
    public class LevelSettings
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LevelSettings"/> class.
        /// </summary>
        public LevelSettings()
        {
            this.AppName = string.Empty;
        }

        public static LevelSettings Default
        {
            get
            {
                return new LevelSettings();
            }
        }

        [XmlElement]
        public string AppName
        {
            get;
            set;
        }
    }
}
