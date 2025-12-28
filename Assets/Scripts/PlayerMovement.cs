using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    private CharacterController controller;
    private bool hasItem = false;

    [Header("Movement Settings")]
    public float moveSpeed = 5f;

    [Header("References")]
    public Transform cameraTransform;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        HandleMovement();
    }

    void HandleMovement()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 input = new Vector3(horizontal, 0f, vertical).normalized;

        if (input.magnitude >= 0.1f)
        {
            // Kameranýn yönüne göre düzeltme yap
            Vector3 camForward = cameraTransform.forward;
            Vector3 camRight = cameraTransform.right;

            // Y yönünü yok say (dikey bakýþý etkisiz yap)
            camForward.y = 0;
            camRight.y = 0;
            camForward.Normalize();
            camRight.Normalize();

            // Kamera yönüne göre hareket vektörü
            Vector3 move = camForward * vertical + camRight * horizontal;

            controller.Move(move * moveSpeed * Time.deltaTime);
            // Karakter hareket yönüne baksýn (isteðe baðlý)
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
        Destroy(obj);
    }

    void HandleDoorInteraction(GameObject obj)
    {
        if (!hasItem)
        {
            GameManager.Instance.SetGameState(GameManager.GameState.Locked, true);
        }
        else
        {
            Destroy(obj);
        }
    }

    void HandleEscapeZone()
    {
        if (hasItem)
        {
            GameManager.Instance.SetGameState(GameManager.GameState.Win);
        }
    }

}


