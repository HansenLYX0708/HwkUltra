using HWKUltra.Communication.Abstractions;
using HWKUltra.Core;
using WDConnect.Application;
using WDConnect.Common;

namespace HWKUltra.Communication.Implementations.WDConnect
{
    /// <summary>
    /// WDConnect-based communication controller for CamStar/MES factory host.
    /// Wraps the WDConnect SDK (WDConnectBase) and exposes it through ICommunicationController.
    /// </summary>
    public class WDConnectCommunicationController : ICommunicationController
    {
        private readonly WDConnectToolController _tool;
        private readonly WDConnectCommunicationControllerConfig _config;

        public event EventHandler<CommunicationEventArgs>? MessageReceived;

        public bool IsConnected => _tool.CurrentConnectionStatus == WDConnectConnectionStatus.Connected;

        public WDConnectCommunicationController(WDConnectCommunicationControllerConfig config)
        {
            _config = config;
            _tool = new WDConnectToolController();
        }

        public void Open()
        {
            try
            {
                var toolModelPath = Path.Combine(Directory.GetCurrentDirectory(), _config.ToolModelPath);
                _tool.Initialize(toolModelPath);

                _tool.WDConnectPrimaryIn += OnPrimaryIn;
                _tool.WDConnectSecondaryIn += OnSecondaryIn;
                _tool.WDConnectHostError += OnHostError;

                if (_tool.CurrentConnectionStatus != WDConnectConnectionStatus.Connected)
                    _tool.Connect();
            }
            catch (Exception ex)
            {
                throw new CommunicationException($"Failed to open WDConnect: {ex.Message}", ex);
            }
        }

        public void Close()
        {
            try
            {
                _tool.WDConnectPrimaryIn -= OnPrimaryIn;
                _tool.WDConnectSecondaryIn -= OnSecondaryIn;
                _tool.WDConnectHostError -= OnHostError;

                if (_tool.CurrentConnectionStatus == WDConnectConnectionStatus.Connected)
                    _tool.Disconnect();
            }
            catch (Exception ex)
            {
                throw new CommunicationException($"Failed to close WDConnect: {ex.Message}", ex);
            }
        }

