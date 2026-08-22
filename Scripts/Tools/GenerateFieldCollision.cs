using Godot;
using System.Collections.Generic;


public partial class GenerateField
{
	private StaticBody3D _collisionBody;

	private readonly Dictionary<string, CollisionShape3D>
		_collisionShapes = new();

	private readonly HashSet<string>
		_usedCollisionNames = new();


	private void GenerateCollisions()
	{
		CreateCollisionBody();

		_usedCollisionNames.Clear();

		List<Vector2> perimeter = GeneratePerimeter();

		GenerateFloorCollision();
		GenerateCeilingCollision();

		GenerateWallCollisions(perimeter);
		GenerateCornerCollisions(perimeter);

		RemoveUnusedCollisionShapes();
	}


	// =========================================================
	// COLLISION BODY
	// =========================================================

	private void CreateCollisionBody()
	{
		_collisionBody =
			GetNodeOrNull<StaticBody3D>("Collisions");

		if (_collisionBody == null)
		{
			_collisionBody = new StaticBody3D();
			_collisionBody.Name = "Collisions";

			AddChild(_collisionBody);

			if (Engine.IsEditorHint())
				_collisionBody.Owner =
					GetTree().EditedSceneRoot;
		}
	}


	// =========================================================
	// COLLISION SHAPE REUSE
	// =========================================================

	private CollisionShape3D GetCollisionShape(
		string name)
	{
		_usedCollisionNames.Add(name);

		if (_collisionShapes.TryGetValue(
			name,
			out CollisionShape3D existing))
		{
			return existing;
		}


		/*
		 * The dictionary may be empty after a scene reload,
		 * so check the actual node tree as well.
		 */
		existing =
			_collisionBody.GetNodeOrNull<
				CollisionShape3D>(name);

		if (existing != null)
		{
			_collisionShapes[name] = existing;
			return existing;
		}


		var collision =
			new CollisionShape3D();

		collision.Name = name;

		_collisionBody.AddChild(collision);

		if (Engine.IsEditorHint())
			collision.Owner =
				GetTree().EditedSceneRoot;

		_collisionShapes[name] = collision;

		return collision;
	}


	private void RemoveUnusedCollisionShapes()
	{
		var toRemove =
			new List<string>();

		foreach (var pair in _collisionShapes)
		{
			if (!_usedCollisionNames.Contains(pair.Key))
				toRemove.Add(pair.Key);
		}


		foreach (string name in toRemove)
		{
			CollisionShape3D collision =
				_collisionShapes[name];

			if (GodotObject.IsInstanceValid(collision))
				collision.Free();

			_collisionShapes.Remove(name);
		}
	}


	// =========================================================
	// FLOOR
	// =========================================================

	private void GenerateFloorCollision()
	{
		CollisionShape3D collision =
			GetCollisionShape("Floor");


		WorldBoundaryShape3D shape =
			collision.Shape as WorldBoundaryShape3D;

		if (shape == null)
		{
			shape = new WorldBoundaryShape3D();
			collision.Shape = shape;
		}


		/*
		 * Floor at Y = 0.
		 */
		shape.Plane = new Plane(
			Vector3.Up,
			0.0f
		);


		collision.Position = Vector3.Zero;
		collision.Rotation = Vector3.Zero;
		collision.Scale = Vector3.One;
	}


	// =========================================================
	// CEILING
	// =========================================================

	private void GenerateCeilingCollision()
	{
		if (ColliderWallHeight <= 0.0f)
			return;


		CollisionShape3D collision =
			GetCollisionShape("Ceiling");


		WorldBoundaryShape3D shape =
			collision.Shape as WorldBoundaryShape3D;

		if (shape == null)
		{
			shape = new WorldBoundaryShape3D();
			collision.Shape = shape;
		}


		/*
		 * Ceiling at ColliderWallHeight.
		 *
		 * Downward normal means the valid side
		 * of the boundary is below the ceiling.
		 */
		shape.Plane = new Plane(
			Vector3.Down,
			-ColliderWallHeight
		);


		collision.Position = Vector3.Zero;
		collision.Rotation = Vector3.Zero;
		collision.Scale = Vector3.One;
	}


