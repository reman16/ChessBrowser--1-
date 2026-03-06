using Microsoft.AspNetCore.Components.Forms;
using System.Diagnostics;
using MySql.Data.MySqlClient;

namespace ChessBrowser.Components.Pages
{
  public partial class ChessBrowser
  {
    /// <summary>
    /// Bound to the Unsername form input
    /// </summary>
    private string Username = "";

    /// <summary>
    /// Bound to the Password form input
    /// </summary>
    private string Password = "";

    /// <summary>
    /// Bound to the Database form input
    /// </summary>
    private string Database = "";

    /// <summary>
    /// Represents the progress percentage of the current
    /// upload operation. Update this value to update 
    /// the progress bar.
    /// </summary>
    private int    Progress = 0;

    /// <summary>
    /// This method runs when a PGN file is selected for upload.
    /// Given a list of lines from the selected file, parses the 
    /// PGN data, and uploads each chess game to the user's database.
    /// </summary>
    /// <param name="PGNFileLines">The lines from the selected file</param>
    private async Task InsertGameData(string[] PGNFileLines)
    {
      // This will build a connection string to your user's database on atr,
      // assuimg you've filled in the credentials in the GUI
      string connection = GetConnectionString();

      List<PgnGame> games = PGNParser.ParseLines(PGNFileLines);

      using (MySqlConnection conn = new MySqlConnection(connection))
      {
        try
        {
          // Open a connection
          conn.Open();

          for(int i = 0; i < games.Count; i++)
          {
            PgnGame game = games[i];

            int whiteID = UpdatePlayerInfo(conn, game.WhitePlayer, game.WhiteElo);
            int blackID = UpdatePlayerInfo(conn, game.BlackPlayer, game.BlackElo);
            int eventID = UpdateEventInfo(conn, game.EventName, game.Site, game.EventDate);

            InsertSingleGame(conn, game, whiteID, blackID, eventID);

            Progress = (i+1) * 100 / games.Count;
            await InvokeAsync(StateHasChanged);
          }
        
        }
        catch (Exception e)
        {
          System.Diagnostics.Debug.WriteLine(e.Message);
        }
      }

    }


