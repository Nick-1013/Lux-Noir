using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public class DetectionZone : MonoBehaviour
{
    public List<Collider2D> detectedColliders = new();

    private Collider2D col;
    private Enemy enemy;

    void Awake()
    {
        col = GetComponent<Collider2D>();
        enemy = GetComponentInParent<Enemy>(); // Find enemy automatically

        if (col == null)
        {
            Debug.LogError("DetectionZone requires a Collider2D component.");
        }
        else if (!col.isTrigger)
        {
            Debug.LogWarning("DetectionZone collider should be set as a trigger. Setting it to trigger now.");
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        detectedColliders.Add(collision);
        Debug.Log("Player ENTERED attack zone");

        // Tell enemy it can attack
        if (enemy != null)
        {
            enemy.SetAttackState(true);
        }
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        detectedColliders.Remove(collision);
        Debug.Log("Player EXITED attack zone");

        // If no targets remain, stop attacking
        if (enemy != null && detectedColliders.Count == 0)
        {
            enemy.SetAttackState(false);
        }
        Debug.Log("Colliders in zone: " + detectedColliders.Count);
    }
}