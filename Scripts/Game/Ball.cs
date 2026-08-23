using Godot;
using System;

public partial class Ball : RigidBody3D
{
	
	public bool free{
		get;
		set;
	}

	public int controlledByPlayer{
		get;
		set;
	}
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		free = true;
		controlledByPlayer = -1;
		FreezeMode = FreezeModeEnum.Kinematic;
		
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		

		
	}

	public bool TryToBecomeControlledByPlayer(int playerNum)
	{	
		if(controlledByPlayer >= 0)
		{
			return false;
		}
		free = false;
		controlledByPlayer = playerNum;
		Freeze = true;

		GD.Print($"Ball Controlled by player {playerNum}");

		return true;
	}

	public void BecomeFree()
	{
		free = true;
		Freeze = false;
		controlledByPlayer = -1;
		GD.Print("Ball Free");
	}
}
