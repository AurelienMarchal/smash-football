using Godot;
using System;

public partial class Player : CharacterBody3D
{

    

    public int playerNum
    {
        get;
        set;
    }

    public int teamNum
    {
        get;
        set;
    }

    private TeamColor teamColor_;

    public TeamColor teamColor
    {
        get
        {
            return teamColor_;
        }
        set
        {
            teamColor_ = value;
            SetMaterialAccordingToTeamColor();
        }
    }

    private int? controlledByControllerNum_;

    public int? controlledByControllerNum
    {
        get
        {
            return controlledByControllerNum_;
        }
        set
        {
            controlledByControllerNum_ = value;
            if(value == null)
            {
                controlledByControllerLabel3D.Hide();
            }
            else
            {
                controlledByControllerLabel3D.Text = $"P{value + 1}";
                controlledByControllerLabel3D.Show();
            }
        }
    }

    public Vector2 movementInput{
        get;
        set;
    }

    [Signal]
    public delegate void GainControlOfBallEventHandler(int playerNum, int teamNum);

    [ExportGroup("General")]

    [Export]
    private float walkingSpeed;

    [Export]
    private float passShootPower;

    [Export]
    private float controlledBallDistanceToCenter;

    [Export]
    private float controlledBallY;

    [Export(PropertyHint.Range, "1,90,0.1")]
    private float passCandidateMaxAngle;

    [ExportGroup("Rendering")]
    [Export]
    MeshInstance3D[] bodyMeshes;

    [Export]
    Label3D controlledByControllerLabel3D;

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
    Area3D passCandidateDetectionArea;

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

    public override void _Process(double delta)
    {
        
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
        GD.Print($"Setting Material {mat} for {this}");
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

        if (controlledByControllerNum != null)
        {
            if (movementInput != Vector2.Zero)
            {
                var movementInputNormalized = movementInput.Normalized();

                GlobalRotation = new Vector3(
                    0f,
                    Mathf.Atan2(movementInput.Y, movementInput.X),
                    0f
                );

                Velocity = new Vector3(
                    walkingSpeed * movementInputNormalized.Y ,
                    0f,
                    walkingSpeed * movementInputNormalized.X 
                );
                
            }
            else
            {
                Velocity = Vector3.Zero;
            }
        }
        else
        {
            Velocity = Vector3.Zero;
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
            if(body is Player otherPlayer && otherPlayer.teamNum != teamNum) 
            {
                if (otherPlayer.playerState == PlayerState.ControllingBall)
                {
                    GD.Print("Hit player " + otherPlayer);
                    var ball = otherPlayer.controlledBall;
                    otherPlayer.LooseControlOfBall();
                    otherPlayer.StartHitBySlideTackle();
                    TryToGainControlOfBall(ball);
                }
            }
        }

        
        MoveControlledBall();
        
    }

    public void OnMakePassInput()
    {
        if(playerState == PlayerState.ControllingBall)
        {
            var bestCandidate = GetBestPassCandidate();
            
            if(bestCandidate != null)
            {
                Vector3 dir = bestCandidate.GlobalPosition - GlobalPosition;
                var angle =  Mathf.Atan2(dir.X, dir.Z);
                ShootControlledBall(passShootPower, angle);
            }
            else
            {
                ShootControlledBall(passShootPower);
            }
        }
    }

    public void OnSlideTackleInput()
    {
        if(playerState == PlayerState.Moving)
        {
            StartSlideTackle();
        }
    }

    public bool TryToGainControlOfBall(Ball ball)
    {
        if(ball == null)
        {
            return false;
        }

        var didSucceed = ball.TryToBecomeControlledByPlayer(playerNum, teamNum);
        if (didSucceed)
        {
            controlledBall = ball;
            SetCollisionMaskValue(3/*Ball*/, false);
            ballDetectionArea.SetCollisionMaskValue(3/*Ball*/, false);
            //don't change player state yet if slide tackle for example
            playerState = playerState == PlayerState.Moving ? PlayerState.ControllingBall : playerState;
            EmitSignal(SignalName.GainControlOfBall, playerNum, teamNum);
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
    
    #nullable enable
    public Player? GetBestPassCandidate()
    {
        float minDistance = float.MaxValue;
       
        Player? bestCandidate = null;
        var overlappingBodies = passCandidateDetectionArea.GetOverlappingBodies();
        foreach (var body in overlappingBodies)
        {
            if(body is Player otherPlayer && otherPlayer.teamNum == teamNum && otherPlayer.playerNum != playerNum) 
            {
                Vector3 othePlayerLocalPosition = ToLocal(otherPlayer.GlobalPosition);
                float otherPlayerAngle = Mathf.Atan2(othePlayerLocalPosition.X, othePlayerLocalPosition.Z); 
                if(Mathf.Abs(otherPlayerAngle) > (passCandidateMaxAngle * Mathf.Pi /180f))
                {
                    continue;
                }
                float otherPlayerDistance = GlobalPosition.DistanceTo(otherPlayer.GlobalPosition);
                if(otherPlayerDistance < minDistance)
                {
                    minDistance = otherPlayerDistance;
                    bestCandidate = otherPlayer;
                }
            }
        }

        return bestCandidate;
    }
    #nullable disable

    public void ShootControlledBall(float power)
    {
        ShootControlledBall(power, GlobalRotation.Y);
    }

    public void ShootControlledBall(float power, float angleRad)
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
            power * Mathf.Sin(angleRad), 
            0f, 
            power * Mathf.Cos(angleRad)
        );

        GD.Print($"Shooting with impulse {impulse}");

        controlledBall.BecomeFree();

        
        
        timerBeforeAbleToControlBall.Start();

        controlledBall.LinearVelocity = Vector3.Zero;

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


    public override string ToString()
    {
        return $"[Player {playerNum} of team {teamNum}]";
    }

}
