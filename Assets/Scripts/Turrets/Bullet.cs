using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

    public class Bullet : PoolObject
{ 
    [SerializeField] protected LayerMask enemyLayer;
    [SerializeField] protected float bulletDamage = 10f; // 총알의 기본 데미지
    [SerializeField] private float speed = 15f; // 총알 이동 속도
    [SerializeField] private float _lifeTime = 3f;
    [SerializeField] private Transform _returnPoint;
    protected Transform _target; // 추적할 몬스터 타겟

    public void SetDamage(float newDamage)
    {
        if (newDamage > 0)
        {
            bulletDamage = newDamage;
        }
    }

    public void SetTarget(Transform target)
    {
        _target = target;
    }

    private void OnEnable()
    {
        StartCoroutine(LifeTimeRoutine());
    }

    private void Update()
    {
        Move();
    }

    public override void WakeUp()
    {
        transform.SetParent(null);
        gameObject.SetActive(true);
    }

    public override void Sleep()
    {
        // 💡 [요청 사항 반영 1] 풀 반환 시 이전 타깃을 해제하여 데이터 꼬임을 방지합니다.
        _target = null;

        transform.SetParent(_returnPoint);
        transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.Euler(Vector3.zero));
        gameObject.SetActive(false);
    }

    protected virtual void OnTriggerEnter(Collider other) 
    { 
        if ((enemyLayer.value & (1 << other.gameObject.layer)) > 0) 
        { 
            if (other.TryGetComponent<IDamageable>(out IDamageable damageable))
            {
                DamageLogic(damageable);
            }
            ReturnToPool();
        } 
    }
    
    private IEnumerator LifeTimeRoutine()
    {
        yield return new WaitForSeconds(_lifeTime);
        ReturnToPool();
    }

    public void InitDate()
    {
        _returnPoint = transform.parent;
    }

    protected virtual void Move()
    {
        // 타깃이 비활성화되었는지 검사
        if (_target != null && !_target.gameObject.activeInHierarchy)
        {
            _target = null;
        }

        if (_target == null)
        {
            // 💡 중요: 유도 성능이 상실되면 유도 방향이 아니라 '총알 자체의 정면'으로 곧장 직진해야 자연스럽습니다.
            transform.Translate(Vector3.forward * speed * Time.deltaTime, Space.Self);
            return;
        }

        // 타겟이 있는 방향으로 유도 이동
        Vector3 direction = (_target.position - transform.position).normalized;
        transform.position += direction * speed * Time.deltaTime;
    
        // 방향이 제로 벡터가 아닐 때만 회전 처리 (안전장치)
        if (direction != Vector3.zero)
        {
            transform.forward = direction; 
        }
    }

    protected virtual void DamageLogic(IDamageable damageable)
    {
        damageable.TakeDamage(bulletDamage);
    }
    protected virtual void DamageLogic(IDamageable damageable, float finalDamage)
    {
        damageable.TakeDamage(finalDamage);
    }
}
