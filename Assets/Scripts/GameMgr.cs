using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameMgr : UnitySingleton<GameMgr>
{
    public Camera uiCamera;
    
    public DalleAPI dalleAPI;
    
    public Texture2D DefaultTexture;
    public List<PromptConfig> PromptConfigs;
    
    protected override void Awake()
    {
        base.Awake();
        
        Application.targetFrameRate = 60;
    }

    private void Start()
    {
        //FormMgr.Ins.OpenForm_Replace<FormMain>();
        FormMgr.Ins.OpenForm_Replace<FormTakePhoto>();
    }

    public void GenerateImage(string prompts, Texture2D texInputImage, Texture2D texInputMask,  Action<Texture2D> onComplete)
    {
        if (string.IsNullOrEmpty(prompts))
        {
            Debug.Log("Please enter a valid prompt.");
            return;
        }

        StartCoroutine(dalleAPI.GenerateImageWithMask(prompts, texInputImage, texInputMask, onComplete));
    }
}

[Serializable]
public struct PromptConfig
{
    public Color maskColor;
    public string strPrompt;
}