	// =========================================================
	// WALLS
	// =========================================================

	private void GenerateWallCollisions(
		List<Vector2> perimeter)
	{
		for (int i = 0; i < perimeter.Count; i++)
		{
			int nextIndex =
				(i + 1) % perimeter.Count;

			Vector2 current =
				perimeter[i];

			Vector2 next =
				perimeter[nextIndex];

			Vector2 direction =
				(next - current).Normalized();

			float segmentLength =
				current.DistanceTo(next);


			/*
			* Straight wall running along Width.
			*
			* These are the two walls that can have
			* openings.
			*/
			bool isWidthWall =
				Mathf.Abs(direction.X) > 0.999f &&
				Mathf.Abs(current.Y - next.Y) < 0.0001f;


			/*
			* Straight wall running along Height.
			*
			* These two walls are always solid.
			*
			* Rounded corner segments are NOT included
			* because their direction isn't exactly vertical.
			*/
			bool isHeightWall =
				Mathf.Abs(direction.Y) > 0.999f &&
				Mathf.Abs(current.X - next.X) < 0.0001f;


			// =====================================================
			// SOLID HEIGHT WALL
			// =====================================================

			if (isHeightWall)
			{
				GenerateBoxWallCollision(
					$"Wall_{i}",
					current,
					next,
					0.0f,
					ColliderWallHeight
				);

				continue;
			}


			// =====================================================
			// WIDTH WALL
			// =====================================================

			if (isWidthWall)
			{
				if (HoleWidth <= 0.0f)
				{
					GenerateBoxWallCollision(
						$"Wall_{i}",
						current,
						next,
						0.0f,
						ColliderWallHeight
					);
				}
				else
				{
					GenerateHollowWallCollisions(
						i,
						current,
						next,
						segmentLength
					);
				}

				continue;
			}
		}
	}


	private void GenerateHollowWallCollisions(
		int wallIndex,
		Vector2 current,
		Vector2 next,
		float segmentLength)
	{
		Vector2 direction =
			(next - current).Normalized();


		float holeWidth =
			Mathf.Min(
				HoleWidth,
				segmentLength
			);


		float holeStart =
			(segmentLength - holeWidth) * 0.5f;

		float holeEnd =
			holeStart + holeWidth;


		// -----------------------------------------------------
		// LEFT
		// -----------------------------------------------------

		if (holeStart > 0.001f)
		{
			GenerateBoxWallCollision(
				$"Wall_{wallIndex}_Left",
				current,
				current + direction * holeStart,
				0.0f,
				ColliderWallHeight
			);
		}


		// -----------------------------------------------------
		// RIGHT
		// -----------------------------------------------------

		float rightLength =
			segmentLength - holeEnd;

		if (rightLength > 0.001f)
		{
			GenerateBoxWallCollision(
				$"Wall_{wallIndex}_Right",
				current + direction * holeEnd,
				next,
				0.0f,
				ColliderWallHeight
			);
		}


		// -----------------------------------------------------
		// ABOVE HOLE
		// -----------------------------------------------------

		if (HoleHeight < ColliderWallHeight)
		{
			GenerateBoxWallCollision(
				$"Wall_{wallIndex}_AboveHole",
				current + direction * holeStart,
				current + direction * holeEnd,
				HoleHeight,
				ColliderWallHeight
			);
		}
	}


	// =========================================================
	// BOX WALL
	// =========================================================

