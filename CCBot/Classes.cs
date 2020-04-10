using System;
using System.Linq;
using System.Threading.Tasks;
using EEUniverse.Library;

namespace CCBot
{
    /*
     * Players
     */

    public class Player
    {
        // General
        public int id;
        public string name;
        public bool isMod;

        // !copy !cut !paste
        public Block[,,] clipboard;

        // !mode - Need to know who placed the block
        //public int mode;

        public Player(int _id, string _name)
        {
            id = _id;
            name = _name;

            // Always on start
            //mode = 0;

            // Is the player a moderator?
            isMod = false;

            if (Program.mods.Contains(name))
            {
                isMod = true;
            }
        }
    }

    /*
     * Blocks
     */

    public class Block
    {
        public int id;

        public Block(int i)
        {
            id = i;
        }

        public virtual async Task Place(int l, int x, int y)
        {
            //Console.WriteLine("base");
            if (Program.world[l, x, y].id != id)
            {
                await Program.con.SendAsync(MessageType.PlaceBlock, l, x, y, id);
            }
        }
    }

    public class Effect : Block
    {
        public int value;

        public Effect(int i, int v) : base(i)
        {
            value = v;
        }

        public override async Task Place(int l, int x, int y)
        {
            await Program.con.SendAsync(MessageType.PlaceBlock, l, x, y, id, value);
        }
    }

    public class Portal : Block
    {
        public int rotation;
        public int portalId;
        public int targetId;
        public bool flip;

        public Portal(int i, int r, int pid, int tid, bool f) : base(i)
        {
            rotation = r;
            portalId = pid;
            targetId = tid;
            flip = f;
        }

        public override async Task Place(int l, int x, int y)
        {
            await Program.con.SendAsync(MessageType.PlaceBlock, l, x, y, id, rotation, portalId, targetId, flip);
        }
    }

    public class Sign : Block
    {
        public string text;
        public int morph;

        public Sign(int i, string t, int m) : base(i)
        {
            text = t;
            morph = m;
        }

        public override async Task Place(int l, int x, int y)
        {
            //Console.WriteLine("sign");
            await Program.con.SendAsync(MessageType.PlaceBlock, l, x, y, id, text, morph);
        }
    }
}
