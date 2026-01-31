using UnityEngine;

public abstract class MaskBase : ScriptableObject, IActiveSkill
{
    [Header("Common Data")]
    public MaskData maskData;
    public abstract void ActiveSkill(GameObject parent);
}