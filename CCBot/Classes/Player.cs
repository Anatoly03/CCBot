using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CCBot
{
    /*
     * Players
     */

    public class Player
    {
        public delegate Task MessageHandler(Player sender, string cmd, string[] param);

        public MessageHandler OnMessage;

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
            OnMessage = DefaultMessageHandler;

            if (Program.Mods.Contains(Name))
            {
                IsMod = true;
                OnMessage = ModMessageHandler;
            }
        }

        /// <summary>
        /// Sends a private message to the player who sent the command.
        /// </summary>
        /// <param name="message"> The message. </param>
        public async Task Tell(string message) => await Program.SayPrivate(Name, message);

        /// <summary>
        /// Sends a private message to the player who sent the command.
        /// </summary>
        /// <param name="format"> The formatted message. </param>
        /// <param name="args"> The arguments. </param>
        public async Task Tell(string format, params object[] args) => await Program.SayPrivate(Name, format, args);

        public static async Task ModMessageHandler(Player sender, string cmd, string[] param)
        {
            switch (cmd)
            {
                case "help":
                    await Task.Run(async () =>
                    {
                        if (param.Length > 1)
                        {
                            switch (param[1])
                            {
                                case "tools":
                                    await sender.Tell("!circle l x y d bid = Creates the border of the specified circular area. (!ci)");
                                    await sender.Tell("!clear x1 y1 x2 y2 = Clears everything in the specified area. (!cl)");
                                    await sender.Tell("!clearall = Clears everything.");
                                    await sender.Tell("!curve ... = Creates curves");
                                    await sender.Tell("!fill l x1 y1 x2 y2 bid = Fills everything in the specified area with a block (!fl)");
                                    await sender.Tell("!rect l x1 y1 x2 y2 bid = Creates the border of the rect of the specified area (!re)");
                                    break;

                                case "clipboard":
                                    await sender.Tell("To activate the paste-mode, say '!mode paste'");
                                    await sender.Tell("Copy: Initialize the top left corner with a white basic block, copy a rectangular area with the black basic block (Everything between those two will be copied)");
                                    await sender.Tell("Paste: Place a grey basic block at the top left of where your paste should be");
                                    break;

                                case "modes":
                                    await sender.Tell("Use !mode <modification name>");
                                    await sender.Tell("rainbow: Try it out.");
                                    await sender.Tell("paste: Not working yet.");
                                    await sender.Tell("fill: Place a block to fill a closed area of empty space.");
                                    break;

                                case "mirror":
                                    await sender.Tell("Use !mirror <type>. Then place a grey basic block to define the mirror centre.");
                                    await sender.Tell("h and v are horizontal and vertical mirrors around a line.");
                                    await sender.Tell("p is a point mirror around a point.");
                                    await sender.Tell("h#, v#, p# equavalently shift the centre by 0.5 blocks.");
                                    break;

                                case "storage":
                                    await sender.Tell("Storage Modifications");
                                    await sender.Tell("!getasset <fileName> copies a ready asset to your clipboard.");
                                    await sender.Tell("!listassets PM's you a list of all assets that exist.");
                                    await sender.Tell("DO NOT USE !saveassets for now!!!");
                                    break;

                                case "settings":
                                    await sender.Tell("!brush size d: Sets your brush size");
                                    break;
                            }
                        }
                        else
                        {
                            await sender.Tell("[Creative Crew Bot] For the block id's, see https://github.com/capasha/EEUProtocol/blob/master/Blocks.md");
                            await sender.Tell("List of commands filtered by usage: !help <tools | clipboard | modes | mirror | settings | storage>");
                        }
                    });
                    break;

                /*
                 * Artistic Tools
                 */

                case "clear":
                case "cl":
                    if (param.Length > 4)
                    {
                        try
                        {
                            int x1 = int.Parse(param[1]);
                            int y1 = int.Parse(param[2]);
                            int x2 = int.Parse(param[3]);
                            int y2 = int.Parse(param[4]);

                            for (int l = 0; l < 2; l++)
                                for (int x = x1; x < x2 + 1; x++)
                                    for (int y = y1; y < y2 + 1; y++)
                                        await Program.PlaceBlock(l, x, y, 0);
                        }
                        catch
                        {
                            await sender.Tell("error");
                        }
                    }
                    break;

                case "clearall":
                    for (int l = 0; l < 2; l++)
                        for (int x = 0; x < Program.Width; x++)
                            for (int y = 0; y < Program.Height; y++)
                                await Program.PlaceBlock(l, x, y, 0);
                    break;

                case "fill":
                case "fl":
                    if (param.Length > 6)
                    {
                        try
                        {
                            int l = int.Parse(param[1]);
                            int x1 = int.Parse(param[2]);
                            int y1 = int.Parse(param[3]);
                            int x2 = int.Parse(param[4]);
                            int y2 = int.Parse(param[5]);
                            int bid = int.Parse(param[6]);

                            for (int x = x1; x < x2 + 1; x++)
                                for (int y = y1; y < y2 + 1; y++)
                                    await Program.PlaceBlock(l, x, y, bid);
                        }
                        catch
                        {
                            await sender.Tell("error");
                        }
                    }
                    break;

                case "rect":
                case "re":
                    if (param.Length > 6)
                    {
                        try
                        {
                            int l = int.Parse(param[1]);
                            int x1 = int.Parse(param[2]);
                            int y1 = int.Parse(param[3]);
                            int x2 = int.Parse(param[4]);
                            int y2 = int.Parse(param[5]);
                            int bid = int.Parse(param[6]);

                            for (int x = x1; x < x2 + 1; x++)
                            {
                                await Program.PlaceBlock(l, x, y1, bid);
                                await Program.PlaceBlock(l, x, y2, bid);
                            }

                            for (int y = y1; y < y2 + 1; y++)
                            {
                                await Program.PlaceBlock(l, x1, y, bid);
                                await Program.PlaceBlock(l, x2, y, bid);
                            }
                        }
                        catch
                        {
                            await sender.Tell("error");
                        }
                    }
                    break;

                case "circle":
                case "ci":
                    if (param.Length > 5)
                    {
                        try
                        {
                            int l = int.Parse(param[1]);
                            int _x = int.Parse(param[2]);
                            int _y = int.Parse(param[3]);
                            int r = int.Parse(param[4]);
                            int bid = int.Parse(param[5]);

                            int d = (5 - r * 4) / 4;
                            int x = 0;
                            int y = r;

                            do
                            {
                                await Program.PlaceBlock(l, _x + x, _y + y, bid);
                                await Program.PlaceBlock(l, _x - x, _y + y, bid);
                                await Program.PlaceBlock(l, _x + x, _y - y, bid);
                                await Program.PlaceBlock(l, _x - x, _y - y, bid);
                                await Program.PlaceBlock(l, _x + y, _y + x, bid);
                                await Program.PlaceBlock(l, _x - y, _y + x, bid);
                                await Program.PlaceBlock(l, _x + y, _y - x, bid);
                                await Program.PlaceBlock(l, _x - y, _y - x, bid);

                                if (d < 0)
                                {
                                    d += 2 * x + 1;
                                }
                                else
                                {
                                    d += 2 * (x - y) + 1;
                                    y--;
                                }
                                x++;
                            } while (x <= y);
                        }
                        catch
                        {
                            await Task.Run(async () =>
                            {
                                await sender.Tell("error");
                            });
                        }
                    }
                    break;

                case "replaceall":
                    if (param.Length > 2)
                    {
                        try
                        {
                            int bid = Int32.Parse(param[1]);
                            int bid2 = Int32.Parse(param[2]);

                            for (int l = 0; l < 2; l++)
                                for (int x = 0; x < Program.Width; x++)
                                    for (int y = 0; y < Program.Height; y++)
                                        if (Program.World[1, x, y].Id == bid)
                                            await Program.PlaceBlock(l, x, y, bid2);
                        }
                        catch
                        {
                            await sender.Tell("error");
                        }
                    }
                    break;

                case "curve":
                case "cu":
                    if (param.Length % 2 == 0)
                    {
                        int bid = int.Parse(param[1]);
                        bool drawLines = param[3] == "t";

                        List<Coordinate> pos = new List<Coordinate>();
                        for (int i = 4; i < param.Length; i += 2)
                            pos.Add(new Coordinate(int.Parse(param[i]), int.Parse(param[i + 1])));

                        Queue<Coordinate> queue = new Queue<Coordinate>();
                        int[] k = Program.Tartaglia((uint)pos.Count);
                        int n = pos.Count - 1;
                        int samples = int.Parse(param[2]);
                        for (int d = 0; d <= samples; d += 1)
                        {
                            double t = (double)d / samples;
                            Coordinate c = new Coordinate();
                            for (int i = 0; i <= n; i++)
                            {
                                c.X += (int)Math.Round(k[i] * pos[i].X * Math.Pow(1 - t, n - i) * Math.Pow(t, i));
                                c.Y += (int)Math.Round(k[i] * pos[i].Y * Math.Pow(1 - t, n - i) * Math.Pow(t, i));
                            }
                            queue.Enqueue(c);

                            if (drawLines)
                            {
                                if (queue.Count == 2)
                                {
                                    Coordinate c1 = queue.Dequeue(), c2 = queue.Peek();
                                    int x1 = c1.X, y1 = c1.Y, x2 = c2.X, y2 = c2.Y;

                                    int xC = x1 - x2;
                                    int yC = y1 - y2;

                                    if (Math.Abs(xC) >= Math.Abs(yC))
                                    {
                                        float modY = (float)yC / xC;

                                        if (x1 > x2)
                                            for (int x = x2, i = 0; x < x1; x++, i++)
                                            {
                                                await Program.PlaceBlock(1, x, (int)Math.Round(y2 + modY * i), bid);
                                            }
                                        else
                                            for (int x = x1, i = 0; x < x2; x++, i++)
                                            {
                                                await Program.PlaceBlock(1, x, (int)Math.Round(y1 + modY * i), bid);
                                            }
                                    }
                                    else
                                    {
                                        float modX = (float)xC / yC;

                                        if (y1 > y2)
                                        {
                                            for (int y = y2, i = 0; y < y1; y++, i++)
                                            {
                                                await Program.PlaceBlock(1, (int)Math.Round(x2 + modX * i), y, bid);
                                            }
                                            await Program.PlaceBlock(1, x1, y1, bid);
                                        }
                                        else
                                        {
                                            for (int y = y1, i = 0; y < y2; y++, i++)
                                            {
                                                await Program.PlaceBlock(1, (int)Math.Round(x1 + modX * i), y, bid);
                                            }
                                            await Program.PlaceBlock(1, x2, y2, bid);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                Coordinate cd = queue.Dequeue();
                                await Program.PlaceBlock(1, cd.X, cd.Y, bid);
                            }
                        }
                    }
                    else
                        await sender.Tell("Invalid number of parameters: must be an odd number (<bid> <sampleNum> <drawLines> <x0> <y0> ...)");
                    break;

                /*
                 * Generation
                 */

                case "gen":
                    if (param.Length > 1)
                    {
                        Generator gen = new Generator();

                        switch (param[1])
                        {
                            case "terrain":
                                await gen.Terrain();
                                break;

                            case "sky":
                                await gen.Sky();
                                break;

                            default:
                                await sender.Tell("error");
                                break;
                        }
                    }
                    break;

                /*
                 * Assets
                 */

                case "saveasset":
                    if (param.Length > 1)
                    {
                        string buildName = param[1];

                        if (File.Exists($"../../../assets/{buildName}.json"))
                        {
                            await sender.Tell($"The asset '{buildName}' is already existing!");
                        }
                        else
                        {
                            using (StreamWriter file = File.CreateText($"../../../assets/{buildName}.json"))
                            {
                                var serializer = JsonConvert.SerializeObject(sender.Clipboard, Program.Json_settings);
                                file.WriteLine(serializer);
                            }
                            await sender.Tell("Saved!");
                        }
                    }
                    break;

                case "getasset":
                    if (param.Length > 1)
                    {
                        string buildName = param[1];

                        if (File.Exists($"../../../assets/{buildName}.json"))
                        {
                            var BuildData = File.ReadAllText($"../../../assets/{buildName}.json");
                            var deserializedValueBuild = JsonConvert.DeserializeObject<Block[,,]>(BuildData, Program.Json_settings);

                            sender.Clipboard = deserializedValueBuild;
                        }
                        else
                        {
                            await sender.Tell($"The asset '{buildName}' is not existing!");
                        }
                    }
                    break;

                case "listassets":
                    string[] files = Directory.GetFiles($"../../../assets", "*.json").Select(Path.GetFileName).ToArray();

                    foreach (string k in files)
                    {
                        await sender.Tell(k);
                    }

                    break;

                /*
                 * Mirror
                 */

                case "mirror":
                case "mi":
                    if (param.Length > 1)
                    {
                        switch (param[1])
                        {
                            // Horizontal Mirror
                            case "h":
                                sender.Mode = 3;
                                sender.Mirror = 1;
                                break;

                            // Vertical Mirror
                            case "v":
                                sender.Mode = 3;
                                sender.Mirror = 2;
                                break;

                            // Horizontal Mirror shifted by 0.5 blocks down
                            case "h#":
                                sender.Mode = 3;
                                sender.Mirror = 3;
                                break;

                            // Vertical Mirror shifted by 0.5 blocks right
                            case "v#":
                                sender.Mode = 3;
                                sender.Mirror = 4;
                                break;

                            // Point Mirror
                            case "p":
                                sender.Mode = 3;
                                sender.Mirror = 5;
                                break;

                            // Point Mirror shifted by 0.5 blocks diagonal
                            case "p#":
                                sender.Mode = 3;
                                sender.Mirror = 6;
                                break;

                            // Diagonal x = y Mirror
                            case "d+":
                                sender.Mode = 3;
                                sender.Mirror = 7;
                                await sender.Tell("error");
                                break;

                            // Diagonal x = -y Mirror
                            case "d-":
                                sender.Mode = 3;
                                sender.Mirror = 8;
                                await sender.Tell("error");
                                break;

                            // No Mirror/ Default
                            case "off":
                                sender.Mode = 0;
                                sender.Mirror = 0;
                                sender.BrushSize = 1;
                                break;

                            default:
                                await sender.Tell("error");
                                break;
                        }
                    }
                    break;

                /*
                 * Setting
                 */

                case "mode":
                    if (param.Length > 1)
                    {
                        switch (param[1])
                        {
                            case "default":
                                sender.Mode = 0;
                                sender.Mirror = 0;
                                sender.BrushSize = 1;
                                break;

                            case "rainbow":
                                sender.Mode = 1;
                                break;

                            case "paste":
                                sender.Mode = 2;
                                sender.Mirror = 0;
                                break;

                            // 3 is mirror awaiting

                            case "fill":
                                sender.Mode = 4;
                                break;

                            default:
                                await sender.Tell("error");
                                break;
                        }
                    }
                    break;

                case "brush":
                    if (param.Length > 1)
                        switch (param[1])
                        {
                            case "size":
                                try
                                {
                                    int n = int.Parse(param[2]);
                                    sender.BrushSize = n;
                                }
                                catch
                                {
                                    await sender.Tell("error");
                                }
                                break;

                            case "shape":
                                await sender.Tell("Haha! No...");
                                break;

                            default:
                                await sender.Tell("unknown brush option");
                                break;
                        }
                    break;
                default:
                    await DefaultMessageHandler(sender, cmd, param);
                    break;
            }

        }

        public static async Task DefaultMessageHandler(Player sender, string cmd, string[] param)
        {

        }
    }
}
