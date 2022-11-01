using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace TumblThreeSubProcess
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length != 2) return;
            if (!int.TryParse(args[0], out int processId)) return;
            var filepath = args[1];
            Process process;
            do
            {
                try
                {
                    process = Process.GetProcessById(processId);
                }
                catch (ArgumentException)
                {
                    process = null;
                }
                Thread.Sleep(10000);
            } while (process != null);
            try
            {
                File.Delete(filepath);
            }
            catch (Exception)
            {
            }
        }
    }
}
