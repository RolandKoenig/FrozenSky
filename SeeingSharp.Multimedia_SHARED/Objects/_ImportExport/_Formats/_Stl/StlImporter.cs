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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SeeingSharp.Util;
using System.IO;
using System.Numerics;
using System.Globalization;
using SeeingSharp.Multimedia.Drawing3D;
using SeeingSharp.Checking;
using SeeingSharp.Multimedia.Core;
using SeeingSharp.Infrastructure;

// Define assembly attributes for type that is defined in this file
[assembly: AssemblyQueryableType(
    targetType: typeof(SeeingSharp.Multimedia.Objects.StLImporter),
    contractType: typeof(SeeingSharp.Multimedia.Objects.IModelImporter))]

namespace SeeingSharp.Multimedia.Objects
{
    // This code is based on HelixToolkit (MIT license)
    // https://github.com/helix-toolkit/helix-toolkit/blob/master/Source/HelixToolkit.Wpf/Importers/StLReader.cs

    /// <summary>
    /// Provides an importer for StereoLithography .StL files.
    /// </summary>
    /// <remarks>
    /// The format is documented on <a href="http://en.wikipedia.org/wiki/STL_(file_format)">Wikipedia</a>.
    /// </remarks>
    [SupportedFileFormat("stl", "StereoLithography .StL files")]
    public class StLImporter : IModelImporter
    {
        #region Constants
        private const string RES_KEY_GEO_CLASS = "Geometry";
        private const string RES_KEY_GEO_NAME = "Main";
        #endregion

        #region Static regular expressions for parsing
        private static readonly Encoding ENCODING = Encoding.GetEncoding("us-ascii");
        private static readonly Regex NORMAL_REGEX = new Regex(@"normal\s*(\S*)\s*(\S*)\s*(\S*)");
        private static readonly Regex VERTEX_REGEX = new Regex(@"vertex\s*(\S*)\s*(\S*)\s*(\S*)");
        #endregion

        #region Just for caching
        private List<Vector3> m_cachedPoints = new List<Vector3>(3);
        #endregion

