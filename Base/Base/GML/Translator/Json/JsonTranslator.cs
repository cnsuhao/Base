using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Base.GML.Translator
{
    class Json : Translator
    {
        public static Node Import(string content)
        {
            var json = MiniJSON.Json.Deserialize(content);

            Node root = Node.CreateRoot();

            if (json is Dictionary<string, object>)
                root.AddChild(ImportRaw(json as Dictionary<string, object>));
            else if (json is List<object>)
                root.AddChild(ImportRaw(json as List<object>));
            else
                root.AddChild(new Node(json.ToString()));

            return root;
        }

        public static string Export(Node node)
        {
            return MiniJSON.Json.Serialize(ExportRaw(node));
        }
    }
}
