using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyDefinition", menuName = "Enemies/Enemy Definition")]
public class EnemyDefinition : ScriptableObject
{
    [Header("Identity")]
    public string displayName;

    [Header("Visuals")]
    public List<GameObject> prefabVariants;

    [Header("Size")]
    public float minSize = 0.8f;
    public float maxSize = 1.2f;

    [Header("Speed")]
    public float minSpeed = 3f;
    public float maxSpeed = 6f;

    [Range(0f, 1f)]
    public float sizePenalty = 0f;

    [Header("Spawn Weight")]
    public float spawnWeight = 1f;

    [Header("Weight")]
    public float weight = 1f;

    [Header("Health")]
    public float baseHealth = 100f;

    [Header("Attack")]
    public float baseDamage = 10f;
    public float baseAttackRate = 1f;
    public float baseAttackRange = 2f;

    public float RollSize() => Random.Range(minSize, maxSize);

    public float DeriveSpeed(float size)
    {
        float baseSpeed = Random.Range(minSpeed, maxSpeed);
        float t = Mathf.InverseLerp(minSize, maxSize, size);
        float penalty = Mathf.Lerp(0f, 1f, t * sizePenalty);
        return Mathf.Lerp(baseSpeed, minSpeed, penalty);
    }
}