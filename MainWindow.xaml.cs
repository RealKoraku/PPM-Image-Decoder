using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PPMDecrypt {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {

        public BitmapMaker publicBitmap = new BitmapMaker(0, 0);
        public MainWindow() {
            InitializeComponent();
        }

        #region File dialogs / Open

        private void muiOpenPPM_Click(object sender, RoutedEventArgs e) {

            OpenFileDialog openFileDialog = new OpenFileDialog();       //create open file dialog

            openFileDialog.DefaultExt = ".PPM";                         //filter to ppm files
            openFileDialog.Filter = "PPM Files (.ppm)|*.ppm";

            bool? result = openFileDialog.ShowDialog();                 //open dialog

            if (result == true) {
                string selectedFile = openFileDialog.FileName;          //store file name

                List<byte[]> RGBvalues = new List<byte[]>();

                string[] PPMdata = GetPPMData(selectedFile);            //convert ppm text file to string array
                BuildPPM(PPMdata);
            }
        }

        #endregion

        #region PPM Data

        private string[] GetPPMData(string path) {
            string[] lines;
            string data = "";
            string[] records;

            FileStream inFile = new FileStream(path, FileMode.Open);    //read data from specified file

            while (inFile.Position < inFile.Length) {
                data += (char)inFile.ReadByte();                        //add each char from text to data string
            }//end while

            inFile.Close();                                             //close file

            lines = data.Split("\n");                                   //split lines into string array

            return lines;
        }

        private void BuildPPM(string[] PPMdata) {
            bool parser;

            string fileType = PPMdata[0];                                           //first line is P3 or P6

            List<byte[]> RGBvalues = new List<byte[]>();

            if ((fileType != "P3" && fileType != "P6") || PPMdata.Length < 5) {     //if not P3 or p6, or length < 5 (no pixel data)

            } else {

                string[] comment = GetComments(PPMdata);                            //gather comments into array

                int line = comment.Length + 1;                                      //line after comments

                string[] imgRes = PPMdata[line].Split(" ");                         //gather resolution info
                string RGBchannel = PPMdata[line += 1];                               //RGB max channel next line

                int resHeight;
                int resWidth;

                parser = int.TryParse(imgRes[0], out resWidth);                    //split height and width
                parser = int.TryParse(imgRes[1], out resHeight);

                if (fileType == "P3") {                                             //determine P3 or P6
                    RGBvalues = ReadP3(PPMdata);                                    //populate RGB values

                } else if (fileType == "P6") {
                    RGBvalues = ReadP6(PPMdata);
                }

                BitmapMaker PPMbitmap = new BitmapMaker(0, 0);
                
                PPMbitmap = BuildBitmap(resHeight, resWidth, RGBvalues, PPMbitmap);    //build bitmap
                publicBitmap = PPMbitmap;
                DisplayBitmap(publicBitmap);                                              //display constructed image
            }
        }

        private string[] GetComments(string[] PPMdata) {
            int comments = 0;

            for (int commentLine = 1; commentLine < PPMdata.Length; commentLine++) {
                if (PPMdata[commentLine][0] == '#') {
                    comments++;
                } else {
                    break;
                }
            }
            string[] comment = new string[comments];

            for (int line = 1; line <= comment.Length; line++) {
                comment[line - 1] = PPMdata[line];
            }
            return comment;
        }

        private List<byte[]> ReadP3(string[] PPMdata) {                     //Read P3
            bool parser;
            string[] comment = GetComments(PPMdata);

            List<byte[]> RGBvalues = new List<byte[]>();

            for (int line = comment.Length + 3; line < PPMdata.Length - 1; line += 0) {      //for each line in ppm text, starting after header data

                byte[] RGB = new byte[3];                                   //byte array for RGB data
                byte RGBbyte = 0;                                           //single byte for R/G/B

                for (int rgb = 0; rgb < RGB.Length; rgb++) {                //for length of RGB data

                    parser = byte.TryParse(PPMdata[line], out RGBbyte);     //convert that line into a byte
                    RGB[rgb] = RGBbyte;                                     //set current R/G/B position to that byte
                    line++;                                                 //go to next line    
                }

                RGBvalues.Add(RGB);                                         //once pixel is populated, add to RGBvalues list
            }
            return RGBvalues;
        }

        private List<byte[]> ReadP6(string[] PPMdata) {                     //Read P6
            bool parser;
            string[] comment = GetComments(PPMdata);

            List<byte[]> RGBvalues = new List<byte[]>();

            char[] binaryData = PPMdata[comment.Length + 3].ToCharArray();  //store binary data chars of ppm text to char array

            for (int bytes = 0; bytes < binaryData.Length; bytes += 0) {    //for each character in the binary data array

                byte[] RGB = new byte[3];                                   //byte array for RGB data
                byte RGBbyte = 0;                                           //single byte for R/G/B

                for (int rgb = 0; rgb < RGB.Length; rgb++) {                //for length of RGB array             

                    RGBbyte = (byte)binaryData[bytes];                      //cast current char to byte, store in R/G/B byte
                    RGB[rgb] = RGBbyte;                                     //set current R/G/B to that byte
                    bytes++;                                                //go to next char in binary data
                }
                RGBvalues.Add(RGB);                                         //once pixel is populated, add to RGBvalues list
            }
            return RGBvalues;
        }

        #endregion

        #region Bitmap

        private BitmapMaker BuildBitmap(int resHeight, int resWidth, List<byte[]> RGBvalues, BitmapMaker PPMbitmap) {

            PPMbitmap = new BitmapMaker(resWidth, resHeight);       //create Bitmap of specified height and width

            int RGBvalIndex = 0;

            for (int y = 0; y < resHeight; y++) {                   //scan bitmap x, y
                for (int x = 0; x < resWidth; x++) {

                    byte RGBr = RGBvalues[RGBvalIndex][0];          //red value is equal to the 1st byte in array of RGBvalIndex
                    byte RGBg = RGBvalues[RGBvalIndex][1];          //blue value is equal to 2nd byte in array
                    byte RGBb = RGBvalues[RGBvalIndex][2];          //green = 3rd byte
                    PPMbitmap.SetPixel(x, y, RGBr, RGBg, RGBb);     //set constructed pixel to current position

                    RGBvalIndex++;                                  //next item in RGBvalues list
                }
            }

            return PPMbitmap;
        }

        private void DisplayBitmap(BitmapMaker PPMbitmap) {
            WriteableBitmap wbmImage = PPMbitmap.MakeBitmap();      //PPMbitmap to writeable bitmap
            imgMain.Source = wbmImage;                              //set image box source to writeable bitmap
        }

        #endregion

        #region Decode

        private string DecodeMessage(BitmapMaker bitmap) {
            BitmapMaker encryptedBitmap = publicBitmap;
            string decodedMessage = "";

            return decodedMessage;
        }

        #endregion

        private void BtnDecode_Click(object sender, RoutedEventArgs e) {
            DecodeMessage(publicBitmap);
        }
    }
}