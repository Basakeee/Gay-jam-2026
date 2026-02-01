using UnityEngine;
[CreateAssetMenu(fileName = "MaskData", menuName = "ScriptableObjects/MaskNormal")]
public class MaskNormal : MaskBase
{
    public override void ActiveSkill(GameObject parent)
    {
        maskData.currentCooldown = maskData.cooldownInterval;
        Debug.Log("MaskNormal ActiveSkill");
    }
}