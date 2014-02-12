using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;


namespace vme
{
    enum Marker 
    {
        FrameHeader = 0,
        ScanHeader = 1,
        ImageStart =3,
        ImageEnd = 4,
        Dummy = 5,
        HuffmanTables =6,
        Comment =7,
        DNLSegment =8,
        Restart=9,
        RestartInterval=10,

    };

    class JpegDecode
    {
        public JpgParameters property;
        private int  pos;
        private Marker type;

        public JpegDecode() 
        {
            property = new JpgParameters();
            type = new Marker();
            pos = 0;
        }

        private Marker RetrieveMarkerType(byte b0, byte b1)
        {
            string h0 = b0.ToString("X");
            string h1 = b1.ToString("X");
            string hh = h0 + h1;
            if (property.jdic.jdict.ContainsKey(h0 + h1))
            {
                switch (h0 + h1) 
                {
                    case "FFFE": return Marker.Comment;
                    case "FFC4": return Marker.HuffmanTables;
                    case "FFC0":
                    case "FFC1":
                    case "FFC2":
                    case "FFC3":
                    case "FFC5":
                    case "FFC6":
                    case "FFC7":
                        return Marker.FrameHeader;
                    case "FFDA":
                        return Marker.ScanHeader;
                    case "FFDC":
                        return Marker.DNLSegment;
                    case "FFD0":
                    case "FFD1":
                    case "FFD2":
                    case "FFD3":
                    case "FFD4":
                    case "FFD5":
                    case "FFD6":
                    case "FFD7":
                        return Marker.Restart;
                    case "FFD9":
                        return Marker.ImageEnd;
                    case "FFDD":
                        return Marker.RestartInterval;

                    default: return Marker.Dummy;
                }
            }

            return Marker.Dummy;
        
        }

        /* Retrieves the frame header image */
        private void RetrieveFrameHeader(byte[] frag, ref int i)
        {
            byte k=0;
            byte b0 = frag[i];
            i++;
            byte b1 = frag[i];
            property.frameLength = Convert.ToUInt16((b0 << 8 )+ b1);
            i++;
            property.P = frag[i];
            i++;
            b0 = frag[i];
            i++;
            b1 = frag[i];
            property.Y = Convert.ToUInt16((b0 << 8 ) + b1);
            i++;
            b0 = frag[i];
            i++;
            b1 = frag[i];
            property.X = Convert.ToUInt16((b0 << 8 ) + b1);
            i++;
            b0 = frag[i];
            property.Nf = b0;
            i++;
            property.chvtq = new byte[property.Nf*4];

            for (byte j = 0; j < property.Nf; j++) 
            {
                property.chvtq[k] = frag[i];
                i++;
                k++;
                b0 = (byte) (frag[i] >> 4);
                property.chvtq[k] = b0;
                k++;
                b1 = (byte)(frag[i] << 4);
                property.chvtq[k] = (byte) (b1 >> 4);
                k++;
                i++;
                property.chvtq[k] = frag[i];

            
            }
        
        }

        /* Retrieve entropy codede segment*/
        private void RetrieveECS(byte[] frag, ref int i, int elementLength)
         {
             property.esc = new List<byte>();
             byte b0;
             byte b1;

             while ((i < elementLength - 1) && (type == Marker.ScanHeader))  // until we reached the end of Jpeg file
             {
                 b0 = frag[i];
                 b1 = frag[i + 1];
                 // согласно ISO 10918-1 маркер может быть только "FF + 1..254"
                 if (b0 == 255 && b1 != 0 && b1 != 255)
                 {
                     type = RetrieveMarkerType(b0, b1);
                     i += 2; //Point to the item data immediately after the marker
                 }
                 else
                 {
                     property.esc.Add(b0);
                     i++;
                 }
             }
         
         }

