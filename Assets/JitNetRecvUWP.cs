#if (WINDOWS_UWP)
using System;
using UnityEngine;
using UnityEngine.Windows;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Windows.Storage.Provider;
using System.Threading.Tasks;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using System.Collections.Generic;
using System.Text;

namespace Assets {
    /// <summary>
    /// This class runs on the Hololens (UWP) and is the network listener.
    /// </summary>
    public class JitNetRecvUWP {
        private StreamSocketListener listener;

        public JitNetRecvUWP(int port) {
            listener = new StreamSocketListener();
            listener.Control.QualityOfService = SocketQualityOfService.LowLatency;
            listener.ConnectionReceived += onConnection;
            listener.Control.KeepAlive = true;
            connect(port);
        }

        private async void onConnection(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args) {
            Util.Log("Starting data read");
            DataReader reader = new DataReader(args.Socket.InputStream);
            await listen(reader);
            while (true) {

            }
        }

        private async Task listen(DataReader reader) {
            //await saveTime();
            while (true) {
                DataReaderLoadOperation loadOp = reader.LoadAsync(8);
                await loadOp;
                if (loadOp.Status == Windows.Foundation.AsyncStatus.Completed) {
                    await getJitHeader(reader);
                    return;
                }
            }
        }

        //TODO: Check if JMTX
        private async Task getJitHeader(DataReader reader) {
            try {
                JMTXPacket.jit_net_packet_header head = new JMTXPacket.jit_net_packet_header();
                head.id = reader.ReadString(4);
                reader.ByteOrder = ByteOrder.LittleEndian;
                head.size = reader.ReadInt32(); //Size of next 'chunk'
            } catch (Exception e) {
                Util.Log(e.ToString());
                listener.Dispose();
            }
            await getJMTXHeader(reader);
        }

        private async Task getJMTXHeader(DataReader reader) {
            JMTXPacket.jit_net_packet_matrix JMTXHeader = new JMTXPacket.jit_net_packet_matrix();
            JMTXHeader.dim = new Int32[JMTXPacket.JIT_MATRIX_MAX_DIMCOUNT];
            JMTXHeader.dimStride = new Int32[JMTXPacket.JIT_MATRIX_MAX_DIMCOUNT];
            try {
                while (true) {
                    DataReaderLoadOperation loadOp = reader.LoadAsync(288);
                    await loadOp;
                    if (loadOp.Status == Windows.Foundation.AsyncStatus.Completed) {
                        break;
                    }
                }
                JMTXHeader.hi.id = reader.ReadString(4);
                reader.ByteOrder = ByteOrder.BigEndian;
                JMTXHeader.hi.size = reader.ReadInt32();
                //Why max???
                JMTXHeader.planecount = reader.ReadInt32();
                JMTXHeader.type = getType(reader.ReadInt32());
                JMTXHeader.dimCount = reader.ReadInt32();
                for (int i = 0; i < JMTXPacket.JIT_MATRIX_MAX_DIMCOUNT; i++) {
                    JMTXHeader.dim[i] = reader.ReadInt32();
                }
                for (int i = 0; i < JMTXPacket.JIT_MATRIX_MAX_DIMCOUNT; i++) {
                    JMTXHeader.dimStride[i] = reader.ReadInt32();
                }
                JMTXHeader.datasize = reader.ReadUInt32();
                JMTXHeader.time = reader.ReadDouble();
            } catch (Exception e) {
                Util.Log(e.ToString());
                listener.Dispose();
            }
            switch (JMTXHeader.type.Name) {
                case "Single":
                    await getMatrixData<float>(reader, JMTXHeader);
                    return;
                default:
                    throw new Exception("Invalid Type");
            }
        }

