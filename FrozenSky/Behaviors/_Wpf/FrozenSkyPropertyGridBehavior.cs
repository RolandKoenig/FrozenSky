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

#if DESKTOP
using FrozenSky.Util;
using PropertyTools.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Interactivity;

namespace FrozenSky.Behaviors
{
    public class FrozenSkyPropertyGridBehavior : Behavior<PropertyGrid>
    {
        /// <summary>
        /// Wird nach dem Anfügen des Verhaltens an das "AssociatedObject" aufgerufen.
        /// </summary>
        protected override void OnAttached()
        {
            base.OnAttached();

            base.AssociatedObject.PropertyControlFactory = new FrozenSkyPropertyControlFactory();
            base.AssociatedObject.PropertyItemFactory = new FrozenSkyPropertyItemFactory();
        }

        /// <summary>
        /// Wird aufgerufen, wenn das Verhalten vom "AssociatedObject" getrennt wird. Der Aufruf erfolgt vor dem eigentlichen Trennvorgang.
        /// </summary>
        protected override void OnDetaching()
        {
            base.OnDetaching();
        }
    }
}
#endif