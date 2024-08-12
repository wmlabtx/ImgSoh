using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ImgSoh
{
    public static class AppImgs
    {
        private static readonly object _lock = new object();
        private static readonly SortedList<string, float[]> _vectorList = new SortedList<string, float[]>();
        private static readonly SortedList<string, float> _magnitudeList = new SortedList<string, float>();
        private static readonly SortedList<string, string> _nameList = new SortedList<string, string>();

        public static void Clear()
        {
            lock (_lock) {
                _vectorList.Clear();
                _magnitudeList.Clear();
                _nameList.Clear();
            }
        }

        public static int Count()
        {
            int count;
            lock (_lock) {
                if (_vectorList.Count != _nameList.Count) {
                    throw new Exception();
                }

                count = _vectorList.Count;
            }

            return count;
        }

        private static bool ContainsKey(string key)
        {
            bool result;
            lock (_lock) {
                result = key.Length == 32 ? 
                    _vectorList.ContainsKey(key) : 
                    _nameList.ContainsKey(key);
            }

            return result;
        }

        public static bool TryGetImg(string hash, out Img img)
        {
            img = AppDatabase.GetImg(hash);
            return img != null;
        }

        public static bool TryGetImgByName(string name, out Img img)
        {
            img = null;
            string hash;
            lock (_lock) {
                if (!_nameList.TryGetValue(name, out hash)) {
                    return false;
                }
            }

            img = AppDatabase.GetImg(hash);
            return img != null;
        }

        public static bool TryGetVector(string hash, out float[] vector)
        {
            lock (_lock) {
                return _vectorList.TryGetValue(hash, out vector);
            }
        }

        public static void Add(string hash, string name, float[] vector, float magnitude)
        {
            lock (_lock) {
                _vectorList.Add(hash, vector);
                _magnitudeList.Add(hash, magnitude);
                _nameList.Add(name, hash);
            }
        }

        public static void Remove(string key)
        {
            lock (_lock) {
                if (key.Length == 32) {
                    if (_vectorList.ContainsKey(key)) {
                        string nameToRemove = null;
                        foreach (var kvp in _nameList) {
                            if (kvp.Value.Equals(key)) {
                                nameToRemove = kvp.Key;
                                break;
                            }
                        }

                        if (!string.IsNullOrEmpty(nameToRemove)) {
                            _nameList.Remove(nameToRemove);
                        }

                        _vectorList.Remove(key);
                        _magnitudeList.Remove(key);
                    }
                }
                else {
                    if (_nameList.ContainsKey(key)) {
                        _vectorList.Remove(_nameList[key]);
                        _magnitudeList.Remove(_nameList[key]);
                        _nameList.Remove(key);
                    }
                }
            }
        }

        /*
        private static bool IsValid(Img imgX)
        {
            lock (_lock) {
                if (imgX.Magnitude <=0f ||
                    string.IsNullOrWhiteSpace(imgX.Next) ||
                    (!string.IsNullOrWhiteSpace(imgX.Horizon) &&
                     !string.IsNullOrWhiteSpace(imgX.Next) &&
                     string.CompareOrdinal(imgX.Horizon, imgX.Next) >= 0)
                   ) {
                    return false;
                }

                var next = imgX.Next.Substring(4);
                if (!ContainsKey(next)) {
                    return false;
                }
            }

            return true;
        }
        */

        public static void Find(string hash, string horizon, out string radiusNext, out int counter)
        {
            lock (_lock) {
                var distances = new float[_vectorList.Count];
                var vx = _vectorList[hash];
                var mx = _magnitudeList[hash];
                Parallel.For(0, distances.Length, i => {
                        distances[i] = AppVit.GetDistance(vx, mx, _vectorList.Values[i], _magnitudeList.Values[i]);
                    });

                radiusNext = null;
                counter = 0;
                for (var i = 0; i < _vectorList.Keys.Count; i++) {
                    if (hash.Equals(_vectorList.Keys[i])) {
                        continue;
                    }

                    var radius = Helper.GetRadius(_vectorList.Keys[i], distances[i]);
                    if (string.IsNullOrEmpty(horizon) || (!string.IsNullOrEmpty(horizon) && string.CompareOrdinal(radius, horizon) > 0)) {
                        if (radiusNext == null || string.CompareOrdinal(radius, radiusNext) < 0) {
                            radiusNext = radius;
                        }
                    }

                    if (!string.IsNullOrEmpty(horizon) && string.CompareOrdinal(radius, horizon) <= 0) {
                        counter++;
                    }
                }
            }
        }

        public static string GetName(string hash)
        {
            string name;
            var length = 5;
            do {
                length++;
                name = hash.Substring(0, length);
            } while (ContainsKey(name));

            return name;
        }
    }
}
