using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IgnoreColision : MonoBehaviour
{
    [Header("��������� �������������")]
    [Tooltip("������, � ������� ����� ������������ ��������")]
    public GameObject targetObject;

    [Tooltip("������������ �������� ��� ������")]
    public bool ignoreOnStart = true;

    private Collider2D myCollider;
    private Collider2D targetCollider;

    void Start()
    {
        myCollider = GetComponent<Collider2D>();

        if (targetObject != null)
        {
            targetCollider = targetObject.GetComponent<Collider2D>();
        }

        if (myCollider == null || targetCollider == null)
        {
            Debug.LogError("����������� ���������� �� ��������!");
            return;
        }

        if (ignoreOnStart)
        {
            IgnoreCollisionsWithTarget(true);
        }

        if (GetComponent<Rigidbody2D>() == null)
        {
            Debug.LogWarning("Rigidbody2D �����������. ��� ���������� ������ ��������� �������� Rigidbody2D � �������.");
        }
    }

    public void IgnoreCollisionsWithTarget(bool ignore)
    {
        Physics2D.IgnoreCollision(myCollider, targetCollider, ignore);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ignoreOnStart = !ignoreOnStart;
            IgnoreCollisionsWithTarget(ignoreOnStart);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("InvisWater"))
        {
            GameObject waterObject = FindObjectWithLayer("WaterSpawn");

            if (waterObject != null)
            {
                gameObject.SetActive(false);

                transform.position = waterObject.transform.position;

                gameObject.SetActive(true);
            }
        }
    }

    private GameObject FindObjectWithLayer(string layerName)
    {
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (obj.layer == LayerMask.NameToLayer(layerName))
            {
                return obj;
            }
        }
        return null;
    }
}