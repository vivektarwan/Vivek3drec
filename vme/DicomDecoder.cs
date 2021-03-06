﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;
using System.Globalization;

namespace vme
{
    class DicomDecoder
    {
        const uint PIXEL_REPRESENTATION = 0x00280103;
        const uint TRANSFER_SYNTAX_UID = 0x00020010;
        const uint MODALITY = 0x00080060;
        const uint SLICE_THICKNESS = 0x00180050;
        const uint SLICE_SPACING = 0x00180088;
        const uint SAMPLES_PER_PIXEL = 0x00280002;
        const uint PHOTOMETRIC_INTERPRETATION = 0x00280004;
        const uint PLANAR_CONFIGURATION = 0x00280006;
        const uint NUMBER_OF_FRAMES = 0x00280008;
        const uint ROWS = 0x00280010;
        const uint COLUMNS = 0x00280011;
        const uint PIXEL_SPACING = 0x00280030;
        const uint BITS_ALLOCATED = 0x00280100;
        const uint WINDOW_CENTER = 0x00281050;
        const uint WINDOW_WIDTH = 0x00281051;
        const uint RESCALE_INTERCEPT = 0x00281052;
        const uint RESCALE_SLOPE = 0x00281053;
        const uint RED_PALETTE = 0x00281201;
        const uint GREEN_PALETTE = 0x00281202;
        const uint BLUE_PALETTE = 0x00281203;
        const uint ICON_IMAGE_SEQUENCE = 0x00880200;
        const uint PIXEL_DATA = 0x7FE00010;

        /*There are three special SQ related Data Elements that are not ruled by the VR encoding rules
        conveyed by the Transfer Syntax. They shall be encoded as Implicit VR. NEMA DICOM 3.0*/
        const string ITEM = "FFFEE000";
        const string ITEM_DELIMITATION = "FFFEE00D";
        const string SEQUENCE_DELIMITATION = "FFFEE0DD";
        //----

        //String dicom;
        const int FIRST_OFFSET = 128;
        const string DICM = "DICM";
        
        DicomDictionary dic;
        /* All that relates to decoding jpg lossless */
        JpegDecode jdec;   // decoder for jpg lossless см. ISO/IS 10918-1 и 10918-2
        private byte[] frag;
       
        public List<string> dicomInfo;
        BinaryReader file;

        bool readingDataElements;
        bool oddLocations;
        bool inSequence;

        public double rescaleIntercept, rescaleSlope;
        //ushort planarConfiguration;

        bool littleEndian = true; // by default

        // bool metaHeader = true;
        int elementLength; // because can be negative despite the fact that is a length value
        uint vr; // Value Representation
        uint tag;

        int min8 = Char.MinValue;
        int min16 = short.MinValue;

        const int
            AE = 0x4145,
            AS = 0x4153,
            AT = 0x4154,
            CS = 0x4353,
            DA = 0x4441,
            DS = 0x4453,
            DT = 0x4454,
            FD = 0x4644,
            FL = 0x464C,
            IS = 0x4953,
            LO = 0x4C4F,
            LT = 0x4C54,
            PN = 0x504E,
            SH = 0x5348,
            SL = 0x534C,
            SS = 0x5353,
            ST = 0x5354,
            TM = 0x544D,
            UI = 0x5549,
            UL = 0x554C,
            US = 0x5553,
            UT = 0x5554,
            OB = 0x4F42,
            OW = 0x4F57,
            SQ = 0x5351,
            UN = 0x554E,
            QQ = 0x3F3F;

        const int IMPLICIT_VR = 0x2D2D; 
        public ushort bitsAllocated;
        public int width;
        public int height;
        private int offset;
        public int nImages;
        public int samplesPerPixel;
        public ushort pixelRepresentation;

        public int location;

        String photoInterpretation;
        public double pixelDepth = 1.0;
        public double pixelWidth = 1.0;
        public double pixelHeight = 1.0;
        public string unit;

        List<byte> pixels8;
        List<ushort> pixels16;
        private int ctrPixels;

        bool delimiter;

        long pos = -1;  // read compressed jpg


        public bool dicomFileReadSuccess;
        public bool compressedImage = false; // True if the image data is compressed, false otherwise.
        public bool dicomDir;
        public double windowCentre, windowWidth;
        public bool signedImage = false; //

