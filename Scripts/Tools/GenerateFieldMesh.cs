using Godot;
using System.Collections.Generic;

public partial class GenerateField
{
	private MeshInstance3D _floor;
	private MeshInstance3D _walls;


	private void GenerateMeshes()
	{
		CreateMeshNodes();

		List<Vector2> perimeter = GeneratePerimeter();

		_floor.Mesh = GenerateFloorMesh(perimeter);

		if (WallHeight > 0.0f)
			_walls.Mesh = GenerateWallMesh(perimeter);
		else
			_walls.Mesh = null;
	}


	private void CreateMeshNodes()
	{
		_floor = GetNodeOrNull<MeshInstance3D>("Floor");

		if (_floor == null)
		{
			_floor = new MeshInstance3D();
			_floor.Name = "Floor";
			AddChild(_floor);
		}

		_walls = GetNodeOrNull<MeshInstance3D>("Walls");

		if (_walls == null)
		{
			_walls = new MeshInstance3D();
			_walls.Name = "Walls";
			AddChild(_walls);
		}


		// Make sure generated nodes belong to the edited scene.
		if (Engine.IsEditorHint())
		{
			Node editedRoot = GetTree().EditedSceneRoot;

			if (_floor.Owner != editedRoot)
				_floor.Owner = editedRoot;

			if (_walls.Owner != editedRoot)
				_walls.Owner = editedRoot;
		}
	}


	private List<Vector2> GeneratePerimeter()
	{
		float radius = Mathf.Min(
			CornerRadius,
			Mathf.Min(Width, Height) * 0.5f
		);

		var perimeter = new List<Vector2>();

		AddCorner(
			perimeter,
			new Vector2(
				Width * 0.5f - radius,
				Height * 0.5f - radius
			),
			radius,
			0.0f
		);

		AddCorner(
			perimeter,
			new Vector2(
				-Width * 0.5f + radius,
				Height * 0.5f - radius
			),
			radius,
			Mathf.Pi * 0.5f
		);

		AddCorner(
			perimeter,
			new Vector2(
				-Width * 0.5f + radius,
				-Height * 0.5f + radius
			),
			radius,
			Mathf.Pi
		);

		AddCorner(
			perimeter,
			new Vector2(
				Width * 0.5f - radius,
				-Height * 0.5f + radius
			),
			radius,
			Mathf.Pi * 1.5f
		);

		return perimeter;
	}


	private void AddCorner(
		List<Vector2> perimeter,
		Vector2 center,
		float radius,
		float startAngle)
	{
		for (int i = 0; i <= Segments; i++)
		{
			float t = (float)i / Segments;

			float angle =
				startAngle +
				Mathf.Pi * 0.5f * t;

			perimeter.Add(
				center +
				new Vector2(
					Mathf.Cos(angle),
					Mathf.Sin(angle)
				) * radius
			);
		}
	}


	private ArrayMesh GenerateFloorMesh(
		List<Vector2> perimeter)
	{
		var vertices = new List<Vector3>();
		var normals = new List<Vector3>();
		var uvs = new List<Vector2>();
		var indices = new List<int>();

		vertices.Add(Vector3.Zero);
		normals.Add(Vector3.Up);
		uvs.Add(new Vector2(0.5f, 0.5f));

		foreach (Vector2 point in perimeter)
		{
			vertices.Add(
				new Vector3(
					point.X,
					0.0f,
					point.Y
				)
			);

			normals.Add(Vector3.Up);

			uvs.Add(
				new Vector2(
					point.X / Width + 0.5f,
					point.Y / Height + 0.5f
				)
			);
		}

		for (int i = 0; i < perimeter.Count; i++)
		{
			int current = i + 1;
			int next = i + 2;

			if (next >= vertices.Count)
				next = 1;

			indices.Add(0);
			indices.Add(current);
			indices.Add(next);
		}

		return CreateMesh(
			vertices,
			normals,
			uvs,
			indices
		);
	}


	private ArrayMesh GenerateWallMesh(
		List<Vector2> perimeter)
	{
		var vertices = new List<Vector3>();
		var normals = new List<Vector3>();
		var uvs = new List<Vector2>();
		var indices = new List<int>();

		float perimeterLength =
			CalculatePerimeterLength(perimeter);

		float accumulatedLength = 0.0f;

		for (int i = 0; i < perimeter.Count; i++)
		{
			int nextIndex =
				(i + 1) % perimeter.Count;

			Vector2 current = perimeter[i];
			Vector2 next = perimeter[nextIndex];

			Vector2 direction =
				(next - current).Normalized();

			float segmentLength =
				current.DistanceTo(next);

			bool isWidthWall =
				Mathf.Abs(direction.X) > 0.999f &&
				Mathf.Abs(current.Y - next.Y) < 0.0001f;

			if (isWidthWall && HoleWidth > 0.0f)
			{
				AddWallSegmentWithHole(
					vertices,
					normals,
					uvs,
					indices,
					current,
					next,
					accumulatedLength,
					segmentLength,
					perimeterLength
				);
			}
			else
			{
				AddWallQuad(
					vertices,
					normals,
					uvs,
					indices,
					current,
					next,
					accumulatedLength,
					segmentLength,
					perimeterLength,
					0.0f,
					WallHeight
				);
			}

			accumulatedLength += segmentLength;
		}

		return CreateMesh(
			vertices,
			normals,
			uvs,
			indices
		);
	}


