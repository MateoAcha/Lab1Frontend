using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GameBootstrap))]
[CanEditMultipleObjects]
public class GameBootstrapEditor : Editor
{
    private SerializedProperty playerSize;
    private SerializedProperty meleeEnemySize;
    private SerializedProperty rangedEnemySize;
    private SerializedProperty cameraOrthographicSize;
    private SerializedProperty giantEnemySize;
    private SerializedProperty giantEnemyMaterial;
    private SerializedProperty giantEnemyHealth;
    private SerializedProperty giantEnemyAttackRange;

    private SerializedProperty mapSize;
    private SerializedProperty maps;

    private SerializedProperty exitTexture;
    private SerializedProperty exitColor;
    private SerializedProperty exitSize;
    private SerializedProperty exitTextureSize;
    private SerializedProperty exitCount;

    private SerializedProperty backgroundMaterial;
    private SerializedProperty playerMaterial;
    private SerializedProperty meleeEnemyMaterial;
    private SerializedProperty rangedEnemyMaterial;
    private SerializedProperty enemyProjectileMaterial;
    private SerializedProperty skinVisualDatabase;
    private SerializedProperty weaponVisualDatabase;

    private SerializedProperty swordSwingSprite;
    private SerializedProperty swordSwingTexture;
    private SerializedProperty swordSwingVisualOffset;
    private SerializedProperty swordSwingVisualScale;
    private SerializedProperty swordSwingVisualRotationOffset;
    private SerializedProperty swordSwingDurationMultiplier;
    private SerializedProperty carriedSwordVisualOffset;
    private SerializedProperty carriedSwordVisualScale;
    private SerializedProperty carriedSwordVisualRotationOffset;
    private SerializedProperty carriedSwordSortingOrderOffset;
    private SerializedProperty spearSprite;
    private SerializedProperty spearTexture;
    private SerializedProperty spearVisualOffset;
    private SerializedProperty spearVisualScale;
    private SerializedProperty spearVisualRotationOffset;
    private SerializedProperty spearThrustDistance;
    private SerializedProperty carriedSpearVisualOffset;
    private SerializedProperty carriedSpearVisualScale;
    private SerializedProperty carriedSpearVisualRotationOffset;
    private SerializedProperty carriedSpearSortingOrderOffset;
    private SerializedProperty carriedRangedOrbOffset;
    private SerializedProperty carriedRangedOrbScale;
    private SerializedProperty carriedRangedOrbSortingOrderOffset;
    private SerializedProperty minionMoveSprites;
    private SerializedProperty minionMoveTexture;
    private SerializedProperty minionMoveResource;
    private SerializedProperty minionSpriteScale;
    private SerializedProperty minionMoveFps;

    private SerializedProperty rockSprite;
    private SerializedProperty rockMaterial;
    private SerializedProperty rockBaseSize;
    private SerializedProperty rockSizeJitter;
    private SerializedProperty rocksPer100Units;
    private SerializedProperty centerRockChance;
    private SerializedProperty edgeRockChance;
    private SerializedProperty centerSafeRadius;
    private SerializedProperty borderInset;
    private SerializedProperty borderRockSize;
    private SerializedProperty borderSpacing;

    private bool showSizes = true;
    private bool showCamera = true;
    private bool showGiantEnemy = true;
    private bool showMap = true;
    private bool showExit = true;
    private bool showMaterials = true;
    private bool showSwordVisual = true;
    private bool showCarriedSwordVisual = true;
    private bool showSpearVisual = true;
    private bool showCarriedSpearVisual = true;
    private bool showCarriedRangedOrbVisual = true;
    private bool showMinionVisual = true;
    private bool showRocks = true;

