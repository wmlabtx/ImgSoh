using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;

namespace ImgSoh
{
    public static partial class ImgMdf
    {
        private static int _added;
        private static int _bad;
        private static int _found;
        private static readonly List<string> _importedfamily = new List<string>();

        private static void AddImportedFamily(string hash)
        {
            foreach (var e in _importedfamily) {
                AppDatabase.AddPair(hash, e, true);
            }

            _importedfamily.Add(hash);
        }

        private static void ImportFile(string orgfilename, bool doImportFamily, BackgroundWorker backgroundworker)
        {
            var name = Path.GetFileNameWithoutExtension(orgfilename);
            var lastView = DateTime.Now.AddYears(-5);
            backgroundworker.ReportProgress(0, $"importing {name} (a:{_added})/f:{_found}/b:{_bad}){AppConsts.CharEllipsis}");

            var lastmodified = File.GetLastWriteTime(orgfilename);
            if (lastmodified > DateTime.Now) {
                lastmodified = DateTime.Now;
            }

            byte[] imagedata;
            var orgextension = Path.GetExtension(orgfilename);
            if (orgextension.Equals(AppConsts.DatExtension, StringComparison.OrdinalIgnoreCase) ||
                orgextension.Equals(AppConsts.MzxExtension, StringComparison.OrdinalIgnoreCase)) {
                imagedata = File.ReadAllBytes(orgfilename);
                var decrypteddata = orgextension.Equals(AppConsts.DatExtension, StringComparison.OrdinalIgnoreCase) ?
                    EncryptionHelper.DecryptDat(imagedata, name) :
                    EncryptionHelper.Decrypt(imagedata, name);

                if (decrypteddata != null) {
                     imagedata = decrypteddata;
                }
            }
            else {
                imagedata = File.ReadAllBytes(orgfilename);
            }

            var hash = FileHelper.GetHash(imagedata);
            var found = AppDatabase.TryGetImgFolder(hash, out var folderFound);
            if (found) {
                // we have a record with the same hash...
                var filenamefound = Helper.GetFileName(folderFound, hash);
                if (File.Exists(filenamefound)) {
                    // we have a file
                    var foundimagedata = FileHelper.ReadEncryptedFile(filenamefound);
                    var foundhash = FileHelper.GetHash(foundimagedata);
                    if (hash.Equals(foundhash)) {
                        // and file is okay
                        var foundlastmodified = File.GetLastWriteTime(orgfilename);
                        if (foundlastmodified > lastmodified) {
                            File.SetLastWriteTime(filenamefound, lastmodified);
                        }
                    }
                    else {
                        // but found file was changed or corrupted
                        FileHelper.WriteEncryptedFile(filenamefound, imagedata);
                        File.SetLastWriteTime(filenamefound, lastmodified);
                    }

                    AppDatabase.ImgUpdateProperty(hash, AppConsts.AttributeLastView, lastView);
                    DeleteFile(orgfilename);
                    if (doImportFamily) {
                        AddImportedFamily(hash);
                    }

                    _found++;
                    return;
                }

                // ...but file is missing
                AppDatabase.DeleteImg(hash);
            }

            byte[] vector;
            using (var magickImage = BitmapHelper.ImageDataToMagickImage(imagedata)) {
                if (magickImage == null) {
                    DeleteFile(orgfilename);
                    _bad++;
                    return;
                }

                var datetaken = BitmapHelper.GetDateTaken(magickImage, DateTime.Now);
                if (datetaken < lastmodified) {
                    lastmodified = datetaken;
                }

                using (var bitmap = BitmapHelper.MagickImageToBitmap(magickImage, RotateFlipType.RotateNoneFlipNone)) {
                    vector = VggHelper.CalculateVector(bitmap);
                }
            }

            var folder = Helper.GetFolder();
            var newfilename = Helper.GetFileName(folder, hash);
            if (!orgfilename.Equals(newfilename)) {
                FileHelper.WriteEncryptedFile(newfilename, imagedata);
                File.SetLastWriteTime(newfilename, lastmodified);
            }

            var vimagedata = FileHelper.ReadEncryptedFile(newfilename);
            if (vimagedata == null) {
                DeleteFile(newfilename);
                return;
            }

            var vhash = FileHelper.GetHash(vimagedata);
            if (!hash.Equals(vhash)) {
                DeleteFile(newfilename);
                return;
            }

            if (!orgfilename.Equals(newfilename)) {
                DeleteFile(orgfilename);
            }

            AppDatabase.AddImg(
                hash:hash, 
                folder:folder, 
                vector: vector,
                lastView: lastView,
                orientation: RotateFlipType.RotateNoneFlipNone
                );

            if (doImportFamily) {
                AddImportedFamily(hash);
            }

            _added++;
        }

