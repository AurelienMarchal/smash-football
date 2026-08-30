using Godot;
using System;

public partial class Player : CharacterBody3D
{

    [ExportGroup("General")]

    [Export]
    private int playerNum;

    [Export]
    private int teamNum;

    [Export]
    private TeamColor teamColor;

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

    [ExportGroup("Rendering")]
    [Export]
    MeshInstance3D[] bodyMeshes;

    [ExportSubgroup("Materials")]
    [Export]
    Material blueMat;
    [Export]
    Material redMat;
    [Export]
    Material greenMat;
    [Export]
    Material yellowMat;

    [ExportGroup("Animation")]
    [Export]
    private AnimationTree animationTree;

    private AnimationNodeStateMachinePlayback stateMachinePlayback;

    [ExportSubgroup("SlideTackle")]

    [Export]
    private Curve slideTackleMovementCurve;

    [Export]
    private float slideTackleDuration = 0.8f;

    private float slideTackleTimer = 0f;

    public PlayerState playerState
    {
        get;
        private set;
    }

    Timer timerBeforeAbleToControlBall;

    [ExportGroup("CollidersAndAreas")]

    [Export]
    Area3D ballDetectionArea;

    [Export]
    Area3D rightFootCollisionArea;

    public Ball controlledBall{
		get;
		private set;
	}

    public override void _Ready()
	{
		controlledBall = null;
        playerState = PlayerState.Moving;
        stateMachinePlayback = (AnimationNodeStateMachinePlayback)animationTree.Get("parameters/playback");
        timerBeforeAbleToControlBall = GetNode<Timer>("./TimerBeforeAbleToControlBall");
        SetMaterialAccordingToTeamColor();

	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _PhysicsProcess(double delta)
	{
        switch (playerState)
        {
            case PlayerState.Moving:
                HandleNormalMovement(delta);
                break;

            case PlayerState.ControllingBall:
                HandleNormalMovement(delta);
                break;

            case PlayerState.SlideTackle:
                HandleSlideTackle(delta);
                break;
        }

	}

    public void SetMaterialAccordingToTeamColor()
    {
        Material mat = null;
        switch (teamColor)
        {
            case TeamColor.Blue :
                mat = blueMat;
                break;
            case TeamColor.Red  :
                mat = redMat;
                break;
            case TeamColor.Green  :
                mat = greenMat;
                break;
            case TeamColor.Yellow:
                mat = yellowMat;
                break;
        }

        if(mat == null)
        {
            return;
        }
        foreach (MeshInstance3D mesh in bodyMeshes)
        {
            
            mesh.SetSurfaceOverrideMaterial(0, mat);
            
        }
    }

    private void HandleNormalMovement(double delta)
    {
        //Must be state dependant
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
                    walkingSpeed * inputVectorNormalized.Y ,
                    0f,
                    walkingSpeed * inputVectorNormalized.X 
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

    private void HandleSlideTackle(double delta)
    {
        
        slideTackleTimer += (float)delta;

        float progress = Mathf.Clamp(
            slideTackleTimer / slideTackleDuration,
            0f,
            1f
        );

        float speed = slideTackleMovementCurve.Sample(progress);

        Velocity = speed * Transform.Basis.Z;

        MoveAndSlide();

        var overlappingBodies = rightFootCollisionArea.GetOverlappingBodies();
        foreach (var body in overlappingBodies)
        {
            if(body is Player otherPlayer && otherPlayer.playerNum != playerNum) 
            {
                if (otherPlayer.playerState == PlayerState.ControllingBall)
                {
                    GD.Print("Hit player " + otherPlayer.playerNum);
                    var ball = otherPlayer.controlledBall;
                    otherPlayer.LooseControlOfBall();
                    otherPlayer.StartHitBySlideTackle();
                    TryToGainControlOfBall(ball);
                }
            }
        }

        
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
            if(playerState == PlayerState.ControllingBall)
            {
                ShootControlledBall(passShootPower);
            }
            
        }
        if (@event.IsActionPressed("SlideTackle"))
        {
            if(playerState == PlayerState.Moving)
            {
                GD.Print("SlideTackle");
                StartSlideTackle();
            }
            
        }
    }

    public bool TryToGainControlOfBall(Ball ball)
    {
        if(ball == null)
        {
            return false;
        }

        var didSucceed = ball.TryToBecomeControlledByPlayer(playerNum);
        if (didSucceed)
        {
            controlledBall = ball;
            SetCollisionMaskValue(3/*Ball*/, false);
            ballDetectionArea.SetCollisionMaskValue(3/*Ball*/, false);
            playerState = PlayerState.ControllingBall;
        }

        return didSucceed;
       
    }

    public void LooseControlOfBall()
    {
        controlledBall.BecomeFree();
        controlledBall = null;
        playerState = PlayerState.Moving;
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

        playerState = PlayerState.Moving;
        
        controlledBall = null;
        
    }

    private void StartSlideTackle()
    {
        playerState = PlayerState.SlideTackle;

        stateMachinePlayback.Travel("Special_slide_tackle");

        slideTackleTimer = 0f;
    }


    public void EndSlideTackle()
    {
        playerState = controlledBall == null ? PlayerState.Moving : PlayerState.ControllingBall;
        Velocity = Vector3.Zero;
    }

    private void StartHitBySlideTackle()
    {
        playerState = PlayerState.HitBySlideTackle;

        stateMachinePlayback.Travel("Hit_tackled");

        SetCollisionMaskValue(2/*Player*/, false);
        SetCollisionLayerValue(2 /*Player*/, false);
    }

    private void EndHitBySlideTackle()
    {
        SetCollisionMaskValue(2/*Player*/, true);
        SetCollisionLayerValue(2 /*Player*/, true);
        playerState = PlayerState.Moving;
    }


    public void OnAnimationTreeAnimationFinished(string animName)
    {
        GD.Print("Animation End " + animName);
        switch (animName)
        {
            case "Special/slide_tackle" :
                EndSlideTackle();
                break;
            case "Hit/tackled":
                EndHitBySlideTackle();
                break;
        }
    }
}
