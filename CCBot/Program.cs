using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EEUniverse.Library;
using EEUniverse.LoginExtensions;
using Newtonsoft.Json;

namespace Turnskin
{
    class Program
    {
        // General
        public static Connection con;
        public static Block[,,] world;
        public static string worldid = "ZSgvAgo2ONps";

        // Players
        public static List<Player> players = new List<Player>(); // Players in the world
        public static List<string> mods = new List<string>(); // Players' database. Saved data

        // World
        public static int width;
        public static int height;

        public static int mode;
        public static int modeInt = 0;
        public static int[] rainbow = { 21, 4, 21, 22, 5, 22, 23, 6, 23, 24, 7, 24, 25, 8, 25, 26, 9, 26, 27, 9, 26 };

        // Mix
        public static JsonSerializerSettings json_settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto };

        static async Task Main(string[] args)
        {
            Console.WriteLine("Reading cookie...");

            // Operators/ Moderators

            var PlayerData = File.ReadAllText("../../../profiles.json");
            var deserializedValuesPlayers = JsonConvert.DeserializeObject<List<string>>(PlayerData, json_settings);

            foreach (string s in deserializedValuesPlayers)
            {
                mods.Add(s);
            }

            using (StreamReader r = File.OpenText("../../../cookie.txt"))
            {
                string cookie = r.ReadToEnd();
                await Main2(cookie);
            }
        }

        static async Task Main2(string token)
        {
            Console.WriteLine("Logging in...");

            try
            {
                var client = await GoogleLogin.GetClientFromCookieAsync(token);
                await client.ConnectAsync();

                con = client.CreateWorldConnection(worldid);

                con.OnMessage += async (s, m) =>
                {
                    await Main3(m);
                };
                await con.SendAsync(MessageType.Init, 0);

                Thread.Sleep(-1);
            }
            catch
            {
                Console.WriteLine("Login failed! Update google cookie!");
            }
        }

