using UnityEngine;

public class UnlockOrb : MonoBehaviour
{

    [Header("Visuals")] public GameObject pickupEffect; // Effect ตอนเก็บ (ถ้ามี)
    [Header("Sounds")] public AudioClip collectSound;
    public enum OrbType { Collectible,Mask };
    public OrbType orbType;
    public MaskBase maskBase;
    private void Start()
    {
        if(orbType == OrbType.Collectible)
            Totem.instance.RegisterOrb(this);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            // 2. เล่น Effect (ถ้ามี)
            if (pickupEffect != null)
            {
                Instantiate(pickupEffect, transform.position, Quaternion.identity);
            }
            if (orbType == OrbType.Collectible)
                Totem.instance.CollectOrb(this);
            if (orbType == OrbType.Mask && maskBase != null)
                if(collision.TryGetComponent<PlayerController>(out var player))
                    player.AddNewMask(maskBase);
            

            gameObject.SetActive(false);
        }
    }
}
