﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Imaging;




namespace vme
{


    public partial class Main : Form
    {
        public bool readable = false;

        DicomDecoder dec;
        public List<byte> pixels8;
        public List<ushort> pixels16;
        public List<ushort> pixels_volume;
        long[] histogram;
        public uint[] colors; // for transmission to the DVR
        public float[] opacity;
        public int knots_counter; 
        VoxelImage vform;

        private ushort num_of_images;
        private ushort image_number;
        private string safename;

        private bool navi;

        public int imageWidth;
        public int imageHeight;
        public double winWidth;
        public double winCentre;
        public ushort bpp;
        public int spp;
        public bool signedImage;
        public short intercept;
        public short slope;

        private string path;

        public Color inkColor;

        public Main()
        {
            InitializeComponent();
            dec = new DicomDecoder();
            pixels8 = new List<byte>();
            pixels16 = new List<ushort>();
            pixels_volume = new List<ushort>();
            colors = new uint[256];
            opacity = new float[256];

            signedImage = false;
            num_of_images = 0;
            image_number = 0;
            safename = "";
            navi = false;

            inkColor = Color.Red;
        }

        private void openDicom_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();

            ofd.Filter = "All DICOM Files(*.dcm)|*.*";

            if ((ofd.ShowDialog() == DialogResult.OK) && (ofd.FileName.Length > 0))
            {
                pixels16.Clear();
                pixels8.Clear();
                dec.EraseFields();
                ImagePlane.EraseFields();

                Cursor = Cursors.WaitCursor;
                ReadAndDisplayDicomFile(ofd.FileName, ofd.SafeFileName);
                Cursor = Cursors.Default;
                image_label.Text = ofd.FileName;
                num_of_images++;
            }
            ofd.Dispose();
        }

        private void ReadAndDisplayDicomFile(string filename, string name)
        {
            if (readable = dec.ReadFile(filename))
                DisplayData();
            else
                MessageBox.Show("Can not process file");
            return;
        }

        private void DisplayData()
        {
            imageWidth = dec.width;
            imageHeight = dec.height;
            bpp = dec.bitsAllocated; // the number of bits per pixel
            winCentre = dec.windowCentre; // average between the brightest and the most lackluster pixel
            winWidth = dec.windowWidth; // the difference between the brightest and the most lackluster pixel
            spp = dec.samplesPerPixel;
            if (dec.rescaleIntercept < 0)
            {
                dec.signedImage = true;
            }
            signedImage = dec.signedImage;
            ImagePlane.Signed16Image = dec.signedImage; // short or unshort image
            ImagePlane.NewImage = true;
            histogram = new long[256];
            if (spp == 1 && bpp == 8)
            {
                pixels8.Clear();
                pixels16.Clear();
                dec.GetPixels8(ref pixels8);
                if (winCentre == 0 && winWidth == 0)
                {
                    winWidth = 256;
                    winCentre = 128;
                }
                ImagePlane.SetParameters(ref pixels8, imageWidth, imageHeight, winWidth, winCentre, spp, true, this, histogram, inkColor);
            }
            if (spp == 1 && bpp == 16)
            {
                pixels16.Clear();
                pixels8.Clear();
                dec.GetPixels16(ref pixels16);
                intercept = (short)dec.rescaleIntercept;
                slope = (short)dec.rescaleSlope;

                // consider Modality LUT
                if (dec.rescaleIntercept < 0)
                {
                    for (int i = 0; i < pixels16.Count; i++)
                        pixels16[i] = (ushort)(((short)(pixels16[i] * slope + intercept)) + 32768);
                }
                if (winCentre == 0 && winWidth == 0)
                {
                    winWidth = 65536;
                    winCentre = 32768;
                }

                if (!navi)
                {
                    for (int i = 0; i < pixels16.Count; i++)
                    {
                        pixels_volume.Add(pixels16[i]);
                    }
                }

                ImagePlane.SetParameters(ref pixels16, intercept, imageWidth, imageHeight, winWidth, winCentre, true, this, ref histogram, inkColor);
                ColoredTFobj.PassAlong(this);
            }

            /* if we 16bpp lossless CT image */
            if (spp == 1 && bpp == 16 && dec.compressedImage)
            {
                // later to finish reading and recording lossless predictor, despite the fact that the tree is already building the right is the raw data
            }
        }

        private void EraseHistogramArray()
        {
            for (int i = 0; i < 256; i++)
                histogram[i] = 0;
        }

        public void UpdateWindowLevel(int winWidth_from_Canvas, int winCentre_from_Canvas, Imagebpp bpp, long[] histogram)
        {
            int winMin = Convert.ToInt32(winCentre_from_Canvas - 0.5 * winWidth_from_Canvas);
            int winMax = winMin + winWidth_from_Canvas;
            winWidth = winWidth_from_Canvas;
            winCentre = winCentre_from_Canvas;

            int w = (int)winWidth;
            int с = (int)winCentre;

            this.Windowing.SetWindowWidthCentre(winMin, winMax, w, с, bpp, signedImage);
            this.ColoredTFobj.SetParametersHistogram(winMin, winMax, w, с, bpp, signedImage, histogram);
        }

        
        public void UpdateFromColoredTF()
        {
            winWidth = dec.windowWidth;
            winCentre = dec.windowCentre;

            this.ImagePlane.viewcolor = false;
            //EraseHistogramArray();
            this.ImagePlane.SetParameters(ref pixels16, intercept, imageWidth, imageHeight, winWidth, winCentre, false, this, ref  histogram, inkColor);
        }
         

