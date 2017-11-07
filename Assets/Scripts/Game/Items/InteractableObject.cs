﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;



[RequireComponent(typeof(AudioSource))]
public abstract class InteractableObject : MonoBehaviour, IIdentifier
{
    [SerializeField]
    string m_ObjectName = "Interactable Object";

    [SerializeField]
    Vector3 m_UIOffset = new Vector3(0, 1, 2);
    
    [SerializeField]
    protected int activationCost;


    [Header("Sounds")]

    [SerializeField]
    protected SoundClip activationSound;



    [SerializeField]
    [EnumFlags]
    UIManager.Component m_UIComponents;

    
    public event Delegates.Alert OnUse;
    public event EventHandler ObjectAcquired;

    protected List<GameObject> activatingObjects = new List<GameObject>();


    GenericUI activeUI;
    protected Transform m_Transform;
    protected AudioSource m_Audio;



    protected virtual void Awake()
    {
        m_Transform = GetComponent<Transform>();
        m_Audio = GetComponent<AudioSource>();
        m_Audio.loop = false;
        m_Audio.playOnAwake = false;
    }
    void OnDisable()
    {
        DeflateUI();
    }


    public virtual bool Interact(PlayerController controller)
    {
        if (activatingObjects.Count == 0)
            return false;


        bool isActivator = false;

        for (int i = 0; i < activatingObjects.Count; i++)
        {
            if (activatingObjects[i] == controller.gameObject)
                isActivator = true;
        }

        if (!isActivator)
            return false;





        Health playerHealth = controller.GetComponent<Health>();



        //if(Utilities.HasFlag(m_ActivationCosts.Currency, CurrencyType.Experience))
        //{
        //    expSum = m_ActivationCosts.ExperienceCost;
        //}

        //if (Utilities.HasFlag(m_ActivationCosts.Currency, CurrencyType.Health))
        //{
        //    healthSum = m_ActivationCosts.ExperienceCost;
        //}

        //if (Utilities.HasFlag(m_ActivationCosts.Currency, CurrencyType.LevelPoints))
        //{
        //    throw new NotImplementedException();
        //}

        //if (Utilities.HasFlag(m_ActivationCosts.Currency, CurrencyType.StatLevel))
        //{
        //    throw new NotImplementedException();
        //}

        //int expSum = 0;
        //int healthSum = 0;
        //int levelSum = 0;
        //Dictionary<StatType, int> statSum = new Dictionary<StatType, int>();


        //for (int i = 0; i < ActivationCosts.Count; i++)
        //{
        //    switch (ActivationCosts[i].Currency)
        //    {
        //        case CurrencyType.Experience:
        //            expSum += ActivationCosts[i].Value;
        //            break;
        //        case CurrencyType.Health:
        //            healthSum += ActivationCosts[i].Value;
        //            break;
        //        case CurrencyType.LevelPoints:
        //            levelSum += ActivationCosts[i].Value;
        //            break;
        //        case CurrencyType.StatLevel:
        //            if (statSum.ContainsKey(ActivationCosts[i].StatType))
        //            {
        //                statSum[ActivationCosts[i].StatType] += ActivationCosts[i].Value;
        //            }
        //            else
        //            {
        //                statSum.Add(ActivationCosts[i].StatType, ActivationCosts[i].Value);
        //            }
        //            break;
        //    }

        //}



        //bool isSuccess = controller.CanModifyExp(Mathf.RoundToInt(expSum));

        //if (!isSuccess)
        //   return false;



        //if (expSum != 0)
        //    controller.ModifyExp(Mathf.RoundToInt(expSum));


        //if (healthSum != 0)
        //    playerHealth.HealthArithmetic(healthSum, false, transform);

        if (!controller.CreditArithmetic(ActivationCost))
        {
            return false;
        }

        OnUseTrigger();

        PlaySound(activationSound);

        return true;
    }
    public abstract void Drop();

    /*
    protected bool CanAfford(CurrencyType _currency)
    {
        if (activationCosts.Count == 0)
            return true;


        float totalCost = 0;
        for(int i = 0; i < activationCosts.Count; i++)
        {
            if(activationCosts[i].ObjectA == _currency)
            {
                totalCost += activationCosts[i].ObjectB;
            }
        }


        switch (_currency)
        {

        }
    }
    */






