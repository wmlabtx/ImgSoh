using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using ImgSoh;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test
{
    [TestClass]
    public class VggHelperTests
    {
        [TestMethod]
        public void GetInfo()
        {
            VggHelper.LoadNet(null);
            var layers = VggHelper.GetInfo();
            foreach (var layer in layers) {
                Debug.WriteLine(layer);
            }

            /*
onnx_node!vgg0_conv0_fwd
onnx_node!vgg0_relu0_fwd
onnx_node!vgg0_conv1_fwd
onnx_node!vgg0_relu1_fwd
onnx_node!vgg0_pool0_fwd
onnx_node!vgg0_conv2_fwd
onnx_node!vgg0_relu2_fwd
onnx_node!vgg0_conv3_fwd
onnx_node!vgg0_relu3_fwd
onnx_node!vgg0_pool1_fwd
onnx_node!vgg0_conv4_fwd
onnx_node!vgg0_relu4_fwd
onnx_node!vgg0_conv5_fwd
onnx_node!vgg0_relu5_fwd
onnx_node!vgg0_conv6_fwd
onnx_node!vgg0_relu6_fwd
onnx_node!vgg0_pool2_fwd
onnx_node!vgg0_conv7_fwd
onnx_node!vgg0_relu7_fwd
onnx_node!vgg0_conv8_fwd
onnx_node!vgg0_relu8_fwd
onnx_node!vgg0_conv9_fwd
onnx_node!vgg0_relu9_fwd
onnx_node!vgg0_pool3_fwd
onnx_node!vgg0_conv10_fwd
onnx_node!vgg0_relu10_fwd
onnx_node!vgg0_conv11_fwd
onnx_node!vgg0_relu11_fwd
onnx_node!vgg0_conv12_fwd
onnx_node!vgg0_relu12_fwd
onnx_node!vgg0_pool4_fwd
onnx_node!flatten_60/flatten
onnx_node!flatten_60
onnx_node!vgg0_dense0_fwd
onnx_node!vgg0_dense0_relu_fwd
onnx_node!vgg0_dropout0_fwd
onnx_node!flatten_65/flatten
onnx_node!flatten_65
onnx_node!vgg0_dense1_fwd
onnx_node!vgg0_dense1_relu_fwd
onnx_node!vgg0_dropout1_fwd
onnx_node!flatten_70/flatten
onnx_node!flatten_70
onnx_node!vgg0_dense2_fwd
vgg0_dense2_fwd
            */
        }

        [TestMethod]
        public void Single()
        {
            VggHelper.LoadNet(null);
            var name = $"DataSet1\\gab_org.jpg";
            var imagedata = File.ReadAllBytes(name);
            using (var magickImage = BitmapHelper.ImageDataToMagickImage(imagedata))
            using (var bitmap = BitmapHelper.MagickImageToBitmap(magickImage, RotateFlipType.RotateNoneFlipNone)) {
                var vector = VggHelper.CalculateFloatVector(bitmap);
            }
        }

        [TestMethod]
        public void GetDistance1()
        {
            VggHelper.LoadNet(null);
            var images = new[] {
                "gab_org", "gab_bw", "gab_scale", "gab_flip", "gab_r90", "gab_crop", "gab_toside",
                "gab_blur", "gab_exp", "gab_logo", "gab_noice", "gab_r3", "gab_r10",
                "gab_face", "gab_sim1", "gab_sim2",
                "gab_nosim1", "gab_nosim2", "gab_nosim3", "gab_nosim4", "gab_nosim5", "gab_nosim6"
            };

            var vectors = new Tuple<string, byte[]>[images.Length];
            for (var i = 0; i < images.Length; i++) {
                var name = $"DataSet1\\{images[i]}.jpg";
                var imagedata = File.ReadAllBytes(name);
                using (var magickImage = BitmapHelper.ImageDataToMagickImage(imagedata))
                using (var bitmap = BitmapHelper.MagickImageToBitmap(magickImage, RotateFlipType.RotateNoneFlipNone)) {
                    var vector = VggHelper.CalculateVector(bitmap);
                    vectors[i] = new Tuple<string, byte[]>(name, vector);
                }
            }

            for (var i = 0; i < vectors.Length; i++) {
                var distance = VggHelper.GetDistance(vectors[0].Item2, vectors[i].Item2);
                Debug.WriteLine($"{images[i]} = {distance:F4}");
            }

            /*
float:
gab_org = 0.0000
gab_bw = 0.1705
gab_scale = 0.0033
gab_flip = 0.1451
gab_r90 = 0.3838
gab_crop = 0.1227
gab_toside = 0.4747
gab_blur = 0.0434
gab_exp = 0.0120
gab_logo = 0.0001
gab_noice = 0.1938
gab_r3 = 0.1014
gab_r10 = 0.3155
gab_face = 0.7730
gab_sim1 = 0.3489
gab_sim2 = 0.5505
gab_nosim1 = 0.8048
gab_nosim2 = 0.8090
gab_nosim3 = 0.8738
gab_nosim4 = 0.7431
gab_nosim5 = 0.8146
gab_nosim6 = 0.8672

byte:
gab_org = 0.0000
gab_bw = 0.1722
gab_scale = 0.0033
gab_flip = 0.1469
gab_r90 = 0.3872
gab_crop = 0.1239
gab_toside = 0.4790
gab_blur = 0.0441
gab_exp = 0.0121
gab_logo = 0.0002
gab_noice = 0.1957
gab_r3 = 0.1023
gab_r10 = 0.3180
gab_face = 0.7761
gab_sim1 = 0.3517
gab_sim2 = 0.5556
gab_nosim1 = 0.8088
gab_nosim2 = 0.8119
gab_nosim3 = 0.8764
gab_nosim4 = 0.7475
gab_nosim5 = 0.8175
gab_nosim6 = 0.8706
             */
        }
    }
}
