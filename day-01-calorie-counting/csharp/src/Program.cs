// https://adventofcode.com/2022/day/1

using System.Diagnostics;

var sw = Stopwatch.StartNew();

var input = await File.ReadAllTextAsync("input.txt");
var caloriesPerElf = new List<int>();
var splitOptions = StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries;
var groups = input.Split(new[] { "\r\n\r\n", "\n\n" }, splitOptions);
foreach (var group in groups)
{
    var entries = group.Split(new[] { "\r\n", "\n" }, splitOptions);
    caloriesPerElf.Add(entries.Select(e => int.TryParse(e, out var calories) ? calories : 0).Sum());
}

// part 1
var maxCalories = caloriesPerElf.Max();

// part 2
var top3 = caloriesPerElf.OrderDescending().Take(3);
var sumOfTop3 = top3.Sum();

var elapsedMs = sw.ElapsedMilliseconds;

// part 1
Console.WriteLine($"The most calories an elf is carrying: {maxCalories}");

// part 2
Console.WriteLine($"The top 3 elves are carrying: {sumOfTop3} calories");

Console.WriteLine($"Elapsed: {sw.ElapsedMilliseconds:N2} ms");
