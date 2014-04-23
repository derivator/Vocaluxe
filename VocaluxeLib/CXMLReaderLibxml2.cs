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
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace VocaluxeLib.Libxml2
{
    /// <summary>
    /// libxml2 implementation of CXMLReader, because XPathNavigator is slow in Mono.
    /// </summary>
    public class CXMLReaderLibxml2 : CXMLReader
    {
        #region **** libxml2 binding ****
        //don't warn about unused fields
        #pragma warning disable 169
        private const string _Libxml2Dll = "libxml2.so";

        private static readonly Object _MutexLibxml = new Object();
		
        protected enum ExmlElementType {
            XML_ELEMENT_NODE = 1,
            XML_ATTRIBUTE_NODE = 2,
            XML_TEXT_NODE = 3,
            XML_CDATA_SECTION_NODE = 4,
            XML_ENTITY_REF_NODE = 5,
            XML_ENTITY_NODE = 6,
            XML_PI_NODE = 7,
            XML_COMMENT_NODE = 8,
            XML_DOCUMENT_NODE = 9,
            XML_DOCUMENT_TYPE_NODE = 10,
            XML_DOCUMENT_FRAG_NODE = 11,
            XML_NOTATION_NODE = 12,
            XML_HTML_DOCUMENT_NODE = 13,
            XML_DTD_NODE = 14,
            XML_ELEMENT_DECL = 15,
            XML_ATTRIBUTE_DECL = 16,
            XML_ENTITY_DECL = 17,
            XML_NAMESPACE_DECL = 18,
            XML_XINCLUDE_START = 19,
            XML_XINCLUDE_END = 20,
            XML_DOCB_DOCUMENT_NODE = 21
        }

        protected enum ExmlXPathObjectType {
            XPATH_UNDEFINED = 0,
            XPATH_NODESET = 1,
            XPATH_BOOLEAN = 2,
            XPATH_NUMBER = 3,
            XPATH_STRING = 4,
            XPATH_POINT = 5,
            XPATH_RANGE = 6,
            XPATH_LOCATIONSET = 7,
            XPATH_USERS = 8,
            XPATH_XSLT_TREE = 9 // An XSLT value tree, non modifiable
        }

		[StructLayout(LayoutKind.Sequential)]
        protected struct SxmlDoc {
			IntPtr	_private;	// application data
            ExmlElementType	type;	// XML_DOCUMENT_NODE, must be second !
			[MarshalAs(UnmanagedType.LPStr)] string	name;	// name/filename/URI of the document
			IntPtr children;	// the document tree
			IntPtr last;	// last child link
			IntPtr parent;	// child->parent link
			IntPtr next;	// next sibling link
			IntPtr prev;	// previous sibling link
			IntPtr doc;	// autoreference to itself End of common p
			int	compression;	// level of zlib compression
			int	standalone;	// standalone document (no external refs)
			IntPtr intSubset;	// the document internal subset
			IntPtr extSubset;	// the document external subset
			IntPtr oldNs;	// Global namespace, the old way
			[MarshalAs(UnmanagedType.LPStr)] public string	version;	// the XML version string
			[MarshalAs(UnmanagedType.LPStr)] public string	encoding;	// external initial encoding, if any
			IntPtr ids;	// Hash table for ID attributes if any
			IntPtr refs;	// Hash table for IDREFs attributes if any
			[MarshalAs(UnmanagedType.LPStr)] public string	URL;	// The URI for that document
			int	charset;	// encoding of the in-memory content actua
			IntPtr dict;	// dict used to allocate names or NULL
			IntPtr psvi;	// for type/PSVI informations
			int	parseFlags;	// set of xmlParserOption used to parse th
			int	properties;	// set of xmlDocProperties for this docume

            public static SxmlDoc FromPointer(IntPtr ptr){
                return (SxmlDoc)Marshal.PtrToStructure(ptr,typeof(SxmlDoc));
            }

            public bool HasNext(){
                return next != IntPtr.Zero;
            }

            public bool GetNext(ref SxmlNode output){
                if (next != IntPtr.Zero) {
                    output = SxmlNode.FromPointer(next);
                    return true;
                }
                return false;
            }

            public bool GetFirstChild(ref SxmlNode output){
                if (children != IntPtr.Zero) {
                    output = SxmlNode.FromPointer(children);
                    return true;
                }
                return false;
            }
		}

		[StructLayout(LayoutKind.Sequential)]
        protected struct SxmlXPathObject {
            public ExmlXPathObjectType type;
            IntPtr nodesetval;
			int	boolval;
			double	floatval;
			[MarshalAs(UnmanagedType.LPStr)] public string stringval;
			IntPtr	user;
			int	index;
			IntPtr	user2;
			int	index2;

            public static SxmlXPathObject FromPointer(IntPtr ptr){
                return (SxmlXPathObject)Marshal.PtrToStructure(ptr,typeof(SxmlXPathObject));
            }

            public SxmlNodeSet AsNodeSet(){
                return (SxmlNodeSet)Marshal.PtrToStructure(nodesetval,typeof(SxmlNodeSet));
            }
		}

		[StructLayout(LayoutKind.Sequential)]
        protected struct SxmlNodeSet {
			public int	nodeNr; //number of nodes in the set
			int	nodeMax; //size of the array as allocated
			IntPtr nodeTab; //array of nodes in no particular order @

			public SxmlNode ByIndex(int index){
				IntPtr p = (IntPtr)Marshal.PtrToStructure(nodeTab+Marshal.SizeOf(typeof(IntPtr))*index,typeof(IntPtr));
				return (SxmlNode)Marshal.PtrToStructure(p,typeof(SxmlNode));
			}
		}

		[StructLayout(LayoutKind.Sequential)]
        protected struct SxmlNode {
			IntPtr	_private; //application data
            public ExmlElementType type; //type number, must be second !
			[MarshalAs(UnmanagedType.LPStr)] public string	name; //the name of the node, or the entity
			public IntPtr children; //parent->childs link
            public IntPtr last; //last child link
		    IntPtr parent; //child->parent link
            public IntPtr next; //next sibling link
		    IntPtr prev; //previous sibling link
			IntPtr	doc; //the containing document End of common p
			IntPtr ns; //pointer to the associated namespace
			[MarshalAs(UnmanagedType.LPStr)] public string	content; //the content
			IntPtr	properties; //properties list
			IntPtr nsDef; //namespace definitions on this node
			IntPtr	psvi; //for type/PSVI informations
			ushort line; //line number
			ushort	extra; //extra data for XPath/XSLT
            #pragma warning restore 169

            public static SxmlNode FromPointer(IntPtr ptr){
                return (SxmlNode)Marshal.PtrToStructure(ptr,typeof(SxmlNode));
            }

            public bool GetFirstChild(ref SxmlNode output){
                if (children != IntPtr.Zero) {
                    output = SxmlNode.FromPointer(children);
                    return true;
                }
                return false;
            }

            public bool GetNext(ref SxmlNode output){
                if (next != IntPtr.Zero) {
                    output = SxmlNode.FromPointer(next);
                    return true;
                }
                return false;
            }

            public string GetAttribute(string attribute){
                SxmlNode current = new SxmlNode();
                if (GetFirstChild(ref current)) {
                    do {
                        if(current.type==ExmlElementType.XML_ATTRIBUTE_NODE && current.name == attribute)
                            return current.content;
                    } while(current.GetNext(ref current));
                }
                return String.Empty;
            }
		}

		[DllImport(_Libxml2Dll, CallingConvention = CallingConvention.Cdecl)]
		private static extern void xmlInitParser();

		[DllImport(_Libxml2Dll, CallingConvention = CallingConvention.Cdecl)]
		private static extern void xmlCleanupParser();

		[DllImport(_Libxml2Dll, CallingConvention = CallingConvention.Cdecl)]
		private static extern IntPtr xmlParseFile(string filename);

		[DllImport(_Libxml2Dll, CallingConvention = CallingConvention.Cdecl)]
		private static extern void xmlFreeDoc(IntPtr document);

		[DllImport(_Libxml2Dll, CallingConvention = CallingConvention.Cdecl)]
		private static extern IntPtr xmlXPathNewContext(IntPtr document);

		[DllImport(_Libxml2Dll, CallingConvention = CallingConvention.Cdecl)]
		private static extern void xmlXPathFreeContext(IntPtr context);

		[DllImport(_Libxml2Dll, CallingConvention = CallingConvention.Cdecl)]
		private static extern IntPtr xmlXPathEvalExpression(string expression, IntPtr context);

		[DllImport(_Libxml2Dll, CallingConvention = CallingConvention.Cdecl)]
		private static extern void xmlXPathFreeObject(IntPtr obj);

        [DllImport(_Libxml2Dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr xmlGetProp( IntPtr node, string attribute);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void xmlFreeDelegate(IntPtr ptr);

        [DllImport(_Libxml2Dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern int xmlMemGet(ref IntPtr freeFunc, ref IntPtr mallocFunc, ref IntPtr reallocFunc, ref IntPtr strdupFunc);

        private static xmlFreeDelegate xmlFree;
        #endregion

        private static bool _ParserInitialized = false;
        private IntPtr pDocument;
		private IntPtr pContext;


		private CXMLReaderLibxml2(string fileName) : base(fileName)
		{
            //libxml2 should be thread-safe, as long as two threads aren't working on the same document
            //xmlInitParser() has to be called once before the parser can be used
            lock (_MutexLibxml ) { 
                if (!_ParserInitialized){
					xmlInitParser();
                    //xmlFree() doesn't survive the switch to C#, so we have to get it manually
                    IntPtr xmlFreePtr = IntPtr.Zero, b = IntPtr.Zero, c= IntPtr.Zero, d = IntPtr.Zero;
                    int success = xmlMemGet(ref xmlFreePtr, ref b, ref c, ref d);
                    if(success != 0)
                        throw new Exception("libxml2 initialization failed.");

                    xmlFree = (xmlFreeDelegate) Marshal.GetDelegateForFunctionPointer(xmlFreePtr, typeof(xmlFreeDelegate));
					_ParserInitialized = true;
				}
			}
			pDocument = xmlParseFile(fileName);
			pContext = xmlXPathNewContext(pDocument);
		}

		~CXMLReaderLibxml2()
		{
			xmlXPathFreeContext(pContext); 
			xmlFreeDoc(pDocument);
			
		}
        public static void CleanupParser(){
            lock (_MutexLibxml) {
                if (_ParserInitialized) {
                    xmlCleanupParser();
                    _ParserInitialized = false;
                }
            }
        }
        public static new CXMLReaderLibxml2 OpenFile(string fileName)
		{
			return new CXMLReaderLibxml2(fileName);
		}

		public override bool GetValue(string cast, out string value, string defaultValue)
		{
			IntPtr xpathObj = xmlXPathEvalExpression(cast, pContext);
            if (xpathObj != IntPtr.Zero) 
            {
                SxmlXPathObject o = SxmlXPathObject.FromPointer(xpathObj);
                if (o.type != ExmlXPathObjectType.XPATH_NODESET) {
					value = defaultValue;
					return false;
				}

                SxmlNodeSet nodes = o.AsNodeSet();
				if (nodes.nodeNr != 1) {
					value = defaultValue;
					return false;
				}

				SxmlNode resultNode = nodes.ByIndex(0);
                if (resultNode.GetFirstChild(ref resultNode)) {
                    value = resultNode.content;
                } else {
                    value = defaultValue;
                }

				xmlXPathFreeObject(xpathObj);
				return true;
			}
			value = defaultValue;
			return false;
		}

        public override List<string> GetValues(string cast)
        {
            var values = new List<string>();

            SxmlDoc doc = SxmlDoc.FromPointer(pDocument);
            SxmlNode current = new SxmlNode();

            if (!(doc.GetFirstChild(ref current) && current.GetFirstChild(ref current)))
                return values;

            do {
                if (current.name == cast) {
                    if (current.GetFirstChild(ref current)) {
                        do {
                            if(current.type==ExmlElementType.XML_ELEMENT_NODE)
                                values.Add(current.name);
                        } while(current.GetNext(ref current));
                    }
                    break;
                }
            } while(current.GetNext(ref current));

            return values;
        }

        public override IEnumerable<string> GetAttributes(string cast, string attribute)
        {
            var values = new List<string>();

            SxmlDoc doc = SxmlDoc.FromPointer(pDocument);
            SxmlNode current = new SxmlNode();

            if (!doc.GetFirstChild(ref current))
                return values;

            do {
                if (current.name == cast) {
                    IntPtr pChild = current.children;
                    while(pChild != IntPtr.Zero) {
                        IntPtr prop = xmlGetProp(pChild,attribute);
                        if(prop!=IntPtr.Zero){
                            values.Add(Marshal.PtrToStringAuto(prop));

                            xmlFree(prop);
                        }
                        pChild = SxmlNode.FromPointer(pChild).next;
                    }
                    return values;
                }
            } while(current.GetNext(ref current));

            return values;
        }

        public override bool GetInnerValues(string cast, ref List<string> values)
        {
            SxmlDoc doc = SxmlDoc.FromPointer(pDocument);
            SxmlNode current = new SxmlNode();
            SxmlNode content = new SxmlNode();

            if (!(doc.GetFirstChild(ref current) && current.GetFirstChild(ref current)))
                return false;

            do {
                if (current.name == cast) {
                    if (current.GetFirstChild(ref current)) {
                        do {
                            if(current.type==ExmlElementType.XML_ELEMENT_NODE && current.GetFirstChild(ref content))
                                values.Add(content.content);

                        } while(current.GetNext(ref current));
                    }
                    return true;
                }
            } while(current.GetNext(ref current));
                
            return true;
        }
            
    }
	
}

