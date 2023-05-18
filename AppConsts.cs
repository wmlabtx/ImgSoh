namespace ImgSoh
{
    public static class AppConsts
    {
        public const string PathRoot = @"D:\Users\Murad\Spacer";
        public const string PathRootProtected = @"M:\Spacer";
        public const string FolderDb = "Db";
        public const string FileVgg = PathRoot + @"\" + FolderDb +@"\" + "resnet152-v2-7.onnx";
        public const string FileDatabase = PathRoot + @"\" + FolderDb + @"\" + "images.mdf";
        public const string FilePalette = PathRoot + @"\" + FolderDb + @"\" + "palette.png";
        public const string FolderRw = "Rw";
        public const string PathRw = PathRoot + @"\" + FolderRw;
        public const string PathRwProtected = PathRootProtected + @"\" + FolderRw;
        public const string FolderHp = "Hp";
        public const string PathHp = PathRoot + @"\" + FolderHp;
        public const string FolderGb = "Gb";
        public const string PathGbProtected = PathRootProtected + @"\" + FolderGb;
        public const string FolderDe = "De";
        public const string PathDeProtected = PathRootProtected + @"\" + FolderDe;

        public const int MaxImportFiles = 100000;

        public const string MzxExtension = ".mzx";
        public const string DatExtension = ".dat";
        public const string CorruptedExtension = ".corrupted";

        public const char CharEllipsis = '\u2026';
        public const char CharRightArrow = '\u2192';

        public const int LockTimeout = 10000;
        public const double WindowMargin = 5.0;
        public const double TimeLapse = 500.0;

        public const string TableImages = "Images";
        public const string AttributeHash = "Hash";
        public const string AttributeFolder = "Folder";
        public const string AttributeDateTaken = "DateTaken";
        public const string AttributeLastView = "LastView";
        public const string AttributeVector = "Vector";
        public const string AttributeOrientation = "Orientation";
        public const string AttributeDistance = "Distance";
        public const string AttributeLastCheck = "LastCheck";
        public const string AttributeReview = "Review";
        public const string AttributeNext = "Next";
        public const string TableVars = "Vars";
        public const string AttributeDateTakenLast = "DateTakenLast";
    }
}