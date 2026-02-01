using System;
using System.Collections;
using UnityEngine;
[Serializable]
public class MaskData
{
    [Header("Config")]
    public float jumpHeight;
    [Range(0.01f,2f)] public float wallFrictionSlide;
    public float cooldownInterval;
    public float currentCooldown;
}
