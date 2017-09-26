using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum MapEvent
{
    Generate_Finish,
}

public class MapManager : MonoBehaviour
{
    static MapManager instance;
    public static MapManager Instance
    {
        get
        {
            return instance;
        }
    }
    public const int Line = 40;
    public const int Row = 80;
    private Canvas canvas;
    private Block blockModel;

    public Block[,] allBlock = new Block[Line, Row];
    private void Start()
    {
        instance = this;
        canvas = GetComponent<Canvas>();
        blockModel = Resources.Load<Block>("Block");

        GenerateMap();
    }


    #region 生成地图--
    public void GenerateMap()
    {
        StartCoroutine(GenerateMapIE());
    }

    private IEnumerator GenerateMapIE()
    {
        if (canvas != null && blockModel != null)
        {
            Rect canvasRect = canvas.GetComponent<RectTransform>().rect;
            int perBlockWidth = (int)canvasRect.width / Row;
            int perBlockHeight = (int)canvasRect.height / Line;
            int blockSideOfLength = Mathf.Min(perBlockWidth, perBlockHeight);
            blockModel.image.rectTransform.sizeDelta = new Vector2(blockSideOfLength, blockSideOfLength);

            Vector2 startPos = new Vector2(-blockSideOfLength * (Row - 1) / 2, blockSideOfLength * (Line - 1) / 2);

            for (int line = 0; line < Line; line++)
            {
                for (int row = 0; row < Row; row++)
                {
                    var block = Instantiate(blockModel);
                    block.transform.parent = canvas.transform;
                    block.transform.localScale = Vector3.one;
                    block.transform.localPosition = startPos + new Vector2(row * blockSideOfLength, -line * blockSideOfLength);
                    block.image.color = Color.gray;
                    allBlock[line, row] = block;
                }
            }
            yield return 1;
        }
        Messenger.Broadcast(MapEvent.Generate_Finish);
    }
    #endregion

