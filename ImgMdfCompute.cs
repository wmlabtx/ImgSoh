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

                    AppDatabase.ImgUpdateLastView(hash, lastView);
                    DeleteFile(orgfilename);
                    if (doImportFamily) {
                        //AddImportedFamily(hash);
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

            if (vector == null) {
                DeleteFile(orgfilename);
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

            var imgnew = new Img(
                hash: hash,
                folder: folder,
                vector: vector,
                orientation: RotateFlipType.RotateNoneFlipNone,
                lastview: lastView,
                next: hash,
                distance: 1f,
                lastcheck: lastView,
                verified: false
            );

            AppDatabase.AddImg(imgnew);

            if (doImportFamily) {
                //AddImportedFamily(hash);
            }

            _added++;
        }

        private static void ImportFiles(string path, BackgroundWorker backgroundworker)
        {
            var directoryInfo = new DirectoryInfo(path);
            var fs = directoryInfo.GetFiles("*.*", SearchOption.AllDirectories).ToArray();
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
                //_importedfamily.Clear();
                ImportFiles(AppConsts.PathHp, backgroundworker);
                ImportFiles(AppConsts.PathRw, backgroundworker);
                ImportFiles(AppConsts.PathRwProtected, backgroundworker);
                AppVars.ImportRequested = false;
            }

            var imgX = AppDatabase.GetNextCheck();
            if (imgX.GetVector().Length != 4096) {
                var filename = Helper.GetFileName(imgX.Folder, imgX.Hash);
                var imagedata = FileHelper.ReadEncryptedFile(filename);
                using (var magickImage = BitmapHelper.ImageDataToMagickImage(imagedata))
                using (var bitmap = BitmapHelper.MagickImageToBitmap(magickImage, RotateFlipType.RotateNoneFlipNone)) {
                    var vector = VggHelper.CalculateVector(bitmap);
                    imgX = AppDatabase.ImgUpdateVector(imgX.Hash, vector);
                }
            }

            var pairs = AppDatabase.GetPairs(imgX.Hash);
            var vectors = AppDatabase.GetVectors();
            vectors.Remove(imgX.Hash);
            foreach (var e in pairs) {
                vectors.Remove(e.Key);
            }

            var hashesY = new string[] { null, null };
            var mindistance = float.MaxValue;
            var minlv = DateTime.MaxValue;
            var vectorX = imgX.GetVector();
            foreach (var e in vectors) {
                if (pairs.ContainsKey(e.Key)) {
                    if (AppDatabase.TryGetImg(e.Key, out var img)) {
                        if (img.LastView < minlv) {
                            hashesY[0] = e.Key;
                            minlv = img.LastView;
                        }
                    }
                }
                else {
                    var distance = VggHelper.GetDistance(vectorX, e.Value);
                    if (distance < mindistance) {
                        hashesY[1] = e.Key;
                        mindistance = distance;
                    }
                }
            }

            if (hashesY[0] != null || hashesY[1] != null) {
                int mode;
                do {
                    mode = AppVars.RandomNext(2);
                } while (hashesY[mode] == null);

                if (!imgX.Next.Equals(hashesY[mode])) {
                    var age = Helper.TimeIntervalToString(DateTime.Now.Subtract(imgX.LastView));
                    var shortfilename = Helper.GetShortFileName(imgX.Folder, imgX.Hash);
                    backgroundworker.ReportProgress(0, $"[{age} ago] {shortfilename}");
                    AppDatabase.ImgUpdateNext(imgX.Hash, hashesY[mode]);
                }

                if (mode == 1) {
                    AppDatabase.ImgUpdateDistance(imgX.Hash, mindistance);
                }
            }

            AppDatabase.ImgUpdateLastCheck(imgX.Hash, DateTime.Now);
        }
    }
}
