using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Controller : MonoBehaviour 
{
	public float _moveSpeed = 6.0f;

	public Rigidbody _rigidbody;
	public Camera _viewCamera;
	Vector3 _velocity;

	// Use this for initialization
	void Start () 
	{
		_rigidbody = GetComponent<Rigidbody>();
		_viewCamera = Camera.main;
	}
	
	// Update is called once per frame
	void Update () 
	{
		Vector3 mousePos = _viewCamera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, _viewCamera.transform.position.y));
		transform.LookAt(mousePos + Vector3.up * transform.position.y);
	}
}
