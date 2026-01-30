using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PlayerController : MonoBehaviour
{
    // ... (Header และตัวแปรเดิม คงไว้เหมือนเดิม) ...
    [Header("Mask")]
    public List<MaskBase> allMask;
    public MaskType currentMaskType;

    [Header("Player Movement Config")]
    public float speed;
    public float jumpCheckRange = 0.6f;
    
    [Header("Wall Config")]
    public float wallCheckDistance = 0.6f; 
    public LayerMask wallMask; 

    public float jumpCutMultipier = 0.5f;
    public LayerMask groundMask;

    [Header("Player Mask Config")]
    public float cooldownInterval = 1f;
    private float _currentSwapCooldown;
    public float freezeGravityTime = 0.25f;
    private float _currentFreezeGravityTime;
    
    [Header("Dash Config")]
    public LayerMask dashLayerMask; // อย่าลืมตั้ง Layer นี้ให้ตรงกับ Object ที่ทำลายได้
    public Vector2 knockbackForce;
    
    private Rigidbody2D _rb;
    private GameControl _controls;
    private Vector2 _moveInput;
    private Transform _platformSpawnPos;
    private CircleCollider2D _dashCollider;
    private bool _canJump;
    private bool _facingRight = true;
    
    private bool _isTouchingWall;
    private bool _isWallSliding;
    
    //Abilities
    private bool isDashing; // สถานะ Dash

    // ... (Awake, OnEnable, OnDisable, Start เหมือนเดิม) ...
    private void Awake()
    {
        _controls = new GameControl();
        _controls.Player.UseSkill.performed += cfx => UseSkill();
        _controls.Player.Move.performed += cfx => _moveInput = cfx.ReadValue<Vector2>();
        _controls.Player.Move.canceled += cfx => _moveInput = Vector2.zero;
        _controls.Player.Jump.performed += cfx => JumpStart();
        _controls.Player.Jump.canceled += cfx => JumpStop();
        _controls.Player.Slot1.performed += cfx => ChangeMask(1);
        _controls.Player.Slot2.performed += cfx => ChangeMask(2);
        _controls.Player.Slot3.performed += cfx => ChangeMask(3);
    }
    
    private void OnEnable() => _controls.Enable();
    private void OnDisable() => _controls.Disable();

    void Start()
    {
        _rb = GetComponent<Rigidbody2D>();
        _platformSpawnPos = GetComponentInChildren<Transform>().Find("PlatformSpawnPos");
        // หา Collider ลูก ถ้าหาไม่เจอลองลากใส่แบบ Public แทนก็ได้
        Transform dashColTransform = GetComponentInChildren<Transform>().Find("dashCollider");
        if(dashColTransform != null)
             _dashCollider = dashColTransform.GetComponent<CircleCollider2D>();
             
        if (wallMask == 0) wallMask = groundMask;
    }

    void Update()
    {
        // 1. ถ้ากำลัง Dash ให้เช็คการชน
        if (isDashing)
        {
            HandleDashCollision();
        }

        CheckCanJump();
        CheckTouchingWall();
        
        // 2. ถ้า Dash อยู่ ห้ามขยับด้วยปุ่มเดิน (ให้ Dash คุม)
        if (!isDashing) 
        {
            PlayerMove();
            HandleWallSlide(); 
        }

        FreezeGravity();
        TickingDownMaskSwap();
        MaskCooldownTicking();
    }

    // ฟังก์ชันใหม่สำหรับเช็คการชนตอน Dash (ทำงานใน Update)
    private void HandleDashCollision()
    {
        if(_dashCollider == null) return;

        // เช็คว่าชนอะไรใน Layer ที่กำหนดไหม
        Collider2D hit = Physics2D.OverlapCircle(_dashCollider.transform.position, _dashCollider.radius, dashLayerMask);
        
        if (hit != null)
        {
            // ทำลายของ
            Destroy(hit.gameObject);
            
            // กระแทกตัวผู้เล่นกลับ
            int direction = _facingRight ? -1 : 1; // กระแทกสวนทางกับที่หัน
            _rb.linearVelocity = Vector2.zero; // รีเซ็ตความเร็วก่อน
            _rb.AddForce(new Vector2(knockbackForce.x * direction, knockbackForce.y), ForceMode2D.Impulse);
            
            // จบการ Dash ทันที
            OnEndDashing();
        }
    }

    // ... (CheckTouchingWall, HandleWallSlide, MaskCooldownTicking, FreezeGravity เหมือนเดิม) ...
    private void CheckTouchingWall()
    {
        Vector2 direction = _facingRight ? Vector2.right : Vector2.left;
        _isTouchingWall = Physics2D.Raycast(transform.position, direction, wallCheckDistance, wallMask);
    }

    private void HandleWallSlide()
    {
        if (_isTouchingWall && !_canJump && _rb.linearVelocity.y < 0 && _moveInput.x != 0)
        {
            _isWallSliding = true;
        }
        else
        {
            _isWallSliding = false;
        }

        if (_isWallSliding)
        {
            if(allMask.Count == 0 || allMask[(int)currentMaskType] == null) return;
            float slideSpeed = allMask[(int)currentMaskType].maskData.wallFrictionSlide;
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, Mathf.Clamp(_rb.linearVelocity.y, -slideSpeed, float.MaxValue));
        }
    }

    private void MaskCooldownTicking()
    {
        foreach (var var in allMask)
        {
            if (var.maskData.currentCooldown > 0)
                var.maskData.currentCooldown -= Time.deltaTime;
        }
    }
    
    private void FreezeGravity()
    {
        if (_currentFreezeGravityTime > 0)
        {
            _currentFreezeGravityTime -= Time.deltaTime;
            _rb.gravityScale = 0;
            _rb.linearVelocity = Vector2.zero;
        }
        else
        {
            _rb.gravityScale = 1;
        }
    }

    private void TickingDownMaskSwap()
    {
        if(_currentSwapCooldown > 0)
            _currentSwapCooldown -= Time.deltaTime;
    }

    private void CheckCanJump()
    {
        _canJump = Physics2D.Raycast(transform.position, Vector2.down, jumpCheckRange, groundMask);
    }
    
    // ... (PlayerMove, FlipSprite, ChangeMask, UseSkill, JumpStart, JumpStop, OnDrawGizmos เหมือนเดิม) ...

    private void PlayerMove()
    {
        FlipSprite();
        _rb.linearVelocity = new Vector2(_moveInput.x * speed, _rb.linearVelocity.y);
    }
    
    private void FlipSprite()
    {
        if (_moveInput.x < 0 && _facingRight)
            _facingRight = false;
        else if (_moveInput.x > 0 && !_facingRight)
            _facingRight = true;
        int direction = _facingRight ? 1 : -1;
        transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x) * direction, transform.localScale.y, transform.localScale.z);
    }
    private void ChangeMask(int changeIndex)
    {
        if (_currentSwapCooldown > 0 || currentMaskType == (MaskType)changeIndex) return;
        currentMaskType = (MaskType)changeIndex;
        _currentFreezeGravityTime = freezeGravityTime;
        _currentSwapCooldown = cooldownInterval;
    }

    private void UseSkill()
    {
        if (allMask[(int)currentMaskType] == null) return;
        if (allMask[(int)currentMaskType].maskData.currentCooldown > 0) return;
        allMask[(int)currentMaskType].ActiveSkill(gameObject);
    }

    private void JumpStart()
    {
        if (allMask[(int)currentMaskType] == null || allMask.Count == 0 || !_canJump) return;
        float jumpHeight = allMask[(int)currentMaskType].maskData.jumpHeight;
        _rb.AddForce(Vector3.up * jumpHeight, ForceMode2D.Impulse);
    }

    private void JumpStop()
    {
        if (allMask[(int)currentMaskType] == null) return;
        if (_rb.linearVelocity.y > 0)
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, _rb.linearVelocity.y * jumpCutMultipier);
    }
    
    private void OnDrawGizmos()
    {
        Gizmos.color = _canJump ? Color.green : Color.red;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * jumpCheckRange);

        Gizmos.color = _isTouchingWall ? Color.green : Color.red;
        Vector3 direction = _facingRight ? Vector3.right : Vector3.left;
        Gizmos.DrawLine(transform.position, transform.position + direction * wallCheckDistance);

        if (_dashCollider != null)
        {
            Gizmos.color = Color.lightPink;
            Gizmos.DrawWireSphere(_dashCollider.transform.position,_dashCollider.radius);
        }
    }

    // --- แก้ไขตรงนี้: ลบ while loop ออก ---
    public void OnStartDashing()
    {
        isDashing = true; 
        // แค่เปิด Boolean แล้วให้ Update ทำงานต่อ
    }

    public void OnEndDashing()
    {
        isDashing = false;
        // เมื่อจบ Dash ให้รีเซ็ตความเร็วแกน X (ถ้าต้องการให้หยุดทันที)
        // _rb.linearVelocity = Vector2.zero; 
    }
    
    // Getter
    public bool GetFacingRight() => _facingRight;
    public Transform GetPlatformSpawnPos() => _platformSpawnPos;
    public bool IsDashing() => isDashing; // เผื่อ MaskDash อยากเช็ค
    
    public enum MaskType { Normal, Dash, Jump, Hook }
}