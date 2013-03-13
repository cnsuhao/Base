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
using System.Runtime.Serialization;

namespace Base.GML
{
    class Feature
    {
    }

    class ParserException : System.Exception
    {
        private string message;
        private int linenum;

        public ParserException(string message, int linenum = -1) : base(message)
        {
            this.message = message;
            this.linenum = linenum;
        }

        public override string Message { get { return Message; } }
        public int Linenum { get { return linenum; } }

        public override string ToString()
        {
            return "ParseException at " + linenum.ToString() + " : " + message;
        }
    }

    class Parser : IDisposable
    {
        const string WHITE_SPACE = " \t\n\r";
        const string WORD_BREAK = " \t\n\r{}[]()=->;:,\"\'/~";
        static readonly Token[] IGNORE_TOKEN = new Token[]{ Token.Semicolon };

        enum Token
        {
            None,
            Curly_Open,
            Curly_Close,
            Squared_Open,
            Squared_Close,
            Bracket_Open,
            Bracket_Close,
            Equal,
            Arrow,
            Semicolon,
            Colon,
            Comma,
            Word,
            String,
            Dollar,
            Inverse,
            Null
        };

        int line;
        int lastTokenLine;
        StringReader reader;
        Token lastToken;
        string content;
        List<string> warnings = new List<string>();
        List<Node> nodes = new List<Node>();

        public string[] Warnings { get { return warnings.ToArray(); } }
        private void AddWarning(string warning) { warnings.Add(warning); }
        Parser(string content)
        {
            line = 1;
            reader = new StringReader(content);
        }

        public void Dispose()
        {
            reader.Dispose();
            reader = null;
        }

        #region Public Interfaces
        public static Node Deserialize(string content) {
            return Parse(content); 
        }
        #endregion

