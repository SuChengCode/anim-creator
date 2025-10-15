using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.UIElements;

namespace AnimCreator
{
    public class AnimCreator : EditorWindow
    {
        [SerializeField]
        private VisualTreeAsset m_VisualTreeAsset = default;

        private IntegerField _frameRateField;
        private Button _createBtn;
        private TextField _animNameField;
        private Toggle _loopToggle;

        [MenuItem("Window/AnimCreator")]
        public static void ShowExample()
        {
            AnimCreator wnd = GetWindow<AnimCreator>();
            wnd.titleContent = new GUIContent("AnimCreator");
        }

        public void CreateGUI()
        {
            // Each editor window contains a root VisualElement object
            VisualElement root = rootVisualElement;

            VisualElement uxmlInstance = m_VisualTreeAsset.Instantiate();
            root.Add(uxmlInstance);

            // 绑定控件到字段
            _frameRateField = uxmlInstance.Q<IntegerField>("frameRateField");
            _createBtn = uxmlInstance.Q<Button>("createBtn");
            _animNameField = uxmlInstance.Q<TextField>("animNameField");
            _loopToggle = uxmlInstance.Q<Toggle>("loopToggle");

            // 绑定事件
            _createBtn.clicked += OnCreateButtonClicked;
        }

        private void OnCreateButtonClicked()
        {
            int frameRate = _frameRateField.value;
            CreateAnimationFromSelection(frameRate);
        }

        private void CreateAnimationFromSelection(int frameRate)
        {
            // 获取当前选中的 Sprite 资源
            // 获取选中的对象
            var selectedObjects = Selection.objects;

            var allSprites = new List<Sprite>();

            foreach (var obj in selectedObjects)
            {
                if (obj is Sprite sprite)
                {
                    // 选中的是 Sprite，直接加入
                    allSprites.Add(sprite);
                }
                else if (obj is Texture2D tex)
                {
                    // 选中的是 PNG，获取切片 Sprite
                    string tPath = AssetDatabase.GetAssetPath(tex);
                    var sprites = AssetDatabase.LoadAllAssetsAtPath(tPath).OfType<Sprite>();
                    allSprites.AddRange(sprites);
                }
            }

            // 获取当前目录
            string path = "Assets";
            if (Selection.activeObject != null)
            {
                path = AssetDatabase.GetAssetPath(Selection.activeObject);
                if (!AssetDatabase.IsValidFolder(path))
                {
                    path = Path.GetDirectoryName(path);
                }
            }

            // 排序（按名称）
            allSprites.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));

            string animName = $"{_animNameField.value}";

            // === 创建 AnimationClip ===
            AnimationClip clip = new AnimationClip { frameRate = frameRate };

            var binding = new EditorCurveBinding
            {
                type = typeof(SpriteRenderer),
                path = "",
                propertyName = "m_Sprite"
            };

            var keyFrames = new ObjectReferenceKeyframe[allSprites.Count];
            for (int i = 0; i < allSprites.Count; i++)
            {
                keyFrames[i] = new ObjectReferenceKeyframe
                {
                    time = i / (float)frameRate,
                    value = allSprites[i]
                };
            }

            AnimationUtility.SetObjectReferenceCurve(clip, binding, keyFrames);
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            string clipPath = Path.Combine(path, animName + "Anim.anim");
            AssetDatabase.CreateAsset(clip, clipPath);

            // === 创建 AnimatorController ===
            string controllerPath = Path.Combine(path, animName + "Animator.controller");
            var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

            var state = controller.layers[0].stateMachine.AddState(animName);
            state.motion = clip;
            controller.layers[0].stateMachine.defaultState = state;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Animation 和 Animator 创建完成：\nAnimation: {clipPath}\nAnimator: {controllerPath}, 帧率{frameRate}，数量{allSprites.Count}");
        }

    }
}


