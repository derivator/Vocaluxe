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
using System.Xml.XPath;
using System.Collections.Generic;

namespace VocaluxeLib
{
    /// <summary>
    /// .NET implementation of CXMLReader
    /// </summary>
    public class CXMLReaderDotNET : CXMLReader
    {
        private readonly XPathNavigator _Navigator;

        public XPathNavigator Navigator
        {
            get { return _Navigator; }
        }

        private CXMLReaderDotNET (string uri) : base(uri)
        {
            var xmlDoc = new XPathDocument(uri);
            _Navigator = xmlDoc.CreateNavigator();
        }

        public static new CXMLReaderDotNET OpenFile(string fileName)
        {
            return new CXMLReaderDotNET(fileName);           
        }

        public override bool GetValue(string cast, out string value, string defaultValue)
        {
            if(!cast.StartsWith("/")) //Allow only absolute paths
                throw new ArgumentException();

            int resultCt = 0;
            string val = string.Empty;

            XPathNodeIterator iterator = _Navigator.Select(cast);


            while (iterator.MoveNext())
            {
                val = iterator.Current.Value;
                resultCt++;
            }

            if (resultCt != 1)
            {
                value = defaultValue;
                return false;
            }
            value = val;
            return true;
        }

        public override List<string> GetValues(string cast)
        {
            var values = new List<string>();

            _Navigator.MoveToRoot();
            _Navigator.MoveToFirstChild();
            _Navigator.MoveToFirstChild();

            while (_Navigator.Name != cast)
                _Navigator.MoveToNext();

            _Navigator.MoveToFirstChild();

            values.Add(_Navigator.LocalName);
            while (_Navigator.MoveToNext())
                values.Add(_Navigator.LocalName);

            return values;
        }

        public override IEnumerable<string> GetAttributes(string cast, string attribute)
        {
            var values = new List<string>();

            _Navigator.MoveToRoot();
            _Navigator.MoveToFirstChild();

            while (_Navigator.Name != cast)
                _Navigator.MoveToNext();

            _Navigator.MoveToFirstChild();

            values.Add(_Navigator.GetAttribute(attribute, ""));
            while (_Navigator.MoveToNext())
                values.Add(_Navigator.GetAttribute(attribute, ""));

            return values;
        }

        public override bool GetInnerValues(string cast, ref List<string> values)
        {
            _Navigator.MoveToRoot();
            _Navigator.MoveToFirstChild();
            _Navigator.MoveToFirstChild();

            while (_Navigator.Name != cast)
                _Navigator.MoveToNext();

            _Navigator.MoveToFirstChild();

            values.Add(_Navigator.Value);
            while (_Navigator.MoveToNext())
                values.Add(_Navigator.Value);

            return true;
        }


    }
}

