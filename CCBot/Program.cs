using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EEUniverse.Library;
using EEUniverse.LoginExtensions;
using Newtonsoft.Json;

namespace CCBot
{
    public class Program
    {
        // General
        public static Connection con;
        public static Block[,,] world;
        public static string worldid = "LylVqeLVd8V5";//"ZSgvAgo2ONps";
        public static int botid;

        // Players
        public static List<Player> players = new List<Player>(); // Players in the world
        public static List<string> mods = new List<string>(); // Players' database. Saved data

        // World
        public static int width;
        public static int height;

        // Mix
        public static int[] rainbow = { 21, 4, 21, 22, 5, 22, 23, 6, 23, 24, 7, 24, 25, 8, 25, 26, 9, 26, 27, 9, 26 };

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
                    botid = m.GetInt(0);

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

                    if (player.isMod)
                    {
                        await con.SendAsync(MessageType.Chat, $"/giveedit {player.name}"); // If the world belongs to the owner of the bot, give it a try :)
                    }

                    break;


                case MessageType.PlayerExit:
                    player = players.FirstOrDefault(p => p.id == m.GetInt(0));
                    players.RemoveAll(p => p.id == m.GetInt(0));

                    await con.SendAsync(MessageType.Chat, $"[CC] {player.name} left!");

                    break;

                case MessageType.PlayerGod:
                    player = players.FirstOrDefault(p => p.id == m.GetInt(0));  
                    player.isInGod = m.GetBool(1);

                    break;

                case MessageType.PlaceBlock:
                    Block blockBefore = world[m.GetInt(1), m.GetInt(2), m.GetInt(3)];

                    // Assign block to the world
                    world[m.GetInt(1), m.GetInt(2), m.GetInt(3)] = new Block(m.GetInt(4));

                    switch (m.GetInt(4))
                    {
                        // Signs
                        case 55:
                        case 56:
                        case 57:
                        case 58:
                            string text = m.GetString(5);
                            int morph = m.GetInt(6);
                            world[m.GetInt(1), m.GetInt(2), m.GetInt(3)] = new Sign(m.GetInt(4), text, morph);
                            break;

                        // Portals
                        case 59:
                            int rotation = m.GetInt(5);
                            int p_id = m.GetInt(6);
                            int t_id = m.GetInt(7);
                            bool flip = m.GetBool(8);
                            world[m.GetInt(1), m.GetInt(2), m.GetInt(3)] = new Portal(m.GetInt(4), rotation, p_id, t_id, flip);
                            break;

                        // Effects
                        case 93:
                        case 94:
                            int r = m.GetInt(5);
                            world[m.GetInt(1), m.GetInt(2), m.GetInt(3)] = new Effect(m.GetInt(4), r);
                            break;
                    }

