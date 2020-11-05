using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using Emgu.Util;

namespace FetchRig6
{
    public partial class ChessboardCalibration : Form
    {
        string imageFolder;
        string cameraMatrixFolder;
        bool saveCamMatrix = false;
        string[] imageNames;
        int nImages;
        static List<Image<Gray, byte>> allImages;
        Size imageSize;
        List<VectorOfPointF> foundFastCorners;

        int idx;
        int _internalCornersWidth = 8;
        int _internalCornersHeight = 6;
        int _nInternalCorners;

        List<int> foundBoardIdx;
        int nBoardsFound;

        private float _squareSize;
        private Size _patternSize;

        MCvPoint3D32f[][] _cornersObjectArray;
        PointF[][] _cornersPointsArray;
        VectorOfPointF[] _cornersPointsVec;

        readonly Mat _cameraMatrix = new Mat(3, 3, DepthType.Cv64F, 1);
        readonly Mat _distCoeffs = new Mat(8, 1, DepthType.Cv64F, 1);

        public ChessboardCalibration()
        {
            InitializeComponent();

            imageFolder = @"D:\ChessBoardImages";
            cameraMatrixFolder = @"D:\ChessBoardImages\CameraMatrix";
            imageNames = Directory.GetFiles(path: imageFolder);
            nImages = imageNames.Length;
            allImages = new List<Image<Gray, byte>>(capacity: nImages);

            _squareSize = 25.0f;  // side length of chessboard square in millimeters
            _patternSize = new Size(width: _internalCornersWidth, height: _internalCornersHeight);
            _nInternalCorners = _internalCornersWidth * _internalCornersHeight;

            Console.WriteLine("nImages: {0}", nImages.ToString());
            calibration_imageNumUpDown.Minimum = 0;
            calibration_imageNumUpDown.Maximum = nImages - 1;

            foundBoardIdx = new List<int>(capacity: nImages);
            foundFastCorners = new List<VectorOfPointF>(capacity: nImages);

            for (int i = 0; i < nImages; i++)
            {
                allImages.Add(new Image<Gray, byte>(fileName: imageNames[i]));

                VectorOfPointF _corners = new VectorOfPointF();
                bool _find = CvInvoke.FindChessboardCorners(image: allImages[i], patternSize: _patternSize, corners: _corners, flags: CalibCbType.FastCheck);
                Console.WriteLine("imageNum: {0}    FastCheck Chessboard Found: {1}", i.ToString(), _find.ToString());

                if (_find)
                {
                    CvInvoke.FindChessboardCorners(image: allImages[i], patternSize: _patternSize, corners: _corners, flags: CalibCbType.Accuracy);
                    foundBoardIdx.Add(i);
                    foundFastCorners.Add(_corners);
                }
            }

            imageSize = new Size(width: allImages[0].Width, height: allImages[0].Height);

            nBoardsFound = foundBoardIdx.Count();
            _cornersObjectArray = new MCvPoint3D32f[nBoardsFound][];
            _cornersPointsArray = new PointF[nBoardsFound][];
            _cornersPointsVec = new VectorOfPointF[nBoardsFound];

            var objectList = new List<MCvPoint3D32f>();
            for (int j = 0; j < _internalCornersWidth; j++)
            {
                for (int k = 0; k < _internalCornersHeight; k++)
                {
                    objectList.Add(new MCvPoint3D32f(x: j * _squareSize, y: k * _squareSize, z: 0.0F));
                }
            }

            for (int i = 0; i < nBoardsFound; i++)
            {
                // for higher corner accuracy
                CvInvoke.CornerSubPix(allImages[foundBoardIdx[i]], foundFastCorners[i], win: new Size(11, 11), zeroZone: new Size(-1, -1), criteria: new MCvTermCriteria(maxIteration: 30, eps: 0.1));
                Console.WriteLine("SubPix accuracy calculated for image {0}", foundBoardIdx[i].ToString());

                _cornersObjectArray[i] = objectList.ToArray();
                _cornersPointsArray[i] = foundFastCorners[i].ToArray();
            }

            double error = CvInvoke.CalibrateCamera(objectPoints: _cornersObjectArray, imagePoints: _cornersPointsArray, imageSize: imageSize, cameraMatrix: _cameraMatrix, distortionCoeffs: _distCoeffs,
                calibrationType: CalibType.RationalModel, termCriteria: new MCvTermCriteria(maxIteration: 30, eps: 0.1), out Mat[] rotationVectors, out Mat[] translationVectors);

            Console.WriteLine("\nIntrinsic Calculation Error: " + error.ToString());

            Matrix<double> camMatrix = new Matrix<double>(rows: 3, cols: 3, data: _cameraMatrix.DataPointer);
            Console.WriteLine("\nCamera Matrix:\n");
            for (int i = 0; i < camMatrix.Rows; i++)
            {
                for (int j = 0; j < camMatrix.Cols; j++)
                {
                    Console.Write(camMatrix[row: i, col: j].ToString() + "  ");
                }
                Console.WriteLine();
            }

            Matrix<double> distCoeffs = new Matrix<double>(rows: _distCoeffs.Rows, cols: _distCoeffs.Cols, data: _distCoeffs.DataPointer);
            Console.WriteLine("\nDisortion Coefficients:\n");
            for (int i = 0; i < distCoeffs.Rows; i++)
            {
                for (int j = 0; j < distCoeffs.Cols; j++)
                {
                    Console.Write(distCoeffs[row: i, col: j].ToString() + "  ");
                }
                Console.WriteLine();
            }

            if (saveCamMatrix)
            {
                string camMatrixFile = cameraMatrixFolder + @"\" + "camMatrix_" + DateTime.Now.ToString("yyyy_MM_dd_hh_mm_ss");
                System.Xml.Linq.XDocument doc1 = Toolbox.XmlSerialize(camMatrix);
                doc1.Save(fileName: camMatrixFile);

                string distCoeffsFile = cameraMatrixFolder + @"\" + "distCoefficients_" + DateTime.Now.ToString("yyyy_MM_dd_hh_mm_ss");
                System.Xml.Linq.XDocument doc2 = Toolbox.XmlSerialize(distCoeffs);
                doc2.Save(fileName: distCoeffsFile);
            }
        }

        private void showChessboardButton_Click(object sender, EventArgs e)
        {
            idx = (int)calibration_imageNumUpDown.Value;
            Image<Bgr, byte> rawImage = new Image<Bgr, byte>(size: imageSize);
            CvInvoke.CvtColor(src: allImages[index: idx], dst: rawImage, code: ColorConversion.Gray2Bgr);

            if (foundBoardIdx.Contains(item: idx))
            {
                int f = foundBoardIdx.IndexOf(item: idx);
                bool success = true;
                CvInvoke.DrawChessboardCorners(image: rawImage, patternSize: _patternSize, corners: foundFastCorners[index: f], patternWasFound: success);
            }
            else
            {
                Console.WriteLine("No chessboard was found for image number {0}", idx.ToString());
            }

            calibration_imageBox1.Image = rawImage;

            Image<Gray, byte> img = new Image<Gray, byte>(size: imageSize);
            CvInvoke.Undistort(src: allImages[idx], dst: img, cameraMatrix: _cameraMatrix, distortionCoeffs: _distCoeffs);
            calibration_imageBox2.Image = img;
        }
    }
}