    public virtual void InflateUI()
    {
        if (!this.enabled)
            return;

        if (activeUI != null && activeUI.gameObject.activeInHierarchy && activeUI.TargetTransform == m_Transform)
        {
            activeUI.SetFollowOffset(Vector3.zero);
        }
        else
        {
            if (ObjectPoolerManager.Instance == null)
                return;


            bool hasUIInflated = false;

            GameObject uiObj = ObjectPoolerManager.Instance.InteractableUIPooler.GetPooledObject();

            if (uiObj == null)
                return;

            if (m_Transform == null)
                m_Transform = GetComponent<Transform>();


            activeUI = uiObj.GetComponent<GenericUI>();

            uiObj.transform.position = transform.position;
            uiObj.SetActive(true);
            activeUI.Initialize(m_Transform);



            GameObject textUI;// = activeUI.GetPrefab("ID");

            if (!string.IsNullOrEmpty(Name) && (textUI = activeUI.GetPrefab("ID")) != null)
            {
                DisplayUI uiDisplay = textUI.GetComponent<DisplayUI>();
                textUI.SetActive(!string.IsNullOrEmpty(Name));


                activeUI.AddAttribute(new GenericUI.DisplayProperties("ID", new Orientation(m_UIOffset, Vector3.zero, Vector3.one), uiDisplay));
                activeUI.UpdateAttribute("ID", Name);

                hasUIInflated = true;
            }


            Transform tr = activeUI.GetParentTransform("Charges");

            if (tr != null)
            {
                tr.gameObject.SetActive(ActivationCost != 0);
            }

            if (ActivationCost != 0)
            {
                AddCostUI("Credits", ActivationCost);
            }


            hasUIInflated = true;

            if (hasUIInflated)
            {
                SoundClip uiSound = SoundManager.Instance.UI_Sound;
                if (m_Audio != null && uiSound.Sound != null)
                {
                    m_Audio.Stop();

                    m_Audio.volume = uiSound.Volume;
                    m_Audio.pitch = uiSound.Pitch;
                    m_Audio.PlayOneShot(uiSound.Sound);
                }
            }
        }
    }
    public virtual void DeflateUI()
    {
        if (activeUI == null || !activeUI.gameObject.activeInHierarchy || activeUI.TargetTransform != m_Transform)
            return;

        activeUI.Deflate();
    }


    private void AddCostUI(string name, int amount)
    {
        GameObject costUI = activeUI.GetPrefab("Cost");

        if (costUI == null)
            return;

        ProgressBarController controller = costUI.GetComponent<ProgressBarController>();
        controller.SetText(amount.ToString());

        costUI.SetActive(true);


        activeUI.AddAttribute(new GenericUI.DisplayProperties(name, new Orientation(Vector3.zero, Vector3.zero, Vector3.one), controller), amount < 0 ? "Cost" : "Reward");

    }


    protected virtual void OnTriggerEnter(Collider coll)
    {
        PlayerController pController = coll.GetComponent<PlayerController>();
        if (!coll.isTrigger && pController != null)
        {
            activatingObjects.Add(coll.gameObject);

        }
    }
    protected virtual void OnTriggerExit(Collider coll)
    {
        if (!coll.isTrigger)
        {
            activatingObjects.Remove(coll.gameObject);
        }
    }

    protected virtual void PlaySound(SoundClip sound)
    {
        if (sound.UseRemnant)
        {
            GameObject remnantObj = ObjectPoolerManager.Instance.AudioRemnantPooler.GetPooledObject();
            AudioRemnant remnantAudio = remnantObj.GetComponent<AudioRemnant>();

            remnantObj.SetActive(true);
            remnantAudio.PlaySound(sound);
        }
        else
        {
            m_Audio.volume = sound.Volume;
            m_Audio.pitch = sound.Pitch;

            if (sound.IsLooping)
            {
                m_Audio.loop = true;
                m_Audio.clip = sound.Sound;
                m_Audio.Play();
            }
            else
            {
                m_Audio.loop = false;
                m_Audio.PlayOneShot(sound.Sound);
            }
        }
    }

    #region Event Triggers

    protected void OnUseTrigger()
    {
        if (OnUse != null)
        {
            ObjectAcquired(this, EventArgs.Empty);
            OnUse();
        }
    }

    #endregion


    protected void OnObjectAcquired()
    {
        ObjectAcquired(this, EventArgs.Empty);
    }



    #region Accessors

    public string Name
    {
        get { return m_ObjectName; }
        set { m_ObjectName = value; }
    }

    public virtual bool IsUsable
    {
        get { return gameObject.activeInHierarchy; }
    }
    public abstract bool IsUsableOutsideFOV { get; }


    public GameObject Object
    {
        get
        {
            return gameObject;
        }
    }
    protected int ActivationCost
    {
        get { return activationCost; }
        set { activationCost = Mathf.Clamp(value, 0, value); }
    }

    #endregion

    protected virtual void OnValidate()
    {
        ActivationCost = ActivationCost;
    }
}
