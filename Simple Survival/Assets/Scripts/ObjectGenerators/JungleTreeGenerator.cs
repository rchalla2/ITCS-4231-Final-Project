using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JungleTreeGenerator : ObjectGenInterface {

	// Setup noises
	static JungleTreeGenerator() {

	}

	// Generates the mesh values for marching cubes to be applied
	public override ObjectGenerationData[] GenerateMeshValues() {
		return new ObjectGenerationData[0];
	}

	// Gets the color based on the coordinates
	public override Color GetColor(int objectGenerationDataIndex, int x, int y, int z) {
		return Color.green;
	}
}
