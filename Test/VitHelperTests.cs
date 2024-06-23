using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using ImgSoh;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test
{
    [TestClass]
    public class VitHelperTests
    {
            /*
onnx_node!Conv_0
onnx_node!Reshape_8
onnx_node!Transpose_9
onnx::Concat_231
onnx_node!Concat_25
vit.embeddings.position_embeddings
onnx_node!Add_26
vit.encoder.layer.0.layernorm_before.weight
vit.encoder.layer.0.layernorm_before.bias
onnx_node!Add_37
onnx_node!MatMul_38
vit.encoder.layer.0.attention.attention.query.bias
onnx_node!Add_39
onnx_node!MatMul_40
vit.encoder.layer.0.attention.attention.key.bias
onnx_node!Add_41
onnx_node!Reshape_53
onnx_node!MatMul_54
vit.encoder.layer.0.attention.attention.value.bias
onnx_node!Add_55
onnx_node!Reshape_67
onnx_node!Transpose_68
onnx_node!Reshape_80
onnx_node!Transpose_81
onnx_node!Transpose_82
onnx_node!MatMul_83
onnx::Div_312
onnx_node!Div_85
onnx_node!Softmax_86
onnx_node!MatMul_87
onnx_node!Transpose_88
onnx_node!Reshape_100
onnx_node!MatMul_101
vit.encoder.layer.0.attention.output.dense.bias
onnx_node!Add_102
onnx_node!Add_103
vit.encoder.layer.0.layernorm_after.weight
vit.encoder.layer.0.layernorm_after.bias
onnx_node!Add_114
onnx_node!MatMul_115
vit.encoder.layer.0.intermediate.dense.bias
onnx_node!Add_116
onnx_node!Mul_124
onnx_node!MatMul_125
vit.encoder.layer.0.output.dense.bias
onnx_node!Add_126
onnx_node!Add_127
vit.encoder.layer.1.layernorm_before.weight
vit.encoder.layer.1.layernorm_before.bias
onnx_node!Add_138
onnx_node!MatMul_139
vit.encoder.layer.1.attention.attention.query.bias
onnx_node!Add_140
onnx_node!MatMul_141
vit.encoder.layer.1.attention.attention.key.bias
onnx_node!Add_142
onnx_node!Reshape_154
onnx_node!MatMul_155
vit.encoder.layer.1.attention.attention.value.bias
onnx_node!Add_156
onnx_node!Reshape_168
onnx_node!Transpose_169
onnx_node!Reshape_181
onnx_node!Transpose_182
onnx_node!Transpose_183
onnx_node!MatMul_184
onnx::Div_440
onnx_node!Div_186
onnx_node!Softmax_187
onnx_node!MatMul_188
onnx_node!Transpose_189
onnx_node!Reshape_201
onnx_node!MatMul_202
vit.encoder.layer.1.attention.output.dense.bias
onnx_node!Add_203
onnx_node!Add_204
vit.encoder.layer.1.layernorm_after.weight
vit.encoder.layer.1.layernorm_after.bias
onnx_node!Add_215
onnx_node!MatMul_216
vit.encoder.layer.1.intermediate.dense.bias
onnx_node!Add_217
onnx_node!Mul_225
onnx_node!MatMul_226
vit.encoder.layer.1.output.dense.bias
onnx_node!Add_227
onnx_node!Add_228
vit.encoder.layer.2.layernorm_before.weight
vit.encoder.layer.2.layernorm_before.bias
onnx_node!Add_239
onnx_node!MatMul_240
vit.encoder.layer.2.attention.attention.query.bias
onnx_node!Add_241
onnx_node!MatMul_242
vit.encoder.layer.2.attention.attention.key.bias
onnx_node!Add_243
onnx_node!Reshape_255
onnx_node!MatMul_256
vit.encoder.layer.2.attention.attention.value.bias
onnx_node!Add_257
onnx_node!Reshape_269
onnx_node!Transpose_270
onnx_node!Reshape_282
onnx_node!Transpose_283
onnx_node!Transpose_284
onnx_node!MatMul_285
onnx::Div_568
onnx_node!Div_287
onnx_node!Softmax_288
onnx_node!MatMul_289
onnx_node!Transpose_290
onnx_node!Reshape_302
onnx_node!MatMul_303
vit.encoder.layer.2.attention.output.dense.bias
onnx_node!Add_304
onnx_node!Add_305
vit.encoder.layer.2.layernorm_after.weight
vit.encoder.layer.2.layernorm_after.bias
onnx_node!Add_316
onnx_node!MatMul_317
vit.encoder.layer.2.intermediate.dense.bias
onnx_node!Add_318
onnx_node!Mul_326
onnx_node!MatMul_327
vit.encoder.layer.2.output.dense.bias
onnx_node!Add_328
onnx_node!Add_329
vit.encoder.layer.3.layernorm_before.weight
vit.encoder.layer.3.layernorm_before.bias
onnx_node!Add_340
onnx_node!MatMul_341
vit.encoder.layer.3.attention.attention.query.bias
onnx_node!Add_342
onnx_node!MatMul_343
vit.encoder.layer.3.attention.attention.key.bias
onnx_node!Add_344
onnx_node!Reshape_356
onnx_node!MatMul_357
vit.encoder.layer.3.attention.attention.value.bias
onnx_node!Add_358
onnx_node!Reshape_370
onnx_node!Transpose_371
onnx_node!Reshape_383
onnx_node!Transpose_384
onnx_node!Transpose_385
onnx_node!MatMul_386
onnx::Div_696
onnx_node!Div_388
onnx_node!Softmax_389
onnx_node!MatMul_390
onnx_node!Transpose_391
onnx_node!Reshape_403
onnx_node!MatMul_404
vit.encoder.layer.3.attention.output.dense.bias
onnx_node!Add_405
onnx_node!Add_406
vit.encoder.layer.3.layernorm_after.weight
vit.encoder.layer.3.layernorm_after.bias
onnx_node!Add_417
onnx_node!MatMul_418
vit.encoder.layer.3.intermediate.dense.bias
onnx_node!Add_419
onnx_node!Mul_427
onnx_node!MatMul_428
vit.encoder.layer.3.output.dense.bias
onnx_node!Add_429
onnx_node!Add_430
vit.encoder.layer.4.layernorm_before.weight
vit.encoder.layer.4.layernorm_before.bias
onnx_node!Add_441
onnx_node!MatMul_442
vit.encoder.layer.4.attention.attention.query.bias
onnx_node!Add_443
onnx_node!MatMul_444
vit.encoder.layer.4.attention.attention.key.bias
onnx_node!Add_445
onnx_node!Reshape_457
onnx_node!MatMul_458
vit.encoder.layer.4.attention.attention.value.bias
onnx_node!Add_459
onnx_node!Reshape_471
onnx_node!Transpose_472
onnx_node!Reshape_484
onnx_node!Transpose_485
onnx_node!Transpose_486
onnx_node!MatMul_487
onnx::Div_824
onnx_node!Div_489
onnx_node!Softmax_490
onnx_node!MatMul_491
onnx_node!Transpose_492
onnx_node!Reshape_504
onnx_node!MatMul_505
vit.encoder.layer.4.attention.output.dense.bias
onnx_node!Add_506
onnx_node!Add_507
vit.encoder.layer.4.layernorm_after.weight
vit.encoder.layer.4.layernorm_after.bias
onnx_node!Add_518
onnx_node!MatMul_519
vit.encoder.layer.4.intermediate.dense.bias
onnx_node!Add_520
onnx_node!Mul_528
onnx_node!MatMul_529
vit.encoder.layer.4.output.dense.bias
onnx_node!Add_530
onnx_node!Add_531
vit.encoder.layer.5.layernorm_before.weight
vit.encoder.layer.5.layernorm_before.bias
onnx_node!Add_542
onnx_node!MatMul_543
vit.encoder.layer.5.attention.attention.query.bias
onnx_node!Add_544
onnx_node!MatMul_545
vit.encoder.layer.5.attention.attention.key.bias
onnx_node!Add_546
onnx_node!Reshape_558
onnx_node!MatMul_559
vit.encoder.layer.5.attention.attention.value.bias
onnx_node!Add_560
onnx_node!Reshape_572
onnx_node!Transpose_573
onnx_node!Reshape_585
onnx_node!Transpose_586
onnx_node!Transpose_587
onnx_node!MatMul_588
onnx::Div_952
onnx_node!Div_590
onnx_node!Softmax_591
onnx_node!MatMul_592
onnx_node!Transpose_593
onnx_node!Reshape_605
onnx_node!MatMul_606
vit.encoder.layer.5.attention.output.dense.bias
onnx_node!Add_607
onnx_node!Add_608
vit.encoder.layer.5.layernorm_after.weight
vit.encoder.layer.5.layernorm_after.bias
onnx_node!Add_619
onnx_node!MatMul_620
vit.encoder.layer.5.intermediate.dense.bias
onnx_node!Add_621
onnx_node!Mul_629
onnx_node!MatMul_630
vit.encoder.layer.5.output.dense.bias
onnx_node!Add_631
onnx_node!Add_632
vit.encoder.layer.6.layernorm_before.weight
vit.encoder.layer.6.layernorm_before.bias
onnx_node!Add_643
onnx_node!MatMul_644
vit.encoder.layer.6.attention.attention.query.bias
onnx_node!Add_645
onnx_node!MatMul_646
vit.encoder.layer.6.attention.attention.key.bias
onnx_node!Add_647
onnx_node!Reshape_659
onnx_node!MatMul_660
vit.encoder.layer.6.attention.attention.value.bias
onnx_node!Add_661
onnx_node!Reshape_673
onnx_node!Transpose_674
onnx_node!Reshape_686
onnx_node!Transpose_687
onnx_node!Transpose_688
onnx_node!MatMul_689
onnx::Div_1080
onnx_node!Div_691
onnx_node!Softmax_692
onnx_node!MatMul_693
onnx_node!Transpose_694
onnx_node!Reshape_706
onnx_node!MatMul_707
vit.encoder.layer.6.attention.output.dense.bias
onnx_node!Add_708
onnx_node!Add_709
vit.encoder.layer.6.layernorm_after.weight
vit.encoder.layer.6.layernorm_after.bias
onnx_node!Add_720
onnx_node!MatMul_721
vit.encoder.layer.6.intermediate.dense.bias
onnx_node!Add_722
onnx_node!Mul_730
onnx_node!MatMul_731
vit.encoder.layer.6.output.dense.bias
onnx_node!Add_732
onnx_node!Add_733
vit.encoder.layer.7.layernorm_before.weight
vit.encoder.layer.7.layernorm_before.bias
onnx_node!Add_744
onnx_node!MatMul_745
vit.encoder.layer.7.attention.attention.query.bias
onnx_node!Add_746
onnx_node!MatMul_747
vit.encoder.layer.7.attention.attention.key.bias
onnx_node!Add_748
onnx_node!Reshape_760
onnx_node!MatMul_761
vit.encoder.layer.7.attention.attention.value.bias
onnx_node!Add_762
onnx_node!Reshape_774
onnx_node!Transpose_775
onnx_node!Reshape_787
onnx_node!Transpose_788
onnx_node!Transpose_789
onnx_node!MatMul_790
onnx::Div_1208
onnx_node!Div_792
onnx_node!Softmax_793
onnx_node!MatMul_794
onnx_node!Transpose_795
onnx_node!Reshape_807
onnx_node!MatMul_808
vit.encoder.layer.7.attention.output.dense.bias
onnx_node!Add_809
onnx_node!Add_810
vit.encoder.layer.7.layernorm_after.weight
vit.encoder.layer.7.layernorm_after.bias
onnx_node!Add_821
onnx_node!MatMul_822
vit.encoder.layer.7.intermediate.dense.bias
onnx_node!Add_823
onnx_node!Mul_831
onnx_node!MatMul_832
vit.encoder.layer.7.output.dense.bias
onnx_node!Add_833
onnx_node!Add_834
vit.encoder.layer.8.layernorm_before.weight
vit.encoder.layer.8.layernorm_before.bias
onnx_node!Add_845
onnx_node!MatMul_846
vit.encoder.layer.8.attention.attention.query.bias
onnx_node!Add_847
onnx_node!MatMul_848
vit.encoder.layer.8.attention.attention.key.bias
onnx_node!Add_849
onnx_node!Reshape_861
onnx_node!MatMul_862
vit.encoder.layer.8.attention.attention.value.bias
onnx_node!Add_863
onnx_node!Reshape_875
onnx_node!Transpose_876
onnx_node!Reshape_888
onnx_node!Transpose_889
onnx_node!Transpose_890
onnx_node!MatMul_891
onnx::Div_1336
onnx_node!Div_893
onnx_node!Softmax_894
onnx_node!MatMul_895
onnx_node!Transpose_896
onnx_node!Reshape_908
onnx_node!MatMul_909
vit.encoder.layer.8.attention.output.dense.bias
onnx_node!Add_910
onnx_node!Add_911
vit.encoder.layer.8.layernorm_after.weight
vit.encoder.layer.8.layernorm_after.bias
onnx_node!Add_922
onnx_node!MatMul_923
vit.encoder.layer.8.intermediate.dense.bias
onnx_node!Add_924
onnx_node!Mul_932
onnx_node!MatMul_933
vit.encoder.layer.8.output.dense.bias
onnx_node!Add_934
onnx_node!Add_935
vit.encoder.layer.9.layernorm_before.weight
vit.encoder.layer.9.layernorm_before.bias
onnx_node!Add_946
onnx_node!MatMul_947
vit.encoder.layer.9.attention.attention.query.bias
onnx_node!Add_948
onnx_node!MatMul_949
vit.encoder.layer.9.attention.attention.key.bias
onnx_node!Add_950
onnx_node!Reshape_962
onnx_node!MatMul_963
vit.encoder.layer.9.attention.attention.value.bias
onnx_node!Add_964
onnx_node!Reshape_976
onnx_node!Transpose_977
onnx_node!Reshape_989
onnx_node!Transpose_990
onnx_node!Transpose_991
onnx_node!MatMul_992
onnx::Div_1464
onnx_node!Div_994
onnx_node!Softmax_995
onnx_node!MatMul_996
onnx_node!Transpose_997
onnx_node!Reshape_1009
onnx_node!MatMul_1010
vit.encoder.layer.9.attention.output.dense.bias
onnx_node!Add_1011
onnx_node!Add_1012
vit.encoder.layer.9.layernorm_after.weight
vit.encoder.layer.9.layernorm_after.bias
onnx_node!Add_1023
onnx_node!MatMul_1024
vit.encoder.layer.9.intermediate.dense.bias
onnx_node!Add_1025
onnx_node!Mul_1033
onnx_node!MatMul_1034
vit.encoder.layer.9.output.dense.bias
onnx_node!Add_1035
onnx_node!Add_1036
vit.encoder.layer.10.layernorm_before.weight
vit.encoder.layer.10.layernorm_before.bias
onnx_node!Add_1047
onnx_node!MatMul_1048
vit.encoder.layer.10.attention.attention.query.bias
onnx_node!Add_1049
onnx_node!MatMul_1050
vit.encoder.layer.10.attention.attention.key.bias
onnx_node!Add_1051
onnx_node!Reshape_1063
onnx_node!MatMul_1064
vit.encoder.layer.10.attention.attention.value.bias
onnx_node!Add_1065
onnx_node!Reshape_1077
onnx_node!Transpose_1078
onnx_node!Reshape_1090
onnx_node!Transpose_1091
onnx_node!Transpose_1092
onnx_node!MatMul_1093
onnx::Div_1592
onnx_node!Div_1095
onnx_node!Softmax_1096
onnx_node!MatMul_1097
onnx_node!Transpose_1098
onnx_node!Reshape_1110
onnx_node!MatMul_1111
vit.encoder.layer.10.attention.output.dense.bias
onnx_node!Add_1112
onnx_node!Add_1113
vit.encoder.layer.10.layernorm_after.weight
vit.encoder.layer.10.layernorm_after.bias
onnx_node!Add_1124
onnx_node!MatMul_1125
vit.encoder.layer.10.intermediate.dense.bias
onnx_node!Add_1126
onnx_node!Mul_1134
onnx_node!MatMul_1135
vit.encoder.layer.10.output.dense.bias
onnx_node!Add_1136
onnx_node!Add_1137
vit.encoder.layer.11.layernorm_before.weight
vit.encoder.layer.11.layernorm_before.bias
onnx_node!Add_1148
onnx_node!MatMul_1149
vit.encoder.layer.11.attention.attention.query.bias
onnx_node!Add_1150
onnx_node!MatMul_1151
vit.encoder.layer.11.attention.attention.key.bias
onnx_node!Add_1152
onnx_node!Reshape_1164
onnx_node!MatMul_1165
vit.encoder.layer.11.attention.attention.value.bias
onnx_node!Add_1166
onnx_node!Reshape_1178
onnx_node!Transpose_1179
onnx_node!Reshape_1191
onnx_node!Transpose_1192
onnx_node!Transpose_1193
onnx_node!MatMul_1194
onnx::Div_1720
onnx_node!Div_1196
onnx_node!Softmax_1197
onnx_node!MatMul_1198
onnx_node!Transpose_1199
onnx_node!Reshape_1211
onnx_node!MatMul_1212
vit.encoder.layer.11.attention.output.dense.bias
onnx_node!Add_1213
onnx_node!Add_1214
vit.encoder.layer.11.layernorm_after.weight
vit.encoder.layer.11.layernorm_after.bias
onnx_node!Add_1225
onnx_node!MatMul_1226
vit.encoder.layer.11.intermediate.dense.bias
onnx_node!Add_1227
onnx_node!Mul_1235
onnx_node!MatMul_1236
vit.encoder.layer.11.output.dense.bias
onnx_node!Add_1237
onnx_node!Add_1238
vit.layernorm.weight
vit.layernorm.bias
onnx_node!Add_1249
onnx::Gather_1781
onnx_node!Gather_1251
onnx_node!Gemm_1252
logits
            */

        [TestMethod]
        public void Single()
        {
            AppVit.LoadNet(null);
            var name1 = $"DataSet1\\gab_org.jpg";
            var imagedata1 = File.ReadAllBytes(name1);
            using (var magickImage = AppBitmap.ImageDataToMagickImage(imagedata1))
            using (var bitmap = AppBitmap.MagickImageToBitmap(magickImage, RotateFlipType.RotateNoneFlipNone)) {
                var vector = AppVit.CalculateVector(bitmap);
            }
        }


        [TestMethod]
        public void GetDistanceSingle()
        {
            AppVit.LoadNet(null);
            var images = new[] {
                "gab_org", "gab_bw", "gab_scale", "gab_flip", "gab_r90", "gab_crop", "gab_toside",
                "gab_blur", "gab_exp", "gab_logo", "gab_noice", "gab_r3", "gab_r10",
                "gab_face", "gab_sim1", "gab_sim2",
                "gab_nosim1", "gab_nosim2", "gab_nosim3", "gab_nosim4", "gab_nosim5", "gab_nosim6"
            };

            var vectors = new Tuple<string, IEnumerable<float>, float>[images.Length];
            for (var i = 0; i < images.Length; i++) {
                var name = $"DataSet1\\{images[i]}.jpg";
                var imagedata = File.ReadAllBytes(name);
                using (var magickImage = AppBitmap.ImageDataToMagickImage(imagedata))
                using (var bitmap = AppBitmap.MagickImageToBitmap(magickImage, RotateFlipType.RotateNoneFlipNone)) {
                    var vector = AppVit.CalculateVector(bitmap).ToArray();
                    var magnitude = AppVit.GetMagnitude(vector);
                    vectors[i] = new Tuple<string, IEnumerable<float>, float>(name, vector, magnitude);
                }
            }

            for (var i = 0; i < vectors.Length; i++) {
                var distance = AppVit.GetDistance(vectors[0].Item2, vectors[0].Item3, vectors[i].Item2, vectors[i].Item3);
                Debug.WriteLine($"{images[i]} = {distance:F4}");
            }

            /*
vgg:
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
vit:
gab_org = 0.0000
gab_bw = 0.1593
gab_scale = 0.0032
gab_flip = 0.0161
gab_r90 = 0.1460
gab_crop = 0.0623
gab_toside = 0.2091
gab_blur = 0.0250
gab_exp = 0.0169
gab_logo = 0.0000
gab_noice = 0.0513
gab_r3 = 0.0536
gab_r10 = 0.1113
gab_face = 0.3257
gab_sim1 = 0.1495
gab_sim2 = 0.3518
gab_nosim1 = 0.6151
gab_nosim2 = 0.4668
gab_nosim3 = 0.5458
gab_nosim4 = 0.3748
gab_nosim5 = 0.4693
gab_nosim6 = 0.7024
             */
        }
    }
}
