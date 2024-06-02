using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;

namespace ImgSoh
{
    public static partial class ImgMdf
    {
        private static int _delay;

        private static int _added;
        private static int _bad;
        private static int _found;
        private static int _moved;

        private static bool ImportFile(string orgfilename, BackgroundWorker backgroundworker)
        {
            var name = Path.GetFileNameWithoutExtension(orgfilename);
            if (name.Length == 12) {
                if (AppDatabase.TryGetImg(name, out var imgE)) {
                    var filenameF = Helper.GetFileName(imgE.Path, imgE.Hash, imgE.Ext);
                    if (orgfilename.Equals(filenameF)) {
                        // existing file
                        return true;
                    }
                }
            }

            var istrusted = false;
            var orgpath = Path.GetDirectoryName(orgfilename);
            var orgext = Path.GetExtension(orgfilename);
            if (orgext.StartsWith(".")) {
                orgext = orgext.Substring(1);
            }

            if (orgpath != null && orgpath.StartsWith(AppConsts.PathHp) && orgpath.Length > AppConsts.PathHp.Length) {
                orgpath = orgpath.Substring(AppConsts.PathHp.Length + 1);
                if (!orgpath.StartsWith("-")) {
                    istrusted = true;
                }
            }

            backgroundworker.ReportProgress(0, $"importing {name} (a:{_added})/f:{_found}/m:{_moved}/b:{_bad}){AppConsts.CharEllipsis}");
            var lastmodified = File.GetLastWriteTime(orgfilename);
            if (lastmodified > DateTime.Now) {
                lastmodified = DateTime.Now;
            }
             
            byte[] imagedata;
            if (orgext.Equals(AppConsts.DatExtension, StringComparison.OrdinalIgnoreCase) ||
                orgext.Equals(AppConsts.MzxExtension, StringComparison.OrdinalIgnoreCase)) {
                imagedata = File.ReadAllBytes(orgfilename);
                var decrypteddata = orgext.Equals(AppConsts.DatExtension, StringComparison.OrdinalIgnoreCase) ?
                    EncryptionHelper.DecryptDat(imagedata, name) :
                    EncryptionHelper.Decrypt(imagedata, name);

                if (decrypteddata != null) {
                    imagedata = decrypteddata;
                    orgext = "tmp";
                    var tmporgfilename = orgfilename + "." + orgext;
                    File.WriteAllBytes(tmporgfilename, imagedata);
                    File.Delete(orgfilename);
                    orgfilename = tmporgfilename;
                }
            }
            else {
                imagedata = File.ReadAllBytes(orgfilename);
            }

            var hash = FileHelper.GetHash(imagedata);
            var found = AppDatabase.TryGetImg(hash, out var imgF);
            if (found && !imgF.Deleted) {
                var filenameF = Helper.GetFileName(imgF.Path, hash, imgF.Ext);
                if (File.Exists(filenameF)) {
                    // we have a file
                    var imagedataF = FileHelper.ReadFile(filenameF);
                    var foundhash = FileHelper.GetHash(imagedataF);
                    if (hash.Equals(foundhash)) {
                        // and file is okay
                        if (!istrusted) {
                            // we have trusted image
                            File.Delete(orgfilename);
                            _found++;
                            return true;
                        }

                        // delete unstrusted image and work with new trusted one
                        Delete(hash);
                    }
                    else {
                        // but found file was changed or corrupted
                        Delete(hash);
                    }
                }
                else {
                    // ...but file is missing
                    if (istrusted) {
                        // set new trusted location
                        AppDatabase.SetPath(hash, orgpath);
                        AppDatabase.SetExt(hash, orgext);
                        File.Delete(filenameF);
                        _moved++;
                        return true;
                    }

                    // delete record with missing file
                    Delete(hash);
                }
            }

            var index = AppDatabase.GetAvailableIndex(hash);
            if (index <= 0) {
                throw new Exception("wrong index");
            }

            if (index >= AppConsts.MaxImages) {
                return false;
            }

            var path = istrusted ? orgpath : Helper.GetRandomPath();

            float[] vector;
            ExifInfo exifinfo;
            string ext;
            using (var magickImage = BitmapHelper.ImageDataToMagickImage(imagedata)) {
                if (magickImage == null) {
                    DeleteFile(orgfilename);
                    _bad++;
                    return true;
                }

                ext = magickImage.Format.ToString().ToLower();
                exifinfo = new ExifInfo(orgfilename);
                using (var bitmap = BitmapHelper.MagickImageToBitmap(magickImage, RotateFlipType.RotateNoneFlipNone)) {
                    vector = VitHelper.CalculateFloatVector(bitmap);
                }
            }

            if (vector == null) {
                DeleteFile(orgfilename);
                _bad++;
                return true;
            }

            var newfilename = Helper.GetFileName(path, hash, ext);
            if (!orgfilename.Equals(newfilename)) {
                FileHelper.WriteFile(newfilename, imagedata);
                File.SetLastWriteTime(newfilename, lastmodified);
                File.Delete(orgfilename);
            }

            var lastview = AppDatabase.GetLastView();
            var imgnew = new Img(
                index: index,
                deleted: false,
                hash: hash,
                path: path,
                ext: ext,
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
            foreach (var e in fs) {
                var orgfilename = e.FullName;
                if (!ImportFile(orgfilename, backgroundworker)) {
                    break;
                }

                if (_added >= AppConsts.MaxImportFiles) {
                    break;
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
                ImportFiles(AppConsts.PathRw, SearchOption.TopDirectoryOnly, backgroundworker);
                var directoryInfo = new DirectoryInfo(AppConsts.PathRw);
                var ds = directoryInfo.GetDirectories("*.*", SearchOption.TopDirectoryOnly).ToArray();
                foreach (var di in ds) {
                    ImportFiles(di.FullName, SearchOption.AllDirectories, backgroundworker);
                }

                Helper.CleanupDirectories(AppConsts.PathRw, AppVars.Progress);
                AppVars.ImportRequested = false;
            }

            var hashX = AppDatabase.GetNextCheck();
            if (hashX != null) {
                if (AppDatabase.TryGetImg(hashX, out var imgX)) {
                    var filename = Helper.GetFileName(imgX.Path, imgX.Hash, imgX.Ext);
                    var imagedata = FileHelper.ReadFile(filename);
                    if (imagedata == null) {
                        Delete(hashX);
                        return;
                    }

                    var hashT = FileHelper.GetHash(imagedata);
                    if (!hashT.Equals(hashX)) {
                        AppDatabase.SetDeleted(hashX);
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
                        var shortfilename = Helper.GetShortFileName(imgX.Path, imgX.Hash);
                        var radiusLast = string.IsNullOrWhiteSpace(imgX.Next) ? "----" : imgX.Next.Substring(0, 4);
                        backgroundworker.ReportProgress(0,
                            $"[{age} ago] {shortfilename} ({imgX.Counter}) {radiusLast} {AppConsts.CharRightArrow} ({counter}) {radiusNext.Substring(0, 4)}");
                        AppDatabase.SetNext(hashX, radiusNext);
                        _delay = 0;
                    }

                    if (counter != imgX.Counter) {
                        if (counter != imgX.Counter + 1) {
                            AppDatabase.SetPrev(hashX, string.Empty);
                            AppDatabase.SetNext(hashX, string.Empty);
                            AppDatabase.SetHorizon(hashX);
                            AppDatabase.SetCounter(hashX, 0);
                        }
                        else {
                            AppDatabase.SetCounter(hashX, counter);
                        }
                    }

                    AppDatabase.SetLastCheck(hashX);
                }
            }

            _delay = Math.Min(2500, _delay + 10);
            Thread.Sleep(_delay);
        }
    }
}
