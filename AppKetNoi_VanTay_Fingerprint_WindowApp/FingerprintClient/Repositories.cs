using FingerPrintForm;
using log4net;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Reflection;
using System.Text;

namespace WindowsFormsApp1
{
    public class Repositories
    {
        private SqlConnection connection;
        private string connectionString = null;
        private ReaderProcess ReaderProcess = new ReaderProcess();
        protected static readonly ILog Log = LogManager.GetLogger(MethodInfo.GetCurrentMethod().DeclaringType);
        //Phương thức lấy chuỗi kết nối
        public string ConnectionString
        {
            get
            {
                if (connectionString != null)
                    return connectionString;
                //hoặc lấy thông tin trong tệp config.xml(nếu làm với winform)
                try
                {
                    return ConfigurationManager.ConnectionStrings["connectionstring"].ConnectionString;
                }
                catch (Exception ex)
                {
                    Log.Error(String.Format("Error connectionString"), ex);
                    return string.Empty;
                    //throw ex;
                }
            }
            set { connectionString = value; }
        }
        //Phương thức lấy biến kết nối
        public SqlConnection SqlConnection
        {
            get
            {
                try
                {
                    connection = new SqlConnection();
                    connection.ConnectionString = ConnectionString;
                    connection.Open();
                    Console.WriteLine("OK");
                    return connection;
                }
                catch (Exception ex)
                {
                    Log.Error(String.Format("COnnection:" + ex.Message));
                    return null;
                }
            }
            set { connection = value; }
        }
        //Phương thức đóng kết nối
        public void CloseConnection()
        {
            if (connection.State == ConnectionState.Open) connection.Close();
        }
        public DataTable GetFingerFromDb()
        {
            SqlCommand cmm = null;
            SqlDataAdapter da = null;
            DataTable dt = null;
            try
            {
                string sql = @"SELECT TOP 1000 PatientId, PatientCode, LeftFingerImageId, RightFingerImageId, OtherFingerImageId FROM Patient ORDER BY CreatedDate DESC";
                cmm = new SqlCommand(sql, SqlConnection);
                da = new SqlDataAdapter(cmm);
                DataSet ds = new DataSet();
                da.Fill(ds);
                dt = ds.Tables[0];
            }
            catch (Exception ex)
            {
                Log.Error(String.Format("Error calling GetFingerFromDb api"), ex);
            }
            finally
            {
                da.Dispose();
                cmm.Dispose();
                CloseConnection();
            }
            return dt;
        }
        public DataTable GetStationFingerScannerIdById(string id)
        {
            SqlCommand cmm = null;
            SqlDataAdapter da = null;
            DataTable dt = null;
            try
            {
                string sql = $@"select FingerprintScannerId from Station where StationId = '{id}'";
                cmm = new SqlCommand(sql, SqlConnection);
                da = new SqlDataAdapter(cmm);
                DataSet ds = new DataSet();
                da.Fill(ds);
                dt = ds.Tables[0];
            }
            catch (Exception ex)
            {
                Log.Error(String.Format("Error calling GetStationFingerScannerIdById api"), ex);
            }
            finally
            {
                da.Dispose();
                cmm.Dispose();
                CloseConnection();
            }
            return dt;
        }
        public object SaveLogOnServer(DeviceType deviceType, DateTime logDate, string logLevel,string logMessage, string stationId)
        {
            object result = null;
            string sql = $@"INSERT INTO [dbo].[DeviceLog] (DeviceType, LogDate, LogLevel, LogMessage, StationId) VALUES ({(int)deviceType}, '{logDate}','{logLevel}','{logMessage}',{stationId})";
            SqlCommand cmm = new SqlCommand(sql, SqlConnection);
            try
            {
                //cmm = new SqlCommand(sql, SqlConnection);
                result = cmm.ExecuteScalar();
            }
            catch (Exception ex)
            {
                Log.Error(String.Format("Error calling SaveLogOnServer api"), ex);
            }
            finally
            {
                cmm.Dispose();
                CloseConnection();
            }
            return result;
        }
        public DataTable GetStatusClientByCode(string code)
        {
            SqlCommand cmm = null;
            SqlDataAdapter da = null;
            DataTable dt = null;
            try
            {
                string sql = $@"select IsActive from Client where ClientCode = '{code}'";
                cmm = new SqlCommand(sql, SqlConnection);
                da = new SqlDataAdapter(cmm);
                DataSet ds = new DataSet();
                da.Fill(ds);
                dt = ds.Tables[0];
            }
            catch (Exception ex)
            {
                Log.Error(String.Format("Error calling GetStationFingerScannerIdById api"), ex);
            }
            finally
            {
                da.Dispose();
                cmm.Dispose();
                CloseConnection();
            }
            return dt;
        }
    }
}
