using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.IO;

namespace IISLogFileAnalysis {
    public class LogFileReader {

        private readonly string logFile;
        private readonly DataTable logTable;
        private readonly LogFileAnalysis logFileAnalysis;
        private DataRow lastRow;

        public LogFileReader(string logFile, DataTable logTable, LogFileAnalysis logFileAnalysis) {
            this.logFile = logFile;
            this.logTable = logTable;
            this.logFileAnalysis = logFileAnalysis;
        }

        public void Execute() {
            // Open the file for reading
            using (var reader = new StreamReader(File.Open(logFile, FileMode.Open, FileAccess.Read))) {
                while (!reader.EndOfStream) {
                    var line = reader.ReadLine();
                    ParseLine(line);
                }
            }
        }

        private int NumberOfFields { get { return logTable.Columns.Count; } }

        private void ParseLine(string line) {
            if (lastRow == null)
                lastRow = logTable.NewRow();
            // if the line begins with "#" then skip the line
            if (line.StartsWith("#"))
                return;
            // Split the line by space
            var parts = line.Split(new string[] { " " }, StringSplitOptions.None);
            if (parts.Length != NumberOfFields)
                throw new InvalidOperationException("Log file row does not match expected number of fields");
            // Create a row
            var row = lastRow;
            for (int i = 0; i < parts.Length; i++) {
                row[i] = parts[i];
            }
            logFileAnalysis.AddRow(row);
        }
    }
}
