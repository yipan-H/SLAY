using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XGame;

public class PlayerScript : MonoSingleton<PlayerScript>, IHealth, IExperience, IHunger
{
    const float HUNGER_DECREASE_INTERVAL = 5f;
    const float MOVE_THRESHOLD = .1f;

    public Storage inventory;
    public float speed = 3.0f;
    public int health => Hp;
    public int maxHealth => MaxHp;

    private bool isMoving = false;
    private float hungerTimer = 0f;
    MeleeAttack meleeAttack;
    Rigidbody2D rigidbody2d;
    Animator animator;

    public void Init()
    {
        inventory = new Storage(40);

        ReadRecord(1);
        Level = 1;
        Hp = MaxHp;
        Hunger = MaxHunger;

        this.RegisterEvent<PlayerInteractEvent>(PlayerInteract);
        meleeAttack = GetComponent<MeleeAttack>();
        rigidbody2d = GetComponent<Rigidbody2D>();
        animator = transform.Find("Sprite").GetComponent<Animator>();
    }

    void ReadRecord(int level)
    {
        var record = XGame.MainController.GetRecord<Player_Record>(level);
        MaxExp = record.Exp;
        MaxHp = record.Hp;
        MaxHunger = record.Hunger;
        MaxLevel = XGame.MainController.GetRecords<Player_Record>().Count;
    }

    void PlayerInteract(PlayerInteractEvent e)
    {
        Debug.Log("got PlayerInteractEvent");
        switch (e.type)
        {
            case InteractType.Attack:
                if (meleeAttack.IsReady)
                {
                    animator.SetTrigger("Attack");
                    meleeAttack.Use();
                }
                break;
            default:
                break;
        }
    }

    // Update is called once per frame
    void Update()
    {
        HandleHunger();
    }

    private void FixedUpdate()
    {
        HandleMoving();
    }

    void HandleHunger()
    {
        hungerTimer += Time.deltaTime;
        int hungerDecreaseUnit = (int)Math.Floor(hungerTimer / HUNGER_DECREASE_INTERVAL);
        if (hungerDecreaseUnit <= 0) return;
        hungerTimer -= (float)hungerDecreaseUnit * HUNGER_DECREASE_INTERVAL;
        int hungerDecrease = hungerDecreaseUnit * 10;
        if (hungerDecrease <= 0) return;

        Debug.Log(string.Format("player's hunger -{0}", hungerDecrease));
        int newHunger = Hunger - hungerDecrease;
        Hunger = newHunger < 0 ? 0 : newHunger;
    }

    private void HandleMoving()
    {
        var joystick = JoystickSingleton.instance;
        if (joystick == null) return;

        var x = joystick.Horizontal;
        var y = joystick.Vertical;
        var offset = new Vector2(x, y);
        isMoving = offset.magnitude >= MOVE_THRESHOLD;
        if (isMoving)
        {
            float step = speed * Time.deltaTime;
            rigidbody2d.MovePosition(rigidbody2d.position + step * offset);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        switch (collision.tag)
        {
            case "掉落物":
                DropItemScript dropItemScript = collision.gameObject.GetComponent<DropItemScript>();
                Item remainItem = inventory.AddItem(dropItemScript.item);
                if (remainItem == null)
                {
                    collision.SendMessage("Pick");
                }


                //
                //if(!XGame.MainController.NeedGamePause())
                //{

                //}
                //AudioManager.Instance.PlayOneShot(AudioModel.Cloect);
                //AudioManager.Instance.StopAudio(AudioModel.Cloect);
                //EventCenterManager.Send<SentUpdateGoldText>(new SentUpdateGoldText() { isShowPalu = true });  //发送
                //XGame.MainController.DataMgr.AddReward(Reward.Gold,50);



                //XGame.MainController.ShowUI<UI_Menu>(new MenuDate()
                //{
                //    PALUID = 1,

                //    dongtaiwuti = this.gameObject,
                //}) ;
                //XGame.MainController.HideUI<UI_Menu>();
                break;
            default:
                break;
        }
    }

    private void OnDestroy()
    {
        this.UnRegisterEvent<PlayerInteractEvent>();
    }

    public int Hp { get; protected set; }

    public int MaxHp { get; protected set; }

    public void Heal(int value)
    {
        Hp = Math.Clamp(Hp + value, 0, MaxHp);
    }

    public void Hurt(int value)
    {
        Debug.Log(String.Format("Player got hurt, damage: {0}", value));
        animator.SetTrigger("Hurt");
        Hp = Math.Clamp(Hp - value, 0, MaxHp);
    }

    int _Exp;
    public int Exp
    {
        get => _Exp;
        protected set
        {
            _Exp = value;
            EventCenterManager.Send(new ExpUpdatedEvent
            {
                min = 0,
                max = MaxExp,
                value = _Exp
            });
        }
    }

    public int MaxExp { get; protected set; }

    public int Level { get; protected set; }

    public int MaxLevel { get; protected set; }


    public void GainExp(int value)
    {
        if (Level == MaxLevel) return;

        int newExp = Exp + value;
        if (newExp >= MaxExp)
        {
            Exp = 0;
            LevelUp();
        }
        else
        {
            Exp = newExp;
        }
    }

    protected void LevelUp()
    {
        if (Level >= MaxLevel) return;
        Level++;
        ReadRecord(Level);
        Hp = MaxHp;
        Hunger = MaxHunger;
        XGame.MainController.ShowTips("Level Up");
    }

    int _Hunger;
    public int Hunger
    {
        get => _Hunger;
        protected set
        {
            _Hunger = value;
            EventCenterManager.Send(new HungerUpdatedEvent
            {
                min = 0,
                max = MaxHunger,
                value = Hunger
            });
        }
    }

    public int MaxHunger { get; protected set; }

    public void HungerInc(int value)
    {
        Hunger = Math.Clamp(Hunger + value, 0, MaxHunger);
    }

    public void HungerDec(int value)
    {
        Hunger = Math.Clamp(Hunger - value, 0, MaxHunger);
    }
}
