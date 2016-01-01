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
using SeeingSharp.Multimedia.Objects;
using SeeingSharp.Util;
using System;

namespace SeeingSharp.Multimedia.Drawing3D
{
    internal static class ResourceDictionaryExtensions
    {
        /// <summary>
        /// Gets or creates the material resource for the given VertexStructure object.
        /// </summary>
        internal static MaterialResource GetOrCreateMaterialResourceAndEnsureLoaded(this ResourceDictionary resourceDict, VertexStructure targetStructure)
        {
            MaterialResource materialResource = GetOrCreateMaterialResource(resourceDict, targetStructure);
            if (!materialResource.IsLoaded)
            {
                materialResource.LoadResource();
            }

            return materialResource;
        }

        /// <summary>
        /// Gets or creates the material resource for the given VertexStructure object.
        /// </summary>
        internal static MaterialResource GetOrCreateMaterialResource(this ResourceDictionary resourceDict, VertexStructure targetStructure)
        {
            NamedOrGenericKey materialKey = targetStructure.Material;
            NamedOrGenericKey textureKey = targetStructure.TextureKey;

            // Get the material if it is already created
            if ((!materialKey.IsEmpty) && (resourceDict.ContainsResource(materialKey))) { return resourceDict.GetResource<MaterialResource>(materialKey); }

            // Generate a material key
            if(materialKey.IsEmpty)
            {
                if (textureKey.IsEmpty) { materialKey = new NamedOrGenericKey(typeof(SimpleColoredMaterialResource)); }
                else { materialKey = new NamedOrGenericKey("Generated Material: " + textureKey.Description); }
            }

            // Get the material if it is already created
            if (resourceDict.ContainsResource(materialKey)) { return resourceDict.GetResource<MaterialResource>(materialKey); }

            if(textureKey.IsEmpty)
            {
                // Create a default material without any texture
                return resourceDict.AddResource<SimpleColoredMaterialResource>(materialKey, new SimpleColoredMaterialResource());
            }
            else
            {
                // Create texture resource if needed
                try
                {
                    if ((!resourceDict.ContainsResource(textureKey)) &&
                       (!string.IsNullOrEmpty(textureKey.NameKey)))
                    {
                        // Try to find and create the texture resource by its name
                        if (targetStructure.ResourceLink != null)
                        {
                            var textureResourceLink = targetStructure.ResourceLink.GetForAnotherFile(textureKey.NameKey);
                           
                            resourceDict.AddResource<StandardTextureResource>(
                                textureKey,
                                new StandardTextureResource(
                                    targetStructure.ResourceLink.GetForAnotherFile(textureKey.NameKey)));
                        }
                        else if (targetStructure.ResourceSourceAssembly != null)
                        {
                            var textureResourceLink = new AssemblyResourceLink(
                                targetStructure.ResourceSourceAssembly,
                                targetStructure.ResourceSourceAssembly.GetName().Name + ".Resources.Textures",
                                textureKey.NameKey);
                            if (textureResourceLink.IsValid())
                            {
                                resourceDict.AddResource<StandardTextureResource>(
                                    textureKey,
                                    new StandardTextureResource(textureResourceLink));
                            }
                            else
                            {
                                // Unable to resolve texture
                                textureKey = NamedOrGenericKey.Empty;
                            }
                        }
                        else
                        {
                            // Unable to resolve texture
                            textureKey = NamedOrGenericKey.Empty;
                        }
                    }
                }
                catch { }

                // Create a default textured material 
                if (!textureKey.IsEmpty)
                {
                    return resourceDict.AddResource<SimpleColoredMaterialResource>(materialKey, new SimpleColoredMaterialResource(textureKey));
                }
                else
                {
                    return resourceDict.AddResource<SimpleColoredMaterialResource>(materialKey, new SimpleColoredMaterialResource());
                }
            }
        }

        /// <summary>
        /// Adds a new geometry resource.
        /// </summary>
        internal static GeometryResource AddGeometry(this ResourceDictionary resourceDiciontary, params VertexStructure[] structures)
        {
            return resourceDiciontary.AddResource(new GeometryResource(structures));
        }

