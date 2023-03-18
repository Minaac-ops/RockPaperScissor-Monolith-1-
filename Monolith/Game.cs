using System;
using System.Collections.Generic;
using System.Linq;
using Events;
using Monitoring;

namespace Monolith;

public class Game
{
    private readonly Dictionary<Guid, GameModel?> _games = new();
    private readonly IPlayer _player1 = new RandomPlayer();
    private readonly IPlayer _player2 = new CopyPlayer();
    
    public void Start()
    {
        Guid gameId = Guid.NewGuid();
        using (var activity = MonitorService.ActivitySource.StartActivity())
        {
            _games.Add(gameId, new GameModel {GameId = gameId});
        
            var startEvent = new GameStartedEvent { GameId = gameId };
        
            MonitorService.Log.Information("Game with gameId "+gameId+ " has started!");

            var p1Event = _player1.MakeMove(startEvent);
            MonitorService.Log.Information(p1Event.PlayerId + " made a move in game with gameId " + p1Event.GameId + ". the move was " + p1Event.Move);
            
            ReceivePlayerEvent(p1Event);
        
            var p2Event = _player2.MakeMove(startEvent);
            MonitorService.Log.Information(p2Event.PlayerId + " made a move in game with gameId " + p2Event.GameId + ". the move was " + p2Event.Move);


            ReceivePlayerEvent(p2Event);
        }
        
        
    }

    public string DeclareWinner(KeyValuePair<string, Move> p1, KeyValuePair<string, Move> p2)
    {
        string? winner = null;
        using (var activity = MonitorService.ActivitySource.StartActivity())
        {
            switch (p1.Value)
            {
                case Move.Rock:
                    winner = p2.Value switch
                    {
                        Move.Paper => p2.Key,
                        Move.Scissor => p1.Key,
                        _ => winner
                    };
                    break;
                case Move.Paper:
                    winner = p2.Value switch
                    {
                        Move.Rock => p1.Key,
                        Move.Scissor => p2.Key,
                        _ => winner
                    };
                    break;
                case Move.Scissor:
                    winner = p2.Value switch
                    {
                        Move.Rock => p2.Key,
                        Move.Paper => p1.Key,
                        _ => winner
                    };
                    break;
            }
            MonitorService.Log.Information("The winner was "+winner);
            return winner ?? "Tie";
        }
    }

    public void ReceivePlayerEvent(PlayerMovedEvent e)
    {
        if (_games.TryGetValue(e.GameId, out var game))
        {
            lock (game)
            {
                game.Moves.Add(e.PlayerId, e.Move);
                if (game.Moves.Values.Count == 2)
                {
                    KeyValuePair<string?, Move> p1 = game.Moves.First()!;
                    KeyValuePair<string?, Move> p2 = game.Moves.Skip(1).First()!;

                    var finishedEvent = PrepareWinnerAnnouncement(game, p1, p2);
                    _player1.ReceiveResult(finishedEvent);
                    _player2.ReceiveResult(finishedEvent);

                    _games.Remove(game.GameId);
                }
            }
        }
    }

    public GameFinishedEvent PrepareWinnerAnnouncement(GameModel game, KeyValuePair<string?, Move> p1, KeyValuePair<string?, Move> p2)
    {
        
        var finishedEvent = new GameFinishedEvent
        {
            GameId = game.GameId,
            Moves = game.Moves,
            WinnerId = DeclareWinner(p1!, p2!)
        };
        return finishedEvent;
    }
}

public class GameModel
{
    public Guid GameId { get; set; }
    public Dictionary<string, Move> Moves { get; set; } = new();
}