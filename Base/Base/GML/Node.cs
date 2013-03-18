/*
 *  Created by Hyf042 on 1/13/12.
 *  Copyright 2012 Hyf042. All rights reserved.
 *
 *  This is a custom data described language named GML,
 *  The idea & framework of GML are comed from GLtracy,
 *  reference website, you can see http://hi.baidu.com/gltracy/item/e205bf06830b60f2a01034ce
 */
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace Base.GML
{
    /*
     * A container to contain a bunch of nodes
     * It holds a list to store all the nodes in order
     * And it also holds a dictionary to store all the named nodes for request by name
     */
    class Container : IEnumerable, ICloneable
    {
        private Container baseContainer = null;
        private List<Node> nodes = new List<Node>();
        private Dictionary<string, Node> map = new Dictionary<string, Node>();

        public int Count { get { return nodes.Count; } }
        public bool Empty { get { return nodes.Count == 0; } }
        public Container Whole { get { return this + baseContainer.Whole; } }
        public bool HasBase { get { return baseContainer != null; }}
        public Container Base { get { return baseContainer; } set { baseContainer = value; } }

        public Node this[string key]
        {
            get {
                if (map.ContainsKey(key))
                    return map[key];
                else if (HasBase)
                    return baseContainer[key];
                else
                    throw new KeyNotFoundException();
            }
            set
            {
                value.Key = key;
                Add(value);
            }
        }
        public Node this[int index]
        {
            get {
                if (index < nodes.Count)
                    return nodes[index];
                else if (HasBase)
                    return baseContainer[index - nodes.Count];
                else
                    throw new ArgumentOutOfRangeException();
            }
            set
            {
                Add(value);
            }
        }

        public bool Has(string key)
        {
            if (map.ContainsKey(key))
                return true;
            else if (HasBase)
                return baseContainer.Has(key);
            else
                return false;
        }

        public void Clear()
        {
            nodes.Clear();
            map.Clear();
        }

        // remove by key, it will remove all the key with the same name
        public Container Remove(string key)
        {
            if (!map.ContainsKey(key))
                return this;
            map.Remove(key);

            List<Node> newlist = new List<Node>();
            for (int i = 0; i < nodes.Count; i++)
                if (nodes[i].Key != key)
                    newlist.Add(nodes[i]);
            nodes = newlist;

            return this;
        }
        public Container Remove(Node node)
        {
            if (node.HasKey)
                map.Remove(node.Key);
            nodes.Remove(node);
            return this;
        }
        // if there exists a node with same key, it will replace it in map, but they will both store in list
        public Container Add(Node node)
        {
            nodes.Add(node);
            if (node.HasKey)
            {
                if (map.ContainsKey(node.Key))
                    map.Remove(node.Key);
                map.Add(node.Key, node);
            }

            return this;
        }

        #region Interface Func
        public object Clone()
        {
            Container clone = new Container();
            clone.baseContainer = this.baseContainer;
            foreach (Node node in this.nodes)
                clone.Add(node.Clone() as Node);
            return clone;
        }

        public static Container operator +(Container lhs, Container rhs)
        {
            Container ret = lhs.Clone() as Container;
            foreach (Node node in rhs)
                ret.Add(node);
            return ret;
        }

        // Enumerator, first self, then baseContainer
        public IEnumerator GetEnumerator()
        {
            return new ContainerEnumerator(this);
        }
        private class ContainerEnumerator : IEnumerator
        {
            private bool selfPhase = true;
            private IEnumerator self = null;
            private IEnumerator parent = null;
            private Container contain = null;

            public ContainerEnumerator(Container instance)
            {
                contain = instance;
                Reset();
            }

            public bool MoveNext()
            {
                if (selfPhase)
                {
                    if (self.MoveNext())
                        return true;
                    selfPhase = false;
                }
                if(!selfPhase)
                    if (parent != null)
                        return parent.MoveNext();
                return false;
            }
            public void Reset()
            {
                self = contain.nodes.GetEnumerator();
                parent = contain.HasBase?contain.baseContainer.GetEnumerator():null;
                selfPhase = true;
            }
            public object Current
            {
                get
                {
                    if (selfPhase)
                        return self.Current;
                    else if (parent != null)
                        return parent.Current;
                    else
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
        #endregion
    };

    class Node : IEnumerable, ICloneable
    {
        // type, you can check this for knowning the type of node's value
        public enum Type
        {
            None = 0,
            Interger = 1,
            Real = 2,
            Boolean = 4,
            Reference = 8,
            Attribute = 16,
            Children = 32,
            Key = 64,
            Value = 128,
            Number = Interger | Real,
            Container = Attribute | Children
        };
        #region Variables
        private string key = "";
        private string value = "";
        private string baseRef = "";
        private Node parent = null;
        private Node baseNode = null;
        private Container attributes = new Container();
        private Container children = new Container();
        #endregion

        #region Properties
        // Main Pro
        public string Name { get { return key; } }
        public string Key { get { return key; } set { key = value; } }
        public string Value { get { return value; } set { this.value = value; } }
        public byte[] RawValue { get { return Encoding.UTF8.GetBytes(value); } }
        public Node Parent { get { return parent; } }
        public Node Base { get { return baseNode; } 
            set {
                baseNode = value;
                attributes.Base = baseNode!=null?baseNode.Attributes:null;
                children.Base = baseNode!=null?baseNode.Children:null;
            }
        }
        public string BaseRef { get { return baseRef; } }
        public Node Root
        {
            get
            {
                if (IsRoot)
                    return this;
                else
                    return parent.Root;
            }
        }
        public string Path {
            get{
                if (IsRoot)
                    return HasKey ? Key : "_";
                else
                {
                    string parentPath = parent.Path;
                    return parentPath + (parentPath == "/" ? "" : "/") + (HasKey ? Key : "_");
                }
            }
        }
        public Container Attributes { get { return attributes; } }
        public Container Children { get { return children; } }
        public Type GetNodeType { get { return ValidateType(this); } }
        // Value Gets
        public int AsInt { get { return Convert.ToInt32(value); } }
        public long AsLong { get { return Convert.ToInt64(value); } }
        public bool AsBoolean { get { return Convert.ToBoolean(value); } }
        public double AsDouble { get { return Convert.ToDouble(value); } }
        public string AsString { get { return Value; } }
        public byte[] AsBytes { get { return Encoding.UTF8.GetBytes(Value); } }
        // Query
        public bool HasName { get { return key != ""; } }
        public bool HasKey { get { return key != ""; } }
        public bool HasValue { get { return value != ""; } }
        public bool HasAttribute { get { return !attributes.Empty; } }
        public bool HasChild { get { return !children.Empty; } }
        public bool IsRoot { get { return parent == null; } }
        public bool HasBase { get { return baseNode == null; } }
        public bool IsAnonymous { get { return !HasKey && !HasValue; } }
        public bool IsInt { get { int _tmp; return Int32.TryParse(value, out _tmp); } }
        public bool IsLong { get { long _tmp; return Int64.TryParse(value, out _tmp); } }
        public bool IsBoolean { get { bool _tmp; return Boolean.TryParse(value, out _tmp); } }
        public bool IsDouble { get { double _tmp; return Double.TryParse(value, out _tmp); } }
        public bool IsNumber { get { return IsDouble || IsInt || IsLong; } }
        public bool IsReference { get { return BaseRef == "$"; } }
        public bool IsContainer { get { return HasChild; } }
        public bool IsKeyValuePair { get { return HasKey && HasValue; } }
        #endregion

        #region Operators
        public Node this[string key] { get { return Children[key]; } set { Children[key] = value; } }
        public Node this[int index] { get { return Children[index]; } set { Children[key] = value; } }
        public static implicit operator Node(int value)
        {
            return Create(value);
        }
        public static implicit operator Node(long value)
        {
            return Create(value);
        }
        public static implicit operator Node(bool value)
        {
            return Create(value);
        }
        public static implicit operator Node(double value)
        {
            return Create(value);
        }
        public static implicit operator Node(string value)
        {
            return Create(value);
        }
        public static implicit operator int(Node node) { return node.AsInt; }
        public static implicit operator long(Node node) { return node.AsLong; }
        public static implicit operator bool(Node node) { return node.AsBoolean; }
        public static implicit operator double(Node node) { return node.AsDouble; }
        public static implicit operator string(Node node) { return node.Value; }
        public static implicit operator byte[](Node node) { return node.AsBytes; }
        #endregion

        #region Contructor
        public Node() { }
        public Node(string v, string key = "", string baseRef = "") { Assign(v, key, baseRef); }
        public Node(int v, string key = "", string baseRef = "") { Assign(v, key, baseRef); }
        public Node(long v, string key = "", string baseRef = "") { Assign(v, key, baseRef); }
        public Node(bool v, string key = "", string baseRef = "") { Assign(v, key, baseRef); }
        public Node(double v, string key = "", string baseRef = "") { Assign(v, key, baseRef); }
        public Node Assign<T>(T v, string key = "", string baseRef = "")
        {
            this.key = key;
            this.value = v.ToString();
            this.baseRef = baseRef;
            return this;
        }
        #endregion

        #region Public Interfaces
        public Node AddAttribute(Node node)
        {
            node.parent = this;
            if (!node.IsRoot)
                attributes.Add(node);
            else
                foreach (Node child in node)
                    attributes.Add(child);
            return this;
        }
        public Node AddChild(Node node)
        {
            node.parent = this;
            if (!node.IsRoot)
                children.Add(node);
            else
                foreach (Node child in node)
                    children.Add(child);
            return this;
        }
        // Reference by another node, it will copy all the values & attris & children from it
        public Node ReferenceBy(Node other)
        {
            value = other.Value;
            children = other.Attributes.Clone() as Container;
            attributes = other.Attributes.Clone() as Container;
            return this;
        }
        public bool IsAncestor(Node other)
        {
            Node node = other.parent;
            while (node != null)
            {
                if (node == this)
                    return true;
                node = node.parent;
            }
            return false;
        }
        // override ToString function, it will use Serialize with standard style
        public override string ToString()
        {
            return Writer.Serialize(this);
        }
        // you can choose custom style to serialize the node
        public string ToString(Writer.Style style)
        {
            return Writer.Serialize(this, style);
        }
        #endregion

        #region Private Functions
    
        public object Clone()
        {
            Node clone = new Node();
            clone.key = this.key;
            clone.value = this.value;
            clone.attributes = this.attributes.Clone() as Container;
            clone.children = this.children.Clone() as Container;
            return clone;
        }
        public IEnumerator GetEnumerator()
        {
            return Children.GetEnumerator();
        }
        #endregion

        #region Utility Functions
        // Validate the type of this node
        public static Type ValidateType(Node node)
        {
            Type type = Type.None;
            if (node.HasKey)
                type |= Type.Key;
            if (node.HasValue)
                type |= Type.Value;
            if (node.IsInt || node.IsLong)
                type |= Type.Interger;
            if (node.IsDouble)
                type |= Type.Real;
            if (node.IsReference)
                type |= Type.Reference;
            if (node.HasChild)
                type |= Type.Children;
            if (node.HasAttribute)
                type |= Type.Attribute;
            return type;
        }
        // Create a node with random value
        public static Node Create<T>(T v, string key = "", string baseRef = "")
        {
            Node node = new Node();
            node.Assign(v, key, baseRef);
            return node;
        }
        // Calculate the distance between a & b in the tree, you must gurrantee they have the same root
        public static int Distance(Node a, Node b)
        {
            int count = 0, index = 0;
            string lhs = a.Path, rhs = b.Path;
           
            // find maximum prefix
            while(index < Math.Min(lhs.Length, rhs.Length)) {
                if (lhs[index] != rhs[index])
                    break;
                index++;
            }
            // count distance
            if (index < lhs.Length) count++;
            for (int i = index; i < lhs.Length; i++)
                if (lhs[i] == '/')
                    count++;
            if (index < rhs.Length) count++;
            for (int i = index; i < rhs.Length; i++)
                if (rhs[i] == '/')
                    count++;

            return count;
        }
        // Create a empty root
        public static Node CreateRoot()
        {
            return new Node("", "/");
        }
        
        // Find nodes in the sub tree
        // If you use pattern, the syntax is here:
        // Key(=value)@attribute1=value1,attribute2=value2...:child1=value1,child2=value2...
        // All the string can be regular expression
        class FindBundle
        {
            public string keyValue = "";
            public List<string> attributes = null;
            public List<string> children = null;
        }
        public Node Find(string pattern, bool ispattern = true)
        {
            return Find(pattern, MakeFindBundle(pattern, ispattern));
        }
        public Node[] FindAll(string pattern, bool ispattern = true)
        {
            return FindAll(pattern, MakeFindBundle(pattern, ispattern));
        }
        public bool Match(string pattern, bool ispattern = true)
        {
            return Match(pattern, MakeFindBundle(pattern, ispattern));
        }
        private Node Find(string pattern, FindBundle bundle)
        {
            if (Match(pattern, bundle))
                return this;
            Node found = null;
            foreach (Node attri in Attributes)
            {
                found = attri.Find(pattern, bundle);
                if (found != null)
                    return found;
            }
            foreach (Node child in Children)
            {
                found = child.Find(pattern, bundle);
                if (found != null)
                    return found;
            }
            return found;
        }
        private Node[] FindAll(string pattern, FindBundle bundle)
        {
            List<Node> ret = new List<Node>();
            FindAll(pattern, ret, bundle);
            return ret.ToArray();
        }
        private void FindAll(string pattern, List<Node> ret_list, FindBundle bundle)
        {
            if (Match(pattern, bundle))
                ret_list.Add(this);
            foreach (Node attri in Attributes)
                attri.FindAll(pattern, ret_list, bundle);
            foreach (Node child in Children)
                child.FindAll(pattern, ret_list, bundle);
        }
        private FindBundle MakeFindBundle(string pattern, bool ispattern = true)
        {
            if (ispattern == false)
                return null;

            pattern += '^';

            string keyValue = "";
            List<string> attributes = new List<string>(), children = new List<string>();
            string tempStr = "";
            int patternType = 0;

            foreach (char c in pattern)
            {
                if (c == '@' || c == ':' || c == ',' || c == ' ' || c == '^')
                {
                    if (tempStr != "")
                    {
                        if (patternType == 0)
                            keyValue = tempStr;
                        else if (patternType == 1)
                            attributes.Add(tempStr);
                        else
                            children.Add(tempStr);
                    }
                    tempStr = "";
                    if (c == '@')
                        patternType = 1;
                    else if (c == ':')
                        patternType = 2;
                }
                else
                    tempStr += c;
            }

            FindBundle bundle = new FindBundle();
            bundle.keyValue = keyValue;
            bundle.attributes = attributes;
            bundle.children = children;

            return bundle;
        }
        private bool Match(string pattern, FindBundle bundle = null)
        {
            // if not pattern match, just compare key
            if (bundle == null)
                return Key == pattern;

            return Match(bundle);
        }
        private bool MatchKeyValuePair(string keyValue)
        {
            int offset = keyValue.IndexOf('=');
            if (offset < 0)
                return Regex.IsMatch(Key, "^"+keyValue+"$", RegexOptions.IgnoreCase);
            string keyPattern = keyValue.Substring(0, offset);
            string valuePattern = keyValue.Substring(offset + 1);
            valuePattern = valuePattern.Trim(new char[]{'\'','"'});

            if (!Regex.IsMatch(Key, "^"+keyPattern+"$", RegexOptions.IgnoreCase))
                return false;
            if (!Regex.IsMatch(Value, "^"+valuePattern+"$"))
                return false;

            return true;
        }
        private bool Match(FindBundle bundle)
        {
            if (bundle.keyValue != "")
                if (!MatchKeyValuePair(bundle.keyValue))
                    return false;

            if (bundle.attributes.Count > 0)
            {
                List<string> matches = bundle.attributes.ToList();
                foreach (Node node in Attributes)
                    foreach (string match in matches)
                        if (node.MatchKeyValuePair(match))
                        {
                            matches.Remove(match);
                            break;
                        }
                if (matches.Count > 0)
                    return false;
            }

            if (bundle.children.Count > 0)
            {
                List<string> matches = bundle.children.ToList();
                foreach (Node node in Children)
                    foreach (string match in matches)
                        if (node.MatchKeyValuePair(match))
                        {
                            matches.Remove(match);
                            break;
                        }
                if (matches.Count > 0)
                    return false;
            }

            return true;
        }
        // Find a node with reference path
        public Node FindPath(string reference)
        {
            List<string> path = Common.ParseReference(reference);

            // first find local
            Node node = parent;
            Node found = null;
            foreach (string pathNode in path)
            {
                if (node == null)
                {
                    found = null;
                    break;
                }
                else if (pathNode == "ROOT")
                    node = node.Root;
                else if (pathNode == "UP")
                    node = node.parent;
                else if (node.Children.Has(pathNode))
                {
                    node = node.Children[pathNode];
                    found = node;
                }
                else
                {
                    found = null;
                    break;
                }
            }
            if (found != null)
                return found;

            // then found global
            int dist = -1;
            foreach (Node trial in Root.FindAll(path.Last()))
            {
                int rdist = Distance(trial, this);
                if (rdist >= 0 && (dist < 0 || rdist < dist))
                {
                    found = trial;
                    dist = rdist;
                }
            }

            return found;
        }
        #endregion
    }
}
