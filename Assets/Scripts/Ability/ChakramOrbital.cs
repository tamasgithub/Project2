
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Mirror;
using UnityEngine;


public class ChakramOrbital : NetworkBehaviour
{
    private bool detached;
    private Entity _owner;
    private List<Transform> chakrams = new();
    private int detachCount = 0;
    private int returnCount = 0;

    void Start()
    {
        foreach (Transform child in transform)
        {
            chakrams.Add(child);
        }
        SetupVisual();
    }

    [Client]
    private void SetupVisual()
    {
        foreach (Transform child in transform)
        {
            child.Find("Collider").GetComponent<CollisionForwarder>().OnTriggerEnter += OnHit;
            child.Find("Collider").GetComponent<DamageSource>().Load(_owner);
            child.Find("Visual")?.DOLocalRotate(Vector3.forward * 360, 0.7f, RotateMode.FastBeyond360).SetLoops(-1, LoopType.Incremental).SetEase(Ease.Linear);
        }
    }

    private void OnHit(Collider2D collision)
    {
        if (collision.tag != "Enemy") return;
        var enemy = collision.GetComponent<Enemy>();
        enemy.ReceiveDamage(3);
        if (returnCount >= detachCount)
        {
            returnCount = 0;
            detachCount = 0;
            DetachChakrams(enemy.transform);
            detached = true;
            return;
        }
    }

    private void DetachChakrams(Transform target)
    {
        detachCount = 0;
        foreach (var chakram in chakrams)
        {
            var localStartPos = chakram.localPosition;
            var direction = (chakram.position - target.position)*2;
            var distance = direction.magnitude;
            var delay = Random.Range(0.1f, 0.2f);
            var sequence = DOTween.Sequence();
            // sequence.AppendInterval(0.02f * detachCount);
            sequence.Append(chakram.DOMove(target.position - direction * 2, 0.2f).SetEase(Ease.OutQuad));
            sequence.AppendInterval(3f +detachCount * 0.1f);
            sequence.AppendCallback(() => chakram.SetParent(transform, true));
            sequence.Append(chakram.DOLocalMove(localStartPos, 0.4f).SetEase(Ease.OutSine));
            sequence.AppendCallback(() => AttachChakram(chakram));
            chakram.SetParent(null, true);
            sequence.Play();
           
            detachCount++;
        }
    }
    public void AttachChakram(Transform chakram)
    {
        detached = false;
        returnCount++;
    }
    public void Refresh(int level, Entity owner)
    {
        _owner = owner;
        // var angle = 360f / level;
        // for (int i = 0; i < level; i++)
        // {
        //     transform.GetChild(i).gameObject.SetActive(true);
        //     transform.GetChild(i).eulerAngles = Vector3.forward * angle * i;
        // }
    }

}