using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class PlayerController : UnitController
{
    static readonly float EXP_COLLECT_SPEED = 6f;
    static readonly float DROP_UTILITY_DELAY = 0.5f;


    [Space(15)]
    [Header("Levelling")]
    [Space(5)]

    [Tooltip("Current Character Level")]
    int currentLevel = 1;

    [Tooltip("Current Character Exp")]
    int currentExp = 0;

    [Space(15)]
    [Header("Interaction")]
    [Space(5)]

    [Tooltip("Minimum distance necessary to interact with object")]
    [SerializeField]
    float interactDistance = 4f;

    [Tooltip("Should collect collectables?")]
    [SerializeField]
    bool shouldCollect = false;

    [Tooltip("Minimum distance necessary to collect a collectable")]
    [SerializeField]
    float collectRange = 4f;

    [Space(15)]
    [Header("Utility")]
    [Space(5)]


    [SerializeField]
    Transform handheldHolder;

    [Tooltip("Maximum throwing power for UtilityItem")]
    [SerializeField]
    float throwPower;

    [Tooltip("Time to achieve full throw power")]
    [SerializeField]
    [Range(0.1f, 4f)]
    float throwTime;

    [Tooltip("Throw direction")]
    [SerializeField]
    Vector3 throwVector = new Vector3(0f, 1f, 0f);

    float currentThrowTime = 0f;

    [Tooltip("Current Utility Item")]
    [SerializeField]
    UtilityItem m_UtilityItem;

    [Tooltip("Number of UtilityItems left in inventory")]
    [SerializeField]
    int utilityItemCount = 0;
    bool isUsingUtility = false;




    [Space(15)]
    [Header("Items")]
    [Space(5)]


    [Tooltip("Current HandheldItem")]
    [SerializeField]
    protected HandheldItem m_HandheldItem;

    [Tooltip("Native Ability. Can not be dropped")]
    [SerializeField]
    protected Ability nativeAbility;

    [Tooltip("Auxiliary Ability. Can be dropped")]
    [SerializeField]
    protected Ability auxiliaryAbility;




    [Space(15)]
    [Header("Effects")]
    [Space(5)]

    [Tooltip("Effect to be played upon damaging an enemy")]
    [SerializeField]
    DisplayEffect damageAchievedEffect;

    [Tooltip("Effect to be played upon killing an enemy")]
    [SerializeField]
    DisplayEffect killAchievedEffect;


    [Tooltip("Effect to be played upon gaining experience")]
    [SerializeField]
    DisplayEffect experienceGainedEffect;

    [Tooltip("Effect to be played upon losing experience")]
    [SerializeField]
    DisplayEffect experienceLostEffect;


    [SerializeField]
    LayerMask ignoreCollisionLayer;

    InteractableObject currentInteractable;

    Coroutine handheldPickupRoutine = null;
    Coroutine abilityPickupRoutine = null;


    public event Delegates.Alert OnExpChange;



    public override void Start()
    {
        base.Start();

        if (GameManager.Instance != null)
        {
            m_Health.OnDamaged += GameManager.Instance.PlayerDamaged;
            m_Health.OnKilled += GameManager.Instance.PlayerKilled;
        }

        OnExpChange += UpdateExperienceUI;

        //m_Handler.ShowUI(true);
        //m_Handler.UpdateUI(Attribute.Experience, CurrentExperienceLevelProgress, false);


        if (NativeAbility != null)
        {
            GameObject obj = Instantiate(NativeAbility.gameObject) as GameObject;
            obj.transform.position = m_Transform.position;

            NativeAbility = obj.GetComponent<Ability>();
            PickupNativeAbility();
        }

        if (AuxiliaryAbility != null)
        {
            GameObject obj = Instantiate(AuxiliaryAbility.gameObject) as GameObject;
            obj.transform.position = m_Transform.position;

            AuxiliaryAbility = obj.GetComponent<Ability>();
            Pickup(AuxiliaryAbility);
        }

        if (HandheldItem != null)
        {
            GameObject obj = Instantiate(HandheldItem.gameObject) as GameObject;
            obj.transform.position = m_Transform.position;

            HandheldItem = obj.GetComponent<HandheldItem>();
            Pickup(HandheldItem);
        }

        if (UtilityItem != null)
        {
            //GameObject obj = Instantiate(UtilityItem.gameObject) as GameObject;
            //obj.transform.position = m_Transform.position;

            //UtilityItem = obj.GetComponent<UtilityItem>();
            //Pickup(UtilityItem);
        }

        //HandheldItem startingHandheld = GetComponentInChildren<HandheldItem>();
        //Pickup(startingHandheld);

        //if(nativeAbility != null)
        //      {
        //          nativeAbility.transform.SetParent(m_Transform, true);
        //	StartCoroutine(PickupObject(nativeAbility.transform, Vector3.zero, Quaternion.identity));


        //	ItemPickup _pickup = nativeAbility.GetComponent<ItemPickup>();
        //	_pickup.enabled = false;

        //	Rigidbody _rigidbody = nativeAbility.GetComponent<Rigidbody>();
        //          //_rigidbody.useGravity = false;
        //	_rigidbody.isKinematic = true;

        //	Collider _collider = nativeAbility.GetComponent<Collider>();
        //	_collider.enabled = false;


        //          nativeAbility.Initialize(m_Transform);
        //}


        //Pickup(auxiliaryAbility);

        UpdateUI();
    }
    void OnEnable()
    {
        // m_Handler.ShowUI(true);

        if (ShowDebug)
        {
            Debug.Log(m_Handler.ToString());
        }
    }
    public override void OnDisable()
    {
        base.OnDisable();

        RemoveInteractable(currentInteractable);
    }





    #region Interactable Stuff

    public void Interact()
    {
        if (currentInteractable == null)
            return;


        if (!currentInteractable.IsUsable)
        {
            return;
        }

        Vector3 toVector = currentInteractable.transform.position - m_Transform.position;
        if (toVector.magnitude <= InteractDistance) // && (currentInteractable.IsUsableOutsideFOV || CanSee(currentInteractable.transform)))
        {
            currentInteractable.Interact(this);
        }

        RemoveInteractable(currentInteractable);
    }



    void AddInteractable(GameObject tempObj)
    {
        if (tempObj == null)
            return;


        InteractableObject iObj = tempObj.GetComponent<InteractableObject>();
        AddInteractable(iObj);
    }
    void AddInteractable(InteractableObject tempInteractable)
    {
        if (tempInteractable == null || !tempInteractable.IsUsable || tempInteractable == currentInteractable)
            return;



        if (currentInteractable == null || !currentInteractable.IsUsable || (!currentInteractable.IsUsableOutsideFOV && !CanSee(currentInteractable.transform) && CanSee(tempInteractable.transform))) //Vector3.Angle(tempInteractable.transform.position - myInteractable.transform.position, myTransform.forward) > FOV/2f))
        {
            RemoveInteractable(currentInteractable);
            currentInteractable = tempInteractable;
        }
        else if (Vector3.Distance(m_Transform.position, currentInteractable.transform.position) > Vector3.Distance(m_Transform.position, tempInteractable.transform.position))
        {
            RemoveInteractable(currentInteractable);
            currentInteractable = tempInteractable;
        }

        currentInteractable.InflateUI();
    }
    void RemoveInteractable(GameObject tempObj)
    {
        if (tempObj == null)
            return;


        InteractableObject iObj = tempObj.GetComponent<InteractableObject>();
        RemoveInteractable(iObj);
    }
    void RemoveInteractable(InteractableObject tempInteractable)
    {
        if (currentInteractable == null || tempInteractable == null)
            return;

        if (tempInteractable == currentInteractable)
        {
            currentInteractable.DeflateUI();
            currentInteractable = null;
        }
    }

    #endregion



    #region Utility Item Stuff

    public void ActivateUtilityItem()
    {
        if (UtilityItem.ShouldBeThrown)
        {
            currentThrowTime += Time.deltaTime;
            isUsingUtility = true;
        }
        else
        {
            UseUtilityItem();
            isUsingUtility = false;
        }
    }
    public void DeactivateUtilityItem()
    {
        if (UtilityItem.ShouldBeThrown)
        {
            UseUtilityItem();
        }

        isUsingUtility = false;
    }

    void UseUtilityItem()
    {
        float percentage = Mathf.Clamp01(currentThrowTime / throwTime);

        GameObject _obj = (GameObject)Instantiate(m_UtilityItem.gameObject, HandheldHolderPosition, m_Transform.rotation);
        UtilityItem _uScript = _obj.GetComponent<UtilityItem>();


        if (m_UtilityItem.ShouldBeThrown)
        {
            Vector3 launchVector = m_Transform.TransformDirection(throwVector).normalized * ThrowPower * percentage;


            Rigidbody _rigidbody = _obj.GetComponent<Rigidbody>();

            _obj.SetActive(true);
            _rigidbody.velocity = Vector3.zero;
            _rigidbody.AddForce(launchVector);
        }


        _uScript.Activate(m_Transform, CurrentStats);


        currentThrowTime = 0f;
        utilityItemCount--;
    }
    public void CancelUtilityThrow()
    {
        currentThrowTime = 0f;
    }

    IEnumerator DropUtilityItems(GameObject dropPrefab, int numToDrop)
    {
        GameObject genObj = GameObject.Find("Generated Objects");

        while (numToDrop > 0)
        {

            GameObject _dropObj = (GameObject)Instantiate(dropPrefab, Vector3.zero, Quaternion.identity);
            _dropObj.transform.parent = genObj == null ? null : genObj.transform;

            _dropObj.transform.position = HandheldHolderPosition;
            _dropObj.transform.localRotation = Quaternion.identity;
            _dropObj.SetActive(true);

            Rigidbody _rigid = _dropObj.GetComponent<Rigidbody>();
            if (_rigid != null)
            {
                _rigid.velocity = Vector3.zero;
                _rigid.AddForce(_dropObj.transform.forward * DROP_FORCE, ForceMode.Impulse);
            }

            numToDrop--;

            yield return new WaitForSeconds(DROP_UTILITY_DELAY);
        }

        GameObject.Destroy(dropPrefab);
    }

    #endregion


    #region Offensive Stuff

    public void ActivateNativeAbility()
    {
        if (nativeAbility == null || !nativeAbility.CanUseAbility())
            return;

        nativeAbility.ActivateAbility();

    }
    public void DeactivateNativeAbility()
    {
        if (nativeAbility == null)
            return;

        nativeAbility.DeactivateAbility();
    }


    public void ActivateAuxiliaryAbility()
    {
        if (auxiliaryAbility == null || !auxiliaryAbility.CanUseAbility())
            return;

        auxiliaryAbility.ActivateAbility();
    }
    public void DeactivateAuxiliaryAbility()
    {
        if (auxiliaryAbility == null)
            return;

        auxiliaryAbility.DeactivateAbility();

    }


    public void ActivateHandheldPrimary()
    {
        if (m_HandheldItem == null || !m_HandheldItem.CanActivatePrimary())
            return;

        m_HandheldItem.ActivatePrimary();
        //CameraShake.Instance.Shake(myWeapon.ShakeAmountPrimary, myWeapon.ShakeTime);

    }
    public void DeactivateHandheldPrimary()
    {
        if (m_HandheldItem == null)
            return;

        m_HandheldItem.DeactivatePrimary();
    }
    public void ActivateHandheldSecondary()
    {
        if (m_HandheldItem == null || !m_HandheldItem.CanActivateSecondary())
            return;

        m_HandheldItem.ActivateSecondary();
        //CameraShake.Instance.Shake(myWeapon.ShakeAmountSecondary, myWeapon.ShakeTime);

    }
    public void DeactivateHandheldSecondary()
    {
        if (m_HandheldItem == null)
            return;

        m_HandheldItem.DeactivateSecondary();
    }


    public void ActivateHandheldTertiary()
    {
        if (m_HandheldItem == null || !m_HandheldItem.CanActivateTertiary())
            return;


        m_HandheldItem.ActivateTertiary();
    }
    public void DeactivateHandheldUtility()
    {
        if (m_HandheldItem == null)
            return;

        m_HandheldItem.DeactivateTertiary();
    }


    private void PickupNativeAbility()
    {
        NativeAbility = NativeAbility;
        NativeAbility.transform.parent = m_Transform;
        abilityPickupRoutine = StartCoroutine(PickupObject(NativeAbility.transform, Vector3.zero, Quaternion.identity));
        NativeAbility.Initialize(m_Transform);

        ItemPickup _pickup = NativeAbility.GetComponent<ItemPickup>();
        _pickup.enabled = false;

        Rigidbody _rigidbody = NativeAbility.GetComponent<Rigidbody>();
        _rigidbody.isKinematic = true;

        Collider[] _colliders = NativeAbility.GetComponentsInChildren<Collider>();
        for (int i = 0; i < _colliders.Length; i++) { _colliders[i].enabled = false; }

        NativeAbility.OnAbilityChanged += UpdateNativeAbilityUI;
    }
    public void Pickup(Ability newAbility)
    {
        if (newAbility == null)
            return;

        DropAbility();

        auxiliaryAbility = newAbility;
        auxiliaryAbility.transform.parent = m_Transform;
        abilityPickupRoutine = StartCoroutine(PickupObject(AuxiliaryAbility.transform, Vector3.zero, Quaternion.identity));
        auxiliaryAbility.Initialize(m_Transform);

        ItemPickup _pickup = AuxiliaryAbility.GetComponent<ItemPickup>();
        _pickup.enabled = false;

        Rigidbody _rigidbody = AuxiliaryAbility.GetComponent<Rigidbody>();
        _rigidbody.isKinematic = true;

        Collider[] _colliders = AuxiliaryAbility.GetComponentsInChildren<Collider>();
        for (int i = 0; i < _colliders.Length; i++) { _colliders[i].enabled = false; }

        AuxiliaryAbility.OnAbilityChanged += UpdateAuxiliaryAbilityUI;
    }

    public virtual void DropAbility(Ability _ability)
    {
        if (auxiliaryAbility == _ability)
        {
            DropAbility();
        }
    }
    protected virtual void DropAbility()
    {
        if (auxiliaryAbility != null)
        {

            auxiliaryAbility.Terminate();


            GameObject g = GameManager.Instance.generatedObjectHolder;
            auxiliaryAbility.transform.parent = null;

            if (g != null)
            {
                auxiliaryAbility.transform.parent = g.transform;
            }


            auxiliaryAbility.transform.localRotation = Quaternion.identity;



            ItemPickup _pickup = auxiliaryAbility.GetComponent<ItemPickup>();

            if (_pickup != null)
                _pickup.enabled = true;

            Rigidbody _rigidbody = auxiliaryAbility.GetComponent<Rigidbody>();
            _rigidbody.velocity = Vector3.zero;
            _rigidbody.isKinematic = false;
            _rigidbody.velocity = Vector3.zero;
            _rigidbody.AddForce(auxiliaryAbility.transform.forward * DROP_FORCE, ForceMode.Impulse);


            Collider[] _colliders = AuxiliaryAbility.GetComponentsInChildren<Collider>();
            for (int i = 0; i < _colliders.Length; i++) { _colliders[i].enabled = true; }


            if (abilityPickupRoutine != null)
                StopCoroutine(abilityPickupRoutine);
        }
    }



    public void Pickup(HandheldItem newHandheld)
    {

        if (newHandheld == null)
            return;

        DropHandheld();

        m_HandheldItem = newHandheld;
        m_HandheldItem.transform.parent = handheldHolder;
        m_HandheldItem.transform.localPosition = Vector3.zero;
        m_HandheldItem.transform.localRotation = Quaternion.identity;
        // handheldPickupRoutine = StartCoroutine(PickupObject(m_HandheldItem.transform, handheldHolder.position, Quaternion.identity));

        ItemPickup _pickup = m_HandheldItem.GetComponent<ItemPickup>();
        _pickup.enabled = false;

        Rigidbody _rigidbody = m_HandheldItem.GetComponent<Rigidbody>();
        _rigidbody.isKinematic = true;

        Collider[] _colliders = m_HandheldItem.GetComponentsInChildren<Collider>();
        for (int i = 0; i < _colliders.Length; i++) { _colliders[i].enabled = false; }


        //m_HandheldItem.OnActivatePrimary += HandheldActivationPrimary;
        //m_HandheldItem.OnActivateSecondary += HandheldActivationSecondary;
        //m_HandheldItem.OnActivateUtility += HandheldActivationUtility;
        m_HandheldItem.OnWeaponChanged += UpdateHandheldUI;
        m_HandheldItem.OnWeaponCasualty += CasualtyAchieved;

        if (m_HandheldItem is Weapon)
        {
            Weapon _weapon = (Weapon)m_HandheldItem;

            //_weapon.BonusAttackPower = GetStatValue(StatType.Damage);
            //_weapon.BonusCriticalHitChance = GetStatValue(StatType.Luck);
            //_weapon.BonusCriticalHitMultiplier = GetStatValue(StatType.CriticalDamage);
        }

        m_HandheldItem.Initialize(m_Transform, m_Team);
        //m_HandheldItem.SetVolume(currentLevel);
    }

    public virtual void DropHandheld(HandheldItem _item)
    {
        if (m_HandheldItem == _item)
        {
            DropHandheld();
        }
    }
    protected virtual void DropHandheld()
    {
        if (m_HandheldItem == null)
            return;

        m_HandheldItem.Terminate();


        GameObject genObj = GameObject.Find("Generated Objects");
        Transform newParent = (genObj == null) ? null : genObj.transform;

        m_HandheldItem.transform.parent = newParent;
        m_HandheldItem.transform.localRotation = Quaternion.identity;

        ItemPickup _pickup = m_HandheldItem.GetComponent<ItemPickup>();

        if (_pickup != null)
            _pickup.enabled = true;

        Rigidbody _rigidbody = m_HandheldItem.GetComponent<Rigidbody>();
        _rigidbody.velocity = Vector3.zero;
        _rigidbody.isKinematic = false;
        _rigidbody.velocity = Vector3.zero;
        _rigidbody.AddForce(auxiliaryAbility.transform.forward * DROP_FORCE, ForceMode.Impulse);

        Collider[] _colliders = m_HandheldItem.GetComponentsInChildren<Collider>();
        for (int i = 0; i < _colliders.Length; i++) { _colliders[i].enabled = true; }

        m_HandheldItem = null;


        if (handheldPickupRoutine != null)
            StopCoroutine(handheldPickupRoutine);

        handheldPickupRoutine = null;

    }


    //void HandheldActivationPrimary()
    //{
    //    if (m_HandheldItem == null)
    //        return;

    //    CameraShake.Instance.Shake(m_HandheldItem.ShakeAmountPrimary, m_HandheldItem.ShakeTime);
    //}
    //void HandheldActivationSecondary()
    //{
    //    if (m_HandheldItem == null)
    //        return;

    //    CameraShake.Instance.Shake(m_HandheldItem.ShakeAmountSecondary, m_HandheldItem.ShakeTime);
    //}
    //void HandheldActivationUtility()
    //{
    //    if (m_HandheldItem == null)
    //        return;

    //    CameraShake.Instance.Shake(m_HandheldItem.ShakeAmountTertiary, m_HandheldItem.ShakeTime);
    //}

    #endregion


    #region Stat Stuff

    //protected override void UpdateStatEffects(StatType _type)
    //   {
    //	base.UpdateStatEffects(_type);

    //	Stat _stat = GetStat(_type);

    //       if (_stat == null || m_HandheldItem == null)
    //           return;

    //       m_HandheldItem.UpdateStat(new CustomTuple2<StatType, float>(_stat.Type, _stat.CurrentValue));

    //       //if(_stat == null || m_HandheldItem == null || !(m_HandheldItem is Weapon))
    //       //	return;


    //       //      Weapon _weapon = (Weapon)m_HandheldItem;


    //       //switch(_type)
    //       //      {
    //       //          case StatType.Dexterity:
    //       //              _weapon.BonusAttackRate = _stat.CurrentValue;

    //       //              break;
    //       //          case StatType.Damage:
    //       //              _weapon.BonusAttackPower = _stat.CurrentValue;

    //       //	    break;
    //       //    case StatType.CriticalDamage:
    //       //              _weapon.BonusCriticalHitMultiplier = _stat.CurrentValue;

    //       //	    break;
    //       //    case StatType.Luck:
    //       //              _weapon.BonusCriticalHitChance = _stat.CurrentValue;

    //       //	    break;
    //       //    default:
    //       //	    break;
    //       //}
    //   }

    #endregion


    #region Experience Stuff

    public void ModifyExp(int delta)
    {
        if (delta == 0)
            return;

        CurrentExperience += delta;

        if (SoundManager.Instance != null)
        {
            if (delta > 0)
            {
                //SoundManager.Instance.PlaySound(expGainedSound);

                PlayEffect(experienceGainedEffect);
            }
            else
            {
                //SoundManager.Instance.PlaySound(expLostSound);
                PlayEffect(experienceLostEffect);
            }
        }

        if (OnExpChange != null)
        {
            OnExpChange();
        }
    }

    public bool CanModifyExp(int delta)
    {
        int temp = CurrentExperience + delta;

        return temp >= GetExpRequiredForLevel(CurrentLevel); // ? false : true;
    }

    public void ResetExp()
    {
        CurrentExperience = GetExpRequiredForLevel(CurrentLevel);

        if (OnExpChange != null)
        {
            OnExpChange();
        }
    }

    public int GetExpRequiredForLevel(int lvl)
    {
        if (lvl <= 1)
            return 0;

        return (int)((Mathf.Pow(2, lvl) * 100f) + 100);
    }


    #endregion

    protected void PlayEffect(DisplayEffect _effect)
    {
        _effect.Play();
    }



    public override void UpdateUI()
    {
        base.UpdateUI();

        UpdateHandheldUI(m_HandheldItem == null ? 0f : m_HandheldItem.GetPercentage(), false);
        UpdateAbilityUI(false);
        UpdateExperienceUI();
    }

    protected void UpdateHandheldUI(float percentage, bool setImmediate)
    {
        OnUIAttributeChanged(new UIEventArgs(UIManager.Component.Handheld, "", percentage, setImmediate));

        // m_Handler.UpdateUI("Handheld", m_HandheldItem == null ? 0f : m_HandheldItem.GetPercentage(), setImmediate);
    }

    private void UpdateNativeAbilityUI(float percentage)
    {
        OnUIAttributeChanged(new UIEventArgs(UIManager.Component.NativeAbility, "", percentage, false));
    }
    private void UpdateAuxiliaryAbilityUI(float percentage)
    {
        OnUIAttributeChanged(new UIEventArgs(UIManager.Component.AuxiliaryAbility, "", percentage, false));
    }
    protected void UpdateAbilityUI(bool setImmediate)
    {
        OnUIAttributeChanged(new UIEventArgs(UIManager.Component.NativeAbility, "", NativeAbility == null ? 0f : NativeAbility.GetChargePercentage(), setImmediate));

        OnUIAttributeChanged(new UIEventArgs(UIManager.Component.AuxiliaryAbility, "", AuxiliaryAbility == null ? 0f : AuxiliaryAbility.GetChargePercentage(), setImmediate));
    }

    protected void UpdateExperienceUI()
    {
        OnUIAttributeChanged(new UIEventArgs(UIManager.Component.Experience, "", CurrentExperienceLevelProgress, false));

        //Debug.Log(string.Format("Current Exp: {0}. Exp Required for next level: {1}. Percentage: {2} %", CurrentExperience, GetExpRequiredForLevel(CurrentLevel + 1), CurrentExperienceLevelProgress * 100f));
        //UpdateExpBar(CurrentExperienceLevelProgress);
        //m_Handler.UpdateUI(Attribute.Experience, CurrentExperienceLevelProgress, false);
    }




    public void CasualtyAchieved(Health _casualtyHealth)
    {
        IIdentifier identifier = _casualtyHealth.GetComponent<IIdentifier>();
        UIEventArgs args = new UIEventArgs(UIManager.Component.Enemy, (identifier != null ? identifier.Name : ""), _casualtyHealth.HealthPercentage, false);

        OnUIAttributeChanged(args);

        if (_casualtyHealth.IsAlive)
        {
            if (nativeAbility != null)
                nativeAbility.DamageAchieved(_casualtyHealth.LastHealthChange);

            if (auxiliaryAbility != null)
                auxiliaryAbility.DamageAchieved(_casualtyHealth.LastHealthChange);

            //if(SoundManager.Instance != null)
            //	SoundManager.Instance.PlaySound(damageAchievedEffect);

            PlayEffect(damageAchievedEffect);
        }
        else
        {
            if (nativeAbility != null)
                nativeAbility.KillAchieved();

            if (auxiliaryAbility != null)
                auxiliaryAbility.KillAchieved();

            //if(SoundManager.Instance != null)
            //	SoundManager.Instance.PlaySound(killAchievedEffect);

            PlayEffect(killAchievedEffect);
        }
    }




    #region Accessors

    public List<Stat> CurrentStats
    {
        get
        {
            List<Stat> _currentstats = new List<Stat>();

            StatType[] _types = Enum.GetValues(typeof(StatType)) as StatType[];

            for (int i = 0; i < _types.Length; i++)
            {
                Stat _stat = GetStat(_types[i]);

                if (_stat == null)
                    continue;

                _currentstats.Add(_stat);
            }


            return _currentstats;
        }
    }

    public bool ShouldCollect
    {
        get { return shouldCollect; }
        set { shouldCollect = value; }
    }
    public float CollectRange
    {
        get { return collectRange; }
        private set { collectRange = Mathf.Clamp(value, 0f, value); }
    }


    public int CurrentLevel
    {
        get { return currentLevel; }
        private set { currentLevel = Mathf.Clamp(value, 1, value); }
    }
    public int CurrentExperience
    {
        get { return currentExp; }
        private set { currentExp = Mathf.Clamp(value, 0, value); }
    }
    public float CurrentExperienceLevelProgress
    {
        get
        {
            int totalDiff = GetExpRequiredForLevel(CurrentLevel + 1) - GetExpRequiredForLevel(CurrentLevel);
            int curDiff = CurrentExperience - GetExpRequiredForLevel(CurrentLevel);

            return curDiff / (float)totalDiff;
        }
    }



    public float InteractDistance
    {
        get { return interactDistance; }
        private set { interactDistance = Mathf.Clamp(value, 0f, value); }
    }
    public float ThrowPower
    {
        get { return throwPower; }
        private set { throwPower = Mathf.Clamp(value, 0f, value); }
    }

    public bool HasNativeAbility
    {
        get { return nativeAbility != null; }
    }
    public Ability NativeAbility
    {
        get { return nativeAbility; }
        private set { nativeAbility = value; }
    }


    public bool HasAuxiliaryAbility
    {
        get { return auxiliaryAbility != null; }
    }
    public Ability AuxiliaryAbility
    {
        get { return auxiliaryAbility; }
        private set { auxiliaryAbility = value; }
    }


    public bool HasHandheld
    {
        get { return m_HandheldItem != null; }
    }
    public HandheldItem HandheldItem
    {
        get { return m_HandheldItem; }
        private set { m_HandheldItem = value; }
    }
    public Vector3 HandheldHolderPosition
    {
        get { return handheldHolder == null ? m_Transform.position : handheldHolder.position; }
    }

    public bool HasUtilityItem
    {
        get { return m_UtilityItem != null && utilityItemCount > 0; }
    }
    public UtilityItem UtilityItem
    {
        get { return m_UtilityItem; }
    }
    public bool IsUsingUtility
    {
        get { return isUsingUtility; }
    }

    #endregion
    
    protected override void SightGained(GameObject obj)
    {
  //      if (obj.transform == m_Transform)
  //          return;
  //      /*
		//IInteractable _interactable = coll.GetComponent<IInteractable>();
		
		//if(_interactable != null && _interactable.IsEnabled() && _interactable.IsUsableOutsideFOV()){
		//	AddInteractable(coll.gameObject);
		//}*/

  //      if (!coll.isTrigger)
  //      {
  //          AttributeHandler _handler = coll.GetComponent<AttributeHandler>();
  //          if (_handler != null)
  //          {
  //              //_handler.ShowUI(false);
  //          }
  //      }
    }

    protected override void SightMaintained(GameObject obj)
    {
        if (obj.transform == m_Transform )
            return;

        Vector3 directionVector = obj.transform.position - m_Transform.position;

        if (ShowDebug)
        {
            //Debug.DrawLine(m_Transform.position, obj.transform.position, Color.yellow);
        }

        ICollectible _collectible = obj.GetComponent<ICollectible>();

        if (shouldCollect && _collectible != null && directionVector.magnitude <= CollectRange)
        {
            //obj.transform.position = Vector3.MoveTowards(obj.transform.position, m_Transform.position, EXP_COLLECT_SPEED * Time.deltaTime);
            Rigidbody _rigidbody = obj.GetComponent<Rigidbody>();

            if (directionVector.magnitude <= CollectRange && _rigidbody != null)
            {
                Vector3 forceVector = -directionVector.normalized * EXP_COLLECT_SPEED * Time.deltaTime;
                _rigidbody.MovePosition(_rigidbody.position + forceVector);
            }
        }




        InteractableObject _interactable = obj.GetComponent<InteractableObject>();

        if (_interactable != null && _interactable.IsUsable)
        {
            if (directionVector.magnitude <= InteractDistance && (_interactable.IsUsableOutsideFOV || Vector3.Angle(directionVector, m_Transform.forward) <= FOV / 2f))
            {
                AddInteractable(obj.gameObject);
            }
            else
            {
                RemoveInteractable(obj.gameObject);
            }
        }



    }

    protected override void SightLost(GameObject obj)
    {
        if (obj == null || obj.transform == m_Transform)
            return;

        InteractableObject _interactable = obj.GetComponent<InteractableObject>();
        if (_interactable != null)
            RemoveInteractable(obj);
    }

    #region OnCollision / OnTrigger

  //  public virtual void OnTriggerEnter(Collider coll)
  //  {
  //      if (coll.transform == m_Transform || Utilities.IsInLayerMask(coll.gameObject, ignoreCollisionLayer))
  //          return;
  //      /*
		//IInteractable _interactable = coll.GetComponent<IInteractable>();
		
		//if(_interactable != null && _interactable.IsEnabled() && _interactable.IsUsableOutsideFOV()){
		//	AddInteractable(coll.gameObject);
		//}*/

  //      if (!coll.isTrigger)
  //      {
  //          AttributeHandler _handler = coll.GetComponent<AttributeHandler>();
  //          if (_handler != null)
  //          {
  //              //_handler.ShowUI(false);
  //          }
  //      }
  //  }

  //  public virtual void OnTriggerStay(Collider coll)
  //  {
  //      if (coll.transform == m_Transform || Utilities.IsInLayerMask(coll.gameObject, ignoreCollisionLayer))
  //          return;

  //      Vector3 toVector = coll.transform.position - m_Transform.position;

  //      if (ShowDebug)
  //      {
  //          Debug.DrawLine(m_Transform.position, coll.transform.position, Color.yellow);
  //          //Debug.DrawLine(myTransform.position, myTransform.position + (myTransform.forward * 5f), Color.cyan);
  //      }

  //      ICollectible _collectible = coll.GetComponent<ICollectible>();

  //      if (shouldCollect && _collectible != null)
  //      {
  //          Rigidbody _rigidbody = coll.GetComponent<Rigidbody>();

  //          if (toVector.magnitude <= CollectRange && _rigidbody != null)
  //          {
  //              Vector3 forceVector = -toVector * EXP_COLLECT_SPEED;
  //              _rigidbody.AddForce(forceVector);
  //          }
  //      }




  //      InteractableObject _interactable = coll.GetComponent<InteractableObject>();

  //      if (_interactable != null && _interactable.IsUsable)
  //      {
  //          if (toVector.magnitude <= InteractDistance && (_interactable.IsUsableOutsideFOV || Vector3.Angle(toVector, m_Transform.forward) <= FOV / 2f))
  //          {
  //              AddInteractable(coll.gameObject);
  //          }
  //          else
  //          {
  //              RemoveInteractable(coll.gameObject);
  //          }
  //      }



  //  }

  //  public virtual void OnTriggerExit(Collider coll)
  //  {
  //      if (coll.transform == m_Transform || Utilities.IsInLayerMask(coll.gameObject, ignoreCollisionLayer))
  //          return;

  //      InteractableObject _interactable = coll.GetComponent<InteractableObject>();

  //      if (_interactable != null)
  //          RemoveInteractable(coll.gameObject);


  //      if (!coll.isTrigger)
  //      {
  //          AttributeHandler _handler = coll.GetComponent<AttributeHandler>();
  //          if (_handler != null)
  //          {
  //              //_handler.HideUI();
  //          }
  //      }
  //  }

    #endregion



    public bool Equals(PlayerController other)
    {
        return Name.Equals(other.Name);
    }
    public void Copy(PlayerController other)
    {
        if (other == null)
            return;


        Copy(other as UnitController);

        Ability _ability = other.AuxiliaryAbility;
        HandheldItem _handheld = other.HandheldItem;

        other.DropAbility();
        other.DropHandheld();

        Pickup(_ability);
        Pickup(_handheld);


        CurrentLevel = other.CurrentLevel;
        CurrentExperience = other.CurrentExperience;
    }




    public override void OnDrawGizmos()
    {
        base.OnDrawGizmos();

        if (showDebug && m_Transform != null)
        {

            if (currentInteractable != null && currentInteractable.gameObject.activeInHierarchy)
            {
                Gizmos.color = Color.green;

                Gizmos.DrawLine(m_Transform.position, currentInteractable.transform.position);
            }



            Gizmos.color = Color.white;
            Gizmos.DrawLine(m_Transform.position, m_Transform.position + (m_Transform.forward * SightRange));
        }


    }

    public override void OnValidate()
    {
        base.OnValidate();

        CurrentLevel = CurrentLevel;
        CurrentExperience = CurrentExperience;

        CollectRange = CollectRange;

        InteractDistance = InteractDistance;
        ThrowPower = ThrowPower;
        
    }
}

