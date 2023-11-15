using DPUruNet;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.IO;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Drawing.Imaging;
using Microsoft.AspNet.SignalR.Client;
using System.Drawing.Drawing2D;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Windows.Forms;
using FingerPrintForm;
using log4net;
using System.Reflection;

namespace WindowsFormsApp1
{
    public class ReaderProcess
    {
        public FingerPrint frm1 { get; set; }
        public Thread thread1 { get; set; }
        private ReaderCollection _readers;
        public List<string> xmls { get; set; }
        public DataTable fingerData { get; set; }
        public Reader CurrentReader { get; set; }
        private static string SignalRConnection = ConfigurationManager.AppSettings["SignalRConnection"].Trim();
        private static string StationConfigId = ConfigurationManager.AppSettings["StationId"]?.Trim();
        private static string ClientCode = ConfigurationManager.AppSettings["ClientCode"]?.Trim();
        public IHubProxy _hubProxy { get; set; }
        public byte[] imageBytes { get; set; }
        public string fingerRequest { get; set; }
        public string PatientCode { get; set; }
        public string StationId { get; set; }
        protected static readonly ILog Log = LogManager.GetLogger(MethodInfo.GetCurrentMethod().DeclaringType);

        public void RegistCapture()
        {
            GetReader();
            Console.WriteLine("Đặt một ngón tay lên đầu đọc. ");
            if (!StartCaptureAsync(new Reader.CaptureCallback(this.OnRegistCapture)))
            {
                LogMess("OnRegistCapture Fail.");
                ResponeResult(new ResultModel()
                {
                    Code = "0",
                    Message = "OnRegistCapture Fail.",
                    StationId = StationConfigId
                });
            }
            //CancelCaptureAndCloseReader(this.OnRegistCapture);
        }
        public void CaptureVerification()
        {
            GetReader();
            Console.WriteLine("Đặt một ngón tay lên đầu đọc.");//Đặt một ngón tay lên đầu đọc.
            if (!StartCaptureAsync(new Reader.CaptureCallback(this.OnVerification)))
            {
                LogMess("OnVerification Fail.");
                ResponeResult(new ResultModel()
                {
                    Code = "0",
                    Message = "OnVerification Fail.",
                    StationId = StationConfigId
                });
            }
        }
        public void CaptureIdentification()
        {
            GetReader();
            Console.WriteLine("Place a finger on the reader. ");
            if (!StartCaptureAsync(new Reader.CaptureCallback(this.OnIdentification)))
            {
                LogMess("OnIdentification Fail.");
                ResponeResult(new ResultModel()
                {
                    Code = "0",
                    Message = "OnIdentification Fail.",
                    StationId = StationConfigId
                });
            }
        }

