using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;

namespace ImgSoh
{
    public class ExifItem
    {
        public string Group { get; }
        public string Name { get; }
        public string Value { get; }

        public ExifItem(string group, string name, string value)
        {
            Group = group;
            Name = name;
            Value = value;
        }
    }

    public class ExifInfo
    {
        public ExifItem[] Items { get; }
        public DateTime Taken { get; }

        private string stdOut;
        private string stdErr;
        private readonly Process activeProcess;

        private void Thread_ReadStandardError()
        {
            if (activeProcess != null) {
                stdErr = activeProcess.StandardError.ReadToEnd();
            }
        }

        private void Thread_ReadStandardOut()
        {
            if (activeProcess != null) {
                stdOut = activeProcess.StandardOutput.ReadToEnd();
            }
        }

        public ExifInfo(string filename)
        {
            Items = Array.Empty<ExifItem>();
            Taken = DateTime.MinValue;

            var args =
                $"/c {AppConsts.FileExifTool} -fast -q -q -m -t -a -u -G0 -s --File:all --ExifTool:all \"{filename}\"";
            var psi = new ProcessStartInfo("cmd.exe", args) {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            var thread_ReadStandardError = new Thread(Thread_ReadStandardError);
            var thread_ReadStandardOut = new Thread(Thread_ReadStandardOut);
            activeProcess = Process.Start(psi);
            if (activeProcess != null) {
                if (psi.RedirectStandardError) {
                    thread_ReadStandardError.Start();
                }

                if (psi.RedirectStandardOutput) {
                    thread_ReadStandardOut.Start();
                }

                activeProcess.WaitForExit();

                thread_ReadStandardError.Join();
                thread_ReadStandardOut.Join();
                var output = stdOut + stdErr;
                var lines = output.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                var list = new List<ExifItem>();
                foreach (var line in lines) {
                    var pars = line.Split('\t');
                    if (pars.Length == 3) {
                        var exifitem = new ExifItem(pars[0], pars[1], pars[2]);
                        list.Add(exifitem);
                        if (pars[1].IndexOf("Profile", StringComparison.OrdinalIgnoreCase) >= 0) {
                            continue;
                        }

                        var matches = Regex.Matches(pars[2], @"\b\d{4}:\d{2}:\d{2} \d{2}:\d{2}:\d{2}\b");
                        if (matches.Count > 0) {
                            foreach (Match match in matches) {
                                if (DateTime.TryParseExact(match.Value, "yyyy:MM:dd HH:mm:ss", CultureInfo.InvariantCulture,
                                        DateTimeStyles.None, out var datetime)) {
                                    if (datetime > Taken) {
                                        Taken = datetime;
                                    }
                                }
                            }
                        }
                        else {
                            matches = Regex.Matches(pars[2], @"\b\d{4}:\d{2}:\d{2}\b");
                            if (matches.Count > 0) {
                                foreach (Match match in matches) {
                                    if (DateTime.TryParseExact(match.Value, "yyyy:MM:dd", CultureInfo.InvariantCulture,
                                            DateTimeStyles.None, out var datetime)) {
                                        if (datetime > Taken) {
                                            Taken = datetime;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                Items = list.ToArray();
            }
        }

        /*
        private string Compare(ExifInfo other)
        {
            ExifItem diff = null;
            var i = 0;
            var j = 0;
            while (i < Items.Length && j < other.Items.Length) {
                var c = string.CompareOrdinal(Items[i].Group, other.Items[j].Group);
                if (c == 0) {
                    c = string.CompareOrdinal(Items[i].Name, other.Items[j].Name);
                    if (c == 0) {
                        c = string.CompareOrdinal(Items[i].Value, other.Items[j].Value);
                        if (c == 0) {
                            i++;
                            j++;
                        }
                        else {
                            diff = Items[i];
                            if (c < 0) {
                                i++;
                            }
                            else {
                                j++;
                            }
                        }
                    }
                    else {
                        if (c < 0) {
                            i++;
                        }
                        else {
                            j++;
                        }
                    }
                }
                else {
                    if (c < 0) {
                        i++;
                    }
                    else {
                        j++;
                    }
                }
            }

            if (diff == null) {
                diff = Items[0];
            }

            var value = diff.Value;
            if (value.Length > 25) {
                value = $"{AppConsts.CharEllipsis}{value.Substring(value.Length - 25, 25)}";
            }

            var result = $"{diff.Name}={value}";
            return result;
        }

        public string GetMatch(ExifInfo other)
        {
            if (Items.Length == 0) {
                return string.Empty;
            }

            if (string.IsNullOrWhiteSpace(LastModifiedString)) {
                if (other.Items.Length == 0) {
                    return $"E{Items.Length}";
                }

                return Compare(other);
            }

            if (string.IsNullOrWhiteSpace(other.LastModifiedString)) {
                return LastModifiedString;
            }

            if (Taken.Equals(other.Taken)) {
                return Compare(other);
            }

            return LastModifiedString;
        }
        */
    }
}
