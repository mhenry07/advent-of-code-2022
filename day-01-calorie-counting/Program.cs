﻿// https://adventofcode.com/2022/day/1

var caloriesPerElf = new List<int>();
var splitOptions = StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries;
var groups = Data.Input.Split("\r\n\r\n", splitOptions);
foreach (var group in groups)
{
    var entries = group.Split("\r\n", splitOptions);
    caloriesPerElf.Add(entries.Select(e => int.TryParse(e, out var calories) ? calories : 0).Sum());
}

var maxCalories = caloriesPerElf.Max();

Console.WriteLine($"The most calories an elf is carrying: {maxCalories}");

// https://adventofcode.com/2022/day/1#part2

var top3 = caloriesPerElf.OrderDescending().Take(3);
var sumOfTop3 = top3.Sum();

Console.WriteLine($"The top 3 elves are carrying: {sumOfTop3} calories");
