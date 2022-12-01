using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PlayerMovements : MonoBehaviour
{
    public CharacterController controller;

    public TMP_InputField chatBox;

    public float speed = 12f;
    public float turnSpeed = 90;

    // Update is called once per frame
    void Update()
    {
        if (!chatBox.isFocused) {
            Vector3 vel = transform.forward * Input.GetAxis("Vertical") + transform.right * Input.GetAxis("Horizontal");
            controller.SimpleMove(vel * speed);
        } else {
            controller.SimpleMove(new Vector3 (0,0,0));
        }
        
    }
}
