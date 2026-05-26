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