    private int UpdatePlayerInfo(MySqlConnection conn, string playerName, int elo)
    {
      using (MySqlCommand checkCmd = new MySqlCommand(
      "SELECT pID, Elo FROM Players WHERE Name = @name;", conn))
      {
        checkCmd.Parameters.AddWithValue("@name", playerName);
        
      using(MySqlDataReader reader = checkCmd.ExecuteReader())
      {
      if(reader.Read())
      {
        int playerID = Convert.ToInt32(reader["pID"]);
        int currentElo = reader["Elo"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Elo"]);

        reader.Close();

        if(elo > currentElo)
        {
          using MySqlCommand updateCmd = new MySqlCommand(
            "UPDATE Players SET Elo = @elo WHERE pID = @id;", conn);
          updateCmd.Parameters.AddWithValue("@elo", elo);
          updateCmd.Parameters.AddWithValue("@id", playerID);
          updateCmd.ExecuteNonQuery();
        }
        return playerID;
          }
        }
      }

      using (MySqlCommand insertCmd = new MySqlCommand(
        "INSERT INTO Players (Name, Elo) VALUES (@name, @elo); SELECT LAST_INSERT_ID();", conn))
      {
        insertCmd.Parameters.AddWithValue("@name", playerName);
        insertCmd.Parameters.AddWithValue("@elo", elo);

        return Convert.ToInt32(insertCmd.ExecuteScalar());
      }
    }

    private int UpdateEventInfo(MySqlConnection conn, string eventName, string site, string eventDate)
    {
      using (MySqlCommand checkCmd = new MySqlCommand(
        "SELECT eID FROM Events WHERE Name = @name AND Site = @site AND Date = @date;", conn))
      {
        checkCmd.Parameters.AddWithValue("@name", eventName);
        checkCmd.Parameters.AddWithValue("@site", site);
        checkCmd.Parameters.AddWithValue("@date", eventDate);

        object ? result = checkCmd.ExecuteScalar();

        if(result != null)
        {
          return Convert.ToInt32(result);
        }   
      }

      using (MySqlCommand insertCmd = new MySqlCommand(
        "INSERT INTO Events (Name, Site, Date) VALUES (@name, @site, @date); SELECT LAST_INSERT_ID();", conn))
      {
        insertCmd.Parameters.AddWithValue("@name", eventName);
        insertCmd.Parameters.AddWithValue("@site", site);
        insertCmd.Parameters.AddWithValue("@date", eventDate);

        return Convert.ToInt32(insertCmd.ExecuteScalar());
      }
    }

    private void InsertSingleGame(MySqlConnection conn, PgnGame game, int whiteID, int blackID, int eventID)
    {
      using (MySqlCommand insertGameCmd = new MySqlCommand(
        "INSERT INTO Games (Round, Result, Moves, BlackPlayer, WhitePlayer, eID) VALUES(@round, @result, @moves, @black, @white, @event);", conn))
      {
      insertGameCmd.Parameters.AddWithValue("@round", game.Round);
      insertGameCmd.Parameters.AddWithValue("@result", game.Result);
      insertGameCmd.Parameters.AddWithValue("@moves", game.Moves);
      insertGameCmd.Parameters.AddWithValue("@black", blackID);
      insertGameCmd.Parameters.AddWithValue("@white", whiteID);
      insertGameCmd.Parameters.AddWithValue("@event", eventID);

      insertGameCmd.ExecuteNonQuery();

      }
    }

  
    /// <summary>
    /// Queries the database for games that match all the given filters.
    /// The filters are taken from the various controls in the GUI.
    /// </summary>
    /// <param name="white">The white player, or "" if none</param>
    /// <param name="black">The black player, or "" if none</param>
    /// <param name="opening">The first move, e.g. "1.e4", or "" if none</param>
    /// <param name="winner">The winner as "W", "B", "D", or "" if none</param>
    /// <param name="useDate">true if the filter includes a date range, false otherwise</param>
    /// <param name="start">The start of the date range</param>
    /// <param name="end">The end of the date range</param>
    /// <param name="showMoves">true if the returned data should include the PGN moves</param>
    /// <returns>A string separated by newlines containing the filtered games</returns>
    private string PerformQuery(string white, string black, string opening,
      string winner, bool useDate, DateTime start, DateTime end, bool showMoves)
    {
      // This will build a connection string to your user's database on atr,
      // assuimg you've typed a user and password in the GUI
      string connection = GetConnectionString();

      // Build up this string containing the results from your query
      string parsedResult = "";

      // Use this to count the number of rows returned by your query
      // (see below return statement)
      int numRows = 0;

      using (MySqlConnection conn = new MySqlConnection(connection))
      {
        try
        {
          // Open a connection
          conn.Open();

        string query = @"
          SELECT 
            Events.Name AS EventName,
            Events.Site AS EventSite,
            Events.Date AS EventDate,
            WhitePlayerTable.Name AS WhiteName,
            WhitePlayerTable.Elo AS WhiteElo,
            BlackPlayerTable.Name AS BlackName,
            BlackPlayerTable.Elo AS BlackElo,
            Games.Result AS GameResult,
            Games.Moves AS GameMoves
            FROM Games
            JOIN Players AS WhitePlayerTable ON Games.WhitePlayer = WhitePlayerTable.pID
            JOIN Players AS BlackPlayerTable ON Games.BlackPlayer = BlackPlayerTable.pID
            JOIN Events ON Games.eID = Events.eID
            WHERE 1=1";

      if (white != "")
        query += " AND WhitePlayerTable.Name = @white";

      if (black != "")
        query += " AND BlackPlayerTable.Name = @black";

      if (opening != "")
        query += " AND Games.Moves LIKE @opening";

      if (winner != "")
        query += " AND Games.Result = @winner";

      if (useDate)
        query += " AND Events.Date >= @start AND Events.Date <= @end";


      using (MySqlCommand cmd = new MySqlCommand(query, conn))
      {
        if (white != "")
          cmd.Parameters.AddWithValue("@white", white);

        if (black != "")
          cmd.Parameters.AddWithValue("@black", black);

        if (opening != "")
          cmd.Parameters.AddWithValue("@opening", opening + "%");

        if (winner != "")
          cmd.Parameters.AddWithValue("@winner", winner);

        if (useDate)
        {
          cmd.Parameters.AddWithValue("@start", start.Date);
          cmd.Parameters.AddWithValue("@end", end.Date);
        }
        using (MySqlDataReader reader = cmd.ExecuteReader())
            {
            while (reader.Read())
            {
            numRows++;

            parsedResult += "Event: " + reader["EventName"] + "\n";
            parsedResult += "Site: " + reader["EventSite"] + "\n";
            parsedResult += "Date: " + Convert.ToDateTime(reader["EventDate"]).ToString("M/d/yyyy") + "\n";
            parsedResult += "White: " + reader["WhiteName"] + " (" + reader["WhiteElo"] + ")\n";
            parsedResult += "Black: " + reader["BlackName"] + " (" + reader["BlackElo"] + ")\n";
            parsedResult += "Result: " + reader["GameResult"] + "\n";

            if (showMoves)
            parsedResult += reader["GameMoves"] + "\n";

            parsedResult += "\n";
            }
          }

      }

        }

       
        catch (Exception e)
        {
          System.Diagnostics.Debug.WriteLine(e.Message);
        }
      }

      return numRows + " results\n" + parsedResult;
    }


    private string GetConnectionString()
    {
      return "server=atr.eng.utah.edu;database=" + Database + ";uid=" + Username + ";password=" + Password;
    }


    /// <summary>
    /// This method will run when the file chooser is used.
    /// It loads the files contents as an array of strings,
    /// then invokes the InsertGameData method.
    /// </summary>
    /// <param name="args">The event arguments, which contains the selected file name</param>
    private async void HandleFileChooser(EventArgs args)
    {
      try
      {
        string fileContent = string.Empty;

        InputFileChangeEventArgs eventArgs = args as InputFileChangeEventArgs ?? throw new Exception("unable to get file name");
        if (eventArgs.FileCount == 1)
        {
          var file = eventArgs.File;
          if (file is null)
          {
            return;
          }

          // load the chosen file and split it into an array of strings, one per line
          using var stream = file.OpenReadStream(1000000); // max 1MB
          using var reader = new StreamReader(stream);                   
          fileContent = await reader.ReadToEndAsync();
          string[] fileLines = fileContent.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

          // insert the games, and don't wait for it to finish
          // _ = throws away the task result, since we aren't waiting for it
          _ = InsertGameData(fileLines);
        }
      }
      catch (Exception e)
      {
        Debug.WriteLine("an error occurred while loading the file..." + e);
      }
    }

  }

}