        #region Parse Functions
        static Node Parse(string content)
        {
            using (var instance = new Parser(content))
            {
                return instance.Parse();
            }
        }
        Node Parse()
        {
          // create a root node
            Node root = Node.CreateRoot();

            // init parse
            ParseToken();
            // parse all nodes
            while (!Eof)
                root.AddChild(ParseNode());
            // lastObject
            if (lastToken != Token.None)
                root.AddChild(ParseNode());

            // second path
            ParseReference(root);

            return root;
        }
        void ParseReference(Node root)
        {
            foreach (Node node in nodes)
            {
                if (node.BaseRef == "$")
                {
                    Node refer = node.Find(node.Value);
                    if (refer == null)
                        AddWarning(new ParserException("You can not reference a reference node!").ToString());
                    else
                        node.ReferenceBy(refer);
                }
                else if (node.BaseRef != "") {
                    Node refer = node.Find(node.BaseRef);
                    if (node.IsAncestor(refer))
                        throw new ParserException("You can not make an base circle!");
                    node.Base = refer;
                }
            }
        }
        Node ParseNode()
        {
            Node node = null;
            string key = "";
            string value = "";
            string baseNode = "";
            bool hasEqualOP = false;
            bool hasDollor = false;
            bool hasArrow = false;

            // init token
            Token token = lastToken;
            // Get Key Str
            if (token == Token.Null)
                token = ParseToken();
            else if (token == Token.Word)
            {
                if (!Common.ValidateKey(content))
                    throw new ParserException("Key " + content + " is not valid!", lastTokenLine);
                key = content.ToLower();
                token = ParseToken();
            }
            // If have key, then find =
            if (token == Token.Equal)
            {
                if (key=="")
                    throw new ParserException("There is a '=' but no key before it", lastTokenLine);
                token = ParseToken();
                hasEqualOP = true;
            }
            // Get Value Str
            if (token == Token.Dollar)
            {
                hasDollor = true;
                token = ParseToken();
            }

            if (token == Token.Null)
                token = ParseToken();
            else if (token == Token.String)
            {
                if (key != "" && !hasEqualOP)
                    throw new ParserException("There is a no '=' between key and value", lastTokenLine);
                value = content;
                token = ParseToken();
            }
            else if (hasEqualOP)
                throw new ParserException("There is a '=' but no value after it", lastTokenLine);
            else if (hasDollor)
                throw new ParserException("There is no value after $ symbol", lastTokenLine);

            // Get Base Str
            if (token == Token.Colon)
            {
                token = ParseToken();
                if (token != Token.Word)
                    throw new ParserException("There is a ':' but no word after it", lastTokenLine);
                if (hasDollor)
                    throw new ParserException("You can not use reference & base in the same node", lastTokenLine);
                baseNode = content;
                token = ParseToken();
            }
            
            // create node
            node = new Node(value, key, hasDollor?"$":baseNode);
            nodes.Add(node);

            // find attributes
            while (token == Token.Bracket_Open)
            {
                if (node.IsAnonymous)
                    throw new ParserException("There must be either key or value after attributes", lastTokenLine);

                HashSet<string> set = new HashSet<string>();
                foreach (Node subNode in ParseArray())
                {
                    if (subNode.HasKey)
                    {
                        if (set.Contains(subNode.Key))
                            throw new ParserException("The key name of attributes should be unique!", lastTokenLine);
                        else
                            set.Add(subNode.Key);
                    }
                    node.AddAttribute(subNode);
                }
                token = lastToken;
            }

            // check whether there is a '->' to child array
            token = lastToken;
            while (token == Token.Arrow)
            {
                if (node.IsAnonymous)
                    throw new ParserException("There must be either key or value after ->", lastTokenLine);
                token = ParseToken();

                hasArrow = true;
                if (token == Token.Curly_Open || token == Token.Squared_Open)
                    foreach (Node subNode in ParseArray())
                        node.AddChild(subNode);
                else
                    node.AddChild(ParseNode());

                token = lastToken;
            }

            // if nothing catched
            if (!hasArrow && node.IsAnonymous)
                // check single array
                if (token == Token.Curly_Open || token == Token.Squared_Open)
                    foreach (Node subNode in ParseArray())
                        node.AddChild(subNode);
                // otherwise, error occured
                else
                    throw new ParserException("Unknown characters " + content, lastTokenLine);

            token = lastToken;
            if (token == Token.Inverse)
            {
                token = ParseToken();
                if (token != Token.Word || content.ToLower() != node.Key)
                    throw new ParserException("~" + content + " not match", lastTokenLine);
                ParseToken();
            }

            return node;
        }

        List<Node> ParseArray()
        {
            List<Node> nodes = new List<Node>();
            int originLine = lastTokenLine;

            Token end = Opposite(lastToken);
            Token token = ParseToken();

            while (token != end)
            {
                if (Eof)
                    throw new ParserException("no end part of " + content, originLine);

                nodes.Add(ParseNode());
                token = lastToken;

                if (token == Token.Comma)
                    token = ParseToken();
            }
            ParseToken();

            return nodes;
        }

        bool CheckIgnore()
        {
            foreach (Token token in IGNORE_TOKEN)
                if (lastToken == token)
                    return true;
            return false;
        }

        Token ParseToken()
        {
            do
            {
                lastToken = NextToken;
            } while (CheckIgnore());
            
            return lastToken;
        }

        string ParseString()
        {
            StringBuilder builder = new StringBuilder();
            char begin = content[0];
            int originLine = line;

            while (!Eof && PeekChar != begin)
            {
                char c = NextChar;
                if (c == '\\')
                {
                    if (Eof)
                        throw new ParserException("this is no character after \\ in " + content, originLine);
                    c = NextChar;
                    switch (c)
                    {
                        case '"':
                        case '\'':
                        case '\\':
                        case '/':
                            builder.Append(c);
                            break;
                        case 'b':
                            builder.Append('\b');
                            break;
                        case 'f':
                            builder.Append('\f');
                            break;
                        case 'n':
                            builder.Append('\n');
                            break;
                        case 'r':
                            builder.Append('\r');
                            break;
                        case 't':
                            builder.Append('\t');
                            break;
                    }
                }
                else
                    builder.Append(c);
            }

            if (Eof)
                throw new ParserException("no quote before " + begin, originLine);
         
            EatChar();
            return builder.ToString();
        }
        #endregion
        #region Utility
        void EatWhitespace()
        {
            if (Eof)
                return;

            while (WHITE_SPACE.IndexOf(PeekChar) != -1)
            {
                EatChar();

                if (Eof)
                    break;
            }
        }

