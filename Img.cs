 using System;
 using System.Collections.Generic;
 using System.Drawing;
 using System.Linq;

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

        private List<Tuple<string, float>> _distances;
        private readonly SortedSet<string> _history;
        public int HistoryCount => _history.Count;
        public string[] HistoryArray => _history.ToArray();
        public string History => Helper.SortedSetToString(_history);

        public bool IsInHistory(string hash)
        {
            return _history.Contains(hash);
        }

        public void AddToHistory(string hash)
        {
            if (HistoryCount < AppConsts.MaxHistorySize) {
                if (_history.Add(hash)) {
                    AppDatabase.ImgUpdateProperty(Hash, AppConsts.AttributeHistory, History);
                }
            }
            else {
                if (AppDatabase.TryGetImg(hash, out var imgY)) {
                    var distanceY = VggHelper.GetDistance(_vector, imgY.GetVector());
                    if (_distances == null) {
                        _distances = new List<Tuple<string, float>>();
                        foreach (var hashH in _history) {
                            if (AppDatabase.TryGetImg(hashH, out var imgH)) {
                                var distanceH = VggHelper.GetDistance(_vector, imgH.GetVector());
                                _distances.Add(Tuple.Create(hashH, distanceH));
                            }
                        }
                    }

                    var maxdistance = _distances.Max(e => e.Item2);
                    if (distanceY < maxdistance) {
                        _distances.Add(Tuple.Create(hash, distanceY));
                        _distances = _distances.OrderBy(e => e.Item2).Take(AppConsts.MaxHistorySize).ToList();
                        _history.Clear();
                        foreach (var e in _distances) {
                            _history.Add(e.Item1);
                        }

                        AppDatabase.ImgUpdateProperty(Hash, AppConsts.AttributeHistory, History);
                    }
                }
            }
        }

        public void RemoveFromHistory(string hash)
        {
            if (_history.Remove(hash)) {
                AppDatabase.ImgUpdateProperty(Hash, AppConsts.AttributeHistory, History);
            }
        }

        public SortedList<string, string> FingerPrint;
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
            DateTime lastcheck,
            string next,
            bool verified,
            string history,
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
            LastCheck = lastcheck;
            Next = next;
            Verified = verified;
            _history = Helper.StringToSortedSet(history);
            _distances = null;
            DateTaken = datetaken;
            FingerPrintString = fingerprint;
            FingerPrint = ExifHelper.StringtoFingerPrint(fingerprint);
        }
    }
}