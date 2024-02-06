using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System;
using System.Linq;
using System.Management;

namespace ImgSoh
{
    public static class ExifHelper
    {
        private static readonly ExifToolWrapper _wrapper = new ExifToolWrapper(AppConsts.FileExifTool);

        public static KeyValuePair<string, string>[] GetFingerPrint(string name, byte[] imagedata)
        {
            KeyValuePair<string, string>[] fingerprint;
            var tempname = $"{AppConsts.PathGbProtected}\\{name}";
            try {
                File.WriteAllBytes(tempname, imagedata);
                fingerprint = _wrapper.FetchExifFrom(tempname);
            }
            finally {
                if (File.Exists(tempname)) {
                    File.Delete(tempname);
                }
            }

            return fingerprint;
        }

        public static void Start()
        {
            _wrapper.Start();
        }

        public static void Stop()
        {
            _wrapper.Stop();
        }

        public static string FingerprintToString(KeyValuePair<string, string>[] fingerprint)
        {
            var sb = new StringBuilder();
            foreach (var e in fingerprint) {
                if (sb.Length > 0) {
                    sb.Append('\t');
                }

                sb.Append(e.Key);
                sb.Append('\t');
                sb.Append(e.Value);
            }

            return sb.ToString();
        }

        public static KeyValuePair<string, string>[] StringtoFingerPrint(string array)
        {
            var fingerprint = new List<KeyValuePair<string, string>>();
            if (string.IsNullOrEmpty(array)) {
                return fingerprint.ToArray();
            }

            var par = array.Split('\t');
            for (var i = 0; i < par.Length; i += 2) {
                fingerprint.Add(new KeyValuePair<string, string>(par[i], par[i + 1]));
            }

            return fingerprint.ToArray();
        }

        public static void GetMatch(
            KeyValuePair<string, string>[] x, KeyValuePair<string, string>[] y, 
            out int matchname, out int matchvalue)
        {
            matchname = 0; 
            matchvalue = 0;
            var i = 0;
            var j = 0;
            while (i < x.Length && j < y.Length) {
                var c = string.CompareOrdinal(x[i].Key, y[j].Key);
                if (c == 0) {
                    matchname++;
                    if (string.CompareOrdinal(x[i].Value, y[j].Value) == 0) {
                        matchvalue++;
                    }

                    i++;
                    j++;
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
        }
    }

    [Serializable]
    public class ExifToolException : Exception
    {
        public ExifToolException(string msg) : base(msg)
        { }
    }

    public readonly struct ExifToolResponse
    {
        public bool IsSuccess { get; }
        public string Result { get; }

        public ExifToolResponse(bool b, string r)
        {
            IsSuccess = b;
            Result = r;
        }

        //to use ExifToolResponse directly in if (discarding response)
        public static implicit operator bool(ExifToolResponse r) => r.IsSuccess;
    }

    public class ExifToolWrapper : IDisposable
    {
        private string ExifToolPath { get; }

        private const string ExeName = "exiftool(-k).exe";
        private const string Arguments = "-m -q -q -stay_open True -@ - -common_args -d \"%Y.%m.%d %H:%M:%S\" -c \"%d %d %.6f\" -json -b -U --File:all";
        private const string ExitMessage = "-- press RETURN --";

        private const double SecondsToWaitForError = 1.0;
        private const double SecondsToWaitForStop = 5.0;

        private enum ExeStatus { Stopped, Starting, Ready, Stopping }
        private ExeStatus Status { get; set; }

        private enum CommunicationMethod { Auto, ViaFile }

        private const CommunicationMethod Method = CommunicationMethod.Auto;

        private int _cmdCnt;
        private readonly StringBuilder _output = new StringBuilder();
        private readonly StringBuilder _error = new StringBuilder();

        private readonly ProcessStartInfo _psi;
        private Process _proc;

        private readonly ManualResetEvent _waitHandle = new ManualResetEvent(true);
        private readonly ManualResetEvent _waitForErrorHandle = new ManualResetEvent(true);

        public ExifToolWrapper(string path)
        {
            ExifToolPath = path;
            if (!File.Exists(ExifToolPath)) {
                throw new ExifToolException($"{ExeName} not found");
            }

            _psi = new ProcessStartInfo {
                FileName = ExifToolPath,
                Arguments = Arguments,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true
            };

            Status = ExeStatus.Stopped;
        }

        private void OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (string.IsNullOrEmpty(e?.Data))
                return;

            if (Status == ExeStatus.Starting) {
                _waitHandle.Set();

                return;
            }

            if (string.Equals(e.Data, $"{{ready{_cmdCnt}}}", StringComparison.OrdinalIgnoreCase)) {
                _waitHandle.Set();

                return;
            }

            _output.AppendLine(e.Data);
        }

        private void ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (string.IsNullOrEmpty(e?.Data)) {
                return;
            }

            if (string.Equals(e.Data, ExitMessage, StringComparison.OrdinalIgnoreCase)) {
                _proc?.StandardInput.WriteLine();
                return;
            }

            _error.AppendLine(e.Data);
            _waitForErrorHandle.Set();
        }

