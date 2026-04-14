using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using WD.AVI.Common;

namespace WD.AVI.LaserLib
{
    /// <summary>
    /// class for laser device in AVI system
    /// </summary>
    public class LaserControlLib
    {
        private static LaserControlLib uniqueInstance;
        private static readonly object locker = new object();

        private bool isConnect = false;
        private bool isIniting = false;
        private int DEVICEID = 0;
        private const int MaxRequestDataLength = 512000;
        // private int MaxMeasurementDataCountPerTime = 8000;
        private readonly DeviceData deviceData = new DeviceData();
        private LaserControlLib()
        {

        }

        /// <summary>
        /// Get laser device
        /// </summary>
        /// <returns> return laser device</returns>
        public static LaserControlLib GetInstance()
        {
            if (uniqueInstance == null)
            {
                lock (locker)
                {
                    if (uniqueInstance == null)
                    {
                        uniqueInstance = new LaserControlLib();
                    }
                }
            }
            return uniqueInstance;
        }

        /// <summary>
        /// function for handle error
        /// </summary>
        /// <param name="command">function name</param>
        /// <param name="error">error tag</param>
        private static void commandhandler(string command, int error)
        {
            if (error != NativeMethods.CL3IF_RC_OK)
            {
                throw new Exception(String.Format("{0}={1}\n", command, error));
            }
        }

        /// <summary>
        /// laser connect function
        /// </summary>
        public void Connect()
        {
            if (isConnect || isIniting || uniqueInstance == null)
            {
                return;
            }
            isIniting = true;
            int sRtn;
            try
            {
                sRtn = NativeMethods.CL3IF_OpenUsbCommunication(DEVICEID, 5000);
                commandhandler("CL3IF_OpenUsbCommunication", sRtn);
                byte programNo;
                sRtn = NativeMethods.CL3IF_GetProgramNo(DEVICEID, out programNo);

                sRtn = NativeMethods.CL3IF_SetSamplingCycle(DEVICEID, programNo, CL3IF_SAMPLINGCYCLE.CL3IF_SAMPLINGCYCLE_100USEC);

                sRtn = NativeMethods.CL3IF_SetFilter(DEVICEID, programNo, 0, CL3IF_FILTERMODE.CL3IF_FILTERMODE_MOVING_AVERAGE, (ushort)CL3IF_FILTERPARAM_AVERAGE.CL3IF_FILTERPARAM_AVERAGE_4);

                isConnect = true;
                deviceData.Status = DeviceStatus.Usb;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                isIniting = false;
            }
        }

