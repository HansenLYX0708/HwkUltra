using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using WD.AVI.Common;
using WD.AVI.Configurations;
using WD.AVI.Inferlib;
using WD.AVI.SWMsg;
using WD.AVI.Vision;
using Point = System.Drawing.Point;

namespace WD.AVI.Tray
{
    struct TrayData
    {
        Point3D Pos;
        uint DefectCode;
        string DefectInfo;
    }


    /// <summary>
    /// class for tray map in AVI system
    /// </summary>
    public partial class TrayControl : IDisposable
    {
        private bool disposed = false;

        private static TrayControl uniqueInstance;
        private static readonly object locker = new object();

        static SoftwareMessage swmsg;
        static ConfigMgr cfg;
        private bool isInit = false;
        static Queue<ReciveData> que = new Queue<ReciveData>(500);
        static Queue<InputData> queInferLeftTop = new Queue<InputData>(500);
        static Timer processImage;
        static Timer processImage1;
        static Timer processImage2;
        static Timer processImage3;
        static Timer processImage4;
        // static Timer processImage5;
        // static Timer processImage6;
        // static Timer processImage7;
        static object m_obj = new object();
        static Timer inferImageLT;
        //static Timer inferImageRT;
        //static Timer inferImageLB;
        //static Timer inferImageRB;
        static object m_obj_infer1 = new object();
        static object m_obj_infer2 = new object();
        static object m_obj_infer3 = new object();
        static object m_obj_infer4 = new object();
        public static Action<short> SaveDefectEvent { get; set; }
        Stopwatch sw = new Stopwatch();
        int modelRefreshThreahold = 1;
        int modelRunningCount = 0;

        static List<PredictReturn>[,] tmPredictReturn;
        static int[,] predictReturnCount;
        static SharpInferLT inferLT;
        static SharpInferRT inferRT;
        static SharpInferLB inferLB;
        static SharpInferRB inferRB;

        public bool startQueue;

        public static bool[] IsEncounterErrorSlider;

        public static List<Point> WrongHeadType;

        private static Dictionary<EnumSliderStatus, float> defectThreshold;

        ImageCodecInfo jpgEncoder = ImageCodecInfo.GetImageEncoders()
            .First(codec => codec.FormatID == ImageFormat.Jpeg.Guid);

        EncoderParameters encParams = new EncoderParameters(1);

        private TrayControl()
        {
            encParams.Param[0] = new EncoderParameter(Encoder.Quality, 85L);
            TestState = new EnumTrayTestStates[8];
            inferLT = SharpInferLT.GetInstance();
            // inferRT = SharpInferRT.GetInstance();
            //inferLB = SharpInferLB.GetInstance();
            //inferRB = SharpInferRB.GetInstance();
            swmsg = SoftwareMessage.GetInstance();

            cfg = ConfigMgr.GetInstance();
            processImage = new Timer(new TimerCallback(TimeUpForProcessImage), null, 0, 2);
            processImage1 = new Timer(new TimerCallback(TimeUpForProcessImage1), null, 0, 2);
            processImage2 = new Timer(new TimerCallback(TimeUpForProcessImage2), null, 0, 2);
            processImage3 = new Timer(new TimerCallback(TimeUpForProcessImage3), null, 0, 2);
            processImage4 = new Timer(new TimerCallback(TimeUpForProcessImage4), null, 0, 2);
            //processImage5 = new Timer(new TimerCallback(TimeUpForProcessImage5), null, 0, 2);
            //processImage6 = new Timer(new TimerCallback(TimeUpForProcessImage6), null, 0, 2);
            //processImage7 = new Timer(new TimerCallback(TimeUpForProcessImage7), null, 0, 2);

            inferImageLT = new Timer(new TimerCallback(TimeUpForInferImageLeftTop), null, 0, 1);
            //inferImageRT = new Timer(new TimerCallback(TimeUpForInferImageRightTop), null, 0, 1);
            //inferImageLB = new Timer(new TimerCallback(TimeUpForInferImageLeftBottom), null, 0, 1);
            //inferImageRB = new Timer(new TimerCallback(TimeUpForInferImageRightBottom), null, 0, 1);
            SaveDefectEvent -= SaveDefectCallback;
            SaveDefectEvent += SaveDefectCallback;
            
            IsEncounterErrorSlider = new bool[8];
            WrongHeadType = new List<Point>();

            string defectThresholdPath = "Local/configs/DefectThresholdConfig.txt";
            defectThreshold = new Dictionary<EnumSliderStatus, float>();
            string category;
            float conf;
            string line;
            EnumSliderStatus state;
            if (File.Exists(defectThresholdPath))
            {
                using (StreamReader reader = new StreamReader(defectThresholdPath))
                {
                    line = string.Empty;
                    while ((line = reader.ReadLine()) != null)
                    {
                        var parts = line.Trim().Split(',');
                        category = parts[0];
                        if (Enum.IsDefined(typeof(EnumSliderStatus), category))
                        {
                            if (float.TryParse(parts[1], out conf))
                            {
                                Enum.TryParse<EnumSliderStatus>(category, out state);
                                defectThreshold[state] = conf;
                            }
                        }
                    }
                }
            }
        }

        public static TrayControl GetInstance()
        {
            if (uniqueInstance == null)
            {
                lock (locker)
                {
                    if (uniqueInstance == null)
                    {
                        uniqueInstance = new TrayControl();
                    }
                }
            }
            return uniqueInstance;
        }

        private void SaveDefectCallback(short index)
        {
            try
            {
                if (cfg.Syscfg.IsSaveDetailInfo)
                {
                    CSVFile.SaveMatrixData(string.Format("imageSharpness/all/{0}-{1}.csv", TraysResult[index].SerialNum, TimeFormatStr.GetTimeStamp()), ImgSharpness, index, 0);
                    CSVFile.SaveMatrixData(string.Format("imageSharpness/lefttop/{0}-{1}-1.csv", TraysResult[index].SerialNum, TimeFormatStr.GetTimeStamp()), ImgSharpness, index, 1);
                    CSVFile.SaveMatrixData(string.Format("imageSharpness/righttop/{0}-{1}-2.csv", TraysResult[index].SerialNum, TimeFormatStr.GetTimeStamp()), ImgSharpness, index, 2);
                    CSVFile.SaveMatrixData(string.Format("imageSharpness/leftbottom/{0}-{1}-3.csv", TraysResult[index].SerialNum, TimeFormatStr.GetTimeStamp()), ImgSharpness, index, 3);
                    CSVFile.SaveMatrixData(string.Format("imageSharpness/rightbottom/{0}-{1}-4.csv", TraysResult[index].SerialNum, TimeFormatStr.GetTimeStamp()), ImgSharpness, index, 4);
                    
                    if (!Directory.Exists("imageSharpness/olg"))
                    {
                        Directory.CreateDirectory("imageSharpness/olg");
                    }
                    CSVFile.SaveMatrixData(string.Format("imageSharpness/olg/{0}-{1}-olg.csv", TraysResult[index].SerialNum, TimeFormatStr.GetTimeStamp()), OLGSharpness, index);
                }
                Thread.Sleep(2000);
                if (TraysResult[index].DefectSlidersCount == 0)
                {
                    swmsg.UpdateStatus(string.Format("Tray {0}, All sliders are passing, please confirm if there is an issue with undetectable defects", TraysResult[index].SerialNum), EnumInfoLevel.Error);
                }
                float offtRate = ((float)TraysResult[index].OFFTCount) / ((float)TraysResult[index].SlidersCount);
                if (offtRate > 0.2)
                {
                    swmsg.UpdateStatus(string.Format("Tray {0}, OFF-T rate {1}%  over than the limit, please double confirm this result. Limit: 20%, OFF-T count : {2}, total sliders: {3}",
                        TraysResult[index].SerialNum,
                        (offtRate * 100).ToString(),
                        TraysResult[index].OFFTCount.ToString(),
                        TraysResult[index].SlidersCount.ToString()
                        ), EnumInfoLevel.Error);
                }

                string yield = ((((double)TraysResult[index].SlidersCount - (double)TraysResult[index].DefectSlidersCount - (double)TraysResult[index].ErrorCount) / (double)TraysResult[index].SlidersCount) * 100).ToString("f2") + "%";
                swmsg.UpdateTrayInfo(6, index, yield);

                if (ConfigurationManager.AppSettings["Site"] == "THO")
                {
                    TraysResult[index].SaveAsCsvFormat(cfg.Syscfg.TrayRows, cfg.Syscfg.TrayCols, cfg.Camcfg.SaveRotate);
                }
                else if (ConfigurationManager.AppSettings["Site"] == "PHO")
                {
                    TraysResult[index].SaveAsCsvFormat(cfg.Syscfg.TrayRows, cfg.Syscfg.TrayCols, false);
                }
                else
                {
                    TraysResult[index].SaveAsCsvFormat(cfg.Syscfg.TrayRows, cfg.Syscfg.TrayCols, cfg.Camcfg.SaveRotate);
                }
                //TraysResult[index].DefectsList.Clear();
                //TraysResult[index].SlidersSN = new string[TraysResult[index].Rows, TraysResult[index].Columns];
                //TraysResult[index].ContainerIDs = new string[TraysResult[index].Rows, TraysResult[index].Columns];
                //TraysResult[index].SlidersDefectUnique = new string[TraysResult[index].Rows, TraysResult[index].Columns];

                modelRunningCount++;
                if (modelRunningCount >= modelRefreshThreahold)
                {
                    modelRunningCount = 0;
                }
            }
            catch (Exception ex)
            {
                swmsg.UpdateStatus(string.Format("SaveDefectCallback encounter error, message: {0}", ex.Message), EnumInfoLevel.Error);
            }
        }

        public uint Group { get; set; }
        public static TrayDetectionResult[] TraysResult { get; set; }
        public long SliderExposure { get; set; }

        public Point3D[,,] Pockects { get; set; }

        public double[,,] TargetZHeight
        {
            get; set;
        }

        static public bool[,,] SliderState
        {
            get; set;
        }

        static public double[,,,] ImgSharpness { get; set; }


        static public double[,,] OLGSharpness { get; set; }

        public ushort Rows { get; private set; }

        public ushort Cols { get; private set; }

        public bool IsTeachCorner
        {
            get; set;
        }

        public ushort TrayNums
        {
            get; set;
        }

        public EnumTrayTestStates[] TestState
        {
            get; set;
        }

        public void SetShape(ushort nums, ushort rows, ushort cols)
        {
            this.TrayNums = nums;
            this.Rows = rows;
            this.Cols = cols;

            tmPredictReturn = new List<PredictReturn>[Rows, Cols];
            for (int i = 0; i < Rows; i++)
                for (int j = 0; j < Cols; j++)
                {
                    tmPredictReturn[i, j] = new List<PredictReturn>();
                }
            predictReturnCount = new int[Rows, Cols];


            TraysResult = new TrayDetectionResult[nums];
            for (int i = 0; i < TraysResult.Length; i++)
            {
                TraysResult[i] = new TrayDetectionResult((short)i, (short)rows, (short)cols);
            }

            Pockects = new Point3D[this.TrayNums, this.Rows, this.Cols];
            ImgSharpness = new double[this.TrayNums, 5, this.Rows, this.Cols];
            OLGSharpness = new double[this.TrayNums, this.Rows, this.Cols];
            TargetZHeight = new double[this.TrayNums, this.Rows, this.Cols];
            SliderState = new bool[this.TrayNums, this.Rows, this.Cols];
            if (!Directory.Exists("trayPos"))
            {
                Directory.CreateDirectory("trayPos");
            }
            string filePath = "trayPos/" + "Pockets" + ".data";

            if (File.Exists(filePath))
            {
                LoadPocketData(filePath);
            }
            else
            {
                for (ushort i = 0; i < nums; i++)
                {
                    InitXYPosition(i, new Point3D(0, 0, 0), new Point3D(0, 0, 0), new Point3D(0, 0, 0), new Point3D(0, 0, 0));
                }
            }
            isInit = true;
        }

