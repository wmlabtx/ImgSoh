namespace ImgSoh
{
    public static class AppConsts
    {
        private const string PathRoot = @"D:\Users\Murad\Spacer";
        private const string PathRootProtected = @"M:\Spacer";
        private const string FolderDb = "Db";
        public const string FileVgg = PathRoot + @"\" + FolderDb +@"\" + "vgg16.onnx";
        public const string FileDatabase = PathRoot + @"\" + FolderDb + @"\" + "images.mdf";
        public const string FilePalette = PathRoot + @"\" + FolderDb + @"\" + "palette.png";
        private const string FolderRw = "Rw";
        public const string PathRw = PathRoot + @"\" + FolderRw;
        public const string PathRwProtected = PathRootProtected + @"\" + FolderRw;
        private const string FolderHp = "Hp";
        public const string PathHp = PathRoot + @"\" + FolderHp;
        private const string FolderGb = "Gb";
        public const string PathGbProtected = PathRootProtected + @"\" + FolderGb;
        private const string FolderDe = "De";
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
        public const string AttributePalette = "Palette";
        public const string TablePairs = "Pairs";
        public const string AttributeId1 = "Id1";
        public const string AttributeId2 = "Id2";
        public const string AttributeIsFamily = "IsFamily";
    }
}