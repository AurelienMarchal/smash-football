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
	public int controlledByTeam{
		get;
		set;
	}
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		free = true;
		controlledByPlayer = -1;
		controlledByTeam = -1;
		FreezeMode = FreezeModeEnum.Kinematic;
		
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		

		
	}

	public bool TryToBecomeControlledByPlayer(int playerNum, int teamNum)
	{	
		if(controlledByPlayer >= 0)
		{
			return false;
		}
		free = false;
		controlledByPlayer = playerNum;
		controlledByTeam = teamNum;
		Freeze = true;

		GD.Print($"Ball Controlled by [Player {playerNum} of team {teamNum}]");

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
