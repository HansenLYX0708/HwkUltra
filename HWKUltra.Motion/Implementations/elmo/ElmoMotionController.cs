using ElmoMotionControl.GMAS.EASComponents.MMCLibDotNET;
using HWKUltra.Core;
using HWKUltra.Motion.Abstractions;
using HWKUltra.Motion.Implementations;
using HWKUltra.Motion.Implementations.elmo;
using System.Net;
using System.Text.RegularExpressions;
using System.Xml.Linq;

public class ElmoMotionController : IMotionController
{
    private readonly ElmoMotionControllerConfig _config;
    private Dictionary<string, AxisScale> _scales;
    private Dictionary<string, AxisMotionLimit> _limits;
    private Dictionary<string, List<string>> _groupAxisOrder;
    // "XY" → ["X","Y"]
    private bool isInit = false;
    int connectionHandle;


    Dictionary<string, IMMCSingleAxis> axes;
    Dictionary<string, IMMCGroupAxis> groups;
    Timer GetStateTimer;
    Timer ResetErrorTimer;
    MMCCamTable mCCamTable;
    MMCNetwork mmcNetwork;


    public ElmoMotionController(ElmoMotionControllerConfig config)
    {
        _config = config;
        _scales = new Dictionary<string, AxisScale>();
        _limits = new Dictionary<string, AxisMotionLimit>();
        _groupAxisOrder = new Dictionary<string, List<string>>();


        _limits = config.Axes.ToDictionary(
        a => a.Name,
        a => a.Limit ?? GetDefaultLimit()
        );

        foreach (var g in config.Groups)
        {
            _groupAxisOrder[g.Name] = g.Axes;
        }

        axes = new Dictionary<string, IMMCSingleAxis>();
        groups = new Dictionary<string, IMMCGroupAxis>();
    }

    public void Open()
    {
        // initial controller
        if (isInit)
        {
            return;
        }
        try
        {
            // controller connection
            ClearError(new object());
            IPAddress targetIP = IPAddress.Parse(_config.TargetIP);
            IPAddress localIP = IPAddress.Parse(_config.LocalIP);
            int connHndl;
            MMCConnection.ConnectRPC(targetIP, _config.TargetPort, localIP, _config.LocalPort, null,
            _config.Mask, out connHndl);
            this.connectionHandle = connHndl;
            MMCConnection.ResetSystemErrors(this.connectionHandle);
            Thread.Sleep(3000);

            mmcNetwork = new MMCNetwork(this.connectionHandle);

            // open axes and group
            axes.Clear();
            foreach (var axis in _config.Axes)
            {
                axes.Add(axis.Name, new MMCSingleAxis(axis.DriverName, this.connectionHandle));
                PowerOnAxis(axis.Name);
                _scales.Add(axis.Name, new AxisScale(axis.PulsePerUnit));
            }
            // TODO : not include checking???
            foreach (var group in _config.Groups)
            {
                groups.Add(group.Name, new MMCGroupAxis(group.DriverName, this.connectionHandle));
            }
            ResetGroup();
            var ret = false;
            Thread.Sleep(500);

            GetStateTimer = new Timer(new TimerCallback(TimeUpForState), null, 100, 100);
            GetStateTimer.Change(100, 100);
            ResetErrorTimer = new Timer(new TimerCallback(ClearError), null, 3600000, 100);
            ResetErrorTimer.Change(3600000, 100);
            isInit = true;
        }
        catch (Exception ex)
        {
            throw new MotionException("Elmo", "Open failed", ex);
        }
    }

    public void Close() { }

    public void MoveAxis(string axisName, double pos, MotionProfile profile = null)
    {
        if (!isInit)
        {
            return;
        }
        if ((axes[axisName].ReadStatus() & (uint)MC_STATE_SINGLE.ERROR_STOP) != 0)
        {
            axes[axisName].ResetAsync();
            Thread.Sleep(500);
        }
        if ((axes[axisName].ReadStatus() & (uint)MC_STATE_SINGLE.DISABLED) != 0)
        {
            PowerOnAxis(axisName);
        }
        if (axes[axisName].GetActualPosition() == pos)
        {
            return;
        }
        try
        {
            if (axes[axisName].GetOpMode() != OPM402.OPM402_CYCLIC_SYNC_POSITION_MODE)
            {
                axes[axisName].SetOpMode(OPM402.OPM402_CYCLIC_SYNC_POSITION_MODE);
                while (axes[axisName].GetOpMode() != OPM402.OPM402_CYCLIC_SYNC_POSITION_MODE)
                {
                    Thread.Sleep(50);
                }
            }
            var p = Clamp(axisName, profile);

            axes[axisName].MoveAbsolute(pos, p.Vel.Value, p.Acc.Value, p.Dec.Value, p.Jerk.Value, MC_DIRECTION_ENUM.MC_POSITIVE_DIRECTION, MC_BUFFERED_MODE_ENUM.MC_BUFFERED_MODE);
            while ((axes[axisName].ReadStatus() & (uint)MC_STATE_SINGLE.STAND_STILL) == 0)
            {
                Thread.Sleep(50);
            }
        }
        catch (MMCException mmcEx)
        {
            if (mmcEx.MMCError == MMCErrors.NC_MAX_IMM_FB_LIMIT_REACHED)
            {
                return;
            }
        }
        catch (Exception ex)
        {
            throw new MotionException(axisName, "Move failed", ex);
        }
    }