        public DicomDecoder()
        {
            dicomInfo = new List<string>();
            dic  = new DicomDictionary();
            jdec = new JpegDecode(); 
            signedImage = false;
            dicomFileReadSuccess = false;
            dicomInfo = new List<string>();

            readingDataElements = true;
            oddLocations = false;
            delimiter = false;

            location = 0;
            bitsAllocated = 0;
            width = 1;
            height = 1;
            offset = 1;
            nImages = 1;
            samplesPerPixel = 1;
            photoInterpretation = "";
            unit = "mm";
            compressedImage = false;
            dicomDir = false;
            windowCentre = 1;
            windowWidth = 1;
            signedImage = false;
        }

        public void EraseFields()
        {
            signedImage = false;
            dicomFileReadSuccess = false;
            location = 0;
            bitsAllocated = 0;
            readingDataElements = true;
            width = 1;
            height = 1;
            offset = 1;
            nImages = 1;
            samplesPerPixel = 1;
            photoInterpretation = "";
            unit = "mm";
            compressedImage = false;
            dicomDir = false;
            windowCentre = 1;
            windowWidth = 1;
            signedImage = false;
        }

        private string GetString(int length)
        {
            byte[] buff = new byte[length];
            file.BaseStream.Position = location;
            int res = file.BaseStream.Read(buff, 0, length);
            location += length;
            if (res != 0)
                return System.Text.ASCIIEncoding.ASCII.GetString(buff);
            else
            {
                return "";
            }
        }

        /* Reading byte from file*/
        private byte ReadByte()
        {
            file.BaseStream.Position = location;
            byte b = file.ReadByte(); // 
            ++location;
            return b;
        }

        /*Reading 16int from file unsigned*/
        private ushort ReadShort()
        {
            byte b0 = ReadByte();
            byte b1 = ReadByte();

            if (littleEndian)
            {
                return (ushort)((b1 << 8) + b0);
            }
            // bigendianTransferSyntax
            else
            {
                return (ushort)((b0 << 8) + b1);
            }
        }

        /*Reading 32int from file*/
        private int ReadInt()
        {
            byte b0 = ReadByte();
            byte b1 = ReadByte();
            byte b2 = ReadByte();
            byte b3 = ReadByte();

            if (littleEndian)
            {
                return (int)((b3 << 24) + (b2 << 16) + (b1 << 8) + b0);
            }
            // bigendianTransferSyntax
            else
            {
                return (int)((b0 << 24) + (b1 << 16) + (b2 << 8) + b3);
            }

        }

        private int ReadLength()
        {
            byte b0 = ReadByte();
            byte b1 = ReadByte();
            byte b2 = ReadByte();
            byte b3 = ReadByte();

            vr = Convert.ToUInt32((b0 << 8) + b1);  //~& big endian

            switch (vr)
            {
                case OB:
                case OW:
                case SQ:
                case UN:

                    if(b2==0 || b3==0)
                        return ReadInt();

                    vr = IMPLICIT_VR;

                    if (littleEndian)
                        return Convert.ToInt32((b3 << 24) + (b2 << 16) + (b1 << 8) + b0);
                    else
                        return Convert.ToInt32((b0 << 24) + (b1 << 16) + (b2 << 8) + b3);

                case AE:
                case AS:
                case AT:
                case CS:
                case DA:
                case DS:
                case DT:
                case FD:
                case FL:
                case IS:
                case LO:
                case LT:
                case PN:
                case SH:
                case SL:
                case SS:
                case ST:
                case TM:
                case UI:
                case UL:
                case US:
                case QQ:

                    // here we have VL for explicit DE with other VR than OB OW..and etc
                    if (littleEndian)
                        return Convert.ToUInt16((b3 << 8) + b2);
                    else
                        return Convert.ToUInt16((b2 << 8) + b3);

                //implicit
                default:
                    vr = IMPLICIT_VR;
                    if (littleEndian)
                        return Convert.ToInt32((b3 << 24) + (b2 << 16) + (b1 << 8) + b0);
                    else
                        return Convert.ToInt32((b0 << 24) + (b1 << 16) + (b2 << 8) + b3);
            }
            
        }

