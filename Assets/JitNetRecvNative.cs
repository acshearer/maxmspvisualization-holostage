#if (UNITY_NATIVE)
using System;
using System.Threading;
using System.Net;
using System.Net.Sockets;

namespace Assets {
    /// <summary>
    /// This class only allows one packet to be rendered, unfinished.
    /// It is only used to project onto the screens but still needs to be
    /// finished to render live data.
    /// Side note : Please do not use this as refence, it needs a lot of fixing
    /// </summary>
    class JitNetRecvNative {
        private NetworkStream stream;
        private ManualResetEvent ConnectResetEvent = new ManualResetEvent(false);
        private ManualResetEvent ReadResetEvent = new ManualResetEvent(false);

        struct state {
            public byte[] data;
            public JMTXPacket.jit_net_packet_matrix header;
        }

        public JitNetRecvNative(int port) {
            try {
                ConnectResetEvent.Reset();
                ReadResetEvent.Reset();
                TcpListener listener = new TcpListener(IPAddress.Any, port);
                listener.Start();
                listener.BeginAcceptTcpClient(new AsyncCallback(DoAcceptTcpClientCallback), listener);
                Thread t = new Thread(new ParameterizedThreadStart(MREWaitOne));
                t.Start(ConnectResetEvent);
            } catch (Exception e) {
                Util.Log(e.ToString());
            }
        }

        private void MREWaitOne(object mre) {
            var resetEvent = (ManualResetEvent)mre;
            resetEvent.WaitOne();
        }

        private void DoAcceptTcpClientCallback(IAsyncResult ar) {
            TcpListener listener = (TcpListener)ar.AsyncState;
            TcpClient client = listener.EndAcceptTcpClient(ar);
            client.ReceiveBufferSize = int.MaxValue;
            ConnectResetEvent.Set();
            stream = client.GetStream();
            getHeader();
            Util.Log("Listening on port : " + listener.LocalEndpoint);
        }

        private void getHeader() {
            byte[] header = new byte[8];
            stream.BeginRead(header, 0, header.Length, new AsyncCallback(FinishHeaderRead), header);
            Thread t = new Thread(new ParameterizedThreadStart(MREWaitOne));
            t.Start(ReadResetEvent);
        }

        private void FinishHeaderRead(IAsyncResult ar) {
            byte[] header = (byte[])ar.AsyncState;
            stream.EndRead(ar);
            ReadResetEvent.Set();
            parseHeader(header);
        }

        private void parseHeader(byte[] header) {
            string id = System.Text.Encoding.ASCII.GetString(header, 0, 4);
            int size = BitConverter.ToInt32(header, 4);
            if (id == "JMTX" && size == 288) {
                getJMTXHeader();
            }
        }

        private void getJMTXHeader() {
            byte[] JMTXHeader = new byte[288];
            stream.BeginRead(JMTXHeader, 0, JMTXHeader.Length, new AsyncCallback(FinishJMTXHeaderRead), JMTXHeader);
            Thread t = new Thread(new ParameterizedThreadStart(MREWaitOne));
            t.Start(ReadResetEvent);
        }

        private void FinishJMTXHeaderRead(IAsyncResult ar) {
            byte[] JMTXHeader = (byte[])ar.AsyncState;
            stream.EndRead(ar);
            ReadResetEvent.Set();
            parseJMTXHeader(JMTXHeader);
        }

        private void parseJMTXHeader(byte[] JMTXHeader) {
            JMTXPacket.jit_net_packet_matrix header = new JMTXPacket.jit_net_packet_matrix();
            header.dim = new int[JMTXPacket.JIT_MATRIX_MAX_DIMCOUNT];
            header.dimStride = new int[JMTXPacket.JIT_MATRIX_MAX_DIMCOUNT];
            int offset = 0;
            header.hi.id = System.Text.Encoding.ASCII.GetString(JMTXHeader, 0, 4);
            offset += 4;
            header.hi.size = getValueAt<Int32>(JMTXHeader, offset, ref offset);
            header.planecount = getValueAt<Int32>(JMTXHeader, offset, ref offset);
            header.type = getType(getValueAt<Int32>(JMTXHeader, offset, ref offset));
            header.dimCount = getValueAt<Int32>(JMTXHeader, offset, ref offset);
            for (int i = 0; i < JMTXPacket.JIT_MATRIX_MAX_DIMCOUNT; i++) {
                header.dim[i] = getValueAt<Int32>(JMTXHeader, offset, ref offset);
            }
            for (int i = 0; i < JMTXPacket.JIT_MATRIX_MAX_DIMCOUNT; i++) {
                header.dimStride[i] = getValueAt<Int32>(JMTXHeader, offset, ref offset);
            }
            header.datasize = getValueAt<UInt32>(JMTXHeader, offset, ref offset);
            header.time = getValueAt<Double>(JMTXHeader, offset, ref offset);
            if (header.hi.id.Equals("XTMJ")) {
                getData(header);
            }
        }

