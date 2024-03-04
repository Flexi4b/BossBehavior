using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class BossBehavior : MonoBehaviour
{
    [Header("Laser Settings")]
    [SerializeField] private GameObject[] _ObjectAimLines;
    [SerializeField] private LineRenderer[] _AimLines;
    [SerializeField] private GameObject[] _ObjectLaserLines;
    [SerializeField] private LineRenderer[] _LaserLines;
    [SerializeField] private Transform _PlayerPos;
    [SerializeField] private Animator _BossAttackAnim;
    [SerializeField] private int BossLaserDamageToPlayer = 1;

    public float NormalAgentSpeed = 3.5f;
    public float LaserAttackAgentSpeed = 0;

    private float _aimTime = 1f;
    private float _currentAimTimer = 1f;

    [Header("Bulletspray Settings")]
    [SerializeField] private int _amountOfBulletSpray = 25;

    public BulletController bullet;
    public Transform firePoint;
    public float fireSpread = 90f;
    public float bulletSpeed;
    public float timeBetweenShots;
    public int amountOfPellets = 4;
    public int maxMagCapacity = 10;

    private int currentMagAmmo;
    private float shotCounter;

    [Header("Other Settings")]
    public PlayerHealth _playerHealth;
    public EnemyHealth _enemyHealth;

    private FieldOfView _fieldOfView;
    private NavMeshAgent _agent;

    private int[] _randomAttack = { 0,  1 };
    private int _chosenAttack;

    private Coroutine _currentCoroutine;
    private float _attackCooldownTime = 0;
    private float _maxAttackCooldownTimer = 5;

    void Start()
    {
        _fieldOfView = GetComponent<FieldOfView>();
        _agent = GetComponent<NavMeshAgent>();
        _playerHealth = FindObjectOfType<PlayerHealth>();
        _enemyHealth = GetComponent<EnemyHealth>();
        _currentCoroutine = StartCoroutine("AttackIsRandom");
        currentMagAmmo = maxMagCapacity;
        _attackCooldownTime = _maxAttackCooldownTimer;
    }

    void Update()
    {
        if (_fieldOfView.IsPlayerVisable == true)
        {
            if (_chosenAttack == 0 && _currentCoroutine == null)
            {
                _attackCooldownTime -= Time.deltaTime;
                if (_attackCooldownTime > 0)
                {
                    AimAndFireLaser();
                    _BossAttackAnim.SetBool("Attack", true);
                }
                else
                {
                    _currentCoroutine = StartCoroutine("AttackIsRandom");
                    _attackCooldownTime = _maxAttackCooldownTimer;
                    _BossAttackAnim.SetBool("Attack", false);
                }
            }
            else
            {
                _agent.speed = NormalAgentSpeed;

                for (int i = 0; i < _AimLines.Length; i++)
                {
                    _AimLines[i].SetPosition(0, Vector3.zero);
                    _AimLines[i].SetPosition(1, Vector3.zero);
                }
                AimLineOff();

                for (int i = 0; i < _LaserLines.Length; i++)
                {
                    _LaserLines[i].SetPosition(0, Vector3.zero);
                    _LaserLines[i].SetPosition(1, Vector3.zero);
                }
                LaserLineOff();
            }
            
            if (_chosenAttack == 1 && _currentCoroutine == null)
            {
                for (int i = 0; i < _amountOfBulletSpray; i++)
                {
                    BulletSprayIsAGo();
                    _BossAttackAnim.SetBool("Attack", true);
                }

                _currentCoroutine = StartCoroutine("AttackIsRandom");
                _BossAttackAnim.SetBool("Attack", false);
            }
        }

        if (_fieldOfView.IsPlayerVisable == false)
        {
            AimLineOff();
            LaserLineOff();
        }
    }

    IEnumerator AttackIsRandom()
    {
        yield return new WaitForSeconds(1f);
        _chosenAttack = Random.Range(0, _randomAttack.Length);
        Debug.Log(_chosenAttack);
        _currentCoroutine = null;
    }
    private void AimAndFireLaser()
    {
        _agent.speed = LaserAttackAgentSpeed;
        transform.LookAt(_PlayerPos);

        for (int i = 0; i < _AimLines.Length; i++)
        {
            _AimLines[i].SetPosition(0, this.gameObject.transform.position );
            _AimLines[i].SetPosition(1, new Vector3(_PlayerPos.position.x + i, 
                _PlayerPos.position.y, _PlayerPos.position.z + i));
        }

        AimLineOn();

        _currentAimTimer -= Time.deltaTime;
        if (_currentAimTimer <= 0)
        {
            ShootingLaser();
            _currentAimTimer = _aimTime;
        }
    }

    private void ShootingLaser()
    {
        for (int i = 0; i < _AimLines.Length; i++)
        {
            _AimLines[i].SetPosition(0, Vector3.zero);
            _AimLines[i].SetPosition(1, Vector3.zero);
        }

        AimLineOff();
        LaserLineOn();

        for (int i = 0; i < _LaserLines.Length; i++)
        {
            _LaserLines[i].SetPosition(0, this.gameObject.transform.position);
            _LaserLines[i].SetPosition(1, new Vector3(_PlayerPos.position.x + i,
                _PlayerPos.position.y, _PlayerPos.position.z + i));
        }

        Vector3 rayDirection = _PlayerPos.position - transform.position;
        RaycastHit hit;
        if (Physics.Raycast(transform.position, rayDirection, out hit, Mathf.Infinity))
        {
            // Reduce player's health
            _playerHealth.HurtPlayer(BossLaserDamageToPlayer);
        }
    }

    private void AimLineOn()
    {
        foreach (GameObject aimline in _ObjectAimLines)
        {
            aimline.SetActive(true);
        }
    }

    public void AimLineOff()
    {
        foreach (GameObject aimline in _ObjectAimLines)
        {
            aimline.SetActive(false);
        }
    }

    private void LaserLineOn()
    {
        foreach (GameObject laserline in _ObjectLaserLines)
        {
            laserline.SetActive(true);
        }
    }

    public void LaserLineOff()
    {
        foreach (GameObject laserline in _ObjectLaserLines)
        {
            laserline.SetActive(false);
        }
    }

    private void BulletSprayIsAGo()
    {
        transform.LookAt(_PlayerPos);
        shotCounter -= Time.deltaTime;
        if (shotCounter <= 0)
        {
            currentMagAmmo--; // Decrement ammo count

            Quaternion newRotation = firePoint.rotation * Quaternion.Euler(0, -fireSpread / 2, 0);
            float anglePerBullet = fireSpread / amountOfPellets;
            for (int i = 0; i < amountOfPellets; i++)
            {
                BulletController newBullet = Instantiate(bullet, firePoint.position, newRotation * Quaternion.Euler(0, i * anglePerBullet, 0)) as BulletController;
                newBullet.Speed = bulletSpeed;
            }

            shotCounter = timeBetweenShots;
        }
        else
        {
            shotCounter = 0;
        }
    }
}
