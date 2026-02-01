using UnityEngine;
using System.Collections;
using UnityEngine.U2D;

public class BreakablePlatform : MonoBehaviour
{
    public GameObject breakEffect; // ใส่ Particle ตอนพัง (ถ้ามี)
    public float respawnTime = 3f; // เวลาเกิดใหม่ (ถ้า 0 คือพังถาวร)

    private Collider2D _col;
    private SpriteRenderer _ren;
    private SpriteShapeRenderer ssr;
    private void Awake()
    {
        _col = GetComponent<Collider2D>();
        _ren = GetComponent<SpriteRenderer>();
        ssr = GetComponent<SpriteShapeRenderer>();  
    }

    public virtual void Break()
    {
        if (breakEffect != null) Instantiate(breakEffect, transform.position, Quaternion.identity);

        // ซ่อน Platform
        _col.enabled = false;
        _ren.enabled = false;

        Destroy(gameObject);
    }

    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(respawnTime);
        _col.enabled = true;
        _ren.enabled = true;
        ssr.enabled = true;
    }
}