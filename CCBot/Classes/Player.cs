
namespace CCBot
{
    /*
     * Players
     */

    public class Player
    {
        // General
        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsMod { get; set; }

        // Movement Tracking
        public bool IsInGod { get; set; } = false;
        /*public double x;
        public double y;*/

        // !copy !cut !paste
        public Block[,,] Clipboard { get; set; }

        // !mode - Need to know who placed the block
        public int Mode { get; set; }
        public int Mirror { get; set; }
        public int BrushSize { get; set; }
        public Coordinate Checkpoint { get; set; }

        public Player(int _id, string _name)
        {
            Id = _id;
            Name = _name;

            // Always on start
            Mode = 0;
            Mirror = 0;
            BrushSize = 1;

            // Is the player a moderator?
            IsMod = false;

            if (Program.Mods.Contains(Name))
            {
                IsMod = true;
            }
        }

        /// <summary>
        /// Sends a private message to the player who sent the command.
        /// </summary>
        /// <param name="message"> The message. </param>
        public void Tell(string message) => Program.SayPrivate(Name, message);

        /// <summary>
        /// Sends a private message to the player who sent the command.
        /// </summary>
        /// <param name="format"> The formatted message. </param>
        /// <param name="args"> The arguments. </param>
        public void Tell(string format, params object[] args) => Program.SayPrivate(Name, format, args);
    }
}
