using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackTurret : BaseTurret, IBuffable
{
    [SerializeField] private float _attackRange;
    [SerializeField] private LayerMask _targetMask;
    [SerializeField] protected Transform _muzzlePoint;
    [SerializeField] protected Bullet _bullets;
    protected ObjectPool<Bullet>  _bulletPool;
    [SerializeField] private float _bulletCoolTime;
    
    
    [SerializeField] private Transform _target;
    private readonly TurretTargetList _targets = new TurretTargetList();
    private float _lastAttackTime;

    protected virtual void Start()
    {
        _bulletPool = new ObjectPool<Bullet>(_bullets, 10, _muzzlePoint, bullet => { bullet.InitDate(); });
    }

    protected virtual void Update()
    {
        _target = _targets.GetTarget(_target);
        if (_target == null) return;

        // 쿨타임 체크 후 공격
        if (Time.time >= _lastAttackTime + _bulletCoolTime)
        {
            Attack();
            _lastAttackTime = Time.time;
        }
        TurretRotate();
    }

    public override void Attack()
    {
        // 💡 [방어 코드 1] 발사구와 풀이 존재하는지 먼저 확인
        if (_bulletPool == null || _muzzlePoint == null) return;

        // 💡 [방어 코드 2] 타깃이 없거나, 이미 죽어서 비활성화(오브젝트 풀 반환) 상태라면 발사하지 않음
        if (_target == null || !_target.gameObject.activeInHierarchy)
        {
            _target = null; // 죽은 타깃 참조 해제
            return; 
        }

        // 1. 오브젝트 풀에서 총알을 꺼냅니다. (WakeUp 내부에서 활성화 처리가 된다고 가정)
        Bullet bulletScript = _bulletPool.Pop();

        if (bulletScript != null)
        {
            // 2. 총알의 위치와 회전값을 발사구(_muzzlePoint) 기준으로 맞춥니다.
            bulletScript.transform.position = _muzzlePoint.position;
            bulletScript.transform.rotation = _muzzlePoint.rotation;

            // 3. 서포트 타워 등으로 변동된 최종 공격력을 총알에 세팅합니다.
            bulletScript.SetDamage(this.Damage);

            // 4. 총알이 추적할 타겟을 지정해 줍니다.
            bulletScript.SetTarget(_target);
        }
    }

    // 💡 3. IBuffable 인터페이스 구현 함수
    // 서포트 타워의 코루틴 주기에 따라 이 함수가 호출되면서 공격력이 늘어났다 줄어들었다 합니다.
    public void ApplyDamageBuff(float buffAmount)
    {
        // 부모(BaseTurret)에 기재된 기본 공격력 수치를 변경합니다.
        _baseDamage += buffAmount; 
        
        #if UNITY_EDITOR
        Debug.Log($"[버프 알림] {gameObject.name}의 현재 공격력: {this.Damage}");
#endif
    }

    private void OnTriggerEnter(Collider other)
    {
        RegisterTarget(other);
    }

    private void OnTriggerStay(Collider other)
    {
        RegisterTarget(other);
    }

    private void RegisterTarget(Collider other)
    {
        if (!IsTargetLayer(other.gameObject.layer)) return;
        _targets.Register(other);
    }

    private void OnTriggerExit(Collider other)
    {
        _targets.Remove(other);
    }

    protected virtual void OnDisable()
    {
        _targets.Clear();
        _target = null;
    }

    private bool IsTargetLayer(int layer)
    {
        return (_targetMask.value & (1 << layer)) != 0;
    }

    private void TurretRotate()
    {
        if (_target == null) return;

        Vector3 targetPosition = new Vector3(_target.position.x, transform.position.y, _target.position.z);
        transform.LookAt(targetPosition);
    }
}
