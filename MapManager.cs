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
    struct BlankSection
    {
        public int minLine;
        public int maxLine;
        public int minRow;
        public int maxRow;
        public BlankSection(int minL, int maxL, int minR, int maxR)
        {
            minLine = minL;
            maxLine = maxL;
            minRow = minR;
            maxRow = maxR;
        }
    }
    
    static float time;
    public static List<Vector2> LineToAreaCheck(List<Vector2> curLine)
    {
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
            BlankSection bSection = new BlankSection(int.MaxValue, int.MinValue, int.MaxValue, int.MinValue);
            for (int i = 0; i < curLine.Count; i++)
            {
                bSection.minLine = Mathf.Min(bSection.minLine, (int)curLine[i].x);
                bSection.maxLine = Mathf.Max(bSection.maxLine, (int)curLine[i].x);
                bSection.minRow = Mathf.Min(bSection.minRow, (int)curLine[i].y);
                bSection.maxRow = Mathf.Max(bSection.maxRow, (int)curLine[i].y);
            }
            Dictionary<Vector2, int> curLineDict = new Dictionary<Vector2, int>();
            for (int i = 0; i < curLine.Count; i++)
            {
                if (!curLineDict.ContainsKey(curLine[i]))
                {
                    curLineDict.Add(curLine[i], 0);
                }
            }
            Debug.Log("time:" + (Time.realtimeSinceStartup - time));
            var dict = new Dictionary<int, Dictionary<Vector2, int>>();
            //分区--
            for (int i = bSection.minLine; i <= bSection.maxLine; i++)
            {
                for (int j = bSection.minRow; j <= bSection.maxRow; j++)
                {
                    Vector2 target = new Vector2(i, j);
                    bool isAllreadyIn = false;
                    foreach (var temp in dict)
                    {
                        if (temp.Value.ContainsKey(target))
                        {
                            isAllreadyIn = true;
                            break;
                        }
                    }
                    if (!isAllreadyIn && !curLineDict.ContainsKey(target))
                    {
                        Dictionary<Vector2, int> newAreas = new Dictionary<Vector2, int>();
                        newAreas.Add(target, 0);
                        Debug.Log("time1:" + (Time.realtimeSinceStartup - time));
                        SplitAreasReclusion(bSection, curLineDict, target, newAreas, out newAreas);
                        Debug.Log("time2:" + (Time.realtimeSinceStartup - time));
                        dict.Add(dict.Count, newAreas);
                    }
                }
            }
            Debug.Log("time:" + (Time.realtimeSinceStartup - time));
            //对每个区进行开闭检测=--
            Dictionary<Vector2, int> cullAreasDict = new Dictionary<Vector2, int>();
            foreach (var temp in dict)
            {
                if (EnCullAreasCheck(temp.Value, bSection))
                {
                    foreach (var temp2 in temp.Value)
                    {
                        cullAreasDict.Add(temp2.Key, 0);
                    }
                }
            }
            Debug.Log("time:" + (Time.realtimeSinceStartup - time));
            List<Vector2> targetAreas = new List<Vector2>();
            for (int i = bSection.minLine; i <= bSection.maxLine; i++)
            {
                for (int j = bSection.minRow; j <= bSection.maxRow; j++)
                {
                    Vector2 target = new Vector2(i, j);
                    if (!cullAreasDict.ContainsKey(target))
                    {
                        LightOnBlock(target);
                        targetAreas.Add(target);
                    }
                }
            }
            Debug.Log("time:" + (Time.realtimeSinceStartup - time));
            return targetAreas;
        }
        return null;
    }
    /// <summary>
    /// 闭合区间检查--
    /// </summary>
    /// <param name="areas"></param>
    /// <param name="bSection"></param>
    /// <returns></returns>
    static bool EnCullAreasCheck(Dictionary<Vector2, int> areas, BlankSection bSection)
    {
        if (areas.Count > 0)
        {
            int curAreaMinLine = int.MaxValue;
            int curAreaMaxLine = int.MinValue;
            int curAreaMinRow = int.MaxValue;
            int curAreaMaxRow = int.MinValue;
            foreach (var temp in areas)
            {
                curAreaMinLine = Mathf.Min(curAreaMinLine, (int)temp.Key.x);
                curAreaMaxLine = Mathf.Max(curAreaMaxLine, (int)temp.Key.x);
                curAreaMinRow = Mathf.Min(curAreaMinRow, (int)temp.Key.y);
                curAreaMaxRow = Mathf.Max(curAreaMaxRow, (int)temp.Key.y);
            }

            if (curAreaMaxLine >= bSection.maxLine || curAreaMinLine <= bSection.minLine || curAreaMaxRow >= bSection.maxRow || curAreaMinRow <= bSection.minRow)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 区间分割--
    /// </summary>
    /// <param name="bSection"></param>
    /// <param name="curLine"></param>
    /// <param name="targetPoint"></param>
    /// <param name="oldAreas"></param>
    /// <param name="newAreas"></param>
    static void SplitAreasReclusion(BlankSection bSection, Dictionary<Vector2, int> curLine, Vector2 targetPoint, Dictionary<Vector2, int> oldAreas, out Dictionary<Vector2, int> newAreas)
    {
        newAreas = oldAreas;
        float targetLine;
        float targetRow;
        Vector2 newTargetPoint;
        //下--
        targetLine = targetPoint.x + 1;
        targetRow = targetPoint.y;
        newTargetPoint = new Vector2(targetLine, targetRow);
        if (!curLine.ContainsKey(newTargetPoint) && !newAreas.ContainsKey(newTargetPoint)
            && targetLine >= bSection.minLine && targetLine <= bSection.maxLine && targetRow >= bSection.minRow && targetRow <= bSection.maxRow)
        {
            newAreas.Add(newTargetPoint, 0);
            SplitAreasReclusion(bSection, curLine, newTargetPoint, newAreas, out newAreas);
        }
        //右--
        targetLine = targetPoint.x;
        targetRow = targetPoint.y + 1;
        newTargetPoint = new Vector2(targetLine, targetRow);
        if (!curLine.ContainsKey(newTargetPoint) && !newAreas.ContainsKey(newTargetPoint)
            && targetLine >= bSection.minLine && targetLine <= bSection.maxLine && targetRow >= bSection.minRow && targetRow <= bSection.maxRow)
        {
            newAreas.Add(new Vector2(targetLine, targetRow), 0);
            SplitAreasReclusion(bSection, curLine, newTargetPoint, newAreas, out newAreas);
        }
        //上--
        targetLine = targetPoint.x - 1;
        targetRow = targetPoint.y;
        newTargetPoint = new Vector2(targetLine, targetRow);
        if (!curLine.ContainsKey(newTargetPoint) && !newAreas.ContainsKey(newTargetPoint)
            && targetLine >= bSection.minLine && targetLine <= bSection.maxLine && targetRow >= bSection.minRow && targetRow <= bSection.maxRow)
        {
            newAreas.Add(new Vector2(targetLine, targetRow), 0);
            SplitAreasReclusion(bSection, curLine, newTargetPoint, newAreas, out newAreas);
        }
        //左--
        targetLine = targetPoint.x;
        targetRow = targetPoint.y - 1;
        newTargetPoint = new Vector2(targetLine, targetRow);
        if (!curLine.ContainsKey(newTargetPoint) && !newAreas.ContainsKey(newTargetPoint)
            && targetLine >= bSection.minLine && targetLine <= bSection.maxLine && targetRow >= bSection.minRow && targetRow <= bSection.maxRow)
        {
            newAreas.Add(new Vector2(targetLine, targetRow), 0);
            SplitAreasReclusion(bSection, curLine, newTargetPoint, newAreas, out newAreas);
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
