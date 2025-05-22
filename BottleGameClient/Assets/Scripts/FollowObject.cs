using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowObject : MonoBehaviour
{
    [Header("Target Settings")]
    [Tooltip("������, �� ������� ����� ���������")]
    public Transform target; 

    [Header("Follow Settings")]
    [Tooltip("��������� �� ��� X")]
    public bool followX = true;

    [Tooltip("��������� �� ��� Y")]
    public bool followY = true;

    [Tooltip("��������� �� ��� Z (��� 2D ������ �� �����)")]
    public bool followZ = false;

    [Header("Rotation Settings")]
    [Tooltip("��������� ������� ����")]
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
