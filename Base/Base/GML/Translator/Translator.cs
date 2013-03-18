using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Base.GML.Translator
{
    /*
     * Translator to translate other data descripting language into gml or do the inversed operation
     */
    class Translator
    {
        protected static Node ImportRaw(Dictionary<string, object> dict)
        {
            Node node = new Node();

            foreach (KeyValuePair<string, object> pair in dict)
            {
                Node child = null;
                if (pair.Value is Dictionary<string, object>)
                    child = ImportRaw(pair.Value as Dictionary<string, object>);
                else if (pair.Value is List<object>)
                    child = ImportRaw(pair.Value as List<object>);
                else if (pair.Value == null)
                    child = new Node("");
                else
                    child = new Node(pair.Value.ToString());

                child.Key = pair.Key;
                node.AddChild(child);
            }

            return node;
        }

        protected static Node ImportRaw(List<object> list)
        {
            Node node = new Node();

            foreach (object var in list)
            {
                Node child = null;
                if (var is Dictionary<string, object>)
                    child = ImportRaw(var as Dictionary<string, object>);
                else if (var is List<object>)
                    child = ImportRaw(var as List<object>);
                else if (var == null)
                    child = new Node("");
                else
                    child = new Node(var.ToString());

                node.AddChild(child);
            }

            return node;
        }

        protected static object ExportRaw(Node node)
        {
            if (node.HasKey && !node.IsRoot)
            {
                if (node.HasAttribute || node.HasChild)
                {
                    Dictionary<string, object> dict = new Dictionary<string, object>();
                    dict[node.Key] = _ExportDict(node);
                    return dict;
                }
                else if (node.HasValue)
                    return new KeyValuePair<string, object>(node.Key, node.Value);
                else
                    return new KeyValuePair<string, object>(node.Key, null);
            }
            else
            {
                return _ExportDict(node);
            }
        }

        protected static Dictionary<string, object> _ExportDict(Node node)
        {
            Dictionary<string, object> dict = new Dictionary<string,object>();

            if (node.HasValue)
                dict["value"] = node.Value;

            if (node.HasAttribute)
            {
                Dictionary<string, object> subDict = new Dictionary<string, object>();
                Dictionary<string, int> dictCnt = new Dictionary<string, int>();

                dict["contains"] = subDict;
                foreach (Node sub in node.Attributes)
                {
                    if (sub.HasKey)
                    {
                        if (!dictCnt.ContainsKey(sub.Key))
                        {
                            dictCnt[sub.Key] = 1;
                            subDict[sub.Key] = ExportRaw(sub);
                        }
                        else
                        {
                            dictCnt[sub.Key] += 1;
                            string newKey = sub.Key + "_" + dictCnt[sub.Key].ToString();
                            subDict[newKey] = _ExportDict(sub);
                        }
                    }
                    else
                    {
                        if (!subDict.ContainsKey("_anonymous"))
                            subDict["_anonymous"] = new List<object>();

                        (subDict["_anonymous"] as List<object>).Add(ExportRaw(sub));
                    }
                }
            }

            if (node.HasChild)
            {
                Dictionary<string, object> subDict = new Dictionary<string, object>();
                Dictionary<string, int> dictCnt = new Dictionary<string, int>();

                dict["attributes"] = subDict;
                foreach (Node sub in node.Children)
                {
                    if (sub.HasKey)
                    {
                        if (!dictCnt.ContainsKey(sub.Key))
                        {
                            dictCnt[sub.Key] = 1;
                            subDict[sub.Key] = ExportRaw(sub);
                        }
                        else
                        {
                            dictCnt[sub.Key] += 1;
                            string newKey = sub.Key + "_" + dictCnt[sub.Key].ToString();
                            subDict[newKey] = _ExportDict(sub);
                        }
                    }
                    else
                    {
                        if (!subDict.ContainsKey("_anonymous"))
                            subDict["_anonymous"] = new List<object>();

                        (subDict["_anonymous"] as List<object>).Add(ExportRaw(sub));
                    }
                }
            }

            return dict;
        }
    }
}
