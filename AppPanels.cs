using System;
using System.Collections.Generic;
using System.Drawing;
using ImageMagick;

namespace ImgSoh
{
    public static class AppPanels
    {
        private static readonly ImgPanel[] _imgPanels = new ImgPanel[2];
        private static List<Tuple<string, float>> _vector;
        private static int _position;

        public static ImgPanel GetImgPanel(int idPanel)
        {
            return _imgPanels[idPanel];
        }

        private static bool SetPanel(string hash, out Img img, out byte[] imagedata, out string format, out Bitmap bitmap, out int familysize)
        {
            imagedata = null;
            format = string.Empty;
            bitmap = null;
            familysize = 0;
            if (!AppImgs.TryGet(hash, out img)) {
                return false;
            }

            var filename = AppFile.GetFileName(img.Name, AppConsts.PathHp);
            imagedata = AppFile.ReadEncryptedFile(filename);
            if (imagedata == null) {
                return false;
            }

            var hashT = AppHash.GetHash(imagedata);
            if (!hashT.Equals(hash)) {
                return false;
            }

            var magickImage = AppBitmap.ImageDataToMagickImage(imagedata);
            if (magickImage == null) {
                return false;
            }

            format = magickImage.Format.ToString().ToLower();
            bitmap = AppBitmap.MagickImageToBitmap(magickImage, img.Orientation);
            if (bitmap == null) {
                return false;
            }

            familysize = AppImgs.GetFamilySize(img.Family);

            return true;
        }

        public static bool SetLeftPanel(string hash)
        {
            if (!SetPanel(hash, 
                    out var img, 
                    out var imagedata, 
                    out var format, 
                    out var bitmap, 
                    out var familysize)) {
                return false;
            }

            var imgpanel = new ImgPanel(
                hash: hash,
                img: img,
                size: imagedata.LongLength,
                bitmap: bitmap,
                format: format,
                familysize: familysize);

            _imgPanels[0] = imgpanel;

            _vector = AppImgs.GetVector(img);
            _position = 0;

            return true;
        }

        public static bool UpdateRightPanel()
        {
            return SetRightPanel(_vector[_position].Item1);
        }

        public static bool SetRightPanel(string hash)
        {
            if (!SetPanel(hash,
                    out var img,
                    out var imagedata,
                    out var format,
                    out var bitmap,
                    out var familysize)) {
                return false;
            }

            if (AppVars.ShowXOR) {
                using (var xb = new MagickImage())
                using (var yb = new MagickImage()) {
                    xb.Read(_imgPanels[0].Bitmap);
                    yb.Read(bitmap);
                    AppBitmap.Composite(xb, yb, out var zb);
                    var bitmapxor = AppBitmap.MagickImageToBitmap(zb, RotateFlipType.RotateNoneFlipNone);
                    zb.Dispose();
                    bitmap.Dispose();
                    bitmap = bitmapxor;
                }
            }

            var imgpanel = new ImgPanel(
                hash: hash,
                img: img,
                size: imagedata.LongLength,
                bitmap: bitmap,
                format: format,
                familysize: familysize);

            _imgPanels[1] = imgpanel;
            return true;
        }

        public static string GetRight()
        {
            return _vector[_position].Item1;
        }

        public static void MoveRight()
        {
            if (_position + 1 < _vector.Count) {
                _position++;
            }

            SetRightPanel(_vector[_position].Item1);
        }

        public static void MoveLeft()
        {
            if (_position - 1 >= 0) {
                _position--;
            }

            SetRightPanel(_vector[_position].Item1);
        }

        public static void MoveToTheFirst()
        { 
            _position = 0;
            SetRightPanel(_vector[_position].Item1);
        }

        public static void MoveToTheLast()
        {
            _position = _vector.Count - 1;
            SetRightPanel(_vector[_position].Item1);
        }

        public static void SetVictim(int idPanel)
        {
            if (idPanel == 0 || idPanel == 1) {
                _imgPanels[idPanel].SetVictim();
            }
        }

