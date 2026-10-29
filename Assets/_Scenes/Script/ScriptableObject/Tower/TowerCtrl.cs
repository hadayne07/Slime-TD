using UnityEngine;

public class TowerCtrl : MonoBehaviour
{
    private TowerShoot towerShoot;

    [SerializeField] private float timeDelay;

    private void Awake()
    {
        towerShoot = GetComponent<TowerShoot>();
    }

    private void Update()
    {
        if (towerShoot == null || towerShoot.Data == null)
            return;

        // Không xử lý phát bắn khi game đang pause.
        if (Time.deltaTime <= 0f)
            return;

        float interval = Mathf.Max(
            0.01f,
            towerShoot.Data.TimeShoot
        );

        timeDelay += Time.deltaTime;

        // Có thể xử lý nhiều lượt bắn đến hạn trong một frame.
        while (timeDelay >= interval)
        {
            // Giữ lại phần thời gian dư.
            timeDelay -= interval;

            towerShoot.Shoot();
        }
    }
}