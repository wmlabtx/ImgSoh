namespace ImgSoh
{
    public static class AppConsts
    {
        public const string MzxExtension = ".mzx";
        public const string DatExtension = ".dat";
        public const string CorruptedExtension = ".corrupted";

        public const string PathRoot = @"D:\Users\Murad\Spacer";
        public const string PathHp = @"D:\Users\Murad\Diablo IV\Data\chunks";
        private const string PathRootProtected = @"M:\Spacer";
        public const string FileVit = PathRoot + @"\" + "model.onnx";
        private const string FolderRw = "Rw";
        public const string PathRwProtected = PathRootProtected + @"\" + FolderRw;
        private const string FolderGb = "Gb";
        public const string PathGbProtected = PathRootProtected + @"\" + FolderGb;
        public const string FileExifTool = PathRoot + @"\" + "exiftool.exe";

        public const int MaxImages = 10000;
        public const int MaxImportFiles = 10000;

        public const char CharEllipsis = '\u2026';
        public const char CharRightArrow = '\u2192';

        public const int LockTimeout = 10000;
        public const double WindowMargin = 5.0;
        public const double TimeLapse = 500.0;
        public const int VectorLength = 1000;

        public const string AttributeDeleted = "deleted";
        public const string AttributeHash = "hash";
        public const string AttributeFolder = "folder";
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