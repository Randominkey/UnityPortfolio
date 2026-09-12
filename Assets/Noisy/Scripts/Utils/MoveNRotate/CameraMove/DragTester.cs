using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class DragTester : MonoBehaviour, IDragHandler
{
    public float rotateSpeed = 10;

    public void OnDrag(PointerEventData eventData)
    {
        float x = eventData.delta.x * Time.deltaTime * rotateSpeed;
        float y = eventData.delta.y * Time.deltaTime * rotateSpeed;

        transform.Rotate(0, -x, y, Space.World);

        Debug.Log("µå·¡±×");
    }
}
