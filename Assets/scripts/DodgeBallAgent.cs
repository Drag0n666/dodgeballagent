using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class DodgeBallAgent : Agent
{
    [Header("移动设置")]
    public float moveSpeed = 30f;

    [Header("场景引用")]
    public Transform spawner;
    public GameObject ballPrefab;

    private Rigidbody agentRb;
    private float spawnTimer = 0f;
    private bool wasInDangerLastFrame = false;
    private float spawnInterval = 3f;

    public override void OnEpisodeBegin()
    {
        transform.position = new Vector3(0f, 0.5f, 0f);
        agentRb.velocity = Vector3.zero;
        agentRb.angularVelocity = Vector3.zero;
        wasInDangerLastFrame = false;
    }

    public override void Initialize()
    {
        agentRb = GetComponent<Rigidbody>();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
       
        // 1. 自身位置（2个观察值）
        sensor.AddObservation(transform.position.x);
        sensor.AddObservation(transform.position.z);

        // 2. 找所有球，按距离排序（先处理最近的球，因为它们最危险）
        GameObject[] balls = GameObject.FindGameObjectsWithTag("Ball");
        System.Array.Sort(balls, (a, b) =>
        Vector3.Distance(transform.position, a.transform.position)
        .CompareTo(Vector3.Distance(transform.position, b.transform.position)));

        // 3. 只取最近的3个球，每个球加4个观察值（位置x、z + 速度x、z）
        for (int i = 0; i < Mathf.Min(3, balls.Length); i++)
        {
            Rigidbody rb = balls[i].GetComponent<Rigidbody>();
            // 球的位置（忽略y轴，因为球和Agent都在地面高度，y固定）
            sensor.AddObservation(balls[i].transform.position.x);
            sensor.AddObservation(balls[i].transform.position.z);
            // 球的速度方向（关键！告诉AI球往哪飞）
            sensor.AddObservation(rb.velocity.x);
            sensor.AddObservation(rb.velocity.z);
        }

        // 4. 如果球不够3个，用0补齐（保证总观察值数量固定）
        for (int i = balls.Length; i < 3; i++)
        {
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float moveX = actions.ContinuousActions[0];
        float moveZ = actions.ContinuousActions[1];
        agentRb.velocity = new Vector3(moveX * moveSpeed, 0f, moveZ * moveSpeed);

        // ====== 存活奖励 ======
        AddReward(0.03f);

        // ====== 速度奖励 ======
        float speed = agentRb.velocity.magnitude;
        if (speed > 0.5f)
            AddReward(0.01f);

        // ====== 中心区域奖励（新增）======
        float distToCenter = Vector3.Distance(transform.position, Vector3.zero);
        if (distToCenter < 5f)
        {
            // 越靠近中心奖励越高
            AddReward(0.02f * (5f - distToCenter));
        }
        else if (distToCenter > 7f)
        {
            // 太远了惩罚
            AddReward(-0.02f * (distToCenter - 7f));
        }

        // ====== 遍历所有球，给躲避奖励 ======
        GameObject[] balls = GameObject.FindGameObjectsWithTag("Ball");
        float minDist = float.MaxValue;

        foreach (var ball in balls)
        {
            float dist = Vector3.Distance(transform.position, ball.transform.position);
            if (dist < minDist) minDist = dist;

            // ====== 垂直方向奖励（向两侧闪避）======
            Vector3 dirToBall = (ball.transform.position - transform.position).normalized;
            Vector3 agentVel = speed > 0.01f ? agentRb.velocity.normalized : Vector3.zero;

            // 计算垂直方向（球的左右两边）
            Vector3 perpendicularDir = Vector3.Cross(dirToBall, Vector3.up).normalized;

            // 垂直移动程度（0=前后，1=完全左右）
            float sideMovement = Mathf.Abs(Vector3.Dot(perpendicularDir, agentVel));

            if (sideMovement > 0.3f)
                AddReward(sideMovement * 0.12f);

            // ====== 靠近球惩罚 ======
            if (dist < 3f)
                AddReward(-0.08f * (3f - dist));
        }

        // ====== 边界检测 ======
        if (transform.position.x > 9f || transform.position.x < -9f ||
        transform.position.z > 9f || transform.position.z < -9f)
        {
            AddReward(-5f);
            EndEpisode();
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var ca = actionsOut.ContinuousActions;
        ca[0] = Input.GetAxisRaw("Horizontal");
        ca[1] = Input.GetAxisRaw("Vertical");
    }

    private void Update()
    {
        spawnTimer += Time.deltaTime;
        if (spawnTimer >= spawnInterval)
        {
            SpawnBall();
            spawnTimer = 0f;
        }
    }

    private void SpawnBall()
    {
        if (ballPrefab != null && spawner != null)
        {
            float speed = 3f;
            float randomOffsetX = Random.Range(-5f, 5f);

            GameObject ball = Instantiate(ballPrefab, spawner.position, Quaternion.identity);
            Rigidbody ballRb = ball.GetComponent<Rigidbody>();

            Vector3 targetPos = transform.position + new Vector3(randomOffsetX, 0f, 0f);
            Vector3 direction = (targetPos - spawner.position).normalized;

            ballRb.velocity = direction * speed;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ball"))
        {
            AddReward(-5f);
            Destroy(collision.gameObject);
            EndEpisode();
        }

        if (collision.gameObject.name.Contains("Wall"))
        {
            AddReward(-0.05f);
        }
    }
}
