using System;
using System.Collections.Generic;
using UnityEngine;
[Serializable]
public class PlayerController : MonoBehaviour
{
    [Header("Mask")]
    public List<MaskBase> allMask;
    public MaskType currentMaskType;
    [Header("Player Movement Config")]
    public float speed;
    public float jumpCheckRange = 0.1f;
    public float jumpCutMultipier = 0.5f;
    public LayerMask groundMask;
    [Header("Player Mask Config")]
    public float cooldownInterval = 1f;
    private float currentCooldown;
    public float freezeGravityTime = 0.25f;
    private float currentFreezeGravityTime;
    
    private Rigidbody2D rb;
    private GameControl controls;
    private Vector2 moveInput;
    private Transform platformSpawnPos;
    private bool canJump = false;
    private bool facingRight = true;

    private void Awake()
    {
        controls = new GameControl();
        controls.Player.UseSkill.performed += cfx => UseSkill();
        //Move
        controls.Player.Move.performed += cfx => moveInput = cfx.ReadValue<Vector2>();
        controls.Player.Move.canceled += cfx => moveInput = Vector2.zero;
        //Jump
        controls.Player.Jump.performed += cfx => JumpStart();
        controls.Player.Jump.canceled += cfx => JumpStop();
        //Equip Mask
        controls.Player.Slot1.performed += cfx => ChangeMask(1);
        controls.Player.Slot2.performed += cfx => ChangeMask(2);
        controls.Player.Slot3.performed += cfx => ChangeMask(3);
    }

    private void OnEnable()
    {
        controls.Enable();
    }

    private void OnDisable()
    {
        controls.Disable();
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        platformSpawnPos = GetComponentInChildren<Transform>().Find("PlatformSpawnPos");
    }

    void Update()
    {
        PlayerMove();
        CheckCanJump();
        TickingDownMaskSwap();
        FreezeGravity();
        foreach (var var in allMask)
        {
            if (var.maskData.currentCooldown > 0)
                var.maskData.currentCooldown -= Time.deltaTime;
        }
    }

    private void FreezeGravity()
    {
        if (currentFreezeGravityTime > 0)
        {
            currentFreezeGravityTime -= Time.deltaTime;
            rb.gravityScale = 0;
            rb.linearVelocity = Vector2.zero;
        }
        else
        {
            rb.gravityScale = 1;
        }
    }

    private void TickingDownMaskSwap()
    {
        if(currentCooldown > 0)
            currentCooldown -= Time.deltaTime;
    }

    private void CheckCanJump()
    {
        canJump = Physics2D.Raycast(transform.position, Vector2.down, jumpCheckRange,groundMask);
    }

    private void ChangeMask(int changeIndex)
    {
        if(currentCooldown > 0 || currentMaskType == (MaskType)changeIndex) return;
        currentMaskType = (MaskType)changeIndex;
        currentFreezeGravityTime = freezeGravityTime;
        currentCooldown = cooldownInterval;
        switch (currentMaskType)
        {
            case MaskType.Normal :
                break;
            case MaskType.Dash :
                break;
            case MaskType.Hook :
                break;
            case MaskType.Jump :
                break;
        }
    }
    private void PlayerMove()
    {
        FlipSprite();
        rb.linearVelocity = new  Vector2(moveInput.x * speed, rb.linearVelocity.y);
    }

    private void FlipSprite()
    {
        if ( moveInput.x < 0 && facingRight)
            facingRight = false;
        else if ( moveInput.x > 0 && !facingRight)
            facingRight = true;
        int direction = facingRight ? 1 : -1;
        transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x) * direction, transform.localScale.y, transform.localScale.z);    }

    private void UseSkill()
    {
        if (allMask[(int)currentMaskType] == null) return;
        if (allMask[(int)currentMaskType].maskData.currentCooldown > 0) return;
        allMask[(int)currentMaskType].ActiveSkill(gameObject);
        
    }
    private void JumpStart()
    {
        if(allMask[(int)currentMaskType] == null || allMask.Count == 0 || !canJump) return;
        float jumpHeight = allMask[(int)currentMaskType].maskData.jumpHeight;
        rb.AddForce(Vector3.up * jumpHeight, ForceMode2D.Impulse);
    }

    private void JumpStop()
    {
        if (allMask[(int)currentMaskType] == null) return;
        if (rb.linearVelocity.y > 0)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultipier);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = canJump ?  Color.green : Color.red;
        Gizmos.DrawLine(transform.position, new Vector3(transform.position.x, transform.position.y - jumpCheckRange));
    }

    public enum MaskType
    {
        Normal,
        Dash,
        Jump,
        Hook
    }

    public bool GetFacingRight() => facingRight;
    public Transform GetPlatformSpawnPos() => platformSpawnPos;
}