        /* Retrieves header scan images */
         private void RetrieveScanHeader(byte[] frag, ref int i, int elementLength) 
        {
            byte k = 0;
            byte b0 = frag[i];
            i++;
            byte b1 = frag[i];
            property.scanLength = Convert.ToUInt16((b0 << 8) + b1);
            i++;
            property.Ns = frag[i];
            i++;
            property.ctt = new byte[property.Ns*3];

            for (byte j = 0; j < property.Ns; j++)
            {
                property.ctt[k] = frag[i];
                i++;
                k++;
                b0 = (byte)(frag[i] >> 4);
                property.ctt[k] = b0;
                k++;
                b1 = (byte)(frag[i] << 4);
                property.ctt[k] = (byte)(b1 >> 4);
                k++;
                i++;

            }

            property.Ss = frag[i]; // in lossless mode of operations this is a predictor
            i++;
            property.Se = frag[i]; // in lossless mode of operations this has no meaning
            i++;
            b0 = (byte)(frag[i] >> 4);
            property.Ah = b0;
            b1 = (byte)(frag[i] << 4);
            property.Al = (byte)(b1 >> 4);  // in lossless mode of operations, this parameter specifies the point transform Pt.
            i++;

            // extraction ECS
            RetrieveECS(frag, ref  i, elementLength);
        
        }

         /* DNL segment provides a mechanism to define or display the number of lines per frame.   */
        private void RetrieveDNLSegment(byte[] frag, ref int i)
        {
            byte b0 = frag[i];
            i++;
            byte b1 = frag[i];
            property.dnlLength = Convert.ToUInt16((b0 << 8) + b1);
            i++;
            b0 = frag[i];
            i++;
            b1 = frag[i];
            property.numLines = Convert.ToUInt16((b0 << 8) + b1);
            i++;

            
        }
        /* Build the Huffman tree for the two tables */
         /* Algorithm in which we host are, always trying to add value to the left branch.
          * If it is busy, then the right. And if there is no room, then go back up a level and try out.
          * Stop at the level necessary to equal the length of the code. The left branch corresponds to a value of 0, the right one. * /

         /* Do not need every time to start from the top. Added value - go back to a higher level.
          * The right branch there? If yes, then go up again.
          * If not, create the right branch and go there */

        private void BuildHuffmanTree(ref TreeNode root, ref int ctr, int acc, ref int path, ref int i, string code)
        {

            while (ctr < acc) // until we have completed all the tree
            {
                if (root == null)
                {
                    root = new TreeNode();
                    path++;
                    // If we have reached the desired depth of the tree (that is, the length of the code, which falls on value)
                    if (path == property.newlength[i])
                    {
                        root.value = property.HUFFVAL[i];
                        root.code = code;
                        property.hcodes.Add(code);
                        ctr++;
                        i++;
                        path--;
                        return;
                    }
                }
                else
                {
                    BuildHuffmanTree(ref root.left, ref ctr, acc, ref path, ref i, code + "0");  // left
                    if (root.right == null)
                    {
                        BuildHuffmanTree(ref root.right, ref ctr, acc, ref path, ref i, code + "1"); // straight
                    }
                    path--;
                    return;
                }
            }
        }

        /* rebuild the array to a Huffman tree*/
        private void RebuildArray()
        {
            byte j=0;
            property.newlength = new List<byte>();
            for (byte i = 0; i < property.BITS.Count; i++ )
            {
                if (property.BITS[i] != 0)
                {
                    if (property.BITS[i] > 1)
                    {
                        for (byte k = 0; k < property.BITS[i] ; k++) 
                        {
                            property.newlength.Add(i);
                            j++;
                        }
                    }
                    else
                    {
                        property.newlength.Add(i);
                        j++;
                    }
                }
            }
            
        }

        private void GenerateSizeTable()
        {
            property.HUFFSIZE = new List<byte>();
            int k = 0;
            byte length = 1;
            int j = 1;

            while (length < 16) 
            {
                if (j > property.BITS[length])
                {
                    length++;
                    j = 1;
                }
                else 
                {
                    property.HUFFSIZE.Add(length);
                    k++;
                    j++;
                }
            
            }
            property.HUFFSIZE.Add(0); //!!!
            property.lastK = k; //!!!

        }

        private void GenerationOfHuffmanCodes() 
        {
            int code=0;
            int k = 0;
            property.HUFFCODE = new List<int>();
            byte esel = property.HUFFSIZE[0];
            while (true) 
            {
                property.HUFFCODE.Add(code);
                code++;
                k++;
                if (property.HUFFSIZE[k] != esel)
                {
                    if (property.HUFFSIZE[k] == 0)
                    {
                        return;
                    }
                    else 
                    {
                        while(property.HUFFSIZE[k]!=esel)
                        {
                            code = code << 1;
                            esel++;
                        }
                    }
                }
            
            }

        
        }