        public void ClearPredictReturn()
        {
            for (int i = 0; i < Rows; i++)
                for (int j = 0; j < Cols; j++)
                {
                    if (tmPredictReturn == null)
                        throw new Exception("PredictReturn is null.");
                    if (tmPredictReturn[i, j] != null)
                    {
                        tmPredictReturn[i, j].Clear();
                    }
                    else
                        tmPredictReturn[i, j] = new List<PredictReturn>();
                }
        }

        public void SavePocketData()
        {
            FileStream fs = new FileStream("trayPos/" + "Pockets" + ".data", FileMode.Create);
            BinaryFormatter binaryFormatter = new BinaryFormatter();
            binaryFormatter.Serialize(fs, Pockects);
            binaryFormatter = null;
            fs.Close();
            fs.Dispose();
        }

        public void LoadPocketData(string filePath)
        {
            FileStream fs = new FileStream(filePath, FileMode.Open);

            try
            {
                BinaryFormatter binaryFormatter = new BinaryFormatter();
                Pockects = (Point3D[,,])binaryFormatter.Deserialize(fs);
            }
            catch { }
            finally
            {
                fs.Close();
            }
        }

        public void InitXYPosition(ushort trayIndex, Point3D leftTop, Point3D rightTop, Point3D leftBottom, Point3D rightBottom)
        {
            if (uniqueInstance == null)
            {
                return;
            }
            Point3D[] leftList = GetEquallyDividedList(leftTop, leftBottom, Rows);
            Point3D[] rightList = GetEquallyDividedList(rightTop, rightBottom, Rows);
            Point3D start;
            Point3D end;
            for (ushort row = 0; row < this.Rows; row++)
            {
                start = leftList[row];
                end = rightList[row];
                Point3D[] oneRow = GetEquallyDividedList(start, end, Cols);
                for (ushort col = 0; col < this.Cols; col++)
                {
                    Pockects[trayIndex, row, col] = oneRow[col];
                }
            }
        }

        /// <summary>
        /// Get Equally Divided List with Point3D class
        /// </summary>
        /// <param name="start">start point3d</param>
        /// <param name="end">end point 3d</param>
        /// <param name="num"></param>
        /// <returns></returns>
        public Point3D[] GetEquallyDividedList(Point3D start, Point3D end, uint num)
        {
            if (num < 2)
            {
                throw new Exception("num must be large than 2 when get equally divided list.");
            }
            Point3D[] ret = new Point3D[num];
            double stepX = (end.X - start.X) / (num - 1);
            double stepY = (end.Y - start.Y) / (num - 1);
            double stepZ = (end.Z - start.Z) / (num - 1);
            for (int i = 0; i < num - 1; i++)
            {
                ret[i] = new Point3D(0, 0, 0);
                ret[i].X = start.X + i * stepX;
                ret[i].Y = start.Y + i * stepY;
                ret[i].Z = start.Z + i * stepZ;
            }
            ret[num - 1] = end;
            return ret;
        }

        public void EnQueue(short index, short row, short col, int width, int height, byte[] pixels, bool isColor = false, string suffix="")
        {
            Monitor.Enter(m_obj);
            string imgname = "default.jpg";
            if (suffix != string.Empty)
            {
                imgname = string.Format("{0}/{1}/{2}-{3}-{4}-{5}.bmp", 
                    cfg.Syscfg.CapCycPath, 
                    TraysResult[index].LoadLock + "-" + TraysResult[index].SerialNum + "-" + TraysResult[index].LotID,
                    row.ToString(),
                    col.ToString(),
                    TraysResult[index].SlidersSN[row - 1, col - 1],
                    suffix
                );
            }
            else
            {
                imgname = string.Format("{0}/{1}/{2}-{3}-{4}.bmp", 
                    cfg.Syscfg.CapCycPath, 
                    TraysResult[index].LoadLock + "-" + TraysResult[index].SerialNum + "-" + TraysResult[index].LotID,
                    row.ToString(),
                    col.ToString(),
                    TraysResult[index].SlidersSN[row - 1, col - 1]
                );
            }

            que.Enqueue(new ReciveData(index, row, col, imgname, width, height, pixels, isColor));
            Monitor.Pulse(m_obj);
            Monitor.Exit(m_obj);
        }

        static public void EnQueueInputDataLT(short index, short row, short col, int srcrows, int srccols, float[] src, double sharpness)
        {
            Monitor.Enter(m_obj_infer1);
            queInferLeftTop.Enqueue(new InputData(index, row, col, srcrows, srccols, src, sharpness));
            Monitor.Pulse(m_obj_infer1);
            Monitor.Exit(m_obj_infer1);
        }

        static private ReciveData Pop()
        {
            Monitor.Enter(m_obj);
            ReciveData recive = null;
            try
            {
                if (que.Count > 0)
                {
                    recive = que.Dequeue();
                }
            }
            catch (Exception ex)
            {
                swmsg.UpdateStatus(ex.Message, EnumInfoLevel.Error);
            }
            finally
            {
                Monitor.Exit(m_obj);
            }
            return recive;
        }
        static private InputData PopInputLT()
        {
            Monitor.Enter(m_obj_infer1);
            InputData recive = null;
            try
            {
                if (queInferLeftTop.Count > 0)
                {
                    recive = queInferLeftTop.Dequeue();
                }
            }
            catch (Exception ex)
            {
                swmsg.UpdateStatus(ex.Message, EnumInfoLevel.Error);
            }
            finally
            {
                Monitor.Exit(m_obj_infer1);
            }
            return recive;
        }

