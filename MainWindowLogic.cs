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
            await Task.Run(() => { AppImgs.LoadNamesAndVectors(AppConsts.FileDatabase, AppVars.Progress); }).ConfigureAwait(true);
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
            ImgPanelDeleteLeft();
        }

        private void PictureRightBoxMouseClick()
        {
            ImgPanelDeleteRight();
        }

        private async void ButtonLeftNextMouseClick()
        {
            DisableElements();
            await Task.Run(AppPanels.Confirm).ConfigureAwait(true);
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

        private void  DrawCanvas()
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
                pBoxes[index].Source = AppBitmap.ImageSourceFromBitmap(panels[index].Bitmap);
                var sb = new StringBuilder();
                sb.Append($"{panels[index].Img.Name}.{panels[index].Format}");

                if (panels[index].Img.Family.Length > 0) {
                    sb.Append($" [{panels[index].Img.Family}:{panels[index].FamilySize}:{panels[index].Img.Count}]");
                }
                else {
                    if (panels[index].Img.Count > 0) {
                        sb.Append($" [{panels[index].Img.Count}]");
                    }
                }

                sb.AppendLine();

                sb.Append($"{Helper.SizeToString(panels[index].Size)} ");
                sb.Append($" ({panels[index].Bitmap.Width}x{panels[index].Bitmap.Height})");
                sb.AppendLine();

                sb.Append($" {Helper.TimeIntervalToString(DateTime.Now.Subtract(panels[index].Img.LastView))} ago ");
                if (panels[index].Img.Meta > 0) {
                    sb.Append($" [{panels[index].Img.Meta}]");
                }

                if (!panels[index].Img.Taken.Equals(DateTime.MinValue)) {
                    sb.Append($" {panels[index].Img.Taken}");
                }

                pLabels[index].Text = sb.ToString();
                pLabels[index].Background = System.Windows.Media.Brushes.White;
                if (panels[index].IsVictim) {
                      pLabels[index].Background = System.Windows.Media.Brushes.Red;
                }
                else {
                    if (panels[index].Img.Family.Length > 0 && panels[index].Img.Family.Equals(panels[1 - index].Img.Family)) {
                        pLabels[index].Background = System.Windows.Media.Brushes.LightGreen;
                    }
                    else {
                        if (panels[index].Img.History.Length > 0) {
                            pLabels[index].Background = System.Windows.Media.Brushes.Bisque;
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

        private async void ImgPanelDeleteLeft()
        {
            DisableElements();
            await Task.Run(AppPanels.DeleteLeft).ConfigureAwait(true);
            await Task.Run(() => { ImgMdf.Find(null, AppVars.Progress); }).ConfigureAwait(true);
            DrawCanvas();
            EnableElements();
        }

        private async void ImgPanelDeleteRight()
        {
            DisableElements();
            await Task.Run(AppPanels.DeleteRight).ConfigureAwait(true);
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
            AppPanels.UpdateRightPanel();
            DrawCanvas();
            EnableElements();
        }

        private void MoveRight()
        {
            DisableElements();
            AppPanels.MoveRight();
            DrawCanvas();
            EnableElements();
        }

        private void MoveLeft()
        {
            DisableElements();
            AppPanels.MoveLeft();
            DrawCanvas();
            EnableElements();
        }

        private void MoveToTheFirst()
        {
            DisableElements();
            AppPanels.MoveToTheFirst();
            DrawCanvas();
            EnableElements();
        }

        private void MoveToTheLast()
        {
            DisableElements();
            AppPanels.MoveToTheLast();
            DrawCanvas();
            EnableElements();
        }

        private async void CombineToFamily()
        {
            DisableElements();
            await Task.Run(AppPanels.CombineToFamily).ConfigureAwait(true);
            DrawCanvas();
            EnableElements();
        }

        private async void DetachFromFamily()
        {
            DisableElements();
            await Task.Run(AppPanels.DetachFromFamily).ConfigureAwait(true);
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
                case Key.Left: 
                    MoveLeft();
                    break;
                case Key.Right:
                    MoveRight();
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