        private void Ordering()
        {
            int k=0;
            int length;
            property.EHUFFCO = new List<int>();
            property.EHUFSI = new List<byte>();
            
            while(k<property.lastK)
            {
                length = property.HUFFVAL[k];
                property.EHUFFCO.Add(property.HUFFCODE[k]);
                property.EHUFSI.Add(property.HUFFSIZE[k]);
                k++;
            }

        }

        /*rebuild the array to a Huffman tree*/
        private void RetrieveHuffmanTable(byte[] frag, ref int i)
        {
            Marker type2=Marker.Dummy;
            property.tableHuff = new List<byte>();
            property.BITS = new List<byte>();
            property.HUFFVAL = new List<byte>();
            property.hcodes = new List<string>();
            byte b0;
            byte b1;
            int acc=0;
            ushort j = 0;
            while (type2 == Marker.Dummy)
            {   
                b0=frag[i];
                b1=frag[i+1];
                if (b0 == 255 && b1 != 0 && b1 != 255)
                {
                    type2 = RetrieveMarkerType(b0, b1);
                }

                if (type2 == Marker.Dummy)
                {
                    property.tableHuff.Add(frag[i]);
                    i++;
                }
              
            }
            b0 = property.tableHuff[j];
            j++;
            b1 = property.tableHuff[j];
            property.huffmanLength = Convert.ToUInt16((b0<<8)+b1);
            j++;

            b0 = (byte)(property.tableHuff[j] >> 4);
            property.Tc = b0;
            b1 = (byte)(property.tableHuff[j] << 4);
            property.Th = (byte)(b1 >> 4);
            j++;
            property.BITS.Add(0);  // Hack table HUFFSIZE
            for (byte k = 0; k < 16; k++)
            {
                property.BITS.Add(property.tableHuff[j]);
                if(property.tableHuff[j]!=0)
                    acc+=property.tableHuff[j];
                j++;
            }
            for (int k = 0; k < acc; k++)
            {
                property.HUFFVAL.Add(property.tableHuff[j]);
                j++;
            }

            int path=-1;
            int ii=0;
            int ctr=0;
            string code = "";
            RebuildArray();

            GenerateSizeTable();// HUFFSIZE
            GenerationOfHuffmanCodes(); // HUFFCODES
            BuildHuffmanTree(ref property.tree.root, ref  ctr, acc, ref  path, ref  ii,code);
            Ordering();


        }

        private void RetrieveComment(byte[] frag, ref int i)
        {
            byte b0 = frag[i];
            i++;
            byte b1 = frag[i];
            property.comment = new List<byte>();
            property.commentLength = Convert.ToUInt16((b0<<8)+b1);
            i++;
            for (ushort j = 0; j < property.commentLength-2; j++)
            { 
                property.comment.Add(frag[i]);
                i++;
            }

        
        }

        /*  Traversing the tree Huffman while reading ECS */
        private void GoGoTree(ref TreeNode root, byte bit, ref bool node, ref int val)
        {
            if (bit == 0)
            {
                if (root.left != null)
                {
                    root = root.left;
                    if (root.left == null && root.right == null)
                    {
                        node = true;
                        val = root.value;
                        return;
                    }

                }

            }
            if (bit == 1)
            {
                if (root.right != null)
                {
                    root = root.right;
                    if (root.left == null && root.right == null)
                    {
                        node = true;
                        val = root.value;
                        return;
                    }

                }

            }
        
        }

        private void CheckHuffCode(string acc, ref bool node,ref int val)
        {
            for (int i = 0; i < property.hcodes.Count; i++) 
            {
                if (acc == property.hcodes[i])
                {
                    val = property.HUFFVAL[i];
                    node = true;

                }
            }
        
        }

        private void DecoderTableGeneration() 
        {
            property.MinCode = new int[17];
            property.MinCode[0] = -1;
            property.MaxCode = new int[17];
            property.MaxCode[0] = -1;
            property.ValPtr = new int[17];  // the index of the beginning of the list of values ​​in HUFFVAL codes are decoded length length
            

            int length = 0;
            int j = 0;
            
            while(true)
            {
                length++;
                if(length>16)
                    return;
                else
                {
                    if(property.BITS[length]==0)
                    {
                        property.MaxCode[length] = -1;
                        property.MinCode[length] = -1;

                    }
                    else// нет
                    {
                        property.ValPtr[length] = j; // the index of the beginning of the list of values ​​in HUFFVAL codes are decoded length length
                        property.MinCode[length] = property.HUFFCODE[j]; // write minimal code for a given length
                        j = j + property.BITS[length] - 1; //range which contains the codes for a given length
                        property.MaxCode[length] = property.HUFFCODE[j]; //write the code for the maximum length
                        j++;

                    }
                   
                }
            }

        }

