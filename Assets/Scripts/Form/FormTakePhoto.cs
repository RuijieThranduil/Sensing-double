using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class FormTakePhoto : Form
{
    public GameObject panel_TakePhoto;
    public GameObject panel_Confirm;
    public GameObject panel_Photo;
    public GameObject panel_Generating;
    public GameObject panel_Complete;

    public RawImage img_CameraShow;
    public RawImage img_Photo;
    public RawImage img_GenerateResult;
    public RawImage imgDebug_InputPhoto;
    
    public TakePhotoCamera takePhotoCamera;
    
    private List<(Texture2D, Texture2D)> maskResults = new List<(Texture2D, Texture2D)>();
    private List<GameObject> maskResultsGos = new List<GameObject>();

    protected override void OnShow()
    {
        if (GameMgr.Ins.DefaultTexture)
        {
            img_GenerateResult.texture = GameMgr.Ins.DefaultTexture;
            imgDebug_InputPhoto.texture = img_GenerateResult.texture;
        }
    }

    protected override void OnReset()
    {
        panel_TakePhoto.SetActive(true);
        panel_Confirm.SetActive(false);
        panel_Photo.SetActive(false);
        panel_Complete.SetActive(false);
    }

    // PanelTakePhoto ===============================
    public void OnClickTakePhoto()
    {
        Texture2D texPhoto = takePhotoCamera.TakePhoto();
        //img_Photo.rectTransform.localEulerAngles = takePhotoCamera.rawImage.rectTransform.localEulerAngles;
        img_Photo.texture = texPhoto;
        if (GameMgr.Ins.DefaultTexture)
        {
            img_GenerateResult.texture = GameMgr.Ins.DefaultTexture;
        }
        else
        {
            img_GenerateResult.texture = texPhoto;
        }
        
        img_GenerateResult.texture = StretchTexture(img_GenerateResult.texture as Texture2D, 1024, 1024);
        imgDebug_InputPhoto.texture = img_GenerateResult.texture;

        foreach (var config in GameMgr.Ins.PromptConfigs)
        {
            var tuple_texMask =
                ColorMaskDetector.CreateColorMask(img_Photo.texture as Texture2D, config.maskColor, 11, 3);
            maskResults.Add(tuple_texMask);
            
            RawImage img_ResultDebug = Instantiate(imgDebug_InputPhoto, imgDebug_InputPhoto.transform.parent);
            img_ResultDebug.texture = tuple_texMask.Item2;
            img_ResultDebug.name = "maskResult";
            
            maskResultsGos.Add(img_ResultDebug.gameObject);
        }
        
        //img_Photo.texture = img_CameraShow.texture;
        
        panel_TakePhoto.SetActive(false);
        panel_Confirm.SetActive(true);
        panel_Photo.SetActive(true);
    }

    void ClearMasks()
    {
        for (int i = maskResults.Count - 1; i >= 0; i--)
        {
            Destroy(maskResultsGos[i]);
        }
        maskResults.Clear();
        maskResultsGos.Clear();
    }

    // PanelConfirm==================================
    public void OnClickRetake()
    {
        panel_TakePhoto.SetActive(true);
        panel_Confirm.SetActive(false);
        panel_Photo.SetActive(false);
        panel_Complete.SetActive(false);

        ClearMasks();
    }
    
    public void OnClickFinish_TakePhoto()
    {
        panel_Confirm.SetActive(false);
        //panel_PaintMask.SetActive(true);
        
        panel_Generating.SetActive(true);
        
        nCurStep = 0;
        GenerateStepImage(nCurStep);
    }
    private int nCurStep = 0;

    private void GenerateStepImage(int nStep)
    {
        var config = GameMgr.Ins.PromptConfigs[nStep];
        Debug.Log($"======= Step {nStep + 1} ============= Prompt: {config.strPrompt}");
            
        GameMgr.Ins.GenerateImage(config.strPrompt, img_GenerateResult.texture as Texture2D, maskResults[nStep].Item1, _ =>
        {
            Texture2D texResult = _;

            if (texResult == default)
            {
                Debug.Log("Generate Error!");
                panel_Generating.gameObject.SetActive(false);
                panel_Complete.SetActive(true);
            }
            else
            {
                img_GenerateResult.texture = texResult;
                if (nCurStep >= GameMgr.Ins.PromptConfigs.Count - 1)
                {
                    Debug.Log("Generate Complete!");
                
                    panel_Generating.gameObject.SetActive(false);
                    panel_Complete.SetActive(true);
                }
                else
                {
                    nCurStep++;
                    GenerateStepImage(nCurStep);
                }
            }
        });
    }

    public void OnClickBack()
    {
        FormMgr.Ins.OpenForm_Replace<FormMain>();
    }

    public void OnClickRegenerate()
    {
        if (GameMgr.Ins.DefaultTexture)
        {
            img_GenerateResult.texture = GameMgr.Ins.DefaultTexture;
        }
        else
        {
            img_GenerateResult.texture = img_Photo.texture as Texture2D;
        }
        
        OnClickFinish_TakePhoto();
    }
    
    Texture2D StretchTexture(Texture2D originalTexture, int newWidth, int newHeight)
    {
        Texture2D stretchedTexture = new Texture2D(newWidth, newHeight);

        for (int y = 0; y < newHeight; y++)
        {
            for (int x = 0; x < newWidth; x++)
            {
                float u = (float)x / newWidth;
                float v = (float)y / newHeight;

                int originalX = Mathf.RoundToInt(u * originalTexture.width);
                int originalY = Mathf.RoundToInt(v * originalTexture.height);

                Color pixel = originalTexture.GetPixel(originalX, originalY);
                stretchedTexture.SetPixel(x, y, pixel);
            }
        }

        stretchedTexture.Apply();

        return stretchedTexture;
    }
}
