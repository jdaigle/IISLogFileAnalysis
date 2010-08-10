using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.IO;

namespace IISLogFileAnalysis {
    public class LogFileAnalysis {

        public void AddRow(DataRow row) {
            ExtractIPAddressInformation(row);
            ExtractUrlInformation(row);
            ExtractPerSecondInformation(row);
        }

        public string BuildReport() {
            var reportBuilder = new StringBuilder();
            reportBuilder.AppendLine(BuildIPAddressReport());
            reportBuilder.AppendLine(BuildUrlReport());
            reportBuilder.AppendLine(BuildHighCostUrlReport());
            reportBuilder.AppendLine(BuildBytesTransferredReport());
            BuildRequestsPerSecondReport();
            BuildRequestExecutionTimePerSecondReport();
            return reportBuilder.ToString();
        }

        private Dictionary<string, int> IpAddresses = new Dictionary<string, int>();

        private void ExtractIPAddressInformation(DataRow row) {
            var ipaddress = row["c-ip"] as string;
            if (!string.IsNullOrEmpty(ipaddress)) {
                if (!IpAddresses.ContainsKey(ipaddress))
                    IpAddresses[ipaddress] = 0;
                IpAddresses[ipaddress] = IpAddresses[ipaddress] + 1;
            }
        }

        private string BuildIPAddressReport() {
            var totalHits = IpAddresses.Sum(x => x.Value);

            var header = string.Format("Total Requests: {0}" + Environment.NewLine +
                                       "Total Unique IP Address: {1}" + Environment.NewLine +
                                       "Traffic Per IP Address:" + Environment.NewLine, totalHits, IpAddresses.Count)
                                       + "\tIP Address     \tNumber Of Requests" + Environment.NewLine;
            var data = "";
            foreach (var ip in IpAddresses.OrderByDescending(x => x.Value)) {
                data += string.Format("\t{0}\t{1}" + Environment.NewLine, ip.Key.PadRight(15), ip.Value.ToString());
            }
            return header + data;
        }

        private Dictionary<string, Double<int, int>> urls = new Dictionary<string, Double<int, int>>();
        private Dictionary<string, Double<int, int>> highCostUrls = new Dictionary<string, Double<int, int>>();
        private Dictionary<string, Double<long, long>> bytesTransferredPerUrl = new Dictionary<string, Double<long, long>>();

        private void ExtractUrlInformation(DataRow row) {
            var url = row["cs-uri-stem"].ToString().ToLower();
            var timeTaken = int.Parse(row["time-taken"].ToString());
            var bytesSent = long.Parse(row["sc-bytes"].ToString());
            var bytesRecievied = long.Parse(row["cs-bytes"].ToString());
            var queryString = row["cs-uri-query"].ToString().ToLower();

            var url_appender = "";
            if (queryString.Contains("OnlyScheduled=true".ToLower()))
                url_appender = "_scheduled";
            if (queryString.Contains("OnlyPastVisits=true".ToLower()))
                url_appender = "_vistList";
            if (queryString.Contains("OnlyScheduled=false".ToLower()) &&
                queryString.Contains("OnlyPastVisits=false".ToLower()))
                url_appender = "_visitHistory";

            url += url_appender;

            if (!string.IsNullOrEmpty(url)) {

                // URL Information
                if (!urls.ContainsKey(url))
                    urls[url] = new Double<int, int>(0, 0);
                var info = urls[url];
                info.First = info.First + 1;
                info.Second = info.Second + timeTaken;
                urls[url] = info;

                // High cost URL information
                if (timeTaken > 100) {
                    if (!highCostUrls.ContainsKey(url))
                        highCostUrls[url] = new Double<int, int>(0, 0);
                    info = highCostUrls[url];
                    info.First = info.First + 1;
                    info.Second = info.Second + timeTaken;
                    highCostUrls[url] = info;
                }

                // Bytes transferred
                if (!bytesTransferredPerUrl.ContainsKey(url))
                    bytesTransferredPerUrl[url] = new Double<long, long>(0, 0);
                var newinfo = bytesTransferredPerUrl[url];
                newinfo.First = newinfo.First + bytesSent;
                newinfo.Second = newinfo.Second + bytesRecievied;
                bytesTransferredPerUrl[url] = newinfo;
            }
        }

        private string BuildUrlReport() {
            var header = string.Format("Total Unique URLs: {0}" + Environment.NewLine +
                                       "Hits Per URL:" + Environment.NewLine, urls.Count) +
                                       "\tRequests  \tAvg Exec Time\tUrl" + Environment.NewLine;
            var data = "";
            foreach (var url in urls.OrderByDescending(x => x.Value.First)) {
                data += string.Format("\t{1}\t{2}\t{0}" + Environment.NewLine, url.Key, url.Value.First.ToString().PadRight(10), ((url.Value.Second / url.Value.First).ToString() + "ms").PadRight(13));
            }
            return header + data;
        }

        private string BuildHighCostUrlReport() {
            var header = string.Format("High Cost Requests - These are stastics for any request that took longer than 100ms" + Environment.NewLine +
                                       "Total Unique URLs: {0}" + Environment.NewLine +
                                       "Hits Per URL:" + Environment.NewLine, highCostUrls.Count) +
                                       "\tRequests  \tAvg Exec Time\tUrl" + Environment.NewLine;
            var data = "";
            foreach (var url in highCostUrls.OrderByDescending(x => x.Value.Second / x.Value.First)) {
                data += string.Format("\t{1}\t{2}\t{0}" + Environment.NewLine, url.Key, url.Value.First.ToString().PadRight(10), ((url.Value.Second / url.Value.First).ToString() + "ms").PadRight(13));
            }
            return header + data;
        }

