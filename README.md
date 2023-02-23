# Unity Custom Editor Generator
  
Read the post/tutorial about this utility [on my blog](https://owenmagelssen.com/posts/unity-custom-editor-generation/)  

This Unity editor utility will generate a custom editor script for a MonoBehaviour or ScriptableObject in your project by right-clicking on 
it and selecting Create > Custom Editor. All serialized fields from the script are populated as SerializedProperty members in the editor script. 
This project is intended to be a starting point for a solution that suits the needs of your project. See some of my suggestions for extending it 
[in the post](https://owenmagelssen.com/posts/unity-custom-editor-generation/#next-steps).  

<div align="center">
  <video width=400 autoplay loop muted playsinline src="https://user-images.githubusercontent.com/44145090/220857960-da9ec53b-d86e-4c49-9222-0935f9e3fabc.mp4"/>
</div>

### This:

```c#
using UnityEngine;

namespace TestRootNamespace
{
    public class MyMonoBehaviour : MonoBehaviour
    {
        public string myString;
        public float myFloat;

        [SerializeField] private GameObject myGameObject;
        [SerializeField] private Vector3 myVector3;

        private void Update()
        {
            // ...
        }
    }
}
```  

### Generates this:  

```c#
/******************************************************************
 * Copyright (C) 2023 <Name>. All rights reserved.
 * <ContactInfo>
 ******************************************************************/

using UnityEngine;
using UnityEditor;

namespace TestRootNamespace
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(TestRootNamespace.MyMonoBehaviour))]
    public class MyMonoBehaviourEditor : Editor
    {
        private SerializedProperty _myString;
        private SerializedProperty _myFloat;
        private SerializedProperty _myGameObject;
        private SerializedProperty _myVector3;

        private void OnEnable()
        {
            _myString = serializedObject.FindProperty("myString");
            _myFloat = serializedObject.FindProperty("myFloat");
            _myGameObject = serializedObject.FindProperty("myGameObject");
            _myVector3 = serializedObject.FindProperty("myVector3");
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(_myString);
            EditorGUILayout.PropertyField(_myFloat);
            EditorGUILayout.PropertyField(_myGameObject);
            EditorGUILayout.PropertyField(_myVector3);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
```
