using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FormMain : Form
{
    protected override void OnShow()
    {
        
    }

    public void OnClickOpenFormTakePhoto()
    {
        FormMgr.Ins.OpenForm_Replace<FormTakePhoto>();
    }
}
