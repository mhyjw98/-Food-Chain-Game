using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpriteSorter : MonoBehaviour
{
    [SerializeField] private Transform back;
    [SerializeField] private Transform front;
    [SerializeField] private int minOrder = 1;
    [SerializeField] private int maxOrder = 10000;

    public int GetSortingOrder(GameObject obj)
    {
        if (back == null || front == null)
        {
            InitObject();
        }

        if (back == null || front == null || obj == null)
            return minOrder;

        float objDist = Mathf.Abs(back.position.y - obj.transform.position.y);
        float totalDist = Mathf.Abs(back.position.y - front.position.y);

        // Back과 Front가 같은 높이면 나눗셈이 0이 된다
        if (totalDist <= Mathf.Epsilon)
            return minOrder;

        return (int)(Mathf.Lerp(minOrder, maxOrder, objDist / totalDist));
    }

    private void InitObject()
    {
        var backObj = GameObject.Find("Back");
        var frontObj = GameObject.Find("Front");

        if (backObj != null) back = backObj.transform;
        if (frontObj != null) front = frontObj.transform;
    }
}
