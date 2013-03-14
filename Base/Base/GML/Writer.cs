/*
 *  Created by Hyf042 on 1/13/12.
 *  Copyright 2012 Hyf042. All rights reserved.
 *
 *  This is a custom data described language named Water,
 *  The idea & framework of Water are comed from GLtracy,
 *  reference website, you can see http://hi.baidu.com/gltracy/item/e205bf06830b60f2a01034ce
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Base.GML
{
    class Writer
    {
        #region Style
        public class ArrayStyle
        {
            public string begin = "";
            public string end = "";
            public string mid = "";
            public string tail = "";
            public bool lineBreak = false;

            public static ArrayStyle AttributeStyle = new ArrayStyle("(", ")", ", ", "", false);
            public static ArrayStyle ChildrenStyle = new ArrayStyle("{", "}", "", "", true);
            
            public ArrayStyle(string begin, string end, string mid, string tail, bool lineBreak)
            {
                this.begin = begin;
                this.end = end;
                this.mid = mid;
                this.tail = tail;
                this.lineBreak = lineBreak;
            }
        }
        public class Style
        {
            public ArrayStyle attriStyle;
            public ArrayStyle curlyStyle;
            public bool hasLineBreak;
            public bool hasIndentation;
            public bool hasSpace;
            public bool hasInverseSignal;
            public bool hasHeader = false;
            public Style(ArrayStyle attriStyle, ArrayStyle curlyStyle, bool lineBreak, bool indentation, bool space, bool inverse, bool hasHeader)
            {
                this.attriStyle = attriStyle;
                this.curlyStyle = curlyStyle;
                this.hasLineBreak = lineBreak;
                this.hasIndentation = indentation;
                this.hasSpace = space;
                this.hasInverseSignal = inverse;
                this.hasHeader = hasHeader;
            }

            public static readonly Style SimplestStyle = new Style(new ArrayStyle("(", ")", ",", "", false),
                                                             new ArrayStyle("{","}","","",false),
                                                             false,
                                                             false,
                                                             false,
                                                             false,
                                                             true);
            public static readonly Style SmallestStyle = new Style(new ArrayStyle("(", ")", ",", "", false),
                                                             new ArrayStyle("{", "}", "", "", false),
                                                             false,
                                                             false,
                                                             false,
                                                             false,
                                                             false);
            public static readonly Style StandardStyle = new Style(ArrayStyle.AttributeStyle, 
                                                                   ArrayStyle.ChildrenStyle, 
                                                                   true, 
                                                                   true, 
                                                                   true,
                                                                   true, 
                                                                   true);
        }
        #endregion

        Style style;
        StringBuilder writer = new StringBuilder();
        Node root = null;
        int indentation = 0;

        public Writer(Node node, Style style = null)
        {
            this.root = node;
            this.style = style;
            if (this.style == null)
                this.style = Style.StandardStyle;
        }

        public string Generate()
        {
            if (root.IsRoot)
                WriteHeader();
            if (root.Key == "/")
            {
                bool firstItem = true;
                foreach (Node node in root.Children)
                {
                    if (!firstItem)
                        LineFeed();
                    else
                        firstItem = false;
                    SerializeNode(node);
                }
            }
            else
                SerializeNode(root);

            return writer.ToString();
        }

        #region Private Functions
        void SerializeNode(Node node, bool isIndent = false)
        {
            if (isIndent)
                Indentation();

            if (node.HasKey)
                writer.Append(node.Key);

            if (node.HasKey && node.HasValue)
            {
                Space();
                writer.Append("=");
                Space();
            }

            if (node.HasValue)
            {
                if (node.BaseRef == "$")
                    writer.Append('$');
                writer.Append("\"");
                SerializeString(node.Value);
                writer.Append("\"");
            }

            if (node.BaseRef.Length > 0 && node.BaseRef != "$")
            {
                Space();
                writer.Append(":");
                Space();
                writer.Append(node.BaseRef);
            }

            if (node.HasAttribute)
            {
                Space();
                SerializeArray(node.Attributes, style.attriStyle);
            }

            if (node.HasChild)
            {
                if (!node.IsAnonymous)
                {
                    Space();
                    writer.Append("->");
                }
                else if(node.HasAttribute)
                {
                    if (isIndent)
                        Indentation();
                }

                SerializeArray(node.Children, style.curlyStyle, node.IsAnonymous);

                if (node.HasKey && style.hasInverseSignal)
                {
                    LineFeed(style.hasLineBreak);
                    if (isIndent)
                        Indentation();
                    writer.Append("~" + node.Key);
                }
            }
        }
        void SerializeString(string s)
        {
            //writer.Append(s);
            char[] charArray = s.ToCharArray();
            foreach (var c in charArray)
                switch (c)
                {
                    case '"':
                        writer.Append("\\\"");
                        break;
                    case '\'':
                        writer.Append("\'");
                        break;
                    case '\\':
                        writer.Append("\\\\");
                        break;
                    case '\b':
                        writer.Append("\\b");
                        break;
                    case '\f':
                        writer.Append("\\f");
                        break;
                    case '\n':
                        writer.Append("\\n");
                        break;
                    case '\r':
                        writer.Append("\\r");
                        break;
                    case '\t':
                        writer.Append("\\t");
                        break;
                    default:
                        writer.Append(c);
                        break;
                }
        }
        void SerializeArray(Container container, ArrayStyle style, bool IsAnonymous = false)
        {
            if (!IsAnonymous)
            {
                LineFeed(style.lineBreak);
                if (style.lineBreak)
                    Indentation();
            }
            writer.Append(style.begin);
            LineFeed(style.lineBreak);
            bool firstItem = true;
            foreach (Node node in container)
            {
                if (!firstItem)
                {
                    writer.Append(style.mid);
                    LineFeed(style.lineBreak);
                }
                firstItem = false;

                IndentationIn();
                SerializeNode(node, style.lineBreak);
                writer.Append(style.tail);
                IndentationOff();
            }
            LineFeed(style.lineBreak);
            if (style.lineBreak)
                Indentation();
            writer.Append(style.end);
        }
        void WriteHeader()
        {
            if (style.hasHeader)
            {
                writer.Append("/*");
                writer.Append("gml version=\"" + Common.Version + "\" encoding=\"UTF8\"");
                writer.Append("*/");
                LineFeed();
            }
        }
        void LineFeed(bool ln = true)
        {
            if (ln && style.hasLineBreak)
                writer.Append("\n");
        }
        void Space(int size = 1, char c = ' ')
        {
            if (style.hasSpace)
                for (int i = 0; i < size; i++)
                    writer.Append(c);
        }
        void Indentation()
        {
            if (style.hasIndentation && indentation > 0)
                Space(indentation, '\t');
        }
        void IndentationIn()
        {
            indentation++;
        }
        void IndentationOff()
        {
            indentation--;
        }
        #endregion

        public static string Serialize(Node node, Style style = null)
        {
            var instance = new Writer(node, style);
            return instance.Generate();
        }
    }
}
