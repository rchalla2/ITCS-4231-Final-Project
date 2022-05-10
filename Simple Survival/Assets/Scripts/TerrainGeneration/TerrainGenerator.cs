using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainGenerator : MonoBehaviour {

	// Stores chunks by their chunk location
	private Dictionary<Vector2Int, GameObject> chunks = new Dictionary<Vector2Int, GameObject>();

	// The order to load chunks in surrounding the player
	private List<Vector2Int> chunkLoadingOrder = new List<Vector2Int>();

	// The opensimplex noise used for the height biome-mapping
	public static FastNoiseLite biomeHeightNoise;

	// The opensimplex noise used for humidity maps
	public static FastNoiseLite biomeHumidityNoise;

	// The opensimplex noise used on mountainy terrain
	public static FastNoiseLite heightMountainyNoise;

	// The opensimplex noise used on normal terrain
	public static FastNoiseLite heightNormalNoise;

	// The opensimplex noise used on underwater terrain
	public static FastNoiseLite heightUnderwaterNoise;

	// The player's transform/position
	public Transform player;

	// Ocean's transform
	public Transform ocean;

	// The material to render the ground with
	public Material chunkMaterial;

	// The amount of chunks in a circle to render around the player
	public int renderDistance = 8;

	// The number of asynchronous chunk loading tasks to do at a time
	public int numChunkLoaders = 4;

	// Whether or not a chunk is currently generating
	private int numChunksGenerating = 0;

	// Random generators for each thread
	public static System.Random[] randoms;

	// Chunk loader usages
	public bool[] chunkLoaders;

	// Gets a random number in a range, thread safe, inclusive
	public static float RandomRange(int workerNum, float min, float max) {
		return ((float) randoms[workerNum].NextDouble()) * (max - min) + min;
	}

	// Gets a random number in a range, thread safe, inclusive
	public static int RandomRange(int workerNum, int min, int max) {
		return randoms[workerNum].Next(min, max);
	}

	// Handles noise setup
	void Awake() {
		// Setup randoms
		randoms = new System.Random[numChunkLoaders];
		for (int i = 0; i < randoms.Length; i++) randoms[i] = new System.Random();
		chunkLoaders = new bool[numChunkLoaders];
		for (int i = 0; i < chunkLoaders.Length; i++) chunkLoaders[i] = false;

		// Setup Noises
		heightNormalNoise = new FastNoiseLite();
		heightNormalNoise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
		heightNormalNoise.SetSeed(Random.Range(1, 10000));
		heightNormalNoise.SetFractalType(FastNoiseLite.FractalType.FBm);
		heightNormalNoise.SetFractalOctaves(15);

		heightMountainyNoise = new FastNoiseLite();
		heightMountainyNoise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
		heightMountainyNoise.SetSeed(Random.Range(1, 10000));
		heightMountainyNoise.SetFractalType(FastNoiseLite.FractalType.FBm);
		heightMountainyNoise.SetFractalOctaves(15);

		heightUnderwaterNoise = new FastNoiseLite();
		heightUnderwaterNoise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
		heightUnderwaterNoise.SetSeed(Random.Range(1, 10000));
		heightUnderwaterNoise.SetFractalType(FastNoiseLite.FractalType.FBm);
		heightUnderwaterNoise.SetFractalOctaves(5);


		biomeHeightNoise = new FastNoiseLite();
		biomeHeightNoise.SetNoiseType(FastNoiseLite.NoiseType.Perlin);
		biomeHeightNoise.SetFrequency(0.03f);
		biomeHeightNoise.SetSeed(Random.Range(1, 10000));

		biomeHumidityNoise = new FastNoiseLite();
		biomeHumidityNoise.SetNoiseType(FastNoiseLite.NoiseType.Perlin);
		biomeHumidityNoise.SetFrequency(0.03f);
		biomeHumidityNoise.SetSeed(Random.Range(1, 10000));

		// Setup Color gradients
		ChunkHandler.underwaterGradient = new Gradient();
		ChunkHandler.underwaterGradient.SetKeys(new GradientColorKey[]{
			new GradientColorKey(new Color(0.0f/255.0f, 34.0f/255.0f, 102.0f/255.0f), 0.0f),
			new GradientColorKey(new Color(255.0f/255.0f, 102.0f/255.0f, 0.0f/255.0f), 1.0f)
		}, new GradientAlphaKey[]{
			new GradientAlphaKey(1.0f, 0.0f),
			new GradientAlphaKey(1.0f, 1.0f)
		});

		ChunkHandler.normalGradient = new Gradient();
		ChunkHandler.normalGradient.SetKeys(new GradientColorKey[]{
			new GradientColorKey(new Color(0.0f/255.0f, 230.0f/255.0f, 115.0f/255.0f), 0.0f),
			new GradientColorKey(new Color(0.0f/255.0f, 153.0f/255.0f, 77.0f/255.0f), 1.0f)
		}, new GradientAlphaKey[]{
			new GradientAlphaKey(1.0f, 0.0f),
			new GradientAlphaKey(1.0f, 1.0f)
		});

		ChunkHandler.mountainGradient = new Gradient();
		ChunkHandler.mountainGradient.SetKeys(new GradientColorKey[]{
			new GradientColorKey(new Color(38.0f/255.0f, 38.0f/255.0f, 38.0f/255.0f), 0.0f),
			new GradientColorKey(new Color(128.0f/255.0f, 128.0f/255.0f, 128.0f/255.0f), 0.8f),
			new GradientColorKey(new Color(242.0f/255.0f, 242.0f/255.0f, 242.0f/255.0f), 1.0f)
		}, new GradientAlphaKey[]{
			new GradientAlphaKey(1.0f, 0.0f),
			new GradientAlphaKey(1.0f, 0.8f),
			new GradientAlphaKey(1.0f, 1.0f)
		});
	}

	// Handles noise setup for chunkhandler
	void Start() {
		// Setup chunk loading order
		for (int x = (int) -renderDistance; x < (int) renderDistance; x++) {
			for (int y = (int) -renderDistance; y < (int) renderDistance; y++) {
				Vector2Int pos = new Vector2Int(x, y);
				if ((pos - new Vector2(-0.5f, -0.5f)).magnitude <= (float) renderDistance) {
					chunkLoadingOrder.Add(pos);
				}
			}
		}

		chunkLoadingOrder.Sort((vec1, vec2) => (int) (30.0f * ((vec1 - new Vector2(-0.5f, -0.5f)).magnitude - (vec2 - new Vector2(-0.5f, -0.5f)).magnitude)));

		// Setup ocean size
		ocean.localScale = new Vector3(10.0f * renderDistance * 2.0f / 3.0f, 1.0f, 10.0f * renderDistance * 2.0f / 3.0f);
	}

	// Loads a chunk given the chunk position
	void LoadChunk(Vector2Int position) {

		int workerIndex = -1;
		for (int i = 0; i < chunkLoaders.Length; i++) {
			if (!chunkLoaders[i]) {
				workerIndex = i;
				break;
			}
		}

		if (workerIndex == -1) {
			Debug.Log("Worker unavailable when load chunk called");
			return;
		}

		chunkLoaders[workerIndex] = true;

		numChunksGenerating++;
		GameObject chunk = new GameObject("Chunk (x: "+ position.x +", z: + " + position.y + ")");
		chunk.layer = LayerMask.NameToLayer("Ground");
		chunk.AddComponent<MeshFilter>();
		chunk.AddComponent<MeshCollider>();
		
		MeshRenderer renderer = chunk.AddComponent<MeshRenderer>();
		renderer.material = chunkMaterial;
		renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

		ChunkHandler chunkHandler = chunk.AddComponent<ChunkHandler>();
		chunkHandler.position = position;
		chunkHandler.generationCompletetionCallback = () => { numChunksGenerating--; chunkLoaders[workerIndex] = false; };
		chunkHandler.workerIndex = workerIndex;

		chunks.Add(position, chunk);
	}

	// Update is called once per frame
	void Update() {
		Vector2Int playerChunkPos = new Vector2Int(
			Mathf.RoundToInt(player.transform.position.x / (float) ChunkHandler.chunkWidth),
			Mathf.RoundToInt(player.transform.position.z / (float) ChunkHandler.chunkWidth)
		);

		// See if any loaded chunks need to be removed
		// we remove chunks only when they are further away than 1 + the render distance to avoid repeated unloading/loading of chunks on borders
		List<Vector2Int> toRemove = new List<Vector2Int>();
		foreach (Vector2Int pos in chunks.Keys)
			if ((int) (playerChunkPos - pos).magnitude > renderDistance + 1)
				toRemove.Add(pos);
		
		foreach (Vector2Int pos in toRemove) {
			Destroy(chunks[pos]);
			chunks.Remove(pos);
		}

		// Load new chunks if necessary
		if (numChunksGenerating < numChunkLoaders) {
			foreach (Vector2Int pos in chunkLoadingOrder) {
				if (chunks.ContainsKey(pos + playerChunkPos)) continue;
				LoadChunk(pos + playerChunkPos);
				break;
			}
		}

		ocean.position = new Vector3(player.position.x, 32.0f, player.position.z);
	}
}