        private void TimeUpForProcessImage(object value)
        {
            processImage.Change(Timeout.Infinite, Timeout.Infinite);
            try
            {
                ReciveData recive = Pop();
                if (recive != null)
                {
                    if (!SliderState[recive.index, recive.row - 1, recive.col - 1])
                    {
                        if (TraysResult[recive.index].SlidersDefectUnique[recive.row - 1, recive.col - 1] == "OFF-T" ||
                            TraysResult[recive.index].SlidersDefectUnique[recive.row - 1, recive.col - 1].Contains("error"))
                        {
                            // Save OFFT samples
                            Bitmap offtbmp = GetSliderROI.ByteToBitmap(recive.pixels, recive.height, recive.width);
                            //string SaveName = "";
                            //string[] names = recive.fileName.Split('.');
                            //SaveName = recive.fileName;
                            //SaveName = names[0] + "-OFFT." + names[1];
                            offtbmp.Save(recive.fileName);
                        }
                        return;
                    }
                    Bitmap source= null;
                    float[] tra1_src1 = null;
                    double[] sharpness = { 0, 0, 0, 0, 0 };
                    GetSliderROIV2 sliderROIV2 = new GetSliderROIV2();
                    if (!cfg.Camcfg.OpenDetect)
                    {
                        sliderROIV2.IsOpenDetect = false;
                    }
                    sliderROIV2.IsDetectHeadType = cfg.Camcfg.IsDetectHeadType;
                    if(cfg.Syscfg.IsOnline)
                    {
                        if (TrayControl.TraysResult[recive.index].ProductName != null && TrayControl.TraysResult[recive.index].ProductName.Contains("SYDNEYE"))
                        {
                            sliderROIV2.HeadType = (TrayControl.TraysResult[recive.index].HeadType == "A") ? "B" : "A";
                        }
                        else if (TrayControl.TraysResult[recive.index].ProductName != null && TrayControl.TraysResult[recive.index].ProductName.Contains("SYDNEYF"))
                        {
                            // TODO : temp disable
                            sliderROIV2.IsDetectHeadType = false;
                        }
                        else
                        {
                            sliderROIV2.HeadType = TrayControl.TraysResult[recive.index].HeadType;
                        }
                    }

                    if (recive.IsColor)
                    {
                        if (cfg.Syscfg.FunctionMode == "rowbarVertical")
                        {
                            bool olg = false;
                            if (recive.fileName.Contains("Sur2"))
                            {
                                if (recive.col == 3 || recive.col == 8 ||
                                    recive.col == 13 || recive.col == 18 ||
                                    recive.col == 23 || recive.col == 28)
                                {
                                    olg = true;
                                }
                                else
                                {
                                    return;
                                }
                            }
                            double olgsharpness;
                            sliderROIV2.GetRowBarDepoEdge(recive.pixels, recive.height, recive.width, olg, false, out source, out olgsharpness);
                            OLGSharpness[recive.index, recive.row - 1, recive.col - 1] = olgsharpness;
                        }
                        else
                        {
                            if (cfg.Syscfg.FunctionMode == "rowbarPoletip")
                            {
                                sliderROIV2.Get50XPoletipROI(recive.pixels, recive.height, recive.width, out source, out sharpness, recive.IsColor);
                            }
                            else
                            {
                                sliderROIV2.GetROI3(recive.pixels, recive.height, recive.width, out source);
                            }
                        }
                    }
                    else
                    {
                        DateTime begin = DateTime.Now;
                        sliderROIV2.GetROI22(recive.pixels, recive.height, recive.width, out source, out sharpness, out tra1_src1, cfg.Camcfg.SaveRotate, false, recive.IsColor, cfg.Syscfg.IsOnline);
                        if (tra1_src1 == null)
                        {
                            sliderROIV2.GetROI2(recive.pixels, recive.height, recive.width, out source, out sharpness, out tra1_src1, cfg.Camcfg.SaveRotate, false, recive.IsColor, cfg.Syscfg.IsOnline);
                        }

                        DateTime end = DateTime.Now;
                    }
                    if (source != null)
                    {
                        string SaveName = "";

                        if (sliderROIV2.backside != 0 || sliderROIV2.orientation != 0 || 
                            sliderROIV2.IsTypeWrong != 0 || sliderROIV2.IsBlurry != 0)
                        {
                            SaveName = recive.fileName;
                        }
                        else
                        {
                            SaveName = recive.fileName;
                        }
                        if (recive.IsColor)
                        {
                            //if (cfg.Syscfg.FunctionMode == "rowbarVertical" || cfg.Syscfg.FunctionMode == "rowbarHorizontal")
                            {
                                string[] names = SaveName.Split('.');
                                SaveName = names[0] + ".jpg"; // + "-error" +
                                EncoderParameters encoderParameters = new EncoderParameters();
                                //source.Save(SaveName, ImageFormat.Jpeg, );
                                source.Save(SaveName, jpgEncoder, encParams);
                            }
                        }
                        else
                        {
                            source.Save(SaveName);
                        }
                    }
                    // check blur image
                    if (recive.row <= cfg.Syscfg.TrayRows && recive.col <= cfg.Syscfg.TrayCols)
                    {
                        ImgSharpness[recive.index, 0, recive.row - 1, recive.col - 1] = sharpness[0];
                        ImgSharpness[recive.index, 1, recive.row - 1, recive.col - 1] = sharpness[1];
                        ImgSharpness[recive.index, 2, recive.row - 1, recive.col - 1] = sharpness[2];
                        ImgSharpness[recive.index, 3, recive.row - 1, recive.col - 1] = sharpness[3];
                        ImgSharpness[recive.index, 4, recive.row - 1, recive.col - 1] = sharpness[4];
                    }

                    // will be return once error
                    if (sliderROIV2.backside == 1)
                    {
                        swmsg.UpdateSliderStates(recive.row - 1, recive.col - 1, EnumSliderStatus.Error);
                        swmsg.UpdateTrayInfo(4, recive.index, "Backside");
                        swmsg.UpdateStatus(string.Format("[Row:{0}], [Col:{1}], Slider is backside.", recive.row.ToString(), recive.col.ToString()), EnumInfoLevel.Error);
                        TrayControl.IsEncounterErrorSlider[recive.index] = true;
                        TrayControl.TraysResult[recive.index].SlidersDefectUnique[recive.row - 1, recive.col - 1] = "error:Backside";
                        TrayControl.TraysResult[recive.index].ErrorCount++;
                        return;
                    }
                    if (sliderROIV2.orientation == 1)
                    {
                        swmsg.UpdateSliderStates(recive.row - 1, recive.col - 1, EnumSliderStatus.Error);
                        swmsg.UpdateTrayInfo(4, recive.index, "Wrong dircetion");
                        swmsg.UpdateStatus(string.Format("[Row:{0}], [Col:{1}], Slider is wrong direction.", recive.row.ToString(), recive.col.ToString()), EnumInfoLevel.Error);
                        TrayControl.IsEncounterErrorSlider[recive.index] = true;
                        TrayControl.TraysResult[recive.index].SlidersDefectUnique[recive.row - 1, recive.col - 1] = "error:wrong dircetion";
                        TrayControl.TraysResult[recive.index].ErrorCount++;
                        return;
                    }
                    if (sliderROIV2.IsTypeWrong == 1)
                    {
                        swmsg.UpdateSliderStates(recive.row - 1, recive.col - 1, EnumSliderStatus.Error);
                        swmsg.UpdateTrayInfo(4, recive.index, "HeadType");
                        swmsg.UpdateStatus(string.Format("[Row:{0}], [Col:{1}], Slider is wrong Head type.", recive.row.ToString(), recive.col.ToString()), EnumInfoLevel.Error);
                        TrayControl.IsEncounterErrorSlider[recive.index] = true;
                        TrayControl.WrongHeadType.Add(new Point(recive.row, recive.col));
                        TrayControl.TraysResult[recive.index].SlidersDefectUnique[recive.row - 1, recive.col - 1] = "error:wrong type";
                        TrayControl.TraysResult[recive.index].ErrorCount++;
                        return;
                    }
                    if (sliderROIV2.IsBlurry == 1)
                    {
                        swmsg.UpdateSliderStates(recive.row - 1, recive.col - 1, EnumSliderStatus.Error);
                        swmsg.UpdateTrayInfo(4, recive.index, "Blurry");
                        swmsg.UpdateStatus(string.Format("[Row:{0}], [Col:{1}], Slider is blurry image.", recive.row.ToString(), recive.col.ToString()), EnumInfoLevel.Error);
                        TrayControl.IsEncounterErrorSlider[recive.index] = true;
                        TrayControl.TraysResult[recive.index].SlidersDefectUnique[recive.row - 1, recive.col - 1] = "error:Blurry";
                        TrayControl.TraysResult[recive.index].ErrorCount++;
                        return;

                    }
                    sliderROIV2 = null;
                    if (recive.row <= cfg.Syscfg.TrayRows && recive.col <= cfg.Syscfg.TrayCols)
                    {
                        if (tra1_src1 != null)
                        {
                            if (cfg.Camcfg.SaveRotate)
                            {
                                EnQueueInputDataLT(recive.index, recive.row, recive.col, source.Width, source.Height, tra1_src1, Math.Min(Math.Min(Math.Min(sharpness[1], sharpness[2]), sharpness[3]), sharpness[4]));
                            }
                            else
                            {
                                EnQueueInputDataLT(recive.index, recive.row, recive.col, source.Height, source.Width, tra1_src1, Math.Min(Math.Min(Math.Min(sharpness[1], sharpness[2]), sharpness[3]), sharpness[4]));
                            }
                            if (recive.row == cfg.Syscfg.TrayRows && recive.col == cfg.Syscfg.TrayCols)
                            {
                                swmsg.UpdateStatus(string.Format("ProcessImage::Get finial image"), EnumInfoLevel.Event);
                            }
                        }
                        else 
                        {
                            if (!recive.IsColor)
                            {
                                swmsg.UpdateSliderStates(recive.row - 1, recive.col - 1, EnumSliderStatus.Error);
                                swmsg.UpdateTrayInfo(4, recive.index, "null data");
                                swmsg.UpdateStatus(string.Format("[Row:{0}], [Col:{1}], null data.", recive.row.ToString(), recive.col.ToString()), EnumInfoLevel.Error);
                                TrayControl.IsEncounterErrorSlider[recive.index] = true;
                                TrayControl.TraysResult[recive.index].SlidersDefectUnique[recive.row - 1, recive.col - 1] = "error:null data";
                                TrayControl.TraysResult[recive.index].ErrorCount++;
                            }
                            return;
                        }
                    }
                    source.Dispose();
                    source = null;
                    recive.Dispose();
                    recive = null;
                    DateTime endPre = DateTime.Now;
                    //Console.WriteLine("****Pre Process thread 1：{0}", (endPre - beginPre).TotalMilliseconds);

                }
            }
            catch (Exception ex)
            {
                swmsg.UpdateStatus(ex.Message, EnumInfoLevel.Error);
            }
            finally
            {
                processImage.Change(0, 2);
            }
        }
        private void TimeUpForProcessImage1(object value)
        {
            processImage1.Change(Timeout.Infinite, Timeout.Infinite);
            try
            {
                ReciveData recive = Pop();
                if (recive != null)
                {
                    if (!SliderState[recive.index, recive.row - 1, recive.col - 1])
                    {
                        if (TraysResult[recive.index].SlidersDefectUnique[recive.row - 1, recive.col - 1] == "OFF-T" ||
                            TraysResult[recive.index].SlidersDefectUnique[recive.row - 1, recive.col - 1].Contains("error"))
                        {
                            // Save OFFT samples
                            Bitmap offtbmp = GetSliderROI.ByteToBitmap(recive.pixels, recive.height, recive.width);
                            //string SaveName = "";
                            //string[] names = recive.fileName.Split('.');
                            //SaveName = recive.fileName;
                            //SaveName = names[0] + "-OFFT." + names[1];
                            offtbmp.Save(recive.fileName);
                        }
                        return;
                    }
                    Bitmap source = null;
                    float[] tra2_src1 = null;
                    double[] sharpness = { 0, 0, 0, 0, 0 };
                    GetSliderROIV2 sliderROIV2 = new GetSliderROIV2();
                    if (!cfg.Camcfg.OpenDetect)
                    {
                        sliderROIV2.IsOpenDetect = false;
                    }
                    sliderROIV2.IsDetectHeadType = cfg.Camcfg.IsDetectHeadType;
                    if (cfg.Syscfg.IsOnline)
                    {
                        if (TrayControl.TraysResult[recive.index].ProductName != null && TrayControl.TraysResult[recive.index].ProductName.Contains("SYDNEYE"))
                        {
                            sliderROIV2.HeadType = (TrayControl.TraysResult[recive.index].HeadType == "A") ? "B" : "A";
                        }
                        else if (TrayControl.TraysResult[recive.index].ProductName != null && TrayControl.TraysResult[recive.index].ProductName.Contains("SYDNEYF"))
                        {
                            // TODO : temp disable
                            sliderROIV2.IsDetectHeadType = false;
                        }
                        else
                        {
                            sliderROIV2.HeadType = TrayControl.TraysResult[recive.index].HeadType;
                        }
                    }
                    
                    if (recive.IsColor)
                    {
                        if (cfg.Syscfg.FunctionMode == "rowbarVertical")
                        {
                            bool olg = false;
                            if (recive.fileName.Contains("Sur2"))
                            {
                                if (recive.col == 3 || recive.col == 8 ||
                                    recive.col == 13 || recive.col == 18 ||
                                    recive.col == 23 || recive.col == 28)
                                {
                                    olg = true;
                                }
                                else
                                {
                                    return;
                                }
                            }
                            double olgsharpness;
                            sliderROIV2.GetRowBarDepoEdge(recive.pixels, recive.height, recive.width, olg, false, out source, out olgsharpness);
                            OLGSharpness[recive.index, recive.row - 1, recive.col - 1] = olgsharpness;
                        }
                        else
                        {
                            if (cfg.Syscfg.FunctionMode == "rowbarPoletip")
                            {
                                sliderROIV2.Get50XPoletipROI(recive.pixels, recive.height, recive.width, out source, out sharpness, recive.IsColor);
                            }
                            else
                            {
                                sliderROIV2.GetROI3(recive.pixels, recive.height, recive.width, out source);
                            }
                        }
                    }
                    else
                    {
                        DateTime begin = DateTime.Now;
                        sliderROIV2.GetROI22(recive.pixels, recive.height, recive.width, out source, out sharpness, out tra2_src1, cfg.Camcfg.SaveRotate, false, recive.IsColor, cfg.Syscfg.IsOnline);
                        if (tra2_src1 == null)
                        {
                            sliderROIV2.GetROI2(recive.pixels, recive.height, recive.width, out source, out sharpness, out tra2_src1, cfg.Camcfg.SaveRotate, false, recive.IsColor, cfg.Syscfg.IsOnline);
                        }
                        DateTime end = DateTime.Now;
                        //Console.WriteLine("****GetROI22 thread 2：{0}", (end - begin).TotalMilliseconds);

                    }
                    if (source != null)
                    {
                        string SaveName = "";

                        if (sliderROIV2.backside != 0 || sliderROIV2.orientation != 0 ||
                            sliderROIV2.IsTypeWrong != 0 || sliderROIV2.IsBlurry != 0)
                        {
                            //string[] names = recive.fileName.Split('.');
                            //SaveName = names[0] + "-error." + names[1];
                            SaveName = recive.fileName;
                        }
                        else
                        {
                            SaveName = recive.fileName;
                        }
                        if (recive.IsColor)
                        {
                            //if (cfg.Syscfg.FunctionMode == "rowbarVertical" || cfg.Syscfg.FunctionMode == "rowbarHorizontal")
                            {
                                string[] names = SaveName.Split('.');
                                SaveName = names[0] + ".jpg"; 
                                // source.Save(SaveName, ImageFormat.Jpeg);
                                source.Save(SaveName, jpgEncoder, encParams);
                            }
                        }
                        else
                        {
                            source.Save(SaveName);
                        }
                    }

                    // check blur image
                    if (recive.row <= cfg.Syscfg.TrayRows && recive.col <= cfg.Syscfg.TrayCols)
                    {
                        ImgSharpness[recive.index, 0, recive.row - 1, recive.col - 1] = sharpness[0];
                        ImgSharpness[recive.index, 1, recive.row - 1, recive.col - 1] = sharpness[1];
                        ImgSharpness[recive.index, 2, recive.row - 1, recive.col - 1] = sharpness[2];
                        ImgSharpness[recive.index, 3, recive.row - 1, recive.col - 1] = sharpness[3];
                        ImgSharpness[recive.index, 4, recive.row - 1, recive.col - 1] = sharpness[4];
                    }

                    // will be return once error
                    if (sliderROIV2.backside == 1)
                    {
                        swmsg.UpdateSliderStates(recive.row - 1, recive.col - 1, EnumSliderStatus.Error);
                        swmsg.UpdateTrayInfo(4, recive.index, "Backside");
                        swmsg.UpdateStatus(string.Format("[Row:{0}], [Col:{1}], Slider is backside.", recive.row.ToString(), recive.col.ToString()), EnumInfoLevel.Error);
                        TrayControl.IsEncounterErrorSlider[recive.index] = true;
                        TrayControl.TraysResult[recive.index].SlidersDefectUnique[recive.row - 1, recive.col - 1] = "error:Backside";
                        TrayControl.TraysResult[recive.index].ErrorCount++;
                        return;
                    }
                    if (sliderROIV2.orientation == 1)
                    {
                        swmsg.UpdateSliderStates(recive.row - 1, recive.col - 1, EnumSliderStatus.Error);
                        swmsg.UpdateTrayInfo(4, recive.index, "Wrong dircetion");
                        swmsg.UpdateStatus(string.Format("[Row:{0}], [Col:{1}], Slider is wrong direction.", recive.row.ToString(), recive.col.ToString()), EnumInfoLevel.Error);
                        TrayControl.IsEncounterErrorSlider[recive.index] = true;
                        TrayControl.TraysResult[recive.index].SlidersDefectUnique[recive.row - 1, recive.col - 1] = "error:wrong dircetion";
                        TrayControl.TraysResult[recive.index].ErrorCount++;
                        return;
                    }
                    if (sliderROIV2.IsTypeWrong == 1)
                    {
                        swmsg.UpdateSliderStates(recive.row - 1, recive.col - 1, EnumSliderStatus.Error);
                        swmsg.UpdateTrayInfo(4, recive.index, "HeadType");
                        swmsg.UpdateStatus(string.Format("[Row:{0}], [Col:{1}], Slider is wrong Head type.", recive.row.ToString(), recive.col.ToString()), EnumInfoLevel.Error);
                        TrayControl.IsEncounterErrorSlider[recive.index] = true;
                        TrayControl.WrongHeadType.Add(new Point(recive.row, recive.col));
                        TrayControl.TraysResult[recive.index].SlidersDefectUnique[recive.row - 1, recive.col - 1] = "error:wrong type";
                        TrayControl.TraysResult[recive.index].ErrorCount++;
                        return;
                    }
                    if (sliderROIV2.IsBlurry == 1)
                    {
                        swmsg.UpdateSliderStates(recive.row - 1, recive.col - 1, EnumSliderStatus.Error);
                        swmsg.UpdateTrayInfo(4, recive.index, "Blurry");
                        swmsg.UpdateStatus(string.Format("[Row:{0}], [Col:{1}], Slider is blurry image.", recive.row.ToString(), recive.col.ToString()), EnumInfoLevel.Error);
                        TrayControl.IsEncounterErrorSlider[recive.index] = true;
                        TrayControl.TraysResult[recive.index].SlidersDefectUnique[recive.row - 1, recive.col - 1] = "error:Blurry";
                        TrayControl.TraysResult[recive.index].ErrorCount++;
                        return;

                    }

                    sliderROIV2 = null;
                    if (recive.row <= cfg.Syscfg.TrayRows && recive.col <= cfg.Syscfg.TrayCols)
                    {
                        if (tra2_src1 != null)
                        {
                            if (cfg.Camcfg.SaveRotate)
                            {
                                EnQueueInputDataLT(recive.index, recive.row, recive.col, source.Width, source.Height, tra2_src1, Math.Min(Math.Min(Math.Min(sharpness[1], sharpness[2]), sharpness[3]), sharpness[4]));
                            }
                            else
                            {
                                EnQueueInputDataLT(recive.index, recive.row, recive.col, source.Height, source.Width, tra2_src1, Math.Min(Math.Min(Math.Min(sharpness[1], sharpness[2]), sharpness[3]), sharpness[4]));
                            }
                            if (recive.row == cfg.Syscfg.TrayRows && recive.col == cfg.Syscfg.TrayCols)
                            {
                                swmsg.UpdateStatus(string.Format("ProcessImage::Get finial image"), EnumInfoLevel.Event);
                            }
                        }
                        else
                        {
                            if (!recive.IsColor)
                            {
                                swmsg.UpdateSliderStates(recive.row - 1, recive.col - 1, EnumSliderStatus.Error);
                                swmsg.UpdateTrayInfo(4, recive.index, "null data");
                                swmsg.UpdateStatus(string.Format("[Row:{0}], [Col:{1}], null data.", recive.row.ToString(), recive.col.ToString()), EnumInfoLevel.Error);
                                TrayControl.IsEncounterErrorSlider[recive.index] = true;
                                TrayControl.TraysResult[recive.index].SlidersDefectUnique[recive.row - 1, recive.col - 1] = "error:null data";
                                TrayControl.TraysResult[recive.index].ErrorCount++;
                            }
                            return;

                        }

                    }
                    source.Dispose();
                    source = null;
                    recive.Dispose();
                    recive = null;

                    DateTime endPre = DateTime.Now;
                    //Console.WriteLine("****Pre Process thread 2：{0}", (endPre - beginPre).TotalMilliseconds);

                }
            }
            catch (Exception ex)
            {
                swmsg.UpdateStatus(ex.Message, EnumInfoLevel.Error);
            }
            finally
            {
                processImage1.Change(0, 2);
            }
        }
        private void TimeUpForProcessImage2(object value)
        {
            processImage2.Change(Timeout.Infinite, Timeout.Infinite);
            try
            {
                ReciveData recive = Pop();
                if (recive != null)
                {
                    DateTime beginPre = DateTime.Now;
                    if (!SliderState[recive.index, recive.row - 1, recive.col - 1])
                    {
                        if (TraysResult[recive.index].SlidersDefectUnique[recive.row - 1, recive.col - 1] == "OFF-T" ||
                            TraysResult[recive.index].SlidersDefectUnique[recive.row - 1, recive.col - 1].Contains("error"))
                        {
                            // Save OFFT samples
                            Bitmap offtbmp = GetSliderROI.ByteToBitmap(recive.pixels, recive.height, recive.width);
                            //string SaveName = "";
                            //string[] names = recive.fileName.Split('.');
                            //SaveName = recive.fileName;
                            //SaveName = names[0] + "-OFFT." + names[1];
                            offtbmp.Save(recive.fileName);
                        }
                        return;
                    }
                    Bitmap source = null;
                    float[] tra3_src1 = null;
                    double[] sharpness = { 0, 0, 0, 0, 0 };
                    GetSliderROIV2 sliderROIV2 = new GetSliderROIV2();
                    if (!cfg.Camcfg.OpenDetect)
                    {
                        sliderROIV2.IsOpenDetect = false;
                    }
                    sliderROIV2.IsDetectHeadType = cfg.Camcfg.IsDetectHeadType;
                    if (cfg.Syscfg.IsOnline)
                    {
                        if (TrayControl.TraysResult[recive.index].ProductName != null && TrayControl.TraysResult[recive.index].ProductName.Contains("SYDNEYE"))
                        {
                            sliderROIV2.HeadType = (TrayControl.TraysResult[recive.index].HeadType == "A") ? "B" : "A";
                        }
                        else if (TrayControl.TraysResult[recive.index].ProductName != null && TrayControl.TraysResult[recive.index].ProductName.Contains("SYDNEYF"))
                        {
                            // TODO : temp disable
                            sliderROIV2.IsDetectHeadType = false;
                        }
                        else
                        {
                            sliderROIV2.HeadType = TrayControl.TraysResult[recive.index].HeadType;
                        }
                    }
                    
                    if (recive.IsColor)
                    {
                        if (cfg.Syscfg.FunctionMode == "rowbarVertical")
                        {
                            bool olg = false;
                            if (recive.fileName.Contains("Sur2"))
                            {
                                if (recive.col == 3 || recive.col == 8 ||
                                    recive.col == 13 || recive.col == 18 ||
                                    recive.col == 23 || recive.col == 28)
                                {
                                    olg = true;
                                }
                                else
                                {
                                    return;
                                }
                            }
                            double olgsharpness;
                            sliderROIV2.GetRowBarDepoEdge(recive.pixels, recive.height, recive.width, olg, false, out source, out olgsharpness);
                            OLGSharpness[recive.index, recive.row - 1, recive.col - 1] = olgsharpness;
                        }
                        else
                        {
                            if (cfg.Syscfg.FunctionMode == "rowbarPoletip")
                            {
                                sliderROIV2.Get50XPoletipROI(recive.pixels, recive.height, recive.width, out source, out sharpness, recive.IsColor);
                            }
                            else
                            {
                                sliderROIV2.GetROI3(recive.pixels, recive.height, recive.width, out source);
                            }
                        }
                    }
                    else
                    {
                        DateTime begin = DateTime.Now;
                        sliderROIV2.GetROI22(recive.pixels, recive.height, recive.width, out source, out sharpness, out tra3_src1, cfg.Camcfg.SaveRotate, false, recive.IsColor, cfg.Syscfg.IsOnline);
                        if (tra3_src1 == null)
                        {
                            sliderROIV2.GetROI2(recive.pixels, recive.height, recive.width, out source, out sharpness, out tra3_src1, cfg.Camcfg.SaveRotate, false, recive.IsColor, cfg.Syscfg.IsOnline);
                        }
                        DateTime end = DateTime.Now;
                        //Console.WriteLine("****GetROI22 thread 3：{0}", (end - begin).TotalMilliseconds);
                    }
                    if (source != null)
                    {
                        string SaveName = "";

                        if (sliderROIV2.backside != 0 || sliderROIV2.orientation != 0 ||
                            sliderROIV2.IsTypeWrong != 0 || sliderROIV2.IsBlurry != 0)
                        {
                            //string[] names = recive.fileName.Split('.');
                            //SaveName = names[0] + "-error." + names[1];
                            SaveName = recive.fileName;
                        }
                        else
                        {
                            SaveName = recive.fileName;
                        }
                        if (recive.IsColor)
                        {
                            //if (cfg.Syscfg.FunctionMode == "rowbarVertical" || cfg.Syscfg.FunctionMode == "rowbarHorizontal")
                            {
                                string[] names = SaveName.Split('.');
                                SaveName = names[0] + ".jpg";
                                // source.Save(SaveName, ImageFormat.Jpeg);
                                source.Save(SaveName, jpgEncoder, encParams);
                            }
                        }
                        else
                        {
                            source.Save(SaveName);
                        }
                    }

                    // check blur image
                    if (recive.row <= cfg.Syscfg.TrayRows && recive.col <= cfg.Syscfg.TrayCols)
                    {
                        ImgSharpness[recive.index, 0, recive.row - 1, recive.col - 1] = sharpness[0];
                        ImgSharpness[recive.index, 1, recive.row - 1, recive.col - 1] = sharpness[1];
                        ImgSharpness[recive.index, 2, recive.row - 1, recive.col - 1] = sharpness[2];
                        ImgSharpness[recive.index, 3, recive.row - 1, recive.col - 1] = sharpness[3];
                        ImgSharpness[recive.index, 4, recive.row - 1, recive.col - 1] = sharpness[4];
                    }

                    // will be return once error
                    if (sliderROIV2.backside == 1)
                    {
                        swmsg.UpdateSliderStates(recive.row - 1, recive.col - 1, EnumSliderStatus.Error);
                        swmsg.UpdateTrayInfo(4, recive.index, "Backside");
                        swmsg.UpdateStatus(string.Format("[Row:{0}], [Col:{1}], Slider is backside.", recive.row.ToString(), recive.col.ToString()), EnumInfoLevel.Error);
                        TrayControl.IsEncounterErrorSlider[recive.index] = true;
                        TrayControl.TraysResult[recive.index].SlidersDefectUnique[recive.row - 1, recive.col - 1] = "error:Backside";
                        TrayControl.TraysResult[recive.index].ErrorCount++;
                        return;
                    }
                    if (sliderROIV2.orientation == 1)
                    {
                        swmsg.UpdateSliderStates(recive.row - 1, recive.col - 1, EnumSliderStatus.Error);
                        swmsg.UpdateTrayInfo(4, recive.index, "Wrong dircetion");
                        swmsg.UpdateStatus(string.Format("[Row:{0}], [Col:{1}], Slider is wrong direction.", recive.row.ToString(), recive.col.ToString()), EnumInfoLevel.Error);
                        TrayControl.IsEncounterErrorSlider[recive.index] = true;
                        TrayControl.TraysResult[recive.index].SlidersDefectUnique[recive.row - 1, recive.col - 1] = "error:wrong dircetion";
                        TrayControl.TraysResult[recive.index].ErrorCount++;
                        return;
                    }
                    if (sliderROIV2.IsTypeWrong == 1)
                    {
                        swmsg.UpdateSliderStates(recive.row - 1, recive.col - 1, EnumSliderStatus.Error);
                        swmsg.UpdateTrayInfo(4, recive.index, "HeadType");
                        swmsg.UpdateStatus(string.Format("[Row:{0}], [Col:{1}], Slider is wrong Head type.", recive.row.ToString(), recive.col.ToString()), EnumInfoLevel.Error);
                        TrayControl.IsEncounterErrorSlider[recive.index] = true;
                        TrayControl.WrongHeadType.Add(new Point(recive.row, recive.col));
                        TrayControl.TraysResult[recive.index].SlidersDefectUnique[recive.row - 1, recive.col - 1] = "error:wrong type";
                        TrayControl.TraysResult[recive.index].ErrorCount++;
                        return;
                    }
                    if (sliderROIV2.IsBlurry == 1)
                    {
                        swmsg.UpdateSliderStates(recive.row - 1, recive.col - 1, EnumSliderStatus.Error);
                        swmsg.UpdateTrayInfo(4, recive.index, "Blurry");
                        swmsg.UpdateStatus(string.Format("[Row:{0}], [Col:{1}], Slider is blurry image.", recive.row.ToString(), recive.col.ToString()), EnumInfoLevel.Error);
                        TrayControl.IsEncounterErrorSlider[recive.index] = true;
                        TrayControl.TraysResult[recive.index].SlidersDefectUnique[recive.row - 1, recive.col - 1] = "error:Blurry";
                        TrayControl.TraysResult[recive.index].ErrorCount++;
                        return;
                    }


                    sliderROIV2 = null;
                    if (recive.row <= cfg.Syscfg.TrayRows && recive.col <= cfg.Syscfg.TrayCols)
                    {
                        if (tra3_src1 != null)
                        {
                            if (cfg.Camcfg.SaveRotate)
                            {
                                EnQueueInputDataLT(recive.index, recive.row, recive.col, source.Width, source.Height, tra3_src1, Math.Min(Math.Min(Math.Min(sharpness[1], sharpness[2]), sharpness[3]), sharpness[4]));
                            }
                            else
                            {
                                EnQueueInputDataLT(recive.index, recive.row, recive.col, source.Height, source.Width, tra3_src1, Math.Min(Math.Min(Math.Min(sharpness[1], sharpness[2]), sharpness[3]), sharpness[4]));
                            }
                            if (recive.row == cfg.Syscfg.TrayRows && recive.col == cfg.Syscfg.TrayCols)
                            {
                                swmsg.UpdateStatus(string.Format("ProcessImage::Get finial image"), EnumInfoLevel.Event);
                            }
                        }
                        else
                        {
                            if (!recive.IsColor)
                            {
                                swmsg.UpdateSliderStates(recive.row - 1, recive.col - 1, EnumSliderStatus.Error);
                                swmsg.UpdateTrayInfo(4, recive.index, "null data");
                                swmsg.UpdateStatus(string.Format("[Row:{0}], [Col:{1}], null data.", recive.row.ToString(), recive.col.ToString()), EnumInfoLevel.Error);
                                TrayControl.IsEncounterErrorSlider[recive.index] = true;
                                TrayControl.TraysResult[recive.index].SlidersDefectUnique[recive.row - 1, recive.col - 1] = "error:null data";
                                TrayControl.TraysResult[recive.index].ErrorCount++;
                            }
                            return;
                        }
                    }
                    source.Dispose();
                    source = null;
                    recive.Dispose();
                    recive = null;
                    DateTime endPre = DateTime.Now;
                    //Console.WriteLine("****Pre Process thread 3：{0}", (endPre - beginPre).TotalMilliseconds);
                }
            }
            catch (Exception ex)
            {
                swmsg.UpdateStatus(ex.Message, EnumInfoLevel.Error);
            }
            finally
            {
                processImage2.Change(0, 2);
            }
        }
        private void TimeUpForProcessImage3(object value)
        {
            processImage3.Change(Timeout.Infinite, Timeout.Infinite);
            try
            {
                ReciveData recive = Pop();
                if (recive != null)
                {
                    DateTime beginPre = DateTime.Now;
                    if (!SliderState[recive.index, recive.row - 1, recive.col - 1])
                    {
                        if (TraysResult[recive.index].SlidersDefectUnique[recive.row - 1, recive.col - 1] == "OFF-T" ||
                            TraysResult[recive.index].SlidersDefectUnique[recive.row - 1, recive.col - 1].Contains("error"))
                        {
                            // Save OFFT samples
                            Bitmap offtbmp = GetSliderROI.ByteToBitmap(recive.pixels, recive.height, recive.width);
                            //string SaveName = "";
                            //string[] names = recive.fileName.Split('.');
                            //SaveName = recive.fileName;
                            //SaveName = names[0] + "-OFFT." + names[1];
                            offtbmp.Save(recive.fileName);
                        }
                        return;
                    }
                    Bitmap source = null;
                    float[] tra4_src1 = null;
                    double[] sharpness = { 0, 0, 0, 0, 0 };
                    GetSliderROIV2 sliderROIV2 = new GetSliderROIV2();
                    if (!cfg.Camcfg.OpenDetect)
                    {
                        sliderROIV2.IsOpenDetect = false;
                    }
                    sliderROIV2.IsDetectHeadType = cfg.Camcfg.IsDetectHeadType;
                    if (cfg.Syscfg.IsOnline)
                    {
                        if (TrayControl.TraysResult[recive.index].ProductName != null && TrayControl.TraysResult[recive.index].ProductName.Contains("SYDNEYE"))
                        {
                            sliderROIV2.HeadType = (TrayControl.TraysResult[recive.index].HeadType == "A") ? "B" : "A";
                        }
                        else if (TrayControl.TraysResult[recive.index].ProductName != null && TrayControl.TraysResult[recive.index].ProductName.Contains("SYDNEYF"))
                        {
                            // TODO : temp disable
                            sliderROIV2.IsDetectHeadType = false;
                        }
                        else
                        {
                            sliderROIV2.HeadType = TrayControl.TraysResult[recive.index].HeadType;
                        }
                    }

                    if (recive.IsColor)
                    {
                        if (cfg.Syscfg.FunctionMode == "rowbarVertical")
                        {
                            bool olg = false;
                            if (recive.fileName.Contains("Sur2"))
                            {
                                if (recive.col == 3 || recive.col == 8 ||
                                    recive.col == 13 || recive.col == 18 ||
                                    recive.col == 23 || recive.col == 28)
                                {
                                    olg = true;
                                }
                                else
                                {
                                    return;
                                }
                            }
                            double olgsharpness;
                            sliderROIV2.GetRowBarDepoEdge(recive.pixels, recive.height, recive.width, olg, false, out source, out olgsharpness);
                            OLGSharpness[recive.index, recive.row - 1, recive.col - 1] = olgsharpness;
                        }
                        else
                        {
                            if (cfg.Syscfg.FunctionMode == "rowbarPoletip")
                            {
                                sliderROIV2.Get50XPoletipROI(recive.pixels, recive.height, recive.width, out source, out sharpness, recive.IsColor);
                            }
                            else
                            {
                                sliderROIV2.GetROI3(recive.pixels, recive.height, recive.width, out source);
                            }
                        }
                    }
                    else
                    {
                        DateTime begin = DateTime.Now;
                        sliderROIV2.GetROI22(recive.pixels, recive.height, recive.width, out source, out sharpness, out tra4_src1, cfg.Camcfg.SaveRotate, false, recive.IsColor, cfg.Syscfg.IsOnline);
                        if (tra4_src1 == null)
                        {
                            sliderROIV2.GetROI2(recive.pixels, recive.height, recive.width, out source, out sharpness, out tra4_src1, cfg.Camcfg.SaveRotate, false, recive.IsColor, cfg.Syscfg.IsOnline);
                        }
                        DateTime end = DateTime.Now;
                        //Console.WriteLine("****GetROI22 thread 4：{0}", (end - begin).TotalMilliseconds);
                    }
                    if (source != null)
                    {
                        string SaveName = "";

                        if (sliderROIV2.backside != 0 || sliderROIV2.orientation != 0 ||
                            sliderROIV2.IsTypeWrong != 0 || sliderROIV2.IsBlurry != 0)
                        {
                            string[] names = recive.fileName.Split('.');
                            SaveName = names[0] + "-error." + names[1];
                            SaveName = recive.fileName;
                        }
                        else
                        {
                            SaveName = recive.fileName;
                        }
                        if (recive.IsColor)
                        {
                            //if (cfg.Syscfg.FunctionMode == "rowbarVertical" || cfg.Syscfg.FunctionMode == "rowbarHorizontal")
                            {
                                string[] names = SaveName.Split('.');
                                SaveName = names[0] + ".jpg";
                                // source.Save(SaveName, ImageFormat.Jpeg);
                                source.Save(SaveName, jpgEncoder, encParams);
                            }
                        }
                        else
                        {
                            source.Save(SaveName);
                        }
                    }

                    // check blur image
                    if (recive.row <= cfg.Syscfg.TrayRows && recive.col <= cfg.Syscfg.TrayCols)
                    {
                        ImgSharpness[recive.index, 0, recive.row - 1, recive.col - 1] = sharpness[0];
                        ImgSharpness[recive.index, 1, recive.row - 1, recive.col - 1] = sharpness[1];
                        ImgSharpness[recive.index, 2, recive.row - 1, recive.col - 1] = sharpness[2];
                        ImgSharpness[recive.index, 3, recive.row - 1, recive.col - 1] = sharpness[3];
                        ImgSharpness[recive.index, 4, recive.row - 1, recive.col - 1] = sharpness[4];
                    }

                    // will be return once error
                    if (sliderROIV2.backside == 1)
                    {
                        swmsg.UpdateSliderStates(recive.row - 1, recive.col - 1, EnumSliderStatus.Error);
                        swmsg.UpdateTrayInfo(4, recive.index, "Backside");
                        swmsg.UpdateStatus(string.Format("[Row:{0}], [Col:{1}], Slider is backside.", recive.row.ToString(), recive.col.ToString()), EnumInfoLevel.Error);
                        TrayControl.IsEncounterErrorSlider[recive.index] = true;
                        TrayControl.TraysResult[recive.index].SlidersDefectUnique[recive.row - 1, recive.col - 1] = "error:Backside";
                        TrayControl.TraysResult[recive.index].ErrorCount++;
                        return;
                    }
                    if (sliderROIV2.orientation == 1)
                    {
                        swmsg.UpdateSliderStates(recive.row - 1, recive.col - 1, EnumSliderStatus.Error);
                        swmsg.UpdateTrayInfo(4, recive.index, "Wrong dircetion");
                        swmsg.UpdateStatus(string.Format("[Row:{0}], [Col:{1}], Slider is wrong direction.", recive.row.ToString(), recive.col.ToString()), EnumInfoLevel.Error);
                        TrayControl.IsEncounterErrorSlider[recive.index] = true;
                        TrayControl.TraysResult[recive.index].SlidersDefectUnique[recive.row - 1, recive.col - 1] = "error:wrong dircetion";
                        TrayControl.TraysResult[recive.index].ErrorCount++;
                        return;
                    }
                    if (sliderROIV2.IsTypeWrong == 1)
                    {
                        swmsg.UpdateSliderStates(recive.row - 1, recive.col - 1, EnumSliderStatus.Error);
                        swmsg.UpdateTrayInfo(4, recive.index, "HeadType");
                        swmsg.UpdateStatus(string.Format("[Row:{0}], [Col:{1}], Slider is wrong Head type.", recive.row.ToString(), recive.col.ToString()), EnumInfoLevel.Error);
                        TrayControl.IsEncounterErrorSlider[recive.index] = true;
                        TrayControl.WrongHeadType.Add(new Point(recive.row, recive.col));
                        TrayControl.TraysResult[recive.index].SlidersDefectUnique[recive.row - 1, recive.col - 1] = "error:wrong type";
                        TrayControl.TraysResult[recive.index].ErrorCount++;
                        return;
                    }
                    if (sliderROIV2.IsBlurry == 1)
                    {
                        swmsg.UpdateSliderStates(recive.row - 1, recive.col - 1, EnumSliderStatus.Error);
                        swmsg.UpdateTrayInfo(4, recive.index, "Blurry");
                        swmsg.UpdateStatus(string.Format("[Row:{0}], [Col:{1}], Slider is blurry image.", recive.row.ToString(), recive.col.ToString()), EnumInfoLevel.Error);
                        TrayControl.IsEncounterErrorSlider[recive.index] = true;
                        TrayControl.TraysResult[recive.index].SlidersDefectUnique[recive.row - 1, recive.col - 1] = "error:Blurry";
                        TrayControl.TraysResult[recive.index].ErrorCount++;
                        return;
                    }

                    sliderROIV2 = null;
                    if (recive.row <= cfg.Syscfg.TrayRows && recive.col <= cfg.Syscfg.TrayCols)
                    {
                        if (tra4_src1 != null)
                        {
                            if (cfg.Camcfg.SaveRotate)
                            {
                                EnQueueInputDataLT(recive.index, recive.row, recive.col, source.Width, source.Height, tra4_src1, Math.Min(Math.Min(Math.Min(sharpness[1], sharpness[2]), sharpness[3]), sharpness[4]));
                            }
                            else
                            {
                                EnQueueInputDataLT(recive.index, recive.row, recive.col, source.Height, source.Width, tra4_src1, Math.Min(Math.Min(Math.Min(sharpness[1], sharpness[2]), sharpness[3]), sharpness[4]));
                            }
                            if (recive.row == cfg.Syscfg.TrayRows && recive.col == cfg.Syscfg.TrayCols)
                            {
                                swmsg.UpdateStatus(string.Format("ProcessImage::Get finial image"), EnumInfoLevel.Event);
                            }
                        }
                        else
                        {
                            if (!recive.IsColor)
                            {
                                swmsg.UpdateSliderStates(recive.row - 1, recive.col - 1, EnumSliderStatus.Error);
                                swmsg.UpdateTrayInfo(4, recive.index, "null data");
                                swmsg.UpdateStatus(string.Format("[Row:{0}], [Col:{1}], null data.", recive.row.ToString(), recive.col.ToString()), EnumInfoLevel.Error);
                                TrayControl.IsEncounterErrorSlider[recive.index] = true;
                                TrayControl.TraysResult[recive.index].SlidersDefectUnique[recive.row - 1, recive.col - 1] = "error:null data";
                                TrayControl.TraysResult[recive.index].ErrorCount++;
                            }
                            return;
                        }
                    }
                    source.Dispose();
                    source = null;
                    recive.Dispose();
                    recive = null;
                    DateTime endPre = DateTime.Now;
                    //Console.WriteLine("****Pre Process thread 4：{0}", (endPre - beginPre).TotalMilliseconds);
                }
            }
            catch (Exception ex)
            {
                swmsg.UpdateStatus(ex.Message, EnumInfoLevel.Error);
            }
            finally
            {
                processImage3.Change(0, 2);
            }
        }

