using Godot;

[Tool]
public partial class GenerateField : Node3D
{
	private float _width = 10.0f;
	private float _height = 6.0f;
	private float _cornerRadius = 1.0f;
	private int _segments = 8;

	private float _wallHeight = 2.5f;

	private float _holeWidth = 1.5f;
	private float _holeHeight = 1.5f;

	private float _colliderWallHeight = 2.5f;


	[Export(PropertyHint.Range, "0.01,100.0,0.01")]
	public float Width
	{
		get => _width;
		set
		{
			_width = Mathf.Max(0.01f, value);
			UpdateField();
		}
	}


	[Export(PropertyHint.Range, "0.01,100.0,0.01")]
	public float Height
	{
		get => _height;
		set
		{
			_height = Mathf.Max(0.01f, value);
			UpdateField();
		}
	}


	[Export(PropertyHint.Range, "0.0,50.0,0.01")]
	public float CornerRadius
	{
		get => _cornerRadius;
		set
		{
			_cornerRadius = Mathf.Max(0.0f, value);
			UpdateField();
		}
	}


	[Export(PropertyHint.Range, "1,64,1")]
	public int Segments
	{
		get => _segments;
		set
		{
			_segments = Mathf.Max(1, value);
			UpdateField();
		}
	}


	[ExportGroup("Wall Mesh")]

	[Export(PropertyHint.Range, "0.0,20.0,0.01")]
	public float WallHeight
	{
		get => _wallHeight;
		set
		{
			_wallHeight = Mathf.Max(0.0f, value);
			UpdateField();
		}
	}


	[Export(PropertyHint.Range, "0.0,100.0,0.01")]
	public float HoleWidth
	{
		get => _holeWidth;
		set
		{
			_holeWidth = Mathf.Max(0.0f, value);
			UpdateField();
		}
	}


	[Export(PropertyHint.Range, "0.0,20.0,0.01")]
	public float HoleHeight
	{
		get => _holeHeight;
		set
		{
			_holeHeight = Mathf.Max(0.0f, value);
			UpdateField();
		}
	}


	[ExportGroup("Wall Collision")]

	[Export(PropertyHint.Range, "0.0,20.0,0.01")]
	public float ColliderWallHeight
	{
		get => _colliderWallHeight;
		set
		{
			_colliderWallHeight = Mathf.Max(0.0f, value);
			UpdateField();
		}
	}


	public override void _Ready()
	{
		UpdateField();
	}


	private void UpdateField()
	{
		if (!IsInsideTree())
			return;

		GenerateMeshes();
		GenerateCollisions();
	}
}
