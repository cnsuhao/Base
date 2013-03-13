using System;
using System.Xml;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;

namespace Base.GML.Translator
{
    class XML : Translator
    {
        public static Node Import(string content)
        {
            DateTime now = DateTime.Now;
            Node root = Node.CreateRoot();
            TimeSpan delta = DateTime.Now - now;
            Console.WriteLine(delta.ToString());

            using (StringReader sr = new StringReader(content))
            using (XmlReader reader = XmlReader.Create(sr))
            {
                root.AddChild(ImportXML(reader));
            }

            return root;
        }

        private static Node ImportXML(XmlReader reader)
        {
            do
            {
                if (reader.NodeType == XmlNodeType.Element)
                    break;
            } while (reader.Read());

            if (reader.NodeType != XmlNodeType.Element)
                throw new Exception("here needs a element begin!");
            
            Node node = new Node();
            node.Key = reader.Name;
            node.Value = reader.Value;
            
            if (reader.HasAttributes)
            {
                for (int i = 0; i < reader.AttributeCount; i++)
                {
                    reader.MoveToAttribute(i);
                    node.AddAttribute(new Node(reader.Value, reader.Name));
                }
                reader.MoveToElement();
            }

            if (!reader.IsEmptyElement)
                while(reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        node.AddChild(ImportXML(reader));
                    }
                    else if (reader.NodeType == XmlNodeType.EndElement)
                        break;
                }

            return node;
        }

        public static string Export(Node node)
        {
            StringBuilder builder = new StringBuilder();
            XmlWriterSettings settings = new XmlWriterSettings();
            using (XmlWriter writer = XmlWriter.Create(builder, settings))
            {
                writer.WriteStartDocument();

                ExportXMLElement(node, writer, "gml");

                writer.WriteEndDocument();
            }

            return builder.ToString();
        }

        private static void ExportXMLElement(Node node, XmlWriter writer, string defaultKey="anonymous")
        {
            writer.WriteStartElement(((!node.IsRoot) && node.HasKey) ? node.Key : defaultKey);

            if (node.HasValue)
                writer.WriteValue(node.Value);
            if (node.HasAttribute)
                foreach (Node sub in node.Attributes)
                    ExportXMLAttribute(sub, writer);

            if (node.HasChild)
                foreach (Node sub in node.Children)
                    ExportXMLElement(sub, writer);

            writer.WriteEndElement();
        }

        private static void ExportXMLAttribute(Node node, XmlWriter writer, string defaultKey = "anonymous")
        {
            writer.WriteStartAttribute((!node.IsRoot || node.HasKey) ? node.Key : defaultKey);

            if (node.HasValue)
                writer.WriteValue(node.Value);
            if (node.HasAttribute)
            {
                foreach (Node sub in node.Attributes)
                {
                    ExportXMLAttribute(sub, writer);
                }
            }

            writer.WriteEndAttribute();
        }
    }
}
