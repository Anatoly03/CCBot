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
        /*
            it's a property
            basically it puts two methods that allow you to access the actual variable behind it
        */
        // General
        public static Connection Con;
        public static Block[,,] World;
        public static string WorldId = "ZSgvAgo2ONps";//"TSz0cNfHyyVO";//"LylVqeLVd8V5";
        public static int Botid;

        // Players
        public static List<Player> Players = new List<Player>(); // Players in the world
        public static List<string> Mods = new List<string>(); // Players' database. Saved data

        // World
        public static int Width;
        public static int Height;

        // Temporary Mix
        public static int[] Rainbow = { 21, 4, 21, 22, 5, 22, 23, 6, 23, 24, 7, 24, 25, 8, 25, 26, 9, 26, 27, 9, 27 };

        public static JsonSerializerSettings Json_settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto };

        static async Task Main(string[] args)
        {
            Console.WriteLine("Reading cookie...");

            // Operators/ Moderators

            var PlayerData = File.ReadAllText("../../../profiles.json");
            var deserializedValuesPlayers = JsonConvert.DeserializeObject<List<string>>(PlayerData, Json_settings);

            foreach (string s in deserializedValuesPlayers)
                Mods.Add(s);

            using (StreamReader r = File.OpenText("../../../cookie.txt"))
                await Main2(r.ReadToEnd());
        }

        static async Task Main2(string token)
        {
            Console.WriteLine("Logging in...");

            try
            {
                var client = await GoogleLogin.GetClientFromCookieAsync(token);
                await client.ConnectAsync();

                Con = client.CreateWorldConnection(WorldId);

                Con.OnMessage += async (s, m) =>
                {
                    try
                    {
                        await Main3(m);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Bot almost crashed!");
                        Console.Write(e);
                    }
                };
                await Con.SendAsync(MessageType.Init, 0);

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
                    await Con.SendAsync(MessageType.Chat, $"[CC] Connected!");
                    Botid = m.GetInt(0);

                    World = new Block[2, m.GetInt(9), m.GetInt(10)];

                    Width = m.GetInt(9);
                    Height = m.GetInt(10);

                    int index = 11;
                    for (int y = 0; y < Width; y++)
                        for (int x = 0; x < Height; x++)
                        {
                            int value = 0;
                            if (m[index++] is int iValue)
                                value = iValue;

                            var backgroundId = value >> 16;
                            var foregroundId = 65535 & value;

                            World[1, x, y] = new Block(foregroundId);
                            World[0, x, y] = new Block(backgroundId);
                            switch (foregroundId)
                            {
                                case 55:
                                case 56:
                                case 57:
                                case 58:
                                    string text = m.GetString(index++);
                                    int morph = m.GetInt(index++);
                                    World[1, x, y] = new Sign(foregroundId, text, morph);
                                    break;

                                case 59:
                                    int rotation = m.GetInt(index++);
                                    int p_id = m.GetInt(index++);
                                    int t_id = m.GetInt(index++);
                                    bool flip = m.GetBool(index++);
                                    World[1, x, y] = new Portal(foregroundId, rotation, p_id, t_id, flip);
                                    break;

                                case 93:
                                case 94:
                                    int r = m.GetInt(index++);
                                    World[1, x, y] = new Effect(foregroundId, r);
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

                    var playerExisting = Players.FirstOrDefault(p => p.Name == m.GetString(1).ToLower());
                    Players.Add(new Player(m.GetInt(0), m.GetString(1).ToLower()));
                    player = Players.FirstOrDefault(p => p.Id == m.GetInt(0));

                    if (m.Type == MessageType.PlayerJoin)
                        await Con.SendAsync(MessageType.Chat, $"[CC] {player.Name} joined!");

                    if (player.IsMod)
                        await Con.SendAsync(MessageType.Chat, $"/giveedit {player.Name}"); // If the world belongs to the owner of the bot, give it a try :)

                    break;


                case MessageType.PlayerExit:
                    player = Players.FirstOrDefault(p => p.Id == m.GetInt(0));
                    Players.RemoveAll(p => p.Id == m.GetInt(0));

                    await Con.SendAsync(MessageType.Chat, $"[CC] {player.Name} left!");

                    break;

                case MessageType.PlayerGod:
                    player = Players.FirstOrDefault(p => p.Id == m.GetInt(0));
                    player.IsInGod = m.GetBool(1);

                    break;

                case MessageType.PlaceBlock:
                    Block blockBefore = World[m.GetInt(1), m.GetInt(2), m.GetInt(3)];

                    // Assign block to the world
                    World[m.GetInt(1), m.GetInt(2), m.GetInt(3)] = new Block(m.GetInt(4));

                    switch (m.GetInt(4))
                    {
                        // Signs
                        case 55:
                        case 56:
                        case 57:
                        case 58:
                            string text = m.GetString(5);
                            int morph = m.GetInt(6);
                            World[m.GetInt(1), m.GetInt(2), m.GetInt(3)] = new Sign(m.GetInt(4), text, morph);
                            break;

                        // Portals
                        case 59:
                            int rotation = m.GetInt(5);
                            int p_id = m.GetInt(6);
                            int t_id = m.GetInt(7);
                            bool flip = m.GetBool(8);
                            World[m.GetInt(1), m.GetInt(2), m.GetInt(3)] = new Portal(m.GetInt(4), rotation, p_id, t_id, flip);
                            break;

                        // Effects
                        case 93:
                        case 94:
                            int r = m.GetInt(5);
                            World[m.GetInt(1), m.GetInt(2), m.GetInt(3)] = new Effect(m.GetInt(4), r);
                            break;
                    }

                    // For special modes, replace the block
                    if (m.GetInt(0) != Botid)
                    {
                        player = Players.FirstOrDefault(p => p.Id == m.GetInt(0));
                        int bid = m.GetInt(4);

                        if (player != null)
                            if (player.BrushSize > 1 && player.Mode == 0)
                                for (int x = 0; x < Width; x++)
                                    for (int y = 0; y < Height; y++)
                                        if (Math.Pow(m.GetInt(2) - x, 2) + Math.Pow(m.GetInt(3) - y, 2) < Math.Pow(player.BrushSize, 2))
                                            await World[m.GetInt(1), m.GetInt(2), m.GetInt(3)].Place(m.GetInt(1), x, y);

                        if (player.BrushSize < 1 || player == null)
                            await blockBefore.Place(m.GetInt(1), m.GetInt(2), m.GetInt(3));

                        switch (player.Mode)
                        {
                            case 1:
                                if (bid == 2)
                                    await Con.SendAsync(MessageType.PlaceBlock, 1, m.GetInt(2), m.GetInt(3), Rainbow[new Random().Next(0, Rainbow.Length - 1)]);
                                break;

                            case 2:
                                switch (bid)
                                {
                                    // Set checkpoint for copying content
                                    case 1:

                                        player.Checkpoint = new Coordinate(m.GetInt(2), m.GetInt(3));
                                        await Con.SendAsync(MessageType.Chat, $"/pm {player.Name} [CC] Checkpoint set!");
                                        await blockBefore.Place(1, m.GetInt(2), m.GetInt(3));

                                        break;

                                    // Paste
                                    case 2:

                                        await blockBefore.Place(1, m.GetInt(2), m.GetInt(3));


                                        if (player.Clipboard != null)
                                            for (int l = 0; l < 2; l++)
                                                for (int x = 0; x < player.Clipboard.GetLength(1); x++)
                                                    for (int y = 0; y < player.Clipboard.GetLength(2); y++)
                                                        if (x >= 0 && y >= 0 && x < Width && y < Height)
                                                            if (player.Clipboard[l, x, y] != null)
                                                                await player.Clipboard[l, x, y].Place(l, m.GetInt(2) + x, m.GetInt(3) + y);

                                        break;

                                    // Copy
                                    case 3:

                                        if (Math.Abs((player.Checkpoint.X - m.GetInt(2)) * (player.Checkpoint.Y - m.GetInt(3))) > 0)
                                        {
                                            await blockBefore.Place(1, m.GetInt(2), m.GetInt(3));

                                            if (player.Checkpoint != null)
                                            {
                                                Coordinate tL = new Coordinate(Math.Min(player.Checkpoint.X, m.GetInt(2)), Math.Min(player.Checkpoint.Y, m.GetInt(3)));
                                                Coordinate bR = new Coordinate(Math.Max(player.Checkpoint.X, m.GetInt(2)), Math.Max(player.Checkpoint.Y, m.GetInt(3)));

                                                player.Clipboard = new Block[2, bR.X - tL.X + 1, bR.Y - tL.Y + 1];

                                                for (int l = 0; l < 2; l++)
                                                    for (int x = tL.X; x < bR.X + 1; x++)
                                                        for (int y = tL.Y; y < bR.Y + 1; y++)
                                                            if (x >= 0 && y >= 0 && x < Width && y < Height)
                                                                if (World[l, x, y].Id != 0)
                                                                    player.Clipboard[l, x - tL.X, y - tL.Y] = World[l, x, y];

                                                player.Clipboard[1, m.GetInt(2) - tL.X, m.GetInt(3) - tL.Y] = blockBefore;
                                            }

                                            await Con.SendAsync(MessageType.Chat, $"/pm {player.Name} [CC] Content copied to clipboard!");
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
                        player = Players.FirstOrDefault(p => p.Id == m.GetInt(0));

                        if (player.IsMod)
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
                                                    await Con.SendAsync(MessageType.Chat, $"/pm {player.Name} !circle l x y d bid = Creates the border of the specified circular area. (!ci)");
                                                    await Con.SendAsync(MessageType.Chat, $"/pm {player.Name} !clear x1 y1 x2 y2 = Clears everything in the specified area. (!cl)");
                                                    await Con.SendAsync(MessageType.Chat, $"/pm {player.Name} !clearall = Clears everything.");
                                                    await Con.SendAsync(MessageType.Chat, $"/pm {player.Name} !fill l x1 y1 x2 y2 bid = Fills everything in the specified area with a block (!fl)");
                                                    await Con.SendAsync(MessageType.Chat, $"/pm {player.Name} !rect l x1 y1 x2 y2 bid = Creates the border of the rect of the specified area (!re)");
                                                    break;

                                                case "clipboard":
                                                    await Con.SendAsync(MessageType.Chat, $"/pm {player.Name} To activate the paste-mode, say '!mode paste'");
                                                    await Con.SendAsync(MessageType.Chat, $"/pm {player.Name} Copy: Initialize the top left corner with a white basic block, copy a rectangular area with the black basic block (Everything between those two will be copied)");
                                                    await Con.SendAsync(MessageType.Chat, $"/pm {player.Name} Paste: Place a grey basic block at the top left of where your paste should be");
                                                    break;

                                                case "modes":
                                                    await Con.SendAsync(MessageType.Chat, $"/pm {player.Name} Use !mode <modification name>");
                                                    await Con.SendAsync(MessageType.Chat, $"/pm {player.Name} rainbow: Try it out.");
                                                    await Con.SendAsync(MessageType.Chat, $"/pm {player.Name} paste: Not working yet.");
                                                    break;

                                                case "settings":
                                                    await Con.SendAsync(MessageType.Chat, $"/pm {player.Name} !brush size d: Sets your brush size");
                                                    break;
                                            }
                                        }
                                        else
                                        {
                                            await Con.SendAsync(MessageType.Chat, $"/pm {player.Name} [Creative Crew Bot] For the block id's, see https://github.com/capasha/EEUProtocol/blob/master/Blocks.md");
                                            await Con.SendAsync(MessageType.Chat, $"/pm {player.Name} List of commands filtered by usage: !help <tools | clipboard | modes | settings>");
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
                                                        await Con.SendAsync(MessageType.PlaceBlock, l, x, y, 0);
                                        }
                                        catch
                                        {
                                            await Task.Run(async () =>
                                            {
                                                await Con.SendAsync(MessageType.Chat, $"/pm {player.Name} error");
                                            });
                                        }
                                    }
                                    break;

                                case "clearall":
                                    for (int l = 0; l < 2; l++)
                                        for (int x = 0; x < Width; x++)
                                            for (int y = 0; y < Height; y++)
                                                await PlaceBlock(l, x, y, 0);
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
                                                    await PlaceBlock(l, x, y, bid);
                                        }
                                        catch
                                        {
                                            await Task.Run(async () =>
                                            {
                                                await Con.SendAsync(MessageType.Chat, $"/pm {player.Name} error");
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
                                                await Con.SendAsync(MessageType.Chat, $"/pm {player.Name} error");
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
                                                await Con.SendAsync(MessageType.Chat, $"/pm {player.Name} error");
                                            });
                                        }
                                    }
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
                                                await Con.SendAsync(MessageType.Chat, $"/pm {player.Name} error");
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
                                            await Con.SendAsync(MessageType.Chat, $"/pm {player.Name} The asset '{buildName}' is already existing!");
                                        }
                                        else
                                        {
                                            using (StreamWriter file = File.CreateText($"../../../assets/{buildName}.json"))
                                            {
                                                var serializer = JsonConvert.SerializeObject(player.Clipboard, Json_settings);
                                                file.WriteLine(serializer);
                                            }
                                            await Con.SendAsync(MessageType.Chat, $"/pm {player.Name} Saved!");
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
                                            var deserializedValueBuild = JsonConvert.DeserializeObject<Block[,,]>(BuildData, Json_settings);

                                            player.Clipboard = deserializedValueBuild;
                                        }
                                        else
                                        {
                                            await Con.SendAsync(MessageType.Chat, $"/pm {player.Name} The asset '{buildName}' is not existing!");
                                        }
                                    }
                                    break;

                                case "listassets":
                                    string[] files = Directory.GetFiles($"../../../assets", "*.json").Select(Path.GetFileName).ToArray();

                                    foreach (string k in files)
                                    {
                                        await Con.SendAsync(MessageType.Chat, $"/pm {player.Name} {k}");
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
                                                player.Mode = 0;
                                                break;

                                            case "rainbow":
                                                player.Mode = 1;
                                                break;

                                            case "paste":
                                                player.Mode = 2;
                                                break;

                                            default:
                                                await Con.SendAsync(MessageType.Chat, $"/pm {player.Name} error");
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
                                                    int n = Int32.Parse(param[2]);
                                                    player.BrushSize = n;
                                                }
                                                catch
                                                {
                                                    await Con.SendAsync(MessageType.Chat, $"/pm {player.Name} error");
                                                }
                                                break;

                                            case "shape":
                                                await Con.SendAsync(MessageType.Chat, $"/pm {player.Name} Haha! No...");
                                                break;

                                            default:
                                                await Con.SendAsync(MessageType.Chat, $"/pm {player.Name} error");
                                                break;
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

        public static async Task PlaceBlock(int l, int x, int y, int id)
        {
            if (World[l, x, y].Id != id)
            {
                await Con.SendAsync(MessageType.PlaceBlock, l, x, y, id);
                World[l, x, y] = new Block(id);
            }
        }

        /*public static async Task PlaceSign(int x, int y, int id, string text, int morph)
        {
            await con.SendAsync(MessageType.PlaceBlock, 1, x, y, id, text, morph);
            world[1, x, y] = new Sign(id, text, morph);
        }

        public static async Task PlacePortal(int x, int y, int id, int morph, int _p_id, int _t_id, bool _flip)
        {
            await con.SendAsync(MessageType.PlaceBlock, 1, x, y, id, morph, _p_id, _t_id, _flip);
            world[1, x, y] = new Portal(id, morph, _p_id, _t_id, _flip);
        }*/
    }
}
