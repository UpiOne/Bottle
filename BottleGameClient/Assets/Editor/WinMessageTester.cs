using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

[CustomEditor(typeof(BottleController))]
public class WinMessageTester : Editor
{
/*private SerializedProperty niceWinTextProperty;
private SerializedProperty bigWinTextProperty;
private SerializedProperty megaWinTextProperty;
private SerializedProperty jackpotTextProperty;

private void OnEnable()
{
    niceWinTextProperty = serializedObject.FindProperty("niceWinText");
    bigWinTextProperty = serializedObject.FindProperty("bigWinText");
    megaWinTextProperty = serializedObject.FindProperty("megaWinText");
    jackpotTextProperty = serializedObject.FindProperty("jackpotText");
}

public override void OnInspectorGUI()
{
    // Отображаем стандартный инспектор
    DrawDefaultInspector();

    serializedObject.Update();

    // Добавляем заголовок для секции тестирования
    EditorGUILayout.Space();
    EditorGUILayout.LabelField("Тестирование надписей выигрыша", EditorStyles.boldLabel);
    EditorGUILayout.Space();

    // Предупреждение о том, что тестирование работает только в режиме воспроизведения
    if (!Application.isPlaying)
    {
        EditorGUILayout.HelpBox("Тестирование надписей доступно только в режиме воспроизведения (Play Mode).", MessageType.Info);
        return;
    }

    BottleController bottleController = (BottleController)target;

    // Проверяем, все ли объекты назначены
    bool allTextObjectsAssigned = (
        niceWinTextProperty.objectReferenceValue != null &&
        bigWinTextProperty.objectReferenceValue != null &&
        megaWinTextProperty.objectReferenceValue != null &&
        jackpotTextProperty.objectReferenceValue != null
    );

    if (!allTextObjectsAssigned)
    {
        EditorGUILayout.HelpBox("Для тестирования необходимо назначить все текстовые объекты (Nice, Big Win, Mega Win, Jackpot).", MessageType.Warning);
    }

    // Поле для тестового множителя
    float testMultiplier = EditorGUILayout.FloatField("Тестовый множитель", bottleController.currentMultiplier);
    if (testMultiplier != bottleController.currentMultiplier)
    {
        bottleController.currentMultiplier = testMultiplier;
    }

    // Кнопки для тестирования каждой надписи
    EditorGUILayout.BeginHorizontal();

    if (GUILayout.Button("Показать Nice (3x)"))
    {
        bottleController.currentMultiplier = 3f;
        //bottleController.HideAllWinTexts();
        ShowWinText(bottleController, "nice");
    }

    if (GUILayout.Button("Показать Big Win (5x)"))
    {
        bottleController.currentMultiplier = 5f;
        //// bottleController.HideAllWinTexts();
        ShowWinText(bottleController, "big");
    }

    EditorGUILayout.EndHorizontal();

    EditorGUILayout.BeginHorizontal();

    if (GUILayout.Button("Показать Mega Win (10x)"))
    {
        bottleController.currentMultiplier = 10f;
        // bottleController.HideAllWinTexts();
        ShowWinText(bottleController, "mega");
    }

    if (GUILayout.Button("Показать Jackpot (>10x)"))
    {
        bottleController.currentMultiplier = 15f;
        // bottleController.HideAllWinTexts();
        ShowWinText(bottleController, "jackpot");
    }

    EditorGUILayout.EndHorizontal();

    // Кнопка для скрытия всех текстов
    if (GUILayout.Button("Скрыть все надписи"))
    {
        //  bottleController.HideAllWinTexts();
    }

    serializedObject.ApplyModifiedProperties();
}

private void ShowWinText(BottleController bottleController, string type)
{
    GameObject textObject = null;

    switch (type)
    {
        case "nice":
            textObject = (GameObject)niceWinTextProperty.objectReferenceValue;
            break;
        case "big":
            textObject = (GameObject)bigWinTextProperty.objectReferenceValue;
            break;
        case "mega":
            textObject = (GameObject)megaWinTextProperty.objectReferenceValue;
            break;
        case "jackpot":
            textObject = (GameObject)jackpotTextProperty.objectReferenceValue;
            break;
    }

    if (textObject != null)
    {
        textObject.SetActive(true);
        // Запускаем корутину анимации
        bottleController.StartCoroutine("AnimateWinText", textObject);
    }
}*/
} 