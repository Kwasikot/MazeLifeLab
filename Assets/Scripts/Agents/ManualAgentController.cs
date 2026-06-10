using UnityEngine;

public class ManualAgentController : MonoBehaviour
{
    [SerializeField] float moveSpeed = 10f;

    public bool ControlEnabled { get; set; } = true;

    void Update()
    {
        if (!ControlEnabled)
            return;

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        if (horizontal == 0f && vertical == 0f)
            return;

        Vector3 move = new Vector3(horizontal, 0f, vertical).normalized * moveSpeed * Time.deltaTime;
        transform.position += move;
    }
}
