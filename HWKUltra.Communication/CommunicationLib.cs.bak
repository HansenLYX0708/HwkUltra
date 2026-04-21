using System;
using System.Collections.Generic;
using System.Configuration;
using System.Windows;
using WD.AVI.Common;
using WDConnect.Application;
using WDConnect.Common;


namespace WD.AVI.CamStarComnunicationLib
{
    public class CommunicationLib
    {
        private static CommunicationLib uniqueInstance;
        private static readonly object locker = new object();

        private ToolController tool;

        public Action<string> SECsPrimaryIn { get; set; }
        public Action<string> SECsHostError { get; set; }

        public Action<TrayInfo> SECsScanTray { get; set; }
        public Action SECsCompleted { get; set; }
        public Action SECsloadGroup { get; set; }
        public Action<string> SECsUnloadGroup { get; set; }
        public Action<bool> SECsLogin { get; set; }
        public Action<string> SECsErrorMsg { get; set; }

        private CommunicationLib()
        {
            try
            {
                tool = new ToolController();
                tool.WDConnectPrimaryIn -= toolController_SECsPrimaryIn;
                tool.WDConnectPrimaryIn += toolController_SECsPrimaryIn;
                tool.WDConnectSecondaryIn -= toolController_SECsSecondaryIn;
                tool.WDConnectSecondaryIn += toolController_SECsSecondaryIn;
                tool.WDConnectHostError -= toolController_SECsHostError;
                tool.WDConnectHostError += toolController_SECsHostError;
                string ToolModelPath = System.IO.Directory.GetCurrentDirectory() + LocalARD.ToolModelPath;
                tool.Initialize(ToolModelPath);
            }
            catch (Exception e)
            {
                throw e;
            }
        }


