using System;
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
            if (AppImgs.ContainsHash(name)) {
                return;
            }

            var lastview = DateTime.Now.AddYears(-5);

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
            var found = AppImgs.TryGetValue(hash, out var imgfound);
            if (found) {
                // we have a record with the same hash...
                lastview = imgfound.LastView;
                var filenamefound = imgfound.GetFileName();
                if (File.Exists(filenamefound)) {
                    // we have a file
                    var foundimagedata = FileHelper.ReadEncryptedFile(filenamefound);
                    var foundhash = FileHelper.GetHash(foundimagedata);
                    if (imgfound.Hash.Equals(foundhash)) {
                        // and file is okay
                        var foundlastmodified = File.GetLastWriteTime(orgfilename);
                        if (foundlastmodified > lastmodified) {
                            File.SetLastWriteTime(filenamefound, lastmodified);
                            imgfound.SetDateTaken(lastmodified);
                        }
                    }
                    else {
                        // but found file was changed or corrupted
                        FileHelper.WriteEncryptedFile(filenamefound, imagedata);
                        File.SetLastWriteTime(filenamefound, lastmodified);
                        imgfound.SetDateTaken(lastmodified);
                    }

                    FileHelper.DeleteToRecycleBin(orgfilename, AppConsts.PathGbProtected);
                    _found++;
                    return;
                }
                else {
                    // ...but file is missing
                    Delete(imgfound, null);
                }
            }

            byte[] vector;
            using (var magickImage = BitmapHelper.ImageDataToMagickImage(imagedata)) {
                if (magickImage == null) {
                    var badname = Path.GetFileName(orgfilename);
                    var badfilename = $"{AppConsts.PathGbProtected}\\{badname}{AppConsts.CorruptedExtension}";
                    if (File.Exists(badfilename)) {
                        FileHelper.DeleteToRecycleBin(badfilename, AppConsts.PathGbProtected);
                    }

                    File.WriteAllBytes(badfilename, imagedata);
                    FileHelper.DeleteToRecycleBin(orgfilename, AppConsts.PathGbProtected);
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

            var folder = AppImgs.GetFolder();
            var nimg = new Img(
                hash: hash,
                folder: folder,
                datetaken: lastmodified,
                vector: vector,
                lastview: lastview,
                orientation: RotateFlipType.RotateNoneFlipNone,
                distance: 1f,
                lastcheck: lastview,
                review: 0,
                next: hash);

            var newfilename = nimg.GetFileName();
            if (!orgfilename.Equals(newfilename)) {
                FileHelper.WriteEncryptedFile(newfilename, imagedata);
                File.SetLastWriteTime(newfilename, lastmodified);
            }

            var vimagedata = FileHelper.ReadEncryptedFile(newfilename);
            if (vimagedata == null) {
                FileHelper.DeleteToRecycleBin(newfilename, AppConsts.PathGbProtected);
                return;
            }

            var vhash = FileHelper.GetHash(vimagedata);
            if (!hash.Equals(vhash)) {
                FileHelper.DeleteToRecycleBin(newfilename, AppConsts.PathGbProtected);
                return;
            }

            if (!orgfilename.Equals(newfilename)) {
                FileHelper.DeleteToRecycleBin(orgfilename, AppConsts.PathGbProtected);
            }

            Add(nimg);
            AppDatabase.AddImage(nimg);

            _added++;
        }

        private static void ImportFiles(string path, BackgroundWorker backgroundworker)
        {
            var directoryInfo = new DirectoryInfo(path);
            var fs = directoryInfo.GetFiles("*.*", SearchOption.AllDirectories).ToArray();
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
                ImportFiles(AppConsts.PathHp, backgroundworker);
                ImportFiles(AppConsts.PathRw, backgroundworker);
                ImportFiles(AppConsts.PathRwProtected, backgroundworker);
                AppVars.ImportRequested = false;
            }

            var imgX = AppImgs.GetNextCheck();
            if (imgX != null) {
                if (imgX.GetVector().Length != 4096) {
                    var filename = imgX.GetFileName();
                    var imagedata = FileHelper.ReadEncryptedFile(filename);
                    using (var magickImage = BitmapHelper.ImageDataToMagickImage(imagedata))
                    using (var bitmap = BitmapHelper.MagickImageToBitmap(magickImage, RotateFlipType.RotateNoneFlipNone)) {
                        var vector = VggHelper.CalculateVector(bitmap);
                        AppImgs.SetVector(imgX.Hash, vector);
                    }
                }

                AppImgs.VerifyPairs(imgX.Hash);
                var pairs = AppDatabase.GetPairs(imgX.Hash);
                var review = (short)pairs.Count(e => e.Value);
                AppImgs.SetReview(imgX.Hash, review);

                var shadow = AppImgs.GetShadow();
                shadow.Remove(imgX.Hash);

                var mindistance = float.MaxValue;
                Img imgY = null;
                foreach (var img in shadow.Values) {
                    if (pairs.ContainsKey(img.Hash)) {
                        continue;
                    }

                    var distance = VggHelper.GetDistance(imgX.GetVector(), img.GetVector());
                    if (distance < mindistance) {
                        mindistance = distance;
                        imgY = img;
                    }
                }

                if (imgY != null) {
                    if (!imgX.Next.Equals(imgY.Hash) || Math.Abs(mindistance - imgX.Distance) > 0.0001f) {
                        var age = Helper.TimeIntervalToString(DateTime.Now.Subtract(imgX.LastCheck));
                        var shortfilename = imgX.GetShortFileName();
                        var xreview = AppDatabase.GetPairs(imgX.Hash).Count;
                        var yreview = AppDatabase.GetPairs(imgY.Hash).Count;
                        backgroundworker.ReportProgress(0,
                            $"[{age} ago] {shortfilename}: [{xreview}] {imgX.Distance:F4} {AppConsts.CharRightArrow} [{yreview}] {mindistance:F4}");
                        AppImgs.SetDistance(imgX.Hash, mindistance);
                        AppImgs.SetNext(imgX.Hash, imgY.Hash);
                    }
                }

                AppImgs.SetLastCheck(imgX.Hash, DateTime.Now);
            }
        }
    }
}
