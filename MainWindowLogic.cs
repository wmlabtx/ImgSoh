using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;

namespace ImgSoh
{
    public sealed partial class MainWindow
    {
        private double _picsMaxWidth;
        private double _picsMaxHeight;
        private double _labelMaxHeight;

        private NotifyIcon _notifyIcon = new NotifyIcon();
        private BackgroundWorker _backgroundWorker;

        private async void WindowLoaded()
        {
            BoxLeft.MouseDown += PictureLeftBoxMouseClick;
            BoxRight.MouseDown += PictureRightBoxMouseClick;

            LabelLeft.MouseDown += ButtonLeftNextMouseClick;
            LabelRight.MouseDown += ButtonRightNextMouseClick;

            Left = SystemParameters.WorkArea.Left + AppConsts.WindowMargin;
            Top = SystemParameters.WorkArea.Top + AppConsts.WindowMargin;
            Width = SystemParameters.WorkArea.Width - AppConsts.WindowMargin * 2;
            Height = SystemParameters.WorkArea.Height - AppConsts.WindowMargin * 2;
            Left = SystemParameters.WorkArea.Left + (SystemParameters.WorkArea.Width - AppConsts.WindowMargin - Width) / 2;
            Top = SystemParameters.WorkArea.Top + (SystemParameters.WorkArea.Height - AppConsts.WindowMargin - Height) / 2;

            _picsMaxWidth = Grid.ActualWidth;
            _labelMaxHeight = LabelLeft.ActualHeight;
            _picsMaxHeight = Grid.ActualHeight - _labelMaxHeight;

            _notifyIcon.Icon = new Icon(@"app.ico");
            _notifyIcon.Visible = false;
            _notifyIcon.DoubleClick +=
                delegate
                {
                    Show();
                    WindowState = WindowState.Normal;
                    _notifyIcon.Visible = false;
                    RedrawCanvas();
                };

            AppVars.Progress = new Progress<string>(message => Status.Text = message);

            await Task.Run(() => { AppVit.LoadNet(AppVars.Progress); }).ConfigureAwait(true);
            await Task.Run(() => { AppDatabase.LoadImages(AppConsts.FileDatabase, AppVars.Progress); }).ConfigureAwait(true);
            //await Task.Run(() => { AppDatabase.Populate(AppVars.Progress); }).ConfigureAwait(true);
            await Task.Run(() => { ImgMdf.Find(null, AppVars.Progress); }).ConfigureAwait(true);

            DrawCanvas();

            AppVars.SuspendEvent = new ManualResetEvent(true);

            _backgroundWorker = new BackgroundWorker { WorkerSupportsCancellation = true, WorkerReportsProgress = true };
            _backgroundWorker.DoWork += DoCompute;
            _backgroundWorker.ProgressChanged += DoComputeProgress;
            _backgroundWorker.RunWorkerAsync();
        }

        private void OnStateChanged()
        {
            if (WindowState != WindowState.Minimized) {
                return;
            }

            Hide();
            _notifyIcon.Visible = true;
        }

        private static void ImportClick()
        {
            AppVars.ImportRequested = true;
        }

        private void PictureLeftBoxMouseClick()
        {
            ImgPanelDelete(0);
        }

        private void PictureRightBoxMouseClick()
        {
            ImgPanelDelete(1);
        }

        private async void ButtonLeftNextMouseClick()
        {
            DisableElements();
            await Task.Run(ImgMdf.Confirm).ConfigureAwait(true);
            await Task.Run(() => { ImgMdf.Find(null, AppVars.Progress); }).ConfigureAwait(true);
            DrawCanvas();
            EnableElements();
        }

        private void DisableElements()
        {
            ElementsEnable(false);
        }

        private void EnableElements()
        {
            ElementsEnable(true);
        }

        private void ElementsEnable(bool enabled)
        {
            foreach (System.Windows.Controls.MenuItem item in Menu.Items) {
                item.IsEnabled = enabled;
            }

            Status.IsEnabled = enabled;
            BoxLeft.IsEnabled = enabled;
            BoxRight.IsEnabled = enabled;
            LabelLeft.IsEnabled = enabled;
            LabelRight.IsEnabled = enabled;
        }

