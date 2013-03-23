using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace Base
{
    class Utility
    {
        public static Hashtable GenHashTable(params object[] args)
        {
            Hashtable hashTable = new Hashtable(args.Length / 2);
            if (args.Length % 2 != 0)
            {
                throw new Exception("Hash table requires an even number of arguments!");
            }
            else
            {
                int i = 0;
                while (i < args.Length - 1)
                {
                    hashTable.Add(args[i], args[i + 1]);
                    i += 2;
                }
                return hashTable;
            }
        }
    }
}
