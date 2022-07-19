using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System;

public class MatchTile : MonoBehaviour
{
    public int tileID { get; set; }
    public int x;
    public int y;

    private SpriteRenderer _sr;
    private bool isSelected; // 타일이 선택중인지
    private bool isMove; // 타일이 이동중인지
    private bool isMatch; // 타일이 매치가능한지

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
    }

    private void OnMouseDown()
    {
        if (isMove) return;

        if(!isSelected)
        {
            Select();
        }
        else
        {
            BoardManager.Instance.SetSelectTile(null);
            DeSelect();
        }
    }

    public void Init(Sprite sprite, int id) // 타일의 Sprite와 타입 ID를 설정
    {
        SetSprite(sprite);
        tileID = id;
    }

    public void Init(MatchTile tile)
    {
        SetSprite(tile.GetSprite());
        tileID = tile.tileID;
    }

    public Sprite GetSprite()
    {
        return _sr.sprite;
    }

    public void SetSprite(Sprite sprite) // 타일의 Sprite 설정
    {
        _sr.sprite = sprite;
    }

    private void Select()
    {
        isSelected = true;
        _sr.color = Color.yellow;
        BoardManager.Instance.SetSelectTile(this);
    }

    public void DeSelect()
    {
        isSelected = false;
        _sr.color = Color.white;
    }

    public void MoveTile(Vector3 pos)
    {
        transform.DOMove(pos, 0.5f).OnComplete(() =>
        {
            ClearAllMatches();
            DeSelect();
            isMove = false;
        });
        isMove = true;
    }

    public void Swap(MatchTile tile)
    {
        if (tile == this) return;

        //// 바꾸기
        //int tempID = tileID;
        //tileID = tile.tileID;
        //tile.tileID = tempID;


        Vector3 tempPos = transform.position;
        MoveTile(tile.transform.position);
        tile.MoveTile(tempPos);
    }

    public List<GameObject> GetAllNearTiles()
    {
        List<GameObject> nearTiles = new List<GameObject>();
        nearTiles.Add(GetNearTile(Vector2.up * 3f));
        nearTiles.Add(GetNearTile(Vector2.down * 3f));
        nearTiles.Add(GetNearTile(Vector2.left * 3f));
        nearTiles.Add(GetNearTile(Vector2.right * 3f));

        return nearTiles;
    }

    private GameObject GetNearTile(Vector2 dir)
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, dir);
        if(hit.collider != null)
        {
            return hit.collider.gameObject;
        }

        return null;
    }

    private List<GameObject> FindMatch(Vector2 dir)
    {
        List<GameObject> matchingTiles = new List<GameObject>();
        RaycastHit2D hit = Physics2D.Raycast(transform.position, dir);
        while(hit.collider != null && hit.collider.GetComponent<MatchTile>().tileID == tileID)
        {
            matchingTiles.Add(hit.collider.gameObject);
            hit = Physics2D.Raycast(hit.collider.transform.position, dir);
        }

        return matchingTiles;
    }

    // if isMatch = true
    private void ClearMatch(Vector2[] paths)
    {
        List<GameObject> matchingTiles = new List<GameObject>();
        for (int i = 0; i < paths.Length; i++)
        {
            matchingTiles.AddRange(FindMatch(paths[i]));
        }

        if (matchingTiles.Count >= 2)
        {
            for (int i = 0; i < matchingTiles.Count; i++)
            {
                matchingTiles[i].GetComponent<MatchTile>().RemoveTile();
            }
            isMatch = true;
        }
    }

    public void ClearAllMatches()
    {
        if (_sr.sprite == null) return;

        ClearMatch(new Vector2[2] { Vector2.left, Vector2.right });
        ClearMatch(new Vector2[2] { Vector2.up, Vector2.down });

        if(isMatch)
        {
            RemoveTile();
            isMatch = false;
            BoardManager.Instance.ReStartFindTile();
        }
    }

    private void RemoveTile()
    {
        _sr.sprite = null;
    }
}