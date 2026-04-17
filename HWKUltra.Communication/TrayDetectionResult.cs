using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WD.AVI.Common
{
    public class TrayDetectionResult
    {
        public TrayDetectionResult(short index, short rows, short cols)
        {
            Index = index;
            Columns = cols;
            Rows = rows;
            DefectsList = new List<DefectDetail>();
            SlidersSN = new string[rows, cols];
            ContainerIDs = new string[rows, cols];
            SlidersDefectUnique = new string[rows, cols]; ;
            Initial();
        }

        public void Initial()
        {
            LoadLock = String.Empty;
            for (int i = 0; i < Rows; i++)
            {
                for (int j = 0; j < Columns; j++)
                {
                    SlidersSN[i, j] = String.Empty;
                    ContainerIDs[i, j] = String.Empty;
                    SlidersDefectUnique[i, j] = String.Empty;
                }
            }
        }

        public List<DefectDetail> DefectsList { get; set; }
        public short Index { get; set; }
        public short Rows { get; set; }
        public short Columns { get; set; }
        public string SerialNum { get; set; }
        public string LotID { get; set; }

        public int SlidersCount { get; set; }

        public int OFFTCount { get; set; }

        public int ErrorCount { get; set; }

        public int DefectSlidersCount { get; set; }

        public string[,] SlidersSN { get; set; }
        public string[,] ContainerIDs { get; set; }
        public string[,] SlidersDefectUnique { get; set; }

        public string LoadLock { get; set; }

        public string StartTestTime { get; set; }
        public string EndTestTime { get; set; }

        public string TestDuration { get; set; }

        public string OperationID { get; set; }

        public string HeadType { get; set; }

        public string ProductName { get; set; }

        public string DeviceType { get; set; }

        public string ToolID { get; set; }

        public string CapCycPath { get; set; }

        public void SaveAsCsvFormat(ushort rows, ushort cols, bool saveRotate)
        {
            try
            {
                if (SerialNum == "")
                {
                    throw new Exception("Save defect result failed, serial number is empty.");
                }
                string savePath = string.Format("{0}/{1}-{2}-{3}.csv", CapCycPath, LoadLock, SerialNum, LotID);
                string yield = ((((double)SlidersCount - (double)DefectSlidersCount - (double)ErrorCount) / (double)SlidersCount) * 100).ToString("f2") + "%";
                using (StreamWriter sw = File.CreateText(savePath))
                {
                    sw.Write(string.Format("Report timestamp: {0}, test start timestamp: {1}, test finish timestamp: {2}, test duration: {3}. \n",
                        DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"),
                        StartTestTime,
                        EndTestTime,
                        TestDuration)
                        );
                    // sw.Write("Report Timestap: " + DateTime.Now.ToString()+ "" + "\n");
                    sw.Write("LotID: " + LotID + ",ProductName:" + ProductName + ",HeadType:" + HeadType + ",DeviceType:" + DeviceType + ",ToolID:" + ToolID + ",HeadType:" + HeadType + "\n");
                    sw.Write("trayID: " + SerialNum + "\n");
                    sw.Write("OperatorID: " + OperationID + " \n");
                    sw.Write("1st prime yield:  " + yield + "\n");
                    sw.Write("Total sliders: " + SlidersCount + "\n");
                    sw.WriteLine();



                    sw.Write("index,row,column,Slider Serial number,Defect category,Coordinate_1 X value,Coordinate_1 Y value,Coordinate_2 X value,Coordinate_2 Y value,Confidence, Default judge, Final judge\n");
                    for (int i = 0; i < DefectsList.Count; i++)
                    {
                        if (saveRotate)
                        {
                            sw.Write(string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11}",
                            i + 1,
                            DefectsList[i].Row,
                            DefectsList[i].Column,
                            SlidersSN[DefectsList[i].Row - 1, DefectsList[i].Column - 1],
                            DefectsList[i].Category,
                            // convert to rotate format
                            DefectsList[i].ImgRows - DefectsList[i].Coordrate_2.Y,
                            DefectsList[i].Coordrate_1.X,
                            DefectsList[i].ImgRows - DefectsList[i].Coordrate_1.Y,
                            DefectsList[i].Coordrate_2.X,

                            DefectsList[i].Confidence,
                            SlidersDefectUnique[DefectsList[i].Row - 1, DefectsList[i].Column - 1],
                            ""
                            ));
                        }
                        else
                        {
                            sw.Write(string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11}",
                            i + 1,
                            DefectsList[i].Row,
                            DefectsList[i].Column,
                            SlidersSN[DefectsList[i].Row - 1, DefectsList[i].Column - 1],
                            DefectsList[i].Category,
                            DefectsList[i].Coordrate_1.X,
                            DefectsList[i].Coordrate_1.Y,
                            DefectsList[i].Coordrate_2.X,
                            DefectsList[i].Coordrate_2.Y,
                            DefectsList[i].Confidence,
                            SlidersDefectUnique[DefectsList[i].Row - 1, DefectsList[i].Column - 1],
                            ""
                            ));
                        }

                        sw.WriteLine();
                    }
                    int goodStartIndex = DefectsList.Count;
                    for (int k = 0; k < rows; k++)
                    {
                        for (int p = 0; p < cols; p++)
                        {
                            if (SlidersDefectUnique[k, p] == string.Empty ||
                                SlidersDefectUnique[k, p] == "Pass" ||
                                SlidersDefectUnique[k, p].Contains("error") ||
                                SlidersDefectUnique[k, p] == "OFF-T")
                            {
                                goodStartIndex = goodStartIndex + 1;
                                sw.Write(string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11}",
                                goodStartIndex,
                                k + 1,
                                p + 1,
                                // 25,
                                // 20,
                                SlidersSN[k, p],
                           "",
                           "",
                           "",
                           "",
                           "",
                           "",
                           SlidersDefectUnique[k, p],
                           ""
                           ));
                                sw.WriteLine();
                            }
                        }
                    }

                    sw.Flush();
                    sw.Close();
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }

    public class DefectDetail
    {
        public DefectDetail(short row, short col, int imgrows, int imgcols, EnumSliderStatus state, Point coord_1, Point coord_2, float confidence)
        {
            Row = row;
            Column = col;
            ImgRows = imgrows;
            ImgCols = imgcols;
            Category = state;
            Coordrate_1 = coord_1;
            Coordrate_2 = coord_2;
            Confidence = confidence;
        }

        public short Row { get; set; }
        public short Column { get; set; }

        public int ImgRows { get; set; }
        public int ImgCols { get; set; }

        public EnumSliderStatus Category { get; set; }

        public Point Coordrate_1 { get; set; }
        public Point Coordrate_2 { get; set; }
        public float Confidence { get; set; }

    }

    public enum EnumSliderStatus
    {
        Empty = 0,
        Teached,
        Pass,
        Untest,
        None,
        Error,
        Clear,
        Full,


        A2,
        A5,
        A1L,
        A1O,
        A8,
        A5C,
        A1F,
        A11,
        A3,
        A4S,
        A0,
        A4P,

        P0532,
        P0452,
        P0492,
        P0042,
        P491,
        P2352,
        P172,
        P3232,
        P9842,
        P3222,
        P2582,
        P2422,
        P452,
        P3142,
        P3022,
        P3162,
        P2162,
        P2762,
        P0142,
        P6992,
        P2242,

        unknown,
        OFF,
    }
}
