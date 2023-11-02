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
            if (_history.Add(hash)) {
                AppDatabase.ImgUpdateProperty(Hash, AppConsts.AttributeHistory, History);
            }
        }

        public void RemoveFromHistory(string hash)
        {
            if (_history.Remove(hash)) {
                AppDatabase.ImgUpdateProperty(Hash, AppConsts.AttributeHistory, History);
            }
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
            string history
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
        }
    }
}