using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets {
    class Util {
        public static void Log(Object message) {
            UnityEngine.Debug.Log(message);
        }

        public static void Log(Object message, UnityEngine.Object context) {
            UnityEngine.Debug.Log(message, context);
        }
    }
}