        public void UpdateColorFromHistogram(double width_from_coloredTF, double center_from_coloredTF)
        {
            int width = (int)(width_from_coloredTF);
            int center = (int)(center_from_coloredTF+short.MinValue);

            this.ImagePlane.viewcolor = true;
            //this.ImagePlane.EraseHistogramArray();
            this.ImagePlane.SetParameters(ref pixels16, intercept, imageWidth, imageHeight, width, center, false, this, ref  histogram, inkColor);
        }

        private void MainClose(object sender, FormClosingEventArgs e)
        {
            pixels8.Clear();
            pixels16.Clear();
            ImagePlane.Dispose();
        }

        /*  By default, the*/
        private void Reset_Click(object sender, EventArgs e)
        {
            if (pixels8 != null || pixels16 != null)
            {
                if ((pixels8.Count > 0) || (pixels16.Count > 0))
                {
                    EraseHistogramArray();
                    winWidth = dec.windowWidth;
                    winCentre = dec.windowCentre;
                    ImagePlane.viewcolor = false;
                    if (bpp == 8)
                    {
                        if (spp == 1)
                            ImagePlane.SetParameters(ref pixels8, imageWidth, imageHeight, winWidth, winCentre, spp, false, this, histogram, inkColor);
                    }

                    if (bpp == 16)
                    {
                        ImagePlane.SetParameters(ref pixels16, intercept, imageWidth, imageHeight, winWidth, winCentre, false, this, ref  histogram, inkColor);
                    }
                }
            }
            else
                MessageBox.Show("load DICOM file before restoring settings!");
        }

        private void backward_Click(object sender, EventArgs e)
        {
            if (image_number>1)
            {
                pixels16.Clear();
                dec.EraseFields();
                ImagePlane.EraseFields();
                image_number--;
                image_label.Text = path + image_number;
                safename = "_" + image_number;
                navi = true;
                ReadAndDisplayDicomFile(path + safename, safename);
            }
        }

        private void forward_Click(object sender, EventArgs e)
        {
            if (image_number < num_of_images)
            {
                pixels16.Clear();
                dec.EraseFields();
                ImagePlane.EraseFields();
                image_number++;
                image_label.Text = path + image_number;
                safename = "_" + image_number;
                navi = true;
                ReadAndDisplayDicomFile(path + safename, safename);
            }
        }

        private void inkButton_Click(object sender, EventArgs e)
        {
            ColorDialog inkDialog = new ColorDialog();
            inkDialog.AllowFullOpen = true;
            inkDialog.ShowHelp = true;
            if (inkDialog.ShowDialog() == DialogResult.OK)
            {
                inkColor = inkDialog.Color;
            }
        }

        private void openChest_Click(object sender, EventArgs e)
        {
            path = "E:\\Vivek\\clearcanvas\\brain\\";

            for (int i = 1; i < 190; i++)
            {

                safename = "_" + Convert.ToString(i);
                pixels16.Clear();
                pixels8.Clear();
                dec.EraseFields();
                ImagePlane.EraseFields();
                Cursor = Cursors.WaitCursor;
                ReadAndDisplayDicomFile(path + safename, safename);
                Cursor = Cursors.Default;
                image_label.Text = path + safename;
                num_of_images++;
            }
            image_number = num_of_images;
        }

        private void openKid_Click(object sender, EventArgs e)
        {
            path = "E:\\Vivek\\clearcanvas\\brain\\";

            for (int i = 30; i < 190; i++)
            {

                safename = "-" + Convert.ToString(i);
                pixels16.Clear();
                pixels8.Clear();
                dec.EraseFields();
                ImagePlane.EraseFields();
                Cursor = Cursors.WaitCursor;
                ReadAndDisplayDicomFile(path + safename, safename);
                Cursor = Cursors.Default;
                image_label.Text = path + safename;
                num_of_images++;
            }
            image_number = num_of_images;
        }

        private void volumeReconstruction_Click(object sender, EventArgs e)
        {
            vform = new VoxelImage();
            vform.VoxelImage_Load(pixels_volume, num_of_images, winCentre, winWidth, dec.rescaleIntercept, dec.signedImage, this);
            vform.Idle();
            vform.Show();
        }

        private void Windowing_Load(object sender, EventArgs e)
        {

        }

        private void Main_Load(object sender, EventArgs e)
        {

        }

        

        private void othertoolsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Reconstruction r = new Reconstruction();
            
            r.Show();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            pictureBox1.Visible = false;
            button1.Visible = false;
        }

       


    }
}