    public static void LineToAreaCheck(List<Vector2> curLine)
    {
        line = curLine;
        int startIndex = -1;
        int endIndex = -1;
        for (int i = 0; i < curLine.Count; i++)
        {
            for (int j = i + 1; j < curLine.Count; j++)
            {
                if (curLine[i] == curLine[j])
                {
                    startIndex = i;
                    endIndex = j;
                    break;
                }
            }
        }
        if (startIndex != -1 && endIndex != -1 && startIndex != endIndex)
        {
            time = Time.realtimeSinceStartup;

            minLine = int.MaxValue;
            maxLine = int.MinValue;
            minRow = int.MaxValue;
            maxRow = int.MinValue;
            for (int i = 0; i < curLine.Count; i++)
            {
                minLine = Mathf.Min(minLine, (int)curLine[i].x);
                maxLine = Mathf.Max(maxLine, (int)curLine[i].x);
                minRow = Mathf.Min(minRow, (int)curLine[i].y);
                maxRow = Mathf.Max(maxRow, (int)curLine[i].y);
            }
            List<Vector2> areas = new List<Vector2>();
            for (int i = minLine; i <= maxLine; i++)
            {
                for (int j = minRow; j <= maxRow; j++)
                {
                    areas.Add(new Vector2(i, j));
                }
            }
            //取外轮廓线条列表
            //递归，依次加入开放区域
            List<Vector2> blankAreas = new List<Vector2>();
            for (int i = 0; i < areas.Count; i++)
            {
                if (!curLine.Contains(areas[i]))
                {
                    blankAreas.Add(areas[i]);
                }
            }
            cullAreas.Clear();
            closureAreas.Clear();
            traceCount = 0;
            for (int i = 0; i < blankAreas.Count; i++)
            {
                if (!cullAreas.Contains(blankAreas[i]) && !closureAreas.Contains(blankAreas[i]))
                {
                    List<Vector2> newAreas = new List<Vector2>();
                    bool state = BlankAreaCheck(blankAreas[i], blankAreas, newAreas, out newAreas);
                    if (state)
                    {
                        cullAreas.AddRange(newAreas);
                    }
                    else
                    {
                        closureAreas.AddRange(newAreas);
                    }

                }
            }
            Debug.Log("traceCount：" + traceCount);
            
            for (int i = 0; i < areas.Count; i++)
            {
                if (!cullAreas.Contains(areas[i]))
                {
                    LightOnBlock(areas[i]);
                }
            }
            Debug.Log("time:" + (Time.realtimeSinceStartup - time));
        }
    }
    static float time;
    static int minLine = int.MaxValue;
    static int maxLine = int.MinValue;
    static int minRow = int.MaxValue;
    static int maxRow = int.MinValue;
    static List<Vector2> cullAreas = new List<Vector2>();
    static List<Vector2> closureAreas = new List<Vector2>();
    static List<Vector2> line = new List<Vector2>();
    static Dictionary<Vector2, bool> blockState = new Dictionary<Vector2, bool>();
    static Dictionary<int, Dictionary<Vector2, int>> dict = new Dictionary<int, Dictionary<Vector2, int>>();
    static int traceCount = 0;
    /// <summary>
    /// 
    /// </summary>
    /// <param name="orginArea">相邻空白点</param>
    /// <param name="targetPoint">空白点</param>
    /// <returns></returns>
    static bool BlankAreaCheck(Vector2 targetPoint,List<Vector2> blankAreas,List<Vector2> oldAreas, out List<Vector2> newAreas)
    {
        traceCount++;
        newAreas = oldAreas;
        if (targetPoint.x > maxLine || targetPoint.x < minLine || targetPoint.y > maxRow || targetPoint.y < minRow)
        {
            return true;
        }
        if(cullAreas.Contains(targetPoint))
        {
            return true;
        }
        if (line.Contains(targetPoint))
        {
            return false;
        }
        if(newAreas.Contains(targetPoint))
        {
            return false;
        }
        if (blankAreas.Contains(targetPoint))
        {
            newAreas.Add(targetPoint);
            int targetLine;
            int targetRow;
            //下--
            targetLine = (int)targetPoint.x + 1;
            targetRow = (int)targetPoint.y;
            bool downState = BlankAreaCheck(new Vector2(targetLine, targetRow), blankAreas, newAreas, out newAreas);
            if (downState)
            {
                return true;
            }
            //右--
            targetLine = (int)targetPoint.x;
            targetRow = (int)targetPoint.y + 1;
            bool right = BlankAreaCheck(new Vector2(targetLine, targetRow), blankAreas, newAreas, out newAreas);
            if (right)
            {
                return true;
            }
            //上--
            targetLine = (int)targetPoint.x - 1;
            targetRow = (int)targetPoint.y;
            bool upState = BlankAreaCheck(new Vector2(targetLine, targetRow), blankAreas, newAreas, out newAreas);
            if(upState)
            {
                return true;
            }
            //左--
            targetLine = (int)targetPoint.x;
            targetRow = (int)targetPoint.y - 1;
            bool leftState = BlankAreaCheck(new Vector2(targetLine, targetRow), blankAreas, newAreas, out newAreas);
            if (leftState)
            {
                return true;
            }
        }
        return false;
    }
    public static void LineToAreaCheck2(List<Vector2> curLine)
    {

        line = curLine;
        int startIndex = -1;
        int endIndex = -1;
        for (int i = 0; i < curLine.Count; i++)
        {
            for (int j = i + 1; j < curLine.Count; j++)
            {
                if (curLine[i] == curLine[j])
                {
                    startIndex = i;
                    endIndex = j;
                    break;
                }
            }
        }
        if (startIndex != -1 && endIndex != -1 && startIndex != endIndex)
        {
            time = Time.realtimeSinceStartup;

            minLine = int.MaxValue;
            maxLine = int.MinValue;
            minRow = int.MaxValue;
            maxRow = int.MinValue;
            for (int i = 0; i < curLine.Count; i++)
            {
                minLine = Mathf.Min(minLine, (int)curLine[i].x);
                maxLine = Mathf.Max(maxLine, (int)curLine[i].x);
                minRow = Mathf.Min(minRow, (int)curLine[i].y);
                maxRow = Mathf.Max(maxRow, (int)curLine[i].y);
            }
            Debug.Log("time:" + (Time.realtimeSinceStartup - time));
            List<Vector2> areas = new List<Vector2>();
            for (int i = minLine; i <= maxLine; i++)
            {
                for (int j = minRow; j <= maxRow; j++)
                {
                    areas.Add(new Vector2(i, j));
                }
            }
            Debug.Log("time:" + (Time.realtimeSinceStartup - time));
            //取外轮廓线条列表
            //递归，依次加入开放区域
            Dictionary<Vector2, int> blankDict = new Dictionary<Vector2, int>();
            for (int i = 0; i < areas.Count; i++)
            {
                if (!curLine.Contains(areas[i]))
                {
                    blankDict.Add(areas[i], i);
                }
            }
            dict.Clear();
            traceCount = 0;
            Debug.Log("time:" + (Time.realtimeSinceStartup - time));

            //分区--
            foreach (var blankTemp in blankDict)
            {
                bool isAllreadyIn = false;
                foreach (var temp in dict)
                {
                    if (temp.Value.ContainsKey(blankTemp.Key))
                    {
                        isAllreadyIn = true;
                        break;
                    }
                }
                if (!isAllreadyIn)
                {
                    Dictionary<Vector2, int> newAreas = new Dictionary<Vector2, int>();
                    newAreas.Add(blankTemp.Key, 0);
                    Debug.Log("time1:" + (Time.realtimeSinceStartup - time));
                    SplitAreasReclusion(blankDict, blankTemp.Key, newAreas, out newAreas);
                    Debug.Log("time2:" + (Time.realtimeSinceStartup - time));
                    dict.Add(dict.Count, newAreas);
                }
            }
            Debug.Log("time:" + (Time.realtimeSinceStartup - time));

            //对每个区进行开闭检测=--
            foreach (var temp in dict)
            {
                if (EnCullAreasCheck(temp.Value))
                {
                    foreach(var temp2 in temp.Value)
                    {
                        cullAreas.Add(temp2.Key);
                    }
                }
            }

            Debug.Log("traceCount：" + traceCount);

            for (int i = 0; i < areas.Count; i++)
            {
                if (!cullAreas.Contains(areas[i]))
                {
                    LightOnBlock(areas[i]);
                }
            }
            Debug.Log("time:" + (Time.realtimeSinceStartup - time));
        }
    }
    static bool EnCullAreasCheck(Dictionary<Vector2, int> areas)
    {
        if (areas.Count > 0)
        {
            int curAreaMinLine = int.MaxValue;
            int curAreaMaxLine = int.MinValue;
            int curAreaMinRow = int.MaxValue;
            int curAreaMaxRow = int.MinValue;
            foreach(var temp in areas)
            {
                curAreaMinLine = Mathf.Min(curAreaMinLine, (int)temp.Key.x);
                curAreaMaxLine = Mathf.Max(curAreaMaxLine, (int)temp.Key.x);
                curAreaMinRow = Mathf.Min(curAreaMinRow, (int)temp.Key.y);
                curAreaMaxRow = Mathf.Max(curAreaMaxRow, (int)temp.Key.y);
            }
            
            if (curAreaMaxLine >= maxLine || curAreaMinLine <= minLine || curAreaMaxRow >= maxRow || curAreaMinRow <= minRow)
            {
                return true;
            }
        }
        return false;
    }
    static void SplitAreasReclusion(Dictionary<Vector2, int> blankAreas, Vector2 targetPoint, Dictionary<Vector2, int> oldAreas, out Dictionary<Vector2, int> newAreas)
    {
        traceCount++;

        newAreas = oldAreas;
        float targetLine;
        float targetRow;
        Vector2 newTargetPoint;
        //下--
        targetLine = targetPoint.x + 1;
        targetRow = targetPoint.y;
        newTargetPoint = new Vector2(targetLine, targetRow);
        if (blankAreas.ContainsKey(newTargetPoint) && !newAreas.ContainsKey(newTargetPoint))
        {
            newAreas.Add(newTargetPoint, 0);
            SplitAreasReclusion(blankAreas, newTargetPoint, newAreas, out newAreas);
        }
        //右--
        targetLine = targetPoint.x;
        targetRow = targetPoint.y + 1;
        newTargetPoint = new Vector2(targetLine, targetRow);
        if (blankAreas.ContainsKey(newTargetPoint) && !newAreas.ContainsKey(newTargetPoint))
        {
            newAreas.Add(new Vector2(targetLine, targetRow), 0);
            SplitAreasReclusion(blankAreas, newTargetPoint, newAreas, out newAreas);
        }
        //上--
        targetLine = targetPoint.x - 1;
        targetRow = targetPoint.y;
        newTargetPoint = new Vector2(targetLine, targetRow);
        if (blankAreas.ContainsKey(newTargetPoint) && !newAreas.ContainsKey(newTargetPoint))
        {
            newAreas.Add(new Vector2(targetLine, targetRow), 0);
            SplitAreasReclusion(blankAreas, newTargetPoint, newAreas, out newAreas);
        }
        //左--
        targetLine = targetPoint.x;
        targetRow = targetPoint.y - 1;
        newTargetPoint = new Vector2(targetLine, targetRow);
        if (blankAreas.ContainsKey(newTargetPoint) && !newAreas.ContainsKey(newTargetPoint))
        {
            newAreas.Add(new Vector2(targetLine, targetRow), 0);
            SplitAreasReclusion(blankAreas, newTargetPoint, newAreas, out newAreas);
        }
    }

    static void LightOnBlock(Vector2 index)
    {
        Block block = MapManager.Instance.allBlock[(int)index.x, (int)index.y];
        if (block != null)
        {
            block.image.color = Color.black;
        }
    }
}