        private static void ImportFiles(string path, BackgroundWorker backgroundworker)
        {
            var directoryInfo = new DirectoryInfo(path);
            var fs = directoryInfo.GetFiles("*.*", SearchOption.AllDirectories).ToArray();
            if (path.Equals(AppConsts.PathHp)) {
                var hashes = AppDatabase.GetHashes();
                var fsmissing = new List<FileInfo>();
                foreach (var e in fs) {
                    var name = Path.GetFileNameWithoutExtension(e.FullName);
                    if (!hashes.ContainsKey(name)) {
                        fsmissing.Add(e);
                    }
                }

                fs = fsmissing.ToArray();
            }

            var count = 0;
            foreach (var e in fs) {
                var orgfilename = e.FullName;
                if (!Path.GetExtension(orgfilename).Equals(AppConsts.CorruptedExtension, StringComparison.OrdinalIgnoreCase)) {
                    ImportFile(orgfilename, !path.Equals(AppConsts.PathHp), backgroundworker);
                    count++;
                    if (count == AppConsts.MaxImportFiles) {
                        break;
                    }
                }
            }

            backgroundworker.ReportProgress(0, $"clean-up {path}{AppConsts.CharEllipsis}");
            Helper.CleanupDirectories(path, AppVars.Progress);
        }

        public static void BackgroundWorker(BackgroundWorker backgroundworker)
        {
            Compute(backgroundworker);
        }

        private static void Compute(BackgroundWorker backgroundworker)
        {
            if (AppVars.ImportRequested) {
                _added = 0;
                _found = 0;
                _bad = 0;
                _importedfamily.Clear();
                ImportFiles(AppConsts.PathHp, backgroundworker);
                ImportFiles(AppConsts.PathRw, backgroundworker);
                ImportFiles(AppConsts.PathRwProtected, backgroundworker);
                AppVars.ImportRequested = false;
            }

            /*
            var hashX = AppDatabase.GetNextCheck();
            if (!string.IsNullOrEmpty(hashX)) {
                if (AppDatabase.TryGetImgCompute(hashX,
                        out var folderX,
                        out var vectorX,
                        out var lastViewX,
                        out var distanceX, 
                        out var nextX)) {

                    if (vectorX.Length != 4096) {
                        var filename = Helper.GetFileName(folderX, hashX);
                        var imagedata = FileHelper.ReadEncryptedFile(filename);
                        using (var magickImage = BitmapHelper.ImageDataToMagickImage(imagedata))
                        using (var bitmap = BitmapHelper.MagickImageToBitmap(magickImage, RotateFlipType.RotateNoneFlipNone)) {
                            vectorX = VggHelper.CalculateVector(bitmap);
                            AppDatabase.ImgUpdateProperty(hashX, AppConsts.AttributeVector, vectorX);
                        }
                    }
                }

                var pairs = AppDatabase.GetPairs(hashX);
                var vectors = AppDatabase.GetVectors();
                vectors.Remove(hashX);
                foreach (var e in pairs.Keys) {
                    vectors.Remove(e);
                }

                var mindistance = float.MaxValue;
                string hashY = null;
                foreach (var e in vectors) {
                    var distance = VggHelper.GetDistance(vectorX, e.Value);
                    if (distance < mindistance) {
                        mindistance = distance;
                        hashY = e.Key;
                    }
                }

                if (hashY != null) {
                    if (!nextX.Equals(hashY)) {
                        var age = Helper.TimeIntervalToString(DateTime.Now.Subtract(lastViewX));
                        var shortfilename = Helper.GetShortFileName(folderX, hashX);
                        backgroundworker.ReportProgress(0,
                            $"[{age} ago] {shortfilename}: {distanceX:F4} {AppConsts.CharRightArrow} {mindistance:F4}");
                        AppDatabase.ImgUpdateProperty(hashX, AppConsts.AttributeNext, hashY);
                    }

                    AppDatabase.ImgUpdateProperty(hashX, AppConsts.AttributeDistance, mindistance);
                }

                AppDatabase.ImgUpdateProperty(hashX, AppConsts.AttributeLastCheck, DateTime.Now);
            }
            */
        }
    }
}
