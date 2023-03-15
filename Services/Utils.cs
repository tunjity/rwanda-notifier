using System.Globalization;

namespace notifier.Services
{
    public static class StringHelper
    {
        public static string GetUntilOrEmpty(this string text, string stopAt = "-")
        {
            if (!String.IsNullOrWhiteSpace(text))
            {
                int charLocation = text.IndexOf(stopAt, StringComparison.Ordinal);

                if (charLocation > 0)
                {
                    return text.Substring(0, charLocation);
                }
            }

            return String.Empty;
        }


        public static string StartUntilOrEmpty(this string text, string startAt = "-")
        {
            if (!String.IsNullOrWhiteSpace(text))
            {
                return text.Substring(text.LastIndexOf(startAt) + 1);
            }
            return String.Empty;
        }




    }

    public class Logger
    {

        private static readonly string _serverPath = ServerRootPath.RootPath();
        private static String ErrorlineNo, Errormsg, extype, exurl, hostIp, ErrorLocation, HostAdd;

        public static void SendErrorToText(Exception ex)
        {
            var line = Environment.NewLine + Environment.NewLine;

            ErrorlineNo = "";
            Errormsg = ex.GetType().Name;
            extype = ex.GetType().ToString();
            ErrorLocation = ex.Message;

            try
            {
                string filepath = Path.Combine(_serverPath, "ErrorLog/");

                if (!Directory.Exists(filepath))
                {
                    Directory.CreateDirectory(filepath);
                }
                filepath = filepath + DateTime.Today.ToString("dd-MMM-yyyy") + ".txt";
                if (!File.Exists(filepath))
                {
                    File.Create(filepath).Dispose();
                }

                using StreamWriter sw = File.AppendText(filepath);
                string error = "Log Written Date:" + " " + DateTime.Now.ToString(CultureInfo.InvariantCulture) + line +
                               "Error Line No :" + " " + ErrorlineNo + line + "Error Message:" + " " + Errormsg + line +
                               "Exception Type:" + " " + extype + line + "Error Location :" + " " + ErrorLocation +
                               line + " Error Page Url:" + " " + exurl + line + "User Host IP:" + " " + hostIp + line;
                sw.WriteLine("-----------Exception Details on " + " " + DateTime.Now.ToString(CultureInfo.InvariantCulture) + "-----------------");
                sw.WriteLine("-------------------------------------------------------------------------------------");
                sw.WriteLine(line);
                sw.WriteLine(error);
                sw.WriteLine("--------------------------------*End*------------------------------------------");
                sw.WriteLine(line);
                sw.Flush();
                sw.Close();
            }
            catch (Exception e)
            {
                e.ToString();
            }
        }


        public static bool WriteToFile(string response, string logPath = "", string fileName = "")
        {
            var line = Environment.NewLine;
            try
            {
                string filepath = Path.Combine(_serverPath, logPath);
                if (!Directory.Exists(filepath))
                {
                    Directory.CreateDirectory(filepath);
                }

                filepath = filepath + (string.IsNullOrWhiteSpace(fileName) ? DateTime.Today.ToString("dd-MMM-yyyy") : fileName) + ".txt";
                File.WriteAllText(filepath, response);
                return true;

            }
            catch (Exception)
            {
                return false;
                //throw;
            }
        }
        public static IDictionary<string, string> ReadFromFile(string logPath = "")
        {
            IDictionary<string, string> response = new Dictionary<string, string>();

            string filepath = Path.Combine(_serverPath, logPath);
            if (!Directory.Exists(filepath))
            {
                response.Add( filepath, "Error: This Path Is Invalid");
                return response;
            }
            var txtFiles = Directory.EnumerateFiles(filepath, "*.txt");
            foreach (string currentFile in txtFiles)
            {
                string resp = File.ReadAllText(currentFile);
                response.Add(currentFile, resp);
            }
            return response;

        }
    }
}