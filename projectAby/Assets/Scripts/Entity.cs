using System.Collections;
using UnityEngine;

using UnityEngine.AI;
using UnityEngine.UI;
using TMPro;

public abstract class Entity : MonoBehaviour
{
    public enum EntityState
    {
        WAIT_FOR_TURN,
        ACTIONS,
        END_TURN,
        INVALID
    }

    // Entity stats
    public int health;
    public int initiative;
    public (bool, Vector3) guard;
    public float armor;
    public float weaponRange;
    public float damage;
    public EntityState state;

    [SerializeField] protected int actionPoints;
    [SerializeField] protected float maxDistance;
    [SerializeField] protected int critChance;
    [SerializeField] protected float critDamage;
    [SerializeField] protected Sprite icon;
    [SerializeField] protected Image personalImage;
    [SerializeField] protected TMP_Text statText;
    [SerializeField] protected TMP_Text infoText;
    [SerializeField] protected SoundManager soundManager;
    [SerializeField] protected GameObject damageObj;
    [SerializeField] protected int initiativePoints;

    protected NavMeshAgent agent;
    [SerializeField] protected GameObject battleControl;
    [SerializeField] protected Button endTurnButton;
    protected GridMap gridMap;
    protected DrawFunctions drawFunctions;
    protected string maskPath;
    protected string[] readObstacles;
    protected ObstacleAgent obsAgent;
    protected Animator anim;
    protected ShootingEffects weaponEffects;
    protected bool isEndgamePanelDisable = true;

    private string critString;

    // Every Entity must have this state functions
    protected abstract IEnumerator WaitState();
    protected abstract IEnumerator ActionState();
    protected abstract IEnumerator EndTurnState();

    /////////////////////////////////////////////

    protected abstract IEnumerator DeathCheck();
    protected abstract IEnumerator EndGame();


    // return the critDamage if an hit is a crit
    protected float CalculateCrit()
    {
        int rangeCrit = Random.Range(1, 101);
        critString = "";

        if (rangeCrit <= critChance)
        {
            critString = "Crit!";
            return critDamage;
        }

        return 1.0f;
    }

    // calculate the total damage
    protected int InflictDamage(float enemyArmor)
    {
        // get the bonus damage (TODO)
        float bonus = 0.0f;

        // get the crit Damage
        float crit = CalculateCrit();

        // Calculate the damage
        float num = 50.0f * (1.0f + bonus);
        float den = (50.0f + enemyArmor) * (1.0f - bonus);
        float rawDamageM = (damage * num * crit) / den;                                                       // crit multiplicative (critDamage start at 1.5 end 2.5)

        return Mathf.RoundToInt(rawDamageM);
    }

    // perform an attack on an enemy (cover give protection)
    protected void Attack(Entity enemy)
    {
        float bonusGuard = 0;

        // if the enemy is near an obstacle
        if (enemy.guard.Item1 == true)
        {
            Vector3 dirFromTarget = (gameObject.transform.position - enemy.gameObject.transform.position).normalized;           // direction from target to me 
            Vector3 dirEnemytoObs = (enemy.guard.Item2 - enemy.gameObject.transform.position).normalized;                       // direction from target to obstacle

            float visibilityTest = Vector3.Dot(dirFromTarget, dirEnemytoObs);                                                   // we test if the target is behind some cover using dot product

            if (visibilityTest > 0)
            {
                bonusGuard = 50.0f;
            }
            else if (visibilityTest == 0)
            {
                bonusGuard = 25.0f;
            }
        }
        int damage = InflictDamage(enemy.armor + bonusGuard);

        enemy.health -= damage;
        StartCoroutine(DisplayDamageOnScreen(enemy, damage));
    }

    IEnumerator DisplayDamageOnScreen(Entity enemy, float damage)
    {
        infoText.text = gameObject.name + " attack " + enemy.name + " and dealt " + damage.ToString() + " damage. " + critString + "\nEnemy remaining health: " + enemy.health.ToString();
        Vector3 damageTextPos = enemy.transform.position + new Vector3(0, 0.5f, 0);
        GameObject dmgTxt = Instantiate(damageObj, damageTextPos, damageObj.transform.rotation);
        dmgTxt.GetComponentInChildren<TextMeshProUGUI>().text = critString + "\n" + damage.ToString();

        float step = 0.05f;

        for(int i = 0; i < 4; i++)
        {
            yield return new WaitForSeconds(0.5f);
            dmgTxt.transform.position += new Vector3(0.0f, step, 0.0f);
        }

        Destroy(dmgTxt);
    }

    // return true if position is near an obstacle and the unit need protection. Also return the obstacle position
    protected (bool, Vector3) isNearObstacle(Vector3 position)
    {
        (bool, Vector3) response;
        response.Item1 = false;
        response.Item2 = Vector3.zero;
        Obstacles[] obs = new Obstacles[readObstacles.Length];

        for (int i = 0; i < readObstacles.Length; i++)
        {
            obs[i] = JsonUtility.FromJson<Obstacles>(readObstacles[i]);
            if (position.x > obs[i].xPos - 1.5f && position.x < obs[i].xPos + 1.5f && position.z > obs[i].zPos - 1.5f && position.z < obs[i].zPos + 1.5f)
            {
                response.Item1 = true;
                response.Item2 = new Vector3(obs[i].xPos, 0.0f, obs[i].zPos);
            }
        }

        return response;
    }
   
    // return true if the object in a position is an obstacle
    protected bool isObstacle(Vector3 position)
    {
        Obstacles[] obs = new Obstacles[readObstacles.Length];

        for (int i = 0; i < readObstacles.Length; i++)
        {
            obs[i] = JsonUtility.FromJson<Obstacles>(readObstacles[i]);
            if (obs[i].xPos == position.x && obs[i].zPos == position.z)
            {
                return true;
            }
        }
        return false;
    }

    // this function transform the originalPos in a new position that is centered inside a cell
    protected Vector3 SetPositionOnCell(Vector3 originalPos)
    {
        Vector3 newPosition = Vector3.zero;
        if (originalPos.x > 0 && originalPos.z > 0)
        {
            newPosition.x = (int)originalPos.x + 0.5f;
            newPosition.z = (int)originalPos.z + 0.5f;
        }
        if (originalPos.x > 0 && originalPos.z < 0)
        {
            newPosition.x = (int)originalPos.x + 0.5f;
            newPosition.z = (int)originalPos.z - 0.5f;
        }
        if (originalPos.x < 0 && originalPos.z < 0)
        {
            newPosition.x = (int)originalPos.x - 0.5f;
            newPosition.z = (int)originalPos.z - 0.5f;

        }
        if (originalPos.x < 0 && originalPos.z > 0)
        {
            newPosition.x = (int)originalPos.x - 0.5f;
            newPosition.z = (int)originalPos.z + 0.5f;
        }
        return newPosition;
    }

    protected IEnumerator RotateToTargetDir(Quaternion startRot, Quaternion endRot, float speed)
    {        
        float t = 0;

        while (t < 1.0f)
        {
            gameObject.transform.rotation = Quaternion.Slerp(startRot, endRot, t);
            t += Time.deltaTime * speed;
            if (t <= 1.0f)
            {
                yield return null;
            }
        }
    }

    protected void EnableUnitSelector(bool state)
    {
        gameObject.transform.GetChild(0).gameObject.SetActive(state);
    }

    protected void UpdateStatPanel()
    {
        personalImage.sprite = icon;
        statText.text = health.ToString() + "\n" + damage.ToString() + "\n" + armor.ToString();
    }
}
