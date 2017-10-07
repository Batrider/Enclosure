using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour {
    public Vector2 curIndex = new Vector2();
    public List<Vector2> curLine = new List<Vector2>();
    public List<Vector2> areas = new List<Vector2>();
    public float moveCD = 0.5f;
    public Color playerColor = Color.yellow;
	// Use this for initialization
	void Start () {
        //Vector2 startPos = MapManager.Instance.allBlock
        Messenger.AddListener(MapEvent.Generate_Finish, OnMapGenerateFinish);

    }
    void OnMapGenerateFinish()
    {
        curLine.Add(Vector2.zero);
        LightOnBlock(Vector2.zero);
    }
    private void OnDestroy()
    {
        Messenger.AddListener(MapEvent.Generate_Finish, OnMapGenerateFinish);
    }

    // Update is called once per frame
    void Update()
    {
        moveCD -= Time.deltaTime;
        if (moveCD < 0)
        {
            moveCD = 0.05f;
            Vector2 dir = GetJoystickDir();
            if (dir != Vector2.zero)
            {
                Vector2 newIndex = curIndex + dir;
                newIndex = new Vector2(Mathf.Clamp(newIndex.x, 0, MapManager.Line - 1), Mathf.Clamp(newIndex.y, 0, MapManager.Row - 1));
                if (newIndex != curIndex)
                {
                    int id = curLine.FindIndex(x => x == newIndex);
                    if (id >= 1)
                    {
                        List<Vector2> abandonVec = curLine.GetRange(0, id);
                        MapManager.RevertBlockColor(this,abandonVec);
                        curLine.RemoveRange(0, id);
                    }

                    curLine.Add(newIndex);
                    LightOnBlock(newIndex);
                    curIndex = newIndex;
                    List<Vector2> targetAreas = MapManager.LineToAreaCheck(this, curLine);
                    if (targetAreas != null && targetAreas.Count > 0)
                    {
                        areas.AddRange(targetAreas);
                        curLine.Clear();
                    }
                }
            }
        }
    }

    void LightOnBlock(Vector2 index)
    {
        Block block = MapManager.Instance.GetBlock((int)index.x, (int)index.y);
        if (block != null)
        {
            block.image.color = playerColor;
        }
    }

    Vector2 GetJoystickDir()
    {
        Vector2 dir = UltimateJoystick.GetPosition("MoveJoystick");
        if (dir.magnitude > 0)
        {
            if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
            {
                return Mathf.Sign(dir.x) * Vector2.up;
            }
            else
            {
                return -Mathf.Sign(dir.y) * Vector2.right;
            }
        }
        return Vector2.zero;
    }
}