        private void DecodeCycle() 
        {
            property.dc = new List<int>();
            byte cnt = 0;
            int T = 0;

            while(property.ptr < property.esc.Count)
            {
                property.dc.Add(Decode(ref cnt));
            }
        }
        
        /* The DECODE procedure decodes an 8-bit value  which,  for  the DC coefficient, determines the difference magnitude category   ISO 10918-1 */
        /* DECODE procedure decodes 8 bit value which, for the DC coefficient is determined difference magnitude category ISO 10918-1*/
        
        private int Decode(ref byte cnt) 
        {
            int length = 1; // code length
            byte val;
            byte by1 = 0;
            int code = NextBit(ref cnt, ref by1); //
            int j;

            while (code > property.MaxCode[length]) 
            {
                length++;
                code = (code << 1) + NextBit(ref cnt, ref by1); //
                
            }

            j = property.ValPtr[length];
            j = j + code - property.MinCode[length];
            val = property.HUFFVAL[j];

            return val;
        }
         
        
       /* Places next ssss bits into the low order of bits with MSB first. It calls NEXTBIT and it returns the value of DIFF to the calling procedure  */
        /*
        private int ReceiveSSSS(int ssss)
        {
            int l = 0;
            int v = 0;
            while(l!= ssss) 
            {
                v = (v << 1) + NextBit(); //  considering the input parameter
                l++;
            }
            return v;
        }
         * */


        private byte NextByte() 
        {
            if (property.ptr < property.esc.Count) 
            {
                property.ptr++;
                return property.esc[property.ptr-1];
                
            }
            return 0; // 
        }

        /* NEXTBIT reads the next bit of compressed data and passes it to higher level routines. It also intercepts and removes stuff bytes and detects markers.
         NEXTBIT reads the bits of a byte starting with a MSB
         */
        private int NextBit(ref byte cnt, ref byte by1) // take into account the previous byte!
        {

            int i = 0;
            byte by2=0;
            int bit=0;

            if (cnt == 0)
            {
                by1 = NextByte();
                cnt = 8;
                if (by1 == 255) 
                {
                    by2 = NextByte();
                    if (by2 != 0)
                    {
                        //Error
                        // Process DNL marker
                    }
                    else//yes
                    {
                        
                    }
                }

            }
            //no
            
            bit = by1 >> 7;
            cnt--;
            by1 = (byte)(by1 << 1);

            return bit;
        }
        
        
        public void JpegDecoder(byte[] frag,int elementLength)
        {

            byte b0;
            byte b1;
            int i = 0;

            while ((i < elementLength - 1) && (type != Marker.ImageEnd))  // until we reached the end of Jpeg file
            {
                
                    b0 = frag[i];
                    b1 = frag[i + 1];
                    // according to ISO 10918-1 marker can only be "FF + 1..254"
                    if (b0 == 255 && b1 != 0 && b1 != 255)
                    {
                        type = RetrieveMarkerType(b0, b1);
                        i += 2; // Point to the item data immediately after the marker
                    }
                    else
                        i++;
                    switch (type)
                    {
                        //case Marker.ImageStart:  
                        case Marker.Comment: 
                            {
                                RetrieveComment(frag, ref i);
                                type = Marker.Dummy;
                                continue;
                            }
                        case Marker.FrameHeader: 
                            {
                                RetrieveFrameHeader(frag, ref i);
                                type = Marker.Dummy;
                                continue; 
                            }
                        case Marker.ScanHeader: 
                            { RetrieveScanHeader(frag,ref i, elementLength);
                              type = Marker.Dummy;
                              continue;
                            }
                        case Marker.DNLSegment: 
                            {
                                RetrieveDNLSegment(frag, ref i);
                                type = Marker.Dummy;
                                continue;
                                
                            }
                        case Marker.HuffmanTables:
                            {
                                RetrieveHuffmanTable(frag, ref i);
                                type = Marker.Dummy;
                                continue;
                            }
                        case Marker.ImageEnd: return;
                        default: { pos++; continue; }
                    }
                
            }
            //Read the entire file, now decode ECS
            //DecodeECS();
            DecoderTableGeneration();
            DecodeCycle();
        
        }


    };
}