        /// <summary>
        /// Imports a model from the given file.
        /// </summary>
        /// <param name="importOptions">Some configuration for the importer.</param>
        /// <param name="sourceFile">The source file to be loaded.</param>
        public ImportedModelContainer ImportModel(ResourceLink sourceFile, ImportOptions importOptions)
        {
            // Get import options
            StlImportOptions stlImportOptions = importOptions as StlImportOptions;
            if(stlImportOptions == null)
            {
                throw new SeeingSharpException("Invalid import options for StlImporter!");
            }

            ImportedModelContainer result = null;
            try
            {
                // Try to read in BINARY format first
                using (Stream inStream = sourceFile.OpenInputStream())
                {
                    result = this.TryReadBinary(inStream, stlImportOptions);
                }

                // Read in ASCII format (if binary did not work)
                if (result == null)
                {
                    using (Stream inStream = sourceFile.OpenInputStream())
                    {
                        result = this.TryReadAscii(inStream, stlImportOptions);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new SeeingSharpException($"Unable to read Stl file {sourceFile}: {ex.Message}!", ex);
            }

            // Handle empty result (unknown format error)
            if (result == null)
            {
                throw new SeeingSharpException($"Unable to read Stl file {sourceFile}: Unrecognized format error!");
            }

            return result;
        }

        /// <summary>
        /// Creates a default import options object for this importer.
        /// </summary>
        public ImportOptions CreateDefaultImportOptions()
        {
            return new StlImportOptions();
        }

        /// <summary>
        /// Parses the ID and values from the specified line.
        /// </summary>
        private static void ParseLine(string line, out string id, out string values)
        {
            line = line.Trim();
            int idx = line.IndexOf(' ');
            if (idx == -1)
            {
                id = line;
                values = string.Empty;
            }
            else
            {
                id = line.Substring(0, idx).ToLower();
                values = line.Substring(idx + 1);
            }
        }

        /// <summary>
        /// Parses a normal string.
        /// </summary>
        private static Vector3 ParseNormal(string input)
        {
            var match = NORMAL_REGEX.Match(input);
            if (!match.Success)
            {
                throw new SeeingSharpException($"Unexpected line while reading Stl file. Line content: {Environment.NewLine}{input}");
            }

            float x = float.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
            float y = float.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
            float z = float.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);

            return new Vector3(x, y, z);
        }

        /// <summary>
        /// Reads a float (4 byte)
        /// </summary>
        private static float ReadFloat(BinaryReader reader)
        {
            var bytes = reader.ReadBytes(4);
            return BitConverter.ToSingle(bytes, 0);
        }

        /// <summary>
        /// Reads a line from the stream reader.
        /// </summary>
        private static void ReadLine(StreamReader reader, string token)
        {
            token.EnsureNotNull(nameof(token));

            var line = reader.ReadLine();
            string id, values;
            ParseLine(line, out id, out values);

            if (!string.Equals(token, id, StringComparison.OrdinalIgnoreCase))
            {
                throw new SeeingSharpException($"Unexpected line (expected: {token}, got:{id})");
            }
        }

        /// <summary>
        /// Reads a 16-bit unsigned integer.
        /// </summary>
        /// <param name="reader">
        /// The reader.
        /// </param>
        /// <returns>
        /// The unsigned integer.
        /// </returns>
        private static ushort ReadUInt16(BinaryReader reader)
        {
            var bytes = reader.ReadBytes(2);
            return BitConverter.ToUInt16(bytes, 0);
        }

        /// <summary>
        /// Reads a 32-bit unsigned integer.
        /// </summary>
        /// <param name="reader">
        /// The reader.
        /// </param>
        /// <returns>
        /// The unsigned integer.
        /// </returns>
        private static uint ReadUInt32(BinaryReader reader)
        {
            var bytes = reader.ReadBytes(4);
            return BitConverter.ToUInt32(bytes, 0);
        }

        /// <summary>
        /// Tries to parse a vertex from a string.
        /// </summary>
        private static bool TryParseVertex(string line, out Vector3 point)
        {
            var match = VERTEX_REGEX.Match(line);
            if (!match.Success)
            {
                point = new Vector3();
                return false;
            }

            float x = float.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
            float y = float.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
            float z = float.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);

            point = new Vector3(x, y, z);
            return true;
        }

        /// <summary>
        /// Reads a facet.
        /// </summary>
        private void ReadFacet(StreamReader reader, string normalString, VertexStructure newStructure, StlImportOptions importOptions)
        {
            m_cachedPoints.Clear();

            // Read all geometry
            Vector3 normal = ParseNormal(normalString);
            ReadLine(reader, "outer");
            while (true)
            {
                string line = reader.ReadLine();
                Vector3 point;
                if (TryParseVertex(line, out point))
                {
                    m_cachedPoints.Add(point);
                    continue;
                }

                string id, values;
                ParseLine(line, out id, out values);

                if (id == "endloop")
                {
                    break;
                }
            }

            // Read end
            ReadLine(reader, "endfacet");

            // Overtake geometry data
            VertexStructureSurface targetSurfae = newStructure.FirstSurface;
            int pointCount = m_cachedPoints.Count;
            switch (m_cachedPoints.Count)
            {
                case 0:
                case 1:
                case 2:
                    break;

                case 3:
                    if (importOptions.IsChangeTriangleOrderNeeded())
                    {
                        targetSurfae.AddTriangle(
                            new Vertex(m_cachedPoints[2], Color4.Transparent, Vector2.Zero, normal),
                            new Vertex(m_cachedPoints[1], Color4.Transparent, Vector2.Zero, normal),
                            new Vertex(m_cachedPoints[0], Color4.Transparent, Vector2.Zero, normal));
                    }
                    else
                    {
                        targetSurfae.AddTriangle(
                            new Vertex(m_cachedPoints[0], Color4.Transparent, Vector2.Zero, normal),
                            new Vertex(m_cachedPoints[1], Color4.Transparent, Vector2.Zero, normal),
                            new Vertex(m_cachedPoints[2], Color4.Transparent, Vector2.Zero, normal));
                    }
                    break;

                default:
                    int[] indices = new int[pointCount];
                    if (importOptions.IsChangeTriangleOrderNeeded())
                    {
                        for (int loop = pointCount - 1; loop > -1; loop--)
                        {
                            indices[loop] = newStructure.AddVertex(
                                new Vertex(m_cachedPoints[loop], Color4.Transparent, Vector2.Zero, normal));
                        }
                    }
                    else
                    {
                        for (int loop = 0; loop < pointCount; loop++)
                        {
                            indices[loop] = newStructure.AddVertex(
                                new Vertex(m_cachedPoints[loop], Color4.Transparent, Vector2.Zero, normal));
                        }
                    }

                    targetSurfae.AddPolygonByCuttingEars(indices);
                    break;
            }
        }