	private void AddWallSegmentWithHole(
		List<Vector3> vertices,
		List<Vector3> normals,
		List<Vector2> uvs,
		List<int> indices,
		Vector2 current,
		Vector2 next,
		float accumulatedLength,
		float segmentLength,
		float perimeterLength)
	{
		Vector2 direction =
			(next - current).Normalized();

		float holeWidth =
			Mathf.Min(HoleWidth, segmentLength);

		float holeStart =
			(segmentLength - holeWidth) * 0.5f;

		float holeEnd =
			holeStart + holeWidth;

		Vector2 holeStartPoint =
			current + direction * holeStart;

		Vector2 holeEndPoint =
			current + direction * holeEnd;


		if (holeStart > 0.0001f)
		{
			AddWallQuad(
				vertices,
				normals,
				uvs,
				indices,
				current,
				holeStartPoint,
				accumulatedLength,
				holeStart,
				perimeterLength,
				0.0f,
				WallHeight
			);
		}


		if (HoleHeight < WallHeight)
		{
			AddWallQuad(
				vertices,
				normals,
				uvs,
				indices,
				holeStartPoint,
				holeEndPoint,
				accumulatedLength + holeStart,
				holeWidth,
				perimeterLength,
				HoleHeight,
				WallHeight
			);
		}


		float rightLength =
			segmentLength - holeEnd;

		if (rightLength > 0.0001f)
		{
			AddWallQuad(
				vertices,
				normals,
				uvs,
				indices,
				holeEndPoint,
				next,
				accumulatedLength + holeEnd,
				rightLength,
				perimeterLength,
				0.0f,
				WallHeight
			);
		}
	}


	private void AddWallQuad(
		List<Vector3> vertices,
		List<Vector3> normals,
		List<Vector2> uvs,
		List<int> indices,
		Vector2 start,
		Vector2 end,
		float accumulatedLength,
		float segmentLength,
		float perimeterLength,
		float bottomHeight,
		float topHeight)
	{
		Vector2 direction =
			(end - start).Normalized();

		Vector2 normal =
			new Vector2(
				direction.Y,
				-direction.X
			).Normalized();

		int baseIndex = vertices.Count;

		vertices.Add(
			new Vector3(
				start.X,
				bottomHeight,
				start.Y
			)
		);

		normals.Add(
			new Vector3(
				normal.X,
				0.0f,
				normal.Y
			)
		);

		uvs.Add(
			new Vector2(
				accumulatedLength / perimeterLength,
				bottomHeight /
					Mathf.Max(WallHeight, 0.001f)
			)
		);


		vertices.Add(
			new Vector3(
				start.X,
				topHeight,
				start.Y
			)
		);

		normals.Add(
			new Vector3(
				normal.X,
				0.0f,
				normal.Y
			)
		);

		uvs.Add(
			new Vector2(
				accumulatedLength / perimeterLength,
				topHeight /
					Mathf.Max(WallHeight, 0.001f)
			)
		);


		vertices.Add(
			new Vector3(
				end.X,
				bottomHeight,
				end.Y
			)
		);

		normals.Add(
			new Vector3(
				normal.X,
				0.0f,
				normal.Y
			)
		);

		uvs.Add(
			new Vector2(
				(accumulatedLength + segmentLength)
					/ perimeterLength,
				bottomHeight /
					Mathf.Max(WallHeight, 0.001f)
			)
		);


		vertices.Add(
			new Vector3(
				end.X,
				topHeight,
				end.Y
			)
		);

		normals.Add(
			new Vector3(
				normal.X,
				0.0f,
				normal.Y
			)
		);

		uvs.Add(
			new Vector2(
				(accumulatedLength + segmentLength)
					/ perimeterLength,
				topHeight /
					Mathf.Max(WallHeight, 0.001f)
			)
		);


		indices.Add(baseIndex);
		indices.Add(baseIndex + 1);
		indices.Add(baseIndex + 2);

		indices.Add(baseIndex + 1);
		indices.Add(baseIndex + 3);
		indices.Add(baseIndex + 2);
	}


	private float CalculatePerimeterLength(
		List<Vector2> perimeter)
	{
		float length = 0.0f;

		for (int i = 0; i < perimeter.Count; i++)
		{
			int next = (i + 1) % perimeter.Count;

			length +=
				perimeter[i].DistanceTo(
					perimeter[next]
				);
		}

		return length;
	}


	private ArrayMesh CreateMesh(
		List<Vector3> vertices,
		List<Vector3> normals,
		List<Vector2> uvs,
		List<int> indices)
	{
		var arrays = new Godot.Collections.Array();

		arrays.Resize((int)Mesh.ArrayType.Max);

		arrays[(int)Mesh.ArrayType.Vertex] =
			vertices.ToArray();

		arrays[(int)Mesh.ArrayType.Normal] =
			normals.ToArray();

		arrays[(int)Mesh.ArrayType.TexUV] =
			uvs.ToArray();

		arrays[(int)Mesh.ArrayType.Index] =
			indices.ToArray();

		var mesh = new ArrayMesh();

		mesh.AddSurfaceFromArrays(
			Mesh.PrimitiveType.Triangles,
			arrays
		);

		return mesh;
	}
}
