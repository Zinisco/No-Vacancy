using System;
using UnityEngine;

[Serializable]
public class DialogueLine
{

    [TextArea(5, 5)]
    public string text;

    public AudioClip voiceClip;
}