/*
 *  Created by Hyf042 on 1/13/12.
 *  Copyright 2012 Hyf042. All rights reserved.
 *
 *  This is a custom data described language named Water,
 *  The idea & framework of Water are comed from GLtracy,
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

    class Node : ICloneable
    {
        // type
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
        public int ToInt { get { return Convert.ToInt32(value); } }
        public long ToLong { get { return Convert.ToInt64(value); } }
        public bool ToBoolean { get { return Convert.ToBoolean(value); } }
        public double ToDouble { get { return Convert.ToDouble(value); } }
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
        public static implicit operator int(Node node) { return node.ToInt; }
        public static implicit operator long(Node node) { return node.ToLong; }
        public static implicit operator bool(Node node) { return node.ToBoolean; }
        public static implicit operator double(Node node) { return node.ToDouble; }
        public static implicit operator string(Node node) { return node.Value; }
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
            attributes.Add(node);
            return this;
        }
        public Node AddChild(Node node)
        {
            node.parent = this;
            children.Add(node);
            return this;
        }
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
        public override string ToString()
        {
            return Writer.Serialize(this);
        }
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
        #endregion

        #region Utility Functions
        //Static
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
        public static Node Create<T>(T v, string key = "", string baseRef = "")
        {
            Node node = new Node();
            node.Assign(v, key, baseRef);
            return node;
        }
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
        public static Node CreateRoot()
        {
            return new Node("", "/");
        }
        
        public Node Search(string key)
        {
            if (Key == key)
                return this;
            Node found = null;
            foreach (Node attri in Attributes)
            {
                found = attri.Search(key);
                if (found != null)
                    return found;
            }
            foreach (Node attri in Children)
            {
                found = attri.Search(key);
                if (found != null)
                    return found;
            }
            return found;
        }
        public Node[] SearchAll(string key)
        {
            List<Node> ret = new List<Node>();
            SearchAll(key, ret);
            return ret.ToArray();
        }
        private void SearchAll(string key, List<Node> ret_list)
        {
            if (Key == key)
                ret_list.Add(this);
            foreach (Node attri in Attributes)
                attri.SearchAll(key, ret_list);
            foreach (Node child in Children)
                child.SearchAll(key, ret_list);
        }
        public bool DetailMatch(string keyPattern, string attriPattern = "", string childPattern = "", string valuePattern = "")
        {
            Regex match = new Regex(keyPattern);
            if (keyPattern != "" && match.Match(key).Length == 0)
                return false;
            match = new Regex(valuePattern);
            if (valuePattern != "" && match.Match(value).Length == 0)
                return false;

            if (attriPattern != "")
            {
                match = new Regex(attriPattern);

                bool flag = false;
                foreach (Node node in Attributes)
                    if (match.Match(node.Key).Length > 0)
                    {
                        flag = true;
                        break;
                    }
                if (flag == false)
                    return false;
            }

            if (valuePattern != "")
            {
                match = new Regex(valuePattern);

                bool flag = false;
                foreach (Node node in Children)
                    if (match.Match(node.Key).Length > 0)
                    {
                        flag = true;
                        break;
                    }
                if (flag == false)
                    return false;
            }

            return true;
        }
        public Node[] SearchDetail(string detailPattern)
        {
            List<string> split = detailPattern.Split('/').ToList();
            while (split.Count < 4)
                split.Add("");

            return SearchDetail(split[0], split[1], split[2], split[3]);
        }
        public Node[] SearchDetail(string keyPattern, string attriPattern = "", string childPattern = "", string valuePattern = "")
        {
            List<Node> ret_list = new List<Node>();
            SearchDetail(keyPattern, attriPattern, childPattern, valuePattern, ret_list);
            return ret_list.ToArray();
        }
        private void SearchDetail(string keyPattern, string attriPattern, string childPattern, string valuePattern, List<Node> ret_list)
        {
            if (DetailMatch(keyPattern, attriPattern, childPattern, valuePattern))
                ret_list.Add(this);

            foreach (Node node in Attributes)
                node.SearchDetail(keyPattern, attriPattern, childPattern, valuePattern, ret_list);
            foreach (Node node in Children)
                node.SearchDetail(keyPattern, attriPattern, childPattern, valuePattern, ret_list);
        }

        public Node Find(string reference)
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
            foreach (Node trial in Root.SearchAll(path.Last()))
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
