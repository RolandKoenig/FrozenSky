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
using System.Threading.Tasks;

namespace SeeingSharp.Util.TableData
{
    public class CsvTableHeaderRow : ITableHeaderRow
    {
        private CsvTableFile m_parentFile;
        private Dictionary<string, int> m_columnIndices;
        private string[] m_headers;

        internal CsvTableHeaderRow(CsvTableFile parentFile, string rowString)
        {
            m_parentFile = parentFile;
            m_columnIndices = new Dictionary<string, int>();

            m_headers = rowString.Split(parentFile.ImporterConfig.SeparationChar);
            for (int loop = 0; loop < m_headers.Length; loop++)
            {
                if (m_headers[loop] != null)
                {
                    m_columnIndices[m_headers[loop]] = loop;
                }
            }
        }

        /// <summary>
        /// Gets the index for the given field name.
        /// </summary>
        /// <param name="fieldName">The name of the field.</param>
        public int GetFieldIndex(string fieldName)
        {
            return m_columnIndices[fieldName];
        }

        /// <summary>
        /// Gets the name of the field with the given index.
        /// </summary>
        /// <param name="fieldIndex">The index of the field.</param>
        public string GetFieldName(int fieldIndex)
        {
            return m_headers[fieldIndex];
        }

        /// <summary>
        /// Gets the total count of fields contained in the datasource.
        /// </summary>
        public int FieldCount
        {
            get { return m_headers.Length; }
        }
    }
}
