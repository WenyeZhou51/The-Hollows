using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections.Generic;

[CustomEditor(typeof(ComicsDisplayController))]
public class ComicsDisplayEditor : Editor
{
    private ReorderableList comicPanelsList;
    private SerializedProperty comicPanelsProperty;
    private SerializedProperty fadeInDurationProperty;
    private SerializedProperty fadeOutDurationProperty;
    private SerializedProperty panelSlideDistanceProperty;
    private SerializedProperty advanceKeyProperty;
    private SerializedProperty triggerKeyProperty;

    private void OnEnable()
    {
        // Get the serialized properties
        comicPanelsProperty = serializedObject.FindProperty("comicPanels");
        fadeInDurationProperty = serializedObject.FindProperty("fadeInDuration");
        fadeOutDurationProperty = serializedObject.FindProperty("fadeOutDuration");
        panelSlideDistanceProperty = serializedObject.FindProperty("panelSlideDistance");
        advanceKeyProperty = serializedObject.FindProperty("advanceKey");
        triggerKeyProperty = serializedObject.FindProperty("triggerKey");

        // Create the reorderable list for comic panels
        comicPanelsList = new ReorderableList(
            serializedObject,
            comicPanelsProperty,
            true, // draggable
            true, // displayHeader
            true, // add button
            true  // remove button
        );

        // Set up the header
        comicPanelsList.drawHeaderCallback = (Rect rect) => {
            EditorGUI.LabelField(rect, "Comic Panels (order matters)");
        };

        // Set up the element drawing
        comicPanelsList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
            SerializedProperty element = comicPanelsProperty.GetArrayElementAtIndex(index);
            SerializedProperty panelObjectProperty = element.FindPropertyRelative("panelObject");
            SerializedProperty transitionDirectionProperty = element.FindPropertyRelative("transitionDirection");

            rect.y += 2;
            rect.height = EditorGUIUtility.singleLineHeight;

            // Panel number label
            Rect labelRect = new Rect(rect.x, rect.y, 30, rect.height);
            EditorGUI.LabelField(labelRect, (index + 1).ToString() + ".");

            // Panel object field
            Rect objectFieldRect = new Rect(rect.x + 30, rect.y, rect.width * 0.6f - 30, rect.height);
            EditorGUI.PropertyField(objectFieldRect, panelObjectProperty, GUIContent.none);

            // Transition direction enum
            Rect enumFieldRect = new Rect(rect.x + rect.width * 0.6f + 5, rect.y, rect.width * 0.4f - 5, rect.height);
            EditorGUI.PropertyField(enumFieldRect, transitionDirectionProperty, GUIContent.none);
        };

        // Set up the add callback
        comicPanelsList.onAddCallback = (ReorderableList list) => {
            int index = list.serializedProperty.arraySize;
            list.serializedProperty.arraySize++;
            list.index = index;
            SerializedProperty element = list.serializedProperty.GetArrayElementAtIndex(index);
            element.FindPropertyRelative("panelObject").objectReferenceValue = null;
            element.FindPropertyRelative("transitionDirection").enumValueIndex = 3; // RIGHT by default
        };
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.Space();

        // Comic panel settings
        EditorGUILayout.LabelField("Comic Panel Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(fadeInDurationProperty);
        EditorGUILayout.PropertyField(fadeOutDurationProperty);
        EditorGUILayout.PropertyField(panelSlideDistanceProperty);

        EditorGUILayout.Space();

        // Input settings
        EditorGUILayout.LabelField("Input Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(advanceKeyProperty);
        EditorGUILayout.PropertyField(triggerKeyProperty);

        EditorGUILayout.Space();

        // Comic panels list
        EditorGUILayout.LabelField("Comic Panels", EditorStyles.boldLabel);
        comicPanelsList.DoLayoutList();

        EditorGUILayout.Space();

        // Help box with instructions
        EditorGUILayout.HelpBox("Comic Panel Sequence:\n" +
            "1. Add each panel to the list in the order you want them to appear\n" +
            "2. Each panel should be a GameObject with an Image component\n" +
            "3. Position the panels in the Canvas as desired\n" +
            "4. Set the transition direction for each panel\n" +
            "5. The sequence can be triggered with F5 or via a ComicsTrigger", MessageType.Info);

        if (GUILayout.Button("Test Comic Sequence"))
        {
            ComicsDisplayController controller = (ComicsDisplayController)target;
            if (Application.isPlaying)
            {
                controller.StartComicSequence();
            }
            else
            {
                EditorUtility.DisplayDialog("Test Error", "Cannot test in Edit mode. Please enter Play mode to test.", "OK");
            }
        }

        serializedObject.ApplyModifiedProperties();
    }
}

[CustomEditor(typeof(ComicsTrigger))]
public class ComicsTriggerEditor : Editor
{
    private ReorderableList comicPanelsList;
    private SerializedProperty comicPanelsProperty;
    private SerializedProperty triggerOnEnterProperty;
    private SerializedProperty playOnceProperty;
    private SerializedProperty triggerDelayProperty;
    private SerializedProperty playerLayerProperty;
    private SerializedProperty showDebugGizmosProperty;
    private SerializedProperty gizmoColorProperty;

