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

        private readonly SortedSet<string> _family;
        public int FamilyCount => _family.Count;
        public string Family => Helper.SortedSetToString(_family);
        public string[] FamilyArray => _family.ToArray();

        public bool IsInFamily(string hash)
        {
            return _family.Contains(hash);
        }

        public void AddToFamily(string hash)
        {
            if (_family.Add(hash)) {
                AppDatabase.ImgUpdateProperty(Hash, AppConsts.AttributeFamily, Helper.SortedSetToString(_family));
            }
        }

        public void RemoveFromFamily(string hash)
        {
            if (_family.Remove(hash)) {
                AppDatabase.ImgUpdateProperty(Hash, AppConsts.AttributeFamily, Helper.SortedSetToString(_family));
            }
        }

        private readonly SortedSet<string> _aliens;
        public int AliensCount => _aliens.Count;
        public string Aliens => Helper.SortedSetToString(_aliens);
        public string[] AliensArray => _aliens.ToArray();

        public bool IsInAliens(string hash)
        {
            return _aliens.Contains(hash);
        }

        public void AddToAliens(string hash)
        {
            if (_aliens.Add(hash)) {
                AppDatabase.ImgUpdateProperty(Hash, AppConsts.AttributeAliens, Helper.SortedSetToString(_aliens));
            }
        }

        public void RemoveFromAliens(string hash)
        {
            if (_aliens.Remove(hash)) {
                AppDatabase.ImgUpdateProperty(Hash, AppConsts.AttributeAliens, Helper.SortedSetToString(_aliens));
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
            SortedSet<string> family,
            SortedSet<string> aliens
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
            _family = family;
            _aliens = aliens;
        }
    }
}