using System;
using System.Collections.Generic;
using UnityEngine;
#if (WINDOWS_UWP)
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Windows.Storage;
#endif

namespace Assets {
    public class GenArrowUWP : MonoBehaviour {
        private static ArrowCollection arrowCollection;
        private static GameObject go;

        //To collect data
        /*
        private static int[,] values = new int[,]{
                //{ 10 ,10},
                //{ 10 ,50}
                //{ 10 ,100},
                //{ 10 ,250},
                //{ 10 ,500},
                //{ 10 ,750}
                //{ 100 ,100},
                //{ 100 ,125},
                //{ 100 ,150},
                //{ 100 ,175},
                //{ 100 ,200},
                //{ 100 ,225},
                //{ 100 ,250},
                //{ 100 ,275},
                //{ 100 ,300}
            };
        private static long[,,] times;
        private static long timeStart, timeEnd;
        private static int valueIndex, numRun;
        private static int numToRun = 100;
        private static long[,] aggregate;
        */
#if (WINDOWS_UWP)
        private static ConcurrentQueue<List<float[,]>> renderFloat;
        private static JitNetRecvUWP listener;
#endif
        void Start() {
            go = gameObject; //Save gameObject for creating arrow collection
            //arrowCollection = ArrowCollection.CreateArrowCollection(100, 150, gameObject);
#if (WINDOWS_UWP)
            renderFloat = new ConcurrentQueue<List<float[,]>>();
            listener = new JitNetRecvUWP(Config.tcpPort);
#else
            Util.Log("Running in editor not supported");
            Application.Quit();
#endif
        }
#if (WINDOWS_UWP)
        public static void addToRenderQueue(List<float[,]> nextFrame) {
            renderFloat.Enqueue(nextFrame);
        }

        //Logging
        /*
        private Task saveTime(string filename, long start, long end) {
            Task t = Task.Run(async () => {
                try {
                    filename += ".csv";
                    StorageFile textFile = await ApplicationData.Current.LocalFolder.GetFileAsync(filename);
                    await FileIO.AppendTextAsync(textFile, "," + start.ToString() + "," + end.ToString() + "\n");
                } catch {
                    //File does not exist
                    StorageFile textFile = await ApplicationData.Current.LocalFolder.CreateFileAsync(filename);
                    await FileIO.WriteTextAsync(textFile, filename + ",startFrame,endFrame\n");
                    await FileIO.AppendTextAsync(textFile, "," + start.ToString() + "," + end.ToString() + "\n");
                }
            });
            return t;
        }

        private async Task saveTimeArray(long[,,] times) {
            try {
                for (int i = 0; i < times.GetLength(0); i++) {
                    string filename = values[i, 0].ToString() + "x" + values[i, 1].ToString() + ".csv";
                    Util.Log(filename);
                    StorageFile textFile = await ApplicationData.Current.LocalFolder.CreateFileAsync(filename);
                    await FileIO.WriteTextAsync(textFile, values[i, 0] + "x" + values[i, 1] + ",startTime,endTime\n");
                    for (int j = 0; j < times.GetLength(1); j++) {
                        await FileIO.AppendTextAsync(textFile, "," + times[i, j, 0] + "," + times[i, j, 1] + "\n");
                    }
                }
            } catch (Exception e) {
                Util.Log(e.ToString());
            }
        }
        */
        private List<float[,]> generateRandomList(int planeCount, int x, int y) {
            List<float[,]> retVal = new List<float[,]>(planeCount);
            for (int k = 0; k < planeCount; k++) {
                retVal.Add(new float[x, y]);
                for (int i = 0; i < x; i++) {
                    for (int j = 0; j < y; j++) {
                        retVal[k][i, j] = UnityEngine.Random.Range(-.25f, .25f);
                    }
                }
            }
            return retVal;
        }
        // Update is called once per frame
        void Update() {
            /* Collecting FPS data
            /*
#if (WINDOWS_UWP)
            //Start
            times[valueIndex, numRun, 0] = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            if (numRun < numToRun - 1) {
                var toRender = generateRandomList(3, values[valueIndex, 0], values[valueIndex, 1]);
                arrowCollection.MoveX(toRender[0]);
                arrowCollection.MoveY(toRender[1]);
                arrowCollection.MoveZ(toRender[2]);
                times[valueIndex, numRun, 1] = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                numRun++;
            } else {
                numRun = 0;
                valueIndex++;
                if (valueIndex < values.GetLength(0)) {
                    arrowCollection.Destroy();
                    arrowCollection = ArrowCollection.CreateArrowCollection(values[valueIndex, 0], values[valueIndex, 1], go);
                } else {
                    Task t = saveTimeArray(times);
                    while (!t.IsCompleted) {

                    }
                    Application.Quit();
                }
            }
#endif
*/
//To render data
//Currently will set grid size to first packet recieved
            if (renderFloat.IsEmpty) {
                return;
            } else {
                List<float[,]> res;
                if (renderFloat.TryDequeue(out res)) {
                    if(arrowCollection == null) {
                        arrowCollection = ArrowCollection.CreateArrowCollection(res[0].GetLength(0), res[0].GetLength(1), go);
                    }
                    arrowCollection.MoveX(res[0]);
                    arrowCollection.MoveY(res[1]);
                    arrowCollection.MoveZ(res[2]);
                }
            }
        }
#endif
    }
}
