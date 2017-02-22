using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Emgu.CV;                  //
using Emgu.CV.CvEnum;           // usual Emgu Cv imports
using Emgu.CV.Structure;        //
using Emgu.CV.UI;               //
using Emgu.CV.Util;
using System.Text.RegularExpressions;

namespace PIW_Mouth_detection
{
    
    public partial class frmMain : Form
    {
        public frmMain()
        {
            InitializeComponent();
            
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void tlpOuter_Paint(object sender, PaintEventArgs e)
        {

        }

        private void btnOpenFile_Click(object sender, EventArgs e)
        {
            DialogResult drChosenFile;

            
            // otwarcie okna dialogowego do wyboru pliku
            drChosenFile = ofdOpenFile.ShowDialog();

            if (drChosenFile != DialogResult.OK || ofdOpenFile.FileName == "")
            {
                lblChosenFile.Text = "file not chosen";
                return;
            }

            Mat imgOriginal;

            try
            {
                imgOriginal = new Mat(ofdOpenFile.FileName);
            }
            catch (Exception ex)
            {
                lblChosenFile.Text = "unable to open image, error: " + ex.Message;
                return;
            }

            if (imgOriginal == null)
            {
                lblChosenFile.Text = "unable to open image";
                return;
            }
            
            // Mat imgRed = new Mat(imgOriginal.Size, DepthType.Cv8U, 1);
            Image<Bgr, byte> img = imgOriginal.ToImage<Bgr,byte>();
            Image<Ycc, Byte> Yimg = img.Convert<Ycc, Byte>();
            Image<Ycc, Byte> imgGray = img.Convert<Ycc, Byte>();
            Image<Gray, Byte> imgY= Yimg[0].Convert<Gray, Byte>(); // Ycc -->    [0] - Luminance, [1] - Cr, [2] - Cb, 
            Image<Gray, Byte> imgCb = Yimg[2].Convert<Gray, Byte>();
            Image<Gray, Byte> imgCr = Yimg[1].Convert<Gray, Byte>();
            Image<Gray, Byte> imgCr2 = imgCr;

            Image<Gray, byte> tempImg = imgY.Convert<Gray,byte>();
            
            imgGray._EqualizeHist();
            
            // Pixel:    0 = black, 255 = white

            Byte[,,] data = Yimg.Data;
            Byte[,,] dataCr = imgCr.Data;
            Byte[,,] dataCr2 = imgCr.Data;
            int rows = img.Rows;
            int cols = img.Cols;
            Byte [,,] data1 = new Byte [rows, cols,1];
            double Cr = 0, Cr2 = 0;

            // wyznaczanie maksymalnej wartości piksela kanału Cr, w celu zlokalizowania ust
            double maxCr = max_pix(dataCr, rows, cols);

            // pętla tworząca obraz końcowy zawierający usta w kolorze białym
            for (int i =0; i<rows; i++)
            {
                for (int j = 0; j<cols; j++)
                {
                    Cr = dataCr[i, j, 0];      // kanał Cr z YCbCr
                    Cr2 = Math.Floor(Math.Pow(Cr, 2)*255/(maxCr*maxCr));
                    dataCr2[i, j, 0] = (byte)(Cr2*Cr2/255);
                    if (dataCr[i, j, 0] > 190) // próg odczytu czerwieni
                        data1[i, j, 0] = 255;
                    else
                        data1[i, j, 0] = 0;               
                }  
            }

            // rysowanie prostokąta wokół ust
            int a = (int)(0.05*rows), b = (int)(0.03*cols);
            int[] srodek = weightPoint(data1, rows, cols);
            Point p1 = new Point { X = srodek[0]+a, Y = srodek[1]-b };
            Point p2 = new Point { X = srodek[0]+a, Y = srodek[1]+b };
            Point p3 = new Point { X = srodek[0]-a, Y = srodek[1]+b };
            Point p4 = new Point { X = srodek[0]-a, Y = srodek[1]-b };

            Point[] pts =new Point[] {p1,p2,p3,p4};
            img.DrawPolyline(pts,true,new Bgr(153,255,51),3);

            imgCr2.Data = dataCr2;
            tempImg.Data = data1;

            CvInvoke.GaussianBlur(tempImg, tempImg, new Size(5, 5), 1.5);


            // Wyświetlenie obraców, kolejno: 
            //  Oryginał, Orginał z oznaczonymi ustami, 
            //  kanał Cr podniesiony do kwadratu i znormalizowany do [0,255],
            //  wynik klasyfikacji obrazu kanału Cr^2  określoną granicą
            ibOriginal.Image = imgOriginal;
            ibCanny.Image = img; 
            imageBox1.Image = imgCr2;
            imageBox2.Image =tempImg;
        }


        // obliczanie maksymalnej i minimalnej wartości poksela obrazu
        public double max_pix(Byte[,,] d, int row, int col)
        {
            double max = 0;
            for (int i = 0; i < row-2; i++)
            {
                for (int j = 0; j < col-2; j++)
                {
                    if (d[i, j, 0] > max)
                        max = d[i, j, 0];
                }
            }
            return max;
        }
        public double min_pix(Byte[,,] d, int row, int col)
        {
            double min = 255;
            for (int i = 0; i < row - 2; i++)
            {
                for (int j = 0; j < col - 2; j++)
                {
                    if (d[i, j, 0] < min)
                        min = d[i, j, 0];
                }
            }
            return min;
        }

        // środek ciężkości obrazu czarno-białego
        public int[] weightPoint(Byte[,,] d, int row, int col)
        {
            int[] weights = { 0, 0 };
            int count = 0;
            for (int i = 0; i < row - 2; i++)
            {
                for (int j = 0; j < col - 2; j++)
                {
                    if (d[i, j, 0] != 0)
                    {
                        weights[1] += i;
                        weights[0] += j;
                        count++;
                    }                       
                }
            }
            weights[0] /= count;
            weights[1] /= count;

            return weights;
        }
    }
}
