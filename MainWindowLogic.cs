using System;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;

namespace ImgSoh
{
    public sealed partial class MainWindow : IDisposable
    {
        private double _picsMaxWidth;
        private double _picsMaxHeight;
        private double _labelMaxHeight;

        private readonly NotifyIcon _notifyIcon = new NotifyIcon();
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

            await Task.Run(ExifHelper.Start).ConfigureAwait(true);
            await Task.Run(() => { VggHelper.LoadNet(AppVars.Progress); }).ConfigureAwait(true);
            await Task.Run(() => { ColorHelper.LoadTable(AppVars.Progress); }).ConfigureAwait(true);
            await Task.Run(() => { AppDatabase.Load(AppVars.Progress); }).ConfigureAwait(true);
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

        private async void ExportClick()
        {
            DisableElements();
            await Task.Run(() => { ImgMdf.Export(0, AppVars.Progress); }).ConfigureAwait(true);
            await Task.Run(() => { ImgMdf.Export(1, AppVars.Progress); }).ConfigureAwait(true);
            EnableElements();
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
                if (AppDatabase.TryGetImg(panels[index].Hash, out var imgX)) {
                    if (AppDatabase.TryGetImg(panels[1 - index].Hash, out var imgY)) {
                        pBoxes[index].Source = BitmapHelper.ImageSourceFromBitmap(panels[index].Bitmap);
                        var sb = new StringBuilder();
                        var shortfilename = Helper.GetShortFileName(imgX.Folder, panels[index].Hash);
                        sb.Append($"{shortfilename}.{panels[index].Format.ToLowerInvariant()}");

                        if (imgX.HistoryCount > 0) {
                            sb.Append($" H{imgX.HistoryCount}");
                        }

                        sb.AppendLine();

                        sb.Append($"{Helper.SizeToString(panels[index].Size)} ");
                        sb.Append($" ({panels[index].Bitmap.Width}x{panels[index].Bitmap.Height})");
                        sb.AppendLine();

                        sb.Append($" {Helper.TimeIntervalToString(DateTime.Now.Subtract(imgX.LastView))} ago ");
                        sb.Append($" [{Helper.GetShortDateTaken(panels[index].DateTaken)}]");

                        pLabels[index].Text = sb.ToString();
                        pLabels[index].Background = System.Windows.Media.Brushes.White;
                        if (imgX.Verified) {
                            if (imgX.IsInHistory(imgY.Hash)) {
                                pLabels[index].Background = System.Windows.Media.Brushes.LightGreen;
                            }
                            else {
                                if (imgX.HistoryCount > 0) {
                                    pLabels[index].Background = System.Windows.Media.Brushes.Bisque;
                                }
                            }
                        }
                        else {
                            pLabels[index].Background = imgX.IsInHistory(imgY.Hash) ?
                                System.Windows.Media.Brushes.YellowGreen :
                                System.Windows.Media.Brushes.Yellow;
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
                if (panel != null && panel.Bitmap != null) {
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
            await Task.Run(() => { ImgMdf.Delete(idpanel, string.Empty, AppVars.Progress); }).ConfigureAwait(true);
            await Task.Run(() => { ImgMdf.Find(null, AppVars.Progress); }).ConfigureAwait(true);
            DrawCanvas();
            EnableElements();
        }

        private async void RotateClick(RotateFlipType rft)
        {
            DisableElements();
            var hash = AppPanels.GetImgPanel(0).Hash;
            await Task.Run(() => { ImgMdf.Rotate(hash, rft, AppVars.Progress); }).ConfigureAwait(true);
            await Task.Run(() => { ImgMdf.Find(hash, AppVars.Progress); }).ConfigureAwait(true);
            DrawCanvas();
            EnableElements();
        }

        ~MainWindow()
        {
            Dispose();
        }

        public void Dispose()
        {
            ClassDispose();
            GC.SuppressFinalize(this);
        }

        private void ClassDispose()
        {
            ExifHelper.Stop();
            _notifyIcon?.Dispose();
            _backgroundWorker?.CancelAsync();
            _backgroundWorker?.Dispose();
        }

        private void RefreshClick()
        {
            DisableElements();
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

        private void CombineToFamily()
        {
            DisableElements();
            ImgMdf.CombineToFamily();
            DrawCanvas();
            EnableElements();
        }

        private void DetachFromFamily()
        {
            DisableElements();
            ImgMdf.DetachFromFamily();
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
            Thread.CurrentThread.Priority = ThreadPriority.Lowest;
            while (!_backgroundWorker.CancellationPending) {
                ImgMdf.BackgroundWorker(_backgroundWorker);
                Thread.Sleep(10);
            }

            args.Cancel = true;
        }
    }
}