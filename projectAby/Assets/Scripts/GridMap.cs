using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridMap : MonoBehaviour
{
    public struct GridPoint
    {
        public float x;
        public float y;
    }

    public class Target
    {
        public string name;
        public int count;
        public List<string> enemyNames;
    }

    public enum TurnState
    {
        WAIT_FOR_END_TURN,
        UNIT_SELECTION
    }

    public GridPoint[,] grid;
    public static List<GameObject> objectToRemove = new List<GameObject>();                                       // is used in CombatMap script (editor)
    public int[,] mask;
    public List<Entity> unitList;
    [HideInInspector] public bool isClick;
    
    const int WIDTH = 20;
    const int HEIGHT = 10;
    private List<string> targetNameList;
    private Dictionary<string, Target> targetInfo = new Dictionary<string, Target>();
    private Dictionary<string, Entity> targetList = new Dictionary<string, Entity>();                             // list of player units used by enemy a.i.
    private Dictionary<string, Vector3> enemiesList = new Dictionary<string,Vector3>();                           // list of all enemies position
    private bool isUnitSelected;    
    private int selUnitCount;
    private int enemiesCount = 0;
    private int internalTargetsCount = 1;

    public int EnemiesCount
    {
        get
        {
            return enemiesCount;
        }
        set
        {
            enemiesCount = value;
        }
    }

    public int getHeight()
    {
        return HEIGHT;
    }

    public int getWidth()
    {
        return WIDTH;
    }

    private void Awake()
    {
        CreateDictionaryTarget();
    }

    private void Start()
    {
        CreateTurnList();
        grid = new GridPoint[HEIGHT, WIDTH];
        mask = new int[HEIGHT, WIDTH];
        CreateGrid();
    }

    private void Update()
    {
        if (isUnitSelected)
        {
            StartCoroutine(WaitForEndTurn());
        }
        else
        {
            StartCoroutine(SelectNextUnit());
        }        
    }

    static int byInitiative(Entity a, Entity b)
    {
        return b.initiative.CompareTo(a.initiative);
    }

    private void CreateTurnList()
    {
        unitList.Sort(byInitiative);   
        unitList[0].state = Entity.EntityState.ACTIONS;  
        isUnitSelected = true;
        selUnitCount = 0;
    }

    public void DeleteUnit(string name)
    {
        if (unitList.Count == 0) return;
        for(int i = 0; i < unitList.Count; i++)
        {
            if(unitList[i].name == name)
            {
                unitList[i].state = Entity.EntityState.INVALID;
            }
        }
    }

    IEnumerator WaitForEndTurn()
    {
        if(unitList[selUnitCount].state == Entity.EntityState.WAIT_FOR_TURN)
        {
            isUnitSelected = false;
        }
        yield return null;
    }

    IEnumerator SelectNextUnit()
    {
        if (selUnitCount == unitList.Count -1)
        {
            selUnitCount = -1;
        }
        selUnitCount++;

        if(unitList[selUnitCount].state == Entity.EntityState.INVALID) yield break;

        unitList[selUnitCount].state = Entity.EntityState.ACTIONS;
        isUnitSelected = true;
        
        yield return null;
    }

    private void CreateGrid()
    {
        float X = -9.5f;
        float Y = 4.5f;
        float incX = 1.0f;
        float incY = -1.0f;

        for (int i = 0; i < HEIGHT; i++)
        {
            for (int j = 0; j < WIDTH; j++)
            {
                GridPoint point = new GridPoint();
                point.x = X;
                point.y = Y;
                grid[i, j] = point;
                mask[i, j] = 0;
                X += incX;
            }
            X = -9.5f;
            Y += incY;
        }
    }

    public (float,float)[,] GetGrid()
    {
        (float, float)[,] matrix = new (float, float)[HEIGHT, WIDTH];
        float X = -9.5f;
        float Y = 4.5f;
        float incX = 1.0f;
        float incY = -1.0f;

        for (int i = 0; i < HEIGHT; i++)
        {
            for (int j = 0; j < WIDTH; j++)
            {
                matrix[i, j].Item1 = X;
                matrix[i, j].Item2 = Y;
                X += incX;
            }
            X = -9.5f;
            Y += incY;
        }

        return matrix;
    }

    public int[,] getMask()
    {
        mask = new int[HEIGHT, WIDTH];

        //single cell obstacle
        int seedN = Random.Range(2,4);                                      // seedN set the number of single obstacle in the map

        for(int i = 0; i < seedN; i++)
        {
            int x = Random.Range(0, 9);                                     // random position on the map
            int y = Random.Range(2,17);
            mask[x, y] = 1;
        }

        //double cell obstacle
        int seedN2 = Random.Range(2, 4);                                    // seedN2 set the number of double cell obstacle in the map

        for(int i = 0; i < seedN2; i++)
        {
            int x = Random.Range(0, 9);
            int y = Random.Range(2, 17);

            if(mask[x,y] != 1)
            {
                mask[x, y] = 2;
            }
        }

        return mask;
    }

    // Dictionary functions
    private void CreateDictionaryTarget()
    {
        GameObject[] unitOnField = GameObject.FindGameObjectsWithTag("unit");
        targetNameList = new List<string>();
        targetNameList.Capacity = unitOnField.Length;
        

        for (int i = 0; i < unitOnField.Length; i++)
        {
            Target target = new Target();
            target.name = unitOnField[i].name;
            target.count = 0;
            target.enemyNames = new List<string>();
            PlayerUnit playerUnit = unitOnField[i].GetComponent<PlayerUnit>();
            targetList.Add(unitOnField[i].name, playerUnit);
            targetNameList.Add(unitOnField[i].name);
            targetInfo.Add(unitOnField[i].name, target);
        }
    }

    public string GetTargetOnMultipleTargets(string firstTargetName, string enemyName, Vector3 myPosition)
    {
        string targetName;
        if (CheckTargetInfoList(enemyName, myPosition, out targetName)) return targetName;

        Target testTarget;
        if(targetInfo.TryGetValue(firstTargetName, out testTarget))
        {
            if(testTarget.count < internalTargetsCount)
            {
                AddNameToEnemyNames(testTarget, enemyName);
            }
            if(testTarget.count == internalTargetsCount)
            {
                foreach(var target in targetInfo)
                {
                    if(target.Value.count < internalTargetsCount)
                    {
                        AddNameToEnemyNames(target.Value, enemyName);
                        return target.Value.name;
                    }
                }

                internalTargetsCount++;
                AddNameToEnemyNames(targetInfo[firstTargetName], enemyName);
            }
        }

        return firstTargetName;
    }

    private void AddNameToEnemyNames(Target target, string enemyName)
    {
        target.enemyNames.Add(enemyName);
        target.count++;
    }

    private void RemoveNameFromEnemyNames(Target target, string enemyName)
    {
        target.enemyNames.Remove(enemyName);
        target.count--;
    }

    private bool CheckTargetInfoList(string enemyName, Vector3 myPosition, out string targetName)
    {
        targetName = "";
        float maxChoiceIndex = 0;                                         // maxChoiceIndex = (1/distance) + (4/health) 
        string chosenTargetName = "";

        // Search for the most convenient target based on distance and health
        foreach(var target in targetInfo)
        {
            Entity entity = GetTargetEntry(target.Value.name);
            float choiceIndex = CalculateChoiceIndex(entity, myPosition);

            if(choiceIndex > maxChoiceIndex)
            {
                maxChoiceIndex = choiceIndex;
                chosenTargetName = entity.name;
            }           
        }

        foreach(var target in targetInfo)
        {
            if (target.Value.enemyNames.Contains(enemyName))
            {
                Entity entity = GetTargetEntry(target.Value.name);
                float choiceIndex = CalculateChoiceIndex(entity, myPosition);

                // we need to change the target with the most convenient target
                if (choiceIndex < maxChoiceIndex)
                {
                    targetName = chosenTargetName;
                    RemoveNameFromEnemyNames(target.Value, enemyName);
                    Target newTarget = targetInfo[chosenTargetName];
                    AddNameToEnemyNames(newTarget, enemyName);
                    return true;
                }

                targetName = target.Value.name;
                return true;
            } 
        }

        return false;
    }

    private float CalculateChoiceIndex(Entity entity, Vector3 myPosition)
    {
        float distance = Vector3.Distance(myPosition, entity.transform.position);
        float health = (float)entity.health;

        return (1 / distance) + (4 / health);
    }

    public Entity GetTargetEntry(string targetName)
    {
        Entity target;
        if(targetList.TryGetValue(targetName, out target))
        {
            return target;
        }
        else return null;
    }

    public List<string> GetTargetKeys()
    {
        return targetNameList;        
    }
    
    public void DeleteTargetKey(string name)
    {
        if (targetNameList.Count == 0) return;

        for (int i = 0; i < targetNameList.Count; i++)
        {
            if(targetNameList[i] == name)
            {
                targetNameList.RemoveAt(i);
            }
        }
    }

    public void DeleteTargetInfoEntry(string targetName)
    {
        if (targetInfo.Count == 0) return;
        targetInfo.Remove(targetName);
    }

    public void EditTargetEntryPosition(string targetName, Vector3 newPosition)
    {
        targetList[targetName].transform.position = newPosition;
    }

    public void EditTargetEntryHealth(string targetName, int newHealth)
    {
        targetList[targetName].health = newHealth;
    }

    public void DeleteTargetEntry(string targetName)
    {
        if (targetList.Count == 0) return;
        targetList.Remove(targetName);
    }

    public void AddTargetEntry(string targetName, Entity target)
    {
        targetList.Add(targetName, target);
    }

    public Vector3[] GetAllTargetPosition()
    {
        Vector3[] targetPosition = new Vector3[targetList.Count];
        int i = 0;

        foreach(var entry in targetList)
        {
            targetPosition[i] = entry.Value.transform.position;
            i++;
        }
        return targetPosition;
    }

    public void AddEnemyPosition(string enemyName, Vector3 enemyPos)
    {
        enemiesList.Add(enemyName, enemyPos);
        enemiesCount++;
    }

    public void DeleteEnemyPosition(string enemyName)
    {
        enemiesList.Remove(enemyName);
    }

    public void EditEnemyPosition(string enemyName, Vector3 enemyPos)
    {
        enemiesList[enemyName] = enemyPos;
    }

    public Vector3 GetEnemyPosition(string enemyName)
    {
            return enemiesList[enemyName];
    }

    public Vector3[] GetAllEnemiesPosition()
    {
        Vector3[] enemiesPosition = new Vector3[enemiesList.Count];
        int i = 0;

        foreach(var entry in enemiesList)
        {
            enemiesPosition[i] = entry.Value;
            i++;
        }
        return enemiesPosition;
    }

    public bool AllTargetsDead()
    {
        return targetNameList.Count == 0;
    }
}
