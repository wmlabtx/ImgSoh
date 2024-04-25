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
        public RotateFlipType Orientation { get; }
        public DateTime LastView { get; }
        public DateTime LastCheck { get; }
        public string Next { get; }
        public bool Verified { get; }
        public float Distance { get; }
        public int Family { get; }

        private readonly float[] _vector;
        public float[] GetVector()
        {
            return _vector;
        }

        private readonly SortedSet<string> _history;
        public string GetHistory()
        {
            return Helper.SortedSetToString(_history);
        }

        public string[] GetHistoryArray()
        {
            return _history.ToArray();
        }

        public bool IsInHistory(string hash)
        {
            return _history.Contains(hash);
        }

        public bool AddToHistory(string hash)
        {
            return _history.Add(hash);
        }

        public bool RemoveFromHistory(string hash)
        {
            return _history.Remove(hash);
        }

        public Img(
            string hash,
            string folder,
            float[] vector,
            DateTime lastview,
            RotateFlipType orientation,
            DateTime lastcheck,
            string next,
            bool verified,
            float distance,
            string history,
            int family
            )
        {
            Hash = hash;
            Folder = folder;
            _vector = vector;
            Orientation = orientation;
            LastView = lastview;
            LastCheck = lastcheck;
            Next = next;
            Verified = verified;
            Distance = distance;
            _history = Helper.StringToSortedSet(history);
            Family = family;
        }
    }
}