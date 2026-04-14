namespace WD.AVI.LaserLib
{
    public enum DeviceStatus
    {
        NoConnection = 0,
        Usb,
        Ethernet,
    };

    public class DeviceData
    {
        private DeviceStatus _status = DeviceStatus.NoConnection;

        public DeviceStatus Status
        {
            get { return _status; }
            set
            {
                EthernetSetting = new CL3IF_ETHERNET_SETTING();
                _status = value;
            }
        }
        public CL3IF_ETHERNET_SETTING EthernetSetting { get; set; }

        public DeviceData()
        {
            EthernetSetting = new CL3IF_ETHERNET_SETTING();
        }

        public string GetStatusString()
        {
            string status = _status == DeviceStatus.NoConnection ? "No connection" : _status.ToString();
            const string SegmentBranch = ".";
            switch (_status)
            {
                case DeviceStatus.Ethernet:
                    status += string.Format("---{0}", EthernetSetting.ipAddress[0] 
                                                      + SegmentBranch + EthernetSetting.ipAddress[1] 
                                                      + SegmentBranch + EthernetSetting.ipAddress[2] 
                                                      + SegmentBranch + EthernetSetting.ipAddress[3]);
                    break;
                default:
                    break;
            }
            return status;
        }
    }
}