        /// <summary>
        /// Reads a triangle from a binary STL file.
        /// </summary>
        private void ReadTriangle(BinaryReader reader, VertexStructure vertexStructure, StlImportOptions importOptions)
        {
            float ni = ReadFloat(reader);
            float nj = ReadFloat(reader);
            float nk = ReadFloat(reader);
            var normal = new Vector3(ni, nj, nk);

            float x1 = ReadFloat(reader);
            float y1 = ReadFloat(reader);
            float z1 = ReadFloat(reader);
            var v1 = new Vector3(x1, y1, z1);

            float x2 = ReadFloat(reader);
            float y2 = ReadFloat(reader);
            float z2 = ReadFloat(reader);
            var v2 = new Vector3(x2, y2, z2);

            float x3 = ReadFloat(reader);
            float y3 = ReadFloat(reader);
            float z3 = ReadFloat(reader);
            var v3 = new Vector3(x3, y3, z3);

            // Try to read color information
            var attrib = Convert.ToString(ReadUInt16(reader), 2).PadLeft(16, '0').ToCharArray();
            var hasColor = attrib[0].Equals('1');
            Color currentColor = Color.Transparent;
            if (hasColor)
            {
                int blue = attrib[15].Equals('1') ? 1 : 0;
                blue = attrib[14].Equals('1') ? blue + 2 : blue;
                blue = attrib[13].Equals('1') ? blue + 4 : blue;
                blue = attrib[12].Equals('1') ? blue + 8 : blue;
                blue = attrib[11].Equals('1') ? blue + 16 : blue;
                int b = blue * 8;

                int green = attrib[10].Equals('1') ? 1 : 0;
                green = attrib[9].Equals('1') ? green + 2 : green;
                green = attrib[8].Equals('1') ? green + 4 : green;
                green = attrib[7].Equals('1') ? green + 8 : green;
                green = attrib[6].Equals('1') ? green + 16 : green;
                int g = green * 8;

                int red = attrib[5].Equals('1') ? 1 : 0;
                red = attrib[4].Equals('1') ? red + 2 : red;
                red = attrib[3].Equals('1') ? red + 4 : red;
                red = attrib[2].Equals('1') ? red + 8 : red;
                red = attrib[1].Equals('1') ? red + 16 : red;
                int r = red * 8;

                currentColor = new Color(Convert.ToByte(r), Convert.ToByte(g), Convert.ToByte(b));
            }

            VertexStructureSurface targetSurface = vertexStructure.FirstSurface;
            if (importOptions.IsChangeTriangleOrderNeeded())
            {
                targetSurface.AddTriangle(
                    new Vertex(v3, currentColor, Vector2.Zero, normal),
                    new Vertex(v2, currentColor, Vector2.Zero, normal),
                    new Vertex(v1, currentColor, Vector2.Zero, normal));
            }
            else
            {
                targetSurface.AddTriangle(
                    new Vertex(v1, currentColor, Vector2.Zero, normal),
                    new Vertex(v2, currentColor, Vector2.Zero, normal),
                    new Vertex(v3, currentColor, Vector2.Zero, normal));
            }
        }

