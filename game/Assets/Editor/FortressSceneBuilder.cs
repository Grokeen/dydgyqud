using System;
using System.IO;
using MiniFortress;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Explicit, non-destructive authoring command. Existing scenes, prefabs and data are never regenerated.
public static class FortressSceneBuilder
{
    public const string Content = "Assets/FortressContent";
    public const string ScenePath = "Assets/Scenes/FortressBattle.unity";
    static Font font;
    static readonly Color PanelColor = new Color(.025f, .055f, .08f, .96f);
    static readonly Color Gold = new Color(.8f, .65f, .35f);

    [MenuItem("Mini Fortress/Open Editable Battlefield")]
    public static void Open()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath)) EditorSceneManager.OpenScene(ScenePath);
        else Generate();
    }

    public static void Generate()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath)) return;
        foreach (string folder in new[] { Content, Content + "/Art", Content + "/Data", Content + "/Prefabs", Content + "/Input", Content + "/UI" })
            Directory.CreateDirectory(folder);
        AssetDatabase.Refresh();
        FortressAnimationBuilder.EnsureAssets();
        font = AssetDatabase.LoadAssetAtPath<Font>(Content + "/UI/NotoSansCJKkr-Regular.otf");
        if (!font) throw new InvalidOperationException("NotoSansCJKkr-Regular.otf is required in FortressContent/UI.");
        var people = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/FortressArt/Characters.png");
        var weapons = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/FortressArt/Weapons.png");
        var background = Bake("Citadel", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/FortressArt/CitadelArena.png"), new Rect(0, 0, 1, 1), 60, Vector2.one * .5f, false, false);
        var archer = Bake("Archer", people, new Rect(0, .5f, .5f, .5f), 4.2f, new Vector2(.5f, 0));
        var spearman = Bake("Spearman", people, new Rect(.5f, .5f, .5f, .5f), 4.2f, new Vector2(.5f, 0));
        var goblin = Bake("Goblin", people, new Rect(0, 0, .5f, .5f), 4.2f, new Vector2(.5f, 0));
        var captain = Bake("Captain", people, new Rect(.5f, 0, .5f, .5f), 4.2f, new Vector2(.5f, 0));
        var bow = Bake("Bow", weapons, new Rect(.16f, .4f, .23f, .6f), 2.45f, Vector2.one * .5f);
        var arrow = Bake("Arrow", weapons, new Rect(.5f, .6f, .5f, .2f), 1.9f, new Vector2(1, .5f), true);
        var spear = Bake("Spear", weapons, new Rect(0, .17f, .65f, .15f), 3.5f, new Vector2(1, .5f), true);
        var glowTexture = new Texture2D(64, 64);
        for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++)
        {
            float d = Vector2.Distance(new Vector2(x, y), Vector2.one * 31.5f) / 31.5f;
            glowTexture.SetPixel(x, y, new Color(1, 1, 1, Mathf.Pow(Mathf.Clamp01(1 - d), 1.7f)));
        }
        glowTexture.Apply();
        var glow = Bake("SoftCircle", glowTexture, new Rect(0, 0, 1, 1), 1, Vector2.one * .5f, false, false);
        Object.DestroyImmediate(glowTexture);
        var archerData = Character("Archer", "궁수", "정교한 곡사 · 넓은 범위 피해", archer, bow, arrow, glow, false, 120, 32, 3.5f, 10, 5, 1);
        var spearData = Character("Spearman", "창병", "강력한 직격 · 높은 생존력", spearman, null, spear, glow, true, 140, 40, 1.5f, 10, 5, 1);
        var goblinData = Character("Goblin", "성채 경비병", "성채를 지키는 궁수", goblin, bow, arrow, glow, false, 40, 10, 3.5f, 4, 3, 1);
        var captainData = Character("Captain", "성채 대장", "성채의 지휘관", captain, bow, arrow, glow, false, 64, 10, 3.5f, 4, 3, 1.25f);
        var rules = AssetDatabase.LoadAssetAtPath<FortressBattleRules>(Content + "/Data/BattleRules.asset");
        if (!rules) { rules = ScriptableObject.CreateInstance<FortressBattleRules>(); AssetDatabase.CreateAsset(rules, Content + "/Data/BattleRules.asset"); }
        var actions = MakeInput();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var arena = new GameObject("Battlefield").AddComponent<FortressArena>();
        arena.rules = rules; arena.effectSprite = glow; arena.playerClasses = new[] { archerData, spearData };
        var painting = SpriteObject(arena.transform, "Background - edit Sprite and Transform", background, new Vector2(50, 18), -20);
        painting.transform.localScale = new Vector3(106.66667f / background.bounds.size.x, 60 / background.bounds.size.y, 1);
        arena.background = painting;
        var camera = Child(arena.transform, "Battle Camera").AddComponent<Camera>();
        camera.transform.position = new Vector3(50, 18, -30); camera.orthographic = true; camera.orthographicSize = 30;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.065f, .105f, .18f);
        camera.tag = "MainCamera"; camera.gameObject.AddComponent<AudioListener>(); arena.worldCamera = camera;
        var display = Child(arena.transform, "HUD Display Camera").AddComponent<Camera>();
        display.clearFlags = CameraClearFlags.SolidColor; display.backgroundColor = Color.black;
        display.cullingMask = 0; display.depth = 1;
        arena.terrainRoot = Child(arena.transform, "Terrain - select a platform and Edit Collider").transform;
        float[,] rectangles = { {-3,18.3f,24.7f,-14}, {18.3f,32.7f,6.9f,-14}, {38.3f,52,8.6f,-14}, {18.3f,63,18.4f,17.7f}, {63,81.5f,28.5f,-14}, {81.5f,103,6.9f,-14}, {92,103,17,16}, {54,58,22,18.4f}, {58,63,24.5f,18.4f} };
        string[] names = { "Left Cliff", "Lower Left", "Lower Center", "Bridge", "Right Cliff", "Lower Right", "Right Ledge", "Step 1", "Step 2" };
        for (int i = 0; i < names.Length; i++)
        {
            var platform = Child(arena.terrainRoot, names[i]).AddComponent<FortressTerrain>();
            float left = rectangles[i,0], right = rectangles[i,1], top = rectangles[i,2], bottom = rectangles[i,3];
            platform.transform.position = new Vector3((left + right) / 2, (top + bottom) / 2, 0);
            var box = platform.GetComponent<BoxCollider2D>(); box.size = new Vector2(right - left, top - bottom); box.isTrigger = true;
            platform.allowDropThrough = i == 3 || i == 6;
        }
        var spawns = Child(arena.transform, "Spawns").transform;
        arena.playerSpawn = Child(spawns, "Player Spawn").transform; arena.playerSpawn.position = new Vector3(9, 24.7f, 0);
        arena.playerSpawn.gameObject.AddComponent<FortressSpawnPoint>().character = archerData;
        arena.enemySpawns = Child(spawns, "Enemies - hierarchy order is turn order").transform;
        Spawn(arena.enemySpawns, "다리 파수꾼", goblinData, 46, 18.4f);
        Spawn(arena.enemySpawns, "성채 대장", captainData, 72, 28.5f);
        Spawn(arena.enemySpawns, "절벽 사수", goblinData, 97, 17);
        Spawn(arena.enemySpawns, "하단 경비병", goblinData, 85, 6.9f);
        var game = new GameObject("Battle Controller").AddComponent<FortressGame>();
        var input = game.gameObject.AddComponent<FortressInput>(); input.actions = actions;
        var hud = CreateHud(game);
        hud.battlefield.texture = background.texture;
        var serialized = new SerializedObject(game);
        serialized.FindProperty("arena").objectReferenceValue = arena;
        serialized.FindProperty("input").objectReferenceValue = input;
        serialized.FindProperty("hud").objectReferenceValue = hud;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        var events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        Selection.activeGameObject = arena.gameObject;
        if (SceneView.lastActiveSceneView) SceneView.lastActiveSceneView.LookAt(new Vector3(50,18,0), Quaternion.identity, 60, true);
        Debug.Log("Editable battlefield created: " + ScenePath);
    }

    static void Spawn(Transform parent, string name, FortressCharacterDefinition data, float x, float y)
    {
        var spawn = Child(parent, name).AddComponent<FortressSpawnPoint>(); spawn.character = data;
        spawn.displayName = name; spawn.transform.position = new Vector3(x, y, 0);
    }
    static GameObject Child(Transform parent, string name)
    {
        var item = new GameObject(name); item.transform.SetParent(parent, false); return item;
    }
    static SpriteRenderer SpriteObject(Transform parent, string name, Sprite sprite, Vector2 position, int order)
    {
        var renderer = Child(parent, name).AddComponent<SpriteRenderer>(); renderer.sprite = sprite;
        renderer.transform.localPosition = position; renderer.sortingOrder = order; return renderer;
    }
    static Sprite Bake(string name, Texture2D source, Rect region, float units, Vector2 pivot, bool byWidth = false, bool trim = true)
    {
        string path = Content + "/Art/" + name + ".png";
        var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path); if (existing) return existing;
        if (File.Exists(path)) throw new InvalidOperationException("Existing art must be imported as Sprite: " + path);
        int x = Mathf.RoundToInt(region.x * source.width), y = Mathf.RoundToInt(region.y * source.height);
        int w = Mathf.Min(Mathf.RoundToInt(region.width * source.width), source.width - x);
        int h = Mathf.Min(Mathf.RoundToInt(region.height * source.height), source.height - y);
        var pixels = source.GetPixels(x, y, w, h);
        int minX = 0, minY = 0, maxX = w - 1, maxY = h - 1;
        if (trim)
        {
            minX = w; minY = h; maxX = maxY = 0;
            for (int py = 0; py < h; py++) for (int px = 0; px < w; px++)
                if (pixels[py*w+px].a > .08f) { minX = Mathf.Min(minX,px); minY = Mathf.Min(minY,py); maxX = Mathf.Max(maxX,px); maxY = Mathf.Max(maxY,py); }
        }
        int cw = maxX-minX+1, ch = maxY-minY+1;
        var result = new Texture2D(cw, ch, TextureFormat.RGBA32, false);
        var cropped = new Color[cw*ch];
        for (int row=0;row<ch;row++) Array.Copy(pixels,(minY+row)*w+minX,cropped,row*cw,cw);
        result.SetPixels(cropped); result.Apply(); File.WriteAllBytes(path,result.EncodeToPNG()); Object.DestroyImmediate(result);
        AssetDatabase.ImportAsset(path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
        var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.Custom; settings.spritePivot = pivot;
        importer.SetTextureSettings(settings);
        importer.spritePixelsPerUnit = (byWidth ? cw : ch) / units;
        importer.alphaIsTransparency = true; importer.mipmapEnabled = false; importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed; importer.maxTextureSize = 2048;
        importer.SaveAndReimport(); return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    static FortressCharacterDefinition Character(string id, string name, string description, Sprite body, Sprite bow, Sprite projectile, Sprite glow, bool spear, int hp, int damage, float radius, float movement, float speed, float scale)
    {
        string path = Content + "/Data/" + id + ".asset";
        var data = AssetDatabase.LoadAssetAtPath<FortressCharacterDefinition>(path); if (data) return data;
        string prefabPath = Content + "/Prefabs/" + id + ".prefab";
        var prefab = AssetDatabase.LoadAssetAtPath<FortressFighterView>(prefabPath);
        if (!prefab)
        {
            if (File.Exists(prefabPath)) throw new InvalidOperationException("Existing prefab needs FortressFighterView: " + prefabPath);
            var root = new GameObject(id); root.transform.localScale = Vector3.one * scale;
            var view = root.AddComponent<FortressFighterView>();
            view.motion = Child(root.transform, "Motion").transform;
            view.body = SpriteObject(view.motion, "Body", body, Vector2.zero, 12);
            var shadow = SpriteObject(root.transform, "Ground shadow", glow, new Vector2(0,.05f),10);
            shadow.color = new Color(0,0,0,.8f); shadow.transform.localScale = new Vector3(2.9f,.4f,1);
            view.aimPivot = Child(view.motion,"AimPivot").transform; view.aimPivot.localPosition = new Vector3(0,2.5f,0);
            view.weaponMotion = Child(view.aimPivot,"WeaponMotion").transform;
            view.weapon = Child(view.weaponMotion, spear ? "Throwing spear" : "Longbow").transform;
            if (bow) SpriteObject(view.weapon,"Bow",bow,new Vector2(1.1f,0),15);
            view.loadedProjectile = SpriteObject(view.weapon,"Loaded projectile",projectile,new Vector2(1.6f,0),16).transform;
            view.animator = view.motion.gameObject.AddComponent<Animator>();
            view.animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(FortressAnimationBuilder.ControllerPath);
            view.animator.applyRootMotion = false; view.animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            prefab = PrefabUtility.SaveAsPrefabAsset(root,prefabPath).GetComponent<FortressFighterView>();
            Object.DestroyImmediate(root);
        }
        data = ScriptableObject.CreateInstance<FortressCharacterDefinition>();
        data.displayName = name; data.description = description; data.prefab = prefab; data.portrait = body; data.projectile = projectile;
        data.weapon = spear ? FortressWeapon.Spear : FortressWeapon.Bow; data.health = hp; data.damage = damage;
        data.blastRadius = radius; data.movementPerTurn = movement; data.movementSpeed = speed; data.releaseTime = spear ? .22f : .16f;
        AssetDatabase.CreateAsset(data,path); return data;
    }
    static InputActionAsset MakeInput()
    {
        string path = Content + "/Input/FortressControls.inputactions";
        var existing = AssetDatabase.LoadAssetAtPath<InputActionAsset>(path); if (existing) return existing;
        var asset = ScriptableObject.CreateInstance<InputActionAsset>(); var map = asset.AddActionMap("Battle");
        Axis(map,"Move","a","d"); Axis(map,"Angle","downArrow","upArrow");
        Axis(map,"Power","leftArrow","rightArrow"); Axis(map,"Select","leftArrow","rightArrow");
        string[,] keys = { {"Jump","w"}, {"Drop","s"}, {"Fire","space"}, {"Restart","r"}, {"Back","escape"}, {"Confirm","enter"}, {"SelectFirst","1"}, {"SelectSecond","2"}, {"EndTurn","tab"} };
        for (int i=0;i<keys.GetLength(0);i++) map.AddAction(keys[i,0],InputActionType.Button,"<Keyboard>/"+keys[i,1]);
        File.WriteAllText(path,asset.ToJson()); Object.DestroyImmediate(asset); AssetDatabase.ImportAsset(path);
        return AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
    }
    static void Axis(InputActionMap map,string name,string negative,string positive)
        => map.AddAction(name,InputActionType.Value).AddCompositeBinding("1DAxis").With("Negative","<Keyboard>/"+negative).With("Positive","<Keyboard>/"+positive);

    static RectTransform Rect(Transform parent,string name,float x,float y,float w,float h)
    {
        var rect = new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent,false);
        rect.anchorMin = rect.anchorMax = new Vector2(0,1); rect.pivot = new Vector2(0,1);
        rect.anchoredPosition = new Vector2(x,-y); rect.sizeDelta = new Vector2(w,h); return rect;
    }
    static Image Panel(Transform parent,string name,float x,float y,float w,float h)
    {
        var rect = Rect(parent,name,x,y,w,h); var image = rect.gameObject.AddComponent<Image>(); image.color = PanelColor;
        var outline = rect.gameObject.AddComponent<Outline>(); outline.effectColor = Gold; outline.effectDistance = new Vector2(1,-1);
        return image;
    }
    static Text Label(Transform parent,string name,string text,float x,float y,float w,float h,int size=20)
    {
        var label = Rect(parent,name,x,y,w,h).gameObject.AddComponent<Text>(); label.font=font; label.fontSize=size;
        label.text=text; label.color=new Color(.91f,.94f,.94f); label.raycastTarget=false; label.verticalOverflow=VerticalWrapMode.Truncate;
        return label;
    }
    static Image Picture(Transform parent,string name,float x,float y,float w,float h)
    {
        var image = Rect(parent,name,x,y,w,h).gameObject.AddComponent<Image>(); image.preserveAspect=true; image.raycastTarget=false; return image;
    }
    static Button Button(Transform parent,string name,string text,float x,float y,float w,float h,UnityAction action)
    {
        var image=Panel(parent,name,x,y,w,h); image.color=new Color(.055f,.18f,.23f);
        var button=image.gameObject.AddComponent<Button>(); button.targetGraphic=image;
        var colors=button.colors; colors.highlightedColor=new Color(.8f,.9f,1); colors.disabledColor=new Color(.35f,.35f,.35f,.6f); button.colors=colors;
        // Explicit game hotkeys handle Enter/Space; focused buttons must not fire the same action twice.
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        var label=Label(image.transform,"Label",text,8,4,w-16,h-8,20); label.alignment=TextAnchor.MiddleCenter;
        if(action!=null) UnityEventTools.AddPersistentListener(button.onClick,action);
        return button;
    }
    static Slider Slider(Transform parent,string name,float x,float y,float min,float max,UnityAction<float> action)
    {
        var root=Rect(parent,name,x,y,240,30); var slider=root.gameObject.AddComponent<Slider>(); slider.minValue=min; slider.maxValue=max;
        var rail=Panel(root,"Track",0,12,240,6); rail.color=new Color(.2f,.28f,.31f);
        var area=Rect(root,"Handle Area",8,0,224,30);
        var handle=Panel(area,"Handle",0,0,18,30); handle.color=Gold;
        var handleRect=(RectTransform)handle.transform;
        handleRect.anchorMin=new Vector2(0,0); handleRect.anchorMax=new Vector2(0,1);
        handleRect.pivot=new Vector2(.5f,.5f); handleRect.sizeDelta=new Vector2(18,0); handleRect.anchoredPosition=Vector2.zero;
        slider.handleRect=(RectTransform)handle.transform; slider.targetGraphic=handle;
        slider.navigation=new Navigation { mode=Navigation.Mode.None };
        UnityEventTools.AddPersistentListener(slider.onValueChanged,action); return slider;
    }
    static FortressActorHud ActorTemplate(Transform parent,bool health)
    {
        var root=Panel(parent,health?"Health Bar Template":"Turn Slot Template",0,0,health?100:112,health?36:64);
        var view=root.gameObject.AddComponent<FortressActorHud>();
        if(!health) view.portrait=Picture(root.transform,"Portrait",2,3,38,48);
        view.label=Label(root.transform,"Label",health?"120/120":"캐릭터",health?0:42,health?0:5,health?100:68,health?28:46,health?15:12);
        view.label.alignment=TextAnchor.MiddleCenter;
        var fill=Picture(root.transform,"Health",3,health?29:55,health?94:106,5); fill.color=new Color(.22f,.8f,.48f);
        // Image fill requires a sprite, even for a solid rectangle.
        fill.sprite=AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        fill.preserveAspect=false;
        fill.type=Image.Type.Filled; fill.fillMethod=Image.FillMethod.Horizontal; fill.fillOrigin=0; view.healthFill=fill;
        view.highlight=Picture(root.transform,"Current Turn",0,0,health?100:112,3); view.highlight.color=Gold;
        return view;
    }
    static FortressHud CreateHud(FortressGame game)
    {
        var canvasObject=new GameObject("Battle UI - Canvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
        var canvas=canvasObject.GetComponent<Canvas>(); canvas.renderMode=RenderMode.ScreenSpaceOverlay;
        var scaler=canvasObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution=new Vector2(1600,900); scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
        var root=Rect(canvasObject.transform,"16 by 9 Frame",0,0,1600,900);
        root.anchorMin=root.anchorMax=root.pivot=new Vector2(.5f,.5f); root.anchoredPosition=Vector2.zero;
        var hud=canvasObject.AddComponent<FortressHud>(); hud.game=game;
        hud.battlefield=Rect(root,"Battlefield Output",0,0,1600,900).gameObject.AddComponent<RawImage>(); hud.battlefield.raycastTarget=false;
        var battle=Rect(root,"Battle HUD",0,0,1600,900); hud.battlePanel=battle.gameObject;
        var top=Panel(battle,"Top Bar",16,14,1568,68);
        Label(top.transform,"Game Title","검은 달의 성채",18,6,330,40,27);
        hud.turnText=Label(top.transform,"Round","턴 1",365,15,150,40,24);
        hud.turnRoot=Rect(top.transform,"Turn Order",520,2,620,64);
        var turns=hud.turnRoot.gameObject.AddComponent<HorizontalLayoutGroup>(); turns.spacing=8; turns.childControlWidth=true; turns.childControlHeight=true; turns.childForceExpandWidth=true;
        hud.turnTemplate=ActorTemplate(hud.turnRoot,false);
        Button(top.transform,"Selection","캐릭터 선택",1200,10,180,46,game.OpenSelection);
        Button(top.transform,"Restart","다시 시작",1390,10,160,46,game.RequestRestart);
        var info=Panel(battle,"Player Status",16,102,285,132);
        hud.playerPortrait=Picture(info.transform,"Portrait",10,12,80,110);
        hud.playerStatus=Label(info.transform,"Stats","궁수\nHP 120 / 120\n이동 10 / 10 m",104,20,178,104,19);
        var enemy=Panel(battle,"Battle Objective",1335,102,249,150);
        Label(enemy.transform,"Title","전장 정보",18,12,215,35,24);
        hud.enemyStatus=Label(enemy.transform,"Enemy Count","모든 적 처치\n남은 적 4 / 4명",18,62,215,80,20);
        hud.healthRoot=Rect(battle,"World Health Bars",0,0,1600,900);
        hud.healthTemplate=ActorTemplate(hud.healthRoot,true);
        var bottom=Panel(battle,"Controls",16,749,1568,135);
        Label(bottom.transform,"Movement Help","이동 A/D · 점프 W\n내려가기 S · 턴 종료 Tab",18,22,320,90,20);
        hud.aimText=Label(bottom.transform,"Angle Label","각도 48°",350,13,240,35,22);
        hud.angleSlider=Slider(bottom.transform,"Angle",350,60,10,80,game.SetAngle);
        hud.powerText=Label(bottom.transform,"Power Label","위력 26",625,13,240,35,22);
        hud.powerSlider=Slider(bottom.transform,"Power",625,60,10,38,game.SetPower);
        var left=Button(bottom.transform,"Move Left","A",920,10,64,44,null);
        var hold=left.gameObject.AddComponent<FortressHoldButton>(); hold.game=game; hold.direction=-1;
        var right=Button(bottom.transform,"Move Right","D",996,10,64,44,null);
        hold=right.gameObject.AddComponent<FortressHoldButton>(); hold.game=game; hold.direction=1;
        hud.jumpButton=Button(bottom.transform,"Jump","점프",1072,10,90,44,game.RequestJump);
        hud.dropButton=Button(bottom.transform,"Drop","내려가기",920,70,242,44,game.RequestDrop);
        hud.fireButton=Button(bottom.transform,"Fire","화살 발사 [Space]",1200,10,350,46,game.RequestFire);
        hud.attackText=hud.fireButton.GetComponentInChildren<Text>();
        hud.endTurnButton=Button(bottom.transform,"End Turn","턴 넘기기 [Tab]",1200,70,350,48,game.RequestEndTurn);
        var message=Panel(battle,"Message",360,696,830,40);
        hud.messageText=Label(message.transform,"Text","이동하고 조준한 뒤 발사하세요.",15,5,800,32,19);
        var result=Panel(battle,"Battle Result",490,320,620,190); hud.resultPanel=result.gameObject;
        hud.resultText=Label(result.transform,"Result","전투 결과",30,22,560,60,25);
        Button(result.transform,"Retry","다시 도전",160,104,300,58,game.RequestRestart); result.gameObject.SetActive(false);
        var selection=Panel(root,"Character Selection",245,84,1110,716); hud.selectionPanel=selection.gameObject;
        Label(selection.transform,"Title","검은 달의 성채 · 출전 준비",35,20,1040,50,30);
        Label(selection.transform,"Subtitle","캐릭터 한 명을 선택하세요 · 1 / 2 또는 방향키",35,78,1040,38,20);
        hud.classCardRoot=Rect(selection.transform,"Class Cards",35,140,1040,440);
        var layout=hud.classCardRoot.gameObject.AddComponent<HorizontalLayoutGroup>(); layout.spacing=30;
        layout.childControlWidth=true; layout.childControlHeight=true; layout.childForceExpandWidth=true;
        var card=Panel(hud.classCardRoot,"Class Card Template",0,0,505,440);
        hud.classCardTemplate=card.gameObject.AddComponent<FortressClassCard>(); var cv=hud.classCardTemplate;
        cv.border=card; cv.title=Label(card.transform,"Class Name","궁수",25,20,450,45,27);
        cv.portrait=Picture(card.transform,"Portrait",15,88,238,270);
        cv.stats=Label(card.transform,"Stats","체력 120\n직격 피해 32\n범위 3.5 m\n이동 10 m / 턴",265,120,222,180,21);
        cv.description=Label(card.transform,"Description","직업 설명",25,370,450,50,20);
        cv.button=card.gameObject.AddComponent<Button>(); cv.button.targetGraphic=card;
        cv.button.navigation=new Navigation {mode=Navigation.Mode.None};
        Button(selection.transform,"Begin Battle","전투 시작 [Enter]",355,622,400,64,game.BeginBattle);
        return hud;
    }
}
