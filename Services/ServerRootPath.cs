using System;
using System.IO;

namespace notifier.Services
{
    public static class ServerRootPath
    {
        public static string MapPath(string path)
        {
            return Path.Combine(
                (string)AppDomain.CurrentDomain.GetData("ContentRootPath") ?? string.Empty,
                path);
        }


        public static string RootPath()
        {
            return (string)AppDomain.CurrentDomain.GetData("ContentRootPath") ?? string.Empty;
        }
    }
}
