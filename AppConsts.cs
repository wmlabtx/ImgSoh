﻿namespace ImgSoh
{
    public static class AppConsts
    {
        private const string PathRoot = @"D:\Users\Murad\Spacer";
        private const string PathRootProtected = @"M:\Spacer";
        private const string FolderDb = "Db";
        public const string FileVit = PathRoot + @"\" + FolderDb + @"\" + "model.onnx";
        public const string FileDatabase = PathRoot + @"\" + FolderDb + @"\" + "images.mdf";
        private const string FolderRw = "Rw";
        public const string PathRwProtected = PathRootProtected + @"\" + FolderRw;
        private const string FolderHp = "Hp";
        public const string PathHp = PathRoot + @"\" + FolderHp;
        private const string FolderGb = "Gb";
        public const string PathGbProtected = PathRootProtected + @"\" + FolderGb;

        public const int MaxImportFiles = 10000;

        public const string MzxExtension = ".mzx";
        public const string DatExtension = ".dat";
        public const string CorruptedExtension = ".corrupted";

        public const char CharEllipsis = '\u2026';
        public const char CharRightArrow = '\u2192';

        public const int LockTimeout = 10000;
        public const double WindowMargin = 5.0;
        public const double TimeLapse = 500.0;
        public const int VectorLength = 1000;

        public const string TableImages = "Images";
        public const string AttributeHash = "Hash";
        public const string AttributeFolder = "Folder";
        public const string AttributeVector = "Vector";
        public const string AttributeOrientation = "Orientation";
        public const string AttributeLastView = "LastView";
        public const string AttributeNext = "Next";
        public const string AttributeLastCheck = "LastCheck";
        public const string AttributeVerified = "Verified";
        public const string AttributeFamily = "Family";
        public const string AttributeHistory = "History";
    }
}