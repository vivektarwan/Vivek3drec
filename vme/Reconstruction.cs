using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;

namespace vme
{
    public partial class Reconstruction : Form
    {
        public Reconstruction()
        {
            InitializeComponent();
        }
        public void ChooseFolder()
        {
            FolderBrowserDialog fD = new FolderBrowserDialog();

            if (fD.ShowDialog() == DialogResult.OK)
            {
                 label2.Text =fD.SelectedPath;
            }
        }
        private void button1_Click(object sender, EventArgs e)
        {
            ChooseFolder();
        }

        private void button5_Click(object sender, EventArgs e)
        {
             string ex2 = label2.Text; 

            // Use ProcessStartInfo class
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.CreateNoWindow = false;
            startInfo.UseShellExecute = false;
            startInfo.FileName = "E:\\VTK\\Build\\bin\\debug\\GPURenderDemo.exe";
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.Arguments = "-DICOM " + ex2 + " -CT_Skin -Clip";

            try
            {
                // Start the process with the info we specified.
                // Call WaitForExit and then the using statement will close.
                using (Process exeProcess = Process.Start(startInfo))
                {
                    exeProcess.WaitForExit();
                }
            }
            catch
            {
                // Log error.
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            string ex3 = label2.Text;

            // Use ProcessStartInfo class
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.CreateNoWindow = false;
            startInfo.UseShellExecute = false;
            startInfo.FileName = "E:\\VTK\\Build\\bin\\debug\\GPURenderDemo.exe";
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.Arguments = "-DICOM " + ex3 + " -CT_Muscle -Clip";

            try
            {
                // Start the process with the info we specified.
                // Call WaitForExit and then the using statement will close.
                using (Process exeProcess = Process.Start(startInfo))
                {
                    exeProcess.WaitForExit();
                }
            }
            catch
            {
                // Log error.
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            string ex4 = label2.Text;

            // Use ProcessStartInfo class
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.CreateNoWindow = false;
            startInfo.UseShellExecute = false;
            startInfo.FileName = "E:\\VTK\\Build\\bin\\debug\\GPURenderDemo.exe";
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.Arguments = "-DICOM " + ex4 + " -CT_Bone -Clip";

            try
            {
                // Start the process with the info we specified.
                // Call WaitForExit and then the using statement will close.
                using (Process exeProcess = Process.Start(startInfo))
                {
                    exeProcess.WaitForExit();
                }
            }
            catch
            {
                // Log error.
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            string ex5 = label2.Text;

            // Use ProcessStartInfo class
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.CreateNoWindow = false;
            startInfo.UseShellExecute = false;
            startInfo.FileName = "E:\\VTK\\Build\\bin\\debug\\GPURenderDemo.exe";
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.Arguments = "-DICOM " + ex5;

            try
            {
                // Start the process with the info we specified.
                // Call WaitForExit and then the using statement will close.
                using (Process exeProcess = Process.Start(startInfo))
                {
                    exeProcess.WaitForExit();
                }
            }
            catch
            {
                // Log error.
            }
        }
    }
}
