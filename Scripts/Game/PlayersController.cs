using Godot;
using System;
using System.Collections.Generic;

public partial class PlayersController : Node
{
    [ExportGroup("Teams")]
    [Export]
    Player[] playersTeam1;

    [Export]
    Player[] playersTeam2;

    [Export]
    TeamColor colorTeam1;

    [Export]
    TeamColor colorTeam2;

    [Export]
    int[] controllersNumsTeam1;

    [Export]
    int[] controllersNumsTeam2;

    public override void _Ready()
	{
        for (int i = 0; i < playersTeam1.Length; i++)
        {
            var player = playersTeam1[i];
            
            player.teamNum = 1;
            player.playerNum = i;
            player.teamColor = colorTeam1;
            player.GainControlOfBall += OnPlayerGainControlOfBall;
            if(controllersNumsTeam1 != null && i < controllersNumsTeam1.Length)
            {
                player.controlledByControllerNum = controllersNumsTeam1[i];
            }
            else
            {
                player.controlledByControllerNum = null;
            }

        }

        for (int i = 0; i < playersTeam2.Length; i++)
        {
            var player = playersTeam2[i];
            
            player.teamNum = 2;
            player.playerNum = i;
            player.teamColor = colorTeam2;
            player.GainControlOfBall += OnPlayerGainControlOfBall;
            if(controllersNumsTeam2 != null && i < controllersNumsTeam2.Length)
            {
                player.controlledByControllerNum = controllersNumsTeam2[i];
            }
            else
            {
                player.controlledByControllerNum = null;
            }
        }
	}

    public override void _PhysicsProcess(double delta)
    {
        //temp. In the future, have mapped input for four controllers
        //Put that in a function to do the same for every controller
        var movementInputController0 = Input.GetVector(       
            "MovePlayerLeft", 
            "MovePlayerRight",
            "MovePlayerDown", 
            "MovePlayerUp"
        );

        var playerControllerByController0 = GetPlayerControlledByController(0);

        if(playerControllerByController0 == null)
        {
            return;
            
        }
        playerControllerByController0.movementInput = movementInputController0;
    }

    //temp. In the future, have mapped input for four controllers
    public override void _Input(InputEvent @event)
    {   
        //Put that in a function to do the same for every controller
        var playerControllerByController0 = GetPlayerControlledByController(0);
        if(playerControllerByController0 == null)
        {
            return;
        }
        if (@event.IsActionPressed("MakePass"))
        {
            GD.Print("MakePass Input", "Controller 0");
            playerControllerByController0.OnMakePassInput();
            
            
        }
        if (@event.IsActionPressed("SlideTackle"))
        {
            GD.Print("SlideTackle Input", "Controller 1");
            playerControllerByController0.OnSlideTackleInput(); 
        }
    }

    #nullable enable

    public Player? GetPlayer(int playerNum, int teamNum)
    {
        
        switch (teamNum)
        {
            case 1 : 
                if(playerNum > playersTeam1.Length || playerNum < 0)
                {
                    return null;
                }
                return playersTeam1[playerNum];
            case 2 : 
                if(playerNum > playersTeam2.Length || playerNum < 0)
                {
                    return null;
                }
                return playersTeam2[playerNum];
            
            default:
                return null;
        }
    }


    //Can be optimized by having a variable keeping track of player <-> controllerNum relationship
    public Player? GetPlayerControlledByController(int controllerNum)
    {
        if(controllersNumsTeam1 != null)
        {
            foreach (var controllerNumTeam1 in controllersNumsTeam1)
            {
                if(controllerNumTeam1 == controllerNum)
                {
                    foreach (var playerTeam1 in playersTeam1)
                    {
                        if(playerTeam1.controlledByControllerNum == controllerNum)
                        {
                            return playerTeam1;
                        }
                    }
                    return null;
                }
            }
        }
        
        if(controllersNumsTeam2 != null)
        {
            foreach (var controllerNumTeam2 in controllersNumsTeam2)
            {
                if(controllerNumTeam2 == controllerNum)
                {
                    foreach (var playerTeam2 in playersTeam2)
                    {
                        if(playerTeam2.controlledByControllerNum == controllerNum)
                        {
                            return playerTeam2;
                        }
                    }
                    return null;
                }
            }


            
        }
        return null;
    }

    
    public void ChangePlayerControlledByController(int controllerNum, Player newPlayerControlled, Player? formerPlayerControlled)
    {
        if(formerPlayerControlled != null){
            formerPlayerControlled.controlledByControllerNum = null;
            formerPlayerControlled.movementInput = Vector2.Zero;
        }
        
        newPlayerControlled.controlledByControllerNum = controllerNum;
    }
    #nullable disable

    private void OnPlayerGainControlOfBall(int playerNum, int teamNum)
    {   
        
        if(controllersNumsTeam1.Length == 0)
        {
            return;
        }

        Player player = GetPlayer(playerNum, teamNum);
        if(player == null)
        {
            return;
        }

        //temp. In the future, cycle between controller nums
        var controllerNum = controllersNumsTeam1[0];

        var formerPlayerControlled = GetPlayerControlledByController(controllerNum);

        if(formerPlayerControlled.teamNum == player.teamNum)
        {
            ChangePlayerControlledByController(controllerNum, player, formerPlayerControlled);
        }
    }
}
