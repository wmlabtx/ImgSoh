﻿using System;

namespace ImgSoh
{
    public static partial class ImgMdf
    {
        public static void LoadImages(IProgress<string> progress)
        {
            AppVit.LoadNet(progress);
        }
    }
}