    private void OnEnable()
    {
        playerSize = serializedObject.FindProperty("playerSize");
        meleeEnemySize = serializedObject.FindProperty("meleeEnemySize");
        rangedEnemySize = serializedObject.FindProperty("rangedEnemySize");
        cameraOrthographicSize = serializedObject.FindProperty("cameraOrthographicSize");
        giantEnemySize = serializedObject.FindProperty("giantEnemySize");
        giantEnemyMaterial = serializedObject.FindProperty("giantEnemyMaterial");
        giantEnemyHealth = serializedObject.FindProperty("giantEnemyHealth");
        giantEnemyAttackRange = serializedObject.FindProperty("giantEnemyAttackRange");

        mapSize = serializedObject.FindProperty("mapSize");
        maps = serializedObject.FindProperty("maps");

        exitTexture = serializedObject.FindProperty("exitTexture");
        exitColor = serializedObject.FindProperty("exitColor");
        exitSize = serializedObject.FindProperty("exitSize");
        exitTextureSize = serializedObject.FindProperty("exitTextureSize");
        exitCount = serializedObject.FindProperty("exitCount");

        backgroundMaterial = serializedObject.FindProperty("backgroundMaterial");
        playerMaterial = serializedObject.FindProperty("playerMaterial");
        meleeEnemyMaterial = serializedObject.FindProperty("meleeEnemyMaterial");
        rangedEnemyMaterial = serializedObject.FindProperty("rangedEnemyMaterial");
        enemyProjectileMaterial = serializedObject.FindProperty("enemyProjectileMaterial");
        skinVisualDatabase = serializedObject.FindProperty("skinVisualDatabase");
        weaponVisualDatabase = serializedObject.FindProperty("weaponVisualDatabase");

        swordSwingSprite = serializedObject.FindProperty("swordSwingSprite");
        swordSwingTexture = serializedObject.FindProperty("swordSwingTexture");
        swordSwingVisualOffset = serializedObject.FindProperty("swordSwingVisualOffset");
        swordSwingVisualScale = serializedObject.FindProperty("swordSwingVisualScale");
        swordSwingVisualRotationOffset = serializedObject.FindProperty("swordSwingVisualRotationOffset");
        swordSwingDurationMultiplier = serializedObject.FindProperty("swordSwingDurationMultiplier");
        carriedSwordVisualOffset = serializedObject.FindProperty("carriedSwordVisualOffset");
        carriedSwordVisualScale = serializedObject.FindProperty("carriedSwordVisualScale");
        carriedSwordVisualRotationOffset = serializedObject.FindProperty("carriedSwordVisualRotationOffset");
        carriedSwordSortingOrderOffset = serializedObject.FindProperty("carriedSwordSortingOrderOffset");
        spearSprite = serializedObject.FindProperty("spearSprite");
        spearTexture = serializedObject.FindProperty("spearTexture");
        spearVisualOffset = serializedObject.FindProperty("spearVisualOffset");
        spearVisualScale = serializedObject.FindProperty("spearVisualScale");
        spearVisualRotationOffset = serializedObject.FindProperty("spearVisualRotationOffset");
        spearThrustDistance = serializedObject.FindProperty("spearThrustDistance");
        carriedSpearVisualOffset = serializedObject.FindProperty("carriedSpearVisualOffset");
        carriedSpearVisualScale = serializedObject.FindProperty("carriedSpearVisualScale");
        carriedSpearVisualRotationOffset = serializedObject.FindProperty("carriedSpearVisualRotationOffset");
        carriedSpearSortingOrderOffset = serializedObject.FindProperty("carriedSpearSortingOrderOffset");
        carriedRangedOrbOffset = serializedObject.FindProperty("carriedRangedOrbOffset");
        carriedRangedOrbScale = serializedObject.FindProperty("carriedRangedOrbScale");
        carriedRangedOrbSortingOrderOffset = serializedObject.FindProperty("carriedRangedOrbSortingOrderOffset");
        minionMoveSprites = serializedObject.FindProperty("minionMoveSprites");
        minionMoveTexture = serializedObject.FindProperty("minionMoveTexture");
        minionMoveResource = serializedObject.FindProperty("minionMoveResource");
        minionSpriteScale = serializedObject.FindProperty("minionSpriteScale");
        minionMoveFps = serializedObject.FindProperty("minionMoveFps");

        rockSprite = serializedObject.FindProperty("rockSprite");
        rockMaterial = serializedObject.FindProperty("rockMaterial");
        rockBaseSize = serializedObject.FindProperty("rockBaseSize");
        rockSizeJitter = serializedObject.FindProperty("rockSizeJitter");
        rocksPer100Units = serializedObject.FindProperty("rocksPer100Units");
        centerRockChance = serializedObject.FindProperty("centerRockChance");
        edgeRockChance = serializedObject.FindProperty("edgeRockChance");
        centerSafeRadius = serializedObject.FindProperty("centerSafeRadius");
        borderInset = serializedObject.FindProperty("borderInset");
        borderRockSize = serializedObject.FindProperty("borderRockSize");
        borderSpacing = serializedObject.FindProperty("borderSpacing");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawScriptField();
        EditorGUILayout.Space(4f);

        showSizes = EditorGUILayout.Foldout(showSizes, "Sizes", true);
        if (showSizes)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(playerSize);
            EditorGUILayout.PropertyField(meleeEnemySize);
            EditorGUILayout.PropertyField(rangedEnemySize);
            EditorGUI.indentLevel--;
        }