        /// <summary>
        /// laser close function
        /// </summary>
        public void Close()
        {
            if (!isConnect || uniqueInstance == null)
            {
                return;
            }
            int sRtn;
            try
            {
                sRtn = NativeMethods.CL3IF_CloseCommunication(DEVICEID);
                commandhandler("CL3IF_CloseCommunication", sRtn);

                deviceData.Status = DeviceStatus.NoConnection;
                isConnect = false;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// get current laser measure data
        /// </summary>
        /// <returns></returns>
        public double GetMeasurementData()
        {
            double data = -9999;
            if (!isConnect)
            {
                return data;
            }
            byte[] buffer = new byte[MaxRequestDataLength];
            int sRtn;
            try
            {
                using (PinnedObject pin = new PinnedObject(buffer))
                {
                    CL3IF_MEASUREMENT_DATA measurementData = new CL3IF_MEASUREMENT_DATA();
                    measurementData.outMeasurementData = new CL3IF_OUTMEASUREMENT_DATA[NativeMethods.CL3IF_MAX_OUT_COUNT];
                    sRtn = NativeMethods.CL3IF_GetMeasurementData(DEVICEID, pin.Pointer);
                    commandhandler("CL3IF_GetMeasurementData", sRtn);

                    measurementData.addInfo = (CL3IF_ADD_INFO)Marshal.PtrToStructure(pin.Pointer, typeof(CL3IF_ADD_INFO));
                    int readPosition = Marshal.SizeOf(typeof(CL3IF_ADD_INFO));
                    measurementData.outMeasurementData[DEVICEID] = (CL3IF_OUTMEASUREMENT_DATA)Marshal.PtrToStructure(pin.Pointer +
                        readPosition,
                        typeof(CL3IF_OUTMEASUREMENT_DATA));
                    data = ((double)measurementData.outMeasurementData[DEVICEID].measurementValue) / 10000;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return data;
        }

        /// <summary>
        /// get a series data of measure data index
        /// </summary>
        /// <returns></returns>
        public uint GetTrendIndex()
        {
            uint index = 0;
            if (!isConnect || uniqueInstance == null)
            {
                return index;
            }
            int sRtn;
            try
            {
                sRtn = NativeMethods.CL3IF_GetTrendIndex(DEVICEID, out index);
                commandhandler("CL3IF_GetTrendIndex", sRtn);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return index;
        }

        /// <summary>
        /// get a series data of measure data
        /// </summary>
        /// <param name="start">start index</param>
        /// <param name="end">end index</param>
        /// <returns></returns>
        public double[] GetTrendData(uint start, uint end, out int count, int defaultCount=20)
        {
            count = 0;

            if (!isConnect || uniqueInstance == null || end - start <= 0)
            {
                return null;
            }
            // string[] inputstr = new string[6000];
            double[] data = new double[defaultCount];
            int sRtn;
            uint nextInd = 0;
            int idx = 0;
            uint obtainedDataCount = 0;
            CL3IF_OUTNO outTarget = 0;
            byte[] buffer = new byte[MaxRequestDataLength];
            uint MAX_MEASURE_COUNT = MaxRequestDataLength / (16 * 8) - 1;
            uint last_epoch_count = (end - start) % MAX_MEASURE_COUNT;
            uint epochs = (end - start) / MAX_MEASURE_COUNT + 1;

            CL3IF_MEASUREMENT_DATA lastOne = new CL3IF_MEASUREMENT_DATA(); ;
            bool flag = false;
            count = 0;
            // int last_i = 700;
            string saveTime = TimeFormatStr.GetTimeStamp();

            for (uint epoch = 0; epoch < epochs; epoch++)
            {
                uint startInx = epoch * MAX_MEASURE_COUNT + start;
                uint length = (epoch < epochs - 1) ? MAX_MEASURE_COUNT : last_epoch_count;

                using (PinnedObject pin = new PinnedObject(buffer))
                {
                    sRtn = NativeMethods.CL3IF_GetTrendData(DEVICEID, startInx, length, out nextInd, out obtainedDataCount, out outTarget, pin.Pointer);
                    List<int> outTargetList = ConvertOutTargetList(outTarget);
                    int readPosition = 0;
                    for (int i = 0; i < obtainedDataCount; i++)
                    {
                        CL3IF_MEASUREMENT_DATA measureData = new CL3IF_MEASUREMENT_DATA();
                        measureData.outMeasurementData = new CL3IF_OUTMEASUREMENT_DATA[outTargetList.Count];
                        measureData.addInfo = (CL3IF_ADD_INFO)Marshal.PtrToStructure(pin.Pointer + readPosition, typeof(CL3IF_ADD_INFO));
                        readPosition += Marshal.SizeOf(typeof(CL3IF_ADD_INFO));
                        for (int j = 0; j < outTargetList.Count; j++)
                        {
                            measureData.outMeasurementData[j] = (CL3IF_OUTMEASUREMENT_DATA)Marshal.PtrToStructure(pin.Pointer + readPosition, typeof(CL3IF_OUTMEASUREMENT_DATA));
                            readPosition += Marshal.SizeOf(typeof(CL3IF_OUTMEASUREMENT_DATA));
                        }
                        //data[idx] = (double)(measureData.outMeasurementData[0].measurementValue) / 10000;
                        idx++;
                        // string aaaa = String.Format("{0},{1},{2}", idx, measureData.outMeasurementData[0].measurementValue, measureData.outMeasurementData[0].judgeResult);
                        // CSVFile.SaveString("test" + saveTime + ".csv", aaaa);

                        if (!flag)
                        {
                            flag = true;
                            if (measureData.outMeasurementData[0].judgeResult == 2 || measureData.outMeasurementData[0].measurementValue == -999999)
                            {
                                count++;
                                data[count - 1] = (double)(measureData.outMeasurementData[0].measurementValue) / 10000;
                            }

                            lastOne = measureData;
                        }
                        else
                        {
                            if ((measureData.outMeasurementData[0].judgeResult == 2  || measureData.outMeasurementData[0].measurementValue == -999999) &&
                                lastOne.outMeasurementData[0].judgeResult == 0)
                            {
                                count++;
                                if (count > defaultCount)
                                {
                                    string info = "";
                                    for (int k = 0; k < data.Length; k++)
                                    {
                                        info = info + data[k].ToString();
                                    }

                                    throw new Exception("laser measure data out of range." + info);
                                }

                                data[count-1] = (double)(measureData.outMeasurementData[0].measurementValue) / 10000;
                                // last_i = i;
                            }
                            lastOne = measureData;
                        }
                    }
                }
            }
            return data;
        }

        public double[] GetAllTrendData(uint start, uint end)
        {
            if (!isConnect || uniqueInstance == null || end - start <= 0)
            {
                return null;
            }
            double[] data = new double[end - start];
            int sRtn;
            uint nextInd = 0;
            int idx = 0;
            uint obtainedDataCount = 0;
            CL3IF_OUTNO outTarget = 0;
            byte[] buffer = new byte[MaxRequestDataLength];
            uint MAX_MEASURE_COUNT = MaxRequestDataLength / (16 * 8) - 1;
            uint last_epoch_count = (end - start) % MAX_MEASURE_COUNT;
            uint epochs = (end - start) / MAX_MEASURE_COUNT + 1;
            for (uint epoch = 0; epoch < epochs; epoch++)
            {
                uint startInx = epoch * MAX_MEASURE_COUNT + start;
                uint length = (epoch < epochs - 1) ? MAX_MEASURE_COUNT : last_epoch_count;

                using (PinnedObject pin = new PinnedObject(buffer))
                {
                    sRtn = NativeMethods.CL3IF_GetTrendData(DEVICEID, startInx, length, out nextInd, out obtainedDataCount, out outTarget, pin.Pointer);
                    List<int> outTargetList = ConvertOutTargetList(outTarget);
                    int readPosition = 0;
                    for (uint i = 0; i < obtainedDataCount; i++)
                    {
                        CL3IF_MEASUREMENT_DATA measureData = new CL3IF_MEASUREMENT_DATA();
                        measureData.outMeasurementData = new CL3IF_OUTMEASUREMENT_DATA[outTargetList.Count];
                        measureData.addInfo = (CL3IF_ADD_INFO)Marshal.PtrToStructure(pin.Pointer + readPosition, typeof(CL3IF_ADD_INFO));
                        readPosition += Marshal.SizeOf(typeof(CL3IF_ADD_INFO));
                        for (int j = 0; j < outTargetList.Count; j++)
                        {
                            measureData.outMeasurementData[j] = (CL3IF_OUTMEASUREMENT_DATA)Marshal.PtrToStructure(pin.Pointer + readPosition, typeof(CL3IF_OUTMEASUREMENT_DATA));
                            readPosition += Marshal.SizeOf(typeof(CL3IF_OUTMEASUREMENT_DATA));
                        }
                        data[idx] = (double)(measureData.outMeasurementData[0].measurementValue) / 10000;
                        idx++;
                    }
                }
            }
            return data;
        }


        /// <summary>
        /// Convert Out Target List
        /// </summary>
        /// <param name="outTarget"></param>
        /// <returns></returns>
        private List<int> ConvertOutTargetList(CL3IF_OUTNO outTarget)
        {
            byte mask = 1;
            List<int> outlist = new List<int>();
            for (int i = 0; i < NativeMethods.CL3IF_MAX_OUT_COUNT; i++)
            {
                if (((ushort)outTarget & mask) != 0)
                {
                    outlist.Add(i + 1);
                }
                mask = (byte)(mask << 1);
            }
            return outlist;
        }

        /// <summary>
        /// Start Storage Data
        /// </summary>
        public void StartStorageData()
        {
            if (!isConnect || uniqueInstance == null)
            {
                return;
            }
            int sRtn;
            try
            {
                sRtn = NativeMethods.CL3IF_StartStorage(DEVICEID);
                commandhandler("CL3IF_StartStorage", sRtn);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Stop Storage Data
        /// </summary>
        public void StopStorageData()
        {
            if (!isConnect || uniqueInstance == null)
            {
                return;
            }
            int sRtn;
            try
            {
                sRtn = NativeMethods.CL3IF_StopStorage(DEVICEID);
                commandhandler("CL3IF_StopStorage", sRtn);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Clear Storage Data
        /// </summary>
        public void ClearStorageData()
        {
            if (!isConnect || uniqueInstance == null)
            {
                return;
            }
            int sRtn;
            try
            {
                sRtn = NativeMethods.CL3IF_ClearStorageData(DEVICEID);
                commandhandler("CL3IF_ClearStorageData", sRtn);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Get Storage Index
        /// </summary>
        /// <returns></returns>
        public uint GetStorageIndex()
        {
            uint index = 0;
            uint indexNew = 0;
            if (!isConnect || uniqueInstance == null)
            {
                return index;
            }
            int sRtn;
            try
            {
                sRtn = NativeMethods.CL3IF_GetStorageIndex(DEVICEID, CL3IF_SELECTED_INDEX.CL3IF_SELECTED_INDEX_OLDEST, out index);
                sRtn = NativeMethods.CL3IF_GetStorageIndex(DEVICEID, CL3IF_SELECTED_INDEX.CL3IF_SELECTED_INDEX_NEWEST, out indexNew);

                commandhandler("CL3IF_GetTrendIndex", sRtn);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return index;
        }

        /// <summary>
        /// Get Storage Data by index
        /// </summary>
        /// <param name="start">start index</param>
        /// <param name="end">end index</param>
        /// <returns></returns>
        public int[] GetStorageData(uint start, uint end)
        {
            if (!isConnect || uniqueInstance == null || end - start <= 0)
            {
                return null;
            }
            int[] data = new int[end - start];
            int sRtn;
            uint nextInd = 0;
            int idx = 0;
            uint obtainedDataCount = 0;
            CL3IF_OUTNO outTarget = 0;
            byte[] buffer = new byte[MaxRequestDataLength];
            uint MAX_MEASURE_COUNT = MaxRequestDataLength / (16 * 8) - 1;
            uint last_epoch_count = (end - start) % MAX_MEASURE_COUNT;
            uint epochs = (end - start) / MAX_MEASURE_COUNT + 1;
            for (uint epoch = 0; epoch < epochs; epoch++)
            {
                uint startInx = epoch * MAX_MEASURE_COUNT + start;
                uint length = (epoch < epochs - 1) ? MAX_MEASURE_COUNT : last_epoch_count;

                using (PinnedObject pin = new PinnedObject(buffer))
                {
                    sRtn = NativeMethods.CL3IF_GetTrendData(DEVICEID, startInx, length, out nextInd, out obtainedDataCount, out outTarget, pin.Pointer);
                    List<int> outTargetList = ConvertOutTargetList(outTarget);
                    int readPosition = 0;
                    for (uint i = 0; i < obtainedDataCount; i++)
                    {
                        CL3IF_MEASUREMENT_DATA measureData = new CL3IF_MEASUREMENT_DATA();
                        measureData.outMeasurementData = new CL3IF_OUTMEASUREMENT_DATA[outTargetList.Count];
                        measureData.addInfo = (CL3IF_ADD_INFO)Marshal.PtrToStructure(pin.Pointer + readPosition, typeof(CL3IF_ADD_INFO));
                        readPosition += Marshal.SizeOf(typeof(CL3IF_ADD_INFO));
                        for (int j = 0; j < outTargetList.Count; j++)
                        {
                            measureData.outMeasurementData[j] = (CL3IF_OUTMEASUREMENT_DATA)Marshal.PtrToStructure(pin.Pointer + readPosition, typeof(CL3IF_OUTMEASUREMENT_DATA));
                            readPosition += Marshal.SizeOf(typeof(CL3IF_OUTMEASUREMENT_DATA));
                        }
                        data[idx] = measureData.outMeasurementData[0].measurementValue;
                        idx++;
                    }
                }
            }
            return data;
        }

        /// <summary>
        /// control for measrue or not
        /// </summary>
        /// <param name="open"></param>
        public void MeasureControl(bool open)
        {
            if (!isConnect || uniqueInstance == null)
            {
                return;
            }
            int sRtn;
            try
            {
                sRtn = NativeMethods.CL3IF_MeasurementControl(DEVICEID, open);
                commandhandler("CL3IF_MeasurementControl", sRtn);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Get Trend Index data
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public int GetTrendIndexdata(uint index)
        {
            if (!isConnect || uniqueInstance == null)
            {
                return 0;
            }
            int[] data = new int[1];
            int sRtn;
            uint nextInd = 0;
            int idx = 0;
            uint obtainedDataCount = 0;
            CL3IF_OUTNO outTarget = 0;
            byte[] buffer = new byte[MaxRequestDataLength];
            uint MAX_MEASURE_COUNT = MaxRequestDataLength / (16 * 8) - 1;
            uint last_epoch_count = (1) % MAX_MEASURE_COUNT;
            uint epochs = (1) / MAX_MEASURE_COUNT + 1;
            for (uint epoch = 0; epoch < epochs; epoch++)
            {
                uint startInx = epoch * MAX_MEASURE_COUNT + index;
                uint length = (epoch < epochs - 1) ? MAX_MEASURE_COUNT : last_epoch_count;

                using (PinnedObject pin = new PinnedObject(buffer))
                {
                    sRtn = NativeMethods.CL3IF_GetTrendData(DEVICEID, startInx, length, out nextInd, out obtainedDataCount, out outTarget, pin.Pointer);
                    List<int> outTargetList = ConvertOutTargetList(outTarget);
                    int readPosition = 0;
                    for (uint i = 0; i < obtainedDataCount; i++)
                    {
                        CL3IF_MEASUREMENT_DATA measureData = new CL3IF_MEASUREMENT_DATA();
                        measureData.outMeasurementData = new CL3IF_OUTMEASUREMENT_DATA[outTargetList.Count];
                        measureData.addInfo = (CL3IF_ADD_INFO)Marshal.PtrToStructure(pin.Pointer + readPosition, typeof(CL3IF_ADD_INFO));
                        readPosition += Marshal.SizeOf(typeof(CL3IF_ADD_INFO));
                        for (int j = 0; j < outTargetList.Count; j++)
                        {
                            measureData.outMeasurementData[j] = (CL3IF_OUTMEASUREMENT_DATA)Marshal.PtrToStructure(pin.Pointer + readPosition, typeof(CL3IF_OUTMEASUREMENT_DATA));
                            readPosition += Marshal.SizeOf(typeof(CL3IF_OUTMEASUREMENT_DATA));
                        }
                        data[idx] = measureData.outMeasurementData[0].measurementValue;
                        idx++;
                    }
                }
            }
            return data[0];
        }

        public void SetSamplingCycle(int value)
        {
            try
            {
                CL3IF_SAMPLINGCYCLE tmp = CL3IF_SAMPLINGCYCLE.CL3IF_SAMPLINGCYCLE_100USEC;
                if (value == 100)
                {
                    tmp = CL3IF_SAMPLINGCYCLE.CL3IF_SAMPLINGCYCLE_100USEC;
                }
                else if (value == 200)
                {
                    tmp = CL3IF_SAMPLINGCYCLE.CL3IF_SAMPLINGCYCLE_200USEC;
                }
                else if (value == 500)
                {
                    tmp = CL3IF_SAMPLINGCYCLE.CL3IF_SAMPLINGCYCLE_500USEC;
                }
                else if (value == 1000)
                {
                    tmp = CL3IF_SAMPLINGCYCLE.CL3IF_SAMPLINGCYCLE_1000USEC;
                }
                else
                {
                    return;
                }

                int sRtn;
                byte programNo;
                sRtn = NativeMethods.CL3IF_GetProgramNo(DEVICEID, out programNo);
                sRtn = NativeMethods.CL3IF_SetSamplingCycle(DEVICEID, programNo, tmp);
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        public void SetFilterAverage(int value)
        {
            try
            {
                CL3IF_FILTERPARAM_AVERAGE tmp = CL3IF_FILTERPARAM_AVERAGE.CL3IF_FILTERPARAM_AVERAGE_4;
                if (value == 1)
                {
                    tmp = CL3IF_FILTERPARAM_AVERAGE.CL3IF_FILTERPARAM_AVERAGE_1;
                }
                else if (value == 2)
                {
                    tmp = CL3IF_FILTERPARAM_AVERAGE.CL3IF_FILTERPARAM_AVERAGE_2;
                }
                else if (value == 4)
                {
                    tmp = CL3IF_FILTERPARAM_AVERAGE.CL3IF_FILTERPARAM_AVERAGE_4;
                }
                else if (value == 8)
                {
                    tmp = CL3IF_FILTERPARAM_AVERAGE.CL3IF_FILTERPARAM_AVERAGE_8;
                }
                else if (value == 16)
                {
                    tmp = CL3IF_FILTERPARAM_AVERAGE.CL3IF_FILTERPARAM_AVERAGE_16;
                }
                else if (value == 32)
                {
                    tmp = CL3IF_FILTERPARAM_AVERAGE.CL3IF_FILTERPARAM_AVERAGE_32;
                }
                else if (value == 64)
                {
                    tmp = CL3IF_FILTERPARAM_AVERAGE.CL3IF_FILTERPARAM_AVERAGE_64;
                }
                else if (value == 256)
                {
                    tmp = CL3IF_FILTERPARAM_AVERAGE.CL3IF_FILTERPARAM_AVERAGE_256;
                }
                else if (value == 1024)
                {
                    tmp = CL3IF_FILTERPARAM_AVERAGE.CL3IF_FILTERPARAM_AVERAGE_1024;
                }
                else if (value == 4096)
                {
                    tmp = CL3IF_FILTERPARAM_AVERAGE.CL3IF_FILTERPARAM_AVERAGE_4096;
                }
                else if (value == 16384)
                {
                    tmp = CL3IF_FILTERPARAM_AVERAGE.CL3IF_FILTERPARAM_AVERAGE_16384;
                }
                else if (value == 65536)
                {
                    tmp = CL3IF_FILTERPARAM_AVERAGE.CL3IF_FILTERPARAM_AVERAGE_65536;
                }
                else if (value == 262144)
                {
                    tmp = CL3IF_FILTERPARAM_AVERAGE.CL3IF_FILTERPARAM_AVERAGE_262144;
                }
                else
                {
                    return;
                }

                int sRtn;
                byte programNo;
                sRtn = NativeMethods.CL3IF_GetProgramNo(DEVICEID, out programNo);
                sRtn = NativeMethods.CL3IF_SetFilter(DEVICEID, programNo, 0, CL3IF_FILTERMODE.CL3IF_FILTERMODE_MOVING_AVERAGE, (ushort)tmp);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