        public static CommunicationLib GetInstance()
        {
            try
            {
                if (uniqueInstance == null)
                {
                    lock (locker)
                    {
                        if (uniqueInstance == null)
                        {
                            uniqueInstance = new CommunicationLib();
                        }
                    }
                }
                return uniqueInstance;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private void toolController_SECsHostError(object sender, SECsHostErrorEventArgs e)
        {
        }

        private void toolController_SECsSecondaryIn(object sender, SECsSecondaryInEventArgs e)
        {
            try
            {
                if (e.Transaction.Secondary.CommandID.ToUpper().Trim() == "LOGINRESPONSE")
                {
                    bool isValidUser = Convert.ToBoolean(e.Transaction.Secondary.Item.Items[2].Value);
                    SECsLogin?.Invoke(isValidUser);
                    return;
                }
                else if (e.Transaction.Secondary.CommandID.ToUpper().Trim() == "UNLOADRESPONSE")
                {
                    string loadLock = Convert.ToString(e.Transaction.Secondary.Item.Items[2].Value);
                    // bool returnFlag = Convert.ToBoolean(e.Transaction.Secondary.Item.Items[5].Value);
                    SECsUnloadGroup?.Invoke(loadLock);
                    return;
                }
                else if (e.Transaction.Secondary.CommandID.ToUpper().Trim() == "LOADRESPONSE")
                {
                    SECsloadGroup?.Invoke();
                    return;
                }

                ReplyMessage replyMessage = new ReplyMessage(
                           Convert.ToString(((SCIItem)e.Transaction.Secondary.Item.Items[9].Value).Items[0].Value),
                           Convert.ToBoolean(((SCIItem)e.Transaction.Secondary.Item.Items[9].Value).Items[1].Value),
                           Convert.ToString(((SCIItem)e.Transaction.Secondary.Item.Items[9].Value).Items[2].Value)
                       );
                if (replyMessage.ReturnFlag)
                {
                    switch (e.Transaction.Secondary.CommandID.ToUpper().Trim())
                    {
                        case "STARTSCANRESPONSE":
                            SCIItem TraytMap = (SCIItem)e.Transaction.Secondary.Item.Items[7].Value;
                            List<SliderInfo> trayMap = new List<SliderInfo>();
                            foreach (SCIItem sCIItem in TraytMap.Items)
                            {
                                SliderInfo sliderInfo = new SliderInfo();
                                sliderInfo.ContainerID = Convert.ToString(sCIItem.Items[0].Value);
                                sliderInfo.SliderSN = Convert.ToString(sCIItem.Items[1].Value);
                                sliderInfo.PosX = Convert.ToString(sCIItem.Items[2].Value);
                                sliderInfo.PosY = Convert.ToString(sCIItem.Items[3].Value);
                                sliderInfo.DefectCode = Convert.ToString(sCIItem.Items[4].Value);
                                trayMap.Add(sliderInfo);
                            }

                            TrayInfo trayInfo = new TrayInfo();
                            trayInfo.TrayMap = trayMap;
                            trayInfo.HostReply = replyMessage;
                            trayInfo.ToolID = Convert.ToString(e.Transaction.Secondary.Item.Items[0].Value);
                            trayInfo.ExecutedBy = Convert.ToString(e.Transaction.Secondary.Item.Items[1].Value);
                            trayInfo.LoadLock = Convert.ToString(e.Transaction.Secondary.Item.Items[2].Value);
                            trayInfo.TrayID = Convert.ToString(e.Transaction.Secondary.Item.Items[3].Value);
                            trayInfo.ProductName = Convert.ToString(e.Transaction.Secondary.Item.Items[4].Value);
                            trayInfo.DeviceType = Convert.ToString(e.Transaction.Secondary.Item.Items[5].Value);
                            trayInfo.HeadType = Convert.ToString(e.Transaction.Secondary.Item.Items[6].Value);
                            if (e.Transaction.Secondary.Item.Items[8] != null)
                            {
                                trayInfo.LotID = Convert.ToString(e.Transaction.Secondary.Item.Items[8].Value);
                            }
                            trayInfo.IsAbort = Convert.ToBoolean(e.Transaction.Secondary.Item.Items[10].Value);
                            SECsScanTray?.Invoke(trayInfo);
                            break;
                        case "COMPLETERESPONSE":

                            SECsCompleted?.Invoke();
                            break;
                        case "LOADRESPONSE":
                            SECsloadGroup?.Invoke();
                            break;
                        case "LOGINRESPONSE":
                            bool isValidUser = Convert.ToBoolean(e.Transaction.Secondary.Item.Items[2].Value);
                            SECsLogin?.Invoke(isValidUser);
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    SECsErrorMsg?.Invoke(replyMessage.ReturnMsg);
                }
            }
            catch
            {
            }
        }

        private void toolController_SECsPrimaryIn(object sender, SECsPrimaryInEventArgs e)
        {
            if (SECsPrimaryIn != null)
            {
                //e.Transaction.Secondary.Item.Items[0]
                SECsPrimaryIn(e.Transaction.Primary.CommandID);
            }
        }


        public void Connect()
        {
            if (tool.ConnectionStatus == ConnectionStatus.NotConnected)
                tool.Connect();
        }

        public void Disconnect()
        {
            if (tool.ConnectionStatus == ConnectionStatus.Connected)
                tool.Disconnect();
        }

        public ConnectionStatus ConnectState
        {
            get => tool.ConnectionStatus;
            set => tool.ConnectionStatus = value;
        }

        public void StartScan(string trayID, string loadLock, string empID)
        {
            SCITransaction trans1 = tool.CreatePrimaryTransaction("StartScanRequest", true, "StartScanRequest");
            trans1.Primary.Item = new SCIItem();
            trans1.Primary.Item.Format = SCIFormat.List;
            trans1.Primary.Item.Items = new SCIItemCollection();

            trans1.Primary.Item.Items.Add(new SCIItem { Format = SCIFormat.String, Name = "ToolID", Value = tool.ToolId });
            trans1.Primary.Item.Items.Add(new SCIItem { Format = SCIFormat.String, Name = "EmpID", Value = empID });
            trans1.Primary.Item.Items.Add(new SCIItem { Format = SCIFormat.String, Name = "LoadLock", Value = loadLock });  // L: LEFT, R: RIGHT
            trans1.Primary.Item.Items.Add(new SCIItem { Format = SCIFormat.String, Name = "TrayID", Value = trayID });
            trans1.Primary.Item.Items.Add(new SCIItem { Format = SCIFormat.String, Name = "StartTime", Value = DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss") });
            tool.SendMessage(trans1);
        }

        public void Load(string loadLock, string empID)
        {
            SCIItem TrayMap = new SCIItem();
            TrayMap.Format = SCIFormat.List;
            TrayMap.Items = new SCIItemCollection();

            SCITransaction trans1 = tool.CreatePrimaryTransaction("LoadRequest", true, "LoadRequest");
            trans1.Primary.Item = new SCIItem();
            trans1.Primary.Item.Format = SCIFormat.List;
            trans1.Primary.Item.Items = new SCIItemCollection();

            SCIItem TrayList = new SCIItem();
            TrayList.Format = SCIFormat.List;
            TrayList.Items = new SCIItemCollection();

            trans1.Primary.Item.Items.Add(new SCIItem { Format = SCIFormat.String, Name = "ToolID", Value = tool.ToolId });
            trans1.Primary.Item.Items.Add(new SCIItem { Format = SCIFormat.String, Name = "EmpID", Value = empID });
            trans1.Primary.Item.Items.Add(new SCIItem { Format = SCIFormat.String, Name = "LoadLock", Value = loadLock });  // L: LEFT, R: RIGHT
            trans1.Primary.Item.Items.Add(new SCIItem { Format = SCIFormat.String, Name = "UnloadTime", Value = DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss") });
            tool.SendMessage(trans1);
        }


        public void Unload(string loadLock, string empID)
        {
            SCIItem TrayMap = new SCIItem();
            TrayMap.Format = SCIFormat.List;
            TrayMap.Items = new SCIItemCollection();

            SCITransaction trans1 = tool.CreatePrimaryTransaction("UnloadRequest", true, "UnloadRequest");
            trans1.Primary.Item = new SCIItem();
            trans1.Primary.Item.Format = SCIFormat.List;
            trans1.Primary.Item.Items = new SCIItemCollection();

            SCIItem TrayList = new SCIItem();
            TrayList.Format = SCIFormat.List;
            TrayList.Items = new SCIItemCollection();

            trans1.Primary.Item.Items.Add(new SCIItem { Format = SCIFormat.String, Name = "ToolID", Value = tool.ToolId });
            trans1.Primary.Item.Items.Add(new SCIItem { Format = SCIFormat.String, Name = "EmpID", Value = empID });
            trans1.Primary.Item.Items.Add(new SCIItem { Format = SCIFormat.String, Name = "LoadLock", Value = loadLock });  // L: LEFT, R: RIGHT
            trans1.Primary.Item.Items.Add(new SCIItem { Format = SCIFormat.String, Name = "UnloadTime", Value = DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss") });
            tool.SendMessage(trans1);
        }

        public void CompleteRequest(TrayDetectionResult trayInfo)
        {
            try
            {
                SCIItem TrayMap = new SCIItem();
                TrayMap.Format = SCIFormat.List;
                TrayMap.Items = new SCIItemCollection();

                // TODO : debug
                // generate traymap for 39389459
                trayInfo.SerialNum = "39389459";
                for (int i = 0; i < trayInfo.Rows - 3; i++)
                {
                    for (int j = 0; j < trayInfo.Columns; j++)
                    {

                        SCIItem TrayItem = new SCIItem();
                        TrayItem.Format = SCIFormat.List;

                        TrayItem.Items = new SCIItemCollection();
                        TrayItem.Items.Add(new SCIItem { Format = SCIFormat.String, Name = "ContainerID", Value = trayInfo.ContainerIDs[i, j] });
                        TrayItem.Items.Add(new SCIItem { Format = SCIFormat.String, Name = "SliderSN", Value = trayInfo.SlidersSN[i, j] });
                        TrayItem.Items.Add(new SCIItem { Format = SCIFormat.String, Name = "PosX", Value = i + 1 });
                        TrayItem.Items.Add(new SCIItem { Format = SCIFormat.String, Name = "PosY", Value = j + 1 });

                        if (i == 1 && j == 18)
                        {
                            TrayItem.Items.Add(new SCIItem { Format = SCIFormat.String, Name = "DefectCode", Value = EnumSliderStatus.A2.ToString() });
                            TrayMap.Items.Add(TrayItem);
                        }
                        if (i == 2 && j == 3)
                        {
                            TrayItem.Items.Add(new SCIItem { Format = SCIFormat.String, Name = "DefectCode", Value = EnumSliderStatus.OFF.ToString() });
                            TrayMap.Items.Add(TrayItem);
                        }

                        if (i == 2 && j == 15)
                        {
                            TrayItem.Items.Add(new SCIItem { Format = SCIFormat.String, Name = "DefectCode", Value = EnumSliderStatus.A3.ToString() });
                            TrayMap.Items.Add(TrayItem);
                        }

                        // TODO : debug
                        //TrayMap.Items.Add(TrayItem);
                    }
                }

                SCITransaction trans1 = tool.CreatePrimaryTransaction("CompleteRequest", true, "CompleteRequest");
                trans1.Primary.Item = new SCIItem();
                trans1.Primary.Item.Format = SCIFormat.List;
                trans1.Primary.Item.Items = new SCIItemCollection();
                trans1.Primary.Item.Items.Add(new SCIItem { Format = SCIFormat.String, Name = "ToolID", Value = tool.ToolId });
                trans1.Primary.Item.Items.Add(new SCIItem { Format = SCIFormat.String, Name = "EmpID", Value = "00000000" });
                trans1.Primary.Item.Items.Add(new SCIItem { Format = SCIFormat.String, Name = "LoadLock", Value = trayInfo.LoadLock });  // L: LEFT, R: RIGHT
                trans1.Primary.Item.Items.Add(new SCIItem { Format = SCIFormat.String, Name = "TrayID", Value = trayInfo.SerialNum });
                trans1.Primary.Item.Items.Add(new SCIItem { Format = SCIFormat.List, Name = "DefectSliders", Value = TrayMap });

                tool.SendMessage(trans1);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public void UserAuthentication(string userID, string password)
        {
            SCIItem TrayMap = new SCIItem();
            TrayMap.Format = SCIFormat.List;
            TrayMap.Items = new SCIItemCollection();

            SCITransaction trans1 = tool.CreatePrimaryTransaction("LoginRequest", true, "LoginRequest");
            trans1.Primary.Item = new SCIItem();
            trans1.Primary.Item.Format = SCIFormat.List;
            trans1.Primary.Item.Items = new SCIItemCollection();

            SCIItem TrayList = new SCIItem();
            TrayList.Format = SCIFormat.List;
            TrayList.Items = new SCIItemCollection();

            trans1.Primary.Item.Items.Add(new SCIItem { Format = SCIFormat.String, Name = "UserID", Value = userID });
            trans1.Primary.Item.Items.Add(new SCIItem { Format = SCIFormat.String, Name = "Password", Value = password });

            string xml = trans1.XMLText;
            CSVFile.SaveString("UserAuthentication.txt", xml);
            tool.SendMessage(trans1);
        }

        public void Abort(string trayID, string loadLock, string empID)
        {
            SCITransaction trans1 = tool.CreatePrimaryTransaction("AbortRequest", true, "AbortRequest");
            trans1.Primary.Item = new SCIItem();
            trans1.Primary.Item.Format = SCIFormat.List;
            trans1.Primary.Item.Items = new SCIItemCollection();

            trans1.Primary.Item.Items.Add(new SCIItem { Format = SCIFormat.String, Name = "ToolID", Value = tool.ToolId });
            trans1.Primary.Item.Items.Add(new SCIItem { Format = SCIFormat.String, Name = "EmpID", Value = empID });
            trans1.Primary.Item.Items.Add(new SCIItem { Format = SCIFormat.String, Name = "LoadLock", Value = loadLock });  // L: LEFT, R: RIGHT
            trans1.Primary.Item.Items.Add(new SCIItem { Format = SCIFormat.String, Name = "TrayID", Value = trayID });
            trans1.Primary.Item.Items.Add(new SCIItem { Format = SCIFormat.String, Name = "AbortTime", Value = DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss") });
            tool.SendMessage(trans1);
        }
    }
}