        private string BuildBytesTransferredReport() {
            var header = string.Format("Bytes Transferred Per Url - " + Environment.NewLine +
                                       "Total {0}, Sent {1}, Recieved {2}" + Environment.NewLine +
                                       "Hits Per URL:" + Environment.NewLine, bytesTransferredPerUrl.Sum(x => x.Value.First + x.Value.Second), bytesTransferredPerUrl.Sum(x => x.Value.First), bytesTransferredPerUrl.Sum(x => x.Value.Second)) +
                                       "\tBytes Total    \tBytes Sent     \tBytes Recieved \tAvg Bytes Sent \tAvg Bytes Recv \tUrl" + Environment.NewLine;
            var data = "";
            foreach (var url in bytesTransferredPerUrl.OrderByDescending(x => x.Value.First)) {
                var totalRequests = urls[url.Key].First;
                data += string.Format("\t{1}\t{2}\t{3}\t{4}\t{5}\t{0}" + Environment.NewLine,
                    url.Key,
                    (url.Value.First + url.Value.Second).ToString().PadRight(15),
                    url.Value.First.ToString().PadRight(15),
                    url.Value.Second.ToString().PadRight(15),
                    (url.Value.First / totalRequests).ToString().PadRight(15),
                    (url.Value.Second / totalRequests).ToString().PadRight(15));
            }
            return header + data;
        }

        private Dictionary<int, Dictionary<string, int>> totalRequestsPerSecond = new Dictionary<int, Dictionary<string, int>>();
        private Dictionary<int, Dictionary<string, Double<int, int>>> totalRequestExecutionTimePerSecond = new Dictionary<int, Dictionary<string, Double<int, int>>>();

        private void ExtractPerSecondInformation(DataRow row) {
            var time = TimeSpan.Parse(row["time"].ToString().ToLower());
            if (time < new TimeSpan(17, 30, 0))
                return;
            var url = row["cs-uri-stem"].ToString().ToLower();
            var timeTaken = int.Parse(row["time-taken"].ToString());
            var secondOfDay = (int)Math.Floor(time.TotalSeconds);
            
            var requestsPerSecond = GetRequestsPerSecond(secondOfDay);
            if (!requestsPerSecond.ContainsKey(url)) {
                requestsPerSecond.Add(url, 1);
            } else {
                requestsPerSecond[url] = requestsPerSecond[url] + 1;
            }

            var requestExecutionTimePerSecond = GetRequestExecutionTimePerSecond(secondOfDay);
            if (!requestExecutionTimePerSecond.ContainsKey(url)) {
                requestExecutionTimePerSecond.Add(url, new Double<int, int>(1, timeTaken));
            } else {
                var original = requestExecutionTimePerSecond[url];
                requestExecutionTimePerSecond[url] = new Double<int, int>(original.First + 1, original.Second + timeTaken);
            }
        }

        private Dictionary<string, int> GetRequestsPerSecond(int second) {
            if (!totalRequestsPerSecond.ContainsKey(second))
                totalRequestsPerSecond.Add(second, new Dictionary<string, int>());
            return totalRequestsPerSecond[second];
        }

        private Dictionary<string, Double<int, int>> GetRequestExecutionTimePerSecond(int second) {
            if (!totalRequestExecutionTimePerSecond.ContainsKey(second))
                totalRequestExecutionTimePerSecond.Add(second, new Dictionary<string, Double<int, int>>());
            return totalRequestExecutionTimePerSecond[second];
        }


        private void BuildRequestsPerSecondReport() {
            using (var stream = File.Create(Path.Combine(Program.logDirectory, "numberOfRequests.csv"))) {
                using (var writer = new StreamWriter(stream)) {
                    var orderedRequestsPerSecond = totalRequestsPerSecond.OrderBy(x => x.Key);

                    writer.Write("Url");
                    foreach (var request in orderedRequestsPerSecond) {
                        writer.Write(", " + request.Key.ToString());
                    }
                    writer.WriteLine();
                    foreach (var url in urls.OrderByDescending(x => x.Value.First)) {
                        writer.Write(url.Key);
                        foreach (var second in orderedRequestsPerSecond) {
                            if (second.Value.ContainsKey(url.Key)) {
                                writer.Write(", " + second.Value[url.Key].ToString());
                            } else {
                                writer.Write(", 0");
                            }
                            
                        }
                        writer.WriteLine();
                    }
                }
            }
        }

        private void BuildRequestExecutionTimePerSecondReport() {
            using (var stream = File.Create(Path.Combine(Program.logDirectory, "requestExecutionTime.csv"))) {
                using (var writer = new StreamWriter(stream)) {
                    var orderedRequestsPerSecond = totalRequestsPerSecond.OrderBy(x => x.Key);

                    writer.Write("Url");
                    foreach (var request in orderedRequestsPerSecond) {
                        writer.Write(", " + request.Key.ToString());
                    }
                    writer.WriteLine();
                    foreach (var url in urls.OrderByDescending(x => x.Value.Second / x.Value.First)) {
                        writer.Write(url.Key);
                        foreach (var second in totalRequestExecutionTimePerSecond) {
                            if (second.Value.ContainsKey(url.Key)) {
                                var stuff = second.Value[url.Key];
                                int avg = stuff.Second / stuff.First;
                                writer.Write(", " + avg.ToString());
                            } else {
                                writer.Write(", 0");
                            }

                        }
                        writer.WriteLine();
                    }
                }
            }
        }

    }
}
