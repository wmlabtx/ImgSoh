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
                }
            }

            if (vector == null) {
                DeleteFile(orgfilename, AppConsts.CorruptedExtension);
                _bad++;
                return;
            }

            var fingerprint = ExifHelper.GetFingerPrint(name, imagedata);
            if (fingerprint.Length == 0) {
                DeleteFile(orgfilename, AppConsts.CorruptedExtension);
                _bad++;
                return;
            }

            var fingerprintstring = ExifHelper.FingerprintToString(fingerprint);

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
                fingerprint: fingerprintstring,
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

                if (imgX.GetVector().Length != 4096 || imgX.FingerPrint.Length == 0 || imgX.DateTaken.Year == 1900) {
                    using (var magickImage = BitmapHelper.ImageDataToMagickImage(imagedata)) {
                        if (magickImage == null) {
                            Delete(hashX, AppConsts.CorruptedExtension, null);
                            return;
                        }

                        using (var bitmap =
                               BitmapHelper.MagickImageToBitmap(magickImage, RotateFlipType.RotateNoneFlipNone)) {
                            var vector = VggHelper.CalculateVector(bitmap);
                            imgX.SetVector(vector);
                            var lastmodified = File.GetLastWriteTime(filename);
                            var datetaken = BitmapHelper.GetDateTaken(magickImage, lastmodified);
                            imgX.SetDateTaken(datetaken);
                        }
                    }

                    var name = Path.GetFileNameWithoutExtension(filename);
                    var fingerprint = ExifHelper.GetFingerPrint(name, imagedata);
                    if (fingerprint.Length == 0) {
                        Delete(hashX, AppConsts.CorruptedExtension, null);
                        return;
                    }

                    var fingerprintstring = ExifHelper.FingerprintToString(fingerprint);
                    imgX.SetFingerPrint(fingerprintstring);
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

                const float VggSim = 0.2f;
                string hashVgg = null;
                float distanceVgg = 1f;
                int nameVgg = 0;
                int valueVgg = 0;
                foreach (var e in candidates) {
                    var distance = VggHelper.GetDistance(imgX.GetVector(), e.Value.GetVector());
                    ExifHelper.GetMatch(imgX.FingerPrint, e.Value.FingerPrint, out int name, out int value);
                    if (hashVgg == null) {
                        distanceVgg = distance;
                        hashVgg = e.Key;
                        nameVgg = name;
                        valueVgg = value;
                        continue;
                    }

                    if (distance < VggSim) {
                        if (distance < distanceVgg) {
                            distanceVgg = distance;
                            hashVgg = e.Key;
                            nameVgg = name;
                            valueVgg = value;
                        }

                        continue;
                    }

                    if (distanceVgg < VggSim) {
                        continue;
                    }

                    if (value > valueVgg) {
                        distanceVgg = distance;
                        hashVgg = e.Key;
                        nameVgg = name;
                        valueVgg = value;
                        continue;
                    }

                    if (value < valueVgg) {
                        continue;
                    }

                    if (name > nameVgg) {
                        distanceVgg = distance;
                        hashVgg = e.Key;
                        nameVgg = name;
                        valueVgg = value;
                        continue;
                    }

                    if (name < nameVgg) {
                        continue;
                    }

                    if (distance < distanceVgg) {
                        distanceVgg = distance;
                        hashVgg = e.Key;
                        nameVgg = name;
                        valueVgg = value;
                    }
                }

                if (!string.IsNullOrEmpty(hashVgg)) {
                    if (!imgX.Next.Equals(hashVgg) || Math.Abs(imgX.Distance - distanceVgg) > 0.0001f) {
                        var age = Helper.TimeIntervalToString(DateTime.Now.Subtract(imgX.LastView));
                        var shortfilename = Helper.GetShortFileName(imgX.Folder, imgX.Hash);
                        backgroundworker.ReportProgress(0, $"[{age} ago] {shortfilename} {imgX.Distance:F4} {AppConsts.CharRightArrow} {distanceVgg:F4}");
                        imgX.SetDistance(distanceVgg);
                        imgX.SetNext(hashVgg);
                    }
                }

                imgX.SetLastCheck(DateTime.Now);
            }
        }
    }
}
