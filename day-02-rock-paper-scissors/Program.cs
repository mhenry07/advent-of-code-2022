// Day 2: https://adventofcode.com/2022/day/2

using System;
using System.Linq;

var rounds = Data.Input
    .Split("\r\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    .Select(r => Round.Parse(r));
var totalScore = rounds.Sum(r => r.Score());

Console.WriteLine($"Total score: {totalScore} points");

public enum Shape
{
    Rock = 1,
    Paper = 2,
    Scissors = 3
}

public class Round
{
    public const int WinScore = 6;
    public const int DrawScore = 3;
    public const int LoseScore = 0;

    public Shape Opponent { get; set; }
    public Shape Response { get; set; }

    public static Round Parse(string line)
    {
        var tokens = line.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var opponent = tokens[0] switch
        {
            "A" => Shape.Rock,
            "B" => Shape.Paper,
            "C" => Shape.Scissors,
            _ => throw new InvalidOperationException()
        };

        var response = tokens[1] switch
        {
            "X" => Shape.Rock,
            "Y" => Shape.Paper,
            "Z" => Shape.Scissors,
            _ => throw new InvalidOperationException()
        };

        return new Round
        {
            Opponent = opponent,
            Response = response
        };
    }

    public int Score()
    {
        var shapeScore = (int)Response;
        var outcomeScore = RoundOutcomeScore();
        return shapeScore + outcomeScore;
    }

    public int RoundOutcomeScore()
    {
        if (Opponent == Response)
            return DrawScore;

        return Opponent switch
        {
            Shape.Rock => Response switch
            {
                Shape.Paper => WinScore,
                Shape.Scissors => LoseScore,
                _ => throw new InvalidOperationException()
            },
            Shape.Paper => Response switch
            {
                Shape.Scissors => WinScore,
                Shape.Rock => LoseScore,
                _ => throw new InvalidOperationException()
            },
            Shape.Scissors => Response switch
            {
                Shape.Rock => WinScore,
                Shape.Paper => LoseScore,
                _ => throw new InvalidOperationException()
            },
            _ => throw new InvalidOperationException()
        };
    }
}
