﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class EnemyBase : MonoBehaviour, IEnemy, IPoolManager, IDamageSystem
{
    #region VariablesDeclaration

    [Header("Stats Settings")]
    public EnemyStats Stats;
    private EnemyStats _instanceStats;
    public EnemyStats instanceStats
    {
        get { return _instanceStats; }
        set
        {
            _instanceStats = value;
        }
    }

    [Header("Shooting Settings")]
    public BulletBase shootingBulletPrefab;
    public Transform shootPoint;

    public ObjectTypes objectID
    {
        get
        {
            return getID();
        }
    }
    protected abstract ObjectTypes getID();

    private State _currentState;
    public State CurrentState
    {
        get { return _currentState; }
        set { _currentState = value; }
    }

    public GameObject ownerObject
    {
        get
        {
            return ownerobject;
        }
        set
        {
            ownerobject = value;
        }
    }
    private GameObject ownerobject;

    protected PoolManager pool;
    protected float rateoTimer;

    #endregion

    #region EventsDeclaration

    public event PoolManagerEvets.Events OnObjectSpawn;
    public event PoolManagerEvets.Events OnObjectDestroy;

    #endregion

    #region ScreenCheck
    protected float screenHeight;
    protected float screenWidth;
    protected bool CheckScreenPosition()
    {
        if (transform.position.z < -screenHeight)
            return true;
        return false;
    }
    #endregion

    #region Shoot

    public virtual void Shoot()
    {
        BulletBase bulletToShoot = pool.GetPooledObject(shootingBulletPrefab.objectID, gameObject).GetComponent<BulletBase>();
        bulletToShoot.transform.position = shootPoint.position;
        bulletToShoot.OnObjectDestroy += BulletDestroy;
        bulletToShoot.Shoot(shootPoint.forward);
    }

    protected virtual void ShootRateo()
    {
        rateoTimer += Time.deltaTime;
        if (rateoTimer > shootingBulletPrefab.Stats.fireRate)
        {
            Shoot();
            rateoTimer = 0;
        }
    }

    #endregion

    private void Start()
    {
        StartDefault();
    }
    protected virtual void StartDefault()
    {
        pool = PoolManager.instance;
        screenHeight = Camera.main.orthographicSize-Camera.main.transform.position.z;
        screenWidth = (screenHeight * Screen.width / Screen.height)- transform.localScale.magnitude;
        Setup();
    }

    private void Update()
    {
        UpdateDefault();
    }
    protected virtual void UpdateDefault()
    {
        if (CurrentState == State.InUse)
        {
            ShootRateo();
            Movement();
            if (CheckScreenPosition())
                DestroyBehaviour();
        }
    }

    public virtual void Movement()
    {
        transform.position += transform.forward * instanceStats.movementSpeed * Time.deltaTime;
    }

    public virtual void Damaged(BulletBase bulletHitted)
    {
        if (bulletHitted.ownerObject.tag == "Player")
        {
            instanceStats.life -= bulletHitted.Stats.damage;
            bulletHitted.OnEnemyHit(this,bulletHitted);
            if (instanceStats.life <= 0)
            {
                bulletHitted.OnEnemyKill(this, bulletHitted);
                DestroyBehaviour();
            }
        }
    }

    private void Setup()
    {
        if (!Stats)
            return;
        instanceStats = Instantiate(Stats);
    }

    public virtual void Spawn(Vector3 spawnPosition)
    {
        CurrentState = State.InUse;
        if (OnObjectSpawn != null)
        {
            OnObjectSpawn(this);
        }
        rateoTimer = 0;
        transform.position = spawnPosition;
        Setup();
    }

    #region DestroyFunctions

    protected virtual void BulletDestroy(IPoolManager _gameObject)
    {
        _gameObject.OnObjectDestroy -= BulletDestroy;
    }

    public virtual void DestroyBehaviour()
    {
        CurrentState = State.Destroying;
        DestroyVisualEffect();
    }

    public virtual void DestroyMe()
    {
        if (OnObjectDestroy != null)
            OnObjectDestroy(this);
    }

    public virtual void DestroyVisualEffect()
    {
        DestroyMe();
    }

    #endregion
}
