using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Movement : MonoBehaviour
{
    CharacterController characterController;
    [SerializeField] float speed = 5;

    private void Start()
    {
        characterController = GetComponent<CharacterController>();
    }

    private void Update()
    {
        Move();
    }
    private void Move()
    {
        float hor = Input.GetAxis("Horizontal");
        float vert = Input.GetAxis("Vertical");
        Vector3 hormove = new Vector3(hor, 0, 0);
        Vector3 vertmove = new Vector3(0, 0, vert);

        if (Input.GetKeyDown(KeyCode.A))
        {
            transform.position = new Vector3(transform.position.x - speed, transform.position.y, transform.position.z);//hormove ;
        }
        else if (Input.GetKeyDown(KeyCode.D))
        {
            transform.position = new Vector3(transform.position.x + speed, transform.position.y, transform.position.z);
        }
        else if (Input.GetKeyDown(KeyCode.W))
        {
            transform.position = new Vector3(transform.position.x, transform.position.y, transform.position.z + speed);
        }
        else if (Input.GetKeyDown(KeyCode.S))
        {
            transform.position = new Vector3(transform.position.x, transform.position.y, transform.position.z - speed);
        }
    }
}