        private uint ReadTag()
        {
            // tag consists from 2 parts:
            // grouptag and elementtag
            ushort b0 = ReadShort();
            ushort b1 = ReadShort();
            string group = b0.ToString("X");
            string element = b1.ToString("X");
            UInt32 z = (Convert.ToUInt32(b0)<<16);
            z = z | b1;
            return z;
            //return Convert.ToUInt32(b0 << 16 | b1);  /// when I'm in sequence
        }

        private string GetHeaderInfo(uint tag, String value)
        {

            string str = tag.ToString("X8"); // get tag in hex notation
            if (str == ITEM_DELIMITATION || str == SEQUENCE_DELIMITATION) // this is the end of SQ element or Item with unknown length
            {
                inSequence = false;
                return null;
            }

            string id = null;

            if (dic.dict.ContainsKey(str)) // if our dictionary contains str data_element
            {
                id = dic.dict[str];
                if (id != null)
                {
                    if (vr == IMPLICIT_VR)
                        vr = (uint)((id[0] << 8) + id[1]); // if VR is implicit, in that case we just copy it from our dictionary
                    id = id.Substring(2); // returns data_element name without VR 
                }
            }

            // if it is an item in sequence
            if (str == ITEM)
                return (id != null ? id : ":null");
            if (value != null)
                return id + ": " + value;

            switch (vr)
            {
                case FD:
                    for (int i = 0; i < elementLength; ++i)
                        ReadByte();
                    break;
                case FL:
                    for (int i = 0; i < elementLength; i++)
                        ReadByte();
                    break;
                case AE:
                case AS:
                case AT:
                case CS:
                case DA:
                case DS:
                case DT:
                case IS:
                case LO:
                case LT:
                case PN:
                case SH:
                case ST:
                case TM:
                case UI:
                    value = GetString(elementLength);
                    break;
                case US:
                    if (elementLength == 2)
                        value = Convert.ToString(ReadShort());
                    else
                    {
                        value = "";
                        int n = elementLength / 2;
                        for (int i = 0; i < n; i++)
                            value += Convert.ToString(ReadShort()) + " ";
                    }
                    break;
                case IMPLICIT_VR:
                    value = GetString(elementLength);
                    if (elementLength > 44)
                        value = null;
                    break;
                case SQ:
                    value = "";
                    bool privateTag = ((tag >> 16) & 1) != 0;
                    if (tag != ICON_IMAGE_SEQUENCE && !privateTag)
                        break;
                    goto default;
                default:
                    location += elementLength;
                    value = "";
                    break;
            }

            if (value != null && id == null && value != "")
                return "---: " + value;
            else if (id == null)
                return null;
            else
                return id + ": " + value;
        }


        private void AddInfo(uint tag, string length)
        {

            string dataElementsName = GetHeaderInfo(tag, length);
            string tagRepX8 = tag.ToString("X");
            string tagRepX8Padded0 = tagRepX8.PadLeft(8, '0');

            string strInfo_DataElementsName;

            if (inSequence && dataElementsName != null && vr != SQ)
                dataElementsName = ">" + dataElementsName;

            if (dataElementsName != null && tagRepX8 != ITEM)
            {
                if (dataElementsName.Contains("---"))
                    strInfo_DataElementsName = dataElementsName.Replace("---", "Private Tag");    //~&
                else
                    strInfo_DataElementsName = dataElementsName;

                dicomInfo.Add(tagRepX8Padded0 + "//" + strInfo_DataElementsName);
            }

            return;
        }