                    // For special modes, replace the block
                    if (m.GetInt(0) != botid)
                    {
                        player = players.FirstOrDefault(p => p.id == m.GetInt(0));
                        int bid = m.GetInt(4);

                        if (player.brushSize > 1 && player.mode == 0)
                        {
                            for (int x = 0; x < width; x++)
                                for (int y = 0; y < height; y++)
                                    if (Math.Pow(m.GetInt(2) - x, 2) + Math.Pow(m.GetInt(3) - y, 2) < Math.Pow(player.brushSize, 2))
                                        await PlaceBlock(m.GetInt(1), x, y, bid);
                        }

                        switch (player.mode)
                        {
                            case 1:
                                if (bid == 2)
                                    await con.SendAsync(MessageType.PlaceBlock, 1, m.GetInt(2), m.GetInt(3), rainbow[new Random().Next(0, rainbow.Length - 1)]);
                                break;

                            case 2:
                                switch (bid)
                                {
                                    // Set checkpoint for copying content
                                    case 1:

                                        player.copyTopLeftCorner = new Coordinate(m.GetInt(2), m.GetInt(3));
                                        await con.SendAsync(MessageType.Chat, $"/pm {player.name} [CC] Checkpoint set!");
                                        await blockBefore.Place(1, m.GetInt(2), m.GetInt(3));

                                        break;

                                    // Paste
                                    case 2:

                                        await blockBefore.Place(1, m.GetInt(2), m.GetInt(3));
                                        for (int l = 0; l < 2; l++)
                                            for (int x = 0; x < player.clipboard.GetLength(1); x++)
                                                for (int y = 0; y < player.clipboard.GetLength(2); y++)
                                                    if (x >= 0 && y >= 0 && x < width && y < height)
                                                        if (player.clipboard[l, x, y] != null)
                                                            await player.clipboard[l, x, y].Place(l, m.GetInt(2) + x, m.GetInt(3) + y);

                                        break;

                                    // Copy
                                    case 3:

                                        if (Math.Abs((player.copyTopLeftCorner.x - m.GetInt(2)) * (player.copyTopLeftCorner.y - m.GetInt(3))) > 0)
                                        {
                                            await blockBefore.Place(1, m.GetInt(2), m.GetInt(3));
                                            player.clipboard = new Block[2, m.GetInt(2) - player.copyTopLeftCorner.x + 1, m.GetInt(3) - player.copyTopLeftCorner.y + 1];

                                            for (int l = 0; l < 2; l++)
                                                for (int x = player.copyTopLeftCorner.x; x < m.GetInt(2) + 1; x++)
                                                    for (int y = player.copyTopLeftCorner.y; y < m.GetInt(3) + 1; y++)
                                                        if (x >= 0 && y >= 0 && x < width && y < height)
                                                            if (world[l, x, y].id != 0)
                                                                player.clipboard[l, x - player.copyTopLeftCorner.x, y - player.copyTopLeftCorner.y] = world[l, x, y];
                                            player.clipboard[1, m.GetInt(2) - player.copyTopLeftCorner.x, m.GetInt(3) - player.copyTopLeftCorner.y] = blockBefore;

                                            await con.SendAsync(MessageType.Chat, $"/pm {player.name} [CC] Content copied to clipboard!");
                                        }

                                        break;
                                }
                                break;
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
                                        if (param.Length > 1)
                                        {
                                            switch (param[0])
                                            {
                                                case "tools":
                                                    await con.SendAsync(MessageType.Chat, $"/pm {player.name} !circle l x y d bid = Creates the border of the specified circular area. (!ci)");
                                                    await con.SendAsync(MessageType.Chat, $"/pm {player.name} !clear x1 y1 x2 y2 = Clears everything in the specified area. (!cl)");
                                                    await con.SendAsync(MessageType.Chat, $"/pm {player.name} !clearall = Clears everything.");
                                                    await con.SendAsync(MessageType.Chat, $"/pm {player.name} !fill l x1 y1 x2 y2 bid = Fills everything in the specified area with a block (!fl)");
                                                    await con.SendAsync(MessageType.Chat, $"/pm {player.name} !rect l x1 y1 x2 y2 bid = Creates the border of the rect of the specified area (!re)");
                                                    break;

                                                case "clipboard":
                                                    await con.SendAsync(MessageType.Chat, $"/pm {player.name} To activate the paste-mode, say '!mode paste'");
                                                    await con.SendAsync(MessageType.Chat, $"/pm {player.name} Copy: Initialize the top left corner with a white basic block, copy a rectangular area with the black basic block (Everything between those two will be copied)");
                                                    await con.SendAsync(MessageType.Chat, $"/pm {player.name} Paste: Place a grey basic block at the top left of where your paste should be");
                                                    break;

                                                case "modes":
                                                    await con.SendAsync(MessageType.Chat, $"/pm {player.name} Use !mode <modification name>");
                                                    await con.SendAsync(MessageType.Chat, $"/pm {player.name} rainbow: Try it out.");
                                                    await con.SendAsync(MessageType.Chat, $"/pm {player.name} paste: Not working yet.");
                                                    break;

                                                case "settings":
                                                    await con.SendAsync(MessageType.Chat, $"/pm {player.name} !brush size d: Sets your brush size");
                                                    break;
                                            }
                                        }
                                        else
                                        {
                                            await con.SendAsync(MessageType.Chat, $"/pm {player.name} [Creative Crew Bot] For the block id's, see https://github.com/capasha/EEUProtocol/blob/master/Blocks.md");
                                            await con.SendAsync(MessageType.Chat, $"/pm {player.name} List of commands filtered by usage: !help <tools | clipboard | modes | settings>");
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
                                                await PlaceBlock(l, x, y, 0);
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
                                                    await PlaceBlock(l, x, y, bid);
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
                                                await PlaceBlock(l, x, y1, bid);
                                                await PlaceBlock(l, x, y2, bid);
                                            }

                                            for (int y = y1; y < y2 + 1; y++)
                                            {
                                                await PlaceBlock(l, x1, y, bid);
                                                await PlaceBlock(l, x2, y, bid);
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

                                            int d = (5 - r * 4) / 4;
                                            int x = 0;
                                            int y = r;

                                            do
                                            {
                                                await PlaceBlock(l, _x + x, _y + y, bid);
                                                await PlaceBlock(l, _x - x, _y + y, bid);
                                                await PlaceBlock(l, _x + x, _y - y, bid);
                                                await PlaceBlock(l, _x - x, _y - y, bid);
                                                await PlaceBlock(l, _x + y, _y + x, bid);
                                                await PlaceBlock(l, _x - y, _y + x, bid);
                                                await PlaceBlock(l, _x + y, _y - x, bid);
                                                await PlaceBlock(l, _x - y, _y - x, bid);

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
                                                await con.SendAsync(MessageType.Chat, $"/pm {player.name} error");
                                            });
                                        }
                                    }
                                    break;