    public void MoveAxisRelative(string axisName, double distance, MotionProfile profile = null)
    {
        if (!isInit) return;
        var currentPos = GetPosition(axisName);
        MoveAxis(axisName, currentPos + distance, profile);
    }

    public void MoveAxisVelocity(string axisName, double velocity, MotionProfile profile = null)
    {
        if (!isInit) return;
        try
        {
            var p = Clamp(axisName, profile);
            axes[axisName].MoveVelocity((float)velocity, p.Acc.Value, p.Dec.Value, p.Jerk.Value,
                velocity >= 0 ? MC_DIRECTION_ENUM.MC_POSITIVE_DIRECTION : MC_DIRECTION_ENUM.MC_NEGATIVE_DIRECTION,
                MC_BUFFERED_MODE_ENUM.MC_BUFFERED_MODE);
        }
        catch (Exception ex) { throw new MotionException(axisName, "Velocity move failed", ex); }
    }

    public void HomeAxis(string axisName)
    {
        if (!isInit) return;
        try
        {
            axes[axisName].MoveAbsolute(0, 10, 100, 100, 1000,
                MC_DIRECTION_ENUM.MC_POSITIVE_DIRECTION, MC_BUFFERED_MODE_ENUM.MC_BUFFERED_MODE);
            while ((axes[axisName].ReadStatus() & (uint)MC_STATE_SINGLE.STAND_STILL) == 0)
            {
                Thread.Sleep(50);
            }
        }
        catch (Exception ex) { throw new MotionException(axisName, "Home failed", ex); }
    }

    public double GetPosition(string axisName)
    {
        if (!isInit) return 0;
        try { return axes[axisName].GetActualPosition(); }
        catch (Exception ex) { throw new MotionException(axisName, "GetPosition failed", ex); }
    }

    public void StopAxis(string axisName)
    {
        if (!isInit) return;
        try { axes[axisName].Stop(100, 1000, MC_BUFFERED_MODE_ENUM.MC_BUFFERED_MODE); }
        catch (Exception ex) { throw new MotionException(axisName, "Stop failed", ex); }
    }

    public bool IsAxisBusy(string axisName)
    {
        if (!isInit) return false;
        try { return (axes[axisName].ReadStatus() & (uint)MC_STATE_SINGLE.STAND_STILL) == 0; }
        catch { return false; }
    }

    public void Stop(int axisId) { }

    public bool IsBusy(int axisId)
    {
        return false;
    }

