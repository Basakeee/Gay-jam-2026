using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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
    [HideInInspector] public bool isOnIce = false;
    
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
    
    [Header("Hook Config")]
    public LineRenderer hookLineRenderer; // ลาก Component LineRenderer มาใส่
    public LayerMask hookLayer; // Layer ของ HookTarget
    public LayerMask obstacleLayer;       // Layer ของกำแพง/สิ่งกีดขวาง (Ground, Wall)
    public GameObject targetIndicator;    // Sprite เป้าเล็งที่จะไปโผล่บน HookTarget
    public LineRenderer rangeIndicator;   // เส้นวงกลมบอกระยะ (LineRenderer แบบ Loop)
    [Range(0, 50)] public int rangeCircleSegments = 50; // ความละเอียดวงกลม
    public float indicatorShowBuffer = 5f;
    private HookTarget _currentValidTarget; // เก็บเป้าหมายที่เล็งได้ปัจจุบัน

    [Header("Other Config")] public int CurrentRoomIndex = 0;

    private bool isHooking = false;
    
    private Rigidbody2D _rb;
    private GameControl _controls;
    private Vector2 _moveInput;
    private Transform _platformSpawnPos;
    private CircleCollider2D _dashCollider;
    private bool _canJump;
    private bool _facingRight = true;
    
    private bool _isTouchingWall;
    private bool _isWallSliding;

    public float CameraTransitionInterval = 1.5f;
    public float CurrentCameraTransitionCooldown;
    
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
        if (currentMaskType == MaskType.Hook)
        {
            HandleHookAiming();
        }
        else
        {
            // ปิด Indicator เมื่อไม่ได้ใช้หน้ากาก Hook
            if (targetIndicator != null) targetIndicator.SetActive(false);
            if (rangeIndicator != null) rangeIndicator.enabled = false;
        }
        CheckCanJump();
        CheckTouchingWall();
        
        // 2. ถ้า Dash อยู่ ห้ามขยับด้วยปุ่มเดิน (ให้ Dash คุม)
        if (!isDashing && CurrentCameraTransitionCooldown <= 0) 
        {
            PlayerMove();
            HandleWallSlide(); 
        }

        CameraTransitionTicking();
        FreezeGravity();
        TickingDownMaskSwap();
        MaskCooldownTicking();
    }

    private void CameraTransitionTicking()
    {
        if (CurrentCameraTransitionCooldown > 0)
        {
            CurrentCameraTransitionCooldown -= Time.deltaTime;
        }
    }

    // ฟังก์ชันใหม่สำหรับเช็คการชนตอน Dash (ทำงานใน Update)
    private void HandleDashCollision()
    {
        if(_dashCollider == null) return;

        Collider2D hit = Physics2D.OverlapCircle(_dashCollider.transform.position, _dashCollider.radius, dashLayerMask);
        
        if (hit != null)
        {
            // --- แก้ไขตรงนี้ ---
            // ลองดึง Component BreakablePlatform ออกมา
            BreakablePlatform breakable = hit.GetComponent<BreakablePlatform>();
            if (breakable != null)
            {
                breakable.Break(); // สั่งให้พัง
            }
            else
            {
                Destroy(hit.gameObject); // เผื่อไว้สำหรับของที่ไม่มี Script
            }
            // ------------------

            // กระแทกตัวผู้เล่นกลับ
            int direction = _facingRight ? -1 : 1; 
            _rb.linearVelocity = Vector2.zero; 
            _rb.AddForce(new Vector2(-knockbackForce.x * direction, knockbackForce.y), ForceMode2D.Impulse);
            
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
        float targetSpeedX = _moveInput.x * speed;
        float newSpeedX;
        if (isOnIce)
        {
            float acceleration = 2f; 
            if (_moveInput.x != 0) acceleration = 5f; 
            newSpeedX = Mathf.Lerp(_rb.linearVelocity.x, targetSpeedX, acceleration * Time.deltaTime);
        }
        else
        {
            newSpeedX = targetSpeedX;
        }

        _rb.linearVelocity = new Vector2(newSpeedX, _rb.linearVelocity.y);
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
    private void HandleHookAiming()
    {
        // 1. ดึงระยะ Hook จริงๆ มาก่อน
        float realRange = 0f;
        if (allMask[(int)currentMaskType] is MaskHook hookMask)
        {
            realRange = hookMask.hookRange;
        }

        // --- [ส่วนที่เพิ่มใหม่] เช็คการแสดงผลวงกลม ---
        
        // ระยะตรวจสอบ = ระยะจริง + ระยะเผื่อ (เช่น 10 + 5 = 15)
        float checkRange = realRange + indicatorShowBuffer;

        // เช็คว่ามี HookTarget อยู่ในระยะไกลๆ บ้างไหม (แค่เช็คว่ามีไหม ไม่ต้องหาตัวที่ดีที่สุด)
        bool isAnyTargetNearby = Physics2D.OverlapCircle(transform.position, checkRange, hookLayer);

        if (isAnyTargetNearby)
        {
            // ถ้ามีของอยู่แถวๆ นี้ ให้วาดวงกลม (ขนาดเท่าระยะจริง)
            DrawRangeCircle(realRange);
        }
        else
        {
            // ถ้าไม่มีอะไรเลย ปิดวงกลมไปซะ จะได้ไม่รกตา
            if (rangeIndicator != null) rangeIndicator.enabled = false;
        }

        // ------------------------------------------

        // 3. หาเป้าหมายที่จะล็อคจริงๆ (ใช้ระยะ realRange เท่านั้น ห้ามโกงระยะ)
        _currentValidTarget = FindBestHookTarget(realRange);

        // 4. อัปเดตตัวชี้เป้า (Crosshair)
        if (_currentValidTarget != null)
        {
            if (targetIndicator != null)
            {
                targetIndicator.SetActive(true);
                targetIndicator.transform.position = _currentValidTarget.transform.position;
            }
        }
        else
        {
            if (targetIndicator != null) targetIndicator.SetActive(false);
        }
    }

    // --- ฟังก์ชันคำนวณ: หาเป้าที่ดีที่สุดและเช็คกำแพง ---
    private HookTarget FindBestHookTarget(float range)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, range, hookLayer);
        
        HookTarget bestTarget = null;
        float closestDistance = float.MaxValue;

        foreach (var hit in hits)
        {
            // เช็คระยะห่าง
            float distance = Vector2.Distance(transform.position, hit.transform.position);
            
            // เงื่อนไข 1: ต้องใกล้กว่าตัวที่เคยเจอ
            if (distance < closestDistance)
            {
                // เงื่อนไข 2: ต้องไม่มีอะไรบัง (Raycast Check)
                Vector2 direction = (hit.transform.position - transform.position).normalized;
                
                // ยิง Ray จากตัวเรา ไปหาเป้าหมาย (ระยะ = distance)
                // ตรวจจับเฉพาะ obstacleLayer
                RaycastHit2D obstacleHit = Physics2D.Raycast(transform.position, direction, distance, obstacleLayer);

                // ถ้า Ray ไม่ชนอะไรเลย (null) แสดงว่าทางสะดวก
                if (obstacleHit.collider == null)
                {
                    HookTarget target = hit.GetComponent<HookTarget>();
                    if (target != null)
                    {
                        closestDistance = distance;
                        bestTarget = target;
                    }
                }
            }
        }

        return bestTarget;
    }

    // --- ฟังก์ชันวาดวงกลมด้วย LineRenderer ---
    private void DrawRangeCircle(float radius)
    {
        if (rangeIndicator == null) return;
        rangeIndicator.enabled = true;
        
        rangeIndicator.positionCount = rangeCircleSegments + 1;
        float angle = 0f;
        
        for (int i = 0; i <= rangeCircleSegments; i++)
        {
            float x = Mathf.Sin(Mathf.Deg2Rad * angle) * radius;
            float y = Mathf.Cos(Mathf.Deg2Rad * angle) * radius;

            rangeIndicator.SetPosition(i, transform.position + new Vector3(x, y, 0));
            angle += (360f / rangeCircleSegments);
        }
    }
    public void StartHook(float range, float speed)
    {
        if (isHooking) return;

        // ไม่ต้อง OverlapCircle ใหม่แล้ว ใช้ค่าจาก HandleHookAiming ได้เลย
        if (_currentValidTarget != null)
        {
            StartCoroutine(HookRoutine(_currentValidTarget, _currentValidTarget.transform.position, speed));
        }
    }

    private IEnumerator HookRoutine(HookTarget target, Vector2 targetPos, float speed)
    {
        isHooking = true;
        _rb.linearVelocity = Vector2.zero;
        
        // เก็บค่า Gravity เดิมไว้
        float originalGravity = _rb.gravityScale;
        _rb.gravityScale = 0; 

        hookLineRenderer.enabled = true;

        // --- กรณีที่ 1: ดึงตัวผู้เล่นไปหาจุด (Spiderman style) ---
        if (target.hookType == HookTarget.HookType.PullPlayerToPoint)
        {
            // หยุดจนกว่าจะถึงจุดหมาย (หรือใกล้มากพอ)
            while (Vector2.Distance(transform.position, targetPos) > 0.5f)
            {
                // อัปเดตเส้นเชือก
                hookLineRenderer.SetPosition(0, transform.position); // ต้นทาง: ตัวเรา
                hookLineRenderer.SetPosition(1, targetPos);          // ปลายทาง: จุดเกาะ

                // เคลื่อนที่
                transform.position = Vector2.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);
                yield return null;
            }
        }
        // --- กรณีที่ 2: ดึง Platform มา (ผู้เล่นอยู่ที่เดิม Platform ขยับ) ---
        else if (target.hookType == HookTarget.HookType.PullObjectToPlayer)
        {
            // สั่งงาน Platform
            target.OnHooked(gameObject);

            // Effect: วาดเส้นเชือกค้างไว้แปปนึงให้รู้ว่าดึงแล้ว (เช่น 0.3 วินาที)
            float effectTimer = 0.3f;
            while (effectTimer > 0)
            {
                hookLineRenderer.SetPosition(0, transform.position);
                hookLineRenderer.SetPosition(1, targetPos);
                
                effectTimer -= Time.deltaTime;
                yield return null;
            }
        }

        // จบการทำงาน
        hookLineRenderer.enabled = false;
        _rb.gravityScale = originalGravity; // คืนค่า Gravity
        isHooking = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Spike"))
        {
            // Play Animation Dead then Reset
            // Animation Dead will cal OnPlayerDie
            OnPlayerDie();
        }
    }

    private void OnPlayerDie()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    
    private void OnDrawGizmos()
    {
        Gizmos.color = _canJump ? Color.green : Color.red;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * jumpCheckRange);

        Gizmos.color = _isTouchingWall ? Color.green : Color.red;
        Vector3 direction = _facingRight ? Vector3.right : Vector3.left;
        Gizmos.DrawLine(transform.position, transform.position + direction * wallCheckDistance);

        if (currentMaskType == MaskType.Hook)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 10f);
        }

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

    public void OnTrasitionCamera()
    {
        CurrentCameraTransitionCooldown = CameraTransitionInterval;
        _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
        _moveInput = Vector2.zero;
    }
    // Getter
    public bool GetFacingRight() => _facingRight;
    public Transform GetPlatformSpawnPos() => _platformSpawnPos;
    public bool IsDashing() => isDashing; // เผื่อ MaskDash อยากเช็ค
    
    public enum MaskType { Normal, Dash, Jump, Hook }
}