        public void Start()
        {
            if (Status != ExeStatus.Stopped) {
                throw new ExifToolException("Process is not stopped");
            }

            Status = ExeStatus.Starting;

            _proc = new Process { StartInfo = _psi, EnableRaisingEvents = true };
            _proc.OutputDataReceived += OutputDataReceived;
            _proc.ErrorDataReceived += ErrorDataReceived;
            _proc.Exited += ProcExited;
            _proc.Start();

            _proc.BeginOutputReadLine();
            _proc.BeginErrorReadLine();
            _proc.StandardInput.AutoFlush = true;

            _waitHandle.Reset();
            _proc.StandardInput.Write("-ver\n-execute0000\n");
            _waitHandle.WaitOne();

            Status = ExeStatus.Ready;
        }

        private void ProcExited(object sender, EventArgs e)
        {
            if (_proc != null) {
                _proc.Dispose();
                _proc = null;
            }

            Status = ExeStatus.Stopped;
        }

        private static void KillProcessAndChildren(int pid)
        {
            if (pid == 0) {
                return;
            }

            var searcher = new ManagementObjectSearcher
                ("Select * From Win32_Process Where ParentProcessID=" + pid);
            ManagementObjectCollection moc = searcher.Get();
            foreach (var o in moc) {
                var mo = (ManagementObject)o;
                KillProcessAndChildren(Convert.ToInt32(mo["ProcessID"]));
            }
            try {
                var proc = Process.GetProcessById(pid);
                proc.Kill();
            }
            catch (ArgumentException) {
                // Process already exited.
            }
        }

        public void Stop()
        {
            if (Status == ExeStatus.Ready) {
                KillProcessAndChildren(_proc.Id);
                //_proc.Kill();
                //_proc.StandardInput.Write("-stay_open\nFalse\n");
                Status = ExeStatus.Stopping;
                _waitHandle.Reset();
                if (!_waitHandle.WaitOne(TimeSpan.FromSeconds(SecondsToWaitForStop))) {
                    if (_proc != null) {
                        try {
                            _proc.Kill();
                            _proc.WaitForExit((int)(1000 * SecondsToWaitForStop / 2));
                        }
                        catch (Exception xcp) {
                            Debug.WriteLine(xcp.ToString());
                        }

                        _proc = null;
                    }

                    Status = ExeStatus.Stopped;
                }
            }
        }

        private readonly object _lockObj = new object();

        private void DirectSend(string cmd, params object[] args)
        {
            _proc.StandardInput.Write("{0}\n-execute{1}\n", args.Length == 0 ? cmd : string.Format(cmd, args), _cmdCnt);
            _waitHandle.WaitOne();
        }

        private void SendViaFile(string cmd, params object[] args)
        {
            var argFile = Path.GetTempFileName();
            try {
                using (var sw = new StreamWriter(argFile)) {
                    sw.WriteLine(args.Length == 0 ? cmd : string.Format(cmd, args), _cmdCnt);
                }

                _proc.StandardInput.Write("-charset\nfilename=UTF8\n-@\n{0}\n-execute{1}\n", argFile, _cmdCnt);
                _waitHandle.WaitOne();
            }
            finally {
                File.Delete(argFile);
            }
        }

