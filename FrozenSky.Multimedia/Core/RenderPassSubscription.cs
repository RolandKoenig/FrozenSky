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
    public struct RenderPassSubscription
    {
        internal ViewRelatedSceneLayerSubset LayerViewSubset;
        internal RenderPassInfo RenderPass;
        internal SceneObject SceneObject;
        internal Action<RenderState> RenderMethod;

        /// <summary>
        /// Very important variable to find the subscription in the host collection!
        /// </summary>
        internal int SubscriptionIndex;

        /// <summary>
        /// A special flag for optimizing unsubscription process.
        /// </summary>
        internal bool IsSubscribed;

        /// <summary>
        /// Initializes a new instance of the <see cref="RenderPassSubscription" /> struct.
        /// </summary>
        /// <param name="layerViewSubset">The ViewRelatedSceneLayerSubset object this subscription belongs to.</param>
        /// <param name="renderPass">The render pass on which to register.</param>
        /// <param name="sceneObject">The scene object which should be registered.</param>
        /// <param name="renderMethod">The render method which is to be registered.</param>
        internal RenderPassSubscription(ViewRelatedSceneLayerSubset layerViewSubset, RenderPassInfo renderPass, SceneObject sceneObject, Action<RenderState> renderMethod)
        {
            LayerViewSubset = layerViewSubset;
            RenderPass = renderPass;
            SceneObject = sceneObject;
            RenderMethod = renderMethod;
            IsSubscribed = true;
            SubscriptionIndex = 0;
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return "{" + SceneObject + "} => {" + RenderPass + "}";
        }

        /// <summary>
        /// Unsubscribes this subscription.
        /// </summary>
        public void Unsubscribe()
        {
            LayerViewSubset.UnsubscribeForPass(this);

            //if (SceneObject != null)
            //{
            //    IsSubscribed = false;
            //    //
            //    //LayerViewSubset = null;
            //    //RenderPass = null;
            //    //SceneObject = null;
            //    //RenderMethod = null;
            //}
        }
    }
}
