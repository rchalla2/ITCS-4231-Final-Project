using System.Collections;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class ChunkHandler : MonoBehaviour {

	// The width of a single chunk on both the x and z axes
	public const int chunkWidth = 32;

	// The height of a single chunk on the y axis
	public const int chunkHeight = 256;

	// Gradient used for coloring based on height
	public static Gradient underwaterGradient;
	
	// Gradient used for coloring based on height
	public static Gradient normalGradient;
	
	// Gradient used for coloring based on height
	public static Gradient mountainGradient;

	// Which biome a chunk is in
	public enum Biome {
		//                    Low Humidity     Medium Humidity   High Humidity
		/* Low Height */      Ocean,//         Ocean,            Ocean,
		/* Medium Height */   Desert,          Plains,           Jungle,
		/* High Height */     Mountains,//     Mountains,        Mountains
	};

	// The mesh associated with this chunk
	public Mesh mesh;

	// The biome of this chunk
	public Biome biome;

	// The chunk position of this chunk
	public Vector2Int position;

	// Callback to run on completeion of generation
	public System.Action generationCompletetionCallback;

	// Chunk object types
	public enum ChunkObjectType {
		Cacti, Tree, JungleTree, Rock
	}

	// Objects such as rocks, trees, cacti, etc
	public List<GameObject> chunkObjects;

	// The index of this chunk worker
	public int workerIndex;

	// List of object creators to be called on the main thread
	private Queue<System.Action> objectCreators = new Queue<System.Action>();

	// Whether or not this chunk is currently generating
	public bool generating = true;
	
	// Handles chunk creation
	void Start() {
		mesh = new Mesh();
		GetComponent<MeshFilter>().mesh = mesh;
		GetComponent<MeshCollider>().sharedMesh = mesh;

		GenerateChunk();
	}
	
	// Gets the biome based on the coordinates of this chunk
	Biome GetBiome(Vector2Int position) {
		int height = Mathf.FloorToInt(1.5f + 1.5f * TerrainGenerator.biomeHeightNoise.GetNoise((float) position.x, (float) position.y));
		if (height == 0) return Biome.Ocean;
		if (height == 2) return Biome.Mountains;

		int humidity = Mathf.FloorToInt(1.5f + 1.5f * TerrainGenerator.biomeHumidityNoise.GetNoise((float) position.x, (float) position.y));
		
		if (humidity == 0) return Biome.Desert;
		else if (humidity == 1) return Biome.Plains;
		else return Biome.Jungle;
	}

	// Goes through the complete process of generating this chunk
	async void GenerateChunk() {
		biome = GetBiome(position);
		this.transform.position = new Vector3(position.x * chunkWidth, 0, position.y * chunkWidth);

		List<Color> colors = new List<Color>();
		List<Vector3> vertices = new List<Vector3>();
		float[,,] weights = new float[chunkWidth + 1, chunkWidth + 1, 3]; // [Underwaterweight, Normalweight, Mountainweight] for each x and z

		await Task.Run(() => BuildMesh(GenerateTerrain(weights), colors, vertices, weights));

		// Assign values to mesh and update mesh to reflect changes
		mesh.vertices = vertices.ToArray();
		mesh.colors = colors.ToArray();
		mesh.triangles = Enumerable.Range(0, vertices.Count).ToArray();
		mesh.RecalculateBounds();
		mesh.RecalculateNormals();
		mesh.RecalculateTangents();
		GetComponent<MeshFilter>().mesh = mesh;
		GetComponent<MeshCollider>().sharedMesh = mesh;
		generationCompletetionCallback();
		generating = false;
	}

	// Performs terrain generation for all biomes
	float[,,] GenerateTerrain(float[,,] weights) {
		float[,,] terrain = new float[chunkWidth + 1, chunkWidth + 1, chunkHeight + 1];

		(Vector2, System.Func<Vector2, int, int, int, float>) underwaterChunk = GetGeneratrorOffset(Biome.Ocean);
		(Vector2, System.Func<Vector2, int, int, int, float>) normalChunk = GetGeneratrorOffset(Biome.Plains);
		(Vector2, System.Func<Vector2, int, int, int, float>) mountainChunk = GetGeneratrorOffset(Biome.Mountains);
		
		// Loop through each individual marching cube
		for (int x = 0; x <= chunkWidth; x++) {
			for (int z = 0; z <= chunkWidth; z++) {
				float height = 1.5f + 1.5f * TerrainGenerator.biomeHeightNoise.GetNoise((float) position.x + x / (float) chunkWidth,
					(float) position.y + z / (float) chunkWidth);
				float underwaterWeight, normalWeight, mountainWeight;

				if (height < 1.0f) { // Underwater
					underwaterWeight = 1.0f;
					normalWeight = 0.0f;
					mountainWeight = 0.0f;
				}
				else if (height < 2.0f) { // Normal
					underwaterWeight = height > 1.2f ? 0.0f : 1.0f - (height - 1.0f) / 0.2f;
					mountainWeight = height < 1.8 ? 0.0f : (height - 1.8f) / 0.2f;

					if (height > 1.2f && height < 1.8f) normalWeight = 1.0f;
					else if (height < 1.2f) normalWeight = (height - 1.0f) / 0.2f;
					else normalWeight = 1.0f - (height - 1.8f) / 0.2f;
				}
				else { // Mountain
					underwaterWeight = 0.0f;
					normalWeight = 0.0f;
					mountainWeight = 1.0f;
				}

				// Save the weights to allow for color calculation later
				weights[x,z,0] = underwaterWeight;
				weights[x,z,1] = normalWeight;
				weights[x,z,2] = mountainWeight;

				for (int y = 0; y <= chunkHeight; y++) {
					float underwaterNoise = underwaterWeight == 0.0f ? 0.0f : underwaterChunk.Item2(underwaterChunk.Item1, x, y, z);
					float normalNoise = normalWeight == 0.0f ? 0.0f : normalChunk.Item2(normalChunk.Item1, x, y, z);
					float mountainNoise = mountainWeight == 0.0f ? 0.0f : mountainChunk.Item2(mountainChunk.Item1, x, y, z);

					terrain[x,z,y] = underwaterWeight * underwaterNoise + normalWeight * normalNoise + mountainWeight * mountainNoise;
				}
			}
		}
		
		return terrain;
	}

	// Gets the offset and terrain generator function based on the biome type
	(Vector2, System.Func<Vector2, int, int, int, float>) GetGeneratrorOffset(Biome biome) {
		System.Func<Vector2, int, int, int, float> terrainGen;
		Vector2 o;
		if (biome == Biome.Ocean) {
			o = new Vector2(position.x * chunkWidth * 3f, position.y * chunkWidth * 3f);
			terrainGen = GenerateOceanTerrain;
		}
		else if (biome == Biome.Mountains) {
			o = new Vector2(position.x * chunkWidth * 0.3f, position.y * chunkWidth * 0.3f);
			terrainGen = GenerateMountainyTerrain;
		}
		else {
			o = new Vector2(position.x * chunkWidth * 0.1f, position.y * chunkWidth * 0.1f);
			terrainGen = GenerateNormalTerrain;
		}
		return (o, terrainGen);
	}

	// Generates mountainy terrain
	float GenerateMountainyTerrain(Vector2 o, int x, int y ,int z) {
		return (TerrainGenerator.heightMountainyNoise.GetNoise(o.x + x * 0.3f, o.y + z * 0.3f, 0.2f*y) * 0.5f + 0.5f) - (float) y / chunkHeight;
	}

	// Generates normal/flat terrain
	float GenerateNormalTerrain(Vector2 o, int x, int y ,int z) {
		return (TerrainGenerator.heightNormalNoise.GetNoise(o.x + x * 0.1f, o.y + z * 0.1f, 0.15f*y) * 0.5f + 0.5f) - (float)y * 2.0f / chunkHeight;
	}

	// Generates ocean/underwater terrain
	float GenerateOceanTerrain(Vector2 o, int x, int y ,int z) {
		return (TerrainGenerator.heightUnderwaterNoise.GetNoise(o.x + x * 3f, o.y + z * 3f, 4.0f * y) * 0.5f + 0.5f) - (float)y * 8.0f / chunkHeight;
	}

	// Adds a chunk object based on the biome
	void AddChunkObject(int x, int y, int z) {
		ChunkObjectType type;

		if (biome == Biome.Desert)
			type = TerrainGenerator.RandomRange(workerIndex, 0.0f, 1.0f) > 0.2f ? ChunkObjectType.Cacti : ChunkObjectType.Rock;
		else if (biome == Biome.Plains) {
			if (TerrainGenerator.RandomRange(workerIndex, 0.0f, 1.0f) > 0.3f)
				return;
			else
				type = TerrainGenerator.RandomRange(workerIndex, 0.0f, 1.0f) > 0.2f ? ChunkObjectType.Tree : ChunkObjectType.Rock;
		}
		else { // Jungle
			type = TerrainGenerator.RandomRange(workerIndex, 0.0f, 1.0f) > 0.5f ? ChunkObjectType.JungleTree : ChunkObjectType.Tree;
		}

		objectCreators.Enqueue(() => {
			GameObject chunkObject = (GameObject) Instantiate(Resources.Load<GameObject>(type.GetPath()));
			chunkObject.transform.position = new Vector3(position.x * chunkWidth + x, y - 0.5f, position.y * chunkWidth + z);
			chunkObject.layer = LayerMask.NameToLayer("Ground");
			chunkObject.transform.parent = this.transform;
			chunkObject.transform.localScale = chunkObject.transform.localScale * 10f;
			chunkObject.AddComponent<MeshCollider>();

			ObjectHandler obj = chunkObject.AddComponent<ObjectHandler>();
			obj.type = type;
			switch (type) {
				case ChunkObjectType.Tree: case ChunkObjectType.JungleTree:
					obj.count = Random.Range(1, 3);
					obj.item = InventoryItem.Log;
					break;
				case ChunkObjectType.Rock:
					obj.count = Random.Range(1, 2);
					obj.item = InventoryItem.Stone;
					break;
			}
		});
	}

	// Builds the mesh using the marching cubes algorithm
	// Followed tutorial from https://www.youtube.com/watch?v=M3iI2l0ltbE&t=304
	void BuildMesh(float[,,] terrain, List<Color> colors, List<Vector3> vertices, float[,,] weights) {
		// Reserve space to avoid excessive amounts of resizing of lists
		// Rough estimates, should only require one or two reallocs at worst
		vertices.Capacity = (chunkWidth+1) * (chunkWidth+1) * 10;
		colors.Capacity = vertices.Capacity;

		Dictionary<Vector2Int, bool> points = new Dictionary<Vector2Int, bool>();
	
		if (biome != Biome.Ocean && biome != Biome.Mountains) {
			for (int x = 0; x < chunkWidth; x += 16) {
				for (int y = 0; y < chunkWidth; y += 16) {
					points.Add(new Vector2Int(
						x + TerrainGenerator.RandomRange(workerIndex, 2, 6),
						y + TerrainGenerator.RandomRange(workerIndex, 2, 6)
					), true);
				}
			}
		}
		
		// Loop through every point and perform marching cubes
		for (int x = 0; x < chunkWidth; x++) {
			for (int z = 0; z < chunkWidth; z++) {
				// Perform enum check to avoid searching hash table
				bool containsObject = biome != Biome.Ocean && biome != Biome.Mountains && points.ContainsKey(new Vector2Int(x, z));

				for (int y = 0; y < chunkHeight; y++) {
					float[] vals = {
						terrain[x, z, y], // lower back left
						terrain[x+1, z, y], // lower back right
						terrain[x+1, z+1, y], // lower front right
						terrain[x, z+1, y], // lower front left
						terrain[x, z, y+1], // upper back left
						terrain[x+1, z, y+1], // upper back right
						terrain[x+1, z+1, y+1], // upper front right
						terrain[x, z+1, y+1], // upper front left
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

					if (containsObject && y > 32 && weights[x, z, 1] == 1.0f) {
						AddChunkObject(x, y, z);
						containsObject = false;
					}

					Color oceanColor = underwaterGradient.Evaluate(Mathf.Clamp((y - 16.0f) / 10.0f, 0.0f, 1.0f));
					Color normalColor = normalGradient.Evaluate(Mathf.Clamp(vals[0] / 2.0f + 0.5f, 0.0f, 1.0f));
					Color mountainColor = mountainGradient.Evaluate(Mathf.Clamp((y - 96.0f) / 64.0f, 0.0f, 1.0f));
					Color color = weights[x,z,0] * oceanColor + weights[x,z,1] * normalColor + weights[x,z,2] * mountainColor;

					foreach (int edge in edges) {
						if (edge == -1) break;
						int index1 = MarchingCubesTable.cornerIndexAFromEdge[edge];
						int index2 = MarchingCubesTable.cornerIndexBFromEdge[edge];

						// Use lerp between the two corners to smooth out points
						vertices.Add(Vector3.Lerp(corners[index1], corners[index2], -vals[index1] / (vals[index2] - vals[index1])));
						colors.Add(color);
					}
				}
			}
		}
	}

	// Called each frame
	void Update() {
		// Go through action queue
		if (!generating)
			while (objectCreators.Count > 0) objectCreators.Dequeue()();
	}
}

static class ChunkObjectTypeMethods {
	public static string GetPath(this ChunkHandler.ChunkObjectType type) {
		switch (type) {
			case ChunkHandler.ChunkObjectType.Cacti:
				return PrefabPaths.cactiPaths[Random.Range(0, PrefabPaths.cactiPaths.Length - 1)];
			case ChunkHandler.ChunkObjectType.Tree: case ChunkHandler.ChunkObjectType.JungleTree:
				return PrefabPaths.treePaths[Random.Range(0, PrefabPaths.treePaths.Length - 1)];
			case ChunkHandler.ChunkObjectType.Rock:
				return PrefabPaths.rockPaths[Random.Range(0, PrefabPaths.rockPaths.Length - 1)];
			default:
				return "";
		}
	}
}
