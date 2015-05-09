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

using SeeingSharp.Multimedia.Core;
using SeeingSharp.Multimedia.Drawing3D;
using SeeingSharp.Multimedia.DrawingVideo;
using SeeingSharp.Multimedia.Objects;
using SeeingSharp.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RKVideoMemory.Game
{
    /// <summary>
    /// This component is responsible for playing videos when the player has uncovered a CardPair.
    /// </summary>
    public class VideoPlayLogic : SceneLogicalObject
    {
        /// <summary>
        /// Called when user has uncovered a CardPair.
        /// </summary>
        private async void OnMessage_Received(CardPairUncoveredByPlayerMessage message)
        {
            // Define 'global' variables
            TexturePainter objVideoPainter = null;
            NamedOrGenericKey resVideoTexture = NamedOrGenericKey.Empty;

            // Get the link to the video file
            ResourceLink firstVideo =
                message.CardPair.PairData.ChildVideos.FirstOrDefault();
            if (firstVideo == null) { return; }

            // Attach the video texture to the scene
            Task startAnimationTask = null;
            await base.Scene.ManipulateSceneAsync((manipulator) =>
            {
                // Create the layer (if necessary)
                if (!manipulator.ContainsLayer(Constants.GFX_LAYER_VIDEO_FOREGROUND))
                {
                    SceneLayer bgLayer = manipulator.AddLayer(Constants.GFX_LAYER_VIDEO_FOREGROUND);
                    bgLayer.ClearDepthBufferBefreRendering = true;
                    manipulator.SetLayerOrderID(
                        bgLayer,
                        Constants.GFX_LAYER_VIDEO_FOREGROUND_ORDERID);
                }

                // Load the texture painter
                resVideoTexture = manipulator.AddResource(
                    () => new BitmapTextureResource(message.CardPair.PairData.FirstVideoFrame));
                objVideoPainter = new TexturePainter(resVideoTexture);
                objVideoPainter.Scaling = 0.6f;
                objVideoPainter.AccentuationFactor = 1f;
                objVideoPainter.Opacity = 0.0f;
                startAnimationTask = objVideoPainter.BuildAnimationSequence()
                    .Delay(300)
                    .WaitFinished()
                    .ScaleTo(1f, TimeSpan.FromMilliseconds(500))
                    .ChangeOpacityTo(1f, TimeSpan.FromMilliseconds(500))
                    .ApplyAsync();
              
                manipulator.Add(
                    objVideoPainter,
                    Constants.GFX_LAYER_VIDEO_FOREGROUND);
            });

            // Wait until start animation has finished
            await startAnimationTask;

            // Trigger start of video playing
            this.Messenger.Publish(new PlayMovieRequestMessage(firstVideo));

            // Wait for finished video rendering
            await this.Messenger.WaitForMessageAsync<PlayMovieFinishedMessage>();

            // Remove video resources again
            await base.Scene.ManipulateSceneAsync((manipulator) =>
            {
                manipulator.Remove(objVideoPainter);
                manipulator.RemoveResource(resVideoTexture);
            });

            // Tell the system that we are back on the mainscreen
            this.Messenger.Publish<MainMemoryScreenEnteredMessage>();
        }
    }
}
