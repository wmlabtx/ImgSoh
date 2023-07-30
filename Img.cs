using System;
using System.Drawing;

namespace ImgSoh
{
    public class Img
    {
        public string Hash { get; }
        public string Folder { get; }

        public DateTime DateTaken { get; private set; }
        public void SetDateTaken(DateTime dateTaken)
        {
            DateTaken = dateTaken;
            AppDatabase.ImageUpdateProperty(Hash, AppConsts.AttributeDateTaken, DateTaken);
        }

        private byte[] _vector;
        public byte[] GetVector()
        {
            return _vector;
        }

        public void SetVector(byte[] vector)
        {
            _vector = vector;
            AppDatabase.ImageUpdateProperty(Hash, AppConsts.AttributeVector, _vector);
        }

        public DateTime LastView { get; private set; }
        public void SetLastView(DateTime lastview)
        {
            LastView = lastview;
            AppDatabase.ImageUpdateProperty(Hash, AppConsts.AttributeLastView, LastView);
        }

        public RotateFlipType Orientation { get; private set; }
        public void SetOrientation(RotateFlipType rft)
        {
            Orientation = rft;
            AppDatabase.ImageUpdateProperty(Hash, AppConsts.AttributeOrientation, Helper.RotateFlipTypeToByte(Orientation));
        }

        public float Distance { get; private set; }
        public void SetDistance(float distance)
        {
            Distance = distance;
            AppDatabase.ImageUpdateProperty(Hash, AppConsts.AttributeDistance, Distance);
        }

        public DateTime LastCheck { get; private set; }
        public void SetLastCheck(DateTime lastcheck)
        {
            LastCheck = lastcheck;
            AppDatabase.ImageUpdateProperty(Hash, AppConsts.AttributeLastCheck, LastCheck);
        }

        public short Review { get; private set; }
        public void SetReview(short review)
        {
            Review = review;
            AppDatabase.ImageUpdateProperty(Hash, AppConsts.AttributeReview, review);
        }

        public string Next { get; private set; }
        public void SetNext(string next)
        {
            Next = next;
            AppDatabase.ImageUpdateProperty(Hash, AppConsts.AttributeNext, next);
        }

        public string GetFileName()
        {
            return $"{AppConsts.PathHp}\\{Folder[0]}\\{Folder[1]}\\{Hash}{AppConsts.MzxExtension}";
        }

        public string GetShortFileName()
        {
            return $"{Folder}\\{Hash.Substring(0, 4)}.{Hash.Substring(4, 4)}.{Hash.Substring(8, 4)}";
        }

        public Img(
        string hash,
            string folder,
            DateTime datetaken,
            byte[] vector,
            DateTime lastview,
            RotateFlipType orientation,
            float distance,
            DateTime lastcheck,
            short review,
            string next
            )
        {
            Hash = hash;
            Folder = folder;
            DateTaken = datetaken;
            _vector = vector;
            Orientation = orientation;
            LastView = lastview;
            Distance = distance;
            LastCheck = lastcheck;
            Review = review;
            Next = next;
        }
    }
}