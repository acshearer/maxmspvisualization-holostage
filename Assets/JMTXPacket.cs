using System;

namespace Assets {
    //Maybe this is how you are supposed to do this?
    public class JMTXPacket {
        //Constants
        public const String JIT_MATRIX_PACKET_ID = "JMTX"; //This is what we want
        public const String JIT_MATRIX_LATENCY_PACKET_ID = "JMLP";
        public const String JIT_MESSAGE_PACKET_ID = "JMMP";
        public const Int32 JIT_MATRIX_MAX_DIMCOUNT = 32; //max-sdk-7.33/source/c74support/jit-includes/jit.common.h
                                                         //Structs for header info
        public struct jit_net_packet_header {
            public String id;
            public Int32 size;
        }
        public struct jit_net_packet_matrix { //JMTX
            public jit_net_packet_header hi;
            public Int32 planecount;
            public Type type; //0=char,1=long,2=float32,3=float64
            public Int32 dimCount;
            public Int32[] dim;
            public Int32[] dimStride; //planecount, width*planecount, 30 0's
            public uint datasize;
            public double time;
        }
    }
    public class JMTXPacket<T> : JMTXPacket {

        //We should have a struct for JMLP and JMMP headers but we currently do not use them

        //Other member variables
        jit_net_packet_matrix JMTXHeader;
        private T[] values;

        public JMTXPacket(jit_net_packet_matrix JMTXHeader, T[] values) {
            this.JMTXHeader = JMTXHeader;
            this.values = values;
        }
    }
}