
var previous = UnityEditor.SceneManagement.EditorSceneManager.GetSceneManagerSetup();
var font=AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
RectTransform Rect(string name,Transform parent,Vector2 min,Vector2 max,Vector2 pos,Vector2 size) {
 var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=min;r.anchorMax=max;r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=pos;r.sizeDelta=size;return r;
}
TMPro.TMP_Text Text(Transform parent,string value,int size) {
 var r=Rect("Label",parent,Vector2.zero,Vector2.one,Vector2.zero,new Vector2(-20,-8));var t=r.gameObject.AddComponent<TMPro.TextMeshProUGUI>();t.font=font;t.fontSize=size;t.color=new Color(.94f,.89f,.77f);t.text=value;t.alignment=TMPro.TextAlignmentOptions.Center;t.raycastTarget=false;return t;
}
UnityEngine.UI.Button ButtonAt(string name,Transform parent,string label,float y,float width=480) {
 var r=Rect(name,parent,new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(0,y),new Vector2(width,54));
 var image=r.gameObject.AddComponent<UnityEngine.UI.Image>();image.color=new Color(.29f,.25f,.19f);var b=r.gameObject.AddComponent<UnityEngine.UI.Button>();b.targetGraphic=image;Text(r,label,20);return b;
}
foreach(var path in new[]{"Assets/Scenes/2.Base.unity","Assets/Scenes/3.Arena.unity"}) {
 var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path);bool isBase=path.Contains("2.Base");
 if(UnityEngine.Object.FindFirstObjectByType<Code.Gameplay.PlatformRewardsView>(FindObjectsInactive.Include)!=null)continue;
 var root=new GameObject("PlatformRewardsCanvas",typeof(RectTransform),typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler),typeof(UnityEngine.UI.GraphicRaycaster));
 root.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;root.GetComponent<Canvas>().sortingOrder=85;
 var scaler=root.GetComponent<UnityEngine.UI.CanvasScaler>();scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1600,900);scaler.matchWidthOrHeight=.5f;
 var view=root.AddComponent<Code.Gameplay.PlatformRewardsView>();var so=new SerializedObject(view);so.FindProperty("_base").boolValue=isBase;
 var panel=Rect("RewardsPanel",root.transform,isBase?new Vector2(1,.5f):new Vector2(.5f,0),isBase?new Vector2(1,.5f):new Vector2(.5f,0),isBase?new Vector2(-284,0):new Vector2(0,60),isBase?new Vector2(528,430):new Vector2(680,100));
 panel.gameObject.AddComponent<UnityEngine.UI.Image>().color=new Color(.12f,.11f,.10f,.98f);so.FindProperty("_panel").objectReferenceValue=panel.gameObject;
 if(isBase){
   var toggle=Rect("RecoveryToggle",root.transform,new Vector2(1,1),new Vector2(1,1),new Vector2(-165,-130),new Vector2(290,54));
   toggle.gameObject.AddComponent<UnityEngine.UI.Image>().color=new Color(.29f,.25f,.19f);var tb=toggle.gameObject.AddComponent<UnityEngine.UI.Button>();Text(toggle,"Бонусы и восстановление",20);so.FindProperty("_toggle").objectReferenceValue=tb;
   so.FindProperty("_money").objectReferenceValue=ButtonAt("MoneyAd",panel,"Реклама: +50 денариев",-44);
   so.FindProperty("_supplies").objectReferenceValue=ButtonAt("SupplyAd",panel,"Реклама: +1 расходник",-106);
   so.FindProperty("_heal").objectReferenceValue=ButtonAt("HealAd",panel,"Реклама: восстановить отряд",-168);
   so.FindProperty("_rest").objectReferenceValue=ButtonAt("FreeRecovery",panel,"Бесплатный отдых: 2:00",-238);
   var status=Rect("Status",panel,new Vector2(0,0),new Vector2(1,0),new Vector2(0,72),new Vector2(-24,118));so.FindProperty("_status").objectReferenceValue=Text(status,"Ранения сохраняются между боями",19);
 }else{
   so.FindProperty("_battle").objectReferenceValue=ButtonAt("BattleBonusAd",panel,"Реклама: удвоить награду за бой",-38,640);
   var status=Rect("Status",panel,new Vector2(0,0),new Vector2(1,0),new Vector2(0,25),new Vector2(-24,36));so.FindProperty("_status").objectReferenceValue=Text(status,"Дополнительный бонус за просмотр",18);
 }
 so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(view);panel.gameObject.SetActive(false);
 UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
}
UnityEditor.SceneManagement.EditorSceneManager.RestoreSceneManagerSetup(previous);
return "Rewards UI created in Base and Arena; content catalogs untouched.";
