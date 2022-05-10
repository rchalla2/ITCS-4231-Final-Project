using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RockGenerator : ObjectGenInterface {

	// Noise for bumpiness
	private static FastNoiseLite noise = new FastNoiseLite();

	// Setup noises
	static RockGenerator() {
		noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
		noise.SetFrequency(0.4f);
		noise.SetSeed(Random.Range(1, 10000));
	}

	// Called prior to any generation, can be used for changing transform
	public override void Start(Transform transform) {
		transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);
	}

	// Generates the mesh values for marching cubes to be applied
	public override ObjectGenerationData[] GenerateMeshValues() {
		float[,,] values = new float[8,8,8];
		float offset = Random.Range(0.0f, 10000.0f);

		for (int x = 0; x < values.GetLength(0); x++) {
			for (int z = 0; z < values.GetLength(1); z++) {
				for (int y = 0; y < values.GetLength(2); y++) {
					values[x, z, y] = noise.GetNoise(offset + x, offset + y, offset + z);
					Vector3 pos = new Vector3(x, y, z);
					values[x, z, y] -= (pos - new Vector3(4f, 4f, 4f)).magnitude * 1.2f - 3f;
				}
			}
		}

		ObjectGenerationData[] data = new ObjectGenerationData[1];
		data[0].values = values;
		data[0].offset = new Vector3(0, 0, 0);
		data[0].scaling = new Vector3(1, 1, 1);

		return data;
	}

	// Gets the color based on the coordinates
	public override Color GetColor(int objectGenerationDataIndex, int x, int y, int z) {
		float col = 0.5f - y / 8.0f * 0.5f + 0.2f;
		return new Color(col, col, col);
	}
}
