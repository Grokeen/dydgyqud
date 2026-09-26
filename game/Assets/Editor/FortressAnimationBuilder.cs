using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// These editable assets also ship in player builds; no UnityEditor code runs in-game.
[InitializeOnLoad]
public static class FortressAnimationBuilder
{
    public const string AssetFolder = "Assets/Resources/FortressAnimation";
    public const string ControllerPath = AssetFolder + "/Fighter.controller";
    const string BodyPath = "Body";
    const string WeaponPath = "AimPivot/WeaponMotion";

    static FortressAnimationBuilder()
    {
        EditorApplication.delayCall += EnsureAssets;
    }

    // May also be invoked with Unity's -executeMethod FortressAnimationBuilder.EnsureAssets.
    public static void EnsureAssets()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (EditorApplication.isCompiling)
        {
            EditorApplication.delayCall += EnsureAssets;
            return;
        }
        Build(false);
    }

    [MenuItem("Mini Fortress/Rebuild Character Animations")]
    public static void RebuildAssets()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Stop Play Mode before rebuilding character animation assets.");
            return;
        }
        Build(true);
    }

    static void Build(bool overwrite)
    {
        if (!AssetDatabase.IsValidFolder(AssetFolder))
            AssetDatabase.CreateFolder("Assets/Resources", "FortressAnimation");

        var clips = new Dictionary<string, AnimationClip>();
        Add(clips, "Idle", true, overwrite,
            P(0), P(.55f, sy: 1.025f, sx: .985f, wy: .025f), P(1.1f));
        Add(clips, "Move", true, overwrite,
            P(0, sx: 1.04f, sy: .95f, br: 4, wy: -.035f, wr: -3),
            P(.12f, by: .16f, sx: .97f, sy: 1.04f, br: -3, wy: .11f, wr: 4),
            P(.24f, sx: 1.04f, sy: .95f, br: 4, wy: -.035f, wr: -3),
            P(.36f, by: .12f, sx: .97f, sy: 1.04f, br: -3, wy: .08f, wr: 4),
            P(.48f, sx: 1.04f, sy: .95f, br: 4, wy: -.035f, wr: -3));
        Add(clips, "Jump", false, overwrite,
            P(0, sx: .91f, sy: 1.13f, br: -7, wy: .12f, wr: -8),
            P(.2f, sx: .97f, sy: 1.05f, br: -4, wy: .08f, wr: -5),
            P(.45f, sx: .98f, sy: 1.04f, br: -3, wy: .06f, wr: -4));
        Add(clips, "Fall", true, overwrite,
            P(0, sx: 1.04f, sy: .97f, br: 6, wy: -.1f, wr: 9),
            P(.25f, sx: 1.065f, sy: .95f, br: 8, wy: -.15f, wr: 11),
            P(.5f, sx: 1.04f, sy: .97f, br: 6, wy: -.1f, wr: 9));
        Add(clips, "Aim", true, overwrite,
            P(0, bx: -.025f, sx: 1.02f, sy: .975f, br: -2, wx: -.06f, wr: -1),
            P(.5f, bx: -.025f, sx: 1.015f, sy: .99f, br: -2, wx: -.08f, wy: .025f, wr: 1),
            P(1, bx: -.025f, sx: 1.02f, sy: .975f, br: -2, wx: -.06f, wr: -1));
        // Release keys match the gameplay windup: bow at .16s, spear at .22s.
        // The weapon returns to its neutral tip at release, matching the physical shot origin.
        Add(clips, "BowAttack", false, overwrite,
            P(0, bx: -.025f, br: -2, wx: -.06f),
            P(.10f, bx: -.09f, br: -5, wx: -.22f, wsx: .88f, wr: -5),
            P(.16f, bx: -.15f, sx: 1.045f, sy: .96f, br: -8),
            P(.23f, bx: -.07f, br: -3, wx: -.05f, wr: -4),
            P(.38f));
        Add(clips, "SpearAttack", false, overwrite,
            P(0, bx: -.04f, br: -3, wx: -.1f, wr: -5),
            P(.13f, bx: -.11f, br: -7, wx: -.35f, wy: .05f, wr: -14),
            P(.22f, bx: .25f, sx: .97f, sy: 1.03f, br: 12),
            P(.32f, bx: .11f, br: 5, wx: .2f, wr: 2),
            P(.44f));
        Add(clips, "Hit", false, overwrite,
            P(0, bx: -.11f, br: -7, color: new Color(1, .35f, .35f, 1)),
            P(.065f, bx: -.22f, sx: 1.1f, sy: .89f, br: -11, wx: -.13f, wr: -12,
                color: new Color(1, .35f, .35f, 1)),
            P(.15f, bx: .06f, sx: .97f, sy: 1.04f, br: 4, wx: .04f, wr: 5),
            P(.28f));
        Add(clips, "Death", false, overwrite,
            P(0),
            P(.16f, bx: -.13f, sx: 1.08f, sy: .86f, br: -18, wx: -.1f, wy: -.2f, wr: -18,
                color: new Color(.8f, .65f, .65f, 1)),
            P(.5f, bx: -.35f, by: .08f, sx: 1.05f, sy: .8f, br: -78,
                wx: -.25f, wy: -1, wr: -60, wsx: .65f, wsy: .65f,
                color: new Color(.55f, .5f, .5f, .75f)),
            P(.8f, bx: -.4f, by: .06f, sx: 1.05f, sy: .8f, br: -84,
                wx: -.25f, wy: -1.1f, wr: -70, wsx: 0, wsy: 0,
                color: new Color(.55f, .5f, .5f, 0)));

        if (overwrite) AssetDatabase.DeleteAsset(ControllerPath);
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) == null)
            CreateController(clips);
        AssetDatabase.SaveAssets();
    }

    static void CreateController(Dictionary<string, AnimationClip> clips)
    {
        var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("Grounded", AnimatorControllerParameterType.Bool);
        controller.AddParameter("VerticalSpeed", AnimatorControllerParameterType.Float);
        controller.AddParameter("Aiming", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Dead", AnimatorControllerParameterType.Bool);
        controller.AddParameter("BowAttack", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("SpearAttack", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Hit", AnimatorControllerParameterType.Trigger);

        var machine = controller.layers[0].stateMachine;
        machine.anyStatePosition = new Vector3(20, -100, 0);
        machine.entryPosition = new Vector3(20, 0, 0);
        var states = new Dictionary<string, AnimatorState>();
        int index = 0;
        foreach (var pair in clips)
        {
            var state = machine.AddState(pair.Key, new Vector3(260 + index % 3 * 250, index / 3 * 120, 0));
            state.motion = pair.Value;
            state.writeDefaultValues = false;
            states.Add(pair.Key, state);
            index++;
        }
        machine.defaultState = states["Idle"];

        // Any State transitions are ordered by priority. Death is terminal, and every
        // other transition checks Dead so neither queued triggers nor locomotion revive it.
        var death = Configure(machine.AddAnyStateTransition(states["Death"]), .035f);
        death.AddCondition(AnimatorConditionMode.If, 0, "Dead");
        death.interruptionSource = TransitionInterruptionSource.None;
        foreach (string name in new[] { "Hit", "BowAttack", "SpearAttack" })
        {
            var trigger = Configure(machine.AddAnyStateTransition(states[name]), .025f);
            trigger.AddCondition(AnimatorConditionMode.IfNot, 0, "Dead");
            trigger.AddCondition(AnimatorConditionMode.If, 0, name);
            var recover = Configure(states[name].AddTransition(states["Idle"]), .045f);
            recover.hasExitTime = true;
            recover.exitTime = 1;
            recover.AddCondition(AnimatorConditionMode.IfNot, 0, "Dead");
        }

        foreach (string from in new[] { "Idle", "Move", "Aim", "Jump", "Fall" })
        {
            if (from != "Jump")
            {
                var jump = Link(states, from, "Jump");
                jump.AddCondition(AnimatorConditionMode.IfNot, 0, "Grounded");
                jump.AddCondition(AnimatorConditionMode.Greater, .05f, "VerticalSpeed");
            }
            if (from != "Fall")
            {
                var fall = Link(states, from, "Fall");
                fall.AddCondition(AnimatorConditionMode.IfNot, 0, "Grounded");
                fall.AddCondition(AnimatorConditionMode.Less, .05f, "VerticalSpeed");
            }
            if (from != "Move")
            {
                var move = Link(states, from, "Move");
                move.AddCondition(AnimatorConditionMode.If, 0, "Grounded");
                move.AddCondition(AnimatorConditionMode.Greater, .08f, "Speed");
            }
            if (from != "Aim")
            {
                var aim = Link(states, from, "Aim");
                aim.AddCondition(AnimatorConditionMode.If, 0, "Grounded");
                aim.AddCondition(AnimatorConditionMode.Less, .08f, "Speed");
                aim.AddCondition(AnimatorConditionMode.If, 0, "Aiming");
            }
            if (from != "Idle")
            {
                var idle = Link(states, from, "Idle");
                idle.AddCondition(AnimatorConditionMode.If, 0, "Grounded");
                idle.AddCondition(AnimatorConditionMode.Less, .08f, "Speed");
                idle.AddCondition(AnimatorConditionMode.IfNot, 0, "Aiming");
            }
        }
        EditorUtility.SetDirty(controller);
    }

    static AnimatorStateTransition Link(Dictionary<string, AnimatorState> states, string from, string to)
    {
        var transition = Configure(states[from].AddTransition(states[to]), .06f);
        transition.AddCondition(AnimatorConditionMode.IfNot, 0, "Dead");
        return transition;
    }

    static AnimatorStateTransition Configure(AnimatorStateTransition transition, float duration)
    {
        transition.hasExitTime = false;
        transition.hasFixedDuration = true;
        transition.duration = duration;
        transition.canTransitionToSelf = false;
        transition.interruptionSource = TransitionInterruptionSource.SourceThenDestination;
        transition.orderedInterruption = true;
        return transition;
    }

    struct Pose
    {
        public float time;
        public Vector3 bodyPosition, bodyScale, bodyRotation;
        public Vector3 weaponPosition, weaponScale, weaponRotation;
        public Color color;
    }

    static Pose P(float time, float bx = 0, float by = 0, float sx = 1, float sy = 1,
        float br = 0, float wx = 0, float wy = 0, float wsx = 1, float wsy = 1,
        float wr = 0, Color? color = null)
    {
        return new Pose
        {
            time = time, bodyPosition = new Vector3(bx, by, 0), bodyScale = new Vector3(sx, sy, 1),
            bodyRotation = new Vector3(0, 0, br), weaponPosition = new Vector3(wx, wy, 0),
            weaponScale = new Vector3(wsx, wsy, 1), weaponRotation = new Vector3(0, 0, wr),
            color = color ?? Color.white
        };
    }

    static void Add(Dictionary<string, AnimationClip> clips, string name, bool loop, bool overwrite, params Pose[] poses)
    {
        string path = AssetFolder + "/" + name + ".anim";
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip != null && !overwrite) { clips.Add(name, clip); return; }
        bool isNew = clip == null;
        if (isNew) clip = new AnimationClip();
        else clip.ClearCurves();
        clip.name = name;
        clip.frameRate = 60;
        clip.legacy = false;
        clip.wrapMode = loop ? WrapMode.Loop : WrapMode.ClampForever;

        // Every clip binds the same full pose, including alpha and unchanging axes,
        // so hit tint, death fade, weapon recoil and nonuniform scales cannot leak.
        VectorCurves(clip, BodyPath, "m_LocalPosition", poses, p => p.bodyPosition);
        VectorCurves(clip, BodyPath, "m_LocalScale", poses, p => p.bodyScale);
        VectorCurves(clip, BodyPath, "localEulerAnglesRaw", poses, p => p.bodyRotation);
        VectorCurves(clip, WeaponPath, "m_LocalPosition", poses, p => p.weaponPosition);
        VectorCurves(clip, WeaponPath, "m_LocalScale", poses, p => p.weaponScale);
        VectorCurves(clip, WeaponPath, "localEulerAnglesRaw", poses, p => p.weaponRotation);
        Curve(clip, BodyPath, typeof(SpriteRenderer), "m_Color.r", poses, p => p.color.r);
        Curve(clip, BodyPath, typeof(SpriteRenderer), "m_Color.g", poses, p => p.color.g);
        Curve(clip, BodyPath, typeof(SpriteRenderer), "m_Color.b", poses, p => p.color.b);
        Curve(clip, BodyPath, typeof(SpriteRenderer), "m_Color.a", poses, p => p.color.a);

        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        settings.loopBlend = false;
        settings.startTime = 0;
        settings.stopTime = poses[poses.Length - 1].time;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        if (isNew) AssetDatabase.CreateAsset(clip, path);
        else EditorUtility.SetDirty(clip);
        clips.Add(name, clip);
    }

    static void VectorCurves(AnimationClip clip, string path, string property, Pose[] poses, Func<Pose, Vector3> read)
    {
        Curve(clip, path, typeof(Transform), property + ".x", poses, p => read(p).x);
        Curve(clip, path, typeof(Transform), property + ".y", poses, p => read(p).y);
        Curve(clip, path, typeof(Transform), property + ".z", poses, p => read(p).z);
    }

    static void Curve(AnimationClip clip, string path, Type type, string property, Pose[] poses, Func<Pose, float> read)
    {
        var keys = new Keyframe[poses.Length];
        for (int i = 0; i < poses.Length; i++) keys[i] = new Keyframe(poses[i].time, read(poses[i]));
        var curve = new AnimationCurve(keys);
        for (int i = 0; i < keys.Length; i++)
        {
            AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto);
            AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto);
        }
        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, type, property), curve);
    }
}
