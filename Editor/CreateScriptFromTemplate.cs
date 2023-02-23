using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace OWEN
{
    public static class CreateScriptFromTemplate
    {
        private static string TemplateDirectory => Path.GetDirectoryName(Application.dataPath) + "/Packages/com.owen.customeditorgenerator/Editor/Templates/";
        private const string EditorTemplate = "CustomEditor.cs.txt";
        private static string TempDirPath => Path.GetDirectoryName(Application.dataPath) + "/Temp/CustomEditorGenerator.Temp";
        private static string GeneratedTemplatePath
        { 
            get
            {
                if (!Directory.Exists(TempDirPath))
                    Directory.CreateDirectory(TempDirPath);
                
                return TempDirPath + "/CustomEditorGeneratedTemplate.cs.txt";
            }
        }
        
        private static string Truncate(this string value, int maxLength)
        {
            return string.IsNullOrEmpty(value) ? value : value[..Math.Min(value.Length, maxLength)];
        }

		private static bool HasSerializeFieldAttribute(Type type, string fieldName)
		{
			FieldInfo info = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
			if (info == null) return false;
			return Attribute.IsDefined(info, typeof(SerializeField));
		}

        private static void CreateGeneratedEditor<T>(SerializedObject serializedObject)
        {
            SerializedProperty serializedProperty = serializedObject.GetIterator();
            Type type = typeof(T);
            string propertyDeclarations = string.Empty;
            string propertyInitializers = string.Empty;
            string propertyDrawing = string.Empty;
            
            while (serializedProperty.Next(true))
            {
				string name = serializedProperty.name;

				if (type.GetField(name) != null
					|| HasSerializeFieldAttribute(type, name))
        		{
                	propertyDeclarations = string.Concat(propertyDeclarations, $"\t\tprivate SerializedProperty _{name};\n");
                	propertyInitializers = string.Concat(propertyInitializers, $"\t\t\t_{name} = serializedObject.FindProperty(\"{name}\");\n");
                	propertyDrawing = string.Concat(propertyDrawing, $"\t\t\tEditorGUILayout.PropertyField(_{name});\n");
				}
            }

            // remove trailing line breaks
            propertyInitializers = propertyInitializers.Truncate(propertyInitializers.Length - 1);
            propertyDrawing = propertyDrawing.Truncate(propertyDrawing.Length - 1);

            string templatePath = TemplateDirectory + EditorTemplate;
            string templateContent = File.ReadAllText(templatePath);
            string withDeclarations = templateContent.Replace("#PROPERTIES#", propertyDeclarations);
            string withInitializers = withDeclarations.Replace("#PROPERTYINITIALIZERS#", propertyInitializers);
            string withDrawing = withInitializers.Replace("#PROPERTYDRAWING#", propertyDrawing);
            string withTargetName = withDrawing.Replace("#TARGETNAME#", type.FullName);
            File.WriteAllText(GeneratedTemplatePath, withTargetName);
            ProjectWindowUtil.CreateScriptAssetFromTemplateFile(GeneratedTemplatePath,  type.Name + "Editor.cs");
        }

        public static void CreateGeneratedScriptableObjectEditor<T>() where T : ScriptableObject
        {
            var scriptableObject = ScriptableObject.CreateInstance(typeof(T));
            SerializedObject serializedObject = new SerializedObject(scriptableObject);
            CreateGeneratedEditor<T>(serializedObject);
        }

        public static void CreateGeneratedMonoBehaviourEditor<T>() where T : MonoBehaviour
        {
            var obj = new GameObject();
            var monoBehaviour = obj.AddComponent<T>();
                
            if (monoBehaviour == null)
            {
                UnityEngine.Object.DestroyImmediate(obj);
                CreateGenericEditor();
                return;
            }
                    
            SerializedObject serializedObject = new SerializedObject(monoBehaviour);
            CreateGeneratedEditor<T>(serializedObject);
            UnityEngine.Object.DestroyImmediate(obj);
        }

        public static void CreateGenericEditor()
        {
            string templatePath = TemplateDirectory + EditorTemplate;
            string templateContent = File.ReadAllText(templatePath);
            string withDeclarations = templateContent.Replace("#PROPERTIES#", "\t\t");
            string withInitializers = withDeclarations.Replace("#PROPERTYINITIALIZERS#", "\t\t\t");
            string withDrawing = withInitializers.Replace("#PROPERTYDRAWING#", "\t\t\t");
            string withTargetName = withDrawing.Replace("#TARGETNAME#", "#NAME#");
            File.WriteAllText(GeneratedTemplatePath, withTargetName);
            ProjectWindowUtil.CreateScriptAssetFromTemplateFile(GeneratedTemplatePath,  "NewCustomEditor.cs");
        }
        
        [MenuItem("Assets/Create/Custom Editor", priority = 41)]
        public static void CreateCustomEditor()
        {
            string selectionPath = AssetDatabase.GetAssetPath(Selection.activeInstanceID);
            
            if (string.IsNullOrEmpty(selectionPath))
            {
                CreateGenericEditor();
                return;
            }

            var monoScript = AssetDatabase.LoadAssetAtPath<MonoScript>(selectionPath);
            
            if (monoScript != null)
            {
                var scriptClass = monoScript.GetClass();

                if (scriptClass.IsSubclassOf(typeof(MonoBehaviour)))
                {
                    var mi = typeof(CreateScriptFromTemplate).GetMethod("CreateGeneratedMonoBehaviourEditor");
                    var methodRef = mi.MakeGenericMethod(scriptClass);
                    methodRef.Invoke(null, null);
                    return;
                }
                
                if (scriptClass.IsSubclassOf(typeof(ScriptableObject)))
                {
                    var mi = typeof(CreateScriptFromTemplate).GetMethod("CreateGeneratedScriptableObjectEditor");
                    var methodRef = mi.MakeGenericMethod(scriptClass);
                    methodRef.Invoke(null, null);
                    return;
                }
            }
            
            CreateGenericEditor();
        }
    }
}