        private void TimeUpForProcessImage4(object value)
        {
            processImage4.Change(Timeout.Infinite, Timeout.Infinite);
            try
            {
                ReciveData recive = Pop();
                if (recive != null)
                {
                    DateTime beginPre = DateTime.Now;
                    if (!SliderState[recive.index, recive.row - 1, recive.col - 1])
                    {
                        if (TraysResult[recive.index].SlidersDefectUnique[recive.row - 1, recive.col - 1] == "OFF-T" ||
                            TraysResult[recive.index].SlidersDefectUnique[recive.row - 1, recive.col - 1].Contains("error"))
                        {
                            // Save OFFT samples
                            Bitmap offtbmp = GetSliderROI.ByteToBitmap(recive.pixels, recive.height, recive.width);
                            //string SaveName = "";
                            //string[] names = recive.fileName.Split('.');
                            //SaveName = recive.fileName;
                            //SaveName = names[0] + "-OFFT." + names[1];
                            offtbmp.Save(recive.fileName);
                        }
                        return;
                    }
                    Bitmap source = null;
                    float[] tra5_src1 = null;
                    double[] sharpness = { 0, 0, 0, 0, 0 };
                    GetSliderROIV2 sliderROIV2 = new GetSliderROIV2();
                    if (!cfg.Camcfg.OpenDetect)
                    {
                        sliderROIV2.IsOpenDetect = false;
                    }
                    sliderROIV2.IsDetectHeadType = cfg.Camcfg.IsDetectHeadType;
                    if (cfg.Syscfg.IsOnline)
                    {
                        if (TrayControl.TraysResult[recive.index].ProductName != null && TrayControl.TraysResult[recive.index].ProductName.Contains("SYDNEYE"))
                        {
                            sliderROIV2.HeadType = (TrayControl.TraysResult[recive.index].HeadType == "A") ? "B" : "A";
                        }
                        else if (TrayControl.TraysResult[recive.index].ProductName != null && TrayControl.TraysResult[recive.index].ProductName.Contains("SYDNEYF"))
                        {
                            // TODO : temp disable
                            sliderROIV2.IsDetectHeadType = false;
                        }
                        else
                        {
                            sliderROIV2.HeadType = TrayControl.TraysResult[recive.index].HeadType;
                        }
                    }

                    if (recive.IsColor)
                    {
                        if (cfg.Syscfg.FunctionMode == "rowbarVertical")
                        {
                            bool olg = false;
                            if (recive.fileName.Contains("Sur2"))
                            {
                                if (recive.col == 3 || recive.col == 8 ||
                                    recive.col == 13 || recive.col == 18 ||
                                    recive.col == 23 || recive.col == 28)
                                {
                                    olg = true;
                                }
                                else
                                {
                                    return;
                                }
                            }
                            double olgsharpness;
                            sliderROIV2.GetRowBarDepoEdge(recive.pixels, recive.height, recive.width, olg, false, out source, out olgsharpness);
                            OLGSharpness[recive.index, recive.row - 1, recive.col - 1] = olgsharpness;
                        }
                        else
                        {
                            if (cfg.Syscfg.FunctionMode == "rowbarPoletip")
                            {
                                sliderROIV2.Get50XPoletipROI(recive.pixels, recive.height, recive.width, out source, out sharpness, recive.IsColor);
                            }
                            else
                            {
                                sliderROIV2.GetROI3(recive.pixels, recive.height, recive.width, out source);
                            }
                        }
                    }
                    else
                    {
                        DateTime begin = DateTime.Now;
                        sliderROIV2.GetROI22(recive.pixels, recive.height, recive.width, out source, out sharpness, out tra5_src1, cfg.Camcfg.SaveRotate, false, recive.IsColor, cfg.Syscfg.IsOnline);
                        if (tra5_src1 == null)
                        {
                            sliderROIV2.GetROI2(recive.pixels, recive.height, recive.width, out source, out sharpness, out tra5_src1, cfg.Camcfg.SaveRotate, false, recive.IsColor, cfg.Syscfg.IsOnline);
                        }
                        DateTime end = DateTime.Now;
                        //Console.WriteLine("****GetROI22 thread 4：{0}", (end - begin).TotalMilliseconds);
                    }
                    if (source != null)
                    {
                        string SaveName = "";

                        if (sliderROIV2.backside != 0 || sliderROIV2.orientation != 0 ||
                            sliderROIV2.IsTypeWrong != 0 || sliderROIV2.IsBlurry != 0)
                        {
                            string[] names = recive.fileName.Split('.');
                            SaveName = names[0] + "-error." + names[1];
                            SaveName = recive.fileName;
                        }
                        else
                        {
                            SaveName = recive.fileName;
                        }
                        if (recive.IsColor)
                        {
                            //if (cfg.Syscfg.FunctionMode == "rowbarVertical" || cfg.Syscfg.FunctionMode == "rowbarHorizontal")
                            {
                                string[] names = SaveName.Split('.');
                                SaveName = names[0] + ".jpg";
                                // source.Save(SaveName, ImageFormat.Jpeg);
                                source.Save(SaveName, jpgEncoder, encParams);
                            }
                        }
                        else
                        {
                            source.Save(SaveName);
                        }
                    }

                    // check blur image
                    if (recive.row <= cfg.Syscfg.TrayRows && recive.col <= cfg.Syscfg.TrayCols)
                    {
                        ImgSharpness[recive.index, 0, recive.row - 1, recive.col - 1] = sharpness[0];
                        ImgSharpness[recive.index, 1, recive.row - 1, recive.col - 1] = sharpness[1];
                        ImgSharpness[recive.index, 2, recive.row - 1, recive.col - 1] = sharpness[2];
                        ImgSharpness[recive.index, 3, recive.row - 1, recive.col - 1] = sharpness[3];
                        ImgSharpness[recive.index, 4, recive.row - 1, recive.col - 1] = sharpness[4];
                    }

                    // will be return once error
                    if (sliderROIV2.backside == 1)
                    {
                        swmsg.UpdateSliderStates(recive.row - 1, recive.col - 1, EnumSliderStatus.Error);
                        swmsg.UpdateTrayInfo(4, recive.index, "Backside");
                        swmsg.UpdateStatus(string.Format("[Row:{0}], [Col:{1}], Slider is backside.", recive.row.ToString(), recive.col.ToString()), EnumInfoLevel.Error);
                        TrayControl.IsEncounterErrorSlider[recive.index] = true;
                        TrayControl.TraysResult[recive.index].SlidersDefectUnique[recive.row - 1, recive.col - 1] = "error:Backside";
                        TrayControl.TraysResult[recive.index].ErrorCount++;
                        return;
                    }
                    if (sliderROIV2.orientation == 1)
                    {
                        swmsg.UpdateSliderStates(recive.row - 1, recive.col - 1, EnumSliderStatus.Error);
                        swmsg.UpdateTrayInfo(4, recive.index, "Wrong dircetion");
                        swmsg.UpdateStatus(string.Format("[Row:{0}], [Col:{1}], Slider is wrong direction.", recive.row.ToString(), recive.col.ToString()), EnumInfoLevel.Error);
                        TrayControl.IsEncounterErrorSlider[recive.index] = true;
                        TrayControl.TraysResult[recive.index].SlidersDefectUnique[recive.row - 1, recive.col - 1] = "error:wrong dircetion";
                        TrayControl.TraysResult[recive.index].ErrorCount++;
                        return;
                    }
                    if (sliderROIV2.IsTypeWrong == 1)
                    {
                        swmsg.UpdateSliderStates(recive.row - 1, recive.col - 1, EnumSliderStatus.Error);
                        swmsg.UpdateTrayInfo(4, recive.index, "HeadType");
                        swmsg.UpdateStatus(string.Format("[Row:{0}], [Col:{1}], Slider is wrong Head type.", recive.row.ToString(), recive.col.ToString()), EnumInfoLevel.Error);
                        TrayControl.IsEncounterErrorSlider[recive.index] = true;
                        TrayControl.WrongHeadType.Add(new Point(recive.row, recive.col));
                        TrayControl.TraysResult[recive.index].SlidersDefectUnique[recive.row - 1, recive.col - 1] = "error:wrong type";
                        TrayControl.TraysResult[recive.index].ErrorCount++;
                        return;
                    }
                    if (sliderROIV2.IsBlurry == 1)
                    {
                        swmsg.UpdateSliderStates(recive.row - 1, recive.col - 1, EnumSliderStatus.Error);
                        swmsg.UpdateTrayInfo(4, recive.index, "Blurry");
                        swmsg.UpdateStatus(string.Format("[Row:{0}], [Col:{1}], Slider is blurry image.", recive.row.ToString(), recive.col.ToString()), EnumInfoLevel.Error);
                        TrayControl.IsEncounterErrorSlider[recive.index] = true;
                        TrayControl.TraysResult[recive.index].SlidersDefectUnique[recive.row - 1, recive.col - 1] = "error:Blurry";
                        TrayControl.TraysResult[recive.index].ErrorCount++;
                        return;
                    }

                    sliderROIV2 = null;
                    if (recive.row <= cfg.Syscfg.TrayRows && recive.col <= cfg.Syscfg.TrayCols)
                    {
                        if (tra5_src1 != null)
                        {
                            if (cfg.Camcfg.SaveRotate)
                            {
                                EnQueueInputDataLT(recive.index, recive.row, recive.col, source.Width, source.Height, tra5_src1, Math.Min(Math.Min(Math.Min(sharpness[1], sharpness[2]), sharpness[3]), sharpness[4]));
                            }
                            else
                            {
                                EnQueueInputDataLT(recive.index, recive.row, recive.col, source.Height, source.Width, tra5_src1, Math.Min(Math.Min(Math.Min(sharpness[1], sharpness[2]), sharpness[3]), sharpness[4]));
                            }
                            // EnQueueInputDataLT(recive.index, recive.row, recive.col, source.Height, source.Width, tra3_src1, Math.Min(Math.Min(Math.Min(sharpness[1], sharpness[2]), sharpness[3]), sharpness[4]));
                            //EnQueueInputDataRT(recive.index, recive.row, recive.col, source.Height, source.Width, tra3_src2, sharpness[2]);
                            //EnQueueInputDataLB(recive.index, recive.row, recive.col, source.Height, source.Width, tra1_src3, sharpness[3]);
                            //EnQueueInputDataRB(recive.index, recive.row, recive.col, source.Height, source.Width, tra1_src4, sharpness[4]);
                            if (recive.row == cfg.Syscfg.TrayRows && recive.col == cfg.Syscfg.TrayCols)
                            {
                                swmsg.UpdateStatus(string.Format("ProcessImage::Get finial image"), EnumInfoLevel.Event);
                            }
                        }
                        else
                        {
                            if (!recive.IsColor)
                            {
                                swmsg.UpdateSliderStates(recive.row - 1, recive.col - 1, EnumSliderStatus.Error);
                                swmsg.UpdateTrayInfo(4, recive.index, "null data");
                                swmsg.UpdateStatus(string.Format("[Row:{0}], [Col:{1}], null data.", recive.row.ToString(), recive.col.ToString()), EnumInfoLevel.Error);
                                TrayControl.IsEncounterErrorSlider[recive.index] = true;
                                TrayControl.TraysResult[recive.index].SlidersDefectUnique[recive.row - 1, recive.col - 1] = "error:null data";
                                TrayControl.TraysResult[recive.index].ErrorCount++;
                            }
                            return;
                        }
                    }
                    source.Dispose();
                    source = null;
                    recive.Dispose();
                    recive = null;
                    DateTime endPre = DateTime.Now;
                    //Console.WriteLine("****Pre Process thread 4：{0}", (endPre - beginPre).TotalMilliseconds);
                }
            }
            catch (Exception ex)
            {
                swmsg.UpdateStatus(ex.Message, EnumInfoLevel.Error);
            }
            finally
            {
                processImage4.Change(0, 2);
            }
        }


