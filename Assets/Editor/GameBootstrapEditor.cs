using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GameBootstrap))]
[CanEditMultipleObjects]
public class GameBootstrapEditor : Editor
{
    private SerializedProperty playerSize;
    private SerializedProperty meleeEnemySize;
    private SerializedProperty rangedEnemySize;

    private SerializedProperty mapSize;

    private SerializedProperty backgroundMaterial;
    private SerializedProperty playerMaterial;
    private SerializedProperty meleeEnemyMaterial;
    private SerializedProperty rangedEnemyMaterial;
    private SerializedProperty enemyProjectileMaterial;

    private SerializedProperty rockSprite;
    private SerializedProperty rockMaterial;
    private SerializedProperty rockColor;
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
    private bool showMap = true;
    private bool showMaterials = true;
    private bool showRocks = true;

    private void OnEnable()
    {
        playerSize = serializedObject.FindProperty("playerSize");
        meleeEnemySize = serializedObject.FindProperty("meleeEnemySize");
        rangedEnemySize = serializedObject.FindProperty("rangedEnemySize");

        mapSize = serializedObject.FindProperty("mapSize");

        backgroundMaterial = serializedObject.FindProperty("backgroundMaterial");
        playerMaterial = serializedObject.FindProperty("playerMaterial");
        meleeEnemyMaterial = serializedObject.FindProperty("meleeEnemyMaterial");
        rangedEnemyMaterial = serializedObject.FindProperty("rangedEnemyMaterial");
        enemyProjectileMaterial = serializedObject.FindProperty("enemyProjectileMaterial");

        rockSprite = serializedObject.FindProperty("rockSprite");
        rockMaterial = serializedObject.FindProperty("rockMaterial");
        rockColor = serializedObject.FindProperty("rockColor");
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

        showMap = EditorGUILayout.Foldout(showMap, "Map", true);
        if (showMap)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(mapSize);
            EditorGUI.indentLevel--;
        }

        showMaterials = EditorGUILayout.Foldout(showMaterials, "Materials", true);
        if (showMaterials)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(backgroundMaterial);
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
            EditorGUILayout.PropertyField(rockMaterial);
            EditorGUILayout.PropertyField(rockColor);
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
