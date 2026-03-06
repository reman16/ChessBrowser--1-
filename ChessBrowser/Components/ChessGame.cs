public class PgnGame{
    public string EventName{ get; set; } = "";
    public string Site{ get; set; } = "?";
    public string Round{ get; set; } = "";
    public string WhitePlayer{ get; set; }= "";
    public string BlackPlayer{ get; set; }= "";
    public int WhiteElo{ get; set; }
    public int BlackElo{ get; set; }
    public string EventDate{ get; set; }  = "0000-00-00";
    public char Result{ get; set; }
    public string Moves{ get; set; } ="";
}
