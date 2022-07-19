using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardManager : MonoBehaviour
{
    public static BoardManager Instance;

    [Header("���Ӻ��� ����")]
    [SerializeField] List<Sprite> tileSpriteList; // Ÿ���� ��������Ʈ��
    [SerializeField] int width = 0; // ������ x
    [SerializeField] int height = 0; // ������ y
    [SerializeField] float padding = 0.5f; // Ÿ�� ���� �е���
    [SerializeField] float tileScale = 1f; // �� Ÿ���� ũ��, �⺻ 1

    [Header("Ÿ�� ����")]
    [SerializeField] GameObject tilePrefab;
    [SerializeField] GameObject tilesParent; // Ÿ�ϵ��� �θ� ������Ʈ(���̾��Ű ������)

    private MatchTile[,] tiles; // ���� ���� Ÿ�ϵ�
    private MatchTile selectedTile; // ���� Ÿ��
    private bool isShifting; // Ÿ���� �����̴� ������
    private bool isMatch; // ���忡 ��ġ�� Ÿ���� �ִ���

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

    private void CreateBoard() // ���Ӻ��� �����
    {
        tiles = new MatchTile[height, width];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // ���� ����� 0,0 �ǰ� �ϴ� �ڵ� | [0,0] ��ġ�� �߽ɿ��� - (����-1) / 2, ��� ��ġ�� 0
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
        if (tile == null) // Ÿ���� ����
        {
            selectedTile = null;
            return;
        }

        if (selectedTile == null) // ���� ���õ� Ÿ���� ����
        {
            selectedTile = tile;
            return;
        }

        // ������ Ÿ�� �˻�
        List<GameObject> selectedNearTiles = selectedTile.GetAllNearTiles();
        if (selectedNearTiles.Contains(tile.gameObject)) // �����¿� Ÿ���̶��
        {
            selectedTile.Swap(tile);

            selectedTile = null;
        }
        else // �ƴ϶��
        {
            selectedTile.DeSelect();
            selectedTile = tile;
        }
    }

    private void CheckCreateMatch() // ���� ���Ӻ��忡 ��ġ(3) ������ ����
    {
        // �ߺ����Ÿ� ���� ������
        List<int> notMatchList = new List<int>(); // �� Ÿ�Ϸ� �ٲ����� ��ġ �ȵǴ� Ÿ�ϵ� ����Ʈ
        int[] canTiles = new int[tileSpriteList.Count]; // ������ ID�� ��ü
        MatchTile nearTile; // �����¿�� ������ Ÿ�� ����
        bool xMatch;
        bool yMatch;

        for (int i = 0; i < canTiles.Length; i++)
        {
            canTiles[i] = i;
        }

        // 3��ġ �˻�
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // ��ġ ���� ������ �ʱ�ȭ
                notMatchList.Clear();
                notMatchList.AddRange(canTiles);
                xMatch = false;
                yMatch = false;

                if (x < width - 2) // ������ 2�� ����
                {
                    xMatch = tiles[y, x].tileID == tiles[y, x + 1].tileID && tiles[y, x].tileID == tiles[y, x + 2].tileID; // ���������� 3���� ���ٸ�(������ġ)
                }

                if (y < height - 2) // ���� 2�� ����
                {
                    yMatch = tiles[y, x].tileID == tiles[y + 1, x].tileID && tiles[y, x].tileID == tiles[y + 2, x].tileID; // ���� 3���� ���ٸ�(������ġ)
                }


                // ���� �ϳ��� �����ϸ� 3��ġ ���� ó�����ֱ�
                if (xMatch || yMatch)
                {
                    if (y > 0) // �Ʒ��� ����
                    {
                        nearTile = tiles[y - 1, x];
                        if (notMatchList.Contains(nearTile.tileID)) // if(�ǾƷ��� �ƴϸ� + ���� ����Ʈ�� ������)
                        {
                            notMatchList.Remove(nearTile.tileID); // �Ʒ��� Ÿ�� ����x
                        }
                    }

                    if (x > 0) // ���� ����
                    {
                        nearTile = tiles[y, x - 1];
                        if (notMatchList.Contains(nearTile.tileID))
                        {
                            notMatchList.Remove(nearTile.tileID);
                        }
                    }

                    if (y < height - 1) // ���� ����
                    {
                        nearTile = tiles[y + 1, x];
                        if (notMatchList.Contains(nearTile.tileID))
                        {
                            notMatchList.Remove(nearTile.tileID);
                        }
                    }

                    if (x < width - 1) // ������ ����
                    {
                        nearTile = tiles[y, x + 1];
                        if (notMatchList.Contains(nearTile.tileID))
                        {
                            notMatchList.Remove(nearTile.tileID);
                        }
                    }

                    // ������ ID���� �����ϰ� ���� ID �� �������� �����ؼ� ����
                    int randTileID = notMatchList[Random.Range(0, notMatchList.Count)];
                    tiles[y, x].Init(tileSpriteList[randTileID], randTileID);
                }
            }
        }
    }
}
