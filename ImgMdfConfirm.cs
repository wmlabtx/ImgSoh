using System;

namespace ImgSoh
{
    public static partial class ImgMdf
    {
        public static void Confirm(int idpanel)
        {
            var hash = AppPanels.GetImgPanel(idpanel).Hash;
            AppDatabase.ImgUpdateProperty(hash, AppConsts.AttributeLastView, DateTime.Now);
        }
    }
} 