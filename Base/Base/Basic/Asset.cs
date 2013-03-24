using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Base
{
    public class Assert
    {
        public static void Null(object o, string name = "")
        {
            if (o == null)
                Logger.Default.Error((name==""?"":o.GetType().ToString()) + " can not be Null");
        }
    }
}
