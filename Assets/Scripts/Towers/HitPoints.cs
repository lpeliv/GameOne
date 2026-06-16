using System;
using System.Collections;
using UnityEngine;

public class HitPoint : MonoBehaviour, IHammerHittable
{
    [SerializeField] private float driveDepth = 0.7f;
    [SerializeField] private float driveSpeed = 8f;
    [SerializeField] private float hideDelay = 0.3f;
    
    private Vector3 driveDirection;
    private bool isHit;
    private int hitPointIndex;
    private Vector3 restPosition;

    public event Action<HitPoint> OnHitPointStruck;

    public bool IsHit => isHit;
    public int HitPointIndex => hitPointIndex;

    public void Initialize(int index)
    {
        hitPointIndex = index;
        isHit = false;
        restPosition = transform.localPosition;
    }

    public void OnHammerHit(float hammerStrength)
    {
        if (isHit) return;
        isHit = true;

        OnHitPointStruck?.Invoke(this);
        StartCoroutine(DriveIn());
    }

    public void Initialize(int index, Vector3 driveDir)
    {
        hitPointIndex = index;
        isHit = false;
        restPosition = transform.position;
        driveDirection = driveDir;
    }

    private IEnumerator DriveIn()
    {
        Vector3 startPos = transform.position;
        Vector3 targetPos = startPos + driveDirection * driveDepth;
        float timer = 0f;

        while (timer < 1f)
        {
            timer += Time.deltaTime * driveSpeed;
            transform.position = Vector3.Lerp(startPos, targetPos, timer);
            yield return null;
        }

        transform.position = targetPos;
        yield return new WaitForSeconds(hideDelay);
        gameObject.SetActive(false);
    }
}