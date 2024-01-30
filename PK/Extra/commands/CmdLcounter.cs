using System;
using Flames;
public class CmdLCounter : Command
{
	public override string name { get { return "LCounter"; } }
	public override string shortcut { get { return "LCount"; } }
	public override string type { get { return "other"; } }
	public override bool museumUsable { get { return true; } }
	public override LevelPermission defaultRank { get { return LevelPermission.Guest; } }
    public static long x;
	public override void Use(Player p, string message)
	{
    bool MessageEmpty = string.IsNullOrEmpty(message);
    bool NegativeNumber = message.CaselessContains("-");
    bool IsZero = message.CaselessEq("0");
	if (IsZero) {
    p.Message("0");
	p.Message("Limit reached!");
	p.Message("Do the command again to restart!");
    return; }
    if (MessageEmpty) {
   Help(p);
   return; }
   bool Success = long.TryParse(message, out long z);
   bool TooHigh = z > 100;
   if (TooHigh) {
   p.Message("The requested end value is too high! Please choose a value less than 101.");
   return;
   }
   bool TooLow = z < -100;
   if (TooLow) {
   p.Message("The requested end value is too low! Please choose a value greater than -101.");
   return;
   }
   if (!Success) {
   Help(p);
   return;
   }
   if (Success) {
   		if (NegativeNumber) {
      while (x != z)
        {
            x--;
            string strX = x.ToString();
            p.Message(strX);
        if (x == z)
        {
            p.Message("Min limit reached!");
            x = 0;
            p.Message("Do the command again to restart!");
            return;
        } 
        }
        }
    else { 
   while (x != z)
        {
            x++;
            string strX = x.ToString();
            p.Message(strX);
        if (x == z)
        {
            p.Message("Max limit reached!");
            x = 0;
            p.Message("Do the command again to restart!");
            return;
        } 
        }
        }
        }
        }
	public override void Help(Player p)
	{
	 p.Message("/LCounter [number] - Counts to the number provided using long instead of int.");
	 p.Message("Number must be less than 101 and greater than -101.");
	}
}