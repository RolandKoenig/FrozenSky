﻿#region License information (SeeingSharp and all based games/applications)
/*
    Seeing# and all games/applications distributed together with it. 
	Exception are projects where it is noted otherwhise.
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
using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Numerics;
using SeeingSharp.Util;

namespace SeeingSharp.Multimedia.Objects
{
    public static class ACFileLoader
    {
        /// <summary>
        /// Imports an object-type form given raw model file.
        /// </summary>
        /// <param name="rawBytes">Raw model file.</param>
        public static ObjectType ImportObjectType(byte[] rawBytes)
        {
            using (MemoryStream inStream = new MemoryStream(rawBytes))
            {
                return ImportObjectType(inStream);
            }
        }

        /// <summary>
        /// Loads an object from the given uri
        /// </summary>
        public static ObjectType ImportObjectType(Stream inStream)
        {
            VertexStructure structures = ImportVertexStructure(inStream);
            return new GenericObjectType(structures);
        }

        /// <summary>
        /// Imports a ac file from the given stream.
        /// </summary>
        /// <param name="resourceLink">The link to the ac file.</param>
        public static ObjectType ImportObjectType(ResourceLink resourceLink)
        {
            using (Stream inStream = resourceLink.OpenInputStream())
            {
                VertexStructure structure = ImportVertexStructure(inStream, resourceLink);
                return new GenericObjectType(structure);
            }
        }

        /// <summary>
        /// Imports a VertexStructure from the given stream.
        /// </summary>
        /// <param name="inStream">The stream to load the data from.</param>
        public static VertexStructure ImportVertexStructure(Stream inStream)
        {
            return ImportVertexStructure(inStream, null);
        }

        /// <summary>
        /// Imports VertexStructures from the given stream.
        /// </summary>
        /// <param name="inStream">The stream to load the data from.</param>
        /// <param name="originalSource">The original source of the generated geometry.</param>
        public static VertexStructure ImportVertexStructure(Stream inStream, ResourceLink originalSource)
        {
            try
            {
                // Load the file and generate all VertexStructures
                ACFileInfo fileInfo = LoadFile(inStream);
                VertexStructure result = GenerateStructure(fileInfo);
                result.ResourceLink = originalSource;

                // return the result
                return result;
            }
            catch (Exception)
            {
                // Create dummy VertexStructure
                VertexStructure dummyStructure = new VertexStructure();
                dummyStructure.FirstSurface.BuildCube24V(
                    new Vector3(),
                    new Vector3(1f, 1f, 1f),
                    Color4.Transparent);
                return dummyStructure;
            }
        }

        /// <summary>
        /// Generates all vertex structures needed for this object
        /// </summary>
        private static VertexStructure GenerateStructure(ACFileInfo fileInfo)
        {
            VertexStructure result = new VertexStructure();

            // Create all vertex structures
            Matrix4Stack transformStack = new Matrix4Stack();
            foreach (ACObjectInfo actObject in fileInfo.Objects)
            {
                FillVertexStructure(result, fileInfo.Materials, actObject, transformStack);
            }

            return result;
        }

        /// <summary>
        /// Fills the given vertex structure using information from the given AC-File-Objects.
        /// </summary>
        /// <param name="objInfo">The object information from the AC file.</param>
        /// <param name="acMaterials">A list containing all materials from the AC file.</param>
        /// <param name="structure">The VertexStructure to be filled.</param>
        /// <param name="transformStack">Current matrix stack (for stacked objects).</param>
        private static void FillVertexStructure(VertexStructure structure, List<ACMaterialInfo> acMaterials, ACObjectInfo objInfo, Matrix4Stack transformStack)
        {
            List<Tuple<int, int>> standardShadedVertices = new List<Tuple<int, int>>();

            transformStack.Push();
            try
            {
                // Perform local transformation for the current AC object
                transformStack.TransformLocal(objInfo.Rotation);
                transformStack.TranslateLocal(objInfo.Translation);

                // Build structures material by material
                for (int actMaterialIndex = 0; actMaterialIndex < acMaterials.Count; actMaterialIndex++)
                {
                    ACMaterialInfo actMaterial = acMaterials[actMaterialIndex];

                    VertexStructureSurface actStructSurface = structure.CreateOrGetExistingSurface(actMaterial.CreateMaterialProperties());
                    bool isNewSurface = actStructSurface.CountTriangles == 0;

                    // Create and configure vertex structure
                    actStructSurface.Material = NamedOrGenericKey.Empty;
                    actStructSurface.TextureKey = !string.IsNullOrEmpty(objInfo.Texture) ? new NamedOrGenericKey(objInfo.Texture) : NamedOrGenericKey.Empty;
                    actStructSurface.MaterialProperties.DiffuseColor = actMaterial.Diffuse;
                    actStructSurface.MaterialProperties.AmbientColor = actMaterial.Ambient;
                    actStructSurface.MaterialProperties.EmissiveColor = actMaterial.Emissive;
                    actStructSurface.MaterialProperties.Shininess = actMaterial.Shininess;
                    actStructSurface.MaterialProperties.SpecularColor = actMaterial.Specular;

                    // Initialize local index table (needed for vertex reuse)
                    int oneSideVertexCount = objInfo.Vertices.Count;
                    int[] localIndices = new int[oneSideVertexCount * 2];
                    for (int loop = 0; loop < localIndices.Length; loop++)
                    {
                        localIndices[loop] = int.MaxValue;
                    }

                    // Process all surfaces
                    foreach (ACSurface actSurface in objInfo.Surfaces)
                    {
                        // Get the vertex index on which to start
                        int startVertexIndex = structure.CountVertices;
                        int startTriangleIndex = actStructSurface.CountTriangles;

                        // Only handle surfaces of the current material
                        if (actSurface.Material != actMaterialIndex) { continue; }

                        // Sort out unsupported surfaces
                        if (actSurface.VertexReferences.Count < 3) { continue; }
                        if (actSurface.IsLine) { continue; }
                        if (actSurface.IsClosedLine) { continue; }

                        // Preprocess referenced vertices
                        int oneSideSurfaceVertexCount = actSurface.VertexReferences.Count;
                        int countSurfaceSides = actSurface.IsTwoSided ? 2 : 1;
                        int[] onStructureReferencedVertices = new int[oneSideSurfaceVertexCount * countSurfaceSides];
                        List<int> surfaceVertexReferences = actSurface.VertexReferences;
                        for (int loop = 0; loop < surfaceVertexReferences.Count; loop++)
                        {
                            Vector2 actTexCoord = actSurface.TextureCoordinates[loop];

                            if (!actSurface.IsFlatShaded)
                            {
                                // Try to reuse vertices on standard shading
                                if (localIndices[surfaceVertexReferences[loop]] == int.MaxValue)
                                {
                                    Vector3 position = Vector3.Transform(
                                        objInfo.Vertices[surfaceVertexReferences[loop]].Position,
                                        transformStack.Top);
                                    localIndices[surfaceVertexReferences[loop]] = structure.AddVertex(new Vertex(
                                        position, Color4.White, actTexCoord, Vector3.Zero));
                                    if (actSurface.IsTwoSided)
                                    {
                                        localIndices[surfaceVertexReferences[loop] + oneSideVertexCount] = structure.AddVertex(new Vertex(
                                            position, Color4.White, actTexCoord, Vector3.Zero));
                                    }
                                }

                                // Store vertex reference for this surface's index
                                onStructureReferencedVertices[loop] = localIndices[surfaceVertexReferences[loop]];
                                if (actSurface.IsTwoSided)
                                {
                                    onStructureReferencedVertices[loop + oneSideSurfaceVertexCount] =
                                        localIndices[surfaceVertexReferences[loop] + oneSideVertexCount];
                                }
                            }
                            else
                            {
                                // Create one vertex for one reference for flat shading
                                Vector3 position = Vector3.Transform(
                                    objInfo.Vertices[surfaceVertexReferences[loop]].Position,
                                    transformStack.Top);
                                onStructureReferencedVertices[loop] = structure.AddVertex(new Vertex(
                                    position, Color4.White, actTexCoord, Vector3.Zero));
                                if (actSurface.IsTwoSided)
                                {
                                    onStructureReferencedVertices[loop + oneSideSurfaceVertexCount] = structure.AddVertex(new Vertex(
                                        position, Color4.White, actTexCoord, Vector3.Zero));
                                }
                            }
                        }

                        // Build object geometry
                        switch (actSurface.VertexReferences.Count)
                        {
                            case 3:
                                // Front side
                                actStructSurface.AddTriangle(
                                    onStructureReferencedVertices[0],
                                    onStructureReferencedVertices[1],
                                    onStructureReferencedVertices[2]);

                                // Back side
                                if (actSurface.IsTwoSided)
                                {
                                    actStructSurface.AddTriangle(
                                        onStructureReferencedVertices[5],
                                        onStructureReferencedVertices[4],
                                        onStructureReferencedVertices[3]);
                                }
                                break;

                            case 4:
                                // Front side
                                actStructSurface.AddTriangle(
                                    onStructureReferencedVertices[0],
                                    onStructureReferencedVertices[1],
                                    onStructureReferencedVertices[2]);
                                actStructSurface.AddTriangle(
                                    onStructureReferencedVertices[2],
                                    onStructureReferencedVertices[3],
                                    onStructureReferencedVertices[0]);

                                // Back side
                                if (actSurface.IsTwoSided)
                                {
                                    actStructSurface.AddTriangle(
                                        onStructureReferencedVertices[6],
                                        onStructureReferencedVertices[5],
                                        onStructureReferencedVertices[4]);
                                    actStructSurface.AddTriangle(
                                        onStructureReferencedVertices[4],
                                        onStructureReferencedVertices[7],
                                        onStructureReferencedVertices[6]);
                                }
                                break;

                            default:
                                if (!actSurface.IsTwoSided)
                                {
                                    // Front side
                                    actStructSurface.AddPolygonByCuttingEars(onStructureReferencedVertices);
                                }
                                else
                                {
                                    // Front and back side
                                    actStructSurface.AddPolygonByCuttingEars(onStructureReferencedVertices.Subset(0, oneSideSurfaceVertexCount));
                                    actStructSurface.AddPolygonByCuttingEars(onStructureReferencedVertices.Subset(oneSideSurfaceVertexCount, oneSideSurfaceVertexCount));
                                }
                                break;
                        }

                        // Perform shading
                        if (actSurface.IsFlatShaded)
                        {
                            actStructSurface.CalculateNormalsFlat(
                                startTriangleIndex, actStructSurface.CountTriangles - startTriangleIndex);
                        }
                        else
                        {
                            // Nothing to be done for now..
                            int vertexCount = structure.CountVertices - startVertexIndex;
                            if (vertexCount > 0)
                            {
                                standardShadedVertices.Add(
                                    Tuple.Create((int)startVertexIndex, vertexCount));
                            }
                        }
                    }

                    // Calculate default shading finally (if any)
                    foreach (var actStandardShadedPair in standardShadedVertices)
                    {
                        structure.CalculateNormals(
                            actStandardShadedPair.Item1,
                            actStandardShadedPair.Item2);
                    }
                    standardShadedVertices.Clear();

                    // Append generated VertexStructure to the output collection
                    if ((actStructSurface.CountTriangles <= 0) &&
                        (isNewSurface))
                    {
                        structure.RemoveSurface(actStructSurface);
                    }
                }

                //Fill in all child object data
                foreach (ACObjectInfo actObjInfo in objInfo.Childs)
                {
                    FillVertexStructure(structure, acMaterials, actObjInfo, transformStack);
                }
            }
            finally
            {
                transformStack.Pop();
            }
        }

        /// <summary>
        /// Loads a ac file from the given uri
        /// </summary>
        private static ACFileInfo LoadFile(Stream inStream)
        {
            ACFileInfo result = null;

            StreamReader reader = null;
            try
            {
                reader = new StreamReader(inStream);

                //Check for correct header
                string header = reader.ReadLine();
                if (!header.StartsWith("AC3D")) { throw new SeeingSharpGraphicsException("Header of AC3D file not found!"); }

                //Create file information object
                result = new ACFileInfo();

                //Create a loaded objects stack
                Stack<ACObjectInfo> loadedObjects = new Stack<ACObjectInfo>();
                Stack<ACObjectInfo> parentObjects = new Stack<ACObjectInfo>();
                ACSurface currentSurface = null;

                //Read the file
                while (!reader.EndOfStream)
                {
                    string actLine = reader.ReadLine().Trim();

                    string firstWord = string.Empty;
                    int spaceIndex = actLine.IndexOf(' ');
                    if (spaceIndex == -1) { firstWord = actLine; }
                    else { firstWord = firstWord = actLine.Substring(0, spaceIndex); }

                    switch (firstWord)
                    {
                        //New Material info
                        case "MATERIAL":
                            ACMaterialInfo materialInfo = new ACMaterialInfo();
                            {
                                //Get the name of the material
                                string[] materialData = actLine.Split(' ');
                                if (materialData.Length > 1) { materialInfo.Name = materialData[1].Trim(' ', '"'); }

                                //Parse components
                                for (int loop = 0; loop < materialData.Length; loop++)
                                {
                                    switch (materialData[loop])
                                    {
                                        case "rgb":
                                            Color4 diffuseColor = materialInfo.Diffuse;
                                            diffuseColor.Alpha = 1f;
                                            diffuseColor.Red = Single.Parse(materialData[loop + 1], CultureInfo.InvariantCulture);
                                            diffuseColor.Green = Single.Parse(materialData[loop + 2], CultureInfo.InvariantCulture);
                                            diffuseColor.Blue = Single.Parse(materialData[loop + 3], CultureInfo.InvariantCulture);
                                            materialInfo.Diffuse = diffuseColor;
                                            break;

                                        case "amb":
                                            Color4 ambientColor = new Color4();
                                            ambientColor.Red = Single.Parse(materialData[loop + 1], CultureInfo.InvariantCulture);
                                            ambientColor.Green = Single.Parse(materialData[loop + 2], CultureInfo.InvariantCulture);
                                            ambientColor.Blue = Single.Parse(materialData[loop + 3], CultureInfo.InvariantCulture);
                                            materialInfo.Ambient = ambientColor;
                                            break;

                                        case "emis":
                                            Color4 emissiveColor = new Color4();
                                            emissiveColor.Red = Single.Parse(materialData[loop + 1], CultureInfo.InvariantCulture);
                                            emissiveColor.Green = Single.Parse(materialData[loop + 2], CultureInfo.InvariantCulture);
                                            emissiveColor.Blue = Single.Parse(materialData[loop + 3], CultureInfo.InvariantCulture);
                                            materialInfo.Emissive = emissiveColor;
                                            break;

                                        case "spec":
                                            Color4 specularColor = new Color4();
                                            specularColor.Red = Single.Parse(materialData[loop + 1], CultureInfo.InvariantCulture);
                                            specularColor.Green = Single.Parse(materialData[loop + 2], CultureInfo.InvariantCulture);
                                            specularColor.Blue = Single.Parse(materialData[loop + 3], CultureInfo.InvariantCulture);
                                            materialInfo.Specular = specularColor;
                                            break;

                                        case "shi":
                                            materialInfo.Shininess = Single.Parse(materialData[loop + 1], CultureInfo.InvariantCulture);
                                            break;

                                        case "trans":
                                            diffuseColor = materialInfo.Diffuse;
                                            diffuseColor.Alpha = 1f - EngineMath.Clamp(Single.Parse(materialData[loop + 1], CultureInfo.InvariantCulture), 0f, 1f);
                                            materialInfo.Diffuse = diffuseColor;
                                            break;
                                    }
                                }
                                result.Materials.Add(materialInfo);
                            }
                            break;

                        //New object starts here
                        case "OBJECT":
                            {
                                ACObjectInfo newObject = new ACObjectInfo();

                                string[] lineData = actLine.Split(' ');
                                if (lineData[1] == "poly") { newObject.Type = ACObjectType.Poly; }
                                else if (lineData[1] == "group") { newObject.Type = ACObjectType.Group; }
                                else if (lineData[1] == "world") { newObject.Type = ACObjectType.World; }

                                loadedObjects.Push(newObject);
                            }
                            break;

                        //End of an object, kids following
                        case "kids":
                            if (loadedObjects.Count == 0) { break; }
                            {
                                //Parse kid count
                                int kidCount = 0;
                                string[] lineData = actLine.Split(' ');
                                if ((lineData != null) && (lineData.Length >= 1))
                                {
                                    Int32.TryParse(lineData[1], out kidCount);
                                }

                                ACObjectInfo currentObject = loadedObjects.Peek();
                                if (currentObject != null)
                                {
                                    //Add object to parent object, if any related
                                    bool addedToParent = false;
                                    if (parentObjects.Count > 0)
                                    {
                                        ACObjectInfo currentParent = parentObjects.Peek();
                                        if (currentParent.Childs.Count < currentParent.KidCount)
                                        {
                                            currentParent.Childs.Add(currentObject);
                                            addedToParent = true;
                                        }
                                        else
                                        {
                                            while (parentObjects.Count > 0)
                                            {
                                                parentObjects.Pop();
                                                if (parentObjects.Count == 0) { break; }

                                                currentParent = parentObjects.Peek();
                                                if (currentParent == null) { break; }
                                                if (currentParent.Childs.Count < currentParent.KidCount) { break; }
                                            }
                                            if ((currentParent != null) &&
                                                (currentParent.Childs.Count < currentParent.KidCount))
                                            {
                                                currentParent.Childs.Add(currentObject);
                                                addedToParent = true;
                                            }
                                        }
                                    }

                                    //Enable this object as parent object
                                    currentObject.KidCount = kidCount;
                                    if (currentObject.KidCount > 0) { parentObjects.Push(currentObject); }

                                    //Add to scene root if this object has no parent
                                    loadedObjects.Pop();
                                    if (!addedToParent)
                                    {
                                        if (loadedObjects.Count == 0)
                                        {
                                            result.Objects.Add(currentObject);
                                        }
                                        else
                                        {
                                            loadedObjects.Peek().Childs.Add(currentObject);
                                        }
                                    }
                                    currentObject = null;
                                }
                            }
                            break;

                        //Current object's name
                        case "name":
                            if (loadedObjects.Count == 0) { break; }
                            {
                                ACObjectInfo currentObject = loadedObjects.Peek();
                                if (currentObject != null)
                                {
                                    currentObject.Name = actLine.Replace("name ", "").Replace("\"", "");
                                }
                            }
                            break;

                        case "data":
                            break;

                        case "texture":
                            if (loadedObjects.Count == 0) { break; }
                            {
                                ACObjectInfo currentObject = loadedObjects.Peek();
                                if (currentObject != null)
                                {
                                    string[] lineData = actLine.Split(' ');
                                    currentObject.Texture = lineData[1].Trim('"');
                                }
                            }
                            break;

                        case "texrep":
                            if (loadedObjects.Count == 0) { break; }
                            {
                                ACObjectInfo currentObject = loadedObjects.Peek();
                                if (currentObject != null)
                                {
                                    string[] lineData = actLine.Split(' ');

                                    Vector2 repetition = new Vector2();
                                    repetition.X = Single.Parse(lineData[1], CultureInfo.InvariantCulture);
                                    repetition.Y = Single.Parse(lineData[2], CultureInfo.InvariantCulture);

                                    currentObject.TextureRepeat = repetition;
                                }
                            }
                            break;

                        case "texoff":
                            if (loadedObjects.Count == 0) { break; }
                            {
                                ACObjectInfo currentObject = loadedObjects.Peek();
                                if (currentObject != null)
                                {
                                    string[] lineData = actLine.Split(' ');

                                    Vector2 offset = new Vector2();
                                    offset.X = Single.Parse(lineData[1], CultureInfo.InvariantCulture);
                                    offset.Y = Single.Parse(lineData[2], CultureInfo.InvariantCulture);

                                    currentObject.TextureRepeat = offset;
                                }
                            }
                            break;

                        case "rot":
                            if (loadedObjects.Count == 0) { break; }
                            {
                                ACObjectInfo currentObject = loadedObjects.Peek();
                                if (currentObject != null)
                                {
                                    string[] lineData = actLine.Split(' ');

                                    Matrix4x4 rotation = Matrix4x4.Identity;
                                    rotation.M11 = !string.IsNullOrEmpty(lineData[1]) ? Single.Parse(lineData[1], CultureInfo.InvariantCulture) : 0f;
                                    rotation.M12 = !string.IsNullOrEmpty(lineData[2]) ? Single.Parse(lineData[2], CultureInfo.InvariantCulture) : 0f;
                                    rotation.M13 = !string.IsNullOrEmpty(lineData[3]) ? Single.Parse(lineData[3], CultureInfo.InvariantCulture) : 0f;
                                    rotation.M21 = !string.IsNullOrEmpty(lineData[4]) ? Single.Parse(lineData[4], CultureInfo.InvariantCulture) : 0f;
                                    rotation.M22 = !string.IsNullOrEmpty(lineData[5]) ? Single.Parse(lineData[5], CultureInfo.InvariantCulture) : 0f;
                                    rotation.M23 = !string.IsNullOrEmpty(lineData[6]) ? Single.Parse(lineData[6], CultureInfo.InvariantCulture) : 0f;
                                    rotation.M31 = !string.IsNullOrEmpty(lineData[7]) ? Single.Parse(lineData[7], CultureInfo.InvariantCulture) : 0f;
                                    rotation.M32 = !string.IsNullOrEmpty(lineData[8]) ? Single.Parse(lineData[8], CultureInfo.InvariantCulture) : 0f;
                                    rotation.M33 = !string.IsNullOrEmpty(lineData[9]) ? Single.Parse(lineData[9], CultureInfo.InvariantCulture) : 0f;

                                    currentObject.Rotation = rotation;
                                }
                            }
                            break;

                        case "url":
                            if (loadedObjects.Count == 0) { break; }
                            {
                                ACObjectInfo currentObject = loadedObjects.Peek();
                                if (currentObject != null)
                                {
                                    string[] lineData = actLine.Split(' ');
                                    currentObject.Url = lineData[1].Trim('"');
                                }
                            }
                            break;

                        //Current object's location
                        case "loc":
                            if (loadedObjects.Count == 0) { break; }
                            {
                                ACObjectInfo currentObject = loadedObjects.Peek();
                                if (currentObject != null)
                                {
                                    string[] lineData = actLine.Split(' ');

                                    Vector3 location = new Vector3();
                                    location.X = Single.Parse(lineData[1], CultureInfo.InvariantCulture);
                                    location.Y = Single.Parse(lineData[2], CultureInfo.InvariantCulture);
                                    location.Z = Single.Parse(lineData[3], CultureInfo.InvariantCulture);

                                    currentObject.Translation = location;
                                }
                            }
                            break;

                        case "numvert":
                            if (loadedObjects.Count == 0) { break; }
                            {
                                ACObjectInfo currentObject = loadedObjects.Peek();
                                if (currentObject != null)
                                {
                                    string[] lineData = actLine.Split(' ');
                                    int numberOfVertices = Int32.Parse(lineData[1], CultureInfo.InvariantCulture);
                                    for (int loop = 0; loop < numberOfVertices; loop++)
                                    {
                                        string actInnerLine = reader.ReadLine().Trim();
                                        string[] splittedVertex = actInnerLine.Split(' ');

                                        Vector3 position = new Vector3();
                                        position.X = Single.Parse(splittedVertex[0], CultureInfo.InvariantCulture);
                                        position.Y = Single.Parse(splittedVertex[1], CultureInfo.InvariantCulture);
                                        position.Z = Single.Parse(splittedVertex[2], CultureInfo.InvariantCulture);

                                        currentObject.Vertices.Add(new ACVertex() { Position = position });
                                    }
                                }
                            }
                            break;

                        //Start of a list of surfaces
                        case "numsurf":
                            break;

                        //New surface starts here
                        case "SURF":
                            {
                                if (currentSurface == null) { currentSurface = new ACSurface(); }

                                string[] lineData = actLine.Split(' ');
                                lineData[1] = lineData[1].Substring(2);
                                currentSurface.Flags = Int32.Parse(lineData[1], NumberStyles.HexNumber);
                            }
                            break;

                        //Current surface's material
                        case "mat":
                            {
                                if (currentSurface == null) { currentSurface = new ACSurface(); }

                                string[] lineData = actLine.Split(' ');
                                currentSurface.Material = Int32.Parse(lineData[1], CultureInfo.InvariantCulture);
                            }
                            break;

                        //Current surface's indices
                        case "refs":
                            if (loadedObjects.Count == 0) { break; }
                            {
                                if (currentSurface == null) { currentSurface = new ACSurface(); }

                                string[] lineData = actLine.Split(' ');
                                int numberOfRefs = Int32.Parse(lineData[1], CultureInfo.InvariantCulture);
                                for (int loop = 0; loop < numberOfRefs; loop++)
                                {
                                    string actInnerLine = reader.ReadLine().Trim();
                                    string[] splittedRef = actInnerLine.Split(' ');

                                    Vector2 texCoord = new Vector2();
                                    int vertexReference = UInt16.Parse(splittedRef[0], CultureInfo.InvariantCulture);
                                    texCoord.X = Single.Parse(splittedRef[1], CultureInfo.InvariantCulture);
                                    texCoord.Y = Single.Parse(splittedRef[2], CultureInfo.InvariantCulture);

                                    currentSurface.TextureCoordinates.Add(texCoord);
                                    currentSurface.VertexReferences.Add(vertexReference);
                                }

                                ACObjectInfo currentObject = loadedObjects.Peek();
                                if (currentObject != null)
                                {
                                    currentObject.Surfaces.Add(currentSurface);
                                }
                                currentSurface = null;
                            }
                            break;

                        default:
                            break;
                    }
                }
            }
            finally
            {
                if (reader != null) { reader.Dispose(); }
            }

            return result;
        }

        /// <summary>
        /// Gets the default extension (e. g. ".ac").
        /// </summary>
        public static string DefaultExtension
        {
            get { return ".ac"; }
        }

        //*********************************************************************
        //*********************************************************************
        //*********************************************************************
        private enum ACObjectType
        {
            World,
            Poly,
            Group
        }

        //*********************************************************************
        //*********************************************************************
        //*********************************************************************
        private class ACFileInfo
        {
            public List<ACMaterialInfo> Materials;
            public List<ACObjectInfo> Objects;

            public ACFileInfo()
            {
                this.Materials = new List<ACMaterialInfo>();
                this.Objects = new List<ACObjectInfo>();
            }

            /// <summary>
            /// Counts all objects within this file
            /// </summary>
            public int CountAllObjects()
            {
                int result = 0;

                foreach (ACObjectInfo actObj in Objects)
                {
                    result++;
                    result += actObj.CountAllChildObjects();
                }

                return result;
            }
        }

        //*********************************************************************
        //*********************************************************************
        //*********************************************************************
        private class ACMaterialInfo
        {
            public string Name;
            public Color4 Diffuse;
            public Color4 Ambient;
            public Color4 Emissive;
            public Color4 Specular;
            public float Shininess;

            public MaterialProperties CreateMaterialProperties()
            {
                MaterialProperties result = new MaterialProperties();
                result.DiffuseColor = Diffuse;
                result.AmbientColor = Ambient;
                result.EmissiveColor = Emissive;
                result.SpecularColor = Specular;
                result.Shininess = Shininess;
                return result;
            }
        }

        //*********************************************************************
        //*********************************************************************
        //*********************************************************************
        private class ACObjectInfo
        {
            public List<ACObjectInfo> Childs;
            public List<ACSurface> Surfaces;
            public List<ACVertex> Vertices;
            public string Texture;
            public Vector2 TextureRepeat;
            public Vector3 Translation;
            public Matrix4x4 Rotation;
            public string Name;
            public string Url;
            public ACObjectType Type;
            public int KidCount;

            /// <summary>
            /// Initializes a new instance of the <see cref="ACObjectInfo"/> class.
            /// </summary>
            public ACObjectInfo()
            {
                this.Childs = new List<ACObjectInfo>();
                this.Surfaces = new List<ACSurface>();
                this.Vertices = new List<ACVertex>();
                this.Rotation = Matrix4x4.Identity;
            }

            /// <summary>
            /// Returns a <see cref="System.String"/> that represents this instance.
            /// </summary>
            /// <returns>
            /// A <see cref="System.String"/> that represents this instance.
            /// </returns>
            public override string ToString()
            {
                return this.Name;
            }

            /// <summary>
            /// Gets total count of all child ojects
            /// </summary>
            public int CountAllChildObjects()
            {
                int result = 0;

                foreach (ACObjectInfo actObj in Childs)
                {
                    result += actObj.CountAllChildObjects();
                }

                return result;
            }
        }

        //*********************************************************************
        //*********************************************************************
        //*********************************************************************
        private class ACSurface
        {
            public List<int> VertexReferences;
            public List<Vector2> TextureCoordinates;
            public int Flags;
            public int Material;

            /// <summary>
            /// Initializes a new instance of the <see cref="ACSurface"/> class.
            /// </summary>
            public ACSurface()
            {
                VertexReferences = new List<int>();
                TextureCoordinates = new List<Vector2>();
            }

            /// <summary>
            /// Is this surface built using polygons?
            /// </summary>
            public bool IsPolygon
            {
                get { return (Flags & 0xF0) == 0; }
            }

            /// <summary>
            /// Is this surface a closed line?
            /// </summary>
            public bool IsClosedLine
            {
                get { return (Flags & 0xF0) == 1; }
            }

            /// <summary>
            /// Is this surface a line?
            /// </summary>
            public bool IsLine
            {
                get { return (Flags & 0xF0) == 2; }
            }

            /// <summary>
            /// Is this surface flat shaded?
            /// </summary>
            public bool IsFlatShaded
            {
                get { return (Flags & 16) != 16; }
            }

            /// <summary>
            /// Is this surface two sided?
            /// </summary>
            public bool IsTwoSided
            {
                get { return (Flags & 32) == 32; }
            }
        }

        //*********************************************************************
        //*********************************************************************
        //*********************************************************************
        private class ACVertex
        {
            public Vector3 Position;
        }
    }
}