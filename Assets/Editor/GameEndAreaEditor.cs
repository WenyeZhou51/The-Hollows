using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(GameEndArea))]
public class GameEndAreaEditor : Editor
{
    private bool showDebugOptions = false;
    
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        EditorGUILayout.Space();
        
        GameEndArea gameEndArea = (GameEndArea)target;
        
        // Draw a separator
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        
        // Debug options for testing in the editor
        showDebugOptions = EditorGUILayout.Foldout(showDebugOptions, "Debug Options");
        
        if (showDebugOptions)
        {
            EditorGUILayout.HelpBox("These options are only for testing in the editor.", MessageType.Info);
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Test End Sequence"))
            {
                // Find a player to use for testing
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                
                if (player == null)
                {
                    // Create a temporary player if none exists
                    player = new GameObject("Temporary Player");
                    player.tag = "Player";
                    player.AddComponent<Rigidbody2D>();
                    player.AddComponent<Animator>();
                    player.AddComponent<SpriteRenderer>();
                    
                    // Add a basic player controller component
                    player.AddComponent<PlayerController>();
                    
                    // Position the player at the GameEndArea
                    player.transform.position = gameEndArea.transform.position;
                    
                    EditorUtility.DisplayDialog("Temporary Player Created", 
                        "A temporary player has been created for testing. It will be used to test the end sequence.", 
                        "OK");
                }
                
                // Manually trigger the OnTriggerEnter2D by creating a fake collider for testing
                Collider2D fakeCollider = player.GetComponent<Collider2D>();
                if (fakeCollider == null)
                {
                    fakeCollider = player.AddComponent<BoxCollider2D>();
                }
                
                // Use reflection to access the private method
                System.Type type = gameEndArea.GetType();
                System.Reflection.MethodInfo method = type.GetMethod("OnTriggerEnter2D", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (method != null)
                {
                    method.Invoke(gameEndArea, new object[] { fakeCollider });
                    EditorUtility.DisplayDialog("End Sequence Started", 
                        "The end sequence should now be running. Note that some effects may not display correctly in edit mode.", 
                        "OK");
                }
            }
            
            if (GUILayout.Button("Position Player Here"))
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                
                if (player != null)
                {
                    Undo.RecordObject(player.transform, "Position Player at GameEndArea");
                    player.transform.position = gameEndArea.transform.position;
                }
                else
                {
                    EditorUtility.DisplayDialog("No Player Found", 
                        "No GameObject with the 'Player' tag was found in the scene.", 
                        "OK");
                }
            }
            
            EditorGUILayout.EndHorizontal();
        }
    }
    
    private void OnSceneGUI()
    {
        GameEndArea gameEndArea = (GameEndArea)target;
        
        // Draw an arrow showing the walking direction
        Handles.color = new Color(1f, 0.5f, 0f, 0.8f);
        
        Vector3 start = gameEndArea.transform.position;
        Vector3 end = start + Vector3.right * 5f;
        
        // Draw the line
        Handles.DrawLine(start, end);
        
        // Draw the arrowhead
        float arrowSize = 0.5f;
        Vector3[] arrowHead = new Vector3[3];
        arrowHead[0] = end;
        arrowHead[1] = end + new Vector3(-arrowSize, arrowSize, 0);
        arrowHead[2] = end + new Vector3(-arrowSize, -arrowSize, 0);
        
        Handles.DrawAAPolyLine(2f, arrowHead);
        
        // Add a label
        Handles.Label(end + Vector3.up * 0.5f, "Game End Path");
    }
} 