        private void OnVerification(CaptureResult captureResult)
        {
            try
            {
                Console.WriteLine("OnVerification");
                // Check capture quality and throw an error if bad.//Kiểm tra chất lượng chụp và đưa ra lỗi nếu xấu.
                if (!CheckCaptureResult(captureResult)) return;

                bool resultCompare = false;
                var fmdData = FeatureExtraction.CreateFmdFromFid(captureResult.Data, Constants.Formats.Fmd.ANSI);
                CancelCapture(this.OnRegistCapture);
                foreach (var item in xmls)
                {
                    if (!string.IsNullOrEmpty(item))
                    {
                        var fmdFinger = Fmd.DeserializeXml(item);
                        Console.WriteLine("Data OnCaptured ==>   " + Fmd.SerializeXml(fmdData.Data));
                        CompareResult compareResult = Comparison.Compare(fmdFinger, 0, fmdData.Data, 0);
                        if (compareResult.ResultCode == Constants.ResultCode.DP_SUCCESS && compareResult.Score < 21474)
                        {
                            resultCompare = true;
                            break;
                        }
                    }
                }
                if (resultCompare)
                {
                    ResponeResult(new ResultModel()
                    {
                        Code = "1",
                        Message = "Verification Success.",
                        PatientCode = PatientCode,
                        StationId = StationConfigId
                    });
                }
                else
                {
                    ResponeResult(new ResultModel()
                    {
                        Code = "0",
                        Message = "Verification False.",
                        PatientCode = PatientCode,
                        StationId = StationConfigId
                    });
                }
            }
            catch (Exception ex)
            {
                LogMess("Exception", ex);
            }
        }
        private void OnRegistCapture(CaptureResult captureResult)
        {
            try
            {
                Console.WriteLine("OnCaptured");
                // Check capture quality and throw an error if bad.//Kiểm tra chất lượng chụp và đưa ra lỗi nếu xấu.
                if (!CheckCaptureResult(captureResult)) return;
                var fmdData = FeatureExtraction.CreateFmdFromFid(captureResult.Data, Constants.Formats.Fmd.ANSI);
                Console.WriteLine("Data OnCaptured ==>   " + Fmd.SerializeXml(fmdData.Data));
                foreach (Fid.Fiv fiv in captureResult.Data.Views)
                {
                    Bitmap image = CreateBitmap(fiv.RawImage, fiv.Width, fiv.Height);
                    System.IO.MemoryStream stream = new System.IO.MemoryStream();
                    image.Save(stream, System.Drawing.Imaging.ImageFormat.Jpeg);
                    imageBytes = stream.ToArray();
                    var imgBase64 = Convert.ToBase64String(imageBytes);

                    ResponeResult(new ResultModel()
                    {
                        Code = "1",
                        Xml = Fmd.SerializeXml(fmdData.Data),
                        Image = imgBase64,
                        Message = "Success",
                        Finger = fingerRequest,
                        PatientCode = PatientCode,
                        StationId = StationConfigId
                    });
                    CancelCapture(this.OnRegistCapture);
                }
            }
            catch (Exception ex)
            {
                LogMess("Exception", ex);
            }
        }
        private void OnIdentification(CaptureResult captureResult)
        {
            try
            {
                Console.WriteLine("OnIdentification");
                // Check capture quality and throw an error if bad.
                if (!CheckCaptureResult(captureResult)) return;
                string patientCode = "";
                bool resultCompare = false;
                var fmdData = FeatureExtraction.CreateFmdFromFid(captureResult.Data, Constants.Formats.Fmd.ANSI);
                Console.WriteLine("Data OnCaptured ==>   " + Fmd.SerializeXml(fmdData.Data));
                CancelCapture(this.OnRegistCapture);
                if (fingerData.Rows.Count > 0)
                {
                    for (int i = 0; i < fingerData.Rows.Count; i++)
                    {
                        if (!string.IsNullOrEmpty(fingerData.Rows[i][1].ToString()))
                        {
                            if (!string.IsNullOrEmpty(fingerData.Rows?[i]?[2]?.ToString()))
                            {
                                var fmdLeftFinger = Fmd.DeserializeXml(fingerData.Rows?[i]?[2]?.ToString());
                                if (CompareFinger(fmdLeftFinger, fmdData.Data))
                                {
                                    patientCode = fingerData.Rows[i][1].ToString();
                                    resultCompare = true;
                                    Console.WriteLine($"Data finger Rows {i} ==>   " + fingerData.Rows?[i]?[2]?.ToString());
                                    break;
                                }
                            }
                            if (!string.IsNullOrEmpty(fingerData.Rows?[i]?[3]?.ToString()))
                            {
                                var fmdRightFinger = Fmd.DeserializeXml(fingerData.Rows?[i]?[3]?.ToString());
                                CompareResult comparefmdRightFinger = Comparison.Compare(fmdRightFinger, 0, fmdData.Data, 0);
                                if (CompareFinger(fmdRightFinger, fmdData.Data))
                                {
                                    patientCode = fingerData.Rows[i][1].ToString();
                                    resultCompare = true;
                                    Console.WriteLine($"Data finger Rows {i} ==>   " + fingerData.Rows?[i]?[3]?.ToString());
                                    break;
                                }
                            }
                            if (!string.IsNullOrEmpty(fingerData.Rows?[i]?[4]?.ToString()))
                            {
                                var fmdOtherFinger = Fmd.DeserializeXml(fingerData.Rows?[i]?[4]?.ToString());
                                CompareResult comparefmdRightFinger = Comparison.Compare(fmdOtherFinger, 0, fmdData.Data, 0);
                                if (CompareFinger(fmdOtherFinger, fmdData.Data))
                                {
                                    patientCode = fingerData.Rows[i][1].ToString();
                                    resultCompare = true;
                                    Console.WriteLine($"Data finger Rows {i} ==>   " + fingerData.Rows?[i]?[4]?.ToString());
                                    break;
                                }
                            }
                        }
                    }
                }
                if (resultCompare)
                {
                    Console.WriteLine("resultCompare: " + resultCompare);
                    ResponeResult(new ResultModel()
                    {
                        Code = "1",
                        Message = "Identification Success.",
                        PatientCode = patientCode,
                        StationId = StationConfigId
                    });
                }
                else
                {
                    ResponeResult(new ResultModel()
                    {
                        Code = "0",
                        Message = "Identification False.",
                        PatientCode = patientCode,
                        StationId = StationConfigId
                    });
                }
            }
            catch (Exception ex)
            {
                LogMess("Exception", ex);
            }
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
            //Repositories repo = new Repositories();
            try
            {
                //repo.SaveLogOnServer(DeviceType.FingerPrint, DateTime.Now, "Error", "Client code: " + ClientCode + "\n" + "Exception: " + exception.Message + "\n" + "Stracktrace: " + exception.StackTrace, StationId);
            }
            catch (Exception ex)
            {
                Log.Error(String.Format("Error calling SaveLogOnServer api"), ex);
            }
        }

