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

namespace RKRocket
{
    internal static class Constants
    {
        public const string GAME_SCENE_NAME = "RocketScene";

        #region Screen properties
        public const float GFX_SCREEN_VPIXEL_WIDTH = 1920f;
        public const float GFX_SCREEN_VPIXEL_HEIGHT = 1080f;
        #endregion

        #region player rocket properties
        public const float GFX_ROCKET_VPIXEL_WIDTH = 105f;
        public const float GFX_ROCKET_VPIXEL_HEIGHT = 120f;
        public const float GFX_ROCKET_VPIXEL_Y_CENTER = 1000f;
        #endregion

        #region projectile properties
        public const float GFX_PROJECTILE_VPIXEL_WIDTH = 32f;
        public const float GFX_PROJECTILE_VPIXEL_HEIGHT = 32f;
        public const float SIM_PROJECTILE_SPEED = -1000f;
        public const float SIM_PROJECTILE_SPEED_AFTER_COLLISION = 600f;
        public const float SIM_PROJECTILE_BRAKE_RETARDATION = 550f;
        #endregion

        #region background properties
        public const float GFX_BACKGROUND_STAR_VPIXEL_WIDTH = 24f;
        public const float GFX_BACKGROUND_STAR_VPIXEL_HEIGHT = 24f;
        public const int GFX_BACKGROUND_MAX_STAR_COUNT = 200;
        public const int GFX_BACKGROUND_STAR_CREATE_PROPABILITY = 90;
        #endregion

        #region Block properties
        public const int BLOCKS_COUNT_X = 12;
        public const float BLOCK_OPACITY_NORMAL = 0.9f;
        public const float BLOCK_OPACITY_WHEN_LEAVING = 0.4f;
        public const int BLOCK_OPACITY_CHANGING_TIME_MS = 500;
        public const float BLOCK_VPIXEL_WIDTH = 128f;
        public const float BLOCK_VPIXEL_HEIGHT = 44f;
        public const float BLOCK_LEAVING_Y_TARGET = 1300f;
        public const float BLOCK_LEAVING_MAX_SPEED = 1200f;
        public const float BLOCK_LEAVING_ACCELERATION = 500f;
        #endregion


    }
}