namespace CES.BusinessTier.Utilities;

public static class Commons
{
    public static string RemoveSpaces(string input)
    {
        return input.Replace(" ", string.Empty);
    }
}