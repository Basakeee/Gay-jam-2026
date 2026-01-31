using UnityEngine;
using System.Collections;

public class BreakablePlatform : MonoBehaviour
{
    public GameObject breakEffect; // ใส่ Particle ตอนพัง (ถ้ามี)
    public float respawnTime = 3f; // เวลาเกิดใหม่ (ถ้า 0 คือพังถาวร)

    private Collider2D _col;
    private SpriteRenderer _ren;

    private void Awake()
    {
        _col = GetComponent<Collider2D>();
        _ren = GetComponent<SpriteRenderer>();
    }

    public void Break()
    {
        if (breakEffect != null) Instantiate(breakEffect, transform.position, Quaternion.identity);

        // ซ่อน Platform
        _col.enabled = false;
        _ren.enabled = false;

        // ถ้ามีการเกิดใหม่
        if (respawnTime > 0) StartCoroutine(RespawnRoutine());
        else Destroy(gameObject, 1f); // พังถาวร (Delay 1 วิเผื่อเสียงจบ)
    }

    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(respawnTime);
        _col.enabled = true;
        _ren.enabled = true;
    }
}