        /// <summary>
        /// Adds a new geometry resource.
        /// </summary>
        internal static GeometryResource AddGeometry(this ResourceDictionary resourceDiciontary, NamedOrGenericKey resourceKey, params VertexStructure[] structures)
        {
            return resourceDiciontary.AddResource(resourceKey, new GeometryResource(structures));
        }

        /// <summary>
        /// Adds a new geometry resource.
        /// </summary>
        internal static GeometryResource AddGeometry(this ResourceDictionary resourceDiciontary, ObjectType objectType)
        {
            return resourceDiciontary.AddResource(new GeometryResource(objectType));
        }

        /// <summary>
        /// Adds a new geometry resource.
        /// </summary>
        internal static GeometryResource AddGeometry(this ResourceDictionary resourceDiciontary, NamedOrGenericKey resourceKey, ObjectType objectType)
        {
            return resourceDiciontary.AddResource(resourceKey, new GeometryResource(objectType));
        }

        /// <summary>
        /// Adds a new text geometry with the given text.
        /// </summary>
        internal static GeometryResource AddTextGeometry(this ResourceDictionary resourceDiciontary, string textToAdd)
        {
            return resourceDiciontary.AddTextGeometry(textToAdd, TextGeometryOptions.Default);
        }

        /// <summary>
        /// Adds a new text geometry with the given text.
        /// </summary>
        internal static GeometryResource AddTextGeometry(this ResourceDictionary resourceDiciontary, NamedOrGenericKey resourceKey, string textToAdd)
        {
            return resourceDiciontary.AddTextGeometry(resourceKey, textToAdd, TextGeometryOptions.Default);
        }

        /// <summary>
        /// Adds a new text geometry with the given text.
        /// </summary>
        internal static GeometryResource AddTextGeometry(this ResourceDictionary resourceDiciontary, string textToAdd, TextGeometryOptions textGeometryOptions)
        {
            VertexStructure newStructure = new VertexStructure();
            newStructure.BuildTextGeometry(textToAdd, textGeometryOptions);
            newStructure.Material = textGeometryOptions.SurfaceMaterial;
            return resourceDiciontary.AddGeometry(newStructure);
        }

        /// <summary>
        /// Adds a new text geometry with the given text.
        /// </summary>
        internal static GeometryResource AddTextGeometry(this ResourceDictionary resourceDiciontary, NamedOrGenericKey resourceKey, string textToAdd, TextGeometryOptions textGeometryOptions)
        {
            VertexStructure newStructure = new VertexStructure();
            newStructure.BuildTextGeometry(textToAdd, textGeometryOptions);
            newStructure.Material = textGeometryOptions.SurfaceMaterial;
            return resourceDiciontary.AddGeometry(resourceKey, newStructure);
        }

#if DESKTOP
        /// <summary>
        /// Adds a new texture resource pointing to the given texture file name.
        /// </summary>
        internal static StandardTextureResource AddTexture(this ResourceDictionary resourceDiciontary, string textureFileName)
        {
            return resourceDiciontary.AddResource(new StandardTextureResource(textureFileName));
        }

        /// <summary>
        /// Adds a new texture resource pointing to the given texture file name.
        /// </summary>
        internal static StandardTextureResource AddTexture(this ResourceDictionary resourceDiciontary, NamedOrGenericKey resourceKey, string textureFileName)
        {
            return resourceDiciontary.AddResource(resourceKey, new StandardTextureResource(textureFileName));
        }

        /// <summary>
        /// Adds a new texture resource pointing to the given texture file name.
        /// </summary>
        internal static StandardTextureResource AddTexture(this ResourceDictionary resourceDiciontary, Uri textureResourceUri)
        {
            return resourceDiciontary.AddResource(new StandardTextureResource(textureResourceUri));
        }

        /// <summary>
        /// Adds a new texture resource pointing to the given texture file name.
        /// </summary>
        internal static StandardTextureResource AddTexture(this ResourceDictionary resourceDiciontary, NamedOrGenericKey resourceKey, Uri textureResourceUri)
        {
            return resourceDiciontary.AddResource(resourceKey, new StandardTextureResource(textureResourceUri));
        }
#endif
    }
}
