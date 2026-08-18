using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "DashConfig", menuName = "EntityConfig/Dash Config")]
public class DashConfigSO : ScriptableObject
{
    [SerializeField] private float _peakSpeed = 10f;
    [SerializeField] private float _duration = 0.5f;
    [SerializeField] private AnimationCurve _speedCurve;
    [SerializeField] private float _inputThreshold = 0.1f;
    [SerializeField] private float _cooldown = 2f;

    public float PeakSpeed => _peakSpeed;
    public float Duration => _duration;
    public AnimationCurve SpeedCurve => _speedCurve;
    public float InputThreshold => _inputThreshold;
    public float Cooldown => _cooldown;

    // 在刚创建 SO 时，给 _speedCurve 赋一个默认值，避免在 Inspector 中为空
    private void Reset()
    {
        _speedCurve = new AnimationCurve(
            new Keyframe(0.00f, 1.00f),
            new Keyframe(0.15f, 1.10f),
            new Keyframe(0.55f, 0.75f),
            new Keyframe(1.00f, 0.00f));
    }
}
