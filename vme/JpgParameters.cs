using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace vme
{
    class JpgParameters
    {
        public JpegDictionary jdic;  // dictionary markers

        public JpgParameters(){
            jdic = new JpegDictionary();

            tree = new TBinarySTree();  // new Huffman tree

            ptr = 0;  // a pointer to the array segment ecs
        }

        public ushort commentLength;
        public List<byte> comment;
        public TBinarySTree tree;
        public List<byte> tableHuff;
        public List<string> hcodes;
        /* ВРЕ - Description Huffman table*/      
        public ushort huffmanLength;
        public byte Tc; // Table class 0 = DC, 1 = AC
        public byte Th; // identifier assigned to the tables
        public List<byte> BITS;
        public List<byte> HUFFVAL;
        public List<byte> newlength;

        public int[] MinCode;
        public int[] MaxCode;
        public int[] ValPtr;
        public List<byte> HUFFSIZE;
        public List<int> HUFFCODE;
        public List<int> EHUFFCO;
        public List<byte> EHUFSI;
        public int ptr;

        /*SOF - Description frame */

        public ushort frameLength; // frame length
        public byte P;     // the accuracy of the sample
        public ushort Y;   // if 0 - then the number of lines is determined by the DNL marker and parameters at the end of the first scan (?)
        public ushort X;   // number of samples per line
        public byte Nf;    //the number of components of an image in the frame

        /* Component-spec. parameters - parameters of the components specifications*/
        /*
        public byte C;     // component ID
        public byte H;     // horizontal sampling factor 4bits
        public byte V;     //vertical sampling factor 4bits
        public byte Tq;    // source selector quantization tables
         * */

        public byte[] chvtq;    // here will be entered all CHV Tq for because in the frame header are only Nf pieces

        /*EOF - the end of the description frame*/

        //---------------------------------------------------------------------------------------------------------------------------

        /* SOS -  Description scan */

        public int scanLength;     // length of the frame header
        public int Ns;             // the number of components in the scan image
        public int Ss;             // Used to select the predictor
        public int Se;             //The end of the spectral sampling, lossless should be zero
        public int Ah;             // Successive approximation bit position high 
        public int Al;             // Successive approximation bit position low or point transform 

        public List<byte> esc;
        public List<int> dc;
       
        public int lastK;
        
        /* DNL Segment */
        public ushort dnlLength;
        public ushort numLines;

        /* Component-spec. parameters - parameters of the components specifications */
        /*
        public int Cs;             // Scan component selector
        public int Td;             // DC entropy coding table destination selector
        public int Ta;             // AC entropy coding table destination selector
         * */

        public byte[] ctt;    // here will be entered all CHV Tq for because in the frame header are only Nf pieces

        /* EOS - end describe scan */
    }
}
