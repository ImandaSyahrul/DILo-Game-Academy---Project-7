using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Painter : MonoBehaviour
{

	public Material backgroundMaterial;

	public const int TEXTURE_WIDTH = 256;
	public const int TEXTURE_HEIGHT = 256;

	public Material tmpDrawMaterial;

	[Tooltip("Target bidang gambar")]
	public MeshRenderer targetRender;

	[Tooltip("Target bidang yang sedang digunakan")]
	public MeshRenderer tempTargetRender;

	Texture2D targetTexture = null;

	Texture2D temporaryTexture = null;

	// Camera utama
	Camera cam = default;

	Vector3 lastPixelPosition;
	Vector3 startDownPos;
	Vector3 lastMouseUpPos;

	Vector3 startTrianglePos;
	int lineCount = 0;

	[System.Serializable]
	public class ShapeModel
	{

		public DrawingMode Mode;
		public List<Vector2> Vertices = new List<Vector2>();
	}

	public List<ShapeModel> ShapeModels = new List<ShapeModel>();

	// gambar yang sedang diolah
	ShapeModel currentDrawnShape;

	public class Edge
	{
		public int x1, y1, x2, y2;
		public Edge(int x1, int y1, int x2, int y2)
		{
			this.x1 = x1;
			this.y1 = y1;
			this.x2 = x2;
			this.y2 = y2;
		}
	}

	public enum DrawingMode
	{
		Line,
		Triangle
	}
	public DrawingMode CurrentDrawingMode = DrawingMode.Line;



	void Start()
	{
		// Mendapatkan camera utama
		cam = Camera.main;

		SetDefaultTexture();
		SetDefaultTemporaryTexture();
	}

	void SetDefaultTexture()
	{
		// Target texture yang akan digambar
		Texture2D targetTexture = null;

		// Setup texture yang akan digambar
		targetTexture = new Texture2D(TEXTURE_WIDTH, TEXTURE_HEIGHT);
		targetTexture.filterMode = FilterMode.Point;
		targetTexture.wrapMode = TextureWrapMode.Clamp;

		// Beri texture secara default berwarna putih
		Color[] cols = targetTexture.GetPixels();
		for (int i = 0; i < cols.Length; ++i)
		{
			cols[i] = Color.white;
		}

		// Set pengaturan texture
		targetTexture.SetPixels(cols);
		targetTexture.Apply();

		// Buat material gambar tidak terpengaruh oleh cahaya
		targetRender.material = new Material(backgroundMaterial);
		targetRender.material.mainTexture = targetTexture;

		this.targetTexture = (Texture2D)targetRender.material.mainTexture;
	}


	void Update()
	{
		RaycastHit hit;
		if (!Physics.Raycast(cam.ScreenPointToRay(Input.mousePosition), out hit))
			return;

		Renderer rend = hit.transform.GetComponent<Renderer>();
		MeshCollider meshCollider = hit.collider as MeshCollider;

		if (rend == null || rend.sharedMaterial == null || rend.sharedMaterial.mainTexture == null || meshCollider == null)
			return;

		Texture2D tex = rend.material.mainTexture as Texture2D;

		Vector2 pixelUV = hit.textureCoord;
		pixelUV.x *= tex.width;
		pixelUV.y *= tex.height;


		//if (Input.GetMouseButton(0))
		//{
		//	DrawDot(ref tex, (int)pixelUV.x, (int)pixelUV.y);
		//}

		

		// Menerima input mouse pertama ditekan
		if (Input.GetMouseButtonDown(0))
		{
			startDownPos = pixelUV;

			switch (this.CurrentDrawingMode)
			{
				case DrawingMode.Line:
					currentDrawnShape = new ShapeModel();
					currentDrawnShape.Mode = DrawingMode.Line;
					break;

				// ========= Fungsional membuat shape segitiga    ======//
				case DrawingMode.Triangle:
					// posisi awal menekan mouse
					if (lineCount == 0)
					{
						currentDrawnShape = new ShapeModel();
						currentDrawnShape.Mode = DrawingMode.Triangle;

						startTrianglePos = startDownPos;

						// tambahkan data titik awal
						currentDrawnShape.Vertices.Add(pixelUV);
					}
					break;

			}
		}


		// Menerima input mouse sedang ditekan
		if (Input.GetMouseButton(0))
		{

			switch (this.CurrentDrawingMode)
			{
				case DrawingMode.Line:
					tempTargetRender.gameObject.SetActive(true);

					if (hit.transform == tempTargetRender.transform)
					{
						ClearColor(ref temporaryTexture);
						DrawBresenhamLine(ref temporaryTexture, (int)startDownPos.x, (int)startDownPos.y, (int)pixelUV.x, (int)pixelUV.y);
					}
					break;


				// =========== 
				// =========== tambahkan kode penggambaran segitiga
				// =========== 
				case DrawingMode.Triangle:
					tempTargetRender.gameObject.SetActive(true);

					if (lineCount == 0 && (startDownPos.x != pixelUV.x && startDownPos.y != pixelUV.y))
					{
						lastMouseUpPos = startDownPos;
						lineCount = 1;
						// tambahkan data titik baru
						currentDrawnShape.Vertices.Add(pixelUV);
					}

					if (currentDrawnShape.Vertices.Count > 0)
						currentDrawnShape.Vertices[currentDrawnShape.Vertices.Count - 1] = pixelUV;

					break;
			}
		}

		//=========== Copy dari temporary ke tekstur akhir dan bersihkan warna di temporary
		if (Input.GetMouseButtonUp(0))
		{
			switch (this.CurrentDrawingMode)
			{
				case DrawingMode.Line:

					ApplyTemporaryTex(ref temporaryTexture, ref targetTexture);
					targetTexture.Apply();
					ClearColor(ref temporaryTexture);
					break;
			}
		}


		// ======= Menambahkan data shape
		if (Input.GetMouseButtonUp(0) && currentDrawnShape != null)
		{
			switch (this.CurrentDrawingMode)
			{
				// menggambar garis
				case DrawingMode.Line:

					// menambahkan titik awal
					currentDrawnShape.Vertices.Add(startDownPos);
					// menambahkan titik akhir
					currentDrawnShape.Vertices.Add(pixelUV);
					// menyimpan data gambar
					ShapeModels.Add(currentDrawnShape);

					// reset data yang sedang digambar
					currentDrawnShape = null;

					break;
			}

			// tampilkan semua shape
			RenderShapes(ref targetTexture);
		}

		// Menerima input ketika mouse diangkat
		if (Input.GetMouseButtonUp(0) && currentDrawnShape != null)
		{
			switch (this.CurrentDrawingMode)
			{
				// menggambar garis
				case DrawingMode.Line:

					// menambahkan titik awal
					currentDrawnShape.Vertices.Add(startDownPos);
					// menambahkan titik akhir
					currentDrawnShape.Vertices.Add(pixelUV);
					// menyimpan data gambar
					ShapeModels.Add(currentDrawnShape);

					// reset data yang sedang digambar
					currentDrawnShape = null;

					break;

				// =========== 
				// =========== tambahkan kode penyimpanan data segitiga
				// =========== 
				// menggambar segitiga
				case DrawingMode.Triangle:

					// menghitung jumlah garis yang sudah digambar
					// indeks titik 0, 1, dan 2
					if (lineCount < 2)
					{

						if (lineCount == 0 && (startDownPos.x != pixelUV.x && startDownPos.y != pixelUV.y))
						{
							lineCount = 1;
						}
						else
						{
							lineCount++;

							// tambahkan data titik baru
							currentDrawnShape.Vertices.Add(pixelUV);
						}

					}
					else
					{

						// tambahkan data gambar
						ShapeModels.Add(currentDrawnShape);

						// reset indeks garis
						lineCount = 0;

						// reset data yang sedang digambar
						currentDrawnShape = null;
					}

					break;
			}
			lastMouseUpPos = pixelUV;
		}

		// Preview update
		if (currentDrawnShape != null && currentDrawnShape.Vertices.Count > 0)
		{

			Vector2 vertex1, vertex2;

			switch (this.CurrentDrawingMode)
			{
				case DrawingMode.Triangle:
					// titik yang sedang digerakkan
					currentDrawnShape.Vertices[currentDrawnShape.Vertices.Count - 1] = pixelUV;
					break;
			}

			// proses menggambar garis-garis preview
			ClearColor(ref temporaryTexture);
			for (int itVertex = 0; itVertex < currentDrawnShape.Vertices.Count - 1; itVertex++)
			{
				if (itVertex < currentDrawnShape.Vertices.Count - 1)
				{
					vertex1 = currentDrawnShape.Vertices[itVertex];
					vertex2 = currentDrawnShape.Vertices[itVertex + 1];

					int x1 = (int)vertex1.x;
					int y1 = (int)vertex1.y;
					int x2 = (int)vertex2.x;
					int y2 = (int)vertex2.y;

					// garis penghubung
					DrawBresenhamLine(ref temporaryTexture, x1, y1, x2, y2);
				}
			}
			
		}
		lastPixelPosition = hit.textureCoord;
		lastMouseUpPos = lastPixelPosition;
		tex.Apply();
	}

	void DrawDot(ref Texture2D targetTex, int x, int y)
	{
		targetTex.SetPixel(x, y, Color.black);
	}

	void DrawBresenhamLine(ref Texture2D targetTex, int x0, int y0, int x1, int y1)
	{
		int dx = Mathf.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
		int dy = Mathf.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
		int err = (dx > dy ? dx : -dy) / 2, e2;
		for (; ; )
		{
			targetTex.SetPixel(x0, y0, Color.black);
			if (x0 == x1 && y0 == y1) break;
			e2 = err;
			if (e2 > -dx) { err -= dy; x0 += sx; }
			if (e2 < dy) { err += dx; y0 += sy; }
		}
	}

	void ClearColor(ref Texture2D targetTex)
	{
		Color transparentColor = new Color(1, 1, 1, 0);
		Color[] cols = targetTex.GetPixels();
		for (int i = 0; i < cols.Length; ++i)
		{
			cols[i] = transparentColor;
		}
		targetTex.SetPixels(cols);
	}

	void SetDefaultTemporaryTexture()
	{
		// Target texture yang akan digambar
		Texture2D targetTexture = null;

		// Setup texture yang akan digambar
		targetTexture = new Texture2D(TEXTURE_WIDTH, TEXTURE_HEIGHT);
		targetTexture.filterMode = FilterMode.Point;
		targetTexture.wrapMode = TextureWrapMode.Clamp;

		// Beri texture secara default berwarna transparent
		Color transparentColor = new Color(1, 1, 1, 0);
		Color[] cols = targetTexture.GetPixels();
		for (int i = 0; i < cols.Length; ++i)
		{
			cols[i] = transparentColor;
		}

		// Set pengaturan texture
		targetTexture.SetPixels(cols);
		targetTexture.Apply();

		// Buat material gambar tidak terpengaruh oleh cahaya
		tempTargetRender.material = new Material(this.tmpDrawMaterial);
		tempTargetRender.material.mainTexture = targetTexture;

		this.temporaryTexture = (Texture2D)tempTargetRender.material.mainTexture;
	}

	void ApplyTemporaryTex(ref Texture2D originTex, ref Texture2D targetTex)
	{
		Color[] originColors = originTex.GetPixels();
		Color[] targetColors = targetTex.GetPixels();
		for (int i = 0; i < targetColors.Length; ++i)
		{
			targetColors[i] = targetColors[i] * originColors[i];
		}
		targetTex.SetPixels(targetColors);
	}

	void RenderShapes(ref Texture2D texture)
	{
		ClearColor(ref texture);

		int x1, y1, x2, y2;
		Vector2 vertex1;
		Vector2 vertex2;

		List<Edge> edges = new List<Edge>();
		for (int i = 0; i < this.ShapeModels.Count; i++)
		{
			ShapeModel imageModel = this.ShapeModels[i];
			edges.Clear();

			switch (imageModel.Mode)
			{
				case DrawingMode.Line:
					x1 = (int)imageModel.Vertices[0].x;
					y1 = (int)imageModel.Vertices[0].y;
					x2 = (int)imageModel.Vertices[1].x;
					y2 = (int)imageModel.Vertices[1].y;
					edges.Add(new Edge(x1, y1, x2, y2));
					break;

				// ================================
				// ========= Tambahkan proses hubungan antar titik
				// ================================
				case DrawingMode.Triangle:

					for (int itVertex = 0; itVertex < imageModel.Vertices.Count - 1; itVertex++)
					{
						if (itVertex < imageModel.Vertices.Count - 1)
						{
							vertex1 = imageModel.Vertices[itVertex];
							vertex2 = imageModel.Vertices[itVertex + 1];

							x1 = (int)vertex1.x;
							y1 = (int)vertex1.y;
							x2 = (int)vertex2.x;
							y2 = (int)vertex2.y;

							// garis penghubung
							edges.Add(new Edge(x1, y1, x2, y2));
						}
					}

					// garis terakhir
					vertex1 = imageModel.Vertices[imageModel.Vertices.Count - 1];
					vertex2 = imageModel.Vertices[0];
					x1 = (int)vertex1.x;
					y1 = (int)vertex1.y;
					x2 = (int)vertex2.x;
					y2 = (int)vertex2.y;
					edges.Add(new Edge(x1, y1, x2, y2));

					break;
			}

			// gambar garis dari masing-masing edge
			for (int itEdge = 0; itEdge < edges.Count; itEdge++)
			{
				Edge edge = edges[itEdge];
				DrawBresenhamLine(ref texture, edge.x1, edge.y1, edge.x2, edge.y2);
			}

			texture.Apply();
		}
	}
}