        public void StartScan(string trayId, string loadLock, string empId)
        {
            var trans = _tool.CreatePrimaryTransaction("StartScanRequest", true, "StartScanRequest");
            trans.Primary.Item = new SCIItem { Format = SCIFormat.List, Items = new SCIItemCollection() };

            trans.Primary.Item.Items.Add(new SCIItem { Format = SCIFormat.String, Name = "ToolID", Value = _tool.ToolId });
            trans.Primary.Item.Items.Add(new SCIItem { Format = SCIFormat.String, Name = "EmpID", Value = empId });
            trans.Primary.Item.Items.Add(new SCIItem { Format = SCIFormat.String, Name = "LoadLock", Value = loadLock });
            trans.Primary.Item.Items.Add(new SCIItem { Format = SCIFormat.String, Name = "TrayID", Value = trayId });
            trans.Primary.Item.Items.Add(new SCIItem { Format = SCIFormat.String, Name = "StartTime", Value = DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss") });

            _tool.SendMessage(trans);
        }

        public void Load(string loadLock, string empId)
        {
            var trans = _tool.CreatePrimaryTransaction("LoadRequest", true, "LoadRequest");
            trans.Primary.Item = new SCIItem { Format = SCIFormat.List, Items = new SCIItemCollection() };

            trans.Primary.Item.Items.Add(new SCIItem { Format = SCIFormat.String, Name = "ToolID", Value = _tool.ToolId });
            trans.Primary.Item.Items.Add(new SCIItem { Format = SCIFormat.String, Name = "EmpID", Value = empId });
            trans.Primary.Item.Items.Add(new SCIItem { Format = SCIFormat.String, Name = "LoadLock", Value = loadLock });
            trans.Primary.Item.Items.Add(new SCIItem { Format = SCIFormat.String, Name = "UnloadTime", Value = DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss") });

            _tool.SendMessage(trans);
        }

        public void Unload(string loadLock, string empId)
        {
            var trans = _tool.CreatePrimaryTransaction("UnloadRequest", true, "UnloadRequest");
            trans.Primary.Item = new SCIItem { Format = SCIFormat.List, Items = new SCIItemCollection() };

            trans.Primary.Item.Items.Add(new SCIItem { Format = SCIFormat.String, Name = "ToolID", Value = _tool.ToolId });
            trans.Primary.Item.Items.Add(new SCIItem { Format = SCIFormat.String, Name = "EmpID", Value = empId });
            trans.Primary.Item.Items.Add(new SCIItem { Format = SCIFormat.String, Name = "LoadLock", Value = loadLock });
            trans.Primary.Item.Items.Add(new SCIItem { Format = SCIFormat.String, Name = "UnloadTime", Value = DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss") });

            _tool.SendMessage(trans);
        }

        public void CompleteRequest(CommunicationCompleteData data)
        {
            var trayMap = new SCIItem { Format = SCIFormat.List, Items = new SCIItemCollection() };

            foreach (var defect in data.DefectSliders)
            {
                var item = new SCIItem { Format = SCIFormat.List, Items = new SCIItemCollection() };
                item.Items.Add(new SCIItem { Format = SCIFormat.String, Name = "ContainerID", Value = defect.ContainerId });
                item.Items.Add(new SCIItem { Format = SCIFormat.String, Name = "SliderSN", Value = defect.SliderSN });
                item.Items.Add(new SCIItem { Format = SCIFormat.String, Name = "PosX", Value = defect.Row });
                item.Items.Add(new SCIItem { Format = SCIFormat.String, Name = "PosY", Value = defect.Col });
                item.Items.Add(new SCIItem { Format = SCIFormat.String, Name = "DefectCode", Value = defect.DefectCode });
                trayMap.Items.Add(item);
            }

            var trans = _tool.CreatePrimaryTransaction("CompleteRequest", true, "CompleteRequest");
            trans.Primary.Item = new SCIItem { Format = SCIFormat.List, Items = new SCIItemCollection() };

            trans.Primary.Item.Items.Add(new SCIItem { Format = SCIFormat.String, Name = "ToolID", Value = _tool.ToolId });
            trans.Primary.Item.Items.Add(new SCIItem { Format = SCIFormat.String, Name = "EmpID", Value = data.EmpId });
            trans.Primary.Item.Items.Add(new SCIItem { Format = SCIFormat.String, Name = "LoadLock", Value = data.LoadLock });
            trans.Primary.Item.Items.Add(new SCIItem { Format = SCIFormat.String, Name = "TrayID", Value = data.TrayId });
            trans.Primary.Item.Items.Add(new SCIItem { Format = SCIFormat.List, Name = "DefectSliders", Value = trayMap });

            _tool.SendMessage(trans);
        }

        public void Abort(string trayId, string loadLock, string empId)
        {
            var trans = _tool.CreatePrimaryTransaction("AbortRequest", true, "AbortRequest");
            trans.Primary.Item = new SCIItem { Format = SCIFormat.List, Items = new SCIItemCollection() };

            trans.Primary.Item.Items.Add(new SCIItem { Format = SCIFormat.String, Name = "ToolID", Value = _tool.ToolId });
            trans.Primary.Item.Items.Add(new SCIItem { Format = SCIFormat.String, Name = "EmpID", Value = empId });
            trans.Primary.Item.Items.Add(new SCIItem { Format = SCIFormat.String, Name = "LoadLock", Value = loadLock });
            trans.Primary.Item.Items.Add(new SCIItem { Format = SCIFormat.String, Name = "TrayID", Value = trayId });
            trans.Primary.Item.Items.Add(new SCIItem { Format = SCIFormat.String, Name = "AbortTime", Value = DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss") });

            _tool.SendMessage(trans);
        }

        public void UserAuthentication(string userId, string password)
        {
            var trans = _tool.CreatePrimaryTransaction("LoginRequest", true, "LoginRequest");
            trans.Primary.Item = new SCIItem { Format = SCIFormat.List, Items = new SCIItemCollection() };

            trans.Primary.Item.Items.Add(new SCIItem { Format = SCIFormat.String, Name = "UserID", Value = userId });
            trans.Primary.Item.Items.Add(new SCIItem { Format = SCIFormat.String, Name = "Password", Value = password });

            _tool.SendMessage(trans);
        }

        #region Event handlers

        private void OnPrimaryIn(object sender, SECsPrimaryInEventArgs e)
        {
            MessageReceived?.Invoke(this, new CommunicationEventArgs
            {
                MessageType = CommunicationMessageType.PrimaryIn,
                Success = true,
                RawCommandId = e.Transaction.Primary.CommandID,
                Message = e.Transaction.Primary.CommandID
            });
        }

        private void OnSecondaryIn(object sender, SECsSecondaryInEventArgs e)
        {
            try
            {
                var commandId = e.Transaction.Secondary.CommandID.ToUpper().Trim();
                var items = e.Transaction.Secondary.Item.Items;

                // Quick-path responses
                if (commandId == "LOGINRESPONSE")
                {
                    var isValid = Convert.ToBoolean(items[2].Value);
                    MessageReceived?.Invoke(this, new CommunicationEventArgs
                    {
                        MessageType = CommunicationMessageType.LoginResponse,
                        Success = true,
                        RawCommandId = commandId,
                        LoginData = new LoginResponseData { IsValidUser = isValid }
                    });
                    return;
                }

                if (commandId == "UNLOADRESPONSE")
                {
                    var loadLock = Convert.ToString(items[2].Value) ?? "";
                    MessageReceived?.Invoke(this, new CommunicationEventArgs
                    {
                        MessageType = CommunicationMessageType.UnloadResponse,
                        Success = true,
                        RawCommandId = commandId,
                        UnloadData = new UnloadResponseData { LoadLock = loadLock }
                    });
                    return;
                }

                if (commandId == "LOADRESPONSE")
                {
                    MessageReceived?.Invoke(this, new CommunicationEventArgs
                    {
                        MessageType = CommunicationMessageType.LoadResponse,
                        Success = true,
                        RawCommandId = commandId
                    });
                    return;
                }

                // Standard response with ReplyMessage
                var replyItem = (SCIItem)items[9].Value;
                var dataMember = Convert.ToString(replyItem.Items[0].Value) ?? "";
                var returnFlag = Convert.ToBoolean(replyItem.Items[1].Value);
                var returnMsg = Convert.ToString(replyItem.Items[2].Value) ?? "";

                if (!returnFlag)
                {
                    MessageReceived?.Invoke(this, new CommunicationEventArgs
                    {
                        MessageType = CommunicationMessageType.Error,
                        Success = false,
                        RawCommandId = commandId,
                        Message = returnMsg
                    });
                    return;
                }

                switch (commandId)
                {
                    case "STARTSCANRESPONSE":
                        var scanData = ParseScanResponse(items);
                        MessageReceived?.Invoke(this, new CommunicationEventArgs
                        {
                            MessageType = CommunicationMessageType.ScanResponse,
                            Success = true,
                            RawCommandId = commandId,
                            ScanData = scanData
                        });
                        break;

                    case "COMPLETERESPONSE":
                        MessageReceived?.Invoke(this, new CommunicationEventArgs
                        {
                            MessageType = CommunicationMessageType.CompleteResponse,
                            Success = true,
                            RawCommandId = commandId
                        });
                        break;

                    default:
                        MessageReceived?.Invoke(this, new CommunicationEventArgs
                        {
                            MessageType = CommunicationMessageType.Unknown,
                            Success = returnFlag,
                            RawCommandId = commandId,
                            Message = returnMsg
                        });
                        break;
                }
            }
            catch
            {
                // Silently ignore parse errors (matching original behavior)
            }
        }

        private void OnHostError(object sender, SECsHostErrorEventArgs e)
        {
            MessageReceived?.Invoke(this, new CommunicationEventArgs
            {
                MessageType = CommunicationMessageType.Error,
                Success = false,
                Message = "Host error"
            });
        }

        private static ScanResponseData ParseScanResponse(SCIItemCollection items)
        {
            var data = new ScanResponseData
            {
                ToolId = Convert.ToString(items[0].Value) ?? "",
                ExecutedBy = Convert.ToString(items[1].Value) ?? "",
                LoadLock = Convert.ToString(items[2].Value) ?? "",
                TrayId = Convert.ToString(items[3].Value) ?? "",
                ProductName = Convert.ToString(items[4].Value) ?? "",
                DeviceType = Convert.ToString(items[5].Value) ?? "",
                HeadType = Convert.ToString(items[6].Value) ?? ""
            };

            // Parse tray map
            var trayMapItem = (SCIItem)items[7].Value;
            foreach (SCIItem sliderItem in trayMapItem.Items)
            {
                data.TrayMap.Add(new SliderInfo
                {
                    ContainerId = Convert.ToString(sliderItem.Items[0].Value) ?? "",
                    SliderSN = Convert.ToString(sliderItem.Items[1].Value) ?? "",
                    PosX = Convert.ToString(sliderItem.Items[2].Value) ?? "",
                    PosY = Convert.ToString(sliderItem.Items[3].Value) ?? "",
                    DefectCode = Convert.ToString(sliderItem.Items[4].Value) ?? ""
                });
            }

            if (items[8] != null)
                data.LotId = Convert.ToString(items[8].Value) ?? "";

            data.IsAbort = Convert.ToBoolean(items[10].Value);
            return data;
        }

        #endregion
    }

    /// <summary>
    /// Internal WDConnect tool controller wrapper inheriting from WDConnectBase.
    /// Provides typed access to WDConnect SDK functionality.
    /// </summary>
    internal class WDConnectToolController : WDConnectBase
    {
        internal WDConnectConnectionStatus CurrentConnectionStatus { get; set; } = WDConnectConnectionStatus.NotConnected;

        public override void Initialize(string equipmentModelPath)
        {
            base.Initialize(equipmentModelPath);
            CurrentConnectionStatus = WDConnectConnectionStatus.NotConnected;
        }

        public void Connect()
        {
            ProcessOutStream(new SCITransaction()); // trigger connection
            CurrentConnectionStatus = WDConnectConnectionStatus.Connected;
        }

        public void Disconnect()
        {
            CurrentConnectionStatus = WDConnectConnectionStatus.NotConnected;
        }

        public void SendMessage(SCITransaction transaction)
        {
            ProcessOutStream(transaction);
        }

        public string ToolId => EquipmentModel.Nameable.id;

        public SCITransaction CreatePrimaryTransaction(string name, bool needReply, string commandId)
        {
            var trans = new SCITransaction
            {
                MessageType = MessageType.Primary,
                Name = name,
                NeedReply = needReply
            };
            trans.Primary = new SCIMessage { CommandID = commandId };
            return trans;
        }
    }

    /// <summary>
    /// Connection status for WDConnect (replaces old enum from ToolController.cs).
    /// </summary>
    internal enum WDConnectConnectionStatus
    {
        NotConnected,
        Connected
    }
}
