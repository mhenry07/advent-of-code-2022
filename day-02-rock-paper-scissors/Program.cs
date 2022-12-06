// Day 2: https://adventofcode.com/2022/day/2

using System;
using System.Linq;

var rounds = Data.Input
    .Split("\r\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    .Select(r => Round.Parse(r))
    .ToArray();

// part 1
var totalScore1 = rounds.Sum(r => r.Score1());
Console.WriteLine($"Part 1: Total score: {totalScore1} points");

// part 2
var totalScore2 = rounds.Sum(r => r.Score2());
Console.WriteLine($"Part 2: Total score: {totalScore2} points");


// implementations

public enum Shape
{
    Rock = 1,
    Paper = 2,
    Scissors = 3
}

public enum Outcome
{
    Lose = 0,
    Draw = 3,
    Win = 6
}

public class Round
{
    public const int WinScore = 6;
    public const int DrawScore = 3;
    public const int LoseScore = 0;

    public Shape Opponent { get; set; }
    public Shape StrategyResponse1 { get; set; }
    public Outcome StrategyOutcome2 { get; set; }

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

        var strategyResponse1 = tokens[1] switch
        {
            "X" => Shape.Rock,
            "Y" => Shape.Paper,
            "Z" => Shape.Scissors,
            _ => throw new InvalidOperationException()
        };

        var strategyOutcome2 = tokens[1] switch
        {
            "X" => Outcome.Lose,
            "Y" => Outcome.Draw,
            "Z" => Outcome.Win,
            _ => throw new InvalidOperationException()
        };

        return new Round
        {
            Opponent = opponent,
            StrategyResponse1 = strategyResponse1,
            StrategyOutcome2 = strategyOutcome2
        };
    }

    public int Score1()
    {
        var shapeScore = (int)StrategyResponse1;
        var outcomeScore = RoundOutcomeScore1();
        return shapeScore + outcomeScore;
    }

    public int Score2()
    {
        var shapeScore = (int)StrategyResponse2;
        var outcomeScore = (int)StrategyOutcome2;
        return shapeScore + outcomeScore;
    }

    public int RoundOutcomeScore1()
    {
        if (Opponent == StrategyResponse1)
            return DrawScore;

        return Opponent switch
        {
            Shape.Rock => StrategyResponse1 switch
            {
                Shape.Paper => WinScore,
                Shape.Scissors => LoseScore,
                _ => throw new InvalidOperationException()
            },
            Shape.Paper => StrategyResponse1 switch
            {
                Shape.Scissors => WinScore,
                Shape.Rock => LoseScore,
                _ => throw new InvalidOperationException()
            },
            Shape.Scissors => StrategyResponse1 switch
            {
                Shape.Rock => WinScore,
                Shape.Paper => LoseScore,
                _ => throw new InvalidOperationException()
            },
            _ => throw new InvalidOperationException()
        };
    }

    public Shape StrategyResponse2
    {
        get
        {
            return StrategyOutcome2 switch
            {
                Outcome.Draw => Opponent,
                Outcome.Win => Opponent switch
                {
                    Shape.Rock => Shape.Paper,
                    Shape.Paper => Shape.Scissors,
                    Shape.Scissors => Shape.Rock,
                    _ => throw new InvalidOperationException()
                },
                Outcome.Lose => Opponent switch
                {
                    Shape.Rock => Shape.Scissors,
                    Shape.Paper => Shape.Rock,
                    Shape.Scissors => Shape.Paper,
                    _ => throw new InvalidOperationException()
                },
                _ => throw new InvalidOperationException()
            };
        }
    }
}
