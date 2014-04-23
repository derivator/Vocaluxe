#region license
// This file is part of Vocaluxe.
// 
// Vocaluxe is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Vocaluxe is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Vocaluxe. If not, see <http://www.gnu.org/licenses/>.
#endregion

using System;
using System.Collections.Generic;
using System.Xml.XPath;


namespace VocaluxeLib
{
    public abstract class CXMLReader
    {
        private readonly String _FileName;

        public string FileName
        {
            get { return _FileName; }
        }

        //Private method. Use OpenFile factory method to get an instance
        protected CXMLReader(string uri)
        {
            _FileName = uri;
        }            

        public static CXMLReader OpenFile(string fileName)
        {
            try
            {
                #if WIN
                return CXMLReaderDotNET.OpenFile(fileName);
                #endif 

                #if LINUX
                return Libxml2.CXMLReaderLibxml2.OpenFile(fileName);
                #endif
            }
            catch (Exception e)
            {
                CBase.Log.LogError("Can't open XML file: " + fileName + ": " + e.Message);
                return null;
            }
        }

        public bool TryGetEnumValue<T>(string cast, ref T value)
            where T : struct
        {
            string val;
            if (GetValue(cast, out val, Enum.GetName(typeof(T), value)))
            {
                CHelper.TryParse(val, out value, true);
                return true;
            }
            return false;
        }

        public bool TryGetIntValue(string cast, ref int value)
        {
            string val;
            if (GetValue(cast, out val, value.ToString()))
                return int.TryParse(val, out value);
            return false;
        }

        public bool TryGetIntValueRange(string cast, ref int value, int min = 0, int max = 100)
        {
            bool result = TryGetIntValue(cast, ref value);
            if (result)
            {
                if (value < min)
                    value = min;
                else if (value > max)
                    value = max;
            }
            return result;
        }

        public bool TryGetFloatValue(string cast, ref float value)
        {
            string val;
            return GetValue(cast, out val, value.ToString()) && CHelper.TryParse(val, out value);
        }

        public abstract bool GetValue(string cast, out string value, string defaultValue);

        public abstract List<string> GetValues(string cast);

        public abstract IEnumerable<string> GetAttributes(string cast, string attribute);

        public abstract bool GetInnerValues(string cast, ref List<string> values);       

        /// <summary>
        /// Check if item exists (uniquely)
        /// </summary>
        /// <returns><c>true</c>, if exactly one item was found, <c>false</c> otherwise.</returns>
        /// <param name="cast">XPath to check</param>
        public bool ItemExists(string cast) 
        {
            string value;
            return GetValue(cast, out value, String.Empty);
        }

    }
}