        static private void TimeUpForInferImageLeftTop(object value)
        {
            inferImageLT.Change(Timeout.Infinite, Timeout.Infinite);
            DateTime begin, end;
            TimeSpan t0;
            InputData recive = PopInputLT();
            try
            {
                if (recive != null && recive.SRC != null)
                {
                    {

                        if (cfg.Syscfg.IsOpenDetect)
                        {
                            List<PredictReturn> tmp;
                            if (ConfigurationManager.AppSettings["Site"] == "THO")
                            {
                                tmp = inferLT.Predict(recive.SRC, recive.srcRows, recive.srcCols, cfg.Syscfg.AIThres);
                            }
                            else if (ConfigurationManager.AppSettings["Site"] == "PHO")
                            {
                                tmp = inferLT.Predict(recive.SRC, recive.srcCols, recive.srcRows, cfg.Syscfg.AIThres);
                            }
                            else
                            {
                                tmp = inferLT.Predict(recive.SRC, recive.srcRows, recive.srcCols, cfg.Syscfg.AIThres);
                            }

                            if (tmp != null && tmp.Count > 0)
                            {
                                foreach (var ret in tmp)
                                {
                                    PredictReturn ret2 = new PredictReturn();
                                    // ret2.Category = ret.Category;
                                    // ret2.Confidence = ret.Confidence;
                                    // rotate image
                                    /*
                                    ret2.Coordrate_1 = new Point((int)((double)(ret.Coordrate_1.X) * ((double)(recive.srcRows) / 1280.0)),
                                        (int)((double)(ret.Coordrate_1.Y) * ((double)(recive.srcCols) / 1280.0)));
                                    ret2.Coordrate_2 = new Point((int)((double)(ret.Coordrate_2.X) * ((double)(recive.srcRows) / 1280.0)),
                                        (int)((double)(ret.Coordrate_2.Y) * ((double)(recive.srcCols) / 1280.0)));
                                    */
                                    //ret2.Coordrate_1.X = (int)((float)(ret.Coordrate_1.X) * ((double)(recive.srcRows) / 1280.0));
                                    //ret2.Coordrate_1.Y = (int)((float)(ret.Coordrate_1.Y) * ((double)(recive.srcCols) / 1280.0));
                                    //ret2.Coordrate_2.X = (int)((float)(ret.Coordrate_2.X) * ((double)(recive.srcRows) / 1280.0));
                                    //ret2.Coordrate_2.Y = (int)((float)(ret.Coordrate_2.Y) * ((double)(recive.srcCols) / 1280.0));
                                    tmPredictReturn[recive.row - 1, recive.col - 1].Add(ret);

                                }
                            }
                            //predictReturnCount[recive.row - 1, recive.col - 1]++;
                            //Console.WriteLine("Left top count :" + predictReturnCount[recive.row - 1, recive.col - 1].ToString());

                        }
                        //if (predictReturnCount[recive.row - 1, recive.col - 1] == 4)
                        //predictReturnCount[recive.row - 1, recive.col - 1] = 0;
                        begin = DateTime.Now;
                        AnaysisResult(recive.index, recive.row, recive.col, recive.srcRows, recive.srcCols, tmPredictReturn[recive.row - 1, recive.col - 1]);
                        end = DateTime.Now;
                        t0 = end - begin;
                        // Console.WriteLine("***AnaysisResult：{0}", t0.TotalMilliseconds);

                    }
                }
            }
            catch (Exception ex)
            {
                swmsg.UpdateStatus(ex.Message, EnumInfoLevel.Event);
                if (!ex.Message.Contains("component"))
                {
                    swmsg.UpdateStatus(ex.Message, EnumInfoLevel.Error);
                }
                else
                {
                    swmsg.UpdateSliderStates(recive.row - 1, recive.col - 1, EnumSliderStatus.Pass);
                    TrayControl.TraysResult[recive.index].SlidersDefectUnique[recive.row - 1, recive.col - 1] = EnumSliderStatus.Pass.ToString();
                }
            }
            finally
            {
                if (recive != null)
                {
                    recive.Dispose();
                    recive = null;

                }
                inferImageLT.Change(0, 1);
            }
        }

