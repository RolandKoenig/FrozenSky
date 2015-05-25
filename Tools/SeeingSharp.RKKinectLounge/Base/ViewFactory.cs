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
#endregion License information (SeeingSharp and all based games/applications)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using SeeingSharp.Infrastructure;
using SeeingSharp.RKKinectLounge.Base;
using SeeingSharp.RKKinectLounge.Modules.Kinect;
using SeeingSharp.RKKinectLounge.Modules.Multimedia;
using SeeingSharp.Util;

namespace SeeingSharp.RKKinectLounge.Base
{
    public class ViewFactory
    {
        /// <summary>
        /// Creates the view object for the given ViewModel.
        /// The created object will be displayed on the whole window.
        /// </summary>
        /// <param name="viewModel">The view model for which to create a view.</param>
        public static FrameworkElement CreateFullView(NavigateableViewModelBase viewModel)
        {
            // Try to create the view using one of the loaded modules
            // (first one wins)
            foreach (IKinectLoungeModule actModule in ModuleManager.LoadedModules)
            {
                FrameworkElement actResult = actModule.TryCreateFullView(viewModel);
                if (actResult != null) { return actResult; }
            }

            // Create a FolderView object if nothing else found
            FolderView result = new FolderView();
            result.DataContext = viewModel;
            return result;
        }
    }
}