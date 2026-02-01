using System;
using TMPro;
using UnityEngine;

public class DeathCount : MonoBehaviour
{
    public static DeathCount instance;

    private void Start()
    {
        if (instance == null)
            instance = this;
    }

    public TMP_Text deathCountText;
    private int deathCount;

    public void addDeathCount()
    {
        deathCount++;
        deathCountText.text = $"Deaths: {deathCount}";
    }
}