        private void DrawCanvas()
        {
            var panels = new ImgPanel[2];
            panels[0] = AppPanels.GetImgPanel(0);
            panels[1] = AppPanels.GetImgPanel(1);
            if (panels[0] == null || panels[1] == null) {
                return;
            }
             
            var pBoxes = new[] { BoxLeft, BoxRight };
            var pLabels = new[] { LabelLeft, LabelRight };
            for (var index = 0; index < 2; index++) {
                if (AppImgs.TryGetImg(panels[index].Hash, out var imgX)) {
                    if (AppImgs.TryGetImg(panels[1 - index].Hash, out var imgY)) {
                        pBoxes[index].Source = AppBitmap.ImageSourceFromBitmap(panels[index].Bitmap);
                        var sb = new StringBuilder();
                        sb.Append($"{panels[index].Name}.{panels[index].Format}");

                        var next = string.IsNullOrWhiteSpace(imgX.Next) ? "----" : imgX.Next.Substring(0, 4);
                        sb.Append($" [{imgX.Viewed}:{imgX.Counter}:{next}]");

                        if (!string.IsNullOrEmpty(imgX.Family)) {
                            var familysize = AppImgs.GetFamily(imgX.Family).Count();
                            sb.Append($" {imgX.Family}:{familysize}");
                        }

                        sb.AppendLine();

                        sb.Append($"{Helper.SizeToString(panels[index].Size)} ");
                        sb.Append($" ({panels[index].Bitmap.Width}x{panels[index].Bitmap.Height})");
                        sb.AppendLine();

                        sb.Append($" {Helper.TimeIntervalToString(DateTime.Now.Subtract(imgX.LastView))} ago ");
                        if (imgX.Meta > 0) {
                            sb.Append($" [{imgX.Meta}]");
                        }

                        if (!imgX.Taken.Equals(DateTime.MinValue)) {
                            sb.Append($" {imgX.Taken}");
                        }

                        pLabels[index].Text = sb.ToString();
                        pLabels[index].Background = System.Windows.Media.Brushes.White;
                        if (panels[index].IsVictim) {
                              pLabels[index].Background = System.Windows.Media.Brushes.Red;
                        }
                        else {
                            if (!string.IsNullOrEmpty(imgX.Family) && imgX.Family.Equals(imgY.Family)) {
                                pLabels[index].Background = System.Windows.Media.Brushes.LightGreen;
                            }
                            else {
                                if (!imgX.Verified) {
                                    pLabels[index].Background = System.Windows.Media.Brushes.Yellow;
                                }
                                else {
                                    if (!string.IsNullOrWhiteSpace(imgX.Family)) {
                                        pLabels[index].Background = System.Windows.Media.Brushes.Bisque;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            RedrawCanvas();
        }

        private void RedrawCanvas()
        {
            var ws = new double[2];
            var hs = new double[2];
            for (var index = 0; index < 2; index++) {
                var panel = AppPanels.GetImgPanel(index);
                ws[index] = _picsMaxWidth / 2;
                hs[index] = _picsMaxHeight;
                if (panel?.Bitmap != null) {
                    ws[index] = panel.Bitmap.Width;
                    hs[index] = panel.Bitmap.Height;
                }
            }

            var aW = _picsMaxWidth / (ws[0] + ws[1]);
            var aH = _picsMaxHeight / Math.Max(hs[0], hs[1]);
            var a = Math.Min(aW, aH);
            if (a > 1.0) {
                a = 1.0;
            }

            SizeToContent = SizeToContent.Manual;
            Grid.ColumnDefinitions[0].Width = new GridLength(ws[0] * a, GridUnitType.Pixel);
            Grid.ColumnDefinitions[1].Width = new GridLength(ws[1] * a, GridUnitType.Pixel);
            Grid.RowDefinitions[0].Height = new GridLength(Math.Max(hs[0], hs[1]) * a, GridUnitType.Pixel);
            Grid.Width = (ws[0] + ws[1]) * a;
            Grid.Height = Math.Max(hs[0], hs[1]) * a + _labelMaxHeight;
            SizeToContent = SizeToContent.WidthAndHeight;
            Left = SystemParameters.WorkArea.Left + (SystemParameters.WorkArea.Width - AppConsts.WindowMargin - Width) / 2;
            Top = SystemParameters.WorkArea.Top + (SystemParameters.WorkArea.Height - AppConsts.WindowMargin - Height) / 2;
        }

        private async void ImgPanelDelete(int idpanel)
        {
            DisableElements();
            await Task.Run(() => { ImgMdf.Delete(idpanel); }).ConfigureAwait(true);
            await Task.Run(() => { ImgMdf.Find(null, AppVars.Progress); }).ConfigureAwait(true);
            DrawCanvas();
            EnableElements();
        }

        private async void RotateClick(RotateFlipType rft)
        {
            DisableElements();
            var hash = AppPanels.GetImgPanel(0).Hash;
            await Task.Run(() => { ImgMdf.Rotate(hash, rft); }).ConfigureAwait(true);
            await Task.Run(() => { ImgMdf.Find(hash, AppVars.Progress); }).ConfigureAwait(true);
            DrawCanvas();
            EnableElements();
        }

        private void ReleaseResources()
        {
            AppBitmap.StopExif();
            _notifyIcon?.Dispose();
            _notifyIcon = null;
            _backgroundWorker?.CancelAsync();
            _backgroundWorker?.Dispose();
            _backgroundWorker = null;
        }

        private void CloseApp()
        {
            ReleaseResources();
            System.Windows.Application.Current.Shutdown();
        }

        private void RefreshClick()
        {
            DisableElements();
            DrawCanvas();
            EnableElements();
        }

        private async void RandomClick()
        {
            DisableElements();
            await Task.Run(() => { ImgMdf.Random(AppConsts.PathTempProtected, AppVars.Progress); }).ConfigureAwait(true);
            DrawCanvas();
            EnableElements();
        }

        private void ToggleXorClick()
        {
            DisableElements();
            AppVars.ShowXOR = !AppVars.ShowXOR;
            var hashY = AppPanels.GetImgPanel(1).Hash;
            AppPanels.SetImgPanel(1, hashY);
            DrawCanvas();
            EnableElements();
        }

        private async void CombineToFamily()
        {
            DisableElements();
            await Task.Run(ImgMdf.CombineToFamily).ConfigureAwait(true);
            DrawCanvas();
            EnableElements();
        }

        private async void DetachFromFamily()
        {
            DisableElements();
            await Task.Run(ImgMdf.DetachFromFamily).ConfigureAwait(true);
            DrawCanvas();
            EnableElements();
        }

        private void OnKeyDown(Key key)
        {
            switch (key) {
                case Key.A:
                    CombineToFamily();
                    break;
                case Key.D:
                    DetachFromFamily();
                    break;
                case Key.V:
                    ToggleXorClick();
                    break;
            }
        }

        private void DoComputeProgress(object sender, ProgressChangedEventArgs e)
        {
            BackgroundStatus.Text = (string)e.UserState;
        }

        private void DoCompute(object s, DoWorkEventArgs args)
        {
            while (!_backgroundWorker.CancellationPending) {
                ImgMdf.BackgroundWorker(_backgroundWorker);
            }

            args.Cancel = true;
        }
    }
}