﻿#region License information (FrozenSky and all based games/applications)
/*
    FrozenSky and all games/applications based on it (more info at http://www.rolandk.de/wp)
    Copyright (C) 2014 Roland König (RolandK)

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
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FrozenSky.Util
{
    public class AssemblyResourceInfo
    {
        private Assembly m_targetAssembly;
        private string m_resourcePath;
        private string m_key;

        /// <summary>
        /// Creates a new AssemblyResourceInfo object
        /// </summary>
        internal AssemblyResourceInfo(Assembly targetAssembly, string resourcePath, string key)
        {
            m_targetAssembly = targetAssembly;
            m_resourcePath = resourcePath;
            m_key = key;
        }

        /// <summary>
        /// Opens a reading stream
        /// </summary>
        public Stream OpenRead()
        {
            return m_targetAssembly.GetManifestResourceStream(m_resourcePath);
        }

        /// <summary>
        /// Gets the path to the resource
        /// </summary>
        public string ResourcePath
        {
            get { return m_resourcePath; }
        }

        /// <summary>
        /// Gets the target assembly
        /// </summary>
        public Assembly TargetAssembly
        {
            get { return m_targetAssembly; }
        }

        /// <summary>
        /// Gets the key of this object
        /// </summary>
        public string Key
        {
            get { return m_key; }
        }
    }
}
