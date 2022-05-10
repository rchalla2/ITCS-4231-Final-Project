using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using UnityEngine;

public class ObjectGenerator : MonoBehaviour {

	// The type of this object
	public ChunkHandler.ChunkObjectType objectType;

	// The chunk this object is associated to
	public Vector2Int chunk;

	// The mesh of this object
	Mesh mesh;

	// Handles object creation
	void Start() {
		mesh = new Mesh();
		GetComponent<MeshFilter>().mesh = mesh;
		GetComponent<MeshCollider>().sharedMesh = mesh;
		Generate();
	}

	// Gets the generator based on the type
	ObjectGenInterface GetGenerator() {
		switch (objectType) {
			case ChunkHandler.ChunkObjectType.Cacti: return new CactusGenerator();
			case ChunkHandler.ChunkObjectType.Tree: return new TreeGenerator();
			case ChunkHandler.ChunkObjectType.JungleTree: return new JungleTreeGenerator();
			case ChunkHandler.ChunkObjectType.Rock: return new RockGenerator();
			default:
				Debug.Log("Unknown Object Generator Type");
				return null;
		}
	}

	async void Generate() {
		ObjectGenInterface generator = GetGenerator();
		generator.Start(transform);
		ObjectGenInterface.ObjectGenerationData[] objects = generator.GenerateMeshValues();

		List<Color> colors = new List<Color>();
		List<Vector3> vertices = new List<Vector3>();

		for (int i = 0; i < objects.Length; i++) await Task.Run(() => GenerateMesh(generator, i, objects[i], colors, vertices));
		
		mesh.vertices = vertices.ToArray();
		mesh.colors = colors.ToArray();
		mesh.triangles = Enumerable.Range(0, vertices.Count).ToArray();
		mesh.RecalculateBounds();
		mesh.RecalculateNormals();
		mesh.RecalculateTangents();
	}

	// Builds the mesh using the marching cubes algorithm
	// Followed tutorial from https://www.youtube.com/watch?v=M3iI2l0ltbE&t=304
	void GenerateMesh(ObjectGenInterface generator, int objectGenerationDataIndex, ObjectGenInterface.ObjectGenerationData objectGenerationData,
		List<Color> colors, List<Vector3> vertices) {

		float[,,] values = objectGenerationData.values;

		// Reserve space to avoid excessive amounts of resizing of lists
		// Rough estimates, should only require one or two reallocs at worst
		vertices.Capacity += values.Length;
		colors.Capacity = vertices.Capacity;
		
		// Loop through every point and perform marching cubes
		for (int x = 0; x < values.GetLength(0) - 1; x++) {
			for (int z = 0; z < values.GetLength(1) - 1; z++) {
				for (int y = 0; y < values.GetLength(2) - 1; y++) {
					
					float[] vals = {
						values[x, z, y], // lower back left
						values[x+1, z, y], // lower back right
						values[x+1, z+1, y], // lower front right
						values[x, z+1, y], // lower front left
						values[x, z, y+1], // upper back left
						values[x+1, z, y+1], // upper back right
						values[x+1, z+1, y+1], // upper front right
						values[x, z+1, y+1], // upper front left
					};

					Vector3[] corners = {
						new Vector3(x, y, z), // lower back left
						new Vector3(x+1, y, z), // lower back right
						new Vector3(x+1, y, z+1), // lower front right
						new Vector3(x, y, z+1), // lower front left
						new Vector3(x, y+1, z), // upper back left
						new Vector3(x+1, y+1, z), // upper back right
						new Vector3(x+1, y+1, z+1), // upper front right
						new Vector3(x, y+1, z+1), // upper front left
					};

					int cubeIndex = 0;
					for (int i = 0; i < vals.Length; i++)
						if (vals[i] > 0.0f) cubeIndex |= 1 << i;
					
					int[] edges = MarchingCubesTable.triangulation[cubeIndex];

					if (edges[0] == -1) continue;

					Color color = generator.GetColor(objectGenerationDataIndex, x, y, z);

					foreach (int edge in edges) {
						if (edge == -1) break;
						int index1 = MarchingCubesTable.cornerIndexAFromEdge[edge];
						int index2 = MarchingCubesTable.cornerIndexBFromEdge[edge];

						// Use lerp between the two corners to smooth out points
						Vector3 vert = Vector3.Lerp(corners[index1], corners[index2], -vals[index1] / (vals[index2] - vals[index1]));
						vert.x *= objectGenerationData.scaling.x;
						vert.y *= objectGenerationData.scaling.y;
						vert.z *= objectGenerationData.scaling.z;
						vertices.Add(vert + objectGenerationData.offset);
						colors.Add(color);
					}
				}
			}
		}
	}
}
