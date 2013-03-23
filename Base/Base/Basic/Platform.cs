#if UNITY_3_4 || UNITY_3_5 || UNITY_4_0 || UNITY_4_0_1 || UNITY_4_1
    #define UNITY
#else
    #define WIN
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Base
{
    class Platform
    {
        public static bool Unity
        {
            get
            {
#if UNITY
                return true;
#else
                return false;
#endif
            }
        }
    }
}
