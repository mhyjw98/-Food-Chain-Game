using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpriteSorter : MonoBehaviour
{
    [SerializeField] private Transform back;
    [SerializeField] private Transform front;

    public int GetSortingOrder(GameObject obj)
    {
        if (back == null || front == null)
        {
            InitObject();
        }

        float objDist = Mathf.Abs(back.position.y - obj.transform.position.y);
        float totalDist = Mathf.Abs(back.position.y - front.position.y);

        return (int)(Mathf.Lerp(1, System.Int16.MaxValue, objDist / totalDist));
    }

    private void InitObject()
    {
        back = GameObject.Find("Back").transform;
        front = GameObject.Find("Front").transform;
    }
}
