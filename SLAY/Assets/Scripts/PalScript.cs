using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.VisualScripting;
using System.Collections;

public struct PalType
{
    public string name;
    public string description;
    public int maxHp;
    public int maxHunger;
    public int money;
    public int baseLevel; //初始等级
    public Dictionary<string, int> workSkills;
    public List<string> battleSkills;

    public PalType(string name, string desc, int maxHp, int maxHunger, int money)
    {
        this.name = name;
        this.description = desc;
        this.maxHp = maxHp;
        this.maxHunger = maxHunger;
        this.money = money;

        this.baseLevel = 1;
        workSkills = new Dictionary<string, int>();
        battleSkills = new List<string>();
    }
}

public enum PalBelong
{
    Wild, Home
}

public static class PalTypes
{
    static Dictionary<string, PalType> Dic;

    static PalTypes()
    {
        Dic = new Dictionary<string, PalType>
        {
            { "普通帕鲁", new PalType("普通帕鲁", "", 100, 100, 10) },
            { "搬运帕鲁", new PalType("搬运帕鲁", "", 120, 120, 10) },
            { "制作帕鲁", new PalType("制作帕鲁", "", 80, 80, 10) },
            { "料理帕鲁", new PalType("料理帕鲁", "", 90, 90, 10) },
            { "金币帕鲁", new PalType("金币帕鲁", "", 150, 150, 100) }
        };
    }

    public static PalType Get(string key)
    {
        return Dic[key];
    }
}

public struct PalBattleSkill
{
    public string name;
    public string description;
    public int damage;

    public PalBattleSkill(string name, string description, int damage)
    {
        this.name = name;
        this.description = description;
        this.damage = damage;
    }
}

public static class PalBattleSkills
{
    public static Dictionary<string, PalBattleSkill> skills;

    public static void Init()
    {
        skills = new Dictionary<string, PalBattleSkill>
        {
            { "技能1", new PalBattleSkill("技能1", "", 35) },
            { "技能2", new PalBattleSkill("技能2", "", 50) }
        };
    }
}

public class Pal : IHealth, IExperience
{
    public string TypeName;

    public bool IsAggro = false;

    public bool IsCaptured;

    public Dictionary<string, int> workSkills = new Dictionary<string, int>();
    public List<string> battleSkills = new List<string>();

    public Pal(string typeName)
    {
        TypeName = typeName;

        MaxHp = Type.maxHp;
        Hp = MaxHp;
    }

    public PalType Type
    {
        get
        {
            return PalTypes.Get(TypeName);
        }
    }

    public int Hp { get; protected set; }

    public int MaxHp { get; protected set; }

    public virtual void Heal(int value)
    {
        Hp = System.Math.Clamp(Hp + value, 0, MaxHp);
    }

    public virtual void Hurt(int value)
    {
        Hp = System.Math.Clamp(Hp - value, 0, MaxHp);
    }

    public int Exp { get; protected set; }

    public int MaxExp { get; protected set; }

    public int Level { get; protected set; }

    public int MaxLevel { get; protected set; }

    public virtual void GainExp(int value)
    {
        // TODO
    }

    // 设置工作技能
    public void SetWorkSkill(string skillName, int skillLevel)
    {
        workSkills[skillName] = skillLevel;
    }

    // 移除工作技能
    public void RemoveWorkSkill(string skillName)
    {
        workSkills.Remove(skillName);
    }

    // 添加战斗技能
    public void AddBattleSkill(string skillName)
    {
        if (!battleSkills.Contains(skillName))
        {
            battleSkills.Add(skillName);
        }
    }

    // 移除战斗技能
    public void RemoveBattleSkill(string skillName)
    {
        battleSkills.Remove(skillName);
    }

    public int MinDamage { get => 5; }
    public int MaxDamage { get => 10; }

    public int GetDamage()
    {
        return Random.Range(MinDamage, MaxDamage + 1);
    }
}

public enum PalState
{
    Idle,
    Working,
    Stroll,
    Fighting,
    Dead,
    Return
}

public class PalScript : MonoBehaviour
{
    public Pal pal;
    public PalState state;
    public float BaseSpeed = 2f;
    float AdditionalSpeed = 0;
    public float speed { get => Mathf.Max(BaseSpeed + AdditionalSpeed, 0); }
    public bool moveWhileIdle = true;
    public GameObject menu;

    // 闲逛目的地
    private Vector3 StrollTargetPosition;
    Rigidbody2D rigidbody2d;
    TextMeshProUGUI text;
    Animator animator;

    private void Awake()
    {
        text = transform.Find("Canvas/HP").GetComponent<TextMeshProUGUI>();
        rigidbody2d = GetComponent<Rigidbody2D>();
        animator = transform.Find("Sprite").GetComponent<Animator>();
        MeleeAttackTimer = MeleeAttackCd;
        TerritoryCenter = transform.position;
    }

    // Start is called before the first frame update
    void Start()
    {
        state = PalState.Idle;
        pal = new Pal("普通帕鲁");
    }

    // Update is called once per frame
    void Update()
    {
        switch (state)
        {
            case PalState.Idle:
                HandleIdle();
                break;
            case PalState.Working:
                break;
            case PalState.Dead:
                break;
            default:
                break;
        }
    }