    public void MoveGroup(string group, AxisPosition pos, MotionProfile profile = null)
    {
        
        if (!isInit)
        {
            return;
        }
        try
        {
            var axesList = _groupAxisOrder[group];

            var profiles = axesList.Select(a => Clamp(a, profile)).ToList();

            // TODO : select mini
            var final = new MotionProfile
            {
                Vel = profiles.Min(p => p.Vel),
                Acc = profiles.Min(p => p.Acc),
                Dec = profiles.Min(p => p.Dec),
                Jerk = profiles.Min(p => p.Jerk)
            };
            groups[group].GroupEnable();
            while ((groups[group].GroupReadStatus() & (uint)MC_STATE_GROUP.NC_GROUP_STANDBY) == 0)
            {
                Thread.Sleep(50);
            }
            var arr = ToArray(group, pos);
            groups[group].MoveLinearAbsolute(final.Vel.Value, final.Acc.Value, final.Dec.Value, final.Jerk.Value, arr,
            MC_BUFFERED_MODE_ENUM.MC_BUFFERED_MODE,
            MC_COORD_SYSTEM_ENUM.MC_MCS_COORD,
            NC_TRANSITION_MODE_ENUM.MC_TM_NONE_MODE,
            new float[] { (float)0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, 0, 1);
            while ((groups[group].GroupReadStatus() & (uint)MC_STATE_GROUP.NC_GROUP_STANDBY) == 0)
            {
                Thread.Sleep(_config.SDODelay);
            }
        }
        catch (Exception ex)
        {
            throw new MotionException(group, "Group move failed", ex);
        }
        finally
        {
            groups[group].GroupDisable();
            while ((groups[group].GroupReadStatus() & (uint)MC_STATE_GROUP.NC_GROUP_DISABLED) == 0)
            {
                Thread.Sleep(50);
            }
        }
    }



    #region internal functions

    public double[] ToArray(string group, AxisPosition pos)
    {
        var axes = _groupAxisOrder[group];

        var values = new double[axes.Count];

        for (int i = 0; i < axes.Count; i++)
        {
            var axis = axes[i];

            if (!pos.Values.ContainsKey(axis))
            {
                throw new MotionException(axis, $"Missing axis {axis} in position");
            }
            values[i] = pos[axis];
        }

        return values;
    }
    private AxisMotionLimit GetDefaultLimit()
    {
        return new AxisMotionLimit
        {
            MaxVel = 50,
            MaxAcc = 2000,
            MaxDec = 2000,
            MaxJerk = 10000
        };
    }

    private int ToPulse(string axis, double value)
    {
        var scale = _scales[axis].PulsePerUnit;
        return (int)(value * scale);
    }

    private MotionProfile Clamp(string axis, MotionProfile req)
    {
        var limit = _limits[axis];

        return new MotionProfile
        {
            Vel = Math.Min(req?.Vel ?? limit.MaxVel, limit.MaxVel),
            Acc = Math.Min(req?.Acc ?? limit.MaxAcc, limit.MaxAcc),
            Dec = Math.Min(req?.Dec ?? limit.MaxDec, limit.MaxDec),
            Jerk = Math.Min(req?.Jerk ?? limit.MaxJerk, limit.MaxJerk)
        };
    }

    private void ClearError(object value)
    {
        if (!isInit)
        {
            return;
        }
        try
        {
            short error = 0;
            ushort status = 0;
            MmcCommdiagnostics aa = new MmcCommdiagnostics();
            mmcNetwork.GetCommDiagnostics(ref aa);
            mmcNetwork.ResetCommDiagnostics(ref error, ref status);
            mmcNetwork.ResetCommStatistics(ref error, ref status);
        }
        catch (Exception ex)
        {
            throw new MotionException("ClearError", "reset error failed", ex);
        }
    }

    private void ResetGroup()
    {
        try
        {
            foreach (var item in groups)
            {
                item.Value.GroupDisable();
            }
        }
        catch (Exception ex)
        {
            throw new MotionException("ResetGroup", "Reset Group failed", ex);
        }
    }

    private bool PowerOnAxis(string name)
    {
        try
        {
            if ((axes[name].ReadStatus() & (uint)MC_STATE_SINGLE.ERROR_STOP) != 0)
            {
                axes[name].ResetAsync();
                Thread.Sleep(500);
            }
            if ((axes[name].ReadStatus() & (uint)MC_STATE_SINGLE.DISABLED) != 0)
            {
                axes[name].PowerOn(MC_BUFFERED_MODE_ENUM.MC_ABORTING_MODE);
                long start = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
                long end = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
                while ((axes[name].ReadStatus() & (uint)MC_STATE_SINGLE.STAND_STILL) == 0 && end - start < 10)
                {
                    end = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
                    Thread.Sleep(50);
                }
                if (end - start >= 10)
                {
                    throw new MotionException(name, $"Power On axis {name} failed, axis can't switch to STAND_STILL.");
                }

                if (axes[name].GetOpMode() != OPM402.OPM402_CYCLIC_SYNC_POSITION_MODE)
                {
                    axes[name].SetOpMode(OPM402.OPM402_CYCLIC_SYNC_POSITION_MODE);
                    start = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
                    end = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
                    while (axes[name].GetOpMode() != OPM402.OPM402_CYCLIC_SYNC_POSITION_MODE && end - start < 10)
                    {
                        end = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
                        Thread.Sleep(50);
                    }
                    if (end - start >= 10)
                    {
                        throw new MotionException(name, $"Power On axis {name} failed, axis can't switch to OPM402_CYCLIC_SYNC_POSITION_MODE.");
                    }
                }
            }
            return true;
        }
        catch (Exception ex)
        {
            throw new MotionException(name, $"Power On axis {name} failed", ex);
        }
    }

    private bool PowerOffAxis(string name)
    {
        try
        {
            if ((axes[name].ReadStatus() & (uint)MC_STATE_SINGLE.DISABLED) == 0)
            {
                axes[name].PowerOff(MC_BUFFERED_MODE_ENUM.MC_BUFFERED_MODE);
            }
            return true;
        }
        catch (Exception ex)
        {
            throw new MotionException(name, $"Power Off axis {name} failed", ex);
        }
    }

    private void TimeUpForState(object value)
    {
        if (!isInit)
        {
            return;
        }
        uint state = 0;
        try
        {
            // Get position

        }
        catch (Exception ex)
        {
            //throw ex;
        }
    }

    private void RetryOCClose(string axisName)
    {
        try
        {
            int data = 0;
            byte[] bArr = BitConverter.GetBytes(data);
            axes[axisName].DownloadSDO(0x316E, 1, bArr, 4, _config.SDOTimeout);
            Thread.Sleep(_config.SDODelay);
        }
        catch
        {
            try
            {
                int data = 0;
                byte[] bArr = BitConverter.GetBytes(data);
                axes[axisName].DownloadSDO(0x316E, 1, bArr, 4, _config.SDOTimeout);
                Thread.Sleep(_config.SDODelay);
            }
            catch
            {
                try
                {
                    int data = 0;
                    byte[] bArr = BitConverter.GetBytes(data);
                    axes[axisName].DownloadSDO(0x316E, 1, bArr, 4, _config.SDOTimeout);
                    Thread.Sleep(_config.SDODelay);
                }
                catch (Exception ex2)
                {
                    throw new MotionException(axisName, $"Power On axis {axisName} failed", ex2);
                }
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="indexNum"></param>
    /// <param name="axisName"></param>
    /// <param name="ioindex"> 0 : first IO output; 1 : second IO output</param>
    /// <exception cref="MotionException"></exception>
    private void InitOutputCompare(uint indexNum, string axisName, int ioindex)
    {
        if (indexNum > 255 || indexNum < 1)
        {
            throw new MotionException(axisName, $"compare data counts out of range, range should be from 1 to 500");
        }
        if (!isInit)
        {
            return;
        }
        try
        {
            // OC function is driver function
            int data = 0;
            byte[] bArr = BitConverter.GetBytes(data);
            RetryOCClose(axisName);
            data = 0;
            bArr = BitConverter.GetBytes(data);
            axes[axisName].DownloadSDO(0x316E, 6, bArr, 4, _config.SDOTimeout);
            Thread.Sleep(_config.SDODelay);
            data = 0;
            bArr = BitConverter.GetBytes(data);
            axes[axisName].DownloadSDO(0x30AA, 1, bArr, 4, _config.SDOTimeout);
            Thread.Sleep(_config.SDODelay);
            data = 0;
            bArr = BitConverter.GetBytes(data);
            axes[axisName].DownloadSDO(0x30AA, 2, bArr, 4, _config.SDOTimeout);
            Thread.Sleep(_config.SDODelay);
            data = ioindex;
            bArr = BitConverter.GetBytes(data);
            axes[axisName].DownloadSDO(0x3177, 1, bArr, 4, _config.SDOTimeout);
            Thread.Sleep(_config.SDODelay);
            data = _config.OCTriggerDuring;
            bArr = BitConverter.GetBytes(data);
            axes[axisName].DownloadSDO(0x316E, 4, bArr, 4, _config.SDOTimeout);
            Thread.Sleep(_config.SDODelay);
            bArr = BitConverter.GetBytes(indexNum);
            axes[axisName].DownloadSDO(0x316E, 5, bArr, 4, _config.SDOTimeout);
            Thread.Sleep(_config.SDODelay);
        }
        catch (Exception ex)
        {
            throw new MotionException(axisName, $"InitOutputCompareForLaserV2 failed", ex);
        }
    }

    private void InitCAM()
    {
        if (!isInit)
        {
            return;
        }
        try
        {
            Thread.Sleep(_config.SDODelay);
            MMCConnection.UnloadTable(connectionHandle, 0xffffffff);
            uint memHandle = 0;
            mCCamTable = new MMCCamTable(connectionHandle);
            Thread.Sleep(_config.SDODelay);
            mCCamTable.UnloadTable(0xffffffff);
            Int16 errVal = mCCamTable.InitTable(
                MC_BUFFERED_MODE_ENUM.MC_BLENDING_HIGH_MODE,
                _config.CAMPointsCount,
                1,
                CURVE_TYPE_ENUM.eLinearInterp,
                1,
                false,
                true,
                true,
                out memHandle);
            Thread.Sleep(_config.SDODelay);
        }
        catch (Exception ex)
        {
            throw new MotionException("CAM", $"InitCAM failed", ex);
        }
    }
    #endregion

}
