// Day 3: Rucksack Reorganization
// https://adventofcode.com/2022/day/3

using System.Diagnostics;

var sw = Stopwatch.StartNew();

var rucksacks = await File.ReadAllLinesAsync("input.txt");

int badgeTotalPriority = 0;
int totalPriority = 0;
for (int i = 0; i < rucksacks.Length; i++)
{
    var rucksack = rucksacks[i];
    var compartment1 = rucksack.Substring(0, rucksack.Length / 2);
    var compartment2 = rucksack.Substring(rucksack.Length / 2);
    var commonItem = compartment1.Intersect(compartment2).Single();
    var priority = Priority(commonItem);
    totalPriority += priority;
    // Console.WriteLine($"1: {compartment1}, 2: {compartment2}, common item: '{commonItem}', priority: {priority}");

    // part 2
    if (i % 3 == 2)
    {
        var badge = rucksacks[i - 2].Intersect(rucksacks[i - 1]).Intersect(rucksack).Single();
        var badgePriority = Priority(badge);
        badgeTotalPriority += badgePriority;
    }
}

sw.Stop();

// part 1
Console.WriteLine($"Sum of common priorities: {totalPriority}");

// part 2
Console.WriteLine($"Sum of badge priorities: {badgeTotalPriority}");

Console.WriteLine($"Elapsed: {sw.ElapsedMilliseconds:N2} ms for {rucksacks.Length:N0} records");

int Priority(char item)
{
    return item switch
    {
        >= 'a' and <= 'z' => item - 'a' + 1,
        >= 'A' and <= 'Z' => item - 'A' + 27,
        _ => throw new ArgumentOutOfRangeException(nameof(item))
    };
}
