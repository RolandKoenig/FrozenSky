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

using FrozenSky.Multimedia.Core;
using FrozenSky.Multimedia.Objects;
using FrozenSky.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrozenSky.Multimedia.Drawing3D
{
    public static class SceneManipulatorExtensions
    {
        /// <summary>
        /// Adds a new texture resource to the scene.
        /// </summary>
        /// <param name="sceneManipulator">The manipulator of the scene.</param>
        /// <param name="textureSource">The source of the texture.</param>
        public static NamedOrGenericKey AddTexture(this SceneManipulator sceneManipulator, ResourceLink textureSource)
        {
            return sceneManipulator.AddResource<StandardTextureResource>(() => new StandardTextureResource(textureSource));
        }

        /// <summary>
        /// Adds a new texture resource to the scene.
        /// </summary>
        /// <param name="sceneManipulator">The manipulator of the scene.</param>
        /// <param name="textureSourceHighQuality">The source of the texture in high quality.</param>
        /// <param name="textureSourceLowQuality">The texture in low quality.</param>
        public static NamedOrGenericKey AddTexture(
            this SceneManipulator sceneManipulator, 
            ResourceLink textureSourceHighQuality,
            ResourceLink textureSourceLowQuality)
        {
            return sceneManipulator.AddResource<StandardTextureResource>(() => new StandardTextureResource(textureSourceHighQuality, textureSourceLowQuality));
        }

        /// <summary>
        /// Adds a new simple colored material resource to the scene.
        /// </summary>
        /// <param name="sceneManipulator">The manipulator of the scene.</param>
        public static NamedOrGenericKey AddSimpleColoredMaterial(this SceneManipulator sceneManipulator)
        {
            return sceneManipulator.AddResource<SimpleColoredMaterialResource>(() => new SimpleColoredMaterialResource());
        }

        /// <summary>
        /// Adds a new simple colored material resource to the scene.
        /// </summary>
        /// <param name="sceneManipulator">The manipulator of the scene.</param>
        /// <param name="textureKey">The resource key of the texture to be used.</param>
        public static NamedOrGenericKey AddSimpleColoredMaterial(this SceneManipulator sceneManipulator, NamedOrGenericKey textureKey)
        {
            return sceneManipulator.AddResource<SimpleColoredMaterialResource>(() => new SimpleColoredMaterialResource(textureKey));
        }

        /// <summary>
        /// Adds a new simple colored material resource to the scene.
        /// </summary>
        /// <param name="sceneManipulator">The manipulator of the scene.</param>
        /// <param name="textureSource">The source of the texture which should be loaded.</param>
        public static NamedOrGenericKey AddSimpleColoredMaterial(this SceneManipulator sceneManipulator, ResourceLink textureSource)
        {
            NamedOrGenericKey resTexture = sceneManipulator.AddTexture(textureSource);
            return sceneManipulator.AddResource<SimpleColoredMaterialResource>(() => new SimpleColoredMaterialResource(resTexture));
        }

        /// <summary>
        /// Adds a new simple colored material resource to the scene.
        /// </summary>
        /// <param name="sceneManipulator">The manipulator of the scene.</param>
        /// <param name="textureSourceHighQuality">The source of the texture which should be loaded.</param>
        /// <param name="textureSourceLowQuality">The source of the texture with low quality.</param>
        public static NamedOrGenericKey AddSimpleColoredMaterial(
            this SceneManipulator sceneManipulator, 
            ResourceLink textureSourceHighQuality, ResourceLink textureSourceLowQuality)
        {
            NamedOrGenericKey resTexture = sceneManipulator.AddTexture(textureSourceHighQuality, textureSourceLowQuality);
            return sceneManipulator.AddResource<SimpleColoredMaterialResource>(() => new SimpleColoredMaterialResource(resTexture));
        }

        /// <summary>
        /// Adds a new geometry resource to the scene.
        /// </summary>
        /// <param name="sceneManipulator">The manipulator of the scene.</param>
        /// <param name="textureKey">The geometry to be loaded.</param>
        public static NamedOrGenericKey AddGeometry(this SceneManipulator sceneManipulator, params VertexStructure[] vertexStructures)
        {
            return sceneManipulator.AddResource<GeometryResource>(() => new GeometryResource(vertexStructures));
        }

        /// <summary>
        /// Adds a new geometry resource to the scene.
        /// </summary>
        /// <param name="sceneManipulator">The manipulator of the scene.</param>
        /// <param name="objectType">The geometry to be loaded.</param>
        public static NamedOrGenericKey AddGeometry(this SceneManipulator sceneManipulator, ObjectType objectType)
        {
            return sceneManipulator.AddResource<GeometryResource>(() => new GeometryResource(objectType));
        }
    }
}
