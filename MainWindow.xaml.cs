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
        public string globalPath = "";
        public string[] publicHeader;
        public string[] publicFileComments;
        public MainWindow() {
            InitializeComponent();
        }

        #region File dialogs / Open

        private void muiOpenPPM_Click(object sender, RoutedEventArgs e) {
            bool parser;

            OpenFileDialog openFileDialog = new OpenFileDialog();       //create open file dialog

            openFileDialog.DefaultExt = ".PPM";                         //filter to ppm files
            openFileDialog.Filter = "PPM Files (.ppm)|*.ppm";

            bool? result = openFileDialog.ShowDialog();                 //open dialog

            if (result == true) {
                string selectedFile = openFileDialog.FileName;          //store file name

                globalPath = selectedFile;

                string PPMtype = DetermineHeader(selectedFile);

                List<byte[]> RGBvalues = new List<byte[]>();

                if (PPMtype != "P3" && PPMtype != "P6") {
                    //error

                } else {

                    string[] header = new string[0];

                    if (PPMtype == "P3") {

                        string[] PPMdata = GetP3Data(selectedFile);
                        header = BuildHeader(PPMdata);


                        RGBvalues = ReadP3(PPMdata);

                    } else if (PPMtype == "P6") {

                        List<byte> binPPMdata = GetP6Data(selectedFile);
                        header = BuildP6Header(globalPath);

                        RGBvalues = ReadP6(binPPMdata);
                    }

                    string[] imgRes = header[publicFileComments.Length + 1].Split(" ");

                    parser = int.TryParse(imgRes[0], out int imgWidth);                    //split height and width
                    parser = int.TryParse(imgRes[1], out int imgHeight);

                    BitmapMaker imageBmp = BuildBitmap(imgHeight, imgWidth, RGBvalues);
                    DisplayBitmap(imageBmp);
                }
            }
        }

        private string DetermineHeader(string path) {       //grab first line of file
            StreamReader inFile = new StreamReader(path);
            string topLine = inFile.ReadLine();
            inFile.Close();

            return topLine;
        }

        private string[] GetP3Data(string path) {
            string[] lines;
            string data = "";
            string[] records;

            FileStream inFile = new FileStream(path, FileMode.Open);    //read data from specified file

            StringBuilder dataSB = new StringBuilder("");

            while (inFile.Position < inFile.Length) {
                int dataByte = inFile.ReadByte();
                char dataChar = (char)dataByte;
                dataSB.Append(dataChar);
            }

            data = dataSB.ToString();

            lines = data.Split("\n");                                   //split lines into string array

            inFile.Close();
            return lines;
        }

        private List<byte> GetP6Data(string path) {
            List<byte> fileData = new List<byte>();

            FileStream infile = new FileStream(path, FileMode.Open);

            while (infile.Position < infile.Length) {
                int byteInt = infile.ReadByte();
                byte byteData = (byte)byteInt;
                fileData.Add(byteData);
            }
            infile.Close();
            return fileData;
        }

        #endregion

        #region PPM Data

        private string[] BuildHeader(string[] PPMdata) {

            bool parser;

            string fileType = PPMdata[0];                                       //first line is P3 or P6

            List<byte[]> RGBvalues = new List<byte[]>();

            string[] comment = GetComments(PPMdata);                            //gather comments into array
            publicFileComments = comment;

            string[] headerLines = new string[comment.Length + 3];
            int line = comment.Length + 1;                                      //line after comments

            string[] imgRes = PPMdata[line].Split(" ");                         //gather resolution info
            string RGBchannel = PPMdata[line += 1];                             //RGB max channel next line

            int resHeight;
            int resWidth;

            parser = int.TryParse(imgRes[0], out resWidth);                     //split height and width
            parser = int.TryParse(imgRes[1], out resHeight);

            headerLines = CondenseHeader(fileType, comment, imgRes, RGBchannel);

            return headerLines;
        }

        private string[] GetComments(string[] PPMdata) {
            int comments = 0;

            for (int commentLine = 1; commentLine < PPMdata.Length; commentLine++) {
                if (PPMdata[commentLine][0] == '#') {                                   //comments always start with #
                    comments++;
                } else {
                    break;
                }
            }
            string[] comment = new string[comments];

            for (int line = 1; line < comments + 1; line++) {
                comment[line - 1] = PPMdata[line];
            }

            return comment;
        }

        private string[] CondenseHeader(string fileType, string[] comments, string[] imgRes, string channel) {
            string[] headerData = new string[comments.Length + 3];

            headerData[0] = fileType;

            int headerLine = 1;
            for (int line = 0; line < comments.Length; line += 0) {
                headerData[headerLine] = comments[line];
                line++;
                headerLine = line + 1;
            }

            headerData[headerLine] = $"{imgRes[0]} {imgRes[1]}";
            headerLine++;
            headerData[headerLine] = channel;
            return headerData;
        }

        private List<byte[]> ReadP3(string[] PPMdata) {                     //Read P3
            bool parser;
            string[] comment = publicFileComments;

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

        private string[] BuildP6Header(string path) {
            string[] PPMdata;
            string data = "";

            FileStream inFile = new FileStream(path, FileMode.Open);    //read data from specified file

            StringBuilder dataSB = new StringBuilder("");

            while (inFile.Position < inFile.Length) {
                int dataByte = inFile.ReadByte();
                char dataChar = (char)dataByte;
                dataSB.Append(dataChar);
            }
            inFile.Close();

            data = dataSB.ToString();
            PPMdata = data.Split("\n");                                   //split lines into string array

            string[] headerLines = BuildHeader(PPMdata);

            return headerLines;
        }

        private List<byte[]> ReadP6(List<byte> PPMdata) {                     //Read P6
            bool parser;

            List<byte> headerBytes = new List<byte>();                        //store header as byte array
            List<byte> byteValues = new List<byte>();                         //store binary RGB as byte array

            bool headerComplete = false;
            int lineCount = 0;                                                //count new lines

            for (int val = 0; headerComplete == false; val++) {               //scan PPMdata until headerComplete = true
                if (PPMdata[val] == 10) {                                     //if that byte == LineFeed
                    lineCount++;                                              //add to line count
                }
                headerBytes.Add(PPMdata[val]);                                //add byte to headerBytes

                if (lineCount == publicFileComments.Length + 3) {             //total amount of header lines
                    headerComplete = true;                                    //header is complete
                }
            }

            List<byte[]> RGBvalues = new List<byte[]>();

            for (int i = headerBytes.Count; i < PPMdata.Count; i += 3) {      //populate list with byte arrays (pixels)
                byte[] RGB = new byte[3];

                RGB[0] = PPMdata[i];

                RGB[1] = PPMdata[i + 1];

                RGB[2] = PPMdata[i + 2];

                RGBvalues.Add(RGB);
            }
            return RGBvalues;
        }

        #endregion

        #region Bitmap

        private BitmapMaker BuildBitmap(int resHeight, int resWidth, List<byte[]> RGBvalues) {

            BitmapMaker PPMbitmap = new BitmapMaker(resWidth, resHeight);       //create Bitmap of specified height and width

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
            publicBitmap = PPMbitmap;
            imgMain.Source = wbmImage;                              //set image box source to writeable bitmap
        }

        #endregion

        #region Decode

        private string DecodeMessage(BitmapMaker PPMbitmap) {
            bool firstPixel = true;
            BitmapMaker encryptedBitmap = publicBitmap;
            char[] decryptionChars = BuildChars();
            string decodedMessage = "";

            int msgLength = FindMsgLength(encryptedBitmap);

            double yInc = PPMbitmap.Height / 16;            //which pixels to check
            double xInc = PPMbitmap.Width / 16;

            yInc = Math.Floor(yInc);                        //always round decimal down
            xInc = Math.Floor(xInc);

            int xStart = 0;
            int RGBIndex = 2;

            int y = 0;                                      //x, y start at 0 (topleft pixel)
            int x = 0;

            int msgCount = 0;

            for (x = xStart; x < PPMbitmap.Width; x += 0) {

                if (firstPixel) {
                    x = 0;
                    y = 0;
                    firstPixel = false;
                }

                else if (y == PPMbitmap.Height && x == PPMbitmap.Width) {
                    break;
                } else if ((x + xInc) > PPMbitmap.Width-1) {
                    x = 0;
                    y += (int)yInc;
                } else {
                    x += (int)xInc;
                }

                if (y > PPMbitmap.Height - 1) {
                    break;
                }

                byte[] pixelData = PPMbitmap.GetPixelData(x, y);

                RGBIndex = DecideRGBValue(RGBIndex);

                int modVal = pixelData[RGBIndex];

                decodedMessage += decryptionChars[modVal];
                msgCount++;
                if (msgCount == msgLength) {
                    break;
                }
            }
            return decodedMessage;
        }

        private int DecideRGBValue(int RGBIndex) {
            if (RGBIndex == 2) {
                RGBIndex = 0;
            } else {
                RGBIndex++;
            }
            return RGBIndex;
        }


        private char[] BuildChars() {               //create array of A-Z, 0-9, and some punctuation, repeating until it reaches 256
            char[] encryptionChars = { 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z', ' ', '.', ',', '!', '?', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };   //if byte / i == wholeNum
            char[] encryption = new char[256];

            int j = 0;

            for (int i = 0; i < encryption.Length; i++) {

                encryption[i] = encryptionChars[j];     //populate array with repeating loop

                if (j == encryptionChars.Count() - 1) {
                    j = 0;
                } else {
                    j++;
                }
            }
            return encryption;
        }

        private int FindMsgLength(BitmapMaker bitmap) {
            byte[] pixelData = bitmap.GetPixelData(0, 0);
            int length = pixelData[2];

            return length + 1;
        }

        #endregion

        private void BtnDecode_Click(object sender, RoutedEventArgs e) {
            string decodedMessage = DecodeMessage(publicBitmap);
            TxtBoxMessage.Text = decodedMessage;
        }
    }
}