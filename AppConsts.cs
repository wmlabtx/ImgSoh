namespace ImgSoh
{
    public static class AppConsts
    {
        public const string MzxExtension = "mzx";
        public const string DatExtension = "dat";

        private const string PathRoot = @"M:\Sp";
        public const string FileDatabase = PathRoot + @"\" + "spacer.mdf";
        public const string FileVit = PathRoot + @"\" + "model.onnx";
        public const string FileExifTool = PathRoot + @"\" + "exiftool.exe";
        public const string PathHp = @"M:\Hp";
        public const string PathGb = @"M:\Gb";
        public const string PathRw = @"M:\Rw";

        public const int MaxImportFiles = 1000;
        public const int MaxPixelDiff = 128;
        public const int MaxScope = 10000;

        public const char CharEllipsis = '\u2026';
        public const char CharRightArrow = '\u2192';

        public const int LockTimeout = 10000;
        public const double WindowMargin = 5.0;
        public const double TimeLapse = 500.0;
        public const int VectorLength = 1000;

        public const string TableImages = "images";
        public const string AttributeHash = "hash";
        public const string AttributePath = "path";
        public const string AttributeExt = "ext";
        public const string AttributeVector = "vector";
        public const string AttributeOrientation = "orientation";
        public const string AttributeLastView = "lastview";
        public const string AttributeNext = "next";
        public const string AttributeHorizon = "horizon";
        public const string AttributePrev = "prev";
        public const string AttributeLastCheck = "lastcheck";
        public const string AttributeVerified = "verified";
        public const string AttributeCounter = "counter";
        public const string AttributeTaken = "taken";
        public const string AttributeMeta = "meta";
    }
}