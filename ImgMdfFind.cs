﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace ImgSoh
{
    public static partial class ImgMdf
    {
        public static void Find(string hashX, IProgress<string> progress)
        {
            do {
                var totalcount = AppDatabase.ImgCount(false);
                if (totalcount < 2) {
                    progress?.Report($"totalcount = {totalcount}");
                    return;
                }

                if (hashX == null) {
                    progress.Report($"Loading hashes{AppConsts.CharEllipsis}");
                    var hashes = AppDatabase.GetHashes();
                    progress.Report($"Loading pairs{AppConsts.CharEllipsis}");
                    var pairs = AppDatabase.GetPairs();
                    var groups = new SortedList<string, DateTime>[3];
                    groups[0] = new SortedList<string, DateTime>();
                    groups[1] = new SortedList<string, DateTime>();
                    groups[2] = new SortedList<string, DateTime>(hashes);
                    foreach (var p in pairs) {
                        if (groups[2].ContainsKey(p.Item1)) {
                            groups[2].Remove(p.Item1);
                        }

                        if (!hashes.TryGetValue(p.Item1, out var lastView1)) {
                            AppDatabase.DeletePair(p.Item1);
                            continue;
                        }

                        if (!hashes.TryGetValue(p.Item2, out var lastView2)) {
                            AppDatabase.DeletePair(p.Item2);
                            continue;
                        }

                        if (p.Item3) {
                            if (groups[1].ContainsKey(p.Item1)) {
                                groups[1].Remove(p.Item1);
                            }

                            if (!groups[0].ContainsKey(p.Item1)) {
                                groups[0].Add(p.Item1, lastView1);
                            }

                            if (groups[1].ContainsKey(p.Item2)) {
                                groups[1].Remove(p.Item2);
                            }

                            if (!groups[0].ContainsKey(p.Item2)) {
                                groups[0].Add(p.Item2, lastView2);
                            }
                        }
                        else {
                            if (!groups[0].ContainsKey(p.Item1) && !groups[1].ContainsKey(p.Item1)) {
                                groups[1].Add(p.Item1, lastView1);
                            }

                            if (!groups[0].ContainsKey(p.Item2) && !groups[1].ContainsKey(p.Item2)) {
                                groups[1].Add(p.Item2, lastView2);
                            }
                        }
                    }

                    var groupindex = AppVars.RandomNext(3);
                    while (groups[groupindex].Count  == 0) {
                        groupindex = AppVars.RandomNext(3);
                    }

                    hashX = groups[groupindex].OrderBy(e => e.Value).FirstOrDefault().Key;
                }

                if (!AppPanels.SetImgPanel(0, hashX)) {
                    Delete(hashX, progress);
                    hashX = null;
                    continue;
                }

                if (AppDatabase.TryGetImgVectorLastView(hashX, out var folderX, out var vectorX, out var lastView)) {
                    if (vectorX.Length != 4096) {
                        var filename = Helper.GetFileName(folderX, hashX);
                        var imagedata = FileHelper.ReadEncryptedFile(filename);
                        using (var magickImage = BitmapHelper.ImageDataToMagickImage(imagedata))
                        using (var bitmap = BitmapHelper.MagickImageToBitmap(magickImage, RotateFlipType.RotateNoneFlipNone)) {
                            vectorX = VggHelper.CalculateVector(bitmap);
                            AppDatabase.ImgUpdateProperty(hashX, AppConsts.AttributeVector, vectorX);
                        }
                    }
                }

                progress.Report($"Loading pairs{AppConsts.CharEllipsis}");
                var pairsX = AppDatabase.GetPairs(hashX);
                var family = new List<string>();
                var notfamily = new List<string>();
                foreach (var e in pairsX) {
                    if (e.Value) {
                        family.Add(e.Key);
                    }
                    else {
                        notfamily.Add(e.Key);
                    }
                }

                string hashY = null;
                if (family.Count > 0) {
                    var random = AppVars.RandomNext(3);
                    if (random == 0) {
                        var minLastView = DateTime.MaxValue;
                        foreach (var e in family) {
                            if (AppDatabase.TryGetImgLastView(e, out var lastViewFamily)) {
                                if (hashY == null || lastViewFamily < minLastView) {
                                    hashY = e;
                                    minLastView = lastViewFamily;
                                }
                            }
                        }
                    }
                }

                if (string.IsNullOrEmpty(hashY)) {
                    if (notfamily.Count > 0) {
                        var random = AppVars.RandomNext(7);
                        if (random == 0) {
                            var minLastView = DateTime.MaxValue;
                            foreach (var e in notfamily) {
                                if (AppDatabase.TryGetImgLastView(e, out var lastViewNotFamily)) {
                                    if (hashY == null || lastViewNotFamily < minLastView) {
                                        hashY = e;
                                        minLastView = lastViewNotFamily;
                                    }
                                }
                            }
                        }
                    }
                }

                if (string.IsNullOrEmpty(hashY)) {
                    progress.Report($"Loading vectors{AppConsts.CharEllipsis}");
                    var vectors = AppDatabase.GetVectors();
                    vectors.Remove(hashX);
                    foreach (var e in pairsX.Keys) {
                        vectors.Remove(e);
                    }

                    var mindistance = float.MaxValue;
                    foreach (var e in vectors) {
                        var distance = VggHelper.GetDistance(vectorX, e.Value);
                        if (hashY == null || distance < mindistance) {
                            mindistance = distance;
                            hashY = e.Key;
                            progress.Report($"Searching {hashY} - {mindistance:F4}{AppConsts.CharEllipsis}");
                        }
                    }

                }

                if (!string.IsNullOrEmpty(hashY)) {
                    var age = Helper.TimeIntervalToString(DateTime.Now.Subtract(lastView));
                    var shortfilename = Helper.GetShortFileName(folderX, hashX);
                    var imgcount = AppDatabase.ImgCount(false);
                    var newimgcount = AppDatabase.ImgCount(true);
                    var paircount = AppDatabase.PairCount();
                    progress.Report($"{newimgcount}/p{paircount}/{imgcount}: [{age} ago] {shortfilename}: {family.Count}/{notfamily.Count}");

                    if (!AppPanels.SetImgPanel(1, hashY)) {
                        Delete(hashY, progress);
                        hashX = null;
                        continue;
                    }

                    break;
                }

                hashX = null;
            }
            while (true);
        }
    }
}
