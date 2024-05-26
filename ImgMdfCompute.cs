using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;

namespace ImgSoh
{
    public static partial class ImgMdf
    {
        private static int _delay = 0;

        private static int _added;
        private static int _bad;
        private static int _found;

        private static bool ImportFile(string orgfilename, BackgroundWorker backgroundworker)
        {
            var name = Path.GetFileNameWithoutExtension(orgfilename);
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
                    File.Delete(orgfilename);
                    _found++;
                    return true;
                }

                // ...but file is missing
                AppDatabase.SetDeleted(hash);
            }

            var index = AppDatabase.GetAvailableIndex(hash);
            if (index <= 0) {
                throw new Exception("wrong index");
            }

            if (index >= AppConsts.MaxImages) {
                return false;
            }

            float[] vector;
            ExifInfo exifinfo;
            using (var magickImage = BitmapHelper.ImageDataToMagickImage(imagedata)) {
                if (magickImage == null) {
                    DeleteFile(orgfilename, AppConsts.CorruptedExtension);
                    _bad++;
                    return true;
                }

                var ext = magickImage.Format.ToString().ToLower();
                var tempfilename = $"{AppConsts.PathGbProtected}\\temp.{ext}";
                File.WriteAllBytes(tempfilename, imagedata);
                exifinfo = new ExifInfo(tempfilename);
                File.Delete(tempfilename);

                using (var bitmap = BitmapHelper.MagickImageToBitmap(magickImage, RotateFlipType.RotateNoneFlipNone)) {
                    vector = VitHelper.CalculateFloatVector(bitmap);
                }
            }

            if (vector == null) {
                DeleteFile(orgfilename, AppConsts.CorruptedExtension);
                _bad++;
                return true;
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
                return true;
            }

            var vhash = FileHelper.GetHash(vimagedata);
            if (!hash.Equals(vhash)) {
                File.Delete(newfilename);
                return true;
            }

            if (!orgfilename.Equals(newfilename)) {
                File.Delete(orgfilename);
            }

            var lastview = AppDatabase.GetLastView();
            var imgnew = new Img(
                index: index,
                deleted: false,
                hash: hash,
                folder: folder,
                vector: vector,
                orientation: RotateFlipType.RotateNoneFlipNone,
                lastview: lastview,
                next: string.Empty,
                lastcheck: DateTime.Now,
                verified: false,
                prev: string.Empty,
                horizon: string.Empty,
                counter: 0,
                taken: exifinfo.Taken,
                meta: exifinfo.Items.Length
            );

            AppDatabase.AddImg(imgnew);
            _added++;

            return true;
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

            foreach (var e in fs) {
                var orgfilename = e.FullName;
                if (!Path.GetExtension(orgfilename).Equals(AppConsts.CorruptedExtension, StringComparison.OrdinalIgnoreCase)) {
                    if (!ImportFile(orgfilename, backgroundworker)) {
                        break;
                    }

                    if (_added >= AppConsts.MaxImportFiles) {
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
            if (hashX != null) {
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

                    var candidates = AppDatabase.GetCandidates(hashX);
                    string radiusNext = null;
                    string radiusPrev = null;
                    var lastviewPrev = DateTime.MaxValue;
                    var counter = 0;
                    var vectorX = imgX.GetVector();
                    foreach (var candidate in candidates) { 
                        var vectorY = candidate.GetVector();
                        var distance = VitHelper.GetDistance(vectorX, vectorY);
                        var radius = Helper.GetRadius(candidate.Hash, distance);

                        if (string.IsNullOrEmpty(imgX.Horizon) || (!string.IsNullOrEmpty(imgX.Horizon) &&
                                                                   string.CompareOrdinal(radius, imgX.Horizon) > 0)) {
                            if (radiusNext == null || string.CompareOrdinal(radius, radiusNext) < 0) {
                                radiusNext = radius;
                            }
                        }

                        if (!string.IsNullOrEmpty(imgX.Horizon) && string.CompareOrdinal(radius, imgX.Horizon) <= 0) {
                            counter++;
                            if (radiusPrev == null || candidate.LastView < lastviewPrev) {
                                radiusPrev = radius;
                                lastviewPrev = candidate.LastView;
                            }
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(radiusPrev) && !imgX.Prev.Equals(radiusPrev)) {
                        AppDatabase.SetPrev(hashX, radiusPrev);
                    }

                    if (!string.IsNullOrWhiteSpace(radiusNext) && !imgX.Next.Equals(radiusNext)) {
                        var age = Helper.TimeIntervalToString(DateTime.Now.Subtract(imgX.LastCheck));
                        var shortfilename = Helper.GetShortFileName(imgX.Folder, imgX.Hash);
                        var radiusLast = string.IsNullOrWhiteSpace(imgX.Next) ? "----" : imgX.Next.Substring(0, 4);
                        backgroundworker.ReportProgress(0,
                            $"[{age} ago] {shortfilename} ({imgX.Counter}) {radiusLast} {AppConsts.CharRightArrow} ({counter}) {radiusNext.Substring(0, 4)}");
                        AppDatabase.SetNext(hashX, radiusNext);
                        _delay = 0;
                    }

                    if (counter != imgX.Counter) {
                        AppDatabase.SetCounter(hashX, counter);
                    }

                    if (counter > 100) {
                        AppDatabase.SetPrev(hashX, string.Empty);
                        AppDatabase.SetNext(hashX, string.Empty);
                        AppDatabase.SetHorizon(hashX);
                        AppDatabase.SetCounter(hashX, 0);
                    }

                    AppDatabase.SetLastCheck(hashX);
                }
            }

            _delay = Math.Min(2500, _delay + 10);
            Thread.Sleep(_delay);
        }
    }
}
