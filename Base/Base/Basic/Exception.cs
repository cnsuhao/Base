using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Base
{
    public class BaseException : System.Exception
    {
        string module;

        public BaseException(string message, string module = "")
            : base(message)
        {
            this.module = module;
        }

        public string Module { get { return module; } }

        public override string ToString()
        {
            if (module == "")
                return base.ToString();
            else
                return base.ToString() + " in Module[" + module + "]";
        }
    }
}
