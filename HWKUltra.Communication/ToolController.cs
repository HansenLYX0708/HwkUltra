using System.Collections.Generic;
using System.Configuration;
using WDConnect.Common;

namespace WD.AVI.CamStarComnunicationLib
{
    public class ToolController : WDConnect.Application.WDConnectBase
    {

        public override void Initialize(string equipmentModelPath)
        {
            base.Initialize(equipmentModelPath);
            this.ConnectionStatus = ConnectionStatus.NotConnected;
        }

        public void SendMessage(WDConnect.Common.SCITransaction transaction)
        {
            base.ProcessOutStream(transaction);
        }

        public void ReplyOutStream(WDConnect.Common.SCITransaction transaction)
        {
            base.ReplyOutSteam(transaction);
        }

        public string ToolId
        {
            get
            {
                return EquipmentModel.Nameable.id;
            }
        }

        public string connectionMode
        {
            get
            {
                return EquipmentModel.GemConnection.HSMS.connectionMode.ToString();
            }
        }

        public string remoteIPAddress
        {
            get
            {
                return EquipmentModel.GemConnection.HSMS.remoteIPAddress;
            }
        }

        public string remotePortNumber
        {
            get
            {
                return EquipmentModel.GemConnection.HSMS.remotePortNumber.ToString();
            }
        }

        public string localIPAddress
        {
            get
            {
                return EquipmentModel.GemConnection.HSMS.localIPAddress;
            }
        }

        public string localPortNumber
        {
            get
            {
                return EquipmentModel.GemConnection.HSMS.localPortNumber.ToString();
            }
        }


        public int T3Timeout
        {
            get
            {
                return EquipmentModel.GemConnection.HSMS.T3Timeout;
            }
        }

        public int T5Timeout
        {
            get
            {
                return EquipmentModel.GemConnection.HSMS.T5Timeout;
            }
        }


        private ConnectionStatus _connectionStatus;
        public ConnectionStatus ConnectionStatus
        {
            get
            {
                return _connectionStatus;
            }
            set
            {
                _connectionStatus = value;
            }
        }

        public string[] hostConfiguration
        {
            get
            {
                string[] config = {  EquipmentModel.Nameable.id,
                                      ConnectionStatus.NotConnected.ToString(),
                                      EquipmentModel.GemConnection.HSMS.connectionMode.ToString(),
                                      EquipmentModel.GemConnection.HSMS.localIPAddress,
                                      EquipmentModel.GemConnection.HSMS.localPortNumber.ToString(),
                                      string.Empty ,
                                      string.Empty,
                                      string.Empty ,
                                      EquipmentModel.GemConnection.HSMS.T3Timeout.ToString(),
                                      EquipmentModel.GemConnection.HSMS.T5Timeout.ToString()
                                   };
                return config;
            }
        }

        public SCITransaction CreatePrimaryTransaction(string Name, bool NeedReply, string CommandID)
        {
            SCITransaction trans = new SCITransaction();
            trans.MessageType = MessageType.Primary;

            trans.Name = Name;
            trans.NeedReply = NeedReply;

            SCIMessage mes = new SCIMessage();
            mes.CommandID = CommandID;
            trans.Primary = mes;

            return trans;
        }
    }

    public enum ConnectionStatus
    {
        NotConnected,
        Connected
    }

    public static class LocalARD
    {
        public static string ToolModelPath = ConfigurationManager.AppSettings["ToolModelPath"];
    }


    #region Custom Data Model

    public class TrayInfo
    {
        public string LotID { get; set; }
        public string TrayID { get; set; }
        public string LoadLock { get; set; }
        public string ExecutedBy { get; set; }
        public string ToolID { get; set; }
        public string ScanTime { get; set; }
        public string ProductName { get; set; }
        public string DeviceType { get; set; }
        public string HeadType { get; set; }


        public bool IsAbort { get; set; }
        public List<SliderInfo> TrayMap { get; set; }
        public ReplyMessage HostReply { get; set; }
    }

    public class SliderInfo
    {
        public string ContainerID { get; set; }
        public string SliderSN { get; set; }
        public string PosX { get; set; }
        public string PosY { get; set; }
        public string DefectCode { get; set; }
    }

    public class ReplyMessage
    {
        public string DataMember { get; set; }
        public bool ReturnFlag { get; set; }
        public string ReturnMsg { get; set; }

        public ReplyMessage(string dataMember, bool returnFlag, string returnMsg)
        {
            this.DataMember = dataMember;
            this.ReturnFlag = returnFlag;
            this.ReturnMsg = returnMsg;
        }
    }

    #endregion
}