        public bool ReadFile(string filename)
        {
            long offset = Convert.ToInt64(FIRST_OFFSET);
            file = new BinaryReader(File.Open(filename, FileMode.Open, FileAccess.Read));
            file.BaseStream.Seek(offset, SeekOrigin.Begin);
            location += 128;
            if (DICM == GetString(4))
            {
                // MessageBox.Show("This is the file!");
            }
            else
            {
                MessageBox.Show("I can not process this file");
                return false;
            }

            // the real thing that Iam doing here is reading DataElements
            while (readingDataElements)
            {
                // If we've passed header and preamble that means our situation now is in the metaHeader
                // by default Meta header encoded in Explicit VR little endian

                // read tag
                tag = ReadTag();

                // read VR or say that is implicit(watch dicom dictionary)
                elementLength = ReadLength(); // element length can be 16 or 32 or 32 bits length

                // "Undefined" element length.
                // This is a sort of bracket that encloses a sequence of elements.
                if (elementLength == -1 && tag != PIXEL_DATA) // ask yourself about UT and its length
                {
                    elementLength = 0;
                    inSequence = true; // This should be a sequence of elements
                }

                if ((location & 1) != 0)
                    oddLocations = true;

                if (inSequence)  
                {
                    AddInfo(tag, null);                     
                    continue;
                }

                string s;
                switch (tag)
                {
                    case TRANSFER_SYNTAX_UID:
                        s = GetString(elementLength);
                        AddInfo(tag, s); //

                        //1.2.5 = JPEG 12 lossless - CT  
                        if (s.IndexOf("1.2.4") > -1 || s.IndexOf("1.2.5") > -1)
                        {
                            compressedImage = true;
                        }

                        if (s.IndexOf("1.2.840.10008.1.2.2") >= 0)
                        {
                            littleEndian = false;
                        }
                        break;

                    case BITS_ALLOCATED:
                        bitsAllocated = ReadShort();
                        AddInfo(tag, Convert.ToString(bitsAllocated));
                        break;

                    case ROWS:
                        height = ReadShort();
                        AddInfo(tag, Convert.ToString(height));
                        break;

                    case COLUMNS:
                        width = ReadShort();
                        AddInfo(tag, Convert.ToString(width));
                        break;

                    case PIXEL_REPRESENTATION:  //~
                        pixelRepresentation = ReadShort();
                        AddInfo(tag, Convert.ToString(pixelRepresentation));
                        break;

                    case SAMPLES_PER_PIXEL:
                        samplesPerPixel = ReadShort();
                        AddInfo(tag, Convert.ToString(samplesPerPixel));
                        break;

                    case (int)(RESCALE_INTERCEPT):
                        String intercept = GetString(elementLength);
                        rescaleIntercept = Convert.ToDouble(intercept, new CultureInfo("en-US"));
                        AddInfo(tag, intercept);
                        break;
                    case (int)(RESCALE_SLOPE):
                        String slop = GetString(elementLength);
                        rescaleSlope = Convert.ToDouble(slop, new CultureInfo("en-US"));
                        AddInfo(tag, slop);
                        break;

                    case WINDOW_CENTER: //~&
                        String center = GetString(elementLength);
                        int index = center.IndexOf('\\');

                        if (index != -1)
                            center = center.Substring(index + 1);

                        windowCentre = Convert.ToDouble(center, new CultureInfo("en-US"));
                        AddInfo(tag, center);
                        break;

                    case WINDOW_WIDTH:
                        String widthS = GetString(elementLength);
                        index = widthS.IndexOf('\\');

                        if (index != -1) 
                            widthS = widthS.Substring(index + 1);

                        windowWidth = Convert.ToDouble(widthS, new CultureInfo("en-US"));
                        AddInfo(tag, widthS);
                        break;

                    case PIXEL_DATA:
                        // Start of image data
                        if (!compressedImage)
                        {
                            if (elementLength != 0)
                            {
                                this.offset = location;
                                AddInfo(tag, Convert.ToString(location));
                                readingDataElements = false;
                            }
                            else
                                AddInfo(tag, null);
                        }
                        else
                        {
                            this.offset = location;
                            readingDataElements = false;
                        }
                            
                        break;

                    /*Photometric Interpretation (0028,0004)indicates the number of samples per pixel */
                    case PHOTOMETRIC_INTERPRETATION:
                        photoInterpretation = GetString(elementLength);
                        AddInfo(tag, photoInterpretation);
                        break;
                    default:
                        AddInfo(tag, null);
                        break;
                }
            }
            if (compressedImage)
                ReadLosslessPixelData();
            else
                ReadPixelData();
            file.Close();
            return true;
        }

        /*
        public void FileClose() 
        {
            if (file != null)
            {
                file.Close();
                dic = new DicomDictionary();
                jdec = new JpegDecode();
                signedImage = false;
                dicomFileReadSuccess = false;
                dicomInfo = new List<string>();
                location = 0;
                bitsAllocated = 0;
                width = 1;
                height = 1;
                offset = 1;
                nImages = 1;
                samplesPerPixel = 1;
                photoInterpretation = "";
                unit = "mm";
                compressedImage = false;
                dicomDir = false;
                windowCentre = 1;
                windowWidth = 1;
                signedImage = false;
            }

        }
         * */

