using System.Text;

static class PGNParser
{
    public static List<PgnGame> ParseFile(string path)
    {
        return ParseFromLines(File.ReadLines(path));
    }

    public static List<PgnGame> ParseLines(string[] lines)
    {
        return ParseFromLines(lines);
    }
    
    private static List <PgnGame> ParseFromLines(IEnumerable<string> lines)
    {
        List<PgnGame> games = new();
        PgnGame? game = null;
        bool readingMoves = false;
        var moves = new StringBuilder();

        foreach (string rawLine in lines)
        {
            string line = rawLine.TrimEnd();

            if(game == null){
                if(line.StartsWith("[")){

                    game = new PgnGame();
                    readingMoves = false;
                    moves.Clear();
                }
                else
                    continue;
            }

            if(line.Length == 0){
                if(!readingMoves){
                    readingMoves = true;
                }
                else{
                    game.Moves = moves.ToString().Trim();
                    games.Add(game);

                    game = null;
                    readingMoves = false;
                    moves.Clear();
                }
                continue;
                
            }

            if(!readingMoves && line.StartsWith("["))
            {
                int space = line.IndexOf(' ');
                int firstQuote = line.IndexOf('"');
                int lastQuote = line.LastIndexOf('"');

                if(space == -1|| firstQuote == -1|| lastQuote <= firstQuote)
                    continue;

                string tag = line.Substring(1, space -1);
                string value = line.Substring(firstQuote + 1, lastQuote - firstQuote - 1);

                ApplyTag(game, tag, value);
                continue;
            }


            if(readingMoves)
            {
                moves.AppendLine(line);
            }

        }
        if(game!= null){
                game.Moves = moves.ToString().Trim();
                games.Add(game);
            }
            return games;
    }


    private static void ApplyTag(PgnGame game, string tag, string value){
        if(tag == "Event") game.EventName = value;
        else if (tag == "Site") game.Site = string.IsNullOrWhiteSpace(value) ? "?" : value;
        else if (tag == "Round") game.Round = value;
        else if (tag == "White") game.WhitePlayer = value;
        else if (tag == "Black") game.BlackPlayer = value;
        else if (tag == "WhiteElo") game.WhiteElo = int.TryParse(value, out var w) ? w : 0;
        else if (tag == "BlackElo") game.BlackElo = int.TryParse(value, out var b) ? b : 0;
        else if (tag == "Result") game.Result = ConvertResult(value);
        else if (tag == "EventDate") game.EventDate = NormalizeEventDate(value);

    }

    private static char ConvertResult(string r){
        if(r == "1-0") return 'W';
        if(r == "0-1") return 'B';
        return 'D';
    }

    private static string NormalizeEventDate(string d)
    {
        if(d.Contains("?")) return "0000-00-00";
        string[] parts = d.Split('.');
        if(parts.Length != 3) return "0000-00-00";
        if(parts[0].Length != 4 || parts[1].Length != 2|| parts[2].Length !=2){
            return "0000-00-00";    
        }
        return parts[0] + "-" + parts[1]+ "-" + parts[2];
    }




}