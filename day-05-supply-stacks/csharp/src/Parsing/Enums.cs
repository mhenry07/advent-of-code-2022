namespace Parsing;

internal enum ParseState
{
    StartingStacks,
    RearrangementProcedure
}

internal enum StacksParseState
{
    Ids,
    Crates
}

internal enum StackRowType
{
    Unknown,
    Crates,
    Ids
}
