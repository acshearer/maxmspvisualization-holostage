#if (UNITY_NATIVE)
using System;
using Vuforia;
using UnityEngine;

namespace Assets {
    class BoundingManager : MonoBehaviour {
        private GameObject x, y, depth;
        private ImageTarget it;
        private static Vector3 cubeStartingPos;
        private LineRenderer lr;

        public void Start() {
            it = gameObject.GetComponent<ImageTarget>();
            GameObject[] cubes = GameObject.FindGameObjectsWithTag("ConfigCube");
            x = Array.Find(cubes, o => o.name == "X");
            y = Array.Find(cubes, o => o.name == "Y");
            depth = Array.Find(cubes, o => o.name == "Depth");
        }

        public static void setStartingPos(Vector3 startingPos) {
            cubeStartingPos = startingPos;
        }

        private Vector3[] buildlrArray() {
            Vector3[] vec = new Vector3[16];
            //Cache positions for cubes
            cubeStartingPos = new Vector3(0, 0, 0);
            Vector3 xPos = normalize(x.transform.localPosition) + cubeStartingPos;
            Vector3 yPos = normalize(y.transform.localPosition) + cubeStartingPos;
            Vector3 zPos = normalize(depth.transform.localPosition) + cubeStartingPos;
            Vector3 offset = new Vector3(0, 0, 0); // I have no idea how to get the image target's location in unity, hardcode for now, z=0.5f
            vec[0] = cubeStartingPos + offset;
            vec[1] = xPos;
            vec[2] = xPos + yPos;
            vec[3] = yPos;
            vec[4] = cubeStartingPos + offset;
            vec[5] = zPos;
            vec[6] = xPos + zPos;
            vec[7] = xPos;
            vec[8] = xPos + zPos;
            vec[9] = xPos + yPos + zPos;
            vec[10] = xPos + yPos;
            vec[11] = xPos + yPos + zPos;
            vec[12] = yPos + zPos;
            vec[13] = yPos;
            vec[14] = yPos + zPos;
            vec[15] = zPos;
            return vec;
        }

        private Vector3 normalize(Vector3 vec) {
            if(Math.Abs(vec.x) < 1E-6) {
                vec.x = 0;
            }
            if(Math.Abs(vec.y) < 1E-6) {
                vec.y = 0;
            }
            if(Math.Abs(vec.z) < 1E-6) {
                vec.z = 0;
            }
            return vec;
        }

        public void Update() {
            if (cubeStartingPos != null) {
                // Add to ImageTarget
                if(x.transform.parent.gameObject.GetComponent<LineRenderer>() == null) {
                    lr = x.transform.parent.gameObject.AddComponent<LineRenderer>();
                    lr.material = Resources.Load<Material>("HoloToolkit_Default");
                    lr.useWorldSpace = false;
                    lr.widthMultiplier = 0.1f;
                    lr.startWidth = 0.05f;
                    lr.endWidth = 0.05f;
                   lr.positionCount = 16;
                }
                lr.SetPositions(buildlrArray());
            }
        }
    }
}
#endif