        static async Task Main3(Message m)
        {
            Player player;

            switch (m.Type)
            {
                case MessageType.Init:

                    Console.WriteLine("Logged in!");
                    await con.SendAsync(MessageType.Chat, $"[CC] Connected!");
                    //await con.SendAsync(MessageType.Chat, $"/title Turnskin [ON]");

                    world = new Block[2, m.GetInt(9), m.GetInt(10)];

                    width = m.GetInt(9);
                    height = m.GetInt(10);

                    int index = 11;
                    for (int y = 0; y < width; y++)
                        for (int x = 0; x < height; x++)
                        {
                            int value = 0;
                            if (m[index++] is int iValue)
                                value = iValue;

                            var backgroundId = value >> 16;
                            var foregroundId = 65535 & value;

                            world[1, x, y] = new Block(foregroundId);
                            world[0, x, y] = new Block(backgroundId);
                            switch (foregroundId)
                            {
                                case 55:
                                case 56:
                                case 57:
                                case 58:
                                    string text = m.GetString(index++);
                                    int morph = m.GetInt(index++);
                                    world[1, x, y] = new Sign(foregroundId, text, morph);
                                    break;

                                case 59:
                                    int rotation = m.GetInt(index++);
                                    int p_id = m.GetInt(index++);
                                    int t_id = m.GetInt(index++);
                                    bool flip = m.GetBool(index++);
                                    world[1, x, y] = new Portal(foregroundId, rotation, p_id, t_id, flip);
                                    break;

                                case 93:
                                case 94:
                                    int r = m.GetInt(index++);
                                    world[1, x, y] = new Effect(foregroundId, r);
                                    break;
                            }
                        }

                    /*for (int x = 0; x < width; x++)
                        for (int y = 0; y < height; y++)
                        {
                            await con.SendAsync(MessageType.PlaceBlock, 1, x, y, 0);
                        }

                    for (int x = 0; x < width; x++)
                        for (int y = 0; y < height; y++)
                        {
                            //await world[1, x, y].Place(1, x, y);
                            await world[1, x, y].Place(1, x, y);
                        }*/

                    break;

                case MessageType.PlayerJoin:
                case MessageType.PlayerAdd:

                    var playerExisting = players.FirstOrDefault(p => p.name == m.GetString(1).ToLower());
                    players.Add(new Player(m.GetInt(0), m.GetString(1).ToLower()));
                    player = players.FirstOrDefault(p => p.id == m.GetInt(0));

                    await con.SendAsync(MessageType.Chat, $"[CC] {player.name} joined!");

                    break;


                case MessageType.PlayerExit:
                    player = players.FirstOrDefault(p => p.id == m.GetInt(0));
                    players.RemoveAll(p => p.id == m.GetInt(0));

                    await con.SendAsync(MessageType.Chat, $"[CC] {player.name} left!");

                    break;

                case MessageType.PlaceBlock:
                    world[m.GetInt(0), m.GetInt(1), m.GetInt(2)] = new Block(m.GetInt(3));

                    switch (m.GetInt(3))
                    {
                        // Signs
                        case 55:
                        case 56:
                        case 57:
                        case 58:
                            string text = m.GetString(4);
                            int morph = m.GetInt(5);
                            world[m.GetInt(0), m.GetInt(1), m.GetInt(2)] = new Sign(m.GetInt(3), text, morph);
                            break;

                        // Portals
                        case 59:
                            int rotation = m.GetInt(4);
                            int p_id = m.GetInt(5);
                            int t_id = m.GetInt(6);
                            bool flip = m.GetBool(7);
                            world[m.GetInt(0), m.GetInt(1), m.GetInt(2)] = new Portal(m.GetInt(3), rotation, p_id, t_id, flip);
                            break;

                        // Effects
                        case 93:
                        case 94:
                            int r = m.GetInt(4);
                            world[m.GetInt(0), m.GetInt(1), m.GetInt(2)] = new Effect(m.GetInt(3), r);
                            break;
                    }

                    if (m.GetInt(3) == 2)
                    {
                        if (mode == 1)
                        {
                            modeInt++;
                            await con.SendAsync(MessageType.PlaceBlock, 1, m.GetInt(1), m.GetInt(2), rainbow[modeInt%rainbow.Length]);
                        }
                    }
                    break;

                case MessageType.Chat:

                    if (m.GetString(1).StartsWith("!", StringComparison.Ordinal) || m.GetString(1).StartsWith(".", StringComparison.Ordinal))
                    {
                        string[] param = m.GetString(1).ToLower().Substring(1).Split(" ");
                        string cmd = param[0];
                        player = players.FirstOrDefault(p => p.id == m.GetInt(0));

                        if (player.isMod)
                        {
                            switch (cmd)
                            {
                                case "help":
                                    await Task.Run(async () =>
                                    {
                                        await con.SendAsync(MessageType.Chat, $"/pm {player.name} [CC] Artist Commands: (bid: see https://github.com/capasha/EEUProtocol/blob/master/Blocks.md)");
                                        //await con.SendAsync(MessageType.Chat, $"/pm {player.name} !circle l x y r bid = Clears everything in the specified circular area. (!ci)");
                                        await con.SendAsync(MessageType.Chat, $"/pm {player.name} !clear x1 y1 x2 y2 = Clears everything in the specified area. (!cl)");
                                        await con.SendAsync(MessageType.Chat, $"/pm {player.name} !clearall = Clears everything.");
                                        await con.SendAsync(MessageType.Chat, $"/pm {player.name} !drawsectors = First stage of level building: Show all boxes");
                                        await con.SendAsync(MessageType.Chat, $"/pm {player.name} !fill l x1 y1 x2 y2 bid = Fills everything in the specified area with a block (!fl)");
                                        await con.SendAsync(MessageType.Chat, $"/pm {player.name} !fillcircle l x y r bid = Clears everything in the specified circular area. (!fc)");
                                        await con.SendAsync(MessageType.Chat, $"/pm {player.name} !rect l x1 y1 x2 y2 bid = Creates the border of the rect of the specified area (!re)");
                                    });
                                    break;

                                case "amimod":
                                    await Task.Run(async () =>
                                    {
                                        await con.SendAsync(MessageType.Chat, $"/pm {player.name} {player.isMod.ToString()}");
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
                                            int x1 = Int32.Parse(param[1]);
                                            int y1 = Int32.Parse(param[2]);
                                            int x2 = Int32.Parse(param[3]);
                                            int y2 = Int32.Parse(param[4]);

                                            for (int l = 0; l < 2; l++)
                                                for (int x = x1; x < x2 + 1; x++)
                                                    for (int y = y1; y < y2 + 1; y++)
                                                    {
                                                        await con.SendAsync(MessageType.PlaceBlock, l, x, y, 0);
                                                    }
                                        }
                                        catch
                                        {
                                            await Task.Run(async () =>
                                            {
                                                await con.SendAsync(MessageType.Chat, $"/pm {player.name} error");
                                            });
                                        }
                                    }
                                    break;

                                case "clearall":
                                    for (int l = 0; l < 2; l++)
                                        for (int x = 0; x < width; x++)
                                            for (int y = 0; y < height; y++)
                                            {
                                                await con.SendAsync(MessageType.PlaceBlock, l, x, y, 0);
                                            }
                                    break;

                                case "drawsectors":
                                    for (int x = 0; x < width; x++)
                                        for (int y = 0; y < height; y++)
                                        {
                                            if (x % 100 == 0 || y % 100 == 0)
                                            {
                                                await con.SendAsync(MessageType.PlaceBlock, 1, x, y, 1);
                                            }
                                            else if (x % 25 == 0 || y % 25 == 0)
                                            {
                                                await con.SendAsync(MessageType.PlaceBlock, 1, x, y, 2);
                                            }
                                            else if (x % 5 == 0 || y % 5 == 0)
                                            {
                                                await con.SendAsync(MessageType.PlaceBlock, 1, x, y, 3);
                                            }
                                        }
                                    break;

                                case "fill":
                                case "fl":
                                    if (param.Length > 6)
                                    {
                                        try
                                        {
                                            int l = Int32.Parse(param[1]);
                                            int x1 = Int32.Parse(param[2]);
                                            int y1 = Int32.Parse(param[3]);
                                            int x2 = Int32.Parse(param[4]);
                                            int y2 = Int32.Parse(param[5]);
                                            int bid = Int32.Parse(param[6]);

                                            for (int x = x1; x < x2 + 1; x++)
                                                for (int y = y1; y < y2 + 1; y++)
                                                {
                                                    await con.SendAsync(MessageType.PlaceBlock, l, x, y, bid);
                                                }
                                        }
                                        catch
                                        {
                                            await Task.Run(async () =>
                                            {
                                                await con.SendAsync(MessageType.Chat, $"/pm {player.name} error");
                                            });
                                        }
                                    }
                                    break;

                                case "rect":
                                case "re":
                                    if (param.Length > 6)
                                    {
                                        try
                                        {
                                            int l = Int32.Parse(param[1]);
                                            int x1 = Int32.Parse(param[2]);
                                            int y1 = Int32.Parse(param[3]);
                                            int x2 = Int32.Parse(param[4]);
                                            int y2 = Int32.Parse(param[5]);
                                            int bid = Int32.Parse(param[6]);

                                            for (int x = x1; x < x2 + 1; x++)
                                            {
                                                await con.SendAsync(MessageType.PlaceBlock, l, x, y1, bid);
                                                await con.SendAsync(MessageType.PlaceBlock, l, x, y2, bid);
                                            }

                                            for (int y = y1; y < y2 + 1; y++)
                                            {
                                                await con.SendAsync(MessageType.PlaceBlock, l, x1, y, bid);
                                                await con.SendAsync(MessageType.PlaceBlock, l, x2, y, bid);
                                            }
                                        }
                                        catch
                                        {
                                            await Task.Run(async () =>
                                            {
                                                await con.SendAsync(MessageType.Chat, $"/pm {player.name} error");
                                            });
                                        }
                                    }
                                    break;

                                case "fillcircle":
                                case "fc":
                                    if (param.Length > 5)
                                    {
                                        try
                                        {
                                            int l = Int32.Parse(param[1]);
                                            int _x = Int32.Parse(param[2]);
                                            int _y = Int32.Parse(param[3]);
                                            int r = Int32.Parse(param[4]);
                                            int bid = Int32.Parse(param[5]);

                                            for (int x = 0; x < width; x++)
                                                for (int y = 0; y < height; y++)
                                                {
                                                    if (Math.Pow(_x - x, 2) + Math.Pow(_y - y, 2) < Math.Pow(r, 2))
                                                    {
                                                        await con.SendAsync(MessageType.PlaceBlock, l, x, y, bid);
                                                    }
                                                }
                                        }
                                        catch
                                        {
                                            await Task.Run(async () =>
                                            {
                                                await con.SendAsync(MessageType.Chat, $"/pm {player.name} error");
                                            });
                                        }
                                    }
                                    break;

                                case "circle":
                                case "ci":
                                    if (param.Length > 5)
                                    {
                                        try
                                        {
                                            int l = Int32.Parse(param[1]);
                                            int _x = Int32.Parse(param[2]);
                                            int _y = Int32.Parse(param[3]);
                                            int r = Int32.Parse(param[4]);
                                            int bid = Int32.Parse(param[5]);

                                            /*for (int x = 0; x < width; x++)
                                                for (int y = 0; y < height; y++)
                                                {
                                                    if (Math.Pow(_x - x, 2) + Math.Pow(_y - y, 2) < Math.Pow(r, 2) && Math.Pow(_x - x, 2) + Math.Pow(_y - y, 2) > Math.Pow(r - 1, 2))
                                                    {
                                                        await con.SendAsync(MessageType.PlaceBlock, l, x, y, bid);
                                                    }
                                                }*/

                                            int d = (5 - r * 4) / 4;
                                            int x = 0;
                                            int y = r;

                                            do
                                            {
                                                await con.SendAsync(MessageType.PlaceBlock, l, _x + x, _y + y, bid);
                                                await con.SendAsync(MessageType.PlaceBlock, l, _x - x, _y + y, bid);
                                                await con.SendAsync(MessageType.PlaceBlock, l, _x + x, _y - y, bid);
                                                await con.SendAsync(MessageType.PlaceBlock, l, _x - x, _y - y, bid);
                                                await con.SendAsync(MessageType.PlaceBlock, l, _x + y, _y + x, bid);
                                                await con.SendAsync(MessageType.PlaceBlock, l, _x - y, _y + x, bid);
                                                await con.SendAsync(MessageType.PlaceBlock, l, _x + y, _y - x, bid);
                                                await con.SendAsync(MessageType.PlaceBlock, l, _x - y, _y - x, bid);

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
                                        catch (Exception e)
                                        {
                                            await Task.Run(async () =>
                                            {
                                                await con.SendAsync(MessageType.Chat, $"/pm {player.name} error");
                                            });
                                        }
                                    }
                                    break;

                                /*
                                 * Clipboard
                                 */

                                case "copy":
                                    if (param.Length > 4)
                                    {
                                        try
                                        {
                                            int x1 = Int32.Parse(param[1]);
                                            int y1 = Int32.Parse(param[2]);
                                            int x2 = Int32.Parse(param[3]);
                                            int y2 = Int32.Parse(param[4]);

                                            if (Math.Abs(x1 - x2) > 0 && Math.Abs(y1 - y2) > 0)
                                            {
                                                player.clipboard = new Block[2, x2 - x1 + 1, y2 - y1 + 1];

                                                for (int l = 0; l < 2; l++)
                                                    for (int x = x1; x < x2 + 1; x++)
                                                        for (int y = y1; y < y2 + 1; y++)
                                                        {
                                                            if (world[l, x, y].id != 0)
                                                            {
                                                                player.clipboard[l, x - x1, y - y1] = world[l, x, y];
                                                            }
                                                        }

                                                await con.SendAsync(MessageType.Chat, $"/pm {player.name} [CC] Content copied to clipboard");
                                            }
                                            else
                                            {
                                                await con.SendAsync(MessageType.Chat, $"/pm {player.name} [CC] Make sure the area of the rectangle you copy is more than zero!");
                                            }
                                        }
                                        catch (Exception e)
                                        {
                                            await Task.Run(async () =>
                                            {
                                                await con.SendAsync(MessageType.Chat, $"/pm {player.name} error");
                                            });
                                        }
                                    }
                                    break;

                                case "cut":

                                    break;

                                case "paste":
                                    if (param.Length > 2)
                                    {
                                        try
                                        {
                                            int _x = Int32.Parse(param[1]);
                                            int _y = Int32.Parse(param[2]);

                                            for (int l = 0; l < 2; l++)
                                                for (int x = 0; x < player.clipboard.GetLength(1); x++)
                                                    for (int y = 0; y < player.clipboard.GetLength(2); y++)
                                                    {
                                                        if (player.clipboard[l, x, y] != null)
                                                        {
                                                            await player.clipboard[l, x, y].Place(l, _x + x, _y + y);
                                                        }
                                                    }
                                        }
                                        catch
                                        {
                                            await Task.Run(async () =>
                                            {
                                                await con.SendAsync(MessageType.Chat, $"/pm {player.name} error");
                                            });
                                        }
                                    }
                                    break;

                                /*
                                 * Mode setting
                                 */

                                case "mode":
                                    if (param.Length > 1)
                                    {
                                        switch(param[1])
                                        {
                                            case "default":
                                                /*player.*/mode = 0;
                                                //await con.SendAsync(MessageType.Chat, $"/pm {player.name} [CC] Your mode has been set to default!");
                                                break;

                                            case "rainbow":
                                                /*player.*/mode = 1;
                                                break;
                                        }
                                    }
                                    break;
                            }
                        }
                    }
                    break;
            }
        }

        /*
         * Functions
         */

        /*static async Task PlaceBlock(int l, int x, int y, int id)
        {
            await con.SendAsync(MessageType.PlaceBlock, l, x, y, id);
            world[l, x, y] = new Block(id);
        }

        static async Task PlaceSign(int x, int y, int id, string text, int morph)
        {
            await con.SendAsync(MessageType.PlaceBlock, 1, x, y, id, text, morph);
            world[1, x, y] = new Sign(id, text, morph);
        }

        static async Task PlacePortal(int x, int y, int id, int morph, int _p_id, int _t_id, bool _flip)
        {
            await con.SendAsync(MessageType.PlaceBlock, 1, x, y, id, morph, _p_id, _t_id, _flip);
            world[1, x, y] = new Portal(id, morph, _p_id, _t_id, _flip);
        }*/

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

                if (mods.Contains(name))
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
                await con.SendAsync(MessageType.PlaceBlock, l, x, y, id);
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
                await con.SendAsync(MessageType.PlaceBlock, l, x, y, id, value);
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
                await con.SendAsync(MessageType.PlaceBlock, l, x, y, id, rotation, portalId, targetId, flip);
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
                await con.SendAsync(MessageType.PlaceBlock, l, x, y, id, text, morph);
            }
        }

        /*
         * Maps
         */

        public class Coordinate
        {
            public int l;
            public int x;
            public int y;

            public Coordinate(int _l, int _x, int _y)
            {
                l = _l;
                x = _x;
                y = _y;
            }
        }
    }
}
