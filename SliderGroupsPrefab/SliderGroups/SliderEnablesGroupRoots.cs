using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

public class SliderEnablesGroupRoots : UdonSharpBehaviour
{
    [Header("UI")]
    [Tooltip("Whole Numbers ON. We recommend Min=0, Max will be set to group count automatically.")]
    public Slider slider;

    [Header("Groups (add as many roots as you want)")]
    [Tooltip("Each element is a parent/root. All children under it are treated as that group's objects.")]
    public Transform[] groupRoots;

    [Header("Options")]
    [Tooltip("If true, includes inactive children when collecting objects.")]
    public bool includeInactiveChildren = true;

    [Tooltip("If true, the script will set slider.maxValue = groupRoots.Length at Start.")]
    public bool autoSetSliderMax = true;

    private GameObject[][] groupObjects; // runtime-only, not assigned in inspector
    private int lastLevel = int.MinValue;

    private void Start()
    {
        BuildGroupCaches();

        if (slider != null && autoSetSliderMax)
        {
            slider.wholeNumbers = true;
            slider.minValue = 0f;
            slider.maxValue = (groupObjects != null) ? groupObjects.Length : 0f;
        }

        ApplyFromSlider();
    }

    // Hook to Slider -> OnValueChanged (Dynamic Float)
    public void OnSliderChanged()
    {
        ApplyFromSlider();
    }

    private void BuildGroupCaches()
    {
        int count = (groupRoots != null) ? groupRoots.Length : 0;
        groupObjects = new GameObject[count][];

        for (int i = 0; i < count; i++)
        {
            groupObjects[i] = CollectChildrenAsGameObjects(groupRoots[i], includeInactiveChildren);
        }
    }

    private void ApplyFromSlider()
    {
        if (slider == null || groupObjects == null) return;

        int level = Mathf.RoundToInt(slider.value);

        // Clamp to valid range: 0..groupCount
        int groupCount = groupObjects.Length;
        if (level < 0) level = 0;
        if (level > groupCount) level = groupCount;

        if (level == lastLevel) return;
        lastLevel = level;

        // Rule:
        // level = 0 => none enabled
        // level = 1 => group 0 enabled
        // level = 2 => group 0..1 enabled
        // ...
        // level = groupCount => all enabled
        for (int groupIndex = 0; groupIndex < groupCount; groupIndex++)
        {
            bool shouldEnable = (groupIndex < level);
            SetActiveAll(groupObjects[groupIndex], shouldEnable);
        }
    }

    private void SetActiveAll(GameObject[] objects, bool active)
    {
        if (objects == null) return;

        for (int i = 0; i < objects.Length; i++)
        {
            GameObject obj = objects[i];
            if (obj == null) continue;

            if (obj.activeSelf != active)
                obj.SetActive(active);
        }
    }

    private GameObject[] CollectChildrenAsGameObjects(Transform root, bool includeInactive)
    {
        if (root == null) return new GameObject[0];

        Transform[] transforms = root.GetComponentsInChildren<Transform>(includeInactive);

        // Count children excluding the root itself
        int count = 0;
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i] == root) continue;
            count++;
        }

        GameObject[] result = new GameObject[count];
        int writeIndex = 0;

        for (int i = 0; i < transforms.Length; i++)
        {
            Transform t = transforms[i];
            if (t == root) continue;

            result[writeIndex] = t.gameObject;
            writeIndex++;
        }

        return result;
    }
}