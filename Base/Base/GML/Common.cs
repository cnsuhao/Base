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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Base.GML
{
    class Common
    {
        public const string Version = "V1.0.0.0";
        public const char ReferencePrefixCharacter = '$';
        // Validate whether a key name is legal
        public static bool ValidateKey(string key)
        {
            for (int i = 0; i < key.Length; i++)
            {
                if (key[i] >= 'a' && key[i] <= 'z' || key[i] >= 'A' && key[i] <= 'Z' || key[i] == '_')
                    continue;
                if (key[i] >= '0' && key[i] <= '9' && i != 0)
                    continue;
                return false;
            }
            return true;
        }
        // Parse a reference string
        public static List<string> ParseReference(string reference)
        {
            StringReader reader = new StringReader(reference);
            List<string> path = new List<string>();
            string sub = "";

            if (reference.Length > 0 && reference[0] == '/')
            {
                path.Add("ROOT");
                reader.Read();
            }

            while (reader.Peek() != -1)
            {
                char c = Convert.ToChar(reader.Read());
                if (c == '/')
                {
                    if (sub == "..")
                        path.Add("UP");
                    else if (sub == ".")
                        sub = "";
                    else if (ValidateKey(sub))
                        path.Add(sub.ToLower());
                    else
                        throw new KeyNotFoundException();
                    sub = "";
                }
                else
                    sub += c;
            }
            if (sub != "")
                path.Add(sub.ToLower());

            return path;
        }
    }
}