        public static void Confirm()
        {
            var hashX = _imgPanels[0].Hash;
            var hashY = _imgPanels[1].Hash;
            if (AppImgs.TryGet(hashX, out var imgX) && AppImgs.TryGet(hashY, out var imgY)) {
                AppImgs.SetLastView(imgX.Hash, DateTime.Now);
                if (imgX.Family.Length == 0 ||
                    (imgX.Family.Length > 0 && imgY.Family.Length == 0) ||
                    (imgX.Family.Length > 0 && imgY.Family.Length > 0 && imgX.Family.Equals(imgY.Family))) {

                    var history = new HashSet<string>(imgX.HistoryArray);
                    for (var i = 0; i <= _position; i++) {
                        history.Add(_vector[i].Item1);
                    }

                    var history_new = string.Join(string.Empty, history);
                    if (!imgX.History.Equals(history_new)) {
                        AppImgs.SetHistory(hashX, history_new);
                    }
                }

                AppImgs.SetLastView(imgY.Hash, DateTime.Now);
                AppImgs.AddToHistory(imgY.Hash, imgX.Hash);
            }
        }

        public static void DeleteLeft()
        {
            var hashX = _imgPanels[0].Hash;
            ImgMdf.Delete(hashX);
            var hashY = _imgPanels[1].Hash;
            if (AppImgs.TryGet(hashY, out var imgY)) {
                AppImgs.SetLastView(imgY.Hash, DateTime.Now);
            }
        }

        public static void DeleteRight()
        {
            var hashX = _imgPanels[0].Hash;
            if (AppImgs.TryGet(hashX, out var imgX)) {
                AppImgs.SetLastView(imgX.Hash, DateTime.Now);
            }

            var hashY = _imgPanels[1].Hash;
            ImgMdf.Delete(hashY);

            _vector.RemoveAt(_position);
            if (_position >= _vector.Count) {
                _position = _vector.Count - 1;
            }

            SetRightPanel(_vector[_position].Item1);
        }

        public static void CombineToFamily()
        {
            var hashX = _imgPanels[0].Hash;
            if (!AppImgs.TryGet(hashX, out var imgX)) {
                return;
            }

            var hashY = _imgPanels[1].Hash;
            if (!AppImgs.TryGet(hashY, out var imgY)) {
                return;
            }

            if (!string.IsNullOrEmpty(imgX.Family) && string.IsNullOrEmpty(imgY.Family)) {
                AppImgs.SetFamily(imgY.Hash, imgX.Family);
                if (AppImgs.TryGet(imgY.Hash, out imgY)) {
                    _imgPanels[1].Img = imgY;
                }
            }
            else {
                if (string.IsNullOrEmpty(imgX.Family) && !string.IsNullOrEmpty(imgY.Family)) {
                    AppImgs.SetFamily(imgX.Hash, imgY.Family);
                    if (AppImgs.TryGet(imgX.Hash, out imgX)) {
                        _imgPanels[0].Img = imgX;
                    }
                }
            }
        }

        public static void DetachFromFamily()
        {
            var hashX = _imgPanels[0].Hash;
            if (!AppImgs.TryGet(hashX, out var imgX)) {
                return;
            }

            var hashY = _imgPanels[1].Hash;
            if (!AppImgs.TryGet(hashY, out var imgY)) {
                return;
            }

            if (!string.IsNullOrEmpty(imgX.Family) && string.IsNullOrEmpty(imgY.Family)) {
                AppImgs.SetFamily(imgY.Hash, string.Empty);
                AppImgs.SetHistory(imgY.Hash, string.Empty);
                if (AppImgs.TryGet(imgY.Hash, out imgY)) {
                    _imgPanels[1].Img = imgY;
                }
            }
            else {
                if (string.IsNullOrEmpty(imgX.Family) && !string.IsNullOrEmpty(imgY.Family)) {
                    AppImgs.SetFamily(imgX.Hash, string.Empty);
                    AppImgs.SetHistory(imgX.Hash, string.Empty);
                    if (AppImgs.TryGet(imgX.Hash, out imgX)) {
                        _imgPanels[0].Img = imgX;
                    }
                }
            }
        }
    }
}