        private void getData(JMTXPacket.jit_net_packet_matrix header) {
            byte[] data = new byte[header.datasize];
            state st;
            st.data = data;
            st.header = header;
            var res = stream.BeginRead(st.data, 0, st.data.Length, new AsyncCallback(FinishDataRead), st);
            Util.Log(res.AsyncState.ToString());
            Thread t = new Thread(new ParameterizedThreadStart(MREWaitOne));
            t.Start(ReadResetEvent);
        }

        private void FinishDataRead(IAsyncResult ar) {
            state st = (state)ar.AsyncState;
            stream.EndRead(ar);
            ReadResetEvent.Set();
            switch (st.header.type.Name) {
                case "Single":
                    parseData<float>(st);
                    return;
            }
        }

        private void parseData<T>(state st) {
            //switch case on types
            int seekSize = Convert.ToInt32(getSizeOf(typeof(float)) * (st.header.dimStride[1] / getSizeOf(typeof(float)) - st.header.dim[0] * st.header.planecount));
            int size = 1;
            for (int i = 0; i < st.header.dim.Length; i++) {
                if (i > 2 && st.header.dim[i] != 1) {
                    throw new Exception("Matrix is not 1 or 2 dimensions");
                }
                size *= st.header.dim[i];
            }
            float[,] planes = new float[st.header.planecount, size];

            //Check dim
            if (GenArrowNative.x != st.header.dim[0] || GenArrowNative.y != st.header.dim[1]) {
                GenArrowNative.x = st.header.dim[0];
                GenArrowNative.y = st.header.dim[1];
            }

            for(int i = 0;i < st.data.Length; i++) {
                if(st.data[i] == 0) {
                    Util.Log("WHY");
                }
            }

            int readOffset = 0;
            int offset = 0;
            for (int i = 0; i < st.header.dim[1]; i++) {
                for (int j = 0; j < st.header.dim[0]; j++, offset++) {
                    for (int k = 0; k < st.header.planecount; k++) {
                        //Switch case for type
                        planes[k, offset] = getValueAt<float>(st.data, readOffset, ref readOffset);
                    }
                }
                readOffset += seekSize;
            }
            float[,,] planes2D = parse2D<float>(planes, st.header.dim[0], st.header.dim[1]);
            //GenArrowNative.renderFloat.Enqueue(planes2D);
            GenArrowNative.addToRenderQueue(planes2D);
            //Listen until next header
        }

        private T[,,] parse2D<T>(T[,] data, int x, int y) {
            T[,,] retVal = new T[data.GetLength(0),x,y];
            for(int k = 0;k < retVal.GetLength(0); k++) {
                int offset = 0;
                for(int i = 0;i < x; i++) {
                    for(int j = 0;j < y; j++, offset++) {
                        retVal[k, i, j] = data[k, offset];
                    }
                }
            }
            return retVal;
        }

        private T getValueAt<T>(Byte[] data, int offset, ref int newOffset) {
            object retValue;
            Type type = typeof(T);

            if (type.Name.Equals("Int32")) {
                //Wow this is ugly, fix
                if (BitConverter.IsLittleEndian) {
                    retValue = BitConverter.ToInt32(swapEndian(data, offset, 4), 0);
                } else {
                    retValue = BitConverter.ToInt32(data, offset);
                }
                newOffset = offset + 4;
            } else if (type.Name.Equals("Single")) {
                if (BitConverter.IsLittleEndian) {
                    retValue = BitConverter.ToSingle(swapEndian(data, offset, 4), 0);
                } else {
                    retValue = BitConverter.ToSingle(data, offset);
                }
                newOffset = offset + 4;
            } else if (type.Name.Equals("UInt32")) {
                if (BitConverter.IsLittleEndian) {
                    retValue = BitConverter.ToUInt32(swapEndian(data, offset, 4), 0);
                } else {
                    retValue = BitConverter.ToUInt32(data, offset);
                }
                newOffset = offset + 4;
            } else if (type.Name.Equals("Double")) {
                if (BitConverter.IsLittleEndian) {
                    retValue = BitConverter.ToDouble(swapEndian(data, offset, 8), 0);
                } else {
                    retValue = BitConverter.ToDouble(data, offset);
                }
                newOffset = offset + 8;
            } else {
                throw new Exception("Type not supported: " + type.Name);
            }
            return (T)retValue;
        }

        private byte[] swapEndian(byte[] data, int offset, int size) {
            byte[] temp = new byte[size];

            for (int i = 0; i < size; i++) {
                temp[i] = data[(offset + size - 1) - i];
            }
            return temp;
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
    }
}
#endif