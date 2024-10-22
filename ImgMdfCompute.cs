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
        private static int _added;
        private static int _bad;
        private static int _found;

        private static bool ImportFile(string orgfilename, DateTime lastview, BackgroundWorker backgroundworker)
        {
            var orgname = Path.GetFileNameWithoutExtension(orgfilename).ToLowerInvariant();
            if (AppImgs.TryGetByName(orgname, out var imgE)) {
                var filenameF = AppFile.GetFileName(imgE.Name, AppConsts.PathHp);
                if (orgfilename.Equals(filenameF)) {
                    // existing file
                    return true;
                }
            }

            var orgext = Path.GetExtension(orgfilename);
            if (orgext.StartsWith(".")) {
                orgext = orgext.Substring(1);
            }

            backgroundworker.ReportProgress(0,
                $"importing {orgfilename} (a:{_added})/f:{_found}/b:{_bad}){AppConsts.CharEllipsis}");
            var lastmodified = File.GetLastWriteTime(orgfilename);
            if (lastmodified > DateTime.Now) {
                lastmodified = DateTime.Now;
            }

            var imagedata = AppFile.ReadFile(orgfilename);
            string hash;
            DateTime taken;
            int meta;
            if (orgext.Equals(AppConsts.DatExtension, StringComparison.OrdinalIgnoreCase) ||
                orgext.Equals(AppConsts.MzxExtension, StringComparison.OrdinalIgnoreCase)) {
                var decrypteddata = orgext.Equals(AppConsts.DatExtension, StringComparison.OrdinalIgnoreCase)
                    ? AppEncryption.DecryptDat(imagedata, orgname)
                    : AppEncryption.Decrypt(imagedata, orgname);
                if (decrypteddata == null) {
                    DeleteFile(orgfilename);
                    _bad++;
                    return true;
                }

                hash = AppHash.GetHash(decrypteddata);
                if (AppImgs.TryGet(hash, out var imgF)) {
                    var filenameF = AppFile.GetFileName(imgF.Name, AppConsts.PathHp);
                    if (File.Exists(filenameF)) {
                        // we have a file
                        var imagedataF = AppFile.ReadEncryptedFile(filenameF);
                        var foundhash = AppHash.GetHash(imagedataF);
                        if (hash.Equals(foundhash)) {
                            // ...and file is okay
                            // delete incoming file
                            File.Delete(orgfilename);
                            _found++;
                            return true;
                        }
                    }

                    // ...but found file is missing or changed
                    // delete record with changed file and continue
                    Delete(hash);
                }

                var tmporgfilename = $"{AppConsts.PathGbProtected}\\{orgname}.temp";
                File.WriteAllBytes(tmporgfilename, decrypteddata);
                AppBitmap.Read(tmporgfilename, out taken, out meta);
                File.Delete(tmporgfilename);
                imagedata = decrypteddata;
            }
            else {
                hash = AppHash.GetHash(imagedata);
                if (AppImgs.TryGet(hash, out var imgF)) {
                    var filenameF = AppFile.GetFileName(imgF.Name, AppConsts.PathHp);
                    if (File.Exists(filenameF)) {
                        // we have a file
                        var imagedataF = AppFile.ReadEncryptedFile(filenameF);
                        var foundhash = AppHash.GetHash(imagedataF);
                        if (hash.Equals(foundhash)) {
                            // ...and file is okay
                            // delete incoming file
                            File.SetAttributes(orgfilename, FileAttributes.Normal);
                            File.Delete(orgfilename);
                            _found++;
                            return true;
                        }
                    }

                    // ...but found file is missing or changed
                    // delete record with changed file and continue
                    Delete(hash);
                }

                AppBitmap.Read(orgfilename, out taken, out meta);
            }

            float[] vector;
            float magnitude;
            using (var magickImage = AppBitmap.ImageDataToMagickImage(imagedata)) {
                if (magickImage == null) {
                    DeleteFile(orgfilename);
                    _bad++;
                    return true;
                }

                using (var bitmap = AppBitmap.MagickImageToBitmap(magickImage, RotateFlipType.RotateNoneFlipNone)) {
                    vector = AppVit.CalculateVector(bitmap).ToArray();
                    if (vector.Length != AppConsts.VectorLength) {
                        DeleteFile(orgfilename);
                        _bad++;
                        return true;
                    }

                    magnitude = AppVit.GetMagnitude(vector);
                }
            }

            var name = AppImgs.GetName(hash);
            var newfilename = AppFile.GetFileName(name, AppConsts.PathHp);
            if (!orgfilename.Equals(newfilename)) {
                AppFile.WriteEncryptedFile(newfilename, imagedata);
                File.SetLastWriteTime(newfilename, lastmodified);
                File.SetAttributes(orgfilename, FileAttributes.Normal);
                File.Delete(orgfilename);
            }
            
            var imgnew = new Img(
                hash: hash,
                name: name,
                orientation: RotateFlipType.RotateNoneFlipNone,
                lastview: lastview,
                next: string.Empty,
                verified: false,
                horizon: string.Empty,
                counter: 0,
                taken: taken,
                meta: meta,
                vector: vector,
                magnitude: magnitude,
                viewed: 0
             );

            AppImgs.Save(imgnew);
            _added++;
            return true;
        }

        private static void ImportFiles(string path, SearchOption so, DateTime lastview, BackgroundWorker backgroundworker)
        {
            var directoryInfo = new DirectoryInfo(path);
            var fs = directoryInfo.GetFiles("*.*", so).ToArray();
            foreach (var e in fs) {
                var orgfilename = e.FullName;
                if (!ImportFile(orgfilename, lastview, backgroundworker)) {
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
                var lastview = AppImgs.GetMinimalLastView();
                _added = 0;
                _found = 0;
                _bad = 0;
                ImportFiles(AppConsts.PathHp, SearchOption.AllDirectories, lastview, backgroundworker);
                if (_added < AppConsts.MaxImportFiles) {
                    ImportFiles(AppConsts.PathRawProtected, SearchOption.TopDirectoryOnly, lastview, backgroundworker);
                    if (_added < AppConsts.MaxImportFiles) {
                        var directoryInfo = new DirectoryInfo(AppConsts.PathRawProtected);
                        var ds = directoryInfo.GetDirectories("*.*", SearchOption.TopDirectoryOnly).ToArray();
                        foreach (var di in ds) {
                            ImportFiles(di.FullName, SearchOption.AllDirectories, lastview, backgroundworker);
                            if (_added >= AppConsts.MaxImportFiles) {
                                break;
                            }
                        }
                    }
                }

                Helper.CleanupDirectories(AppConsts.PathRawProtected, AppVars.Progress);
                AppVars.ImportRequested = false;
                ((IProgress<string>)AppVars.Progress).Report($"Imported a:{_added}/f:{_found}/b:{_bad}");
            }

            /*
            if (AppVars.GetLimit() == 0) {
                return;
            }
            */

            var imgX = AppImgs.GetForCheck();
            if (imgX == null) {
                Thread.Sleep(500);
                return;
            }

            var filenameX = AppFile.GetFileName(imgX.Name, AppConsts.PathHp);
            var imagedata = AppFile.ReadEncryptedFile(filenameX);
            if (imagedata == null) {
                Delete(imgX.Hash);
                return;
            }

            var hashT = AppHash.GetHash(imagedata);
            if (!hashT.Equals(imgX.Hash)) {
                Delete(imgX.Hash);
                return;
            }

            if (imgX.Magnitude <= 0f) {
                using (var magickImage = AppBitmap.ImageDataToMagickImage(imagedata)) {
                    using (var bitmap = AppBitmap.MagickImageToBitmap(magickImage, imgX.Orientation)) {
                        var vector = AppVit.CalculateVector(bitmap).ToArray();
                        AppImgs.SetVector(imgX.Hash, vector);
                        var magnitude = AppVit.GetMagnitude(vector);
                        AppImgs.SetMagnitude(imgX.Hash, magnitude);
                        imgX = AppImgs.Get(imgX.Hash);
                    }
                }
            }

            var horizon = imgX.Horizon;
            AppImgs.Find(imgX, out var radiusNext, out var counter);
            var message = string.Empty;
            var imgnext = string.IsNullOrWhiteSpace(imgX.Next) ? "----" : imgX.Next.Substring(0, 4);
            var next = string.IsNullOrWhiteSpace(radiusNext) ? "----" : radiusNext.Substring(0, 4);
            if (counter != imgX.Counter && counter > 0) {
                radiusNext = string.Empty;
                next = string.Empty;
                horizon = string.Empty;
                counter = 0;
            }

            if (imgX.Counter != counter || !imgnext.Equals(next)) {
                message = $"[{imgX.Viewed}:{imgX.Counter}:{imgnext}] {AppConsts.CharRightArrow} [{imgX.Viewed}:{imgX.Counter}:{next}]";
            }

            if (!imgX.Next.Equals(radiusNext)) {
                AppImgs.SetNext(imgX.Hash, radiusNext);
            }

            if (!imgX.Horizon.Equals(horizon)) {
                AppImgs.SetHorizon(imgX.Hash, horizon);
            }

            if (imgX.Counter != counter) {
                AppImgs.SetCounter(imgX.Hash, counter);
            }

            if (!string.IsNullOrEmpty(message)) {
                backgroundworker.ReportProgress(0, $"{message}");
            }

            Thread.Sleep(100);
        }

        public static void Random(string path, IProgress<string> progress)
        {
            var dirs = Directory.GetDirectories(path, "*.*", SearchOption.AllDirectories);
            var rindex = AppVars.RandomNext(dirs.Length);
            var dir = dirs[rindex];
            progress?.Report($"Get {dir}");
        }
    }
}