        /*
        static private void TimeUpForInferImageRightTop(object value)
        {
            inferImageRT.Change(Timeout.Infinite, Timeout.Infinite);
            InputData recive = PopInputRT();
            try
            {
                if (recive != null)
                {
                    if (recive.Sharpness > 2.2)
                    {
                        if (cfg.Syscfg.IsOpenDetect)
                        {
                            List<PredictReturn> tmp = inferRT.Predict(recive.SRC, recive.srcRows, recive.srcCols);
                            if (tmp != null && tmp.Count > 0)
                            {
                                foreach (var ret in tmp)
                                {
                                    PredictReturn ret2 = new PredictReturn();
                                    ret2.Category = ret.Category;
                                    ret2.Confidence = ret.Confidence;
                                    //ret2.Coordrate_1 = new Point((int)((double)(ret.Coordrate_1.X) * ((double)(recive.srcCols) / 1280.0)),
                                    //    (int)((double)(ret.Coordrate_1.Y) * ((double)(recive.srcRows) / 1280.0)));
                                    //ret2.Coordrate_2 = new Point((int)((double)(ret.Coordrate_2.X) * ((double)(recive.srcCols) / 1280.0)),
                                    //    (int)((double)(ret.Coordrate_2.Y) * ((double)(recive.srcRows) / 1280.0)));

                                    // rotate image
                                    ret2.Coordrate_1 = new Point((int)((double)(ret.Coordrate_1.X) * ((double)(recive.srcRows) / 1280.0)),
                                        (int)((double)(ret.Coordrate_1.Y) * ((double)(recive.srcCols) / 1280.0)));
                                    ret2.Coordrate_2 = new Point((int)((double)(ret.Coordrate_2.X) * ((double)(recive.srcRows) / 1280.0)),
                                        (int)((double)(ret.Coordrate_2.Y) * ((double)(recive.srcCols) / 1280.0)));

                                    //ret2.Coordrate_1.X = (int)((float)(ret.Coordrate_1.X) * ((double)(recive.srcRows) / 1280.0));
                                    //ret2.Coordrate_1.Y = (int)((float)(ret.Coordrate_1.Y) * ((double)(recive.srcCols) / 1280.0));
                                    //ret2.Coordrate_2.X = (int)((float)(ret.Coordrate_2.X) * ((double)(recive.srcRows) / 1280.0));
                                    //ret2.Coordrate_2.Y = (int)((float)(ret.Coordrate_2.Y) * ((double)(recive.srcCols) / 1280.0));
                                    tmPredictReturn[recive.row - 1, recive.col - 1].Add(ret2);

                                }
                            }
                        }
                        AnaysisResult(recive.index, recive.row, recive.col, recive.srcRows, recive.srcCols, tmPredictReturn[recive.row - 1, recive.col - 1]);
                    }
                }
            }
            catch (Exception ex)
            {
                swmsg.UpdateStatus(ex.Message, EnumInfoLevel.Event);
                if (!ex.Message.Contains("component"))
                {
                    swmsg.UpdateStatus(ex.Message, EnumInfoLevel.Error);
                }
                else
                {
                    swmsg.UpdateSliderStates(recive.row - 1, recive.col - 1, EnumSliderStatus.Pass);
                    TrayControl.TraysResult[recive.index].SlidersDefectUnique[recive.row - 1, recive.col - 1] = EnumSliderStatus.Pass.ToString();
                }
            }
            finally
            {
                if (recive != null)
                {
                    Array.Clear(recive.SRC, 0, recive.SRC.Length);
                    recive = null;
                }
                inferImageLT.Change(0, 1);
            }
        }
        private void TimeUpForInferImageLeftBottom(object value)
        {
            inferImageLB.Change(Timeout.Infinite, Timeout.Infinite);
            InputData recive = PopInputLB();
            try
            {
                if (recive != null)
                {
                    if (recive.Sharpness > 2.2)
                    {
                        if (cfg.Syscfg.IsOpenDetect)
                        {
                            List<PredictReturn> tmp = inferLB.Predict(recive.SRC, recive.srcRows, recive.srcCols);
                            if (tmp != null && tmp.Count > 0)
                            {
                                foreach (var ret in tmp)
                                    tmPredictReturn[recive.row - 1, recive.col - 1].Add(ret);
                            }
                            predictReturnCount[recive.row - 1, recive.col - 1]++;
                            //if (predictReturnCount[recive.row - 1, recive.col - 1] == 4)
                            //{
                            //    predictReturnCount[recive.row - 1, recive.col - 1] = 0;
                            //    AnaysisResult(recive.index, recive.row, recive.col, recive.srcRows, recive.srcCols, tmPredictReturn[recive.row - 1, recive.col - 1]);
                            //}
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (!ex.Message.Contains("component"))
                {
                    swmsg.UpdateStatus(ex.Message, EnumInfoLevel.Error);
                }
                else
                {
                    swmsg.UpdateSliderStates(recive.row - 1, recive.col - 1, EnumSliderStatus.Pass);
                }
            }
            finally
            {
                if (recive != null)
                {
                    if (recive.row == Rows && recive.col == Cols)
                    {
                        SaveDefectEvent(recive.index);
                    }
                    recive = null;
                }
                inferImageLB.Change(0, 1);
            }
        }
        private void TimeUpForInferImageRightBottom(object value)
        {
            inferImageRB.Change(Timeout.Infinite, Timeout.Infinite);
            InputData recive = PopInputRB();
            try
            {
                if (recive != null)
                {
                    if (recive.Sharpness > 2.2)
                    {
                        if (cfg.Syscfg.IsOpenDetect)
                        {
                            List<PredictReturn> tmp = inferRB.Predict(recive.SRC, recive.srcRows, recive.srcCols);
                            if (tmp != null && tmp.Count > 0)
                            {
                                foreach (var ret in tmp)
                                    tmPredictReturn[recive.row - 1, recive.col - 1].Add(ret);
                            }
                            predictReturnCount[recive.row - 1, recive.col - 1]++;
                            //if (predictReturnCount[recive.row - 1, recive.col - 1] == 4)
                            //{
                            //    predictReturnCount[recive.row - 1, recive.col - 1] = 0;
                            //    AnaysisResult(recive.index, recive.row, recive.col, recive.srcRows, recive.srcCols, tmPredictReturn[recive.row - 1, recive.col - 1]);
                            //}
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (!ex.Message.Contains("component"))
                {
                    swmsg.UpdateStatus(ex.Message, EnumInfoLevel.Error);
                }
                else
                {
                    swmsg.UpdateSliderStates(recive.row - 1, recive.col - 1, EnumSliderStatus.Pass);
                }
            }
            finally
            {
                if (recive != null)
                {
                    if (recive.row == Rows && recive.col == Cols)
                    {
                        SaveDefectEvent(recive.index);
                    }
                }
                inferImageRB.Change(0, 1);
            }
        }
        */

