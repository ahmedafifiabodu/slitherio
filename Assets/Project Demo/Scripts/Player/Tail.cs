using UnityEngine;

public class Tail : MonoBehaviour
{
    [SerializeField] private float _delay = 0.1f;
    [SerializeField] private float _distance = 0.3f;
    [SerializeField] private float _moveStep = 10f;

    private Vector3 _targetPoisition;

    internal Transform _networkOwner;
    internal Transform _followTarget;

    private void Update()
    {
        if (_followTarget != null)
        {
            _targetPoisition = _followTarget.position - _followTarget.forward * _distance;
            _targetPoisition += (transform.position - _targetPoisition) * _delay;
            _targetPoisition.z = 0;

            transform.position = Vector3.Lerp(transform.position, _targetPoisition, Time.deltaTime * _moveStep);
        }
    }
}