        void EatChar()
        {
            char _c = NextChar;
        }
        void EatLine()
        {
            reader.ReadLine();
            line++;
        }
        void EatComment()
        {
            bool hitStar = false;
            while (!Eof)
            {
                switch (NextChar)
                {
                    case '*':
                        hitStar = true;
                        break;
                    case '/':
                        if (hitStar)
                            return;
                        break;
                    default:
                        hitStar = false;
                        break;
                }
            }
        }
        Token Opposite(Token token)
        {
            switch (token)
            {
                case Token.Curly_Close:
                    return Token.Curly_Open;
                case Token.Curly_Open:
                    return Token.Curly_Close;
                case Token.Bracket_Close:
                    return Token.Bracket_Open;
                case Token.Bracket_Open:
                    return Token.Bracket_Close;
                case Token.Squared_Close:
                    return Token.Squared_Open;
                case Token.Squared_Open:
                    return Token.Squared_Close;
                default:
                    return token;
            }
        }
        #endregion
        #region Nexts
        char PeekChar
        {
            get {
                return Convert.ToChar(reader.Peek());
            }
        }
        char NextChar
        {
            get
            {
                char c = Convert.ToChar(reader.Read());
                if (c == '\n')
                    line ++;
                return c;
            }
        }
        bool Eof
        {
            get
            {
                return reader.Peek() == -1;
            }
        }
        string NextWord
        {
            get
            {
                StringBuilder word = new StringBuilder();

                if (Eof)
                    return "";

                while (WORD_BREAK.IndexOf(PeekChar) == -1) {
                    word.Append(NextChar);

                    if (Eof)
                        break;
                }

                return word.ToString();
            }
        }
        Token NextToken
        {
            get
            {
                EatWhitespace();
                lastTokenLine = line;
                
                if (Eof)
                    return Token.None;

                content = "";
                char c = NextChar;
                content += c;

                switch (c)
                {
                    case '{':
                        return Token.Curly_Open;
                    case '}':
                        return Token.Curly_Close;
                    case '[':
                        return Token.Squared_Open;
                    case ']':
                        return Token.Squared_Close;
                    case '(':
                        return Token.Bracket_Open;
                    case ')':
                        return Token.Bracket_Close;
                    case '=':
                        return Token.Equal;
                    case ';':
                        return Token.Semicolon;
                    case ':':
                        if (!Eof && PeekChar == ':')
                        {
                            content += NextChar;
                            return Token.Arrow;
                        }
                        return Token.Colon;
                    case ',':
                        return Token.Comma;
                    case '$':
                        return Token.Dollar;
                    case '~':
                        return Token.Inverse;
                    case '/':
                        // Eat Comment // & /*...*/
                        if (PeekChar == '*')
                            EatComment();
                        else if (PeekChar == '/')
                            EatLine();
                        else
                            throw new ParserException("unknown character '/'", line);
                        return NextToken;
                    case '-':
                        if (!Eof && NextChar == '>')
                            return Token.Arrow;
                        throw new ParserException("found '-' but no '>'", line);
                    case '"':
                    case '\'':
                        content = ParseString();
                        return Token.String;
                }

                content += NextWord;
                switch (content)
                {
                    case "null":
                        return Token.Null;
                }

                return Token.Word;
            }
        }
        #endregion
    }
}
