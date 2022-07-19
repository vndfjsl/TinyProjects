using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardManager : MonoBehaviour
{
    public static BoardManager Instance;

    [Header("게임보드 설정")]
    [SerializeField] List<Sprite> tileSpriteList; // 타일의 스프라이트들
    [SerializeField] int width = 0; // 보드의 x
    [SerializeField] int height = 0; // 보드의 y
    [SerializeField] float padding = 0.5f; // 타일 사이 패딩값
    [SerializeField] float tileScale = 1f; // 각 타일의 크기, 기본 1

    [Header("타일 설정")]
    [SerializeField] GameObject tilePrefab;
    [SerializeField] GameObject tilesParent; // 타일들의 부모 오브젝트(하이어라키 정리용)

    private MatchTile[,] tiles; // 보드 위의 타일들
    private MatchTile selectedTile; // 누른 타일
    private bool isShifting; // 타일을 움직이는 중인지
    private bool isMatch; // 보드에 매치된 타일이 있는지

    private void Awake()
    {
        // Singleton
        if (Instance != null)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    private void Start()
    {
        CreateBoard();
        CheckCreateMatch();
    }

    private void CreateBoard() // 게임보드 만들기
    {
        tiles = new MatchTile[height, width];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // 보드 가운데가 0,0 되게 하는 코드 | [0,0] 위치는 중심에서 - (길이-1) / 2, 가운데 위치는 0
                float xPos = x - ((float)width - 1) / 2;
                float yPos = y - ((float)height - 1) / 2;

                MatchTile newTile = Instantiate(tilePrefab, new Vector3(xPos * (tileScale + padding),
                                                                        yPos * (tileScale + padding), 0),
                                                                        Quaternion.identity, tilesParent.transform).GetComponent<MatchTile>();

                newTile.x = x;
                newTile.y = y;
                tiles[y, x] = newTile;
                int randSpriteIndex = Random.Range(0, tileSpriteList.Count);

                newTile.Init(tileSpriteList[randSpriteIndex], randSpriteIndex);
            }
        }
    }

    public void ReStartFindTile()
    {
        StopCoroutine(FindNullTiles());
        StartCoroutine(FindNullTiles());
    }

    public IEnumerator FindNullTiles()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (tiles[y, x].GetComponent<MatchTile>().GetSprite() == null)
                {
                    yield return StartCoroutine(ShiftTilesDown(x, y));
                    break;
                }
            }
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                tiles[y, x].GetComponent<MatchTile>().ClearAllMatches();
            }
        }
    }

    private IEnumerator ShiftTilesDown(int x, int yStart, float delay = 0.05f)
    {
        isShifting = true;
        List<MatchTile> yTiles = new List<MatchTile>();
        int nullCount = 0;

        for (int y = yStart; y < height; y++)
        {
            MatchTile tile = tiles[y, x].GetComponent<MatchTile>();
            if (tile.GetSprite() == null)
            {
                nullCount++;
            }

            yTiles.Add(tile);
        }

        for (int i = 0; i < nullCount; i++)
        {
            yield return new WaitForSeconds(delay);
            for (int j = 0; j < yTiles.Count - 1; j++)
            {
                yTiles[j].Init(yTiles[j + 1].GetSprite(), yTiles[j+1].tileID);

                int newID = GetNewID(height - 1, x);
                yTiles[j + 1].Init(tileSpriteList[newID], newID);
            }
        }

        isShifting = false;
    }

    private int GetNewID(int y, int x)
    {
        List<int> notMatchIDList = new List<int>() { 0, 1, 2, 3, 4, 5, 6 };
        

        if(x > 0)
        {
            notMatchIDList.Remove(tiles[y, x - 1].GetComponent<MatchTile>().tileID);
        }
        if (x < width - 1)
        {
            notMatchIDList.Remove(tiles[y, x + 1].GetComponent<MatchTile>().tileID);
        }
        if (y > 0)
        {
            notMatchIDList.Remove(tiles[y - 1, x].GetComponent<MatchTile>().tileID);
        }

        int randIndex = notMatchIDList[Random.Range(0, notMatchIDList.Count)];
       
        return randIndex;
    }

    public void SetSelectTile(MatchTile tile)
    {
        if (tile == null) // 타일이 없다
        {
            selectedTile = null;
            return;
        }

        if (selectedTile == null) // 기존 선택된 타일이 없다
        {
            selectedTile = tile;
            return;
        }

        // 인접한 타일 검사
        List<GameObject> selectedNearTiles = selectedTile.GetAllNearTiles();
        if (selectedNearTiles.Contains(tile.gameObject)) // 상하좌우 타일이라면
        {
            selectedTile.Swap(tile);

            selectedTile = null;
        }
        else // 아니라면
        {
            selectedTile.DeSelect();
            selectedTile = tile;
        }
    }

    private void CheckCreateMatch() // 시작 게임보드에 매치(3) 있으면 제거
    {
        // 중복제거를 위한 변수들
        List<int> notMatchList = new List<int>(); // 이 타일로 바꿨을때 매치 안되는 타일들 리스트
        int[] canTiles = new int[tileSpriteList.Count]; // 가능한 ID들 전체
        MatchTile nearTile; // 상하좌우로 근접한 타일 정보
        bool xMatch;
        bool yMatch;

        for (int i = 0; i < canTiles.Length; i++)
        {
            canTiles[i] = i;
        }

        // 3매치 검사
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // 매치 관련 변수들 초기화
                notMatchList.Clear();
                notMatchList.AddRange(canTiles);
                xMatch = false;
                yMatch = false;

                if (x < width - 2) // 오른쪽 2줄 제외
                {
                    xMatch = tiles[y, x].tileID == tiles[y, x + 1].tileID && tiles[y, x].tileID == tiles[y, x + 2].tileID; // 오른쪽으로 3개가 같다면(쓰리매치)
                }

                if (y < height - 2) // 왼쪽 2줄 제외
                {
                    yMatch = tiles[y, x].tileID == tiles[y + 1, x].tileID && tiles[y, x].tileID == tiles[y + 2, x].tileID; // 위로 3개가 같다면(쓰리매치)
                }


                // 둘중 하나라도 만족하면 3매치 삭제 처리해주기
                if (xMatch || yMatch)
                {
                    if (y > 0) // 아래쪽 제거
                    {
                        nearTile = tiles[y - 1, x];
                        if (notMatchList.Contains(nearTile.tileID)) // if(맨아래가 아니면 + 가능 리스트에 있으면)
                        {
                            notMatchList.Remove(nearTile.tileID); // 아래의 타일 선택x
                        }
                    }

                    if (x > 0) // 왼쪽 제거
                    {
                        nearTile = tiles[y, x - 1];
                        if (notMatchList.Contains(nearTile.tileID))
                        {
                            notMatchList.Remove(nearTile.tileID);
                        }
                    }

                    if (y < height - 1) // 위쪽 제거
                    {
                        nearTile = tiles[y + 1, x];
                        if (notMatchList.Contains(nearTile.tileID))
                        {
                            notMatchList.Remove(nearTile.tileID);
                        }
                    }

                    if (x < width - 1) // 오른쪽 제거
                    {
                        nearTile = tiles[y, x + 1];
                        if (notMatchList.Contains(nearTile.tileID))
                        {
                            notMatchList.Remove(nearTile.tileID);
                        }
                    }

                    // 제거한 ID들을 제외하고 남은 ID 중 랜덤으로 선정해서 적용
                    int randTileID = notMatchList[Random.Range(0, notMatchList.Count)];
                    tiles[y, x].Init(tileSpriteList[randTileID], randTileID);
                }
            }
        }
    }
}
