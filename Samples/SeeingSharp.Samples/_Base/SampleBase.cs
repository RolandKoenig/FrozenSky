﻿#region License information (SeeingSharp and all based games/applications)
/*
    Seeing# and all games/applications distributed together with it. 
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
using SeeingSharp.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeeingSharp.Samples.Base
{
    public abstract class SampleBase : ViewModelBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SampleBase"/> class.
        /// </summary>
        public SampleBase()
        {

        }

        public void SetClosed()
        {
            if(!this.IsClosed)
            {
                this.IsClosed = true;
                this.OnClosed();
            }
        }

        /// <summary>
        /// Called when the sample has to startup.
        /// </summary>
        /// <param name="targetRenderLoop">The target render loop.</param>
        public abstract Task OnStartupAsync(RenderLoop targetRenderLoop);

        /// <summary>
        /// Called when the sample is closed.
        /// Scene objects and resources are automatically removed, no need to do it
        /// manually in this method's implementation.
        /// </summary>
        public virtual void OnClosed()
        {

        }

        public bool IsClosed
        {
            get;
            private set;
        }
    }
}
