using Godot;
using System;

public partial class Player : CharacterBody3D
{
    
    private float direction;

    [Export]
    private float walkingSpeed;

    public override void _Ready()
	{
		
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _PhysicsProcess(double delta)
	{
		Vector2 inputVector = Input.GetVector("MovePlayerDown", "MovePlayerUp", "MovePlayerLeft", "MovePlayerRight");

        Velocity = new Vector3(walkingSpeed * inputVector.X * (float)delta, 0f, walkingSpeed * inputVector.Y * (float)delta);
        MoveAndSlide();
        
	}


    public void OnBallDetectionAreaBodyEntered(Node3D body)
    {
        GD.Print(body);
    }
}
