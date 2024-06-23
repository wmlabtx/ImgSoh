using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

/*
-stay_open FLAG
If FLAG is 1 or True (case insensitive), causes exiftool keep reading from the -@ ARGFILE even after reaching the end of file. This feature allows calling applications to pre-load exiftool, thus avoiding the overhead of loading exiftool for each command. The procedure is as follows:

1) Execute exiftool -stay_open True -@ ARGFILE, where ARGFILE is the name of an existing (possibly empty) argument file or - to pipe arguments from the standard input.

2) Write exiftool command-line arguments to ARGFILE, one argument per line (see the -@ option for details).

3) Write -execute\n to ARGFILE, where \n represents a newline sequence. (Note: You may need to flush your write buffers here if using buffered output.) ExifTool will then execute the command with the arguments received up to this point, send a "{ready}" message to stdout when done (unless the -q or -T option is used), and continue trying to read arguments for the next command from ARGFILE. To aid in command/response synchronization, any number appended to the -execute option is echoed in the "{ready}" message. For example, -execute613 results in "{ready613}". When this number is added, -q no longer suppresses the "{ready}" message. (Also, see the -echo3 and -echo4 options for additional ways to pass signals back to your application.)

4) Repeat steps 2 and 3 for each command.

5) Write -stay_open\nFalse\n (or -stay_open\n0\n) to ARGFILE when done. This will cause exiftool to process any remaining command-line arguments then exit normally.

The input ARGFILE may be changed at any time before step 5 above by writing the following lines to the currently open ARGFILE:

    -stay_open
    True
    -@
    NEWARGFILE
This causes ARGFILE to be closed, and NEWARGFILE to be kept open. (Without the -stay_open here, exiftool would have returned to reading arguments from ARGFILE after reaching the end of NEWARGFILE.)

-@ ARGFILE
Read command-line arguments from the specified file. The file contains one argument per line (NOT one option per line -- some options require additional arguments, and all arguments must be placed on separate lines). Blank lines and lines beginning with # are ignored (unless they start with #[CSTR], in which case the rest of the line is treated as a C string, allowing standard C escape sequences such as "\n" for a newline). White space at the start of a line is removed. Normal shell processing of arguments is not performed, which among other things means that arguments should not be quoted and spaces are treated as any other character. ARGFILE may exist relative to either the current directory or the exiftool directory unless an absolute pathname is given.

For example, the following ARGFILE will set the value of Copyright to "Copyright YYYY, Phil Harvey", where "YYYY" is the year of CreateDate:

    -d
    %Y
    -copyright<Copyright $createdate, Phil Harvey
Arguments in ARGFILE behave exactly the same as if they were entered at the location of the -@ option on the command line, with the exception that the -config and -common_args options may not be used in an ARGFILE.
 */

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
        public ExifItem[] Items { get; private set; }
        public DateTime Taken { get; private set; }

        private const string _args = "-stay_open 1 -@ - -common_args -charset UTF8 -fast -m -t -a -u -G0 -s --File:all --ExifTool:all -charset UTF8";
        private Process _process;
        private StreamWriter _stdin;
        private StreamReader _stdout;

        public ExifInfo()
        {
            var psi = new ProcessStartInfo(AppConsts.FileExifTool, _args) {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                StandardOutputEncoding = new UTF8Encoding(false)
            };

            _process = Process.Start(psi);
            if (_process != null) {
                _stdin = new StreamWriter(_process.StandardInput.BaseStream, new UTF8Encoding(false));
                _stdout = _process.StandardOutput;
            }
        }

        public void Read(string filename)
        {
            Items = Array.Empty<ExifItem>();
            Taken = DateTime.MinValue;

            _stdin.Write(filename);
            _stdin.Write("\n-execute\n");
            _stdin.Flush();

            var list = new List<ExifItem>();
            while (true) {
                var line = _stdout.ReadLine();
                if (line == null || line.StartsWith("{ready")) {
                    break;
                }

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

        public void Stop()
        {
            if (_process != null) {
                if (!_process.HasExited) {
                    _stdin.Write("-stay_open\n0\n-execute\n");
                    _stdin.Flush();

                    if (!_process.WaitForExit(30000)) {
                        _process.Kill();
                    }
                }

                _stdout?.Dispose();
                _stdout = null;
                _stdin?.Dispose();
                _stdin = null;
                _process.Dispose();
                _process = null;
            }
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