        private Bitmap GrayConvert2RGB(Bitmap grayImage)
        {

            Bitmap rgbImage = new Bitmap(grayImage.Width, grayImage.Height, PixelFormat.Format24bppRgb);

            Graphics g = Graphics.FromImage(rgbImage);

            ColorMatrix colorMatrix = new ColorMatrix(
                new float[][]{
        new float[]{1, 0, 0, 0, 0},
        new float[]{0, 1, 0, 0, 0},
        new float[]{0, 0, 1, 0, 0},
        new float[]{0, 0, 0, 1, 0},
        new float[]{0, 0, 0, 0, 1}});

            ImageAttributes attributes = new ImageAttributes();
            attributes.SetColorMatrix(colorMatrix);

            g.DrawImage(grayImage, new Rectangle(0, 0, grayImage.Width, grayImage.Height),
                0, 0, grayImage.Width, grayImage.Height, GraphicsUnit.Pixel, attributes);
            return rgbImage;
        }

        private void AnaysisResult(short index, short row, short col, int imgrows, int imgcols, int[] a, float[] b)
        {
            EnumSliderStatus state = EnumSliderStatus.Empty;
            try
            {
                if (a == null)
                {
                    swmsg.UpdateSliderStates(row - 1, col - 1, EnumSliderStatus.Untest);
                }
                else if (a[0] == -1)
                {
                    swmsg.UpdateSliderStates(row - 1, col - 1, EnumSliderStatus.Pass);
                }
                else
                {

                    List<string> defectCodeList = new List<string>();

                    for (int i = 0; i < a.Length - 12; i = i + 5)
                    {
                        if (a[i] == -1)
                        {
                            break;
                        }
                        switch (a[i])
                        {
                            case -1:
                                state = EnumSliderStatus.Pass;
                                break;
                            case 0:
                                state = EnumSliderStatus.A5;
                                break;
                            case 1:
                                state = EnumSliderStatus.A3;
                                break;
                            case 2:
                                state = EnumSliderStatus.A1O;
                                break;
                            case 3:
                                state = EnumSliderStatus.A8;
                                break;
                            case 4:
                                state = EnumSliderStatus.A2;
                                break;
                            case 5:
                                state = EnumSliderStatus.A11;
                                break;
                            case 6:
                                state = EnumSliderStatus.A5C;
                                break;
                            case 7:
                                state = EnumSliderStatus.A0;
                                break;
                            case 8:
                                state = EnumSliderStatus.A1F;
                                break;
                            case 9:
                                state = EnumSliderStatus.A4P;
                                break;
                            default:
                                state = EnumSliderStatus.unknown;
                                break;
                        }

                        if (state == EnumSliderStatus.A3 && b[i / 5] < 0.6)
                        {
                            continue;
                        }
                        var result = defectCodeList.Exists(t => t == state.ToString());
                        if (!result)
                        {
                            defectCodeList.Add(state.ToString());
                        }
                        TraysResult[index].DefectsList.Add(new DefectDetail(row, col, imgrows, imgcols, state, new Point(a[i + 1], a[i + 2]), new Point(a[i + 3], a[i + 4]), b[i / 5]));
                    }

                    List<DefectDetail> defectDetailsTmp = new List<DefectDetail>();

                    foreach (DefectDetail defect in TraysResult[index].DefectsList)
                    {
                        defectDetailsTmp.Add(defect);
                    }

                    // nms
                    NMS(ref defectDetailsTmp, 0.4f);
                    // check 
                    string[] strArr = defectCodeList.ToArray();
                    Array.Sort(strArr, (x, y) => FileNameSort.StrCmpLogicalW(x, y));
                    if (strArr.Length != 0)
                    {
                        TraysResult[index].SlidersDefectUnique[row - 1, col - 1] = strArr[0].ToString();
                        EnumSliderStatus uniqueCode;
                        Enum.TryParse<EnumSliderStatus>(strArr[0], out uniqueCode);
                        swmsg.UpdateSliderStates(row - 1, col - 1, uniqueCode);
                    }
                    else
                    {
                        swmsg.UpdateSliderStates(row - 1, col - 1, EnumSliderStatus.Pass);
                    }
                }
            }
            catch (Exception ex)
            {
                if (!ex.Message.Contains("component"))
                {
                    swmsg.UpdateStatus(ex.Message, EnumInfoLevel.Error);
                }
            }
        }
        static private void AnaysisResult(short index, short row, short col, int imgrows, int imgcols, List<PredictReturn> predicts)
        {
            EnumSliderStatus state = EnumSliderStatus.Empty;
            try
            {
                if (predicts.Count == 0)
                {
                    swmsg.UpdateSliderStates(row - 1, col - 1, EnumSliderStatus.Pass);
                    TrayControl.TraysResult[index].SlidersDefectUnique[row - 1, col - 1] = EnumSliderStatus.Pass.ToString();
                }
                else
                {
                    List<string> defectCodeList = new List<string>();
                    bool configExist = (defectThreshold.Count == 0) ? false : true;
                    float confThreshold = 0;
                    foreach (PredictReturn predict in predicts)
                    {
                        state = predict.Category;
                        if (configExist)
                        {
                            if (defectThreshold.ContainsKey(state))
                            {
                                defectThreshold.TryGetValue(state, out confThreshold);
                                if (confThreshold != 0)
                                {
                                    if (predict.Confidence < confThreshold)
                                    {
                                        continue;
                                    }
                                }
                            }
                        }
                        var result = defectCodeList.Exists(t => t == state.ToString());
                        if (!result)
                        {
                            defectCodeList.Add(state.ToString());
                        }
                        TraysResult[index].DefectsList.Add(new DefectDetail(row, col, imgrows, imgcols, state, predict.Coordrate_1, predict.Coordrate_2, predict.Confidence));
                    }
                    //List<DefectDetail> defectDetailsTmp = new List<DefectDetail>();
                    //foreach (DefectDetail defect in TraysResult[index].DefectsList)
                    //{
                    //    defectDetailsTmp.Add(defect);
                    //}

                    // nms
                    //NMS(ref defectDetailsTmp, 0.4f);
                    // check 
                    string[] strArr = defectCodeList.ToArray();
                    Array.Sort(strArr, (x, y) => FileNameSort.StrCmpLogicalW(x, y));
                    if (strArr.Length != 0)
                    {
                        TraysResult[index].DefectSlidersCount++;
                        TraysResult[index].SlidersDefectUnique[row - 1, col - 1] = strArr[0].ToString();
                        EnumSliderStatus uniqueCode;
                        Enum.TryParse<EnumSliderStatus>(strArr[0], out uniqueCode);
                        swmsg.UpdateSliderStates(row - 1, col - 1, uniqueCode);
                    }
                    else
                    {
                        swmsg.UpdateSliderStates(row - 1, col - 1, EnumSliderStatus.Pass);
                        TrayControl.TraysResult[index].SlidersDefectUnique[row - 1, col - 1] = EnumSliderStatus.Pass.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                // if (!ex.Message.Contains("component"))
                {
                    swmsg.UpdateStatus(ex.Message, EnumInfoLevel.Error);
                }
            }
        }
        static int SortItem(DefectDetail item1, DefectDetail item2)
        {
            if (item1.Confidence > item2.Confidence) return -1;  //降序
            else return 1; //升序
        }

        static float IOU(DefectDetail A, DefectDetail B)
        {
            // 左上右下坐标(x1,y1,x2,y2)
            float w = Math.Max(0.0f, Math.Min(A.Coordrate_2.X, B.Coordrate_2.X) - Math.Max(A.Coordrate_1.X, B.Coordrate_1.X) + 1);
            float h = Math.Max(0.0f, Math.Min(A.Coordrate_2.Y, B.Coordrate_2.Y) - Math.Max(A.Coordrate_1.Y, B.Coordrate_1.Y) + 1);
            float area1 = (A.Coordrate_2.X - A.Coordrate_1.X + 1) * (A.Coordrate_2.Y - A.Coordrate_1.Y + 1);
            float area2 = (B.Coordrate_2.X - B.Coordrate_1.X + 1) * (B.Coordrate_2.Y - B.Coordrate_1.Y + 1);
            float inter = w * h;
            float iou = inter / (area1 + area2 - inter);
            return iou;
        }

        public static void NMS(ref List<DefectDetail> vec_boxs, float thresh)
        {

            // box[0:4]: x1, x2, y1, y2; box[4]: score
            // 按分值从大到小排序
            vec_boxs.Sort(SortItem);

            //标志，false代表留下，true代表扔掉
            List<bool> del = new List<bool>(vec_boxs.Count);
            for (int i = 0; i < vec_boxs.Count; i++)
                del.Add(false);

            for (int i = 0; i < vec_boxs.Count - 1; i++)
            {
                if (!del[i])
                {
                    for (int j = i + 1; j < vec_boxs.Count; j++)
                    {
                        if (!del[j] && IOU(vec_boxs[i], vec_boxs[j]) >= thresh)
                        {
                            del[j] = true;  //IOU大于阈值扔掉
                        }
                    }
                }
            }

            List<DefectDetail> result = new List<DefectDetail>();
            for (int i = 0; i < vec_boxs.Count; i++)
            {
                if (!del[i])
                {
                    result.Add(vec_boxs[i]);
                }
            }
            vec_boxs.Clear();

            vec_boxs = result;

        }


        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~TrayControl()
        {
            Dispose(false);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
            { return; }
            if (disposing)
            {
                // clean

            }
            // clean
            disposed = true;
        }
    }
}
