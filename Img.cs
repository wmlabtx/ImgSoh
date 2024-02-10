using System;
 using System.Collections.Generic;
 using System.Drawing;
 using System.Linq;
 using System.Text;

 namespace ImgSoh
{
    public class Img
    {
        public string Hash { get; }
        public string Folder { get; }

        private readonly byte[] _vector;
        public byte[] GetVector()
        {
            return _vector;
        }

        public void SetVector(byte[] vector)
        {
            Array.Copy(vector, _vector, 4096);
            AppDatabase.ImgUpdateProperty(Hash, AppConsts.AttributeVector, _vector);
        }

        public DateTime LastView { get; private set; }
        public void SetLastView(DateTime lastview)
        {
            LastView = lastview;
            AppDatabase.ImgUpdateProperty(Hash, AppConsts.AttributeLastView, LastView);
        }

        public RotateFlipType Orientation { get; private set; }
        public void SetOrientation(RotateFlipType rft)
        {
            Orientation = rft;
            AppDatabase.ImgUpdateProperty(Hash, AppConsts.AttributeOrientation, Helper.RotateFlipTypeToByte(Orientation));
        }

        public float Distance { get; private set; }
        public void SetDistance(float distance)
        {
            Distance = distance;
            AppDatabase.ImgUpdateProperty(Hash, AppConsts.AttributeDistance, Distance);
        }

        public short Match { get; private set; }
        public void SetMatch(short match)
        {
            Match = match;
            AppDatabase.ImgUpdateProperty(Hash, AppConsts.AttributeMatch, Match);
        }

        public DateTime LastCheck { get; private set; }
        public void SetLastCheck(DateTime lastcheck)
        {
            LastCheck = lastcheck;
            AppDatabase.ImgUpdateProperty(Hash, AppConsts.AttributeLastCheck, LastCheck);
        }

        public string Next { get; private set; }
        public void SetNext(string next)
        {
            Next = next;
            AppDatabase.ImgUpdateProperty(Hash, AppConsts.AttributeNext, next);
        }

        public bool Verified { get; private set; }
        public void SetVerified(bool verified)
        {
            Verified = verified;
            AppDatabase.ImgUpdateProperty(Hash, AppConsts.AttributeVerified, verified);
        }

        public DateTime DateTaken { get; private set; }
        public void SetDateTaken(DateTime datetaken)
        {
            DateTaken = datetaken;
            AppDatabase.ImgUpdateProperty(Hash, AppConsts.AttributeDateTaken, datetaken);
        }

        private readonly SortedList<string, char> _history = new SortedList<string, char>();
        public int HistoryCount => _history.Count;
        public int FamilyCount => _history.Count(e => e.Value == '*');
        public string[] HistoryArray => _history.Keys.ToArray();
        public bool IsInHistory(string hash)
        {
            return _history.ContainsKey(hash);
        }

        public bool IsInFamily(string hash)
        {
            return _history.ContainsKey(hash) && _history[hash] == '*';
        }

        public void GetHistory(out string history, out string family)
        {
            var sbhistory = new StringBuilder();
            var sbfamily = new StringBuilder();
            foreach (var e in _history) {
                sbhistory.Append(e.Key);
                sbfamily.Append(e.Value);
            }

            history = sbhistory.ToString();
            family = sbfamily.ToString();
        }

        private void SetHistory(string history, string family)
        {
            _history.Clear();
            var ofamily = 0;
            var ohistory = 0;
            while (ohistory + 12 <= history.Length) {
                var ehistory = history.Substring(ohistory, 12);
                var efamily =  ofamily < family.Length ? family[ofamily] : ehistory[0];
                _history.Add(ehistory, efamily);
                ofamily++;
                ohistory += 12;
            }
        }

        private void UpdateHistory()
        {
            GetHistory(out var history, out var family);
            AppDatabase.ImgUpdateProperty(Hash, AppConsts.AttributeHistory, history);
            AppDatabase.ImgUpdateProperty(Hash, AppConsts.AttributeFamily, family);
        }

        public void AddToHistory(string hash)
        {
            if (!IsInHistory(hash)) {
                _history.Add(hash, hash[0]);
                UpdateHistory();
            }
        }

        public void AddToHistory(string hash, char isfamily)
        {
            if (!IsInHistory(hash)) {
                _history.Add(hash, isfamily);
            }
            else {
                _history[hash] = isfamily;
            }

            UpdateHistory();
        }

        public void RemoveFromHistory(string hash)
        {
            if (_history.Remove(hash)) {
                UpdateHistory();
            }
        }

        public KeyValuePair<string, string>[] FingerPrint;
        public string FingerPrintString { get; private set; }
        public void SetFingerPrint(string fingerprint)
        {
            FingerPrintString = fingerprint;
            FingerPrint = ExifHelper.StringtoFingerPrint(fingerprint);
            AppDatabase.ImgUpdateProperty(Hash, AppConsts.AttributeFingerPrint, fingerprint);
        }

        public Img(
            string hash,
            string folder,
            byte[] vector,
            DateTime lastview,
            RotateFlipType orientation,
            float distance,
            short match,
            DateTime lastcheck,
            string next,
            bool verified,
            string history,
            string family,
            DateTime datetaken,
            string fingerprint
            )
        {
            Hash = hash;
            Folder = folder;
            _vector = vector;
            Orientation = orientation;
            LastView = lastview;
            Distance = distance;
            Match = match;
            LastCheck = lastcheck;
            Next = next;
            Verified = verified;
            SetHistory(history, family);
            DateTaken = datetaken;
            FingerPrintString = fingerprint;
            FingerPrint = ExifHelper.StringtoFingerPrint(fingerprint);
        }
    }
}