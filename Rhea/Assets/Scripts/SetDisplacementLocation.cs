using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class SetDisplacementLocation : MonoBehaviour
{
	private Transform m_transform;

	private void Start()
	{
		m_transform = transform;
	}

	void Update()
    {
		Shader.SetGlobalVector("_DisplacementCenter", m_transform.position);
	}
}
