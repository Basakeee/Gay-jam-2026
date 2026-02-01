using System.Collections.Generic;
using UnityEngine;

public class Totem : BreakablePlatform
{
    private List<UnlockOrb> unlockOrbs = new List<UnlockOrb>();
    private List<UnlockOrb> collectOrb = new List<UnlockOrb>();
    public static Totem instance;
    private void Awake()
    {
        if(instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    public void RegisterOrb(UnlockOrb orb)
    {
        unlockOrbs.Add(orb);
    }
    public void CollectOrb(UnlockOrb orb)
    {
        if (!collectOrb.Contains(orb))
        {
            collectOrb.Add(orb);
        }
    }
    public override void Break()
    {
        if(unlockOrbs.Count == collectOrb.Count)
        {
            base.Break();
        }
    }
}
