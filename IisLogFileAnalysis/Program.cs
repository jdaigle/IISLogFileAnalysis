using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.IO;

namespace IISLogFileAnalysis {
    public class Program {

        public static string logDirectory = @"C:\temp\7-30-2010-prod-logs\";

        static void Main(string[] args) {
            if (args.Length != 0) {
                logDirectory = args[0] + "\\";
            }

            var logTable = new LogTable(Path.Combine(logDirectory, "columns.txt"));
            var analysis = new LogFileAnalysis();

            // Find all log files
            foreach (var file in new DirectoryInfo(logDirectory).GetFiles("*.log").OrderBy(x => x.FullName)) {
                Console.WriteLine("Loading log file {0}" + file.FullName);
                var logFileReader = new LogFileReader(file.FullName, logTable, analysis);
                logFileReader.Execute();
            }

            // Start analysis            
            var report = analysis.BuildReport();
            File.WriteAllText(Path.Combine(logDirectory, "report.txt"), report);
        }
    }
}
