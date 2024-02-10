namespace ImgSoh
{
    public static partial class ImgMdf
    {
        public static void CombineToFamily()
        {
            if (!AppDatabase.TryGetImg(AppPanels.GetImgPanel(0).Hash, out var imgX)) {
                return;
            }

            if (!AppDatabase.TryGetImg(AppPanels.GetImgPanel(1).Hash, out var imgY)) {
                return;
            }

            imgX.AddToHistory(imgY.Hash, '*');
            imgY.AddToHistory(imgX.Hash, '*');
        }

        public static void DetachFromFamily()
        {
            if (!AppDatabase.TryGetImg(AppPanels.GetImgPanel(0).Hash, out var imgX)) {
                return;
            }

            if (!AppDatabase.TryGetImg(AppPanels.GetImgPanel(1).Hash, out var imgY)) {
                return;
            }

            imgX.AddToHistory(imgY.Hash, imgY.Hash[0]);
            imgY.AddToHistory(imgX.Hash, imgX.Hash[0]);
        }
    }
}
