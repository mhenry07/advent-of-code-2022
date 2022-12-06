// Day 3: Rucksack Reorganization
// https://adventofcode.com/2022/day/3

using System;
using System.Linq;

var rucksacks = Data.Input
    .Split("\r\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

int totalPriority = 0;
foreach (var rucksack in rucksacks)
{
    var compartment1 = rucksack.Substring(0, rucksack.Length / 2);
    var compartment2 = rucksack.Substring(rucksack.Length / 2);
    var commonItem = compartment1.Intersect(compartment2).Single();
    var priority = Priority(commonItem);
    totalPriority += priority;
    // Console.WriteLine($"1: {compartment1}, 2: {compartment2}, common item: '{commonItem}', priority: {priority}");
}

// part 1
Console.WriteLine($"Sum of common priorities: {totalPriority}");

int Priority(char item)
{
    return item switch
    {
        >= 'a' and <= 'z' => item - 'a' + 1,
        >= 'A' and <= 'Z' => item - 'A' + 27,
        _ => throw new ArgumentOutOfRangeException(nameof(item))
    };
}
