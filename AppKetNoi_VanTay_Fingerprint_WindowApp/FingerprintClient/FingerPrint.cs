using DPUruNet;
using FingerPrintForm;
using log4net;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class FingerPrint : Form
    {
        protected static readonly ILog Log = LogManager.GetLogger(MethodInfo.GetCurrentMethod().DeclaringType);
        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);
        private ReaderProcess readerProcess;
        private string serialNumberReader;
        private void BringToFront(Process pTemp)
        {
            SetForegroundWindow(pTemp.MainWindowHandle);
        }

        public FingerPrint()
        {
            InitializeComponent();
         
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            var roothPath = Environment.CurrentDirectory;
            SystemTrayIcon.Icon = new System.Drawing.Icon($"{roothPath}\\FingerprintIcon.ico");
            SystemTrayIcon.Visible = true;
            SystemTrayIcon.BalloonTipText = "Fingerprint is running.";
            SystemTrayIcon.BalloonTipTitle = "Notification.";
            SystemTrayIcon.ShowBalloonTip(1000);
            SystemTrayIcon.ContextMenu = new ContextMenu();
            SystemTrayIcon.ContextMenu.MenuItems.Add("Show serial number", OnShowSerialNumberReader);
            SystemTrayIcon.ContextMenu.MenuItems.Add("Exit", OnExit);
            CheckAppConfig();
            HubConnection();
        }
        private void CheckAppConfig()
        {
            readerProcess = new ReaderProcess();
            serialNumberReader = readerProcess.GetSerialNumberReader() != null ? readerProcess.GetSerialNumberReader() : "";
            if (serialNumberReader == "")
            {
                MessageBox.Show("No fingerprint reader is plugged into the computer \n" + "Please check the fingerprint scanner again.", "Notification.", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        private bool CheckFingerScanner()
        {
            var fingerprintScannerId = readerProcess.GetSerialNumberReader();
            if (fingerprintScannerId == "")
            {
                ResponeResult(new ResultModel()
                {
                    Code = "2",
                    Message = "No fingerprint reader is plugged into the computer",
                    PatientCode = PatientCode,
                    StationId = StationConfigId
                });
                return false;
            }
            return true;
        }

        private void OnExit(object sender, EventArgs e)
        {
            Application.Exit();
        }
        private void OnShowSerialNumberReader(object sender, EventArgs e)
        {
            var fingerprintScanner = readerProcess.GetSerialNumberReader();
            ShowSerialNumber showSerialNumber = new ShowSerialNumber();
            if (fingerprintScanner == "")
            {
                showSerialNumber.serialNumber = "No fingerprint reader is plugged into the computer.";
            }
            else
            {
                showSerialNumber.serialNumber = fingerprintScanner.Replace("{", "").Replace("}", "").Replace("-", "");
            }
            showSerialNumber.ShowDialog();
        }
        public FingerPrint frm1 { get; set; }
        public Reader CurrentReader { get; set; }
        private static string SignalRConnection = ConfigurationManager.AppSettings["SignalRConnection"]?.Trim();
        private static string StationConfigId = ConfigurationManager.AppSettings["StationId"]?.Trim();
        private static string ClientCode = ConfigurationManager.AppSettings["ClientCode"]?.Trim();
        public List<string> xmls { get; set; }
        public byte[] imageBytes { get; set; }
        private DataTable fingerData { get; set; }
        private HubConnection _signalRConnection;
        public IHubProxy _hubProxy { get; set; }
        public string PatientCode { get; set; }
        public string StationId { get; set; }
        private const uint SW_SHOWNORMAL = 5;

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr processId);

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();

        [DllImport("user32.dll")]
        private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        [DllImport("user32.dll")]
        private static extern bool BringWindowToTop(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, uint nCmdShow);

        public  void ShowForm()
        {
            
            try
            {
                uint foreThread = GetWindowThreadProcessId(GetForegroundWindow(), IntPtr.Zero);
                uint appThread = GetCurrentThreadId();

                if (foreThread != appThread)
                {
                    AttachThreadInput(foreThread, appThread, true);
                    Thread.Sleep(2000);
                    BringWindowToTop(this.Handle);
                    ShowWindow(this.Handle, SW_SHOWNORMAL); // Use the updated constant
                    AttachThreadInput(foreThread, appThread, false);
                }
                else
                {
                    BringWindowToTop(this.Handle);
                    ShowWindow(this.Handle, SW_SHOWNORMAL);
                }

                this.Activate();
                this.Focus();
            }
            catch (Exception ex)
            {
                // Handle exceptions appropriately, log or display an error message.
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }
        public void ActionCapture(string action = null, string finger = null, List<string> fingers = null)
        {
            //Repositories repo = new Repositories();
            //var statusClient = repo.GetStatusClientByCode(ClientCode);
           
            if (StationId == StationConfigId)
            {
                var fingerScannerResult = CheckFingerScanner();
                if (fingerScannerResult)
                {
                    ReaderProcess readerProcess = new ReaderProcess();
                    readerProcess._hubProxy = _hubProxy;
                    switch (action)
                    {
                        case "0":
                            Console.WriteLine("Action: " + action);
                            //fingerData = repo.GetFingerFromDb();
                            this.ShowForm();
                            readerProcess.fingerData = fingerData;
                            readerProcess.PatientCode = PatientCode;
                            readerProcess.StationId = StationConfigId;
                            readerProcess.CaptureIdentification();

                            break;
                        case "1":
                            Console.WriteLine("Action: " + action);
                            readerProcess.fingerRequest = finger;
                            readerProcess.PatientCode = PatientCode;
                            this.ShowForm();
                            readerProcess.RegistCapture();

                            break;
                        case "2":
                            Console.WriteLine("Action: " + action);
                            readerProcess.xmls = fingers;
                            readerProcess.PatientCode = PatientCode;
                            this.ShowForm();
                            readerProcess.CaptureVerification();

                            break;
                        case "3":
                            break;
                        default:
                            Console.WriteLine("Action: " + action);
                            break;
                    }
                }
            }
        }
        public async void HubConnection()
        {
            Repositories repo = new Repositories();
            _signalRConnection = new HubConnection(SignalRConnection + "signalr");
            _signalRConnection.StateChanged += HubConnection_StateChanged;

            //Get a proxy object that will be used to interact with the specific hub on the server
            //Ther may be many hubs hosted on the server, so provide the type name for the hub
            _hubProxy = _signalRConnection.CreateHubProxy("BiometricAuthenticationHub");
            _hubProxy.On<string, string, string, string>("RequestFinger", (action, finger, stationId, patientCode) =>
            {
                StationId = stationId;
                PatientCode = patientCode;
                ActionCapture(action, finger);
            });
            _hubProxy.On<string, List<string>, string, string>("VerificationFinger", (action, finger, stationId, patientCode) =>
            {
                StationId = stationId;
                PatientCode = patientCode;
                ActionCapture(action, null, finger);
            });
            try
            {
                await _signalRConnection.Start();
            }
            catch (Exception ex)
            {
                //repo.SaveLogOnServer(DeviceType.FingerPrint, DateTime.Now, "Server Error", "Exception: " + ex.Message + "\n" + "Stracktrace: " + ex.StackTrace + "\n" + ex.InnerException + "\n signalUrl: " + _signalRConnection.Url, StationConfigId);
                //MessageBox.Show("Server busy.", "Notification.", MessageBoxButtons.OK, MessageBoxIcon.Error);
                //();
            }
        }
        public void HubConnection_StateChanged(StateChange obj)
        {
            //if (obj.NewState == Microsoft.AspNet.SignalR.Client.ConnectionState.Connected)
            //Console.WriteLine("Connected");
            //Notify("Notification.", "Connected");

            if (obj.NewState == Microsoft.AspNet.SignalR.Client.ConnectionState.Disconnected)
            {
                //Notify("Notification.", "Disconnected");
                _signalRConnection = null;
                _hubProxy = null;
                HubConnection();
            }
            //Console.WriteLine("Disconnected");

        }
        public void ResponeResult(ResultModel result)
        {
            try
            {
                _hubProxy.Invoke("GetResultFinger", result);
                Console.WriteLine("ResponeResult ss");
            }
            catch (Exception ex)
            {
                LogMess("Exception", ex);
            }
        }
        public void LogMess(string mess, Exception exception = null)
        {
            Repositories repo = new Repositories();
            try
            {
                repo.SaveLogOnServer(DeviceType.FingerPrint, DateTime.Now, exception.GetType().ToString(),"Client code: "+ ClientCode + "\n" + "Exception: " + exception.Message + "\n" + "Stracktrace: " + exception.StackTrace, StationId);
            }
            catch (Exception ex)
            {
                Log.Error(String.Format("Error calling SaveLogOnServer api"), ex);
            }
        }

        public void Notify(string title, string message)
        {
            SystemTrayIcon.Icon = new System.Drawing.Icon("./FingerprintIcon.ico");
            SystemTrayIcon.Visible = true;
            SystemTrayIcon.BalloonTipText = message;
            SystemTrayIcon.BalloonTipTitle = title;
            //icon.BalloonTipIcon = ToolTipIcon.Info;
            SystemTrayIcon.ShowBalloonTip(1000);
        }
    }
}
