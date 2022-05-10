using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ObjectGenInterface {

	// Data required to perform maching cubes on objects for generation
	public struct ObjectGenerationData {
		public float[,,] values;
		public Vector3 offset;
		public Vector3 scaling;
	}

	// Called prior to any generation, can be used for changing transform
	public virtual void Start(Transform transform) {}

	// Generates the mesh values for marching cubes to be applied
	public abstract ObjectGenerationData[] GenerateMeshValues();

	// Gets the color based on the coordinates
	public abstract Color GetColor(int objectGenerationDataIndex, int x, int y, int z);
}
