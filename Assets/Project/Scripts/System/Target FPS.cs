using UnityEngine;

public class TargetFPS : MonoBehaviour
{
    [SerializeField] private int _targetFPS = 60;

    private void Awake()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = _targetFPS;
    }
}