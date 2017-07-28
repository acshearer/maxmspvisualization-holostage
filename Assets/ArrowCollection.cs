using System;
using System.Collections;
using UnityEngine;

namespace Assets {
    public class ArrowCollection : MonoBehaviour {

        GameObject[,] arrowArray;

        public static float x { get; private set; }
        public static float y { get; private set; }

        int p;
        int q;

        const float distance = 0.05f;
        const float scale = 0.01f;
        const float posZ = 2f;

        public static ArrowCollection CreateArrowCollection(int x, int y, GameObject go) {
            ArrowCollection arrowCollection = go.AddComponent<ArrowCollection>();
            arrowCollection.arrowArray = new GameObject[x, y];
            ArrowCollection.x = x;
            ArrowCollection.y = y;
            arrowCollection.p = x / 2;
            arrowCollection.q = y / 2;
            arrowCollection.start();
            return arrowCollection;
        }

        public void start() {
            Material mat = Resources.Load<Material>("HoloToolkit_Default"); //Cache material for all cubes, this operation is slow
            for (var i = 0; i < x; i++) {
                for (var j = 0; j < y; j++) {
                    arrowArray[i, j] = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    Renderer r = arrowArray[i, j].GetComponent<Renderer>();

                    //render game objects in front of user
                    arrowArray[i, j].transform.position = new Vector3((i - p) * distance, ((j - q) * distance), (distance) + posZ);

                    r.material = mat;
                    //render game objects around the centre of game objects
                    arrowArray[i, j].transform.localRotation = new Quaternion(0, 0, 0, 0);
                    arrowArray[i, j].transform.localScale = new Vector3(1 * scale, 1 * scale, 1 * scale);

                    // Cube default coloring
                    if (Config.toroidalColors) {
                        //Similiar to opengl view from toroidal demo
                        r.material.color = new UnityEngine.Color(i/x, j/y, i/x * j/y);
                    } else {
                        r.material.color = Color.red;
                    }
                }
            }
        }

        //Hololens (UWP)
        public void MoveX(float[,] pos) {
            for (int i = 0; i < x; i++) {
                for (int j = 0; j < y; j++) {
                    Vector3 a = arrowArray[i, j].transform.position;
                    a.x = pos[i, j]/4;
                    arrowArray[i, j].transform.position = a;
                }
            }
        }

        public void MoveY(float[,] pos) {
            for (int i = 0; i < x; i++) {
                for (int j = 0; j < y; j++) {
                    Vector3 a = arrowArray[i, j].transform.position;
                    a.y = pos[i, j]/4;
                    arrowArray[i, j].transform.position = a;
                }
            }
        }

        public void MoveZ(float[,] pos) {
            for (int i = 0; i < x; i++) {
                for (int j = 0; j < y; j++) {
                    Vector3 a = arrowArray[i, j].transform.position;
                    a.z = pos[i, j]/4 + posZ;
                    arrowArray[i, j].transform.position = a;
                }
            }
        }

        //Native app for OSX/Windows, projection screen
        public void MoveX(float[,,] pos) {
            for (int i = 0; i < x; i++) {
                for (int j = 0; j < y; j++) {
                    Vector3 a = arrowArray[i, j].transform.position;
                    a.x = pos[0, i, j];
                    arrowArray[i, j].transform.position = a;
                }
            }
        }

        public void MoveY(float[,,] pos) {
            for (int i = 0; i < x; i++) {
                for (int j = 0; j < y; j++) {
                    Vector3 a = arrowArray[i, j].transform.position;
                    a.y = pos[1, i, j];
                    arrowArray[i, j].transform.position = a;
                }
            }
        }

        public void MoveZ(float[,,] pos) {
            for (int i = 0; i < x; i++) {
                for (int j = 0; j < y; j++) {
                    Vector3 a = arrowArray[i, j].transform.position;
                    a.z = pos[2, i, j];
                    arrowArray[i, j].transform.position = a;
                }
            }
        }

        public void Destroy() {
            for (var i = 0; i < x; i++) {
                for (var j = 0; j < y; j++) {
                    Destroy(arrowArray[i, j]);
                }
            }
            //Destroy(this);
        }
    }
}