    private void OnEnable()
    {
        // Get the serialized properties
        comicPanelsProperty = serializedObject.FindProperty("comicPanels");
        triggerOnEnterProperty = serializedObject.FindProperty("triggerOnEnter");
        playOnceProperty = serializedObject.FindProperty("playOnce");
        triggerDelayProperty = serializedObject.FindProperty("triggerDelay");
        playerLayerProperty = serializedObject.FindProperty("playerLayer");
        showDebugGizmosProperty = serializedObject.FindProperty("showDebugGizmos");
        gizmoColorProperty = serializedObject.FindProperty("gizmoColor");

        // Create the reorderable list for comic panels
        comicPanelsList = new ReorderableList(
            serializedObject,
            comicPanelsProperty,
            true, // draggable
            true, // displayHeader
            true, // add button
            true  // remove button
        );

        // Set up the header
        comicPanelsList.drawHeaderCallback = (Rect rect) => {
            EditorGUI.LabelField(rect, "Comic Panels (order matters)");
        };

        // Set up the element drawing
        comicPanelsList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
            SerializedProperty element = comicPanelsProperty.GetArrayElementAtIndex(index);
            SerializedProperty panelImageProperty = element.FindPropertyRelative("panelImage");
            SerializedProperty transitionDirectionProperty = element.FindPropertyRelative("transitionDirection");

            rect.y += 2;
            rect.height = EditorGUIUtility.singleLineHeight;

            // Panel number label
            Rect labelRect = new Rect(rect.x, rect.y, 30, rect.height);
            EditorGUI.LabelField(labelRect, (index + 1).ToString() + ".");

            // Panel image field
            Rect objectFieldRect = new Rect(rect.x + 30, rect.y, rect.width * 0.6f - 30, rect.height);
            EditorGUI.PropertyField(objectFieldRect, panelImageProperty, GUIContent.none);

            // Transition direction enum
            Rect enumFieldRect = new Rect(rect.x + rect.width * 0.6f + 5, rect.y, rect.width * 0.4f - 5, rect.height);
            EditorGUI.PropertyField(enumFieldRect, transitionDirectionProperty, GUIContent.none);
        };

        // Set up the add callback
        comicPanelsList.onAddCallback = (ReorderableList list) => {
            int index = list.serializedProperty.arraySize;
            list.serializedProperty.arraySize++;
            list.index = index;
            SerializedProperty element = list.serializedProperty.GetArrayElementAtIndex(index);
            element.FindPropertyRelative("panelImage").objectReferenceValue = null;
            element.FindPropertyRelative("transitionDirection").enumValueIndex = 3; // RIGHT by default
        };
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.Space();

        // Trigger settings
        EditorGUILayout.LabelField("Trigger Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(triggerOnEnterProperty);
        EditorGUILayout.PropertyField(playOnceProperty);
        EditorGUILayout.PropertyField(triggerDelayProperty);
        EditorGUILayout.PropertyField(playerLayerProperty);

        EditorGUILayout.Space();

        // Comic panels list
        EditorGUILayout.LabelField("Comic Panels Configuration", EditorStyles.boldLabel);
        comicPanelsList.DoLayoutList();

        EditorGUILayout.Space();

        // Debug settings
        EditorGUILayout.LabelField("Debug Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(showDebugGizmosProperty);
        EditorGUILayout.PropertyField(gizmoColorProperty);

        EditorGUILayout.Space();

        // Help box with instructions
        EditorGUILayout.HelpBox("Comic Trigger Usage:\n" +
            "1. Ensure this GameObject has a Collider2D set as a trigger\n" +
            "2. Set player layer mask to detect the player\n" +
            "3. Configure the comic panels in the order you want them to appear\n" +
            "4. Each panel should be a UI Image component in your canvas\n" +
            "5. Player will trigger the sequence when entering this area", MessageType.Info);

        if (GUILayout.Button("Manually Trigger Sequence"))
        {
            ComicsTrigger trigger = (ComicsTrigger)target;
            if (Application.isPlaying)
            {
                trigger.TriggerSequence();
            }
            else
            {
                EditorUtility.DisplayDialog("Test Error", "Cannot test in Edit mode. Please enter Play mode to test.", "OK");
            }
        }

        if (GUILayout.Button("Reset Trigger"))
        {
            ComicsTrigger trigger = (ComicsTrigger)target;
            if (Application.isPlaying)
            {
                trigger.ResetTrigger();
                EditorUtility.DisplayDialog("Trigger Reset", "Trigger has been reset and can be activated again.", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Reset Error", "Cannot reset in Edit mode. Please enter Play mode to reset.", "OK");
            }
        }

        serializedObject.ApplyModifiedProperties();
    }
} 