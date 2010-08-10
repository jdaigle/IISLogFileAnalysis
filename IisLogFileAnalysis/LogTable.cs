using System;
using System.Data;
using System.IO;

namespace IISLogFileAnalysis {
    public class LogTable : DataTable {

        public LogTable(string columnsFile) {
            string columns = File.ReadAllText(columnsFile);
            foreach (var col in columns.Split(new string[] { " " }, StringSplitOptions.None)) {
                Columns.Add(col);
            }
        }
    }
}