        /// <summary>
        /// Reads the model in ASCII format from the specified stream.
        /// </summary>
        private ImportedModelContainer TryReadAscii(Stream stream, StlImportOptions importOptions)
        {
            using (var reader = new StreamReader(stream, ENCODING, false, 128, true))
            {
                VertexStructure newStructure = new VertexStructure();
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    if (line == null)
                    {
                        continue;
                    }

                    line = line.Trim();
                    if (line.Length == 0 || line.StartsWith("\0") || line.StartsWith("#") || line.StartsWith("!")
                        || line.StartsWith("$"))
                    {
                        continue;
                    }

                    string id, values;
                    ParseLine(line, out id, out values);
                    switch (id)
                    {
                            // Header.. not needed here
                        case "solid":
                            break;

                            // Geometry data
                        case "facet":
                            this.ReadFacet(reader, values, newStructure, importOptions);
                            break;

                            // End of file
                        case "endsolid":
                            break;
                    }
                }

                // Generate result container
                ImportedModelContainer result = new ImportedModelContainer(importOptions);
                NamedOrGenericKey geoResourceKey = result.GetResourceKey(
                    RES_KEY_GEO_CLASS, RES_KEY_GEO_NAME);
                result.ImportedResources.Add(new ImportedResourceInfo(
                    geoResourceKey,
                    () => new GeometryResource(newStructure)));
                GenericObject geoObject = new GenericObject(geoResourceKey);
                result.Objects.Add(geoObject);

                // Append an object which transform the whole coordinate system
                ScenePivotObject rootObject = result.CreateAndAddRootObject();
                result.ParentChildRelationships.Add(new Tuple<SceneObject, SceneObject>(rootObject, geoObject));

                return result;
            }
        }

        /// <summary>
        /// Reads the model from the specified binary stream.
        /// </summary>
        private ImportedModelContainer TryReadBinary(Stream stream, StlImportOptions importOptions)
        {
            // Check length
            long length = stream.Length;
            if (length < 84)
            {
                throw new SeeingSharpException("Incomplete file (smaller that 84 bytes)");
            }

            // Read number of triangles
            uint numberTriangles = 0;
            using (var reader = new BinaryReader(stream, Encoding.GetEncoding("us-ascii"), true))
            {
                // Read header (is not needed)
                //  (solid stands for Ascii format)
                string header = ENCODING.GetString(reader.ReadBytes(80), 0, 80).Trim();
                if(header.StartsWith("solid", StringComparison.OrdinalIgnoreCase)) { return null; }

                // Read and check number of triangles
                numberTriangles = ReadUInt32(reader);
                if (length - 84 != numberTriangles * 50)
                {
                    throw new SeeingSharpException("Incomplete file (smaller that expected byte count)");
                }

                // Read geometry data
                VertexStructure newStructure = new VertexStructure((int)numberTriangles * 3);
                newStructure.CreateSurface((int)numberTriangles);
                for (int loop = 0; loop < numberTriangles; loop++)
                {
                    this.ReadTriangle(reader, newStructure, importOptions);
                }

                // Generate result container
                ImportedModelContainer result = new ImportedModelContainer(importOptions);
                NamedOrGenericKey geoResourceKey = result.GetResourceKey(
                    RES_KEY_GEO_CLASS, RES_KEY_GEO_NAME);
                result.ImportedResources.Add(new ImportedResourceInfo(
                    geoResourceKey,
                    () => new GeometryResource(newStructure)));
                GenericObject geoObject = new GenericObject(geoResourceKey);
                result.Objects.Add(geoObject);

                // Append an object which transform the whole coordinate system
                ScenePivotObject rootObject = result.CreateAndAddRootObject();
                result.ParentChildRelationships.Add(new Tuple<SceneObject, SceneObject>(rootObject, geoObject));

                return result;
            }
        }
    }
}