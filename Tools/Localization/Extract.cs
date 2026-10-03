var entries = new System.Collections.Generic.SortedDictionary<string,System.Collections.Generic.List<string>>();
void Add(string value,string context) { if(string.IsNullOrWhiteSpace(value))return; if(!entries.ContainsKey(value))entries[value]=new(); entries[value].Add(context); }
foreach(var guid in UnityEditor.AssetDatabase.FindAssets("t:ScriptableObject",new[]{"Assets/Configs"})) {
 var path=UnityEditor.AssetDatabase.GUIDToAssetPath(guid);var asset=UnityEditor.AssetDatabase.LoadMainAssetAtPath(path);var so=new UnityEditor.SerializedObject(asset);var p=so.GetIterator();
 while(p.Next(true)) if(p.propertyType==UnityEditor.SerializedPropertyType.String && (p.name=="_displayName" || p.name=="_description")) Add(p.stringValue,path+" :: "+p.propertyPath);
}
void Labels(UnityEngine.GameObject root,string path) {
 foreach(var t in root.GetComponentsInChildren<TMPro.TMP_Text>(true))Add(t.text,path+" :: "+t.name);
 foreach(var t in root.GetComponentsInChildren<UnityEngine.UI.Text>(true))Add(t.text,path+" :: "+t.name);
}
foreach(var guid in UnityEditor.AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Prefabs"})) {var path=UnityEditor.AssetDatabase.GUIDToAssetPath(guid);Labels(UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(path),path);}
var setup=UnityEditor.SceneManagement.EditorSceneManager.GetSceneManagerSetup();
foreach(var path in new[]{"Assets/Scenes/1.MainMenu.unity","Assets/Scenes/2.Base.unity","Assets/Scenes/3.Arena.unity"}) {
 var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path);foreach(var root in scene.GetRootGameObjects())Labels(root,path);
}
if(setup.Length>0 && setup.Any(s=>s.isLoaded)) UnityEditor.SceneManagement.EditorSceneManager.RestoreSceneManagerSetup(setup);
System.IO.Directory.CreateDirectory("Tools/Localization");
System.IO.File.WriteAllText("Tools/Localization/authored.json",(string)System.AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new object[]{entries}));
return $"Exported {entries.Count} authored strings";