        private async Task getMatrixData<T>(DataReader reader, JMTXPacket.jit_net_packet_matrix JMTXHeader) {
            uint seekSize = getSizeOf(typeof(float)) * (uint)(JMTXHeader.dimStride[1] / getSizeOf(typeof(float)) - JMTXHeader.dim[0] * JMTXHeader.planecount);
            byte[] temp = new byte[seekSize];

            try {
                while (true) {
                    DataReaderLoadOperation loadOp = reader.LoadAsync(JMTXHeader.datasize);
                    await loadOp;
                    if (loadOp.Status == Windows.Foundation.AsyncStatus.Completed) {
                        break;
                    }
                }
                List<float[]> planes = new List<float[]>(JMTXHeader.planecount);
                int size = 1;
                for (int i = 0; i < JMTXHeader.dim.Length; i++) {
                    if (i > 2 && JMTXHeader.dim[i] != 1) {
                        throw new Exception("Matrix is not 1 or 2 dimensions");
                    }
                    size *= JMTXHeader.dim[i];
                }

                for (int i = 0; i < JMTXHeader.planecount; i++) {
                    planes.Add(new float[size]);
                }

                int offset = 0;
                for (int i = 0; i < JMTXHeader.dim[1]; i++) {
                    for (int j = 0; j < JMTXHeader.dim[0]; j++, offset++) {
                        for (int k = 0; k < JMTXHeader.planecount; k++) {
                            planes[k][offset] = readType<float>(reader);
                        }
                    }
                    reader.ReadBytes(temp);
                }

                var test2D = parse2D(planes, JMTXHeader.dim[0], JMTXHeader.dim[1]);
                GenArrowUWP.addToRenderQueue(test2D);
                await listen(reader);
            } catch (Exception e) {
                Util.Log(e.ToString());
                listener.Dispose();
            }
        }

        private uint getSizeOf(Type T) {
            switch (T.Name) {
                case "Char":
                    return 1;//Not unicode...
                case "Int64":
                    return 8;
                case "Single":
                    return 4;
                case "Double":
                    return 8;
                default:
                    throw new Exception("Invalid Type");
            }
        }

        private List<T[,]> parse2D<T>(List<T[]> list, int width, int height) {
            List<T[,]> retVal = new List<T[,]>(list.Count);
            for (int i = 0; i < list.Count; i++) {
                retVal.Add(new T[width, height]);
            }
            for (int k = 0; k < list.Count; k++) {
                int offset = 0;
                for (int i = 0; i < width; i++) {
                    for (int j = 0; j < height; j++, offset++) {
                        retVal[k][i, j] = list[k][offset];
                    }
                }
            }
            return retVal;
        }

        private T readType<T>(DataReader reader) {
            object retVal;
            Type t = typeof(T);
            if (t.Name.Equals("Char")) {
                retVal = reader.ReadString(1).ToCharArray()[0];
            } else if (t.Name.Equals("Int32")) {
                retVal = reader.ReadInt32();
            } else if (t.Name.Equals("Single")) {
                retVal = reader.ReadSingle();
            } else if (t.Name.Equals("Double")) {
                retVal = reader.ReadDouble();
            } else {
                throw new Exception("Invalid Type");
            }
            return (T)retVal;
        }

        private Type getType(int typeID) {
            switch (typeID) {
                case 0:
                    return typeof(Char);
                case 1:
                    return typeof(Int32);
                case 2:
                    return typeof(float);
                case 3:
                    return typeof(double);
                default:
                    throw new NotImplementedException("Unimplemented type id " + typeID);
            }
        }
        
        //Logging code
        /*
        private async Task saveTime() {
            try {
                StorageFile textFile = await ApplicationData.Current.LocalFolder.GetFileAsync("time" + x + "x" + y + ".csv");
                await FileIO.AppendTextAsync(textFile, DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString() + ",\n");
            } catch {
                //File does not exist
                StorageFile textFile = await ApplicationData.Current.LocalFolder.CreateFileAsync("time" + x + "x" + y + ".csv");
                await FileIO.WriteTextAsync(textFile, "Time\n");
                await FileIO.AppendTextAsync(textFile, DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString() + ",\n");
            }
        }
        */
        
        private async void connect(int port) {
            try {
                await listener.BindServiceNameAsync(port.ToString());
                Util.Log("Listening on port " + port);
            } catch (Exception e) {
                listener.Dispose();
                Util.Log(e.ToString());
            }
        }
    }
}
#endif