    private void FixedUpdate()
    {
        switch (state)
        {
            case PalState.Stroll:
                HandleStroll();
                break;
            case PalState.Fighting:
                HandleFighting();
                break;
            case PalState.Return:
                HandleReturn();
                break;
            default:
                break;
        }
    }

    const float FIGHT_SPEED_INC = .2f;
    const float RETURN_SPEED_INC = 2f;
    void ChangeState(PalState toState)
    {
        if (state == toState) return;

        if (state == PalState.Fighting)
        {
            AdditionalSpeed -= FIGHT_SPEED_INC;
        }
        else if (toState == PalState.Fighting)
        {
            AdditionalSpeed += FIGHT_SPEED_INC;
        }

        if (state == PalState.Return)
        {
            AdditionalSpeed -= RETURN_SPEED_INC;
        }
        else if (toState == PalState.Return)
        {
            AdditionalSpeed += RETURN_SPEED_INC;
        }

        state = toState;
        SyncStatusUI();
    }

    void HandleIdle()
    {
        if (moveWhileIdle)
        {
            StrollTargetPosition = TerritoryCenter + Random.insideUnitCircle * TerritoryRadius;
            ChangeState(PalState.Stroll);
        }
    }

    float StrollTimer = 0f;
    const float MOVE_TIMER_MAX = 5f;
    const float ARRIVE_THRESHOLD = .1f;
    private void HandleStroll()
    {
        MoveToTarget(StrollTargetPosition);
        StrollTimer += Time.deltaTime;

        if (StrollTimer >= MOVE_TIMER_MAX || Vector2.Distance(rigidbody2d.position, StrollTargetPosition) < ARRIVE_THRESHOLD)
        {
            ChangeState(PalState.Idle);
            StrollTimer = 0f;
        }
    }

    // TODO: 改为寻路算法
    void MoveToTarget(Vector2 target)
    {
        rigidbody2d.MovePosition(Vector2.MoveTowards(rigidbody2d.position, target, Time.deltaTime * speed));
    }

    const float TRACE_DISTANCE_MIN = 2f;
    float LeaveFightTimer = 0f;
    const float LEAVE_FIGHT_THRESHOLD = 5f;
    public void HandleFighting()
    {
        // Handle leaving fight
        if (Vector2.Distance(TerritoryCenter, transform.position) > TerritoryRadius)
        {
            LeaveFightTimer += Time.deltaTime;
            if (LeaveFightTimer >= LEAVE_FIGHT_THRESHOLD)
            {
                ChangeState(PalState.Return);
                return;
            }
        }
        else
        {
            LeaveFightTimer = 0f;
        }


        // Move toward player
        var playerPosition = PlayerScript.Instance.transform.position;
        float playerDistance = Vector2.Distance(playerPosition, rigidbody2d.position);
        if (playerDistance > TRACE_DISTANCE_MIN)
        {
            MoveToTarget(playerPosition);
        }
        else
        {
            // Attack if within range
            if (IsMeleeAttackReady)
            {
                MeleeAttack();
            }
        }
    }

    public void Hurt(int amount)
    {
        if (state == PalState.Dead)
        {
            return;
        }

        ChangeState(PalState.Fighting);
        pal.Hurt(amount);
        Debug.Log(string.Format("Pal {0} get hurt, damage: {1}, health: {2}", name, amount, pal.Hp));
        if (pal.Hp <= 0)
        {
            Die();
        }
        SyncStatusUI();
    }

    void Die()
    {
        Debug.Log("pal died");
        ChangeState(PalState.Dead);
        PlayerScript.Instance.GainExp(50);
        Destroy(gameObject);
    }

    public void Follow()
    {
        Debug.Log("pal follow");
    }

    public void Inspect()
    {
        Debug.Log("pal inspected");
    }

    void SyncStatusUI()
    {
        var newText = "HP: " + pal.Hp.ToString();
        if (text.text != newText)
        {
            text.text = newText;
        }

        var newColor = state == PalState.Fighting ? Color.red : Color.white;
        if (text.color != newColor)
        {
            text.color = newColor;
        }
    }

    bool IsMeleeAttackReady => MeleeAttackTimer >= MeleeAttackCd;
    float MeleeAttackCd = 3f;
    void MeleeAttack()
    {
        if (!IsMeleeAttackReady) return;

        animator.SetTrigger("Attack");
        PlayerScript.Instance.Hurt(pal.GetDamage());
        StartCoroutine(MeleeAttackRegain());
    }

    float MeleeAttackTimer = 0f;
    IEnumerator MeleeAttackRegain()
    {
        MeleeAttackTimer = 0f;
        while (MeleeAttackTimer < MeleeAttackCd)
        {
            yield return null;
            MeleeAttackTimer += Time.deltaTime;
        }
        Debug.Log("melee attack regained");
    }

    bool IsCastAttackReady = true;
    void CastAttack()
    {
        // TODO
    }

    public float TerritoryRadius = 4f;
    Vector2 TerritoryCenter;

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        if (TerritoryCenter != null)
        {
            Gizmos.DrawWireSphere(TerritoryCenter, TerritoryRadius);
        }
    }

    void HandleReturn()
    {
        MoveToTarget(TerritoryCenter);

        if (Vector2.Distance(rigidbody2d.position, TerritoryCenter) < ARRIVE_THRESHOLD)
        {
            ChangeState(PalState.Idle);
        }
    }
}
