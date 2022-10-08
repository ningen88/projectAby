using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerUnit : Entity
{
    private Vector3 mouseWorldPos;
    private Vector3 mouseWorldCell;
    private List<Vector3> availableCells;
    private int originalAPoints;
    private int originalInitiativePoints;
    private bool canMove;
    private bool death = false;
    private bool inAction = false;
    private bool walking = false;
    private Enemy enemy;
    private int lastHealth;
    
    private void Awake()
    {
        maskPath = Application.dataPath + "/StreamingAssets/obstaclesPosition.json";
        obsAgent = gameObject.GetComponent<ObstacleAgent>();
        anim = gameObject.GetComponent<Animator>();
        weaponEffects = gameObject.GetComponentInChildren<ShootingEffects>();
    }

    void Start()
    {
        InitializeVariables();
    }

    void Update()
    {
        ChooseState();
    }

    private void InitializeVariables()
    {
        gridMap = battleControl.GetComponent<GridMap>();
        drawFunctions = battleControl.GetComponent<DrawFunctions>();
        readObstacles = File.ReadAllLines(maskPath);
        availableCells = new List<Vector3>();
        originalAPoints = actionPoints;
        originalInitiativePoints = initiativePoints;
        canMove = true;
        guard = isNearObstacle(gameObject.transform.position);
        mouseWorldCell = Vector3.zero;
        lastHealth = health;
    }

    private void ChooseState()
    {
        guard = isNearObstacle(gameObject.transform.position);

        if (gameObject.transform.position.x == mouseWorldCell.x && gameObject.transform.position.z == mouseWorldCell.z)
        {
            anim.SetBool("Walk", false);
            if (walking)
            {
                soundManager.StopSound("footstepSand");
                walking = false;
            }
        }

        if (state == EntityState.WAIT_FOR_TURN)
        {
            StartCoroutine(WaitState());
        }
        if (state == EntityState.ACTIONS)
        {
            UpdateStatPanel();
            endTurnButton.interactable = true;
            EnableUnitSelector(true);
            StartCoroutine(ActionState());
        }
        if (state == EntityState.END_TURN)
        {
            StartCoroutine(EndTurnState());
        }
    }

    // STATES
    protected override IEnumerator WaitState()
    {
        initiativePoints = originalInitiativePoints;
        StartCoroutine(DeathCheck());
        if (health < lastHealth)
        {
            StartCoroutine(TakeDamage());
        }

        yield return null;
    }

    protected override IEnumerator ActionState()
    {
        if (actionPoints == 0)
        {
            ResetActionField();
            state = EntityState.END_TURN;
        }
        else
        {
            if (gridMap.isClick)                                                 
            {
                state = EntityState.END_TURN;
            }

            if (canMove)
            {
                DrawActionField(gameObject.transform.position);
                canMove = false;
            }
                        
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);            
            bool isAttacking;
            bool clickOnFriend;
            RangeAttack(ray, out isAttacking, out clickOnFriend);
            RangeMovement(ray, isAttacking, clickOnFriend);           
        }

        yield return null;
    }

    protected override IEnumerator EndTurnState()                                                     
    {
        ResetActionField();
        if (gridMap.EnemiesCount == 0)
        {
            StartCoroutine(EndGame());
        }

        EndTurnButtonCheck();
        Vector3 myPos = gameObject.transform.position;

        if(mouseWorldCell.x == myPos.x && mouseWorldCell.z == myPos.z )
        {
            inAction = false;
        }

        if(initiativePoints > 0)
        {
            DrawAttackPosition();
            InitiativeAttack();
        }
        else
        {
            ResetActionField();
        }

        if (gridMap.isClick)
        {
            actionPoints = originalAPoints;
            gridMap.isClick = false;
            gridMap.EditTargetEntryPosition(gameObject.name, gameObject.transform.position);
            gridMap.EditTargetEntryHealth(gameObject.name, health);
            canMove = true;
            yield return new WaitForSeconds(0.5f);

            if (guard.Item1)
            {
                anim.SetBool("Cover", true);
            }

            EnableUnitSelector(false);
            ResetActionField();
            state = EntityState.WAIT_FOR_TURN;
        }

        yield return null;
    }

    private void RangeAttack(Ray ray, out bool isAttacking, out bool clickOnFriend)
    {
        isAttacking = false;
        clickOnFriend = false;
        RaycastHit hitInfo;

        if (Physics.Raycast(ray, out hitInfo) && Input.GetMouseButtonDown(0) && actionPoints > 0)
        {
            anim.SetBool("Cover", false);
            Vector3 targetPosition = hitInfo.collider.gameObject.transform.position;
            float distFromTarget = Vector3.Distance(targetPosition, gameObject.transform.position);

            if (hitInfo.collider.CompareTag("unit"))
            {
                clickOnFriend = true;
            }

            if (hitInfo.collider.CompareTag("enemy") && distFromTarget <= weaponRange)
            {
                isAttacking = true;
                actionPoints--;
                initiativePoints--;
                enemy = hitInfo.collider.gameObject.GetComponent<Enemy>();
                TurnToEnemyDir();
                StartCoroutine(AttackRoutine());
            }
        }
    }

    private void InitiativeAttack()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hitInfo;
        if(Physics.Raycast(ray, out hitInfo) && Input.GetMouseButtonDown(0) && initiativePoints > 0)
        {
            anim.SetBool("Cover", false);
            Vector3 targetPosition = hitInfo.collider.gameObject.transform.position;
            float distFromTarget = Vector3.Distance(targetPosition, gameObject.transform.position);

            if(hitInfo.collider.CompareTag("enemy") && distFromTarget <= weaponRange)
            {
                initiativePoints--;
                enemy = hitInfo.collider.gameObject.GetComponent<Enemy>();
                TurnToEnemyDir();
                StartCoroutine(AttackRoutine());
            }
        }
    }

    private void RangeMovement(Ray ray, bool isAttacking, bool clickOnFriend)
    {
        Plane plane = new Plane(Vector3.up, 0);
        if (plane.Raycast(ray, out float distance))
        {
            mouseWorldPos = ray.GetPoint(distance);
            mouseWorldCell = SetPositionOnCell(mouseWorldPos);

            // click position is in range, is not on obstacle, the unit is not attacking and you have not clicked on an friendly unit
            if (Input.GetMouseButtonDown(0) && inRange(mouseWorldCell) && !isObstacle(mouseWorldCell) && !isAttacking && !clickOnFriend)
            {
                walking = true;
                anim.SetBool("Cover", false);
                anim.SetBool("Walk", true);
                soundManager.PlaySound("footstepSand");
                inAction = true;
                obsAgent.SetDestination(mouseWorldCell);
                DrawActionField(mouseWorldCell);
                actionPoints--;
            }
        }
    }

    private void TurnToEnemyDir()
    {
        Vector3 targetDir = (enemy.transform.position - gameObject.transform.position).normalized;
        Quaternion endRot = Quaternion.LookRotation(targetDir, gameObject.transform.up);
        StartCoroutine(RotateToTargetDir(gameObject.transform.rotation, endRot, 2.0f));
    }


    // These functions are used to handle the action field (the area in which a unit can move)
    void ResetActionField()
    {
        availableCells.Clear();
        GameObject[] lines = GameObject.FindGameObjectsWithTag("line");
        for(int i = 0; i < lines.Length; i++)
        {
            Destroy(lines[i]);
        }
    }

    void DrawActionField(Vector3 unitPosition)
    {
        int height = gridMap.getHeight();
        int width = gridMap.getWidth();
        ResetActionField();

        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                var fullMap = gridMap.GetGrid();
                Vector3 vecFullMap = new Vector3(fullMap[i, j].Item1, 0.0f, fullMap[i, j].Item2);
                float distance = Vector3.Distance(unitPosition, vecFullMap);

                if (isObstacle(vecFullMap)) continue;
                if (distance < maxDistance)
                {
                    drawFunctions.DrawCell(vecFullMap);
                    availableCells.Add(vecFullMap);
                }
            }
        }

        DrawAttackPosition();

    }

    void DrawAttackPosition()
    {
        Vector3[] enemiesPosition = gridMap.GetAllEnemiesPosition();
        for (int i = 0; i < enemiesPosition.Length; i++)
        {
            float dist = Vector3.Distance(gameObject.transform.position, enemiesPosition[i]);
            if (dist <= weaponRange)
            {
                drawFunctions.DrawCircle(enemiesPosition[i], 0.5f);
            }
        }
    }


    // return true if you click inside the available cells in the battleground
    bool inRange(Vector3 position)
    {
        if (position.x > -10 || position.x < 10 || position.z < 5 || position.z > -5)                                            // verified if you click on battleground
        {
            foreach(Vector3 avPos in availableCells)                                                                            // loop the available cells
            {
                if(Equals(avPos, position))                                                                                    // return false if the position is not on the list of available cells
                {
                    return true;
                }
            }
        }
        return false;
    }

    IEnumerator AttackRoutine()
    {
        inAction = true;
        yield return new WaitForSeconds(1.0f);
        anim.SetBool("Shoot", true);
        yield return new WaitForSeconds(1.5f);
        soundManager.PlaySound("laser");
        weaponEffects.StartShooting(enemy.transform.position + Vector3.up/2);
        yield return new WaitForSeconds(0.5f);
        Attack(enemy);
        inAction = false;
        anim.SetBool("Shoot", false);
        weaponEffects.StopShooting();
    }

    IEnumerator TakeDamage()
    {
        if (guard.Item1 == true)
        {
            anim.SetBool("HitCrouch", true);
        }
        else
        {
            anim.SetBool("Hit", true);
        }

        yield return new WaitForSeconds(1.5f);
        anim.SetBool("Hit", false);
        anim.SetBool("HitCrouch", false);
        lastHealth = health;
    }

    protected override IEnumerator DeathCheck()
    {
        if (health <= 0 && death == false)
        {
            death = true;
            gridMap.DeleteTargetKey(gameObject.name);
            gridMap.DeleteUnit(gameObject.name);
            gridMap.DeleteTargetEntry(gameObject.name);
            gridMap.DeleteTargetInfoEntry(gameObject.name);

            anim.SetBool("Death", true);
            yield return new WaitForSeconds(4.5f);
            gameObject.SetActive(false);
        }
    }

    private void EndTurnButtonCheck()
    {
        if (inAction || !isEndgamePanelDisable)
        {
            endTurnButton.interactable = false;
        }
        else
        {
            endTurnButton.interactable = true;
        }
    }

    protected override IEnumerator EndGame()
    {
        if (isEndgamePanelDisable)
        {
            drawFunctions.ShowWinMessage();            
            isEndgamePanelDisable = false;
            yield return new WaitForSeconds(5.0f);
            SceneManager.LoadScene("Title");
        }
       
        yield return null;
    }
}