        public void GetPixels8(ref List<byte> pixels)
        {
            pixels = pixels8;
        }

        public void GetPixels16(ref List<ushort> pixels)
        {
            pixels = pixels16; //&&
        }


        /* When we got to the encoded pixels, store them in an array, for further processing in JpegDecode */
        public void SaveFragment()
        {
            frag = new byte[elementLength];
            for (int i = 0; i < elementLength; i++)
                frag[i] = ReadByte();
        }


        /* Reads bytes from the compressed image fragment*/
        public byte ReadCompressedByte() 
        {
            pos++;
            return frag[pos];
            
        }

        public bool ReadFragment()
        {
            tag=ReadTag();
            elementLength = ReadInt();

            if(tag.ToString("X")==ITEM)
            {
                SaveFragment();

            }
            if (tag.ToString("X") == SEQUENCE_DELIMITATION && elementLength == 0)
            {
                delimiter = true;
            }
            return true;
        }




        public bool ReadLosslessPixelData() 
        {
                bool basic = false;
                tag = ReadTag();
                elementLength = ReadInt();
                if(elementLength!=0)
                {
                    basic = true;
                    //AccOffsets(offsets); // collected in an array offset for fragments encoded image
                }
                else
                {
                    // we do not have an explicit "Basic offset table"
                    basic = false;
                }

                while (!delimiter) 
                {
                    if(basic)
                    {
                       // ReadFragment(offsets[pos]);
                    }
                    else
                    {
                        ReadFragment();
                        //while I believe that all of the image is contained in a single fragment, so that:
                        jdec.JpegDecoder(frag,elementLength);
                    }
                }

                
                return true;
        }

        /*This method reads an image from the clipboard to the DICOM file */
        public bool ReadPixelData()
        {
            // All the parameters that I need to display an image:
            // heigh+, width+, Bits allocated+, bits stored ,Pixel data, spp,bpp
            // For three-dimensional model, these parameters:
            // ImagePosition
            // ImageOrientation
            // SliceThickness

            // If we 8bpp grayscale uncompressed
            if (samplesPerPixel == 1 && bitsAllocated == 8) 
            {
                if (pixels8 != null) 
                    pixels8.Clear();
                pixels8 = new List<byte>();
                ctrPixels = width * height;
                byte[] buf = new byte[ctrPixels];
                file.BaseStream.Position = this.offset;
                file.Read(buf, 0, ctrPixels);
                for (int i = 0; i < ctrPixels; ++i)
                {
                    pixels8.Add((byte)(pixelRepresentation == 1 ? buf[i] : (buf[i] + min8))); // 8&~
                }

            }

            // If we have a 16bpp grayscale uncompressed
            if (samplesPerPixel == 1 && bitsAllocated == 16)
            {
                if (pixels16 != null)
                    pixels16.Clear();
                pixels16 = new List<ushort>();
                int numPixels = width * height;
                byte[] bufByte = new byte[numPixels * 2];
                file.BaseStream.Position = offset;
                file.Read(bufByte, 0, numPixels * 2);
                ushort unsignedS;
                ushort unsignedS1;
                int i, i1;
                byte b0, b1;
                byte[] signedData = new byte[2];

                for (i = 0; i < numPixels; ++i)
                {
                    i1 = i * 2;
                    b0 = bufByte[i1];
                    b1 = bufByte[i1 + 1];
                    unsignedS1 = Convert.ToUInt16((b1 << 8) + b0);
                    if (pixelRepresentation == 0) // Unsigned
                    {
                        signedImage = false;
                        unsignedS = unsignedS1;
                    }
                    else  // Pixel representation  = 1, indicating a 2s complement image
                    {
                        signedImage = true;
                        signedData[0] = b0;
                        signedData[1] = b1;
                        short s8 = System.BitConverter.ToInt16(signedData, 0);
                        int s4 = s8 - min16; ;
                        unsignedS = (ushort)(s4);
                    }
                    pixels16.Add(unsignedS);
                }
            }

            
            return true;
        }
    }
    
}