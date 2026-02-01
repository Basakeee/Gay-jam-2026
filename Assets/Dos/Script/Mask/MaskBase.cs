using UnityEngine;

public abstract class MaskBase : ScriptableObject, IActiveSkill
{
    [Header("Common Data")]
    public MaskData maskData;
    [Header("Visual & Physics Settings")]
    public AnimatorOverrideController animatorOverride;
    public Vector2 colliderSize = new Vector2(1f, 2f); 
    public Vector2 colliderOffset = new Vector2(0f, 0f);
    public abstract void ActiveSkill(GameObject parent);
}