        private ExifToolResponse SendCommand(string cmd, params object[] args) => SendCommand(Method, cmd, args);
        private ExifToolResponse SendCommand(CommunicationMethod method, string cmd, params object[] args)
        {
            if (Status != ExeStatus.Ready)
                throw new ExifToolException("Process must be ready");

            ExifToolResponse resp;
            lock (_lockObj) {
                _waitHandle.Reset();
                _waitForErrorHandle.Reset();

                if (method == CommunicationMethod.ViaFile)
                    SendViaFile(cmd, args);
                else
                    DirectSend(cmd, args);

                if (_output.Length == 0) {
                    _waitForErrorHandle.WaitOne(TimeSpan.FromSeconds(SecondsToWaitForError));
                    resp = new ExifToolResponse(false, _error.ToString());
                    _error.Clear();
                }
                else {
                    resp = new ExifToolResponse(true, _output.ToString());
                    _output.Clear();
                }

                _cmdCnt++;
            }

            if (!resp.IsSuccess && method == CommunicationMethod.Auto) {
                var err = resp.Result.ToLowerInvariant();
                if (err.Contains("file not found") || err.Contains("invalid filename encoding")) {
                    return SendCommand(CommunicationMethod.ViaFile, cmd, args);
                }
            }

            return resp;
        }

        public KeyValuePair<string, string>[] FetchExifFrom(string path)
        {
            var result = new SortedList<string, string>();
            if (!File.Exists(path)) {
                return result.ToArray();
            }

            var cmdRes = SendCommand(path);
            if (!cmdRes) {
                return result.ToArray();
            }

            var content = cmdRes.Result;
            const string ready = "{ready0000}\r\n";
            if (content.StartsWith(ready)) {
                content = content.Substring(ready.Length);
            }

            content = content.Replace("[{", "{");
            content = content.Replace("}]", "}");

            var o1 = JObject.Parse(content);
            foreach (var jtocken in o1) {
                var name = jtocken.Key;
                var value = jtocken.Value;
                if (value.HasValues) {
                    var array = string.Join(" ", value.Values<string>().ToArray());
                    result.Add(name, array);
                    //Debug.WriteLine($"{name} {array}");
                }
                else {
                    var svalue = value.ToString();
                    const string base64 = "base64:";
                    if (!svalue.StartsWith(base64)) {
                        if (!svalue.Any(char.IsControl)) {
                            result.Add(name, svalue);
                            //Debug.WriteLine($"{name} {svalue}");
                        }
                        else {
                            var buffer = Encoding.UTF8.GetBytes(svalue);
                            var sb = new StringBuilder();
                            using (var md5 = MD5.Create()) {
                                var hashMD5 = md5.ComputeHash(buffer);
                                foreach (var b in hashMD5) {
                                    sb.Append($"{b:x2}");
                                }
                            }

                            svalue = sb.ToString();
                            result.Add(name, svalue);
                            //Debug.WriteLine($"{name} {svalue}");
                        }
                    }
                    else {
                        svalue = svalue.Substring(base64.Length);
                        var buffer = Convert.FromBase64String(svalue);
                        var sb = new StringBuilder();
                        using (var md5 = MD5.Create()) {
                            var hashMD5 = md5.ComputeHash(buffer);
                            foreach (var b in hashMD5) {
                                sb.Append($"{b:x2}");
                            }
                        }

                        svalue = sb.ToString();
                        result.Add(name, svalue);
                        //Debug.WriteLine($"{name} {svalue}");
                    }
                }
            }

            return result.ToArray();
        }

        public DateTime? GetCreationTime(string path)
        {
            if (!File.Exists(path)) {
                return null;
            }

            var cmdRes = SendCommand("-DateTimeOriginal\n-s3\n{0}", path);
            if (!cmdRes) {
                return null;
            }

            if (DateTime.TryParseExact(cmdRes.Result,
                    "yyyy.MM.dd HH:mm:ss",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AllowWhiteSpaces,
                    out var dt)) {
                return dt;
            }

            return null;
        }

        #region IDisposable Members

        public void Dispose()
        {
            if (_proc != null && Status == ExeStatus.Ready) {
                Stop();
            }

            _waitHandle.Dispose();
        }

        #endregion
    }
}
