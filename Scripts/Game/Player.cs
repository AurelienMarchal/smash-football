using Godot;
using System;

public partial class Player : CharacterBody3D
{

    [Export]
    public bool controlledByInput;

    [Export]
    private float walkingSpeed;

    [Export]
    private float passShootPower;

    [Export]
    private float controlledBallDistanceToCenter;

    [Export]
    private float controlledBallY;

    [Export]
    private int playerNum;

    Timer timerBeforeAbleToControlBall;

    Area3D ballDetectionArea;

    public Ball controlledBall{
		get;
		private set;
	}

    public override void _Ready()
	{
		controlledBall = null;
        timerBeforeAbleToControlBall = GetNode<Timer>("./TimerBeforeAbleToControlBall");
        ballDetectionArea = GetNode<Area3D>("./BallDetectionArea");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _PhysicsProcess(double delta)
	{
        if(controlledBall == null)
        {
            var overlappingBodies = ballDetectionArea.GetOverlappingBodies();
            foreach (var body in overlappingBodies)
            {
                if(body is Ball ball) 
                {
                    TryToGainControlOfBall(ball);
                }
            }
        }

        if (controlledByInput)
        {
            Vector2 inputVector = Input.GetVector(
                
                "MovePlayerLeft", 
                "MovePlayerRight",
                "MovePlayerDown", 
                "MovePlayerUp"
            );
            if (inputVector != Vector2.Zero)
            {
                var inputVectorNormalized = inputVector.Normalized();

                GlobalRotation = new Vector3(
                    0f,
                    Mathf.Atan2(inputVector.Y, inputVector.X),
                    0f
                );

                Velocity = new Vector3(
                    walkingSpeed * inputVectorNormalized.Y * (float)delta,
                    0f,
                    walkingSpeed * inputVectorNormalized.X * (float)delta
                );
            }
            else
            {
                Velocity = Vector3.Zero;
            }
        }
        else
        {
            
        }

        MoveAndSlide();
        MoveControlledBall();

	}

    public override void _Input(InputEvent @event)
    {

        if (!controlledByInput)
        {
            return;
        }
       
        if (@event.IsActionPressed("MakePass"))
        {
            GD.Print("MakePass");
            ShootControlledBall(passShootPower);
        }
    }

    public void TryToGainControlOfBall(Ball ball)
    {
        var didSucceed = ball.TryToBecomeControlledByPlayer(playerNum);
        if (didSucceed)
        {
             controlledBall = ball;
            SetCollisionMaskValue(3/*Ball*/, false);
            ballDetectionArea.SetCollisionMaskValue(3/*Ball*/, false);
        }
       
    }

    public void LooseControlOfBall()
    {
        controlledBall.BecomeFree();
        controlledBall = null;
        timerBeforeAbleToControlBall.Start();
    }

    public void OnTimerBeforeAbleToControlBallTimeout()
    {
        GD.Print($"Setting back collision with ball");
        SetCollisionMaskValue(3/*Ball*/, true);
        ballDetectionArea.SetCollisionMaskValue(3/*Ball*/, true);
    }

    public void MoveControlledBall()
    {
        if(controlledBall == null)
        {
            return;
        }

        controlledBall.GlobalPosition = 
            new Vector3(
                GlobalPosition.X, 
                controlledBallY, 
                GlobalPosition.Z
            ) + 
            new Vector3(
                controlledBallDistanceToCenter * Mathf.Sin(GlobalRotation.Y), 
                0f, 
                controlledBallDistanceToCenter * Mathf.Cos(GlobalRotation.Y)
            );
    }

    public void ShootControlledBall(float power)
    {
        if(controlledBall == null)
        {
            return;
        }

        if(power <= 0f)
        {
            return;
        }

        var impulse = new Vector3(
            power * Mathf.Sin(GlobalRotation.Y), 
            0f, 
            power * Mathf.Cos(GlobalRotation.Y)
        );

        GD.Print($"Shooting with impulse {impulse}");

        controlledBall.BecomeFree();
        
        timerBeforeAbleToControlBall.Start();

        controlledBall.ApplyCentralImpulse(impulse);
        
        controlledBall = null;
        
    }

    
}
