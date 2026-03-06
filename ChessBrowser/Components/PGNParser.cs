static class PGNParser
{
    public IEnumerable<PgnGame> ParseFile(string path)
    {
        using var reader = new StreamReader(path);

        while (!reader.EndOfStream)
        {
            string? line = reader.ReadLine();

            if (line == null) break;

            if (line.StartsWith("["))
            {
                // when we have a tag
            }
            else if (line.Length == 0)
            {
                // blank transition between sections
            }
            else
            {
                // handles moves?
            }
        }
    }

}