using System;
using UnityEngine;

public enum MapEnemyType
{
    Melee,
    Ranged,
    Giant
}

[Serializable]
public class MapEnemySpawnRule
{
    public MapEnemyType enemyType = MapEnemyType.Melee;
    public bool enabled = true;
    public float spawnWeight = 1f;
    public float startsAfterSeconds;

    public MapEnemySpawnRule()
    {
    }

    public MapEnemySpawnRule(MapEnemyType enemyType, float spawnWeight, float startsAfterSeconds = 0f)
    {
        this.enemyType = enemyType;
        this.spawnWeight = spawnWeight;
        this.startsAfterSeconds = startsAfterSeconds;
    }
}

[Serializable]
public class GameMapDefinition
{
    public string mapName = "Map";
    public Material floorMaterial;
    public Material obstacleMaterial;
    public Sprite obstacleSprite;
    public Texture2D obstacleTexture;
    public MapMaterialDefinition materialDrop = new MapMaterialDefinition();
    [HideInInspector]
    public Texture2D floorTexture;
    public Texture2D previewTexture;
    [HideInInspector]
    public Color floorColor = new Color(0.08f, 0.08f, 0.1f, 1f);
    [HideInInspector]
    public Color obstacleColor = new Color(0.35f, 0.35f, 0.35f, 1f);
    public float enemySpawnInterval = 1.5f;
    public int maxEnemies = 12;
    public bool giantMinuteSpawns = true;
    public float giantMinuteIntervalSeconds = 60f;
    public MapEnemySpawnRule[] enemySpawnRules;
}

[Serializable]
public class MapMaterialDefinition
{
    public string inventoryKey = "map_material";
    public string itemName = "Map Material";
    public string rarity = "Rare";
    public Texture2D pickupTexture;
    public Material pickupMaterial;
    public Color pickupColor = new Color(0.7f, 0.95f, 1f, 1f);
    public float pickupSize = 0.9f;
}

[Serializable]
public class MapSelectOption
{
    public string mapName = "Map";
    public Texture2D previewTexture;
    public Color previewColor = new Color(0.15f, 0.35f, 0.2f, 1f);
}

public static class GameMapSelection
{
    public static int SelectedMapIndex { get; private set; }

    public static void Select(int mapIndex)
    {
        SelectedMapIndex = Mathf.Max(0, mapIndex);
    }

    public static GameMapDefinition[] CreateDefaultMapDefinitions()
    {
        return new[]
        {
            new GameMapDefinition
            {
                mapName = "Green Fields",
                materialDrop = new MapMaterialDefinition
                {
                    inventoryKey = "green_fields_material",
                    itemName = "Green Fields Material",
                    rarity = "Rare",
                    pickupColor = new Color(1f, 0.82f, 0.25f, 1f)
                },
                floorColor = new Color(1f, 0.7853262f, 0.1273585f, 1f),
                obstacleColor = new Color(1f, 0.88304025f, 0.3726415f, 1f),
                enemySpawnInterval = 1.5f,
                maxEnemies = 12,
                giantMinuteSpawns = true,
                giantMinuteIntervalSeconds = 60f,
                enemySpawnRules = new[]
                {
                    new MapEnemySpawnRule(MapEnemyType.Melee, 1f, 0f),
                    new MapEnemySpawnRule(MapEnemyType.Ranged, 0.4f, 15f)
                }
            },
            new GameMapDefinition
            {
                mapName = "Ash Basin",
                materialDrop = new MapMaterialDefinition
                {
                    inventoryKey = "ash_basin_material",
                    itemName = "Ash Basin Material",
                    rarity = "Rare",
                    pickupColor = new Color(0.95f, 0.36f, 0.25f, 1f)
                },
                floorColor = new Color(0.12f, 0.10f, 0.11f, 1f),
                obstacleColor = new Color(0.42f, 0.32f, 0.34f, 1f),
                enemySpawnInterval = 1.7f,
                maxEnemies = 11,
                giantMinuteSpawns = true,
                giantMinuteIntervalSeconds = 60f,
                enemySpawnRules = new[]
                {
                    new MapEnemySpawnRule(MapEnemyType.Melee, 1f, 0f),
                    new MapEnemySpawnRule(MapEnemyType.Ranged, 0.25f, 20f)
                }
            },
            new GameMapDefinition
            {
                mapName = "Moon Marsh",
                materialDrop = new MapMaterialDefinition
                {
                    inventoryKey = "moon_marsh_material",
                    itemName = "Moon Marsh Material",
                    rarity = "Rare",
                    pickupColor = new Color(0.3f, 0.95f, 0.9f, 1f)
                },
                floorColor = new Color(0.06f, 0.11f, 0.13f, 1f),
                obstacleColor = new Color(0.22f, 0.45f, 0.42f, 1f),
                enemySpawnInterval = 1.3f,
                maxEnemies = 14,
                giantMinuteSpawns = true,
                giantMinuteIntervalSeconds = 60f,
                enemySpawnRules = new[]
                {
                    new MapEnemySpawnRule(MapEnemyType.Melee, 1f, 0f),
                    new MapEnemySpawnRule(MapEnemyType.Ranged, 0.55f, 12f)
                }
            }
        };
    }

    public static MapSelectOption[] CreateDefaultMapSelectOptions()
    {
        return new[]
        {
            new MapSelectOption
            {
                mapName = "Green Fields",
                previewColor = new Color(1f, 0.7853262f, 0.1273585f, 1f)
            },
            new MapSelectOption
            {
                mapName = "Ash Basin",
                previewColor = new Color(0.38f, 0.20f, 0.17f, 1f)
            },
            new MapSelectOption
            {
                mapName = "Moon Marsh",
                previewColor = new Color(0.08f, 0.28f, 0.34f, 1f)
            }
        };
    }
}
