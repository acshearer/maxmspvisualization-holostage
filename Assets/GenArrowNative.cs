#if (UNITY_NATIVE)
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using UnityEngine;


namespace Assets {
    public class GenArrowNative : MonoBehaviour {
        private static ArrowCollection arrowCollection;
        private static GameObject go;
        JitNetRecvNative listener;
        public static int x { get; set; }
        public static int y { get; set; }
        //private static System.Collections.Concurrent.ConcurrentQueue<float[,,]> renderFloat;
        private static Queue<float[,,]> renderFloat;

        void Start() {
            go = gameObject;
            arrowCollection = ArrowCollection.CreateArrowCollection(100, 100, gameObject);
            //renderFloat = new System.Collections.Concurrent.ConcurrentQueue<float[,,]>();
            renderFloat = new Queue<float[,,]>();
            listener = new JitNetRecvNative(7474);
        }

        public static void addToRenderQueue(float[,,] frame) {
            renderFloat.Enqueue(frame);
        }

        // Update is called once per frame
        void Update() {
            if (ArrowCollection.x != x || ArrowCollection.y != y) {
                //arrowCollection.Destroy();
                //Destroy(arrowCollection);
                //ArrowCollection.CreateArrowCollection(x, y, go);
            } else {
                if(renderFloat.Count != 0) {
                    float[,,] render = renderFloat.Dequeue();
                    arrowCollection.MoveX(render);
                    arrowCollection.MoveY(render);
                    arrowCollection.MoveZ(render);
                }
            }
        }
    }
}
#endif