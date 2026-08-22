using Godot;
using System;

public partial class Ball : RigidBody3D
{
	
	public bool free{
		get;
		set;
	}
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		free = true;
		
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		

		
	}
}
