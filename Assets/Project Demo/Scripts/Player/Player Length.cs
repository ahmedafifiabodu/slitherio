using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerLength : NetworkBehaviour
{
    [SerializeField] private GameObject _tailPrefab;

    public NetworkVariable<ushort> length = new(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private List<GameObject> _tails;
    private Collider2D _collider;

    public static event System.Action<ushort> OnLengthChanged;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        _tails = new List<GameObject>();
        _collider = GetComponent<Collider2D>();

        if (!IsServer)
        {
            length.OnValueChanged += OnLengthChangedEvent;
        }

        // Instantiate tails based on the current length
        for (int i = 1; i < length.Value; i++)
        {
            InstantiateTail();
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        DestroyTails();
    }

    private void DestroyTails()
    {
        while (_tails.Count != 0)
        {
            GameObject tail = _tails[0];
            _tails.RemoveAt(0);
            Destroy(tail);
        }

        _tails.Clear();
    }

    public void AddLength()
    {
        length.Value++;
        LengthChanged();
    }

    private void OnLengthChangedEvent(ushort previousValue, ushort newValue) => LengthChanged();

    private void LengthChanged()
    {
        InstantiateTail();

        if (!IsOwner) return;
        OnLengthChanged?.Invoke(length.Value);
        AudioManager.Instance.PlaySFX("Snake Eat");
    }

    private void InstantiateTail()
    {
        GameObject tail = Instantiate(_tailPrefab, transform.position, Quaternion.identity);

        tail.GetComponent<SpriteRenderer>().sortingOrder = -length.Value;

        if (tail.TryGetComponent(out Tail tailComponent))
        {
            tailComponent._networkOwner = transform;
            tailComponent._followTarget = _tails.Count == 0 ? transform : _tails[^1].transform;
            Physics2D.IgnoreCollision(_collider, tail.GetComponent<Collider2D>());
        }

        _tails.Add(tail);
    }
}