using UnityEngine;

[CreateAssetMenu(fileName = "MaskData", menuName = "ScriptableObjects/MaskHook")]
public class MaskHook : MaskBase
{
    [Header("Hook Config")]
    public float hookRange = 10f;
    public float hookSpeed = 15f;

    public override void ActiveSkill(GameObject parent)
    {
        maskData.currentCooldown = maskData.cooldownInterval;
        PlayerController pc = parent.GetComponent<PlayerController>();
        
        // ส่งค่า hookRange ไปด้วย (แม้ pc จะรู้อยู่แล้วจากการเล็ง แต่ส่งไปเพื่อยืนยัน)
        pc.StartHook(hookRange, hookSpeed);
        
        Debug.Log("MaskHook Fired");
    }
}