                                /*
                                 * Setting
                                 */

                                case "mode":
                                    if (param.Length > 1)
                                    {
                                        switch(param[1])
                                        {
                                            case "default":
                                                player.mode = 0;
                                                break;

                                            case "rainbow":
                                                player.mode = 1;
                                                break;

                                            case "paste":
                                                player.mode = 2;
                                                break;

                                            default:
                                                await con.SendAsync(MessageType.Chat, $"/pm {player.name} error");
                                                break;
                                        }
                                    }
                                    break;

                                case "brush":
                                    if (param.Length > 1)
                                    {
                                        switch (param[1])
                                        {
                                            case "size":
                                                try
                                                {
                                                    int n = Int32.Parse(param[2]);
                                                    player.brushSize = n;
                                                }
                                                catch
                                                {
                                                    await con.SendAsync(MessageType.Chat, $"/pm {player.name} error");
                                                }
                                                break;

                                            case "shape":
                                                await con.SendAsync(MessageType.Chat, $"/pm {player.name} Haha! No...");
                                                break;

                                            default:
                                                await con.SendAsync(MessageType.Chat, $"/pm {player.name} error");
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

        static async Task PlaceBlock(int l, int x, int y, int id)
        {
            if (world[l, x, y].id != id)
            {
                await con.SendAsync(MessageType.PlaceBlock, l, x, y, id);
                world[l, x, y] = new Block(id);
            }
        }

        /*static async Task PlaceSign(int x, int y, int id, string text, int morph)
        {
            await con.SendAsync(MessageType.PlaceBlock, 1, x, y, id, text, morph);
            world[1, x, y] = new Sign(id, text, morph);
        }

        static async Task PlacePortal(int x, int y, int id, int morph, int _p_id, int _t_id, bool _flip)
        {
            await con.SendAsync(MessageType.PlaceBlock, 1, x, y, id, morph, _p_id, _t_id, _flip);
            world[1, x, y] = new Portal(id, morph, _p_id, _t_id, _flip);
        }*/
    }
}
