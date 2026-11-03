using UnityEngine;

// Oddiy birinchi shaxs boshqaruvi: WASD, Shift — yugurish, sichqoncha — qarash.
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public static float Sensitivity = 1.5f;

    public Transform cameraPivot;
    public float walkSpeed = 3.2f;
    public float runSpeed = 5.2f;

    CharacterController controller;
    float yaw;
    float pitch;
    float verticalSpeed;
    bool hadControl;
    int lookCooldown;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        yaw = transform.eulerAngles.y;
        Sensitivity = PlayerPrefs.GetFloat("sensitivity", 1.5f);
    }

    void Update()
    {
        bool control = GameManager.I != null && GameManager.I.PlayerControlEnabled;

        // Kursor qulflanganda sichqoncha bir martalik katta siljish beradi — bir necha kadr e'tiborsiz qoldiramiz
        if (control && !hadControl) lookCooldown = 3;
        hadControl = control;

        if (control)
        {
            Vector2 look = GameInput.Look() * Sensitivity;
            if (lookCooldown > 0)
            {
                lookCooldown--;
                look = Vector2.zero;
            }
            yaw += look.x;
            pitch = Mathf.Clamp(pitch - look.y, -85f, 85f);
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        // Menyu va kun hisoboti paytida fizika ishlamaydi — o'yinchi joyida qotib turadi
        if (GameManager.I == null || !GameManager.I.InGame) return;

        Vector2 input = control ? GameInput.Move() : Vector2.zero;
        Vector3 move = transform.right * input.x + transform.forward * input.y;
        if (move.sqrMagnitude > 1f) move.Normalize();
        float speed = control && GameInput.Run() ? runSpeed : walkSpeed;

        if (controller.isGrounded && verticalSpeed < 0f) verticalSpeed = -1f;
        verticalSpeed -= 9.81f * Time.deltaTime;
        controller.Move((move * speed + Vector3.up * verticalSpeed) * Time.deltaTime);

        // Xavfsizlik: biror sabab bilan pol ostiga tushib ketsa, oshxonaga qaytaramiz
        if (transform.position.y < -1f) Teleport(GameManager.I.playerSpawn, GameManager.I.playerSpawnYaw);
    }

    // Polda yotgan narsalar va mijozlar o'yinchini to'sib qo'ymasligi uchun ular bilan to'qnashuv o'chiriladi
    public void IgnoreCollisionsWith(GameObject other)
    {
        foreach (var c in other.GetComponentsInChildren<Collider>(true))
            Physics.IgnoreCollision(controller, c, true);
    }

    public void Teleport(Vector3 position, float yawDegrees)
    {
        controller.enabled = false;
        transform.position = position;
        yaw = yawDegrees;
        pitch = 8f;
        verticalSpeed = 0f;
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        controller.enabled = true;
    }
}