        showCamera = EditorGUILayout.Foldout(showCamera, "Camera", true);
        if (showCamera)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(cameraOrthographicSize);
            EditorGUI.indentLevel--;
        }

        showGiantEnemy = EditorGUILayout.Foldout(showGiantEnemy, "Giant Enemy", true);
        if (showGiantEnemy)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(giantEnemySize);
            EditorGUILayout.PropertyField(giantEnemyMaterial);
            EditorGUILayout.PropertyField(giantEnemyHealth);
            EditorGUILayout.PropertyField(giantEnemyAttackRange);
            EditorGUI.indentLevel--;
        }

        showMap = EditorGUILayout.Foldout(showMap, "Map", true);
        if (showMap)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(mapSize);
            EditorGUILayout.PropertyField(maps, true);
            EditorGUI.indentLevel--;
        }

        showExit = EditorGUILayout.Foldout(showExit, "Exit", true);
        if (showExit)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(exitTexture);
            EditorGUILayout.PropertyField(exitColor);
            EditorGUILayout.PropertyField(exitSize, new GUIContent("Exit Trigger Size"));
            EditorGUILayout.PropertyField(exitTextureSize, new GUIContent("Exit Texture Size"));
            EditorGUILayout.PropertyField(exitCount);
            EditorGUI.indentLevel--;
        }

        showMaterials = EditorGUILayout.Foldout(showMaterials, "Materials", true);
        if (showMaterials)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(backgroundMaterial, new GUIContent("Default Floor Material"));
            EditorGUILayout.PropertyField(playerMaterial);
            EditorGUILayout.PropertyField(meleeEnemyMaterial);
            EditorGUILayout.PropertyField(rangedEnemyMaterial);
            EditorGUILayout.PropertyField(enemyProjectileMaterial);
            EditorGUILayout.PropertyField(skinVisualDatabase);
            EditorGUILayout.PropertyField(weaponVisualDatabase);
            EditorGUI.indentLevel--;
        }

        showSwordVisual = EditorGUILayout.Foldout(showSwordVisual, "Sword Visual", true);
        if (showSwordVisual)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(swordSwingSprite, new GUIContent("Sword Sprite"));
            EditorGUILayout.PropertyField(swordSwingTexture, new GUIContent("Sword Texture"));
            EditorGUILayout.PropertyField(swordSwingVisualOffset, new GUIContent("Visual Offset"));
            EditorGUILayout.PropertyField(swordSwingVisualScale, new GUIContent("Visual Scale X/Y"));
            EditorGUILayout.PropertyField(swordSwingVisualRotationOffset, new GUIContent("Visual Rotation Offset Degrees"));
            EditorGUILayout.PropertyField(swordSwingDurationMultiplier, new GUIContent("Swing Duration Multiplier"));
            EditorGUI.indentLevel--;
        }

        showCarriedSwordVisual = EditorGUILayout.Foldout(showCarriedSwordVisual, "Carried Sword Visual", true);
        if (showCarriedSwordVisual)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(carriedSwordVisualOffset, new GUIContent("Visual Offset"));
            EditorGUILayout.PropertyField(carriedSwordVisualScale, new GUIContent("Visual Scale X/Y"));
            EditorGUILayout.PropertyField(carriedSwordVisualRotationOffset, new GUIContent("Visual Rotation Offset Degrees"));
            EditorGUILayout.PropertyField(carriedSwordSortingOrderOffset, new GUIContent("Sorting Order Offset"));
            EditorGUI.indentLevel--;
        }

        showSpearVisual = EditorGUILayout.Foldout(showSpearVisual, "Spear Visual", true);
        if (showSpearVisual)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(spearSprite, new GUIContent("Spear Sprite"));
            EditorGUILayout.PropertyField(spearTexture, new GUIContent("Spear Texture"));
            EditorGUILayout.PropertyField(spearVisualOffset, new GUIContent("Visual Offset"));
            EditorGUILayout.PropertyField(spearVisualScale, new GUIContent("Visual Scale X/Y"));
            EditorGUILayout.PropertyField(spearVisualRotationOffset, new GUIContent("Visual Rotation Offset Degrees"));
            EditorGUILayout.PropertyField(spearThrustDistance, new GUIContent("Thrust Distance"));
            EditorGUI.indentLevel--;
        }

        showCarriedSpearVisual = EditorGUILayout.Foldout(showCarriedSpearVisual, "Carried Spear Visual", true);
        if (showCarriedSpearVisual)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(carriedSpearVisualOffset, new GUIContent("Visual Offset"));
            EditorGUILayout.PropertyField(carriedSpearVisualScale, new GUIContent("Visual Scale X/Y"));
            EditorGUILayout.PropertyField(carriedSpearVisualRotationOffset, new GUIContent("Visual Rotation Offset Degrees"));
            EditorGUILayout.PropertyField(carriedSpearSortingOrderOffset, new GUIContent("Sorting Order Offset"));
            EditorGUI.indentLevel--;
        }

        showCarriedRangedOrbVisual = EditorGUILayout.Foldout(showCarriedRangedOrbVisual, "Carried Ranged Orb Visual", true);
        if (showCarriedRangedOrbVisual)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(carriedRangedOrbOffset, new GUIContent("Visual Offset"));
            EditorGUILayout.PropertyField(carriedRangedOrbScale, new GUIContent("Visual Scale X/Y"));
            EditorGUILayout.PropertyField(carriedRangedOrbSortingOrderOffset, new GUIContent("Sorting Order Offset"));
            EditorGUI.indentLevel--;
        }

        showMinionVisual = EditorGUILayout.Foldout(showMinionVisual, "Minion Visual", true);
        if (showMinionVisual)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(minionMoveSprites, new GUIContent("Move Sprites"), true);
            EditorGUILayout.PropertyField(minionMoveTexture, new GUIContent("Move Sheet Texture"));
            EditorGUILayout.PropertyField(minionMoveResource, new GUIContent("Resource Sheet Name"));
            EditorGUILayout.PropertyField(minionSpriteScale, new GUIContent("Sprite Scale"));
            EditorGUILayout.PropertyField(minionMoveFps, new GUIContent("Move FPS"));
            EditorGUI.indentLevel--;
        }

        showRocks = EditorGUILayout.Foldout(showRocks, "Rocks", true);
        if (showRocks)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(rockSprite);
            EditorGUILayout.PropertyField(rockMaterial, new GUIContent("Default Obstacle Material"));
            EditorGUILayout.PropertyField(rockBaseSize);
            EditorGUILayout.PropertyField(rockSizeJitter);
            EditorGUILayout.PropertyField(rocksPer100Units);
            EditorGUILayout.PropertyField(centerRockChance);
            EditorGUILayout.PropertyField(edgeRockChance);
            EditorGUILayout.PropertyField(centerSafeRadius);
            EditorGUILayout.PropertyField(borderInset);
            EditorGUILayout.PropertyField(borderRockSize);
            EditorGUILayout.PropertyField(borderSpacing);
            EditorGUI.indentLevel--;
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawScriptField()
    {
        using (new EditorGUI.DisabledScope(true))
        {
            MonoScript script = MonoScript.FromMonoBehaviour((GameBootstrap)target);
            EditorGUILayout.ObjectField("Script", script, typeof(MonoScript), false);
        }
    }
}
