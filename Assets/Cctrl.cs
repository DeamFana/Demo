using UnityEngine;
using UnityEngine.UIElements;

public class Cctrl : MonoBehaviour
{
    public float speed = 10.0F;
    public float rotateSpeed = 3.0F;
    public float maxVerticalView = 50.0f;
    CharacterController controller;

    float verticalRotation = 0f; // ´¹Ö±Ðý×ª½Ç¶È

    private void Start()
    {
        controller = GetComponent<CharacterController>();
        Camera.main.transform.localEulerAngles.Set(0f, 0f, 0f);
    }

    void Update()
    {
        float hor = Input.GetAxis("Horizontal");
        float ver = Input.GetAxis("Vertical");
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y") * rotateSpeed;

        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -maxVerticalView, maxVerticalView);

        Camera.main.transform.localEulerAngles = new Vector3(verticalRotation, 0f, 0f);

        Vector3 forward = transform.TransformDirection(new Vector3(hor, 0, ver));
        controller.SimpleMove(forward * speed);
        transform.Rotate(0, mouseX * rotateSpeed, 0);
    }
}
