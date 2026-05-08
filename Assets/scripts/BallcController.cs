using UnityEngine;

public class BallController : MonoBehaviour
{
    void Start()
    {
        // 10秒后自动销毁
        Destroy(gameObject, 10f);
    }
}
