using UnityEngine;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;

public class ColorMaskDetector
{
    public static (Texture2D, Texture2D) CreateColorMask(Texture2D texture, Color targetColor,
                                         float colorThreshold = 10f, int blurSize = 3)
    {
        if (texture == null)
        {
            Debug.LogError("Input texture is null!");
            return default;
        }

        Mat rgbMat = new Mat(texture.height, texture.width, CvType.CV_8UC4);
        Utils.texture2DToMat(texture, rgbMat, true);  // 第二个参数flip表示是否翻转

        Mat hsvMat = new Mat();
        Imgproc.cvtColor(rgbMat, hsvMat, Imgproc.COLOR_RGBA2RGB);
        Imgproc.cvtColor(hsvMat, hsvMat, Imgproc.COLOR_RGB2HSV);

        Color.RGBToHSV(targetColor, out float h, out float s, out float v);
        int hValue = (int)(h * 180);
        int sValue = (int)(s * 255);
        int vValue = (int)(v * 255);
        float hueRange = colorThreshold * 1.8f;

        Mat mask = CreateHSVMask(hsvMat, hValue, sValue, vValue, hueRange);

        mask = PostProcessMask(mask, blurSize);

        var result = CreateCorrectedTexture(texture, mask, targetColor);

        rgbMat.Dispose();
        hsvMat.Dispose();
        mask.Dispose();

        return result;
    }

    private static Mat CreateHSVMask(Mat hsvMat, int hValue, int sValue, int vValue, float hueRange)
    {
        Mat mask = new Mat();
        
        if (hValue - hueRange/2 < 0 || hValue + hueRange/2 > 180)
        {
            Mat mask1 = new Mat(), mask2 = new Mat();
            Scalar lower1, upper1, lower2, upper2;
            
            if (hValue - hueRange/2 < 0)
            {
                lower1 = new Scalar(0, sValue * 0.7, vValue * 0.7);
                upper1 = new Scalar(hValue + hueRange/2, 255, 255);
                lower2 = new Scalar(180 + (hValue - hueRange/2), sValue * 0.7, vValue * 0.7);
                upper2 = new Scalar(180, 255, 255);
            }
            else
            {
                lower1 = new Scalar(hValue - hueRange/2, sValue * 0.7, vValue * 0.7);
                upper1 = new Scalar(180, 255, 255);
                lower2 = new Scalar(0, sValue * 0.7, vValue * 0.7);
                upper2 = new Scalar(hValue + hueRange/2 - 180, 255, 255);
            }
            
            Core.inRange(hsvMat, lower1, upper1, mask1);
            Core.inRange(hsvMat, lower2, upper2, mask2);
            Core.bitwise_or(mask1, mask2, mask);
            mask1.Dispose();
            mask2.Dispose();
        }
        else
        {
            Scalar lower = new Scalar(
                Mathf.Max(0, hValue - hueRange/2),
                sValue * 0.7,
                vValue * 0.7
            );
            
            Scalar upper = new Scalar(
                Mathf.Min(180, hValue + hueRange/2),
                255,
                255
            );
            
            Core.inRange(hsvMat, lower, upper, mask);
        }
        
        return mask;
    }

    private static Mat PostProcessMask(Mat mask, int blurSize)
    {
        if (blurSize > 0 && blurSize % 2 == 1)
        {
            Mat blurred = new Mat();
            Imgproc.GaussianBlur(mask, blurred, new Size(blurSize, blurSize), 0);
            mask = blurred;
        }

        Mat kernel = Imgproc.getStructuringElement(Imgproc.MORPH_ELLIPSE, new Size(5, 5));
        Imgproc.morphologyEx(mask, mask, Imgproc.MORPH_CLOSE, kernel);
        Imgproc.morphologyEx(mask, mask, Imgproc.MORPH_OPEN, kernel);
        kernel.Dispose();

        return mask;
    }

    private static (Texture2D, Texture2D) CreateCorrectedTexture(Texture2D sourceTexture, Mat mask, Color targetColor)
    {
        Texture2D tempTexture = new Texture2D(mask.cols(), mask.rows(), TextureFormat.R8, false);
        Utils.matToTexture2D(mask, tempTexture, true);
        
        Texture2D resultTexture = new Texture2D(
            sourceTexture.width, 
            sourceTexture.height, 
            TextureFormat.RGBA32, 
            false);
        Texture2D resultTexture_DebugTex = new Texture2D(
            sourceTexture.width, 
            sourceTexture.height, 
            TextureFormat.RGBA32, 
            false);
        
        Color[] maskPixels = tempTexture.GetPixels();
        Color[] sourcePixels = sourceTexture.GetPixels();
        Color[] sourcePixels_DebugTex = sourceTexture.GetPixels();
        
        for (int i = 0; i < sourcePixels.Length; i++)
        {
            if (GameMgr.Ins.dalleAPI.Model == ModelType.gpt_image_1)
            {
                sourcePixels[i] = (maskPixels[i].r > 0.5f) ? new Color(0,0,0,0) : sourcePixels[i];
            }
            else
            {
                sourcePixels[i] = (maskPixels[i].r > 0.5f) ? new Color(0,0,0,0) : sourcePixels[i];
            }
            
            sourcePixels_DebugTex[i] = (maskPixels[i].r > 0.5f) ? targetColor : new Color(0,0,0,0);
        }
        
        resultTexture.SetPixels(sourcePixels);
        resultTexture.Apply();
        
        resultTexture_DebugTex.SetPixels(sourcePixels_DebugTex);
        resultTexture_DebugTex.Apply();
        
        UnityEngine.Object.DestroyImmediate(tempTexture);
        return (resultTexture, resultTexture_DebugTex);
    }
}