using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    private CharacterController controller;

    private Vector3 startPosition;
    private Quaternion startRotation;

    private bool hasItem = false;

    [Header("Movement Settings")]
    public float moveSpeed = 5f;

    [Header("References")]
    public Transform cameraTransform;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    private void Start()
    {
        startPosition = transform.position;
        startRotation = transform.rotation;
    }

    void Update()
    {
        HandleMovement();
    }

    void HandleMovement()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 input = new Vector3(horizontal, 0f, vertical);

        if (input.sqrMagnitude >= 0.1f)
        {
            Vector3 camForward = cameraTransform.forward;
            Vector3 camRight = cameraTransform.right;

            camForward.y = 0;
            camRight.y = 0;
            camForward.Normalize();
            camRight.Normalize();

            Vector3 move = (camForward * input.z + camRight * input.x).normalized;

            controller.Move(move * moveSpeed * Time.deltaTime);

            transform.forward = move;
        }
    }


    private void OnControllerColliderHit(ControllerColliderHit other)
    {
        switch (other.gameObject.tag)
        {
            case "Obj":
                HandleItemPickup(other.gameObject);
                break;
            case "Door":
                HandleDoorInteraction(other.gameObject);
                break;
            case "EndZone":
                HandleEscapeZone();
                break;
        }
    }

    void HandleItemPickup(GameObject obj)
    {
        GameManager.Instance.SetGameState(GameManager.GameState.HasItem);
        hasItem = true;
        obj.SetActive(false); // Destroy yerine
    }

    void HandleDoorInteraction(GameObject obj)
    {
        if (!hasItem)
        {
            GameManager.Instance.SetGameState(GameManager.GameState.Locked, true);
        }
        else
        {
            obj.SetActive(false); // Destroy yerine
        }
    }

    void HandleEscapeZone()
    {
        if (hasItem)
        {
            GameManager.Instance.SetGameState(GameManager.GameState.Win);
        }
    }

    public void ResetPlayer()
    {
        hasItem = false;

        // CharacterController ile pozisyon sýfýrlamak için önce disable/enable yap
        controller.enabled = false;
        transform.position = startPosition;
        transform.rotation = startRotation;
        controller.enabled = true;
    }
}
