﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollectibleItem : MonoBehaviour {

    public enum Type { Health, Experience, Credit }

    [SerializeField]
    Type m_Type;

    [SerializeField]
    int value;

    void OnCollisionEnter(Collision coll)
    {
        if (coll.collider.isTrigger)
            return;

        AttemptApply(coll.gameObject);
    }
    void OnTriggerEnter(Collider other)
    {
        if (other.isTrigger)
            return;

        AttemptApply(other.gameObject);
    }
    
    public void AttemptApply(GameObject targetObject)
    {
        switch (ItemType)
        {
            case Type.Health:
                AttemptAddHealth(targetObject);
                break;
            case Type.Experience:
                AttemptAddExperience(targetObject);
                break;
            case Type.Credit:
                AttemptAddCredit(targetObject);
                break;
        }
    }

    void AttemptAddHealth(GameObject obj)
    {
        Health _health = obj.GetComponent<Health>();

        if (_health != null && _health.NeedsHealth)
        {
            _health.HealthArithmetic(Value, false, transform);

            Destroy(gameObject);
        }
    }
    void AttemptAddExperience(GameObject obj)
    {
        PlayerController _player = obj.GetComponent<PlayerController>();

        if (_player != null && _player.CanModifyExp(Value))
        {
            _player.ExperienceArithmetic(Value);

            Destroy(gameObject);
        }
    }
    void AttemptAddCredit(GameObject obj)
    {
        PlayerController _player = obj.GetComponent<PlayerController>();

        if (_player != null && _player.CreditArithmetic(Value))
        {
            Destroy(gameObject);
        }
    }

    public Type ItemType
    {
        get { return m_Type; }
    }
    public int Value
    {
        get { return value; }
    }
}
