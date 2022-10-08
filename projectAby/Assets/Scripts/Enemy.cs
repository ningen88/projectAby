using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public class Enemy : Entity
{
    public enum Type
    {
        MELEE,
        RANGED
    }

    private struct Cell
    {
        public Vector3 position;
        public int cost;
    } 

    private Vector3 endPosition;
    private Entity priorityTarget;                                                                                  
    private int originalAPoints;
    private int originalInitiativePoints;
    private List<Cell> availableCells;
    private List<string> targetNames;
    private int lastHealth;
    private bool multipleCall;
    

    [SerializeField] Type enemyType;
    
    
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
        guard = isNearObstacle(gameObject.transform.position);
        agent = gameObject.GetComponent<NavMeshAgent>();
        originalAPoints = actionPoints;
        originalInitiativePoints = initiativePoints;
        endPosition = Vector3.zero;
        availableCells = new List<Cell>();
        gridMap.AddEnemyPosition(gameObject.name, gameObject.transform.position);
        targetNames = gridMap.GetTargetKeys();
        priorityTarget = ChoosePriorityTarget();
        lastHealth = health;
        multipleCall = true;
    }

    private void UpdateStatus()
    {
        if (multipleCall)
        {
            multipleCall = false;
            UpdateStatPanel();
            endTurnButton.interactable = false;
            EnableUnitSelector(true);
            priorityTarget = ChoosePriorityTarget();            
        }
    }

    private void ChooseState()
    {
        if (health < lastHealth)
        {
            StartCoroutine(TakeDamage());
        }
        if (state == EntityState.WAIT_FOR_TURN)
        {
            StartCoroutine(WaitState());
        }
        if (state == EntityState.ACTIONS)
        {
            UpdateStatus();

            if (priorityTarget == null) return;
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
        multipleCall = true;
        initiativePoints = originalInitiativePoints;

        if (enemyType == Type.RANGED)
        {
            anim.SetBool("isFiring", false);
        }
        if(enemyType == Type.MELEE)
        {
            anim.SetBool("hitEnemy", false);
        }

        StartCoroutine(DeathCheck());

        if (gridMap.AllTargetsDead())
        {
            StartCoroutine(EndGame());
        }
        yield return null;
    }

    protected override IEnumerator ActionState()
    {
        if (actionPoints == 0)
        {
            state = EntityState.END_TURN;
        }
        else
        {
            yield return StartCoroutine(PerformOneAction());
        }

        yield return null;
    }

    protected IEnumerator PerformOneAction()
    {
        TurnToTarget();
        if (enemyType == Type.MELEE)
        {
            endPosition = CalculateEndPositionMelee();
        }
        else if (enemyType == Type.RANGED)
        {
            endPosition = CalculateEndPositionRanged();
            guard = isNearObstacle(endPosition);
        }

        obsAgent.SetDestination(endPosition);        
        actionPoints--;

        yield return null;
    }

    protected override IEnumerator EndTurnState()
    {
        if (gameObject.transform.position.x == endPosition.x && gameObject.transform.position.z == endPosition.z)
        {
            anim.SetBool("isWalking", false);
            TurnToTarget();

            yield return StartCoroutine(HandleEnemyTypeEndTurn());
           
            gridMap.EditEnemyPosition(gameObject.name, gameObject.transform.position);
            actionPoints = originalAPoints;
            
            if (guard.Item1 == true) anim.SetBool("onGuard", true);

            yield return new WaitForSeconds(2.5f);
            EnableUnitSelector(false);
            state = EntityState.WAIT_FOR_TURN;
        }

        yield return null;        
    }

    IEnumerator HandleEnemyTypeEndTurn()
    {
        if (enemyType == Type.RANGED)
        {
            soundManager.StopSound("footstepSand");

            if (RangedNeedToAttack() && initiativePoints > 0)
            {
                initiativePoints--;
                yield return StartCoroutine(AttackRoutine());
            }
        }
        if (enemyType == Type.MELEE)
        {
            soundManager.StopSound("footstepMelee");
            float targetDistance = Vector3.Distance(priorityTarget.transform.position, gameObject.transform.position);
            if (MeleeNeedToAttack() && initiativePoints > 0 && targetDistance < 2.0f)
            {
                initiativePoints--;
                yield return StartCoroutine(MeleeAttackroutine());
            }
        }
    }

    // with this function a melee unit calculate the end position based on target distance (from game object position to target position)
    // and cell cost.
    Vector3 CalculateEndPositionMelee()
    {
        float actualCost = Mathf.Infinity;
        Vector3 position = gameObject.transform.position;
        float targetDistance = Vector3.Distance(priorityTarget.transform.position, gameObject.transform.position);
        ActionField(priorityTarget.transform.position);

        for (int i = 0; i < availableCells.Count; i++)
        {
            if (MeleeNeedToAttack() && targetDistance < 2.0f)
            {
                initiativePoints--;
                TurnToTarget();
                StartCoroutine(MeleeAttackroutine());
                break;
            }
            else if (availableCells[i].cost < actualCost && availableCells[i].cost != 0)
            {
                actualCost = availableCells[i].cost;
                position = availableCells[i].position;
            }
        }

        anim.SetBool("isWalking", true);
        soundManager.PlaySound("footstepMelee");
        return position;
    }

    Vector3 CalculateEndPositionRanged()
    {
        anim.SetBool("onGuard", false);

        Vector3 position;
        ActionField(priorityTarget.transform.position);
        AdvancedActionField(priorityTarget.transform.position);

        if (SearchForCover(out position))
        {
            anim.SetBool("isWalking", true);
            soundManager.PlaySound("footstepSand");
            return position;
        }

        MoveAndStrike(out position);

        return position;
    }

    // Return true if a cell cost is 0 in the available cells
    private bool MeleeNeedToAttack()
    {
        if (priorityTarget.health <= 0) return false;

        for (int i = 0; i < availableCells.Count; i++)
        {
            if (availableCells[i].cost == 0)
            {
                return true;
            }
        }
        return false;
    }

    // Return true if the target is alive and the distance to target is <= than the weapon range
    private bool RangedNeedToAttack()
    {
        float targetDist = Vector3.Distance(priorityTarget.transform.position, gameObject.transform.position);

        if (targetDist <= weaponRange && priorityTarget.health > 0) return true;

        return false;
        
    }

    private bool SearchForCover(out Vector3 position)
    {
        float maxDot = -Mathf.Infinity;
        bool needCover = false;
        position = gameObject.transform.position;

        for (int i = 0; i < availableCells.Count; i++)
        {
            Vector3 cellPos = availableCells[i].position;
            float guardAttackDistance = Vector3.Distance(priorityTarget.transform.position, cellPos);

            // the new position is near an obstacle and the target can be attacked from that position
            if (isNearObstacle(cellPos).Item1 && guardAttackDistance <= weaponRange)
            {
                Vector3 obsPos = isNearObstacle(cellPos).Item2;

                // if the cover is already taken or you are already in cover don't move to that position
                if (areTargetsNearObstacle(obsPos) || guard.Item1 == true)
                {
                    needCover = false;
                    break;
                }

                Vector3 dirPosToTarget = (cellPos - priorityTarget.transform.position).normalized;
                Vector3 dirPosToObs = (cellPos - obsPos).normalized;

                float visibilityTest = Vector3.Dot(dirPosToTarget, dirPosToObs);

                // search for the position that give the most protection
                if (visibilityTest > 0 && visibilityTest > maxDot)
                {
                    needCover = true;
                    position = cellPos;
                    maxDot = visibilityTest;
                }
            }
        }
        return needCover;
    }

    private void MoveAndStrike(out Vector3 position)
    {
        position = gameObject.transform.position;

        // LOOK IF CAN ATTACK A TARGET OR IF HAVE TO MOVE TO ANOTHER POSITION //
        if (RangedNeedToAttack())
        {
            initiativePoints--;
            TurnToTarget();
            StartCoroutine(AttackRoutine());
        }
        else
        {
            float actualCost = Mathf.Infinity;

            for (int i = 0; i < availableCells.Count; i++)
            {
                if (availableCells[i].cost < 0)
                {
                    actualCost = availableCells[i].cost;
                    position = availableCells[i].position;
                }

                if (availableCells[i].cost < actualCost && availableCells[i].cost > 2)
                {
                    actualCost = availableCells[i].cost;
                    position = availableCells[i].position;
                }
            }

            anim.SetBool("isWalking", true);
            soundManager.PlaySound("footstepSand");
        }
    }

    private void TurnToTarget()
    {
        Vector3 targetDir = (priorityTarget.transform.position - gameObject.transform.position).normalized;
        Quaternion endRot = Quaternion.LookRotation(targetDir, gameObject.transform.up);
        StartCoroutine(RotateToTargetDir(gameObject.transform.rotation, endRot, 2.0f));
    }

    // Add the available cells to the list of available cells and set position and cell cost (based on cell distance from target)
    void ActionField(Vector3 targetPos)
    {
        int width = gridMap.getWidth();
        int height = gridMap.getHeight();
        availableCells.Clear();

        Vector3 targetDirection = (targetPos - gameObject.transform.position).normalized;

        for(int i = 0; i < height; i++)
        {
            for(int j = 0; j < width; j++)
            {
                var fullMap = gridMap.GetGrid();
                Vector3 vecFullMap = new Vector3(fullMap[i,j].Item1, 0.0f, fullMap[i,j].Item2);
               
                if (isObstacle(vecFullMap) || isCellOccupied(vecFullMap, targetPos) || isCellBehind(targetDirection, vecFullMap) || invalidCellNearObstacle(vecFullMap)) continue;

                float distance = Vector3.Distance(gameObject.transform.position, vecFullMap);
                
                if (distance < maxDistance)
                {
                    int targetDistance = (int)Vector3.Distance(targetPos, vecFullMap);
                    Cell cell = new Cell();
                    cell.position = vecFullMap;
                    cell.cost = targetDistance;
                    availableCells.Add(cell);
                }
            }
        }       
    }

    // AdvancedActionField must be called after ActionField(only for range unit) and it allow a unit to get in a better position
    // when the target is on cover and when is not already targeted
    void AdvancedActionField(Vector3 targetPos)
    {
        if (isNearObstacle(targetPos).Item1)
        {
            Vector3 obstaclePosition = isNearObstacle(targetPos).Item2;

            for (int i = 0; i < availableCells.Count; i++)
            {
                Vector3 dirTargetToObstacle = (obstaclePosition - targetPos).normalized;
                Vector3 dirTargetToCell = (availableCells[i].position - targetPos).normalized;
                float takeFromBack = Vector3.Dot(dirTargetToObstacle, dirTargetToCell);

                if (takeFromBack < 0 && availableCells[i].cost >= 2)
                {
                    Cell specialCell = new Cell();
                    specialCell.position = availableCells[i].position;
                    specialCell.cost = -availableCells[i].cost;
                    availableCells.Add(specialCell);
                }
            }
        }
        
    }

    bool isCellBehind(Vector3 targetDirection, Vector3 cellPosition)
    {
        Vector3 cellDirection = (cellPosition - gameObject.transform.position).normalized;

        if(Vector3.Dot(targetDirection, gameObject.transform.forward) <= 0)
        {
            if (Vector3.Dot(cellDirection, gameObject.transform.forward) > 0)
            {
                return true;
            }
            else return false;            
        }
        else
        {
            if (Vector3.Dot(cellDirection, gameObject.transform.forward) < 0)
            {
                return true;
            }
            else return false;
        }   
    }

    // return true if another ally units or a target is on position
    bool isCellOccupied(Vector3 position, Vector3 target)
    {
        Vector3[] enemiesPos = gridMap.GetAllEnemiesPosition();
        Vector3[] targetsPos = gridMap.GetAllTargetPosition();

        // test on ally units
        for(int i = 0; i < enemiesPos.Length; i++)
        {
            if(enemiesPos[i].x == position.x && enemiesPos[i].z == position.z)
            {
                return true;
            }
        }

        // test on targets (except the actual target)
        for(int i = 0; i < targetsPos.Length; i++)
        {
            if (targetsPos[i].x == target.x && targetsPos[i].z == target.z) continue;                      // skip iteration if the actual target is in targetPos
            
            if(targetsPos[i].x == position.x && targetsPos[i].z == position.z)
            {
                return true;
            }
        }

        return false;
    }

    // given the obstacle position return true if a target is near the obstacle
    bool areTargetsNearObstacle(Vector3 obstaclePos)
    {
        Vector3[] targetsPos = gridMap.GetAllTargetPosition();

        for(int i = 0; i < targetsPos.Length; i++)
        {
            if (targetsPos[i].x > obstaclePos.x -1.5f && targetsPos[i].x < obstaclePos.x + 1.5f && targetsPos[i].z > obstaclePos.z - 1.5f && targetsPos[i].z < obstaclePos.z + 1.5f)
            {
                return true;
            }
        }

        return false;
    }

    // return true if position is a cell near a target when is on cover
    bool invalidCellNearObstacle(Vector3 position)
    {
        if (enemyType == Type.MELEE) return false;

        // if the target is not on cover return false
        if (!priorityTarget.guard.Item1) return false;

        // get the obstacle position
        Vector3 obstaclePos = priorityTarget.guard.Item2;     
        
        // test if position is near the obstacle
        if (position.x > obstaclePos.x - 1.5f && position.x < obstaclePos.x + 1.5 && position.z > obstaclePos.z - 1.5f && position.z < obstaclePos.z + 1.5f) return true;

        return false;
    }

    Entity ChoosePriorityTarget()
    {
        string name = "";
        float minDist = Mathf.Infinity;
        float minHealth = Mathf.Infinity;

        for(int i = 0; i < targetNames.Count; i++)
        {
            Entity target = gridMap.GetTargetEntry(targetNames[i]);
            float distToTarget = Vector3.Distance(target.transform.position, gameObject.transform.position);
            float currentHealth = (float)target.health;
            
            // choose the target with minimum health
            if(currentHealth < minHealth)                                                       
            {
                minHealth = currentHealth;
                minDist = distToTarget;                                             // get the distance with the target with minimum health                       
                name = targetNames[i];                                              // get the name of the target with minimum health
            }
        }

        for(int i = 0; i < targetNames.Count; i++)
        {
            Entity target = gridMap.GetTargetEntry(targetNames[i]);
            float distToTarget = Vector3.Distance(target.transform.position, gameObject.transform.position);

            // choose the nearest target only if the distance to target is lower than the distance with the target with minimum health
            if (distToTarget < minDist)
            {
                minDist = distToTarget;
                name = targetNames[i];
            }
        }

        // TEST ON MULTIPLE TARGETS
        string finalName = gridMap.GetTargetOnMultipleTargets(name, gameObject.name, gameObject.transform.position);

        return gridMap.GetTargetEntry(finalName);
    }

    IEnumerator AttackRoutine()
    {        
        anim.SetBool("isFiring", true);
        yield return new WaitForSeconds(1.5f);
        soundManager.PlaySound("laser");
        weaponEffects.StartShooting(priorityTarget.transform.position + Vector3.up/2);
        yield return new WaitForSeconds(0.5f);
        weaponEffects.StopShooting();
        Attack(priorityTarget);
    }

    IEnumerator MeleeAttackroutine()
    {       
        anim.SetBool("hitEnemy", true);
        yield return new WaitForSeconds(1.0f);
        soundManager.PlaySound("punch");
        yield return new WaitForSeconds(0.5f);        
        Attack(priorityTarget);
    }

    IEnumerator TakeDamage()
    {
        if(guard.Item1 == true && enemyType == Type.RANGED)
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
        if (health <= 0)
        {
            gridMap.DeleteUnit(gameObject.name);
            gridMap.DeleteEnemyPosition(gameObject.name);
            gridMap.EnemiesCount--;
            anim.SetBool("Death", true);
            yield return new WaitForSeconds(4.5f);
            gameObject.SetActive(false);            
        }
    }

    protected override IEnumerator EndGame()
    {
        if (isEndgamePanelDisable)
        {
            drawFunctions.ShowLooseMessage();            
            isEndgamePanelDisable = false;
            yield return new WaitForSeconds(5.0f);
            SceneManager.LoadScene("Title");
        }
        
        yield return null;
    }
}
