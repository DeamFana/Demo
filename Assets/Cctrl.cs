using UnityEngine;

public class Cctrl : MonoBehaviour
{
    public float speed = 3.0F;
    public float rotateSpeed = 2.0F;
    public float maxVerticalView = 40.0f;
    CharacterController controller;

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
        float mouseY = Input.GetAxis("Mouse Y");

        float delta = mouseY * rotateSpeed;
        float verView = Camera.main.transform.localEulerAngles.x;

        // Rotate around y - axis
        transform.Rotate(0, mouseX * rotateSpeed, 0);
        if (verView < 360 - maxVerticalView && verView > 180)
        {

            Camera.main.transform.localEulerAngles = new Vector3(360 - maxVerticalView, 0f, 0f);
        }
        else if (verView > maxVerticalView && verView < 180)
        {
            Debug.Log("FFFFFFFFFFFF");
            // 防止浮点数溢出
            Camera.main.transform.localEulerAngles = new Vector3(maxVerticalView - 0.001f, 0f, 0f);
        }
        else if (verView - delta > maxVerticalView)
            delta = maxVerticalView - verView - 1;
        else if (verView - delta < 360 - maxVerticalView)
            delta = 360 - maxVerticalView - verView - 1;
        Camera.main.transform.localEulerAngles = new Vector3(verView - delta, 0f, 0f);

        Debug.Log("delta: " + delta + "\t旋转后的值" + Camera.main.transform.localEulerAngles.x);

        // Move forward / backward
        Vector3 forward = transform.TransformDirection(new Vector3(hor, 0, ver));
        controller.SimpleMove(forward * speed);
    }
}
