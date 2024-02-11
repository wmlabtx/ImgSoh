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
            using (var magickImage = BitmapHelper.ImageDataToMagickImage(imagedata)) {
                if (magickImage == null) {
                    DeleteFile(orgfilename, AppConsts.CorruptedExtension);
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
                match: 0,
                lastcheck: lastView,
                verified: false,
                history: string.Empty,
                family: 0
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

                if (imgX.GetVector().Length != 4096 || imgX.FingerPrint.Length == 0) {
                    using (var magickImage = BitmapHelper.ImageDataToMagickImage(imagedata)) {
                        if (magickImage == null) {
                            Delete(hashX, AppConsts.CorruptedExtension, null);
                            return;
                        }

                        using (var bitmap =
                               BitmapHelper.MagickImageToBitmap(magickImage, RotateFlipType.RotateNoneFlipNone)) {
                            var vector = VggHelper.CalculateVector(bitmap);
                            imgX.SetVector(vector);
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

                if (imgX.Family > 0) {
                    var history = imgX.HistoryArray;
                    foreach (var hash in history) {
                        if (AppDatabase.TryGetImg(hash, out var imgH)) {
                            if (imgX.Family == imgH.Family) {
                                imgX.RemoveFromHistory(hash);
                            }
                        }
                        else {
                            imgX.RemoveFromHistory(hash);
                        }
                    }
                }

                var candidates = AppDatabase.GetCandidates();
                candidates.Remove(hashX);
                var minus = imgX.HistoryArray.ToList();
                if (imgX.Family > 0) {
                    var array = AppDatabase.GetFamily(imgX.Family);
                    minus.AddRange(array);
                }

                foreach (var hash in minus) {
                    if (candidates.ContainsKey(hash)) {
                        candidates.Remove(hash);
                    }
                }

                const float VggSim = 0.2f;
                string hashVgg = null;
                var distanceVgg = 1f;
                short matchVgg = 0;
                foreach (var e in candidates) {
                    var distance = VggHelper.GetDistance(imgX.GetVector(), e.Value.GetVector());
                    var match = ExifHelper.GetMatch(imgX.FingerPrint, e.Value.FingerPrint);
                    if (hashVgg == null) {
                        distanceVgg = distance;
                        hashVgg = e.Key;
                        matchVgg = match;
                        continue;
                    }

                    if (distance < VggSim) {
                        if (distance < distanceVgg) {
                            distanceVgg = distance;
                            hashVgg = e.Key;
                            matchVgg = match;
                        }

                        continue;
                    }

                    if (distanceVgg < VggSim) {
                        continue;
                    }

                    if (match > matchVgg) {
                        distanceVgg = distance;
                        hashVgg = e.Key;
                        matchVgg = match;
                        continue;
                    }

                    if (match < matchVgg) {
                        continue;
                    }

                    if (distance < distanceVgg) {
                        distanceVgg = distance;
                        hashVgg = e.Key;
                        matchVgg = match;
                    }
                }

                var classesFirst = new[] { new List<string>(), new List<string>(), new List<string>() };
                // 0 - next
                // 1 - history
                // 2 - family

                if (!string.IsNullOrEmpty(hashVgg)) {
                    classesFirst[0].Add(hashVgg);
                }

                classesFirst[1].AddRange(imgX.HistoryArray);
                if (imgX.Family > 0) {
                    classesFirst[2].AddRange(AppDatabase.GetFamily(imgX.Family).Where(e => !e.Equals(imgX.Hash)));
                }

                var classid = -1;
                while (classid < 0) {
                    classid = AppVars.RandomNext(3);
                    if (classesFirst[classid].Count == 0) {
                        classid = -1;
                    }
                }

                var rindex = AppVars.RandomNext(classesFirst[classid].Count);
                var hashY = classesFirst[classid][rindex];
                if (AppDatabase.TryGetImg(hashY, out var imgY)) {
                    distanceVgg = VggHelper.GetDistance(imgX.GetVector(), imgY.GetVector());
                    matchVgg = ExifHelper.GetMatch(imgX.FingerPrint, imgY.FingerPrint);
                    if (!imgX.Next.Equals(hashY) || Math.Abs(imgX.Distance - distanceVgg) > 0.0001f || imgX.Match != matchVgg) {
                        var age = Helper.TimeIntervalToString(DateTime.Now.Subtract(imgX.LastView));
                        var shortfilename = Helper.GetShortFileName(imgX.Folder, imgX.Hash);
                        backgroundworker.ReportProgress(0, $"[{age} ago] {shortfilename} [{imgX.Match}]{imgX.Distance:F4} {AppConsts.CharRightArrow} [{matchVgg}]{distanceVgg:F4}");
                        imgX.SetNext(hashY);
                        imgX.SetDistance(distanceVgg);
                        imgX.SetMatch(matchVgg);
                    }
                }

                imgX.SetLastCheck(DateTime.Now);
            }
        }
    }
}
