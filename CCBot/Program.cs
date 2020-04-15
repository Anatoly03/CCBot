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
        public static string WorldId = "YwxWWJH8nloP"; //"ZSgvAgo2ONps"; "TSz0cNfHyyVO"; "LylVqeLVd8V5";
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

            //using (StreamReader r = File.OpenText("../../../cookie.txt"))
            //    await Main2(r.ReadToEnd());

            await Main2(File.ReadAllLines("../../../cookie.txt"));
        }

        static async Task Main2(string[] data)
        {
            Console.WriteLine("Logging in...");

            try
            {
                bool isGoogleToken = data[0] == "google" ? true : false;
                Client client;
                if (isGoogleToken)
                    client = await GoogleLogin.GetClientFromCookieAsync(data[1]);
                else
                    client = new Client(data[1]);

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
                    Say($"[CC] Connected!");
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

                    /*for (int x = 0; x < Width; x++)
                        for (int y = 0; y < Height; y++)
                            if (World[1, x, y].Id == 20)
                                await PlaceBlock(1, x, y, 80);*/

                    /*for (int x = 0; x < width; x++)
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

                    if (player.IsMod)
                        Say($"/giveedit {player.Name}"); // If the world belongs to the owner of the bot, give it a try :)

                    break;


                case MessageType.PlayerExit:
                    player = Players.FirstOrDefault(p => p.Id == m.GetInt(0));
                    Players.RemoveAll(p => p.Id == m.GetInt(0));

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

                        if (player.Mode == 0)
                        {
                            if (player.Mirror == 1 || player.Mirror == 3)
                            {
                                int newY = 2 * player.Checkpoint.Y - m.GetInt(3);
                                if (player.Mirror == 3) newY++;
                                await World[m.GetInt(1), m.GetInt(2), m.GetInt(3)].Place(m.GetInt(1), m.GetInt(2), newY);
                            }
                            else if (player.Mirror == 2 || player.Mirror == 4)
                            {
                                int newX = 2 * player.Checkpoint.X - m.GetInt(2);
                                if (player.Mirror == 4) newX++;
                                await World[m.GetInt(1), m.GetInt(2), m.GetInt(3)].Place(m.GetInt(1), newX, m.GetInt(3));
                            }
                            else if (player.Mirror == 5 || player.Mirror == 6)
                            {
                                int newX = 2 * player.Checkpoint.X - m.GetInt(2);
                                int newY = 2 * player.Checkpoint.Y - m.GetInt(3);
                                if (player.Mirror == 6)
                                {
                                    newX++;
                                    newY++;
                                }
                                await World[m.GetInt(1), m.GetInt(2), m.GetInt(3)].Place(m.GetInt(1), newX, newY);
                            }
                        }

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
                                        player.Tell("[CC] Checkpoint set!");
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

                                            player.Tell("[CC] Content copied to clipboard!");
                                        }

                                        break;
                                }
                                break;

                            case 3:
                                if (bid == 2)
                                {
                                    player.Mode = 0;
                                    player.Checkpoint = new Coordinate(m.GetInt(2), m.GetInt(3));
                                    await blockBefore.Place(1, m.GetInt(2), m.GetInt(3));
                                }
                                break;
                        }
                    }
                    break;

                case MessageType.Chat:

                    if (m.GetString(1).StartsWith("!", StringComparison.Ordinal) || m.GetString(1).StartsWith(".", StringComparison.Ordinal))
                    {
                        string[] param = m.GetString(1).ToLower().Substring(1).Split(" ", StringSplitOptions.RemoveEmptyEntries);
                        string cmd = param[0];
                        player = Players.FirstOrDefault(p => p.Id == m.GetInt(0));

                        if (player.IsMod)
                        {
                            switch (cmd)
                            {
                                case "help":
                                    await Task.Run(() =>
                                    {
                                        if (param.Length > 1)
                                        {
                                            switch (param[1])
                                            {
                                                case "tools":
                                                    player.Tell("!circle l x y d bid = Creates the border of the specified circular area. (!ci)");
                                                    player.Tell("!clear x1 y1 x2 y2 = Clears everything in the specified area. (!cl)");
                                                    player.Tell("!clearall = Clears everything.");
                                                    player.Tell("!fill l x1 y1 x2 y2 bid = Fills everything in the specified area with a block (!fl)");
                                                    player.Tell("!rect l x1 y1 x2 y2 bid = Creates the border of the rect of the specified area (!re)");
                                                    break;

                                                case "clipboard":
                                                    player.Tell("To activate the paste-mode, say '!mode paste'");
                                                    player.Tell("Copy: Initialize the top left corner with a white basic block, copy a rectangular area with the black basic block (Everything between those two will be copied)");
                                                    player.Tell("Paste: Place a grey basic block at the top left of where your paste should be");
                                                    break;

                                                case "modes":
                                                    player.Tell("Use !mode <modification name>");
                                                    player.Tell("rainbow: Try it out.");
                                                    player.Tell("paste: Not working yet.");
                                                    break;

                                                case "mirror":
                                                    player.Tell("Use !mirror <type>. Then place a grey basic block to define the mirror centre.");
                                                    player.Tell("h and v are horizontal and vertical mirrors around a line.");
                                                    player.Tell("p is a point mirror around a point.");
                                                    player.Tell("h#, v#, p# equavalently shift the centre by 0.5 blocks.");
                                                    break;

                                                case "storage":
                                                    player.Tell("Storage Modifications");
                                                    player.Tell("!getasset <fileName> copies a ready asset to your clipboard.");
                                                    player.Tell("!listassets PM's you a list of all assets that exist.");
                                                    player.Tell("DO NOT USE !saveassets for now!!!");
                                                    break;

                                                case "settings":
                                                    player.Tell("!brush size d: Sets your brush size");
                                                    break;
                                            }
                                        }
                                        else
                                        {
                                            player.Tell("[Creative Crew Bot] For the block id's, see https://github.com/capasha/EEUProtocol/blob/master/Blocks.md");
                                            player.Tell("List of commands filtered by usage: !help <tools | clipboard | modes | mirror | settings | storage>");
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
                                                        await Con.SendAsync(MessageType.PlaceBlock, l, x, y, 0);
                                        }
                                        catch
                                        {
                                            player.Tell("error");
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
                                            int l = int.Parse(param[1]);
                                            int x1 = int.Parse(param[2]);
                                            int y1 = int.Parse(param[3]);
                                            int x2 = int.Parse(param[4]);
                                            int y2 = int.Parse(param[5]);
                                            int bid = int.Parse(param[6]);

                                            for (int x = x1; x < x2 + 1; x++)
                                                for (int y = y1; y < y2 + 1; y++)
                                                    await PlaceBlock(l, x, y, bid);
                                        }
                                        catch
                                        {
                                            player.Tell("error");
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
                                            player.Tell("error");
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
                                            await Task.Run(() =>
                                            {
                                                player.Tell("error");
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
                                                for (int x = 0; x < Width; x++)
                                                    for (int y = 0; y < Height; y++)
                                                        if (World[1, x, y].Id == bid)
                                                            await PlaceBlock(l, x, y, bid2);
                                        }
                                        catch
                                        {
                                            await Con.SendAsync(MessageType.Chat, $"/pm {player.Name} error");
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
                                                player.Tell("error");
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
                                            player.Tell($"The asset '{buildName}' is already existing!");
                                        }
                                        else
                                        {
                                            using (StreamWriter file = File.CreateText($"../../../assets/{buildName}.json"))
                                            {
                                                var serializer = JsonConvert.SerializeObject(player.Clipboard, Json_settings);
                                                file.WriteLine(serializer);
                                            }
                                            player.Tell("Saved!");
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
                                            player.Tell($"The asset '{buildName}' is not existing!");
                                        }
                                    }
                                    break;

                                case "listassets":
                                    string[] files = Directory.GetFiles($"../../../assets", "*.json").Select(Path.GetFileName).ToArray();

                                    foreach (string k in files)
                                    {
                                        player.Tell(k);
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
                                                player.Mode = 3;
                                                player.Mirror = 1;
                                                break;

                                            // Vertical Mirror
                                            case "v":
                                                player.Mode = 3;
                                                player.Mirror = 2;
                                                break;

                                            // Horizontal Mirror shifted by 0.5 blocks down
                                            case "h#":
                                                player.Mode = 3;
                                                player.Mirror = 3;
                                                break;

                                            // Vertical Mirror shifted by 0.5 blocks right
                                            case "v#":
                                                player.Mode = 3;
                                                player.Mirror = 4;
                                                break;

                                            // Point Mirror
                                            case "p":
                                                player.Mode = 3;
                                                player.Mirror = 5;
                                                break;

                                            // Point Mirror shifted by 0.5 blocks diagonal
                                            case "p#":
                                                player.Mode = 3;
                                                player.Mirror = 6;
                                                break;

                                            // Diagonal x = y Mirror
                                            case "d+":
                                                player.Mode = 3;
                                                player.Mirror = 7;
                                                player.Tell("error");
                                                break;

                                            // Diagonal x = -y Mirror
                                            case "d-":
                                                player.Mode = 3;
                                                player.Mirror = 8;
                                                player.Tell("error");
                                                break;

                                            // No Mirror/ Default
                                            case "off":
                                                player.Mode = 0;
                                                player.Mirror = 0;
                                                player.BrushSize = 1;
                                                break;

                                            default:
                                                player.Tell("error");
                                                break;
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
                                        for(int i = 4; i < param.Length; i += 2)
                                            pos.Add(new Coordinate(int.Parse(param[i]), int.Parse(param[i + 1])));

                                        Queue<Coordinate> queue = new Queue<Coordinate>();
                                        int[] k = Tartaglia((uint)pos.Count);
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
                                                                await PlaceBlock(1, x, (int)Math.Round(y2 + modY * i), bid);
                                                            }
                                                        else
                                                            for (int x = x1, i = 0; x < x2; x++, i++)
                                                            {
                                                                await PlaceBlock(1, x, (int)Math.Round(y1 + modY * i), bid);
                                                            }
                                                    }
                                                    else
                                                    {
                                                        float modX = (float)xC / yC;

                                                        if (y1 > y2)
                                                        {
                                                            for (int y = y2, i = 0; y < y1; y++, i++)
                                                            {
                                                                await PlaceBlock(1, (int)Math.Round(x2 + modX * i), y, bid);
                                                            }
                                                            await PlaceBlock(1, x1, y1, bid);
                                                        }
                                                        else
                                                        {
                                                            for (int y = y1, i = 0; y < y2; y++, i++)
                                                            {
                                                                await PlaceBlock(1, (int)Math.Round(x1 + modX * i), y, bid);
                                                            }
                                                            await PlaceBlock(1, x2, y2, bid);
                                                        }
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                Coordinate cd = queue.Dequeue();
                                                await PlaceBlock(1, cd.X, cd.Y, bid);
                                            }
                                        }
                                    }
                                    else
                                        player.Tell("Invalid number of parameters: must be an odd number (<bid> <sampleNum> <drawLines> <x0> <y0> ...)");
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
                                                player.Mirror = 0;
                                                player.BrushSize = 1;
                                                break;

                                            case "rainbow":
                                                player.Mode = 1;
                                                player.Mirror = 0;
                                                break;

                                            case "paste":
                                                player.Mode = 2;
                                                player.Mirror = 0;
                                                break;

                                            // 3 is mirror awaiting

                                            default:
                                                player.Tell("error");
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
                                                    player.BrushSize = n;
                                                }
                                                catch
                                                {
                                                    player.Tell("error");
                                                }
                                                break;

                                            case "shape":
                                                player.Tell("Haha! No...");
                                                break;

                                            default:
                                                player.Tell("unknown brush option");
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

        /// <summary>
        /// Sends a public message.
        /// </summary>
        /// <param name="format"> String with format. </param>
        /// <param name="args"> Arguments. </param>
        public static void Say(string format, params object[] args) => Con.Send(MessageType.Chat, string.Format(format, args));

        /// <summary>
        /// Sends a public message.
        /// </summary>
        /// <param name="msg"> Message. </param>
        public static void Say(string msg) => Con.Send(MessageType.Chat, msg);

        /// <summary>
        /// Sends a public message.
        /// </summary>
        /// <param name="obj"> Object whose ToString method is to be called. </param>
        public static void Say(object obj) => Con.Send(MessageType.Chat, obj.ToString());

        /// <summary>
        /// Sends a private message.
        /// </summary>
        /// <param name="player"> The receiver. </param>
        /// <param name="format"> String with format. </param>
        /// <param name="args"> Arguments. </param>
        public static void SayPrivate(string player, string format, params object[] args) => Con.Send(MessageType.Chat, "/pm " + player + " " + string.Format(format, args));

        /// <summary>
        /// Sends a private message.
        /// </summary>
        /// <param name="player"> The receiver. </param>
        /// <param name="msg"> Message. </param>
        public static void SayPrivate(string player, string msg) => Con.Send(MessageType.Chat, "/pm " + player + " " + msg);

        /// <summary>
        /// Sends a private message
        /// </summary>
        /// <param name="player"> The receiver. </param>
        /// <param name="obj"> Object whose ToString method is to be called. </param>
        public static void SayPrivate(string player, object obj) => Con.Send(MessageType.Chat, "/pm " + player + " " + obj.ToString());

        public static int[] Tartaglia(uint num)
        {
            int[] res = new int[num];
            int[] prev = new int[num];
            res[0] = 1;
            for (int n = 0; n < num; n++)
            {
                for (int i = 0; i <= n; i++)
                {
                    if (i == 0 || i == n)
                        res[i] = 1;         // First and last numbers are always 1.
                    else
                        res[i] = prev[i - 1] + prev[i];
                }
                res.CopyTo(prev, 0);
            }
            return res;
        }
    }
}
