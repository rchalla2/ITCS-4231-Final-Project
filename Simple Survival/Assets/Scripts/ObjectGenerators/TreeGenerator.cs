using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TreeGenerator : ObjectGenInterface {

	// Noise for bumpiness
	private static FastNoiseLite noise = new FastNoiseLite();

	// Colors for trunk
	private static Gradient trunkGradient = new Gradient();

	// Setup noises
	static TreeGenerator() {
		noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
		noise.SetFrequency(0.4f);
		noise.SetSeed(Random.Range(1, 10000));

		trunkGradient.SetKeys(new GradientColorKey[]{
			new GradientColorKey(new Color(77f/255f, 51f/255f, 0f), 0f),
			new GradientColorKey(new Color(153f/255f, 102f/255f, 0f), 1f)
		}, new GradientAlphaKey[] {
			new GradientAlphaKey(1f, 0f),
			new GradientAlphaKey(1f, 1f)
		});
	}

	// Called prior to any generation, can be used for changing transform
	public override void Start(Transform transform) {
		transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
	}

	// Calculates a point on a bezier at a certain time
	private Vector3 BezierAt(float t, Vector3 p1, Vector3 cp, Vector3 p2) {
		return cp + Mathf.Pow(1f - t, 2f) * (p1 - cp) + Mathf.Pow(t, 2f) * (p2 - cp);
	}

	// Calculates the distance to a bezier curve
	private float DistToBezier(Vector3 p, Vector3 p1, Vector3 cp, Vector3 p2) {
		// Function for Quadratic Bezier (p0 = start, p1 = control, p2 = end, t = time[0-1]):
		// B(t) = p1 + (1 - t)^2 * (p0 - p1) + t^2 * (p2 - p1)

		// Distance function:
		// sqrt(∑(p_ - B(t)_)^2) where _ is each dimension (x, y, z)

		// Distance between p and B(t):
		// D(t) = sqrt(
		// 	(px - (p1x + (1 - t)^2 * (p0x - p1x) + t^2 * (p2x - p1x)))^2 +
		// 	(py - (p1y + (1 - t)^2 * (p0y - p1y) + t^2 * (p2y - p1y)))^2 +
		// 	(pz - (p1z + (1 - t)^2 * (p0z - p1z) + t^2 * (p2z - p1z)))^2
		// )

		// Derivative of Distance function (Calculated using derivative calculator online)
		// D'(t) =
		// (4 * (p2z - p0z) * ((p2z - p0z) * (t - 1)^2 - p2z + pz) * (t - 1) + 4 * (p2y - p0y) * ((p2y - p0y) * (t - 1)^2 - p2y + py) *
		// (t - 1) + 4 * (p2x - p0x) * ((p2x - p0x) * (t - 1)^2 - p2x + px) * (t - 1)) / (2 * sqrt(((p2z - p0z) * (t - 1)^2 - p2z + pz)^2 +
		// ((p2y - p0y) * (t - 1)^2 - p2y + py)^2 + ((p2x - p0x) * (t - 1)^2 - p2x + px)^2))

		// Now find when the derivative function is 0 with respect to t:
		// t = (sqrt(( - 2 * p2z^2 + 4 * p0z * p2z - 2 * p2y^2 + 4 * p0y * p2y - 2 * p2x^2 + 4 * p0x * p2x - 2 * p0z^2 - 2 * p0x^2 - 2 * p0y^2)^2 - 4 *
		// (p2z^2 - 2 * p0z * p2z + p2y^2 - 2 * p0y * p2y + p2x^2 - 2 * p0x * p2x + p0z^2 + p0x^2 + p0y^2) * (p2y * py - p0y * py - p0z * p2z + pz * p2z -
		// p0y * p2y - p0x * p2x + px * p2x + p0z^2 - pz * p0z + p0x^2 - px * p0x + p0y^2)) + 2 * p2z^2 - 4 * p0z * p2z + 2 * p2y^2 - 4 * p0y * p2y + 2 *
		// p2x^2 - 4 * p0x * p2x + 2 * p0z^2 + 2 * p0x^2 + 2 * p0y^2) / (2 * (p2z^2 - 2 * p0z * p2z + p2y^2 - 2 * p0y * p2y + p2x^2 - 2 * p0x * p2x + p0z^2
		// + p0x^2 + p0y^2))
		// And
		// t = (-sqrt(( - 2 * p2z^2 + 4 * p0z * p2z - 2 * p2y^2 + 4 * p0y * p2y - 2 * p2x^2 + 4 * p0x * p2x - 2 * p0z^2 - 2 * p0x^2 - 2 * p0y^2)^2 - 4 *
		// (p2z^2 - 2 * p0z * p2z + p2y^2 - 2 * p0y * p2y + p2x^2 - 2 * p0x * p2x + p0z^2 + p0x^2 + p0y^2) * (p2y * py - p0y * py - p0z * p2z + pz * p2z -
		// p0y * p2y - p0x * p2x + px * p2x + p0z^2 - pz * p0z + p0x^2 - px * p0x + p0y^2)) + 2 * p2z^2 - 4 * p0z * p2z + 2 * p2y^2 - 4 * p0y * p2y + 2 *
		// p2x^2 - 4 * p0x * p2x + 2 * p0z^2 + 2 * p0x^2 + 2 * p0y^2) / (2 * (p2z^2 - 2 * p0z * p2z + p2y^2 - 2 * p0y * p2y + p2x^2 - 2 * p0x * p2x + p0z^2
		// + p0x^2 + p0y^2))

		// Now we need to find the max/min of these two values
		float t1 = (Mathf.Sqrt(Mathf.Pow( - 2 * Mathf.Pow(p2.z, 2) + 4 * p1.z * p2.z - 2 * Mathf.Pow(p2.y, 2) + 4 * p1.y * p2.y - 2 * Mathf.Pow(p2.x, 2) +
			4 * p1.x * p2.x - 2 * Mathf.Pow(p1.z, 2) - 2 * Mathf.Pow(p1.x, 2) - 2 * Mathf.Pow(p1.y, 2), 2) - 4 * (Mathf.Pow(p2.z, 2) - 2 * p1.z *
			p2.z + Mathf.Pow(p2.y, 2) - 2 * p1.y * p2.y + Mathf.Pow(p2.x, 2) - 2 * p1.x * p2.x + Mathf.Pow(p1.z, 2) + Mathf.Pow(p1.x, 2) +
			Mathf.Pow(p1.y, 2)) * (p2.y * p.y - p1.y * p.y - p1.z * p2.z + p.z * p2.z - p1.y * p2.y - p1.x * p2.x + p.x * p2.x + Mathf.Pow(p1.z, 2) -
			p.z * p1.z + Mathf.Pow(p1.x, 2) - p.x * p1.x + Mathf.Pow(p1.y, 2))) + 2 * Mathf.Pow(p2.z, 2) - 4 * p1.z * p2.z + 2 * Mathf.Pow(p2.y, 2) -
			4 * p1.y * p2.y + 2 * Mathf.Pow(p2.x, 2) - 4 * p1.x * p2.x + 2 * Mathf.Pow(p1.z, 2) + 2 * Mathf.Pow(p1.x, 2) + 2 * Mathf.Pow(p1.y, 2)) /
			(2 * (Mathf.Pow(p2.z, 2) - 2 * p1.z * p2.z + Mathf.Pow(p2.y, 2) - 2 * p1.y * p2.y + Mathf.Pow(p2.x, 2) - 2 * p1.x * p2.x + Mathf.Pow(p1.z,
			2) + Mathf.Pow(p1.x, 2) + Mathf.Pow(p1.y, 2)));
		
		float t2 = (-Mathf.Sqrt(Mathf.Pow( - 2 * Mathf.Pow(p2.z, 2) + 4 * p1.z * p2.z - 2 * Mathf.Pow(p2.y, 2) + 4 * p1.y * p2.y - 2 * Mathf.Pow(p2.x, 2) +
			4 * p1.x * p2.x - 2 * Mathf.Pow(p1.z, 2) - 2 * Mathf.Pow(p1.x, 2) - 2 * Mathf.Pow(p1.y, 2), 2) - 4 * (Mathf.Pow(p2.z, 2) - 2 * p1.z *
			p2.z + Mathf.Pow(p2.y, 2) - 2 * p1.y * p2.y + Mathf.Pow(p2.x, 2) - 2 * p1.x * p2.x + Mathf.Pow(p1.z, 2) + Mathf.Pow(p1.x, 2) +
			Mathf.Pow(p1.y, 2)) * (p2.y * p.y - p1.y * p.y - p1.z * p2.z + p.z * p2.z - p1.y * p2.y - p1.x * p2.x + p.x * p2.x + Mathf.Pow(p1.z, 2) -
			p.z * p1.z + Mathf.Pow(p1.x, 2) - p.x * p1.x + Mathf.Pow(p1.y, 2))) + 2 * Mathf.Pow(p2.z, 2) - 4 * p1.z * p2.z + 2 * Mathf.Pow(p2.y, 2) -
			4 * p1.y * p2.y + 2 * Mathf.Pow(p2.x, 2) - 4 * p1.x * p2.x + 2 * Mathf.Pow(p1.z, 2) + 2 * Mathf.Pow(p1.x, 2) + 2 * Mathf.Pow(p1.y, 2)) /
			(2 * (Mathf.Pow(p2.z, 2) - 2 * p1.z * p2.z + Mathf.Pow(p2.y, 2) - 2 * p1.y * p2.y + Mathf.Pow(p2.x, 2) - 2 * p1.x * p2.x + Mathf.Pow(p1.z,
			2) + Mathf.Pow(p1.x, 2) + Mathf.Pow(p1.y, 2)));
		
		return Mathf.Min((BezierAt(t1, p1, cp, p2) - p).magnitude, (BezierAt(t2, p1, cp, p2) - p).magnitude);
	}

	// Generates the mesh values for marching cubes to be applied
	public override ObjectGenerationData[] GenerateMeshValues() {
		float offset1 = Random.Range(0.0f, 10000.0f), offset2 = Random.Range(0.0f, 10000.0f);
		System.Func<Vector3, Vector3, Vector3> randomVector = (Vector3 min, Vector3 max) => new Vector3(
			Random.Range(min.x, max.x), Random.Range(min.y, max.y), Random.Range(min.z, max.z)
		);

		// Generate trunk based on distance to quadratic bezier + noise
		ObjectGenerationData trunkData = new ObjectGenerationData();
		trunkData.values = new float[10, 10, 20];
		trunkData.scaling = new Vector3(0.75f, 2f, 0.75f);
		trunkData.offset = new Vector3(-5f*0.75f, 0.0f, -5f*0.75f);

		Vector3 trunkStartPoint = new Vector3(5, 0, 5);
		Vector3 trunkEndPoint = randomVector(new Vector3(0, 15, 0), new Vector3(10, 18, 10));
		Vector3 trunkControlPoint = new Vector3(5, trunkEndPoint.y, 5);

		for (int x = 0; x < trunkData.values.GetLength(0); x++) {
			for (int z = 0; z < trunkData.values.GetLength(1); z++) {
				for (int y = 0; y < trunkData.values.GetLength(2); y++) {
					trunkData.values[x, z, y] = noise.GetNoise(offset1 + x, offset1 + y, offset1 + z) * 0.5f;
					trunkData.values[x, z, y] -= Mathf.Pow(DistToBezier(new Vector3(x, y, z), trunkStartPoint, trunkControlPoint, trunkEndPoint), 2) - 3f;
				}
			}
		}

		// Generate leaves based on distance to point above trunk similar to rocks
		ObjectGenerationData leavesData = new ObjectGenerationData();
		leavesData.values = new float[15, 15, 15];
		leavesData.scaling = new Vector3(2f, 2f, 2f);
		leavesData.offset = new Vector3(trunkEndPoint.x*0.75f - 15f, trunkEndPoint.y*2f - 5f, trunkEndPoint.z*0.75f - 15f);

		for (int x = 0; x < leavesData.values.GetLength(0); x++) {
			for (int z = 0; z < leavesData.values.GetLength(1); z++) {
				for (int y = 0; y < leavesData.values.GetLength(2); y++) {
					leavesData.values[x, z, y] = noise.GetNoise(offset2 + x, offset2 + y, offset2 + z) * 1.3f;
					leavesData.values[x, z, y] -= (new Vector3(x, y, z) - new Vector3(7.5f, 7.5f, 7.5f)).magnitude - 6f;
				}
			}
		}

		return new ObjectGenerationData[]{trunkData, leavesData};
	}

	// Gets the color based on the coordinates
	public override Color GetColor(int objectGenerationDataIndex, int x, int y, int z) {
		if (objectGenerationDataIndex == 0) { // Trunk coloring
			return trunkGradient.Evaluate(y / 30f);
		}
		else { // Leaves coloring
			return Color.green;
		}
	}
}