	private void GenerateBoxWallCollision(
		string name,
		Vector2 start,
		Vector2 end,
		float bottomHeight,
		float topHeight)
	{
		float length =
			start.DistanceTo(end);

		if (length <= 0.0001f)
			return;


		float height =
			topHeight - bottomHeight;

		if (height <= 0.0001f)
			return;


		CollisionShape3D collision =
			GetCollisionShape(name);


		BoxShape3D shape =
			collision.Shape as BoxShape3D;

		if (shape == null)
		{
			shape = new BoxShape3D();
			collision.Shape = shape;
		}


		const float collisionThickness = 0.05f;


		shape.Size = new Vector3(
			length,
			height,
			collisionThickness
		);


		Vector2 midpoint =
			(start + end) * 0.5f;


		Vector2 direction =
			(end - start).Normalized();


		collision.Position =
			new Vector3(
				midpoint.X,
				bottomHeight + height * 0.5f,
				midpoint.Y
			);


		/*
		 * Local X follows the wall.
		 * Local Z is the thickness.
		 */
		collision.Rotation =
			new Vector3(
				0.0f,
				-Mathf.Atan2(
					direction.Y,
					direction.X
				),
				0.0f
			);


		collision.Scale = Vector3.One;
	}


	// =========================================================
	// ROUNDED CORNERS
	// =========================================================

	private void GenerateCornerCollisions(
		List<Vector2> perimeter)
	{
		/*
		 * Each corner contains Segments individual
		 * convex pieces.
		 *
		 * The perimeter contains 4 corners, each with
		 * Segments + 1 points.
		 */
		int pointsPerCorner =
			Segments + 1;


		for (int corner = 0; corner < 4; corner++)
		{
			int startIndex =
				corner * pointsPerCorner;


			for (int i = 0; i < Segments; i++)
			{
				Vector2 a =
					perimeter[startIndex + i];

				Vector2 b =
					perimeter[startIndex + i + 1];


				AddCornerCollisionPiece(
					corner,
					i,
					a,
					b
				);
			}
		}
	}


	private void AddCornerCollisionPiece(
		int cornerIndex,
		int segmentIndex,
		Vector2 a,
		Vector2 b)
	{
		Vector2 direction =
			(b - a).Normalized();


		Vector2 outward =
			new Vector2(
				direction.Y,
				-direction.X
			).Normalized();


		const float collisionThickness = 0.05f;


		Vector2 innerA =
			a - outward * collisionThickness;

		Vector2 innerB =
			b - outward * collisionThickness;


		var points = new Vector3[]
		{
			// Bottom outer
			new Vector3(
				a.X,
				0.0f,
				a.Y
			),

			// Bottom outer
			new Vector3(
				b.X,
				0.0f,
				b.Y
			),

			// Bottom inner
			new Vector3(
				innerB.X,
				0.0f,
				innerB.Y
			),

			// Bottom inner
			new Vector3(
				innerA.X,
				0.0f,
				innerA.Y
			),

			// Top outer
			new Vector3(
				a.X,
				ColliderWallHeight,
				a.Y
			),

			// Top outer
			new Vector3(
				b.X,
				ColliderWallHeight,
				b.Y
			),

			// Top inner
			new Vector3(
				innerB.X,
				ColliderWallHeight,
				innerB.Y
			),

			// Top inner
			new Vector3(
				innerA.X,
				ColliderWallHeight,
				innerA.Y
			)
		};


		string name =
			$"Corner_{cornerIndex}_{segmentIndex}";


		CollisionShape3D collision =
			GetCollisionShape(name);


		ConvexPolygonShape3D shape =
			collision.Shape as ConvexPolygonShape3D;


		if (shape == null)
		{
			shape =
				new ConvexPolygonShape3D();

			collision.Shape = shape;
		}


		/*
		 * Godot C# uses Vector3[] for the
		 * PackedVector3Array property.
		 */
		shape.Points = points;


		collision.Position = Vector3.Zero;
		collision.Rotation = Vector3.Zero;
		collision.Scale = Vector3.One;
	}
}
