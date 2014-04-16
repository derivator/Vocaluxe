using System;
using System.Runtime.InteropServices;





namespace VocaluxeLib.Libxml2
{
	public class CXMLReaderLibxml2 : CXMLReader
	{
		private const string _Libxml2Dll = "libxml2.so";

		private static readonly Object _MutexLibxml = new Object();
		/*
		[StructLayout(LayoutKind.Sequential)]
		public struct SxmlDoc {
			IntPtr	_private;	// application data
			int	type;	// XML_DOCUMENT_NODE, must be second !
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
		}*/

		[StructLayout(LayoutKind.Sequential)]
		struct SxmlXPathObject {
			public int	type;
			public IntPtr nodesetval;
			int	boolval;
			double	floatval;
			[MarshalAs(UnmanagedType.LPStr)] public string stringval;
			IntPtr	user;
			int	index;
			IntPtr	user2;
			int	index2;
		}

		[StructLayout(LayoutKind.Sequential)]
		struct SxmlNodeSet {
			public int	nodeNr; //number of nodes in the set
			int	nodeMax; //size of the array as allocated
			IntPtr nodeTab; //array of nodes in no particular order @
			public SxmlNode getNode(int index){
				IntPtr p = (IntPtr)Marshal.PtrToStructure(nodeTab+Marshal.SizeOf(typeof(IntPtr))*index,typeof(IntPtr));
				return (SxmlNode)Marshal.PtrToStructure(p,typeof(SxmlNode));
			}
		}

		[StructLayout(LayoutKind.Sequential)]
		struct SxmlNode {
			IntPtr	_private; //application data
			public int type; //type number, must be second !
			[MarshalAs(UnmanagedType.LPStr)] public string	name; //the name of the node, or the entity
			public IntPtr children; //parent->childs link
		    IntPtr last; //last child link
		    IntPtr parent; //child->parent link
		    IntPtr next; //next sibling link
		    IntPtr prev; //previous sibling link
			IntPtr	doc; //the containing document End of common p
			IntPtr ns; //pointer to the associated namespace
			[MarshalAs(UnmanagedType.LPStr)] public string	content; //the content
			IntPtr	properties; //properties list
			IntPtr nsDef; //namespace definitions on this node
			IntPtr	psvi; //for type/PSVI informations
			ushort line; //line number
			ushort	extra; //extra data for XPath/XSLT

			public SxmlNode getChild(int index){
				//if(index > nodeNr-1)
				//	return null;
				//if (children == null)
				//	return null;
				return (SxmlNode)Marshal.PtrToStructure(children+Marshal.SizeOf(typeof(SxmlNode))*index,typeof(SxmlNode));
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

		private bool parserInitialized;
		private IntPtr pDocument;
		private IntPtr pContext;

		private CXMLReaderLibxml2(string fileName) : base(fileName)
		{
			lock (_MutexLibxml ) {
				if (!parserInitialized){
					xmlInitParser ();
					parserInitialized = true;
				}
			}
			pDocument = xmlParseFile(fileName);
			pContext = xmlXPathNewContext(pDocument);
		}

		~CXMLReaderLibxml2()
		{
			xmlXPathFreeContext(pContext); 
			xmlFreeDoc(pDocument); 
			lock (_MutexLibxml) {
				if (parserInitialized) {
					xmlCleanupParser();
					parserInitialized = false;
				}
			}
		}

		public static new CXMLReaderLibxml2 OpenFile(string fileName)
		{
			try
			{
				return new CXMLReaderLibxml2(fileName);
			}
			catch (Exception e)
			{
				CBase.Log.LogError("Can't open XML file: " + fileName + ": " + e.Message);
				return null;
			}
		}

		public override bool GetValue(string cast, out string value, string defaultValue)
		{
			IntPtr xpathObj = xmlXPathEvalExpression(cast, pContext);
			if (xpathObj != IntPtr.Zero) {
				SxmlXPathObject o = (SxmlXPathObject)Marshal.PtrToStructure(xpathObj, typeof(SxmlXPathObject));
				if (o.type != 1) {//TODO: nodeset magic number
					value = defaultValue;
					return false;
				}
				SxmlNodeSet nodes = (SxmlNodeSet)Marshal.PtrToStructure( o.nodesetval,typeof(SxmlNodeSet));
				if (nodes.nodeNr != 1) {
					value = defaultValue;
					return false;
				}

				SxmlNode resultNode = nodes.getNode(0);
				if (resultNode.children == IntPtr.Zero) {

					value = defaultValue;
					return true;

				}
				resultNode = resultNode.getChild(0);
				value = resultNode.content;		
			


				xmlXPathFreeObject(xpathObj);
				return true;
			}
			value = defaultValue;
			return false;
		}
	}
}

