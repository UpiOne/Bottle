using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowObject : MonoBehaviour
{
    [Header("Target Settings")]
    [Tooltip("Объект, за которым нужно следовать")]
    public Transform target; 

    [Header("Follow Settings")]
    [Tooltip("Следовать по оси X")]
    public bool followX = true;

    [Tooltip("Следовать по оси Y")]
    public bool followY = true;

    [Tooltip("Следовать по оси Z (для 2D обычно не нужно)")]
    public bool followZ = false;

    [Header("Rotation Settings")]
    [Tooltip("Повторять поворот цели")]
    public bool copyRotation = true;

    void Update()
    {
        if (target == null) return;

        Vector3 newPosition = transform.position;

        if (followX) newPosition.x = target.position.x;
        if (followY) newPosition.y = target.position.y;
        if (followZ) newPosition.z = target.position.z;

        transform.position = newPosition;

        if (copyRotation)
        {
            transform.rotation = target.rotation;
        }
    }
}
