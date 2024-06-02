using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ImgSoh
{
    public sealed partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            Trace.Listeners.Add(new ConsoleTraceListener());
        }

        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            WindowLoaded();
        }

        protected override void OnStateChanged(EventArgs e)
        {
            OnStateChanged();
        }

        private void PictureLeftBoxMouseClick(object sender, MouseEventArgs e)
        {
            if (e.RightButton == MouseButtonState.Pressed) {
                PictureLeftBoxMouseClick();
            }
        }

        private void PictureRightBoxMouseClick(object sender, MouseEventArgs e)
        {
            if (e.RightButton == MouseButtonState.Pressed) {
                PictureRightBoxMouseClick();
            }
        }

        private void ButtonLeftNextMouseClick(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed) {
                ButtonLeftNextMouseClick();
            }
        }

        private void ButtonRightNextMouseClick(object sender, MouseEventArgs e)
        {
        }

        private void RotateClick(object sender, EventArgs e)
        {
            var stag = (string)((MenuItem)sender).Tag;
            var tag = byte.Parse(stag);
            var rft = (RotateFlipType)tag;
            RotateClick(rft);
        }


        private void ExitClick(object sender, EventArgs e)
        {
            Close();
        }

        private void ImportClick(object sender, RoutedEventArgs e)
        {
            ImportClick();
        }

        private void RefreshClick(object sender, RoutedEventArgs e)
        {
            RefreshClick();
        }

        private void AddToFamilyClick(object sender, RoutedEventArgs e)
        {
            CombineToFamily();
        }

        private void RemoveFromFamilyClick(object sender, RoutedEventArgs e)
        {
            DetachFromFamily();
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            OnKeyDown(e.Key);
        }

        private void ToggleXorClick(object sender, RoutedEventArgs e)
        {
            ToggleXorClick();
        }

        private void MoveClick(object sender, RoutedEventArgs e)
        {
            var stag = (string)((MenuItem)sender).Tag;
            var tag = int.Parse(stag);
            Move(tag);
        }
    }
}