        #region Capture
        public void GetReader()
        {
            try
            {

                _readers = ReaderCollection.GetReaders();
                CurrentReader = _readers[0];
                foreach (Reader Reader in _readers)
                {
                    Console.WriteLine(Reader.Description.SerialNumber);
                    //Notify("Notification.", Reader.Description.SerialNumber);
                }
                //OpenReader();
            }
            catch (Exception ex)
            {
                LogMess("GetReader false", ex);
            }
        }
        public bool OpenReader()
        {
            try
            {
                //using (Tracer tracer = new Tracer("OpenReader"))
                //{
                Constants.ResultCode result = Constants.ResultCode.DP_DEVICE_FAILURE;

                // Open reader
                result = CurrentReader.Open(Constants.CapturePriority.DP_PRIORITY_COOPERATIVE);

                if (result != Constants.ResultCode.DP_SUCCESS)
                {
                    if (CurrentReader != null)
                    {
                        CurrentReader.CancelCapture();
                        // Dispose of reader handle and unhook reader events.
                        CurrentReader.Dispose();
                        CurrentReader = null;
                    }
                    GetReader();
                }
                //}
                return true;
            }
            catch (Exception ex)
            {
                ResponeResult(new ResultModel()
                {
                    Code = "0",
                    Message = "OpenReader error !!!" + ex.Message,
                });
                //Notify("Notification Error.", "OpenReader error !!!" + ex.Message);
                LogMess("GetReader false", ex);
                return false;
            }
        }
        public void CloseReader(Reader.CaptureCallback OnCaptured)
        {
            if (this.CurrentReader == null)
                return;

            // Dispose of reader handle and unhook reader events.
            CurrentReader.Dispose();
            CurrentReader = null;
        }
        public void CancelCapture(Reader.CaptureCallback OnCaptured)
        {
            if (this.CurrentReader == null)
                return;
            CurrentReader.CancelCapture();
            // Dispose of reader handle and unhook reader events.
            CurrentReader.Dispose();
            CurrentReader = null;
        }
        private bool CaptureFingerAsync()
        {
            //using (Tracer tracer = new Tracer("CaptureFingerAsync"))
            //{
            try
            {
                Console.WriteLine("CaptureFingerAsync");
                GetStatus();

                Constants.ResultCode captureResult = CurrentReader.CaptureAsync(Constants.Formats.Fid.ANSI, Constants.CaptureProcessing.DP_IMG_PROC_DEFAULT, CurrentReader.Capabilities.Resolutions[0]);
                if (captureResult != Constants.ResultCode.DP_SUCCESS)
                {
                    throw new Exception("" + captureResult);
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error:  " + ex.Message);
                return false;
            }
            //}
        }
        public void GetStatus()
        {
            //using (Tracer tracer = new Tracer("GetStatus"))
            //{
            Console.WriteLine("GetStatus");
            Constants.ResultCode result = CurrentReader.GetStatus();

            if ((result != Constants.ResultCode.DP_SUCCESS))
            {
                throw new Exception("" + result);
            }

            if ((CurrentReader.Status.Status == Constants.ReaderStatuses.DP_STATUS_BUSY))
            {
                Thread.Sleep(50);
            }
            else if ((CurrentReader.Status.Status == Constants.ReaderStatuses.DP_STATUS_NEED_CALIBRATION))
            {
                CurrentReader.Calibrate();
            }
            else if ((CurrentReader.Status.Status != Constants.ReaderStatuses.DP_STATUS_READY))
            {
                throw new Exception("Reader Status - " + CurrentReader.Status.Status);
            }
            //}
        }
        public Bitmap CreateBitmap(byte[] bytes, int width, int height)
        {
            byte[] source = new byte[bytes.Length * 3];
            for (int index = 0; index <= bytes.Length - 1; ++index)
            {
                source[index * 3] = bytes[index];
                source[index * 3 + 1] = bytes[index];
                source[index * 3 + 2] = bytes[index];
            }
            Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format16bppRgb555);
            BitmapData bitmapdata = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
            for (int index = 0; index <= bitmap.Height - 1; ++index)
            {
                IntPtr destination = new IntPtr(bitmapdata.Scan0.ToInt64() + (long)(bitmapdata.Stride * index));
                Marshal.Copy(source, index * bitmap.Width * 3, destination, bitmap.Width * 3);
            }
            bitmap.UnlockBits(bitmapdata);
            return bitmap;
        }
        public bool CheckCaptureResult(CaptureResult captureResult)
        {
            //using (Tracer tracer = new Tracer("CheckCaptureResult"))
            //{
            Console.WriteLine("CheckCaptureResult.");
            if (captureResult.Data == null || captureResult.ResultCode != Constants.ResultCode.DP_SUCCESS)
            {
                if (captureResult.ResultCode != Constants.ResultCode.DP_SUCCESS)
                {
                    throw new Exception(captureResult.ResultCode.ToString());
                }

                // Send message if quality shows fake finger
                if ((captureResult.Quality != Constants.CaptureQuality.DP_QUALITY_CANCELED))
                {
                    throw new Exception("Quality - " + captureResult.Quality);
                }
                return false;
            }

            return true;
            //}
        }
        public bool StartCaptureAsync(DPUruNet.Reader.CaptureCallback OnCaptured)
        {
            Console.WriteLine("StartCaptureAsync.");
            //using (new Tracer("StartCaptureAsync"))
            //{
            this.CurrentReader.On_Captured += new DPUruNet.Reader.CaptureCallback(OnCaptured.Invoke);
            return this.CaptureFingerAsync();
            //}
        }
        public bool CompareFinger(Fmd firstFinger, Fmd secondFinger)
        {
            CompareResult comparefmd = Comparison.Compare(firstFinger, 0, secondFinger, 0);
            if (comparefmd.ResultCode == Constants.ResultCode.DP_SUCCESS && comparefmd.Score < 21474)
            {
                return true;
            }
            return false;
        }
        public string GetSerialNumberReader()
        {
            try
            {
                var reader = ReaderCollection.GetReaders();
                var serialNumber = "";
                if (reader.Count > 0)
                {
                    serialNumber = reader[0].Description.SerialNumber.Replace("{","").Replace("}","").Replace("-","");
                }
                return serialNumber;
            }
            catch (Exception)
            {
                return "";
            }
        }
        #endregion
    }
}
