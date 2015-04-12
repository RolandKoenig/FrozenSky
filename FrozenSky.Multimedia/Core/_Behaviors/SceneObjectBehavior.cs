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

namespace FrozenSky.Multimedia.Core
{
    public abstract class SceneObjectBehavior
    {
        private SceneObject m_host;

        /// <summary>
        /// Initializes a new instance of the <see cref="SceneObjectBehavior"/> class.
        /// </summary>
        public SceneObjectBehavior()
        {

        }

        /// <summary>
        /// Sets the host object for this behavior.
        /// </summary>
        /// <param name="hostObject">The object that hosts this behavior.</param>
        internal void SetHostObject(SceneObject hostObject)
        {
            SceneObject oldHostObject = m_host;
            m_host = hostObject;

            OnHostObjectChanged(oldHostObject, m_host);
        }

        /// <summary>
        /// Update logic for per object updates.
        /// Be careful: This method is called in parallel with these methods on other objects.
        /// For methods that depend on other object, use UpdateOverall.
        /// </summary>
        /// <param name="updateState">Current update state of the scene.</param>
        public abstract void Update(UpdateState updateState);

        /// <summary>
        /// Update logic for overall updates.
        /// This method should be used for update logic that also depends on other object.
        /// UpdateOverall methods are called sequentially object by object.
        /// </summary>
        /// <param name="updateState">Current update state of the scene.</param>
        public abstract void UpdateOverall(UpdateState updateState);

        /// <summary>
        /// Called when the current host object has changed.
        /// </summary>
        /// <param name="previousHostObject">The previous host object.</param>
        /// <param name="newHostObject">The newly assigned host object.</param>
        protected virtual void OnHostObjectChanged(SceneObject previousHostObject, SceneObject newHostObject)
        {

        }

        /// <summary>
        /// Gets the host object for this behavior.
        /// </summary>
        public SceneObject HostObject
        {
            get { return m_host; }
        }
    }
}
