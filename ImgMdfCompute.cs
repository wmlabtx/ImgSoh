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

        private static void ImportFile(string orgfilename, BackgroundWorker backgroundworker)
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
            var found = AppDatabase.TryGetImg(hash, out var imgFound);
            if (found) {
                var folderFound = imgFound.Folder;

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

                    //imgFound.SetLastView(lastView);
                    DeleteFile(orgfilename, string.Empty);
                    _found++;
                    return;
                }

                // ...but file is missing
                AppDatabase.ImgDelete(hash);
            }

            byte[] vector;
            byte[] colorvector;
            DateTime datetaken;
            using (var magickImage = BitmapHelper.ImageDataToMagickImage(imagedata)) {
                if (magickImage == null) {
                    DeleteFile(orgfilename, AppConsts.CorruptedExtension);
                    _bad++;
                    return;
                }

                datetaken = BitmapHelper.GetDateTaken(magickImage, DateTime.Now);
                if (datetaken < lastmodified) {
                    lastmodified = datetaken;
                }

                using (var bitmap = BitmapHelper.MagickImageToBitmap(magickImage, RotateFlipType.RotateNoneFlipNone)) {
                    vector = VggHelper.CalculateVector(bitmap);
                    colorvector = ColorHelper.CalculateVector(bitmap);
                }
            }

            if (vector == null || colorvector == null) {
                DeleteFile(orgfilename, AppConsts.CorruptedExtension);
                _bad++;
                return;
            }

            var folder = Helper.GetFolder();
            var newfilename = Helper.GetFileName(folder, hash);
            if (!orgfilename.Equals(newfilename)) {
                FileHelper.WriteEncryptedFile(newfilename, imagedata);
                File.SetLastWriteTime(newfilename, lastmodified);
            }

            var vimagedata = FileHelper.ReadEncryptedFile(newfilename);
            if (vimagedata == null) {
                DeleteFile(newfilename, AppConsts.CorruptedExtension);
                return;
            }

            var vhash = FileHelper.GetHash(vimagedata);
            if (!hash.Equals(vhash)) {
                DeleteFile(newfilename, string.Empty);
                return;
            }

            if (!orgfilename.Equals(newfilename)) {
                DeleteFile(orgfilename, string.Empty);
            }

            var imgnew = new Img(
                hash: hash,
                folder: folder,
                vector: vector,
                colorvector: colorvector,
                orientation: RotateFlipType.RotateNoneFlipNone,
                lastview: lastView,
                next: hash,
                distance: 1f,
                lastcheck: lastView,
                verified: false,
                history: string.Empty,
                datetaken: datetaken
            );

            AppDatabase.AddImg(imgnew);
            _added++;
        }

        private static void ImportFiles(string path, SearchOption so, BackgroundWorker backgroundworker)
        {
            var directoryInfo = new DirectoryInfo(path);
            var fs = directoryInfo.GetFiles("*.*", so).ToArray();
            if (path.Equals(AppConsts.PathHp)) {
                var fsmissing = new List<FileInfo>();
                foreach (var e in fs) {
                    var name = Path.GetFileNameWithoutExtension(e.FullName);
                    if (!AppDatabase.TryGetImg(name, out _)) {
                        fsmissing.Add(e);
                    }
                }

                fs = fsmissing.ToArray();
            }

            var count = 0;
            foreach (var e in fs) {
                var orgfilename = e.FullName;
                if (!Path.GetExtension(orgfilename).Equals(AppConsts.CorruptedExtension, StringComparison.OrdinalIgnoreCase)) {
                    ImportFile(orgfilename, backgroundworker);
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
                ImportFiles(AppConsts.PathHp, SearchOption.AllDirectories, backgroundworker);
                ImportFiles(AppConsts.PathRwProtected, SearchOption.TopDirectoryOnly, backgroundworker);
                var directoryInfo = new DirectoryInfo(AppConsts.PathRwProtected);
                var ds = directoryInfo.GetDirectories("*.*", SearchOption.TopDirectoryOnly).ToArray();
                foreach (var di in ds) {
                    ImportFiles(di.FullName, SearchOption.AllDirectories, backgroundworker);
                }

                Helper.CleanupDirectories(AppConsts.PathRwProtected, AppVars.Progress);
                AppVars.ImportRequested = false;
            }

            var hashX = AppDatabase.GetNextCheck();
            if (AppDatabase.TryGetImg(hashX, out var imgX)) {
                var filename = Helper.GetFileName(imgX.Folder, imgX.Hash);
                var imagedata = FileHelper.ReadEncryptedFile(filename);
                if (imagedata == null) {
                    Delete(hashX, AppConsts.CorruptedExtension, null);
                    return;
                }

                var hashT = FileHelper.GetHash(imagedata);
                if (!hashT.Equals(hashX)) {
                    Delete(hashX, AppConsts.CorruptedExtension, null);
                    return;
                }

                if (imgX.GetVector().Length != 4096 || imgX.GetColorVector().Length != 200 || imgX.DateTaken.Year == 1900) {
                    using (var magickImage = BitmapHelper.ImageDataToMagickImage(imagedata)) {
                        if (magickImage == null) {
                            Delete(hashX, AppConsts.CorruptedExtension, null);
                            return;
                        }

                        using (var bitmap =
                               BitmapHelper.MagickImageToBitmap(magickImage, RotateFlipType.RotateNoneFlipNone)) {
                            var vector = VggHelper.CalculateVector(bitmap);
                            imgX.SetVector(vector);
                            var colorvector = ColorHelper.CalculateVector(bitmap);
                            imgX.SetColorVector(colorvector);
                            var lastmodified = File.GetLastWriteTime(filename);
                            var datetaken = BitmapHelper.GetDateTaken(magickImage, lastmodified);
                            imgX.SetDateTaken(datetaken);
                        }
                    }
                }

                var candidates = AppDatabase.GetCandidates();
                candidates.Remove(hashX);
                foreach (var hash in imgX.HistoryArray) {
                    if (candidates.ContainsKey(hash)) {
                        candidates.Remove(hash);
                    }
                    else {
                        imgX.RemoveFromHistory(hash);
                    }
                }

                string hashVgg = null;
                float distanceVgg = 1f;
                string hashColor = null;
                float distanceColor = 1f;

                foreach (var e in candidates) {
                    var distance = VggHelper.GetDistance(imgX.GetVector(), e.Value.GetVector());
                    if (distance < distanceVgg) {
                        distanceVgg = distance;
                        hashVgg = e.Key;
                    }

                    distance = ColorHelper.GetDistance(imgX.GetColorVector(), e.Value.GetColorVector());
                    if (distance < distanceColor) {
                        distanceColor = distance;
                        hashColor = e.Key;
                    }
                }

                string hashY;
                float mindistance;
                if (distanceVgg < distanceColor) {
                    hashY = hashVgg;
                    mindistance = distanceVgg;
                }
                else {
                    hashY = hashColor;
                    mindistance = distanceColor;
                }

                if (!string.IsNullOrEmpty(hashY)) {
                    if (!imgX.Next.Equals(hashY)) {
                        var age = Helper.TimeIntervalToString(DateTime.Now.Subtract(imgX.LastView));
                        var shortfilename = Helper.GetShortFileName(imgX.Folder, imgX.Hash);
                        backgroundworker.ReportProgress(0, $"[{age} ago] {shortfilename} {imgX.Distance:F4} {AppConsts.CharRightArrow} {mindistance:F4}");
                        imgX.SetDistance(mindistance);
                        imgX.SetNext(hashY);
                    }
                }

                imgX.SetLastCheck(DateTime.Now);
            }
        }
    }
}
