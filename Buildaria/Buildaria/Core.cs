#region References
using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Terraria;

#endregion

namespace Buildaria
{
    public class Core : Main
    {
        #region Static Properties

        #region The "Not-Really-Properties" Static Fields

        public static SpriteBatch spriteBatch; // It's not really a property, but... meh
        public static Version Version; // Same here
        public static GraphicsDeviceManager Graphics; // ... You guessed it

        #endregion 

        public static Texture2D DefaultTexture { get; set; } // A single white pixel texture
        public static string VersionString { get; set; } // Contains the version information as a simple string
        public static Color SelectionOverlay { get; set; } // The color of the drawn selection overlay
        public static Vector2 TileSize { get; set; } // The size of the tiles... I don't know why I use this

        public static Type WorldGenWrapper { get; set; } // For accessing WorldGen functions
        public static Type MainWrapper { get; set; } // For accessing private Main members

        #endregion 

        #region Selection Fields

        Vector2 sel1 = Vector2.Zero;
        Vector2 sel2 = Vector2.Zero;

        Point SelectionSize = new Point(0, 0);
        Point SelectionPosition = new Point(0, 0);
        bool[,] SelectedTiles = new bool[1,1];

        Point CopiedSize = new Point(0, 0);
        Tile[,] Copied = new Tile[1, 1];
        
        Point UndoSize = new Point(0, 0);
        Point UndoPosition = new Point(0, 0);
        Tile[,] Undo = new Tile[1, 1];

        #endregion

        #region Various Private Fields

        int inventoryType = 0;
        int oldMenuMode = 0;
        Vector2 lastPosition = Vector2.Zero;
        KeyboardState oldKeyState = Keyboard.GetState();

        bool itemHax, godMode, npcsEnabled, hover, buildMode, itemsEnabled, displayMessages, lightMe, saveInventoriesOnSwitch, gridMe;
        string[] displayMessagesMsg, otherToggles, selectionMessages, undoMessage, saveLoadInv, setSpawnPoint, lightMeToggle, mouseCoords, teleportMessages, timeMessage;
        string[] ctlF1, ctlF2, ctlF3, ctlF4, ctlF5, ctlF6, ctlF7, ctlF8, ctlF9, ctlF10, ctlF11, ctlF12;
        string[] selectionColor;

        #endregion

        #region Constructors

        public Core()
        { 
            // Load version information
            Version = Assembly.GetExecutingAssembly().GetName().Version;
            VersionString = Version.Major + "." + Version.Minor;

            if (Version.Build != 0)
            {
                VersionString += "." + Version.Build;
            }
        }

        #endregion

        #region Functions

        public static void GenerateConfigFile()
        {
            XmlWriterSettings writerSettings = new XmlWriterSettings { Encoding = Encoding.UTF8, Indent = true };

            XmlWriter writer = XmlWriter.Create("BuildariaConfig.xml", writerSettings);

            if (writer != null)
            {
                writer.WriteStartElement("Buildaria");

                #region Defaults

                writer.WriteStartElement("Defaults");

                string[] defaultSettings = new string[] {
                    "itemHax_true",
                    "godMode_true",
                    "npcsEnabled_false",
                    "hover_false",
                    "buildMode_true",
                    "itemsEnabled_true",
                    "displayMessages_true",
                    "lightMe_true",
                    "saveInventoriesOnSwitch_false",
                    "gridMe_false"
                };

                foreach (string defaults in defaultSettings)
                {
                    string[] dS = defaults.Split('_');

                    writer.WriteStartElement(dS[0]);
                    writer.WriteString(dS[1]);
                    writer.WriteEndElement();
                }

                writer.WriteEndElement();

                #endregion

                #region Chat Output Colors

                writer.WriteStartElement("chatColors");

                string[] chatColors = new string[] {
                    "displayMessagesMsg_255,255,255",
                    "otherToggles_0,255,0",
                    "selectionMessages_50,50,255",
                    "undoMessage_150,50,50",
                    "saveLoadInv_150,100,0",
                    "setSpawnPoint_255,0,0",
                    "lightMeToggle_255,255,0",
                    "mouseCoords_138,43,226",
                    "teleportMessages_0,255,255",
                    "timeMessage_147,197,114"
                };

                foreach (string cColors in chatColors)
                {
                    string[] cClrs = cColors.Split('_');

                    writer.WriteStartElement(cClrs[0]);
                    writer.WriteString(cClrs[1]);
                    writer.WriteEndElement();
                }

                writer.WriteEndElement();

                #endregion

                #region Core
                writer.WriteStartElement("coreFig");

                string[] coreFig = new string[] {
                    "selectionColor_255,100,0"
                };

                foreach (string cFig in coreFig)
                {
                    string[] crFig = cFig.Split('_');

                    writer.WriteStartElement(crFig[0]);
                    writer.WriteString(crFig[1]);
                    writer.WriteEndElement();
                }

                writer.WriteEndElement();
                #endregion

                writer.WriteEndElement();
                writer.WriteEndDocument();
                writer.Flush();
                writer.Close();
            }
        }

        #endregion

        #region XNA Overrides

        protected override void Initialize()
        {
            // We need a configuration file to continue, if it doesn't exist make one!
            if (!File.Exists("BuildariaConfig.xml"))
            {
                GenerateConfigFile();
            }

            #region Fail Safe Config Data (these are initially set, and then overridden if they exist in the config file)

            // Defaults
            itemHax = true;
            godMode = true;
            npcsEnabled = false;
            hover = false;
            buildMode = true;
            itemsEnabled = true;
            displayMessages = true;
            lightMe = true;
            saveInventoriesOnSwitch = false;
            gridMe = false;

            // Chat Output Colors
            displayMessagesMsg = "255,255,255".Split(',');
            otherToggles = "0,255,0".Split(',');
            selectionMessages = "50,50,255".Split(',');
            undoMessage = "150,50,50".Split(',');
            saveLoadInv = "150,100,0".Split(',');
            setSpawnPoint = "255,0,0".Split(',');
            lightMeToggle = "255,255,0".Split(',');
            mouseCoords = "138,43,226".Split(',');
            teleportMessages = "0,255,255".Split(',');
            timeMessage = "147,197,114".Split(',');

            // Core Configuration
            selectionColor = "255,100,0".Split(',');

            #endregion

            #region Gather Config Data (override the failsafes)

            XmlReaderSettings readerSettings = new XmlReaderSettings { IgnoreComments = true, IgnoreWhitespace = true };
            XmlReader reader = XmlReader.Create("BuildariaConfig.xml", readerSettings);

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    #region Defaults

                    if (reader.Name == "itemHax") itemHax = Convert.ToBoolean(reader.ReadString());
                    if (reader.Name == "godMode") godMode = Convert.ToBoolean(reader.ReadString());
                    if (reader.Name == "npcsEnabled") npcsEnabled = Convert.ToBoolean(reader.ReadString());
                    if (reader.Name == "hover") hover = Convert.ToBoolean(reader.ReadString());
                    if (reader.Name == "buildMode") buildMode = Convert.ToBoolean(reader.ReadString());
                    if (reader.Name == "itemsEnabled") itemsEnabled = Convert.ToBoolean(reader.ReadString());
                    if (reader.Name == "displayMessages") displayMessages = Convert.ToBoolean(reader.ReadString());
                    if (reader.Name == "lightMe") lightMe = Convert.ToBoolean(reader.ReadString());
                    if (reader.Name == "saveInventoriesOnSwitch") saveInventoriesOnSwitch = Convert.ToBoolean(reader.ReadString());
                    if (reader.Name == "gridMe") gridMe = Convert.ToBoolean(reader.ReadString());

                    #endregion

                    #region Chat Output Colors

                    if (reader.Name == "displayMessagesMsg") displayMessagesMsg = reader.ReadString().Split(',');
                    if (reader.Name == "otherToggles") otherToggles = reader.ReadString().Split(',');
                    if (reader.Name == "selectionMessages") selectionMessages = reader.ReadString().Split(',');
                    if (reader.Name == "undoMessage") undoMessage = reader.ReadString().Split(',');
                    if (reader.Name == "saveLoadInv") saveLoadInv = reader.ReadString().Split(',');
                    if (reader.Name == "setSpawnPoint") setSpawnPoint = reader.ReadString().Split(',');
                    if (reader.Name == "lightMeToggle") lightMeToggle = reader.ReadString().Split(',');
                    if (reader.Name == "mouseCoords") mouseCoords = reader.ReadString().Split(',');
                    if (reader.Name == "teleportMessages") teleportMessages = reader.ReadString().Split(',');
                    if (reader.Name == "timeMessage") timeMessage = reader.ReadString().Split(',');

                    #endregion

                    #region Custom Teleport Locations

                    if (reader.Name == "F1") ctlF1 = reader.ReadString().Split(',');
                    if (reader.Name == "F2") ctlF2 = reader.ReadString().Split(',');
                    if (reader.Name == "F3") ctlF3 = reader.ReadString().Split(',');
                    if (reader.Name == "F4") ctlF4 = reader.ReadString().Split(',');
                    if (reader.Name == "F5") ctlF5 = reader.ReadString().Split(',');
                    if (reader.Name == "F6") ctlF6 = reader.ReadString().Split(',');
                    if (reader.Name == "F7") ctlF7 = reader.ReadString().Split(',');
                    if (reader.Name == "F8") ctlF8 = reader.ReadString().Split(',');
                    if (reader.Name == "F9") ctlF9 = reader.ReadString().Split(',');
                    if (reader.Name == "F10") ctlF10 = reader.ReadString().Split(',');
                    if (reader.Name == "F11") ctlF11 = reader.ReadString().Split(',');
                    if (reader.Name == "F12") ctlF12 = reader.ReadString().Split(',');

                    #endregion

                    #region Core

                    if (reader.Name == "selectionColor") selectionColor = reader.ReadString().Split(',');

                    #endregion
                }
            }

            #endregion

            screenHeight = 720;
            screenWidth = 1280;

            base.Initialize();
            spriteBatch = new SpriteBatch(base.GraphicsDevice);

            Texture2D t = new Texture2D(base.GraphicsDevice, 1, 1);
            t.SetData<Color>(new Color[] { new Color(255, 255, 255, 255) });
            DefaultTexture = t;
            
            
            TileSize = new Vector2(16, 16);

            Window.Title = "Buildaria v" + VersionString;
            Main.versionNumber = Window.Title + " on Terraria " + Main.versionNumber;

            SelectionOverlay = new Color(Convert.ToByte(selectionColor[0]), Convert.ToByte(selectionColor[1]), Convert.ToByte(selectionColor[2]), 50);

            MemoryStream stream = new MemoryStream();
            Assembly asm = Assembly.Load(new AssemblyName("Terraria"));
            WorldGenWrapper = asm.GetType("Terraria.WorldGen");
            MainWrapper = asm.GetType("Terraria.Main");

            Inventory.LoadInventories();
        }

        protected override void Update(GameTime gameTime)
        {
            #region Modifier Keys

            // Detect modifier keys
            bool shift = keyState.IsKeyDown(Keys.LeftShift) || keyState.IsKeyDown(Keys.RightShift);
            bool alt = keyState.IsKeyDown(Keys.LeftAlt) || keyState.IsKeyDown(Keys.RightAlt);
            bool ctrl = keyState.IsKeyDown(Keys.LeftControl) || keyState.IsKeyDown(Keys.RightControl);

            #endregion

            #region buildMode

            if (buildMode)
            {
                try
                {
                    FieldInfo tilex = player[myPlayer].GetType().GetField("tileRangeX");
                    FieldInfo tiley = player[myPlayer].GetType().GetField("tileRangeY");
                    tilex.SetValue(player[myPlayer], 1000);
                    tiley.SetValue(player[myPlayer], 1000);

                    for (int i = 0; i < player[myPlayer].inventory.Length; i++)
                    {
                        Item it = player[myPlayer].inventory[i];

                        if (i == 39)
                        {
                            player[myPlayer].inventory[i].SetDefaults(0);
                            player[myPlayer].inventory[i].name = "";
                            player[myPlayer].inventory[i].stack = 0;
                            player[myPlayer].inventory[i].UpdateItem(0);
                        }
                        else if (it.name != "Magic Mirror") // Prevent Magic Mirror being hax'd, which prevents it from working.
                        {
                            if (it.name != "")
                            {
                                #region Item Stack Sizes

                                // Set this to false if you want items to be stacked only to their default max stack size.
                                bool haxItemStack = true;

                                // The amount of items you want in a hax'd stack.
                                int haxItemStackSize = 999;

                                // Note: The haxItemStack toggle has no affect on single-stacked items.
                                // In order to keep them unstackable we need to up their max stack size. 10 is a good, solid, number.
                                if (it.maxStack == 1)
                                {
                                    it.stack = 10;
                                    it.maxStack = 10;
                                }
                                if (haxItemStack)
                                {
                                    it.stack = haxItemStackSize;
                                }
                                else
                                {
                                    it.stack = it.maxStack;
                                }

                                #endregion

                                #region Placeable Items!

                                // ItemName_TileID
                                string[] placeableItems = new string[]    
                                {
                                    "Sapphire_63",
                                    "Ruby_64",
                                    "Emerald_65",
                                    "Topaz_66",
                                    "Amethyst_67",
                                    "Diamond_68"
                                };
                                for (int j = 0; j < placeableItems.Length; j++)
                                {
                                    string[] pi = placeableItems[j].Split('_');
                                    if (pi[0] == it.name)
                                    {
                                        it.useTime = 0;
                                        it.createTile = Convert.ToByte(pi[1]);
                                    }

                                }

                                #endregion

                                #region itemHax

                                if (itemHax)
                                {
                                    if (it.name.Contains("axe") || it.name.Contains("Hammer") || it.useTime == 10 || it.useTime == 7 || it.name.Contains("Phaseblade"))
                                    {
                                        it.autoReuse = true;
                                        it.useTime = 0;
                                    }

                                    if (it.hammer > 0 || it.axe > 0)
                                    {
                                        it.hammer = 300;
                                        it.axe = 300;
                                    }
                                    if (it.pick > 0)
                                    {
                                        it.pick = 300;
                                    }
                                }
                                else
                                {
                                    // Values equal to a Molten Hamaxe
                                    if (it.hammer > 0 || it.axe > 0)
                                    {
                                        it.hammer = 70;
                                        it.axe = 150;
                                        it.useTime = 14;
                                    }
                                    // Values are between a Nightmare Pickaxe and a Molten Pickaxe, favoring each items strong points.
                                    if (it.pick > 0)
                                    {
                                        it.pick = 90;
                                        it.useTime = 12;
                                    }
                                    // Slow down, Spider Man.
                                    if (it.name == "Ivy Whip")
                                    {
                                        it.autoReuse = false;
                                        it.useTime = 20;
                                        it.shoot = 32;
                                        it.shootSpeed = 13;
                                    }
                                    if (it.name == "Grappling Hook")
                                    {
                                        it.autoReuse = false;
                                        it.useTime = 20;
                                        it.shoot = 13;
                                        it.shootSpeed = 11;
                                    }
                                }

                                #endregion
                            }
                        }
                        else
                        {
                            it.SetDefaults(50);
                        }

                        player[myPlayer].inventory[i] = it;
                    }
                }
                catch
                {

                }
            }
            else
            {
                FieldInfo tilex = player[myPlayer].GetType().GetField("tileRangeX");
                FieldInfo tiley = player[myPlayer].GetType().GetField("tileRangeY");
                tilex.SetValue(player[myPlayer], 5);
                tiley.SetValue(player[myPlayer], 4);
            }

            #endregion 

            #region Bucket Management

            // Disabled for now, I don't want this bug holding up 1.8.
            // I'll revisit it and get it fixed for 1.8.1.

            /*bool[] lavaBuckets = new bool[40];
            bool[] waterBuckets = new bool[40];
            bool[] emptyBuckets = new bool[40];
            for (int i = 0; i < player[myPlayer].inventory.Length; i++)
            {
                if (player[myPlayer].inventory[i].type == 0xcf)
                {
                    lavaBuckets[i] = true;
                }
                else if (player[myPlayer].inventory[i].type == 0xce)
                {
                    waterBuckets[i] = true;
                }
                else if (player[myPlayer].inventory[i].type == 0xcd)
                {
                    emptyBuckets[i] = true;
                }
            }

            for (int i = 0; i < player[myPlayer].inventory.Length; i++)
            {
                if (player[myPlayer].inventory[i].type == 0xcd)
                {
                    if (lavaBuckets[i] == true)
                    {
                        player[myPlayer].inventory[i].type = 0xcf;
                    }
                    else if (waterBuckets[i] == true)
                    {
                        player[myPlayer].inventory[i].type = 0xce;
                    }
                    else if (emptyBuckets[i] == true)
                    {
                        player[myPlayer].inventory[i].type = 0xcd;
                    }
                }
            }*/

            #endregion

            try
            {
                base.Update(gameTime);
            }
            catch (Exception e)
            {
                Main.NewText(e.Message);
                LoadInventory(0);
                base.Update(gameTime);
            }

            if (gamePaused)
                return;

            trashItem.SetDefaults(0);

            #region NPC Spawning

            if (keyState.IsKeyDown(Keys.C) && oldKeyState.IsKeyUp(Keys.C) && !editSign && !ctrl)
            {
                npcsEnabled = !npcsEnabled;

                if (displayMessages)
                {
                    Main.NewText("NPCs = " + npcsEnabled, Convert.ToByte(otherToggles[0]), Convert.ToByte(otherToggles[1]), Convert.ToByte(otherToggles[2]));
                }
            }

            if (!npcsEnabled)
            {
                foreach (NPC n in npc)
                {
                    if (!n.friendly)
                    {
                        n.life = 0;
                        n.UpdateNPC(0);
                    }
                }
            }

            #endregion

            #region World Items

            if (keyState.IsKeyDown(Keys.M) && oldKeyState.IsKeyUp(Keys.M) && !editSign && !ctrl)
            {
                itemsEnabled = !itemsEnabled;

                if (displayMessages)
                {
                    Main.NewText("Item Drops = " + itemsEnabled, Convert.ToByte(otherToggles[0]), Convert.ToByte(otherToggles[1]), Convert.ToByte(otherToggles[2]));
                }
            }

            if (!itemsEnabled)
            {
                foreach (Item i in item)
                {
                    i.SetDefaults(0);
                    i.stack = 0;
                    i.name = "";
                    i.UpdateItem(0);
                }
            }

            #endregion

            if (menuMode != oldMenuMode)
            {
                sel1 = -Vector2.One;
                sel2 = -Vector2.One;
            }

            if (menuMode != oldMenuMode && menuMode == 10)
            {
                LoadInventory(Inventory.Inventories.Count - 1);
            }
            else if (menuMode == 10) // if in-game ...
            {

                #region Display Chat Messages

                if (keyState.IsKeyDown(Keys.K) && oldKeyState.IsKeyUp(Keys.K) && !editSign && !ctrl && !shift)
                {
                    displayMessages = !displayMessages;

                    Main.NewText("Display Messages = " + displayMessages, Convert.ToByte(displayMessagesMsg[0]), Convert.ToByte(displayMessagesMsg[1]), Convert.ToByte(displayMessagesMsg[2]));
                }

                #endregion

                #region ItemHax

                if (keyState.IsKeyDown(Keys.T) && oldKeyState.IsKeyUp(Keys.T) && !editSign && !ctrl && !shift)
                {
                    itemHax = !itemHax;

                    if (displayMessages)
                    {
                        Main.NewText("ItemHax = " + itemHax, Convert.ToByte(otherToggles[0]), Convert.ToByte(otherToggles[1]), Convert.ToByte(otherToggles[2]));
                    }
                }

                #endregion

                #region Save/Load Inventories File

                if (ctrl && shift && keyState.IsKeyDown(Keys.S) && oldKeyState.IsKeyUp(Keys.S))
                {
                    SaveInventory(inventoryType);
                    Inventory.SaveInventories();
                }

                if (ctrl && shift && keyState.IsKeyDown(Keys.O) && oldKeyState.IsKeyUp(Keys.O))
                    Inventory.LoadInventories();

                #endregion

                #region Ghost/Hover Mode

                if (hover)
                {
                    player[myPlayer].position = lastPosition;
                    float magnitude = 6f;
                    if (shift)
                    {
                        magnitude *= 4;
                    }
                    if (alt && shift)
                    {
                        magnitude *= 8;
                    }
                    if (player[myPlayer].controlUp || player[myPlayer].controlJump)
                    {
                        player[myPlayer].position = new Vector2(player[myPlayer].position.X, player[myPlayer].position.Y - magnitude);
                    }
                    if (player[myPlayer].controlDown)
                    {
                        player[myPlayer].position = new Vector2(player[myPlayer].position.X, player[myPlayer].position.Y + magnitude);
                    }
                    if (player[myPlayer].controlLeft)
                    {
                        player[myPlayer].position = new Vector2(player[myPlayer].position.X - magnitude, player[myPlayer].position.Y);
                    }
                    if (player[myPlayer].controlRight)
                    {
                        player[myPlayer].position = new Vector2(player[myPlayer].position.X + magnitude, player[myPlayer].position.Y);
                    }
                }

                if (keyState.IsKeyDown(Keys.P) && !oldKeyState.IsKeyDown(Keys.P) && !editSign && !ctrl && !shift)
                {
                    hover = !hover;
                    player[myPlayer].fallStart = (int)player[myPlayer].position.Y;
                    player[myPlayer].immune = true;
                    player[myPlayer].immuneTime = 1000;
                    if (!hover)
                    {
                        player[myPlayer].immune = false;
                    }

                    if (displayMessages)
                    {
                        Main.NewText("NoClip = " + hover, Convert.ToByte(otherToggles[0]), Convert.ToByte(otherToggles[1]), Convert.ToByte(otherToggles[2]));
                    }
                }

                #endregion

				#region Grid (ruler)
                
                if (keyState.IsKeyDown(Keys.R) && oldKeyState.IsKeyUp(Keys.R) && !editSign && !ctrl && !shift)
                {
                    
                    gridMe = !gridMe;
                    if (displayMessages)
                    {
                        Main.NewText("Ruler = " + gridMe, Convert.ToByte(otherToggles[0]), Convert.ToByte(otherToggles[1]), Convert.ToByte(otherToggles[2]));
                    }
                }
                if (gridMe)
                {
                    player[myPlayer].rulerAcc = true;
                }
                

                #endregion
                
                #region God Mode

                if (keyState.IsKeyDown(Keys.G) && oldKeyState.IsKeyUp(Keys.G) && !editSign && !ctrl && !shift)
                {
                    godMode = !godMode;

                    if (displayMessages)
                    {
                        Main.NewText("God Mode = " + godMode, Convert.ToByte(otherToggles[0]), Convert.ToByte(otherToggles[1]), Convert.ToByte(otherToggles[2]));
                    }
                }

                if (godMode)
                {
                    player[myPlayer].accWatch = 3;
                    player[myPlayer].accDepthMeter = 3;
                    player[myPlayer].accCompass = 3;
                    player[myPlayer].accFlipper = true;
                    player[myPlayer].statLife = 400;
                    player[myPlayer].statMana = 200;
                    player[myPlayer].dead = false;
                    player[myPlayer].rocketTimeMax = 1000000;
                    player[myPlayer].rocketTime = 1000;
                    player[myPlayer].canRocket = true;
                    player[myPlayer].fallStart = (int)player[myPlayer].position.Y;
                    player[myPlayer].AddBuff(9, 1, true); // Spelunker effect
                }
                else
                {
                    player[myPlayer].respawnTimer = 1;
                }

                #endregion

                #region Set Default Spawn Location

                if (keyState.IsKeyDown(Keys.L) && oldKeyState.IsKeyUp(Keys.L) && !editSign && !ctrl && !shift)
                {
                    int x = (int)((Main.mouseState.X + Main.screenPosition.X) / 16f);
                    int y = (int)((Main.mouseState.Y + Main.screenPosition.Y) / 16f);

                    Main.spawnTileX = x;
                    Main.spawnTileY = y;

                    if (displayMessages)
                    {
                        Main.NewText("Spawn Location Set", Convert.ToByte(setSpawnPoint[0]), Convert.ToByte(setSpawnPoint[1]), Convert.ToByte(setSpawnPoint[2]));
                    }
                }

                #endregion

                #region Built-in Teleport Locations (bound to F1-F?)

                // F1 - Default Spawn Location
                if (keyState.IsKeyDown(Keys.F1) && oldKeyState.IsKeyUp(Keys.F1) && !editSign && !shift && !ctrl)
                {
                    int x = (int)((Main.spawnTileX * 16f));
                    int y = (int)((Main.spawnTileY * 16f) - 16f);

                    player[myPlayer].position = new Vector2(x, y);
                    Main.NewText("You have been teleported to the Default Spawn Location.", Convert.ToByte(teleportMessages[0]), Convert.ToByte(teleportMessages[1]), Convert.ToByte(teleportMessages[2]));
                }

                // F2 - Dungeon
                if (keyState.IsKeyDown(Keys.F2) && oldKeyState.IsKeyUp(Keys.F2) && !editSign && !shift && !ctrl)
                {
                    // not yet implemented
                }

                // Ocean teleports need further tweaking..

                // F3 - Right Ocean
                /*if (keyState.IsKeyDown(Keys.F3) && oldKeyState.IsKeyUp(Keys.F3) && !editSign && !shift && !ctrl)
                {
                    int x = (int)((Main.rightWorld * 16f) + 2048f);
                    int y = (int)((Main.spawnTileY * 16f) - 16f);

                    hover = true;
                    player[myPlayer].position = new Vector2(x, y);
                    Main.NewText("You have been teleported to the Right Ocean.", Convert.ToByte(teleportMessages[0]), Convert.ToByte(teleportMessages[1]), Convert.ToByte(teleportMessages[2]));
                }

                // F4 - Left Ocean
                if (keyState.IsKeyDown(Keys.F4) && oldKeyState.IsKeyUp(Keys.F4) && !editSign && !shift && !ctrl)
                {
                    int x = (int)((Main.leftWorld * 16f) + 2048f);
                    int y = (int)((Main.spawnTileY * 16f) - 16f);

                    hover = true;
                    player[myPlayer].position = new Vector2(x, y);
                    Main.NewText("You have been teleported to the Left Ocean.", Convert.ToByte(teleportMessages[0]), Convert.ToByte(teleportMessages[1]), Convert.ToByte(teleportMessages[2]));
                }*/

                #endregion

                #region Custom Teleport Locations (bound to Ctrl + F1-F12)

                if (ctlF1 != null)
                {
                    if (ctrl && keyState.IsKeyDown(Keys.F1) && oldKeyState.IsKeyUp(Keys.F1) && !editSign && !shift)
                    {
                        int x = (int)((Convert.ToSingle(ctlF1[1]) * 16f));
                        int y = (int)((Convert.ToSingle(ctlF1[2]) * 16f) - 16f);

                        player[myPlayer].position = new Vector2(x, y);
                        Main.NewText("You have been teleported to " + ctlF1[0], Convert.ToByte(teleportMessages[0]), Convert.ToByte(teleportMessages[1]), Convert.ToByte(teleportMessages[2]));
                    }
                }

                if (ctlF2 != null)
                {
                    if (ctrl && keyState.IsKeyDown(Keys.F2) && oldKeyState.IsKeyUp(Keys.F2) && !editSign && !shift)
                    {
                        int x = (int)((Convert.ToByte(ctlF2[1]) * 16f));
                        int y = (int)((Convert.ToByte(ctlF2[2]) * 16f) - 16f);

                        player[myPlayer].position = new Vector2(x, y);
                        Main.NewText("You have been teleported to " + ctlF2[0], Convert.ToByte(teleportMessages[0]), Convert.ToByte(teleportMessages[1]), Convert.ToByte(teleportMessages[2]));
                    }
                }

                if (ctlF3 != null)
                {
                    if (ctrl && keyState.IsKeyDown(Keys.F3) && oldKeyState.IsKeyUp(Keys.F3) && !editSign && !shift)
                    {
                        int x = (int)((Convert.ToByte(ctlF3[1]) * 16f));
                        int y = (int)((Convert.ToByte(ctlF3[2]) * 16f) - 16f);

                        player[myPlayer].position = new Vector2(x, y);
                        Main.NewText("You have been teleported to " + ctlF3[0], Convert.ToByte(teleportMessages[0]), Convert.ToByte(teleportMessages[1]), Convert.ToByte(teleportMessages[2]));
                    }
                }

                if (ctlF4 != null)
                {
                    if (ctrl && keyState.IsKeyDown(Keys.F4) && oldKeyState.IsKeyUp(Keys.F4) && !editSign && !shift)
                    {
                        int x = (int)((Convert.ToByte(ctlF4[1]) * 16f));
                        int y = (int)((Convert.ToByte(ctlF4[2]) * 16f) - 16f);

                        player[myPlayer].position = new Vector2(x, y);
                        Main.NewText("You have been teleported to " + ctlF4[0], Convert.ToByte(teleportMessages[0]), Convert.ToByte(teleportMessages[1]), Convert.ToByte(teleportMessages[2]));
                    }
                }

                if (ctlF5 != null)
                {
                    if (ctrl && keyState.IsKeyDown(Keys.F5) && oldKeyState.IsKeyUp(Keys.F5) && !editSign && !shift)
                    {
                        int x = (int)((Convert.ToByte(ctlF5[1]) * 16f));
                        int y = (int)((Convert.ToByte(ctlF5[2]) * 16f) - 16f);

                        player[myPlayer].position = new Vector2(x, y);
                        Main.NewText("You have been teleported to " + ctlF5[0], Convert.ToByte(teleportMessages[0]), Convert.ToByte(teleportMessages[1]), Convert.ToByte(teleportMessages[2]));
                    }
                }

                if (ctlF6 != null)
                {
                    if (ctrl && keyState.IsKeyDown(Keys.F6) && oldKeyState.IsKeyUp(Keys.F6) && !editSign && !shift)
                    {
                        int x = (int)((Convert.ToByte(ctlF6[1]) * 16f));
                        int y = (int)((Convert.ToByte(ctlF6[2]) * 16f) - 16f);

                        player[myPlayer].position = new Vector2(x, y);
                        Main.NewText("You have been teleported to " + ctlF6[0], Convert.ToByte(teleportMessages[0]), Convert.ToByte(teleportMessages[1]), Convert.ToByte(teleportMessages[2]));
                    }
                }

                if (ctlF7 != null)
                {
                    if (ctrl && keyState.IsKeyDown(Keys.F7) && oldKeyState.IsKeyUp(Keys.F7) && !editSign && !shift)
                    {
                        int x = (int)((Convert.ToByte(ctlF7[1]) * 16f));
                        int y = (int)((Convert.ToByte(ctlF7[2]) * 16f) - 16f);

                        player[myPlayer].position = new Vector2(x, y);
                        Main.NewText("You have been teleported to " + ctlF7[0], Convert.ToByte(teleportMessages[0]), Convert.ToByte(teleportMessages[1]), Convert.ToByte(teleportMessages[2]));
                    }
                }

                if (ctlF8 != null)
                {
                    if (ctrl && keyState.IsKeyDown(Keys.F8) && oldKeyState.IsKeyUp(Keys.F8) && !editSign && !shift)
                    {
                        int x = (int)((Convert.ToByte(ctlF8[1]) * 16f));
                        int y = (int)((Convert.ToByte(ctlF8[2]) * 16f) - 16f);

                        player[myPlayer].position = new Vector2(x, y);
                        Main.NewText("You have been teleported to " + ctlF8[0], Convert.ToByte(teleportMessages[0]), Convert.ToByte(teleportMessages[1]), Convert.ToByte(teleportMessages[2]));
                    }
                }

                if (ctlF9 != null)
                {
                    if (ctrl && keyState.IsKeyDown(Keys.F9) && oldKeyState.IsKeyUp(Keys.F9) && !editSign && !shift)
                    {
                        int x = (int)((Convert.ToByte(ctlF9[1]) * 16f));
                        int y = (int)((Convert.ToByte(ctlF9[2]) * 16f) - 16f);

                        player[myPlayer].position = new Vector2(x, y);
                        Main.NewText("You have been teleported to " + ctlF9[0], Convert.ToByte(teleportMessages[0]), Convert.ToByte(teleportMessages[1]), Convert.ToByte(teleportMessages[2]));
                    }
                }

                if (ctlF10 != null)
                {
                    if (ctrl && keyState.IsKeyDown(Keys.F10) && oldKeyState.IsKeyUp(Keys.F10) && !editSign && !shift)
                    {
                        int x = (int)((Convert.ToByte(ctlF10[1]) * 16f));
                        int y = (int)((Convert.ToByte(ctlF10[2]) * 16f) - 16f);

                        player[myPlayer].position = new Vector2(x, y);
                        Main.NewText("You have been teleported to " + ctlF10[0], Convert.ToByte(teleportMessages[0]), Convert.ToByte(teleportMessages[1]), Convert.ToByte(teleportMessages[2]));
                    }
                }

                if (ctlF11 != null)
                {
                    if (ctrl && keyState.IsKeyDown(Keys.F11) && oldKeyState.IsKeyUp(Keys.F11) && !editSign && !shift)
                    {
                        int x = (int)((Convert.ToByte(ctlF11[1]) * 16f));
                        int y = (int)((Convert.ToByte(ctlF11[2]) * 16f) - 16f);

                        player[myPlayer].position = new Vector2(x, y);
                        Main.NewText("You have been teleported to " + ctlF11[0], Convert.ToByte(teleportMessages[0]), Convert.ToByte(teleportMessages[1]), Convert.ToByte(teleportMessages[2]));
                    }
                }

                if (ctlF12 != null)
                {
                    if (ctrl && keyState.IsKeyDown(Keys.F12) && oldKeyState.IsKeyUp(Keys.F12) && !editSign && !shift)
                    {
                        int x = (int)((Convert.ToByte(ctlF12[1]) * 16f));
                        int y = (int)((Convert.ToByte(ctlF12[2]) * 16f) - 16f);

                        player[myPlayer].position = new Vector2(x, y);
                        Main.NewText("You have been teleported to " + ctlF12[0], Convert.ToByte(teleportMessages[0]), Convert.ToByte(teleportMessages[1]), Convert.ToByte(teleportMessages[2]));
                    }
                }

                #endregion

                #region Display Coordinates

                if (keyState.IsKeyDown(Keys.I) && oldKeyState.IsKeyUp(Keys.I) && !editSign && !ctrl && !shift)
                {
                    int x = (int)((Main.mouseState.X + Main.screenPosition.X) / 16f);
                    int y = (int)((Main.mouseState.Y + Main.screenPosition.Y) / 16f);

                    Main.NewText("Your mouse currently points to " + x + ", " + y, Convert.ToByte(mouseCoords[0]), Convert.ToByte(mouseCoords[1]), Convert.ToByte(mouseCoords[2]));
                }

                #endregion

                #region System DateTime Display

                if (ctrl && keyState.IsKeyDown(Keys.T) && oldKeyState.IsKeyUp(Keys.T) && !editSign && !shift)
                {
                    Main.NewText("The current system time is " + DateTime.Now.ToString("t"), Convert.ToByte(timeMessage[0]), Convert.ToByte(timeMessage[1]), Convert.ToByte(timeMessage[2]));
                }

                #endregion

                #region Light Me (unlimited Shine Potion & Night Owl Potion buff)

                if (keyState.IsKeyDown(Keys.F) && !oldKeyState.IsKeyDown(Keys.F) && !editSign && !ctrl && !shift)
                {
                    lightMe = !lightMe;

                    if (displayMessages)
                    {
                        Main.NewText("Light Me = " + lightMe, Convert.ToByte(lightMeToggle[0]), Convert.ToByte(lightMeToggle[1]), Convert.ToByte(lightMeToggle[2]));
                    }

                }
                if (lightMe)
                {
                    player[myPlayer].AddBuff(11, 1, false); // Shine effect
                    player[myPlayer].AddBuff(12, 1, false); // Night Owl effect
                }

                #endregion

                bool allowStuff = true; // Disallows most buildaria functionality in-game
                // Set to true if the user may not want certain functions to be happening
                try
                {
                    #region Allow Stuff Detection

                    // Detect if mouse is on a hotbar or inventory is open
                    for (int i = 0; i < 11; i++)
                    {
                        int x = (int)(20f + ((i * 0x38) * inventoryScale));
                        int y = (int)(20f + ((0 * 0x38) * inventoryScale));
                        int index = x;
                        if (((mouseState.X >= x) && (mouseState.X <= (x + (inventoryBackTexture.Width * inventoryScale)))) && ((mouseState.Y >= y) && (mouseState.Y <= (y + (inventoryBackTexture.Height * inventoryScale) + 2))))
                        {
                            i = 11;
                            allowStuff = false;
                            break;
                        }
                    }
                    if (playerInventory || !buildMode || editSign)
                        allowStuff = false;

                    #endregion

                    #region Place Anywhere

                    if (mouseState.LeftButton == ButtonState.Pressed && player[myPlayer].inventory[player[myPlayer].selectedItem].createTile >= 0 && allowStuff)
                    {
                        int x = (int)((Main.mouseState.X + Main.screenPosition.X) / 16f);
                        int y = (int)((Main.mouseState.Y + Main.screenPosition.Y) / 16f);

                        if (Main.tile[x, y].active == false)
                        {
                            byte wall = Main.tile[x, y].wall;
                            Main.tile[x, y] = new Tile();
                            Main.tile[x, y].type = (byte)player[myPlayer].inventory[player[myPlayer].selectedItem].createTile;
                            Main.tile[x, y].wall = wall;
                            Main.tile[x, y].active = true;
                            TileFrame(x, y);
                            SquareWallFrame(x, y, true);
                        }
                    }
                    else if (mouseState.LeftButton == ButtonState.Pressed && player[myPlayer].inventory[player[myPlayer].selectedItem].createWall >= 0 && allowStuff)
                    {
                        int x = (int)((Main.mouseState.X + Main.screenPosition.X) / 16f);
                        int y = (int)((Main.mouseState.Y + Main.screenPosition.Y) / 16f);

                        if (Main.tile[x, y].wall == 0)
                        {
                            if (Main.tile[x, y] == null)
                            {
                                Main.tile[x, y] = new Tile();
                                Main.tile[x, y].type = 0;
                            }

                            Main.tile[x, y].wall = (byte)player[myPlayer].inventory[player[myPlayer].selectedItem].createWall;
                            TileFrame(x, y);
                            SquareWallFrame(x, y, true);
                        }
                    }

                    #endregion

                    #region Inventory Change

                    if (!editSign && !ctrl && !shift)
                    {
                        if (keyState.IsKeyDown(Keys.OemOpenBrackets) && !oldKeyState.IsKeyDown(Keys.OemOpenBrackets) && !editSign)
                        {
                            if (saveInventoriesOnSwitch)
                            {
                                SaveInventory(inventoryType);
                            }
                            /*for (int i = 0; i < Inventories[inventoryType].Length; i++)
                            {
                                player[myPlayer].inventory[i].SetDefaults(Inventories[inventoryType][i].type);
                            }*/
                            LoadInventory(inventoryType - 1);
                        }
                        if (keyState.IsKeyDown(Keys.OemCloseBrackets) && !oldKeyState.IsKeyDown(Keys.OemCloseBrackets) && !editSign)
                        {
                            if (saveInventoriesOnSwitch)
                            {
                                SaveInventory(inventoryType);
                            }
                            /*for (int i = 0; i < Inventories[inventoryType].Length; i++)
                            {
                                player[myPlayer].inventory[i].SetDefaults(Inventories[inventoryType][i].type);
                            }*/
                            LoadInventory(inventoryType + 1);
                        }
                    }

                    #endregion

                    if (allowStuff)
                    {
                        UpdateSelection();

                        #region Alt For Circles

                        if (alt && mouseState.LeftButton == ButtonState.Released)
                        {
                            for (int x = 0; x < SelectionSize.X; x++)
                            {
                                for (int y = 0; y < SelectionSize.Y; y++)
                                {
                                    SelectedTiles[x, y] = false;
                                }
                            }
                            Vector2 center = new Vector2(SelectionSize.X / 2f, SelectionSize.Y / 2f);
                            for (int x = 0; x < SelectionSize.X; x++)
                            {
                                for (int y = 0; y < SelectionSize.Y; y++)
                                {
                                    double dx = (x - center.X + 1) / center.X;
                                    double dy = (y - center.Y + 1) / center.Y;
                                    if (dx * dx + dy * dy < 1)
                                    {
                                        SelectedTiles[x, y] = true;
                                    }
                                }
                            }
                        }

                        #endregion

                        #region Shift For Outline

                        if (shift && mouseState.LeftButton == ButtonState.Released)
                        {
                            bool[,] tempTiles = new bool[SelectionSize.X, SelectionSize.Y];
                            for (int x = 0; x < SelectionSize.X; x++)
                            {
                                for (int y = 0; y < SelectionSize.Y; y++)
                                {
                                    tempTiles[x, y] = SelectedTiles[x, y];
                                }
                            }
                            for (int x = 0; x < SelectionSize.X; x++)
                            {
                                bool found1 = false;
                                bool found2 = false;
                                for (int y = 0; y < SelectionSize.Y; y++)
                                {
                                    if (!found1)
                                    {
                                        found1 = SelectedTiles[x, y];
                                        continue;
                                    }
                                    else if (!found2)
                                    {
                                        if (y + 1 > SelectionSize.Y - 1)
                                        {
                                            found2 = SelectedTiles[x, y];
                                            break;
                                        }
                                        else if (!found2 && !SelectedTiles[x, y + 1])
                                        {
                                            found2 = SelectedTiles[x, y];
                                            break;
                                        }
                                        else
                                        {
                                            SelectedTiles[x, y] = false;
                                        }
                                    }
                                    else if (found1 && found2)
                                        break;
                                }
                            }
                            for (int y = 0; y < SelectionSize.Y; y++)
                            {
                                bool found1 = false;
                                bool found2 = false;
                                for (int x = 0; x < SelectionSize.X; x++)
                                {
                                    if (!found1)
                                    {
                                        found1 = tempTiles[x, y];
                                        continue;
                                    }
                                    else if (!found2)
                                    {
                                        if (x + 1 > SelectionSize.X - 1)
                                        {
                                            found2 = tempTiles[x, y];
                                            break;
                                        }
                                        else if (!found2 && !tempTiles[x + 1, y])
                                        {
                                            found2 = tempTiles[x, y];
                                            break;
                                        }
                                        else
                                        {
                                            tempTiles[x, y] = false;
                                        }
                                    }
                                    else if (found1 && found2)
                                        break;
                                }
                            }
                            for (int x = 0; x < SelectionSize.X; x++)
                            {
                                for (int y = 0; y < SelectionSize.Y; y++)
                                {
                                    SelectedTiles[x, y] = SelectedTiles[x, y] || tempTiles[x, y];
                                }
                            }
                        }

                        #endregion

                        #region Day/Night Skip

                        if (keyState.IsKeyDown(Keys.N) && !oldKeyState.IsKeyDown(Keys.N) && !editSign)
                        {
                            if (dayTime)
                            {
                                time = dayLength + 1;

                                if (displayMessages)
                                {
                                    Main.NewText("Skipped to Dusk", Convert.ToByte(otherToggles[0]), Convert.ToByte(otherToggles[1]), Convert.ToByte(otherToggles[2]));
                                }
                            }
                            else
                            {
                                if (displayMessages)
                                {
                                    Main.NewText("Skipped to Dawn", Convert.ToByte(otherToggles[0]), Convert.ToByte(otherToggles[1]), Convert.ToByte(otherToggles[2]));
                                }
                                time = nightLength;
                            }
                        }

                        #endregion

                        #region Selection Modifications

                        #region Copy/Paste

                        if (ctrl && keyState.IsKeyDown(Keys.C) && oldKeyState.IsKeyUp(Keys.C) && !editSign)
                        {
                            Copied = new Tile[SelectionSize.X, SelectionSize.Y];
                            CopiedSize = new Point(SelectionSize.X, SelectionSize.Y);
                            for (int x = 0; x < SelectionSize.X; x++)
                            {
                                for (int y = 0; y < SelectionSize.Y; y++)
                                {
                                    int copyX = x;
                                    int copyY = y;
                                    if (shift)
                                    {
                                        copyX = Math.Abs(copyX - SelectionSize.X);
                                    }
                                    if (alt)
                                    {
                                        copyY = Math.Abs(copyY - SelectionSize.Y);
                                    }
                                    Copied[copyX, copyY] = new Tile();
                                    Copied[copyX, copyY].type = tile[x + SelectionPosition.X, y + SelectionPosition.Y].type;
                                    Copied[copyX, copyY].active = tile[x + SelectionPosition.X, y + SelectionPosition.Y].active;
                                    Copied[copyX, copyY].wall = tile[x + SelectionPosition.X, y + SelectionPosition.Y].wall;
                                    Copied[copyX, copyY].liquid = tile[x + SelectionPosition.X, y + SelectionPosition.Y].liquid;
                                    Copied[copyX, copyY].lava = tile[x + SelectionPosition.X, y + SelectionPosition.Y].lava;
                                }
                            }

                            if (displayMessages)
                            {
                                Main.NewText("Copied Selection", Convert.ToByte(selectionMessages[0]), Convert.ToByte(selectionMessages[1]), Convert.ToByte(selectionMessages[2]));
                            }
                        }

                        if (ctrl && keyState.IsKeyDown(Keys.V) && oldKeyState.IsKeyUp(Keys.V) && !editSign)
                        {
                            if (sel1 != -Vector2.One && sel2 != -Vector2.One)
                            {
                                Undo = new Tile[CopiedSize.X, CopiedSize.Y];
                                UndoPosition = new Point((int)sel1.X, (int)sel1.Y);
                                UndoSize = new Point(CopiedSize.X, CopiedSize.Y);
                                for (int x = 0; x < CopiedSize.X; x++)
                                {
                                    for (int y = 0; y < CopiedSize.Y; y++)
                                    {
                                        try
                                        {
                                            if (Main.tile[x, y] == null)
                                            {
                                                Main.tile[x, y] = new Tile();
                                                Undo[x, y] = null;
                                            }
                                            else
                                            {
                                                Undo[x, y] = new Tile();
                                                Undo[x, y].type = Main.tile[x, y].type;
                                                Undo[x, y].liquid = Main.tile[x, y].liquid;
                                                Undo[x, y].lava = Main.tile[x, y].lava;
                                                Undo[x, y].wall = Main.tile[x, y].wall;
                                                Undo[x, y].active = Main.tile[x, y].active;
                                            }

                                            int copyX = x;
                                            int copyY = y;
                                            if (shift)
                                            {
                                                copyX = Math.Abs(copyX - CopiedSize.X);
                                            }
                                            if (alt)
                                            {
                                                copyY = Math.Abs(copyY - CopiedSize.Y);
                                            }
                                            tile[(int)sel1.X + x, (int)sel1.Y + y] = new Tile();
                                            tile[(int)sel1.X + x, (int)sel1.Y + y].type = Copied[copyX, copyY].type;
                                            tile[(int)sel1.X + x, (int)sel1.Y + y].active = Copied[copyX, copyY].active;
                                            tile[(int)sel1.X + x, (int)sel1.Y + y].wall = Copied[copyX, copyY].wall;
                                            tile[(int)sel1.X + x, (int)sel1.Y + y].liquid = Copied[copyX, copyY].liquid;
                                            tile[(int)sel1.X + x, (int)sel1.Y + y].lava = Copied[copyX, copyY].lava;
                                            TileFrame((int)sel1.X + x, (int)sel1.Y + y);
                                            SquareWallFrame((int)sel1.X + x, (int)sel1.Y + y);
                                        }
                                        catch
                                        {

                                        }
                                    }
                                }
                            }

                            if (displayMessages)
                            {
                                Main.NewText("Pasted Selection", Convert.ToByte(selectionMessages[0]), Convert.ToByte(selectionMessages[1]), Convert.ToByte(selectionMessages[2]));
                            }
                        }

                        #endregion

                        #region Erasers

                        else if (mouseState.RightButton == ButtonState.Pressed && oldMouseState.RightButton == ButtonState.Released && player[myPlayer].inventory[player[myPlayer].selectedItem].pick >= 55)
                        {
                            Undo = new Tile[SelectionSize.X, SelectionSize.Y];
                            UndoPosition = new Point((int)sel1.X, (int)sel1.Y);
                            UndoSize = new Point(SelectionSize.X, SelectionSize.Y);
                            for (int xp = 0; xp < SelectionSize.X; xp++)
                            {
                                for (int yp = 0; yp < SelectionSize.Y; yp++)
                                {
                                    int x = xp + SelectionPosition.X;
                                    int y = yp + SelectionPosition.Y;
                                    if (SelectedTiles[xp, yp])
                                    {
                                        if (Main.tile[x, y] == null)
                                        {
                                            Main.tile[x, y] = new Tile();
                                            Undo[xp, yp] = null;
                                        }
                                        else
                                        {
                                            Undo[xp, yp] = new Tile();
                                            Undo[xp, yp].type = Main.tile[x, y].type;
                                            Undo[xp, yp].liquid = Main.tile[x, y].liquid;
                                            Undo[xp, yp].lava = Main.tile[x, y].lava;
                                            Undo[xp, yp].wall = Main.tile[x, y].wall;
                                            Undo[xp, yp].active = Main.tile[x, y].active;
                                        }

                                        byte wall = Main.tile[x, y].wall;
                                        Main.tile[x, y].type = 0;
                                        Main.tile[x, y].active = false;
                                        Main.tile[x, y].wall = wall;
                                        TileFrame(x, y);
                                        TileFrame(x, y - 1);
                                        TileFrame(x, y + 1);
                                        TileFrame(x - 1, y);
                                        TileFrame(x + 1, y);
                                        SquareWallFrame(x, y, true);
                                    }
                                }
                            }

                            if (sel1 != -Vector2.One && sel2 != -Vector2.One && displayMessages)
                                Main.NewText("Cleared Selection of Blocks", Convert.ToByte(selectionMessages[0]), Convert.ToByte(selectionMessages[1]), Convert.ToByte(selectionMessages[2]));
                        }
                        else if (mouseState.RightButton == ButtonState.Pressed && oldMouseState.RightButton == ButtonState.Released && player[myPlayer].inventory[player[myPlayer].selectedItem].hammer >= 55)
                        {
                            Undo = new Tile[SelectionSize.X, SelectionSize.Y];
                            UndoPosition = new Point((int)sel1.X, (int)sel1.Y);
                            UndoSize = new Point(SelectionSize.X, SelectionSize.Y);
                            for (int xp = 0; xp < SelectionSize.X; xp++)
                            {
                                for (int yp = 0; yp < SelectionSize.Y; yp++)
                                {
                                    int x = xp + SelectionPosition.X;
                                    int y = yp + SelectionPosition.Y;
                                    if (SelectedTiles[xp, yp])
                                    {
                                        if (Main.tile[x, y] == null)
                                        {
                                            Main.tile[x, y] = new Tile();
                                            Undo[xp, yp] = null;
                                        }
                                        else
                                        {
                                            Undo[xp, yp] = new Tile();
                                            Undo[xp, yp].type = Main.tile[x, y].type;
                                            Undo[xp, yp].liquid = Main.tile[x, y].liquid;
                                            Undo[xp, yp].lava = Main.tile[x, y].lava;
                                            Undo[xp, yp].wall = Main.tile[x, y].wall;
                                            Undo[xp, yp].active = Main.tile[x, y].active;
                                        }

                                        Main.tile[x, y].wall = 0;
                                        TileFrame(x, y);
                                        TileFrame(x, y - 1);
                                        TileFrame(x, y + 1);
                                        TileFrame(x - 1, y);
                                        TileFrame(x + 1, y);
                                        SquareWallFrame(x, y, true);
                                    }
                                }
                            }

                            if (displayMessages)
                            {
                                Main.NewText("Cleared Selection of Walls", Convert.ToByte(selectionMessages[0]), Convert.ToByte(selectionMessages[1]), Convert.ToByte(selectionMessages[2]));
                            }
                        }

                        #endregion

                        #region Liquid (Fill/Erase)

                        else if (mouseState.RightButton == ButtonState.Pressed && oldMouseState.RightButton == ButtonState.Released && player[myPlayer].inventory[player[myPlayer].selectedItem].type == 0xcf)
                        {
                            Undo = new Tile[SelectionSize.X, SelectionSize.Y];
                            UndoPosition = new Point((int)sel1.X, (int)sel1.Y);
                            UndoSize = new Point(SelectionSize.X, SelectionSize.Y);
                            for (int xp = 0; xp < SelectionSize.X; xp++)
                            {
                                for (int yp = 0; yp < SelectionSize.Y; yp++)
                                {
                                    int x = xp + SelectionPosition.X;
                                    int y = yp + SelectionPosition.Y;
                                    if (SelectedTiles[xp, yp])
                                    {
                                        if (Main.tile[x, y] == null)
                                        {
                                            Main.tile[x, y] = new Tile();
                                            Undo[xp, yp] = null;
                                        }
                                        else
                                        {
                                            Undo[xp, yp] = new Tile();
                                            Undo[xp, yp].type = Main.tile[x, y].type;
                                            Undo[xp, yp].liquid = Main.tile[x, y].liquid;
                                            Undo[xp, yp].lava = Main.tile[x, y].lava;
                                            Undo[xp, yp].wall = Main.tile[x, y].wall;
                                            Undo[xp, yp].active = Main.tile[x, y].active;
                                        }

                                        Main.tile[x, y].liquid = 255;
                                        Main.tile[x, y].lava = true;
                                        TileFrame(x, y);
                                        SquareWallFrame(x, y, true);
                                    }
                                }
                            }

                            if (displayMessages)
                            {
                                Main.NewText("Filled Selection with Lava", Convert.ToByte(selectionMessages[0]), Convert.ToByte(selectionMessages[1]), Convert.ToByte(selectionMessages[2]));
                            }
                        }
                        else if (mouseState.RightButton == ButtonState.Pressed && oldMouseState.RightButton == ButtonState.Released && player[myPlayer].inventory[player[myPlayer].selectedItem].type == 0xce)
                        {
                            Undo = new Tile[SelectionSize.X, SelectionSize.Y];
                            UndoPosition = new Point((int)sel1.X, (int)sel1.Y);
                            UndoSize = new Point(SelectionSize.X, SelectionSize.Y);
                            for (int xp = 0; xp < SelectionSize.X; xp++)
                            {
                                for (int yp = 0; yp < SelectionSize.Y; yp++)
                                {
                                    int x = xp + SelectionPosition.X;
                                    int y = yp + SelectionPosition.Y;
                                    if (SelectedTiles[xp, yp])
                                    {
                                        if (Main.tile[x, y] == null)
                                        {
                                            Main.tile[x, y] = new Tile();
                                            Undo[xp, yp] = null;
                                        }
                                        else
                                        {
                                            Undo[xp, yp] = new Tile();
                                            Undo[xp, yp].type = Main.tile[x, y].type;
                                            Undo[xp, yp].liquid = Main.tile[x, y].liquid;
                                            Undo[xp, yp].lava = Main.tile[x, y].lava;
                                            Undo[xp, yp].wall = Main.tile[x, y].wall;
                                            Undo[xp, yp].active = Main.tile[x, y].active;
                                        }

                                        Main.tile[x, y].liquid = 255;
                                        Main.tile[x, y].lava = false;
                                        TileFrame(x, y);
                                        SquareWallFrame(x, y, true);
                                    }
                                }
                            }

                            if (displayMessages)
                            {
                                Main.NewText("Filled Selection with Water", Convert.ToByte(selectionMessages[0]), Convert.ToByte(selectionMessages[1]), Convert.ToByte(selectionMessages[2]));
                            }
                        }
                        else if (mouseState.RightButton == ButtonState.Pressed && oldMouseState.RightButton == ButtonState.Released && player[myPlayer].inventory[player[myPlayer].selectedItem].type == 0xcd)
                        {
                            Undo = new Tile[SelectionSize.X, SelectionSize.Y];
                            UndoPosition = new Point((int)sel1.X, (int)sel1.Y);
                            UndoSize = new Point(SelectionSize.X, SelectionSize.Y);
                            for (int xp = 0; xp < SelectionSize.X; xp++)
                            {
                                for (int yp = 0; yp < SelectionSize.Y; yp++)
                                {
                                    int x = xp + SelectionPosition.X;
                                    int y = yp + SelectionPosition.Y;
                                    if (SelectedTiles[xp, yp])
                                    {
                                        if (Main.tile[x, y] == null)
                                        {
                                            Main.tile[x, y] = new Tile();
                                            Undo[xp, yp] = null;
                                        }
                                        else
                                        {
                                            Undo[xp, yp] = new Tile();
                                            Undo[xp, yp].type = Main.tile[x, y].type;
                                            Undo[xp, yp].liquid = Main.tile[x, y].liquid;
                                            Undo[xp, yp].lava = Main.tile[x, y].lava;
                                            Undo[xp, yp].wall = Main.tile[x, y].wall;
                                            Undo[xp, yp].active = Main.tile[x, y].active;
                                        }

                                        Main.tile[x, y].liquid = 0;
                                        Main.tile[x, y].lava = false;
                                        TileFrame(x, y);
                                        SquareWallFrame(x, y, true);
                                    }
                                }
                            }

                            if (displayMessages)
                            {
                                Main.NewText("Drained Selection of Liquid", 50, 50, 255);
                            }
                        }

                        #endregion

                        #region Fills

                        if (mouseState.RightButton == ButtonState.Pressed && oldMouseState.RightButton == ButtonState.Released && player[myPlayer].inventory[player[myPlayer].selectedItem].createTile >= 0)
                        {
                            Undo = new Tile[SelectionSize.X, SelectionSize.Y];
                            UndoPosition = new Point((int)sel1.X, (int)sel1.Y);
                            UndoSize = new Point(SelectionSize.X, SelectionSize.Y);
                            for (int xp = 0; xp < SelectionSize.X; xp++)
                            {
                                for (int yp = 0; yp < SelectionSize.Y; yp++)
                                {
                                    int x = xp + SelectionPosition.X;
                                    int y = yp + SelectionPosition.Y;
                                    if (SelectedTiles[xp, yp])
                                    {
                                        if (Main.tile[x, y] == null)
                                        {
                                            Undo[xp, yp] = null;
                                        }
                                        else
                                        {
                                            Undo[xp, yp] = new Tile();
                                            Undo[xp, yp].type = Main.tile[x, y].type;
                                            Undo[xp, yp].liquid = Main.tile[x, y].liquid;
                                            Undo[xp, yp].lava = Main.tile[x, y].lava;
                                            Undo[xp, yp].wall = Main.tile[x, y].wall;
                                            Undo[xp, yp].active = Main.tile[x, y].active;
                                        }

                                        byte wall = Main.tile[x, y].wall;
                                        Main.tile[x, y] = new Tile();
                                        Main.tile[x, y].type = (byte)player[myPlayer].inventory[player[myPlayer].selectedItem].createTile;
                                        Main.tile[x, y].wall = wall;
                                        Main.tile[x, y].active = true;
                                        TileFrame(x, y);
                                        SquareWallFrame(x, y, true);
                                    }
                                }
                            }

                            if (displayMessages)
                            {
                                Main.NewText("Filled Selection with Block " + player[myPlayer].inventory[player[myPlayer].selectedItem].createTile, Convert.ToByte(selectionMessages[0]), Convert.ToByte(selectionMessages[1]), Convert.ToByte(selectionMessages[2]));
                            }
                        }
                        else if (mouseState.RightButton == ButtonState.Pressed && oldMouseState.RightButton == ButtonState.Released && player[myPlayer].inventory[player[myPlayer].selectedItem].createWall >= 0)
                        {
                            Undo = new Tile[SelectionSize.X, SelectionSize.Y];
                            UndoPosition = new Point((int)sel1.X, (int)sel1.Y);
                            UndoSize = new Point(SelectionSize.X, SelectionSize.Y);
                            for (int xp = 0; xp < SelectionSize.X; xp++)
                            {
                                for (int yp = 0; yp < SelectionSize.Y; yp++)
                                {
                                    int x = xp + SelectionPosition.X;
                                    int y = yp + SelectionPosition.Y;

                                    if (Main.tile[x, y] == null)
                                    {
                                        Undo[xp, yp] = null;
                                    }
                                    else
                                    {
                                        Undo[xp, yp] = new Tile();
                                        Undo[xp, yp].type = Main.tile[x, y].type;
                                        Undo[xp, yp].liquid = Main.tile[x, y].liquid;
                                        Undo[xp, yp].lava = Main.tile[x, y].lava;
                                        Undo[xp, yp].wall = Main.tile[x, y].wall;
                                        Undo[xp, yp].active = Main.tile[x, y].active;
                                    }

                                    if (SelectedTiles[xp, yp])
                                    {
                                        if (Main.tile[x, y] == null)
                                        {
                                            Main.tile[x, y] = new Tile();
                                            Main.tile[x, y].type = 0;
                                        }

                                        Main.tile[x, y].wall = (byte)player[myPlayer].inventory[player[myPlayer].selectedItem].createWall;
                                        TileFrame(x, y);
                                        SquareWallFrame(x, y, true);
                                    }
                                }
                            }

                            if (displayMessages)
                            {
                                Main.NewText("Filled Selection with Wall " + player[myPlayer].inventory[player[myPlayer].selectedItem].createWall, Convert.ToByte(selectionMessages[0]), Convert.ToByte(selectionMessages[1]), Convert.ToByte(selectionMessages[2]));
                            }
                        }

                        #endregion

                        #region Undo

                        if (ctrl && keyState.IsKeyDown(Keys.Z) && oldKeyState.IsKeyUp(Keys.Z) && !editSign)
                        {
                            for (int xp = 0; xp < UndoSize.X; xp++)
                            {
                                for (int yp = 0; yp < UndoSize.Y; yp++)
                                {
                                    int x = xp + UndoPosition.X;
                                    int y = yp + UndoPosition.Y;

                                    if (Undo[xp, yp] == null)
                                        tile[x, y] = null;
                                    else
                                    {
                                        tile[x, y] = new Tile();
                                        tile[x, y].type = Undo[xp, yp].type;
                                        tile[x, y].active = Undo[xp, yp].active;
                                        tile[x, y].wall = Undo[xp, yp].wall;
                                        tile[x, y].liquid = Undo[xp, yp].liquid;
                                        tile[x, y].lava = Undo[xp, yp].lava;
                                        TileFrame(x, y);
                                        SquareWallFrame(x, y);
                                    }
                                }
                            }

                            if (displayMessages)
                            {
                                Main.NewText("Undo Complete", Convert.ToByte(undoMessage[0]), Convert.ToByte(undoMessage[1]), Convert.ToByte(undoMessage[2]));
                            }
                        }

                        #endregion

                        #endregion
                    }
                }
                catch
                {

                }
            }
            oldMenuMode = menuMode;
            lastPosition = player[myPlayer].position;
            oldKeyState = keyState;
        }

        protected override void Draw(GameTime gameTime)
        {
            /*if (menuMode == 10)
            {
                try
                {
                    int minx = (int)((screenPosition.X / 16) - ((screenWidth / 2) / 16) - 1);
                    int maxx = (int)((screenPosition.X / 16) + ((screenWidth / 2) / 16) + 1);
                    int miny = (int)((screenPosition.Y / 16) - ((screenHeight / 2) / 16) - 1);
                    int maxy = (int)((screenPosition.Y / 16) + ((screenHeight / 2) / 16) + 1);

                    if (minx < 0)
                        minx = 0;

                    if (miny < 0)
                        miny = 0;

                    if (maxx > maxTilesX)
                        maxx = maxTilesX - 1;

                    if (maxy > maxTilesY)
                        maxy = maxTilesY - 1;

                    for (int x = minx; x < maxx; x++)
                    {
                        for (int y = miny; y < maxy; y++)
                        {
                            try
                            {
                                if (tile[x, y] == null)
                                    continue;

                                tile[x, y].lighted = true;
                            }
                            catch
                            {
                                continue;
                            }
                        }
                    }
                }
                catch
                {

                }
            }*/

            base.Draw(gameTime);
            spriteBatch.Begin(SpriteSortMode.BackToFront, BlendState.NonPremultiplied);

            if (menuMode == 10)
            {
                DrawSelectionOverlay();
            }

            spriteBatch.End();
        }

        #endregion

        #region Reflection Haxessors

        public float inventoryScale
        {
            // Accesses Main's private field "inventoryScale" for checking if the mouse is in the hotbar
            get
            {
                object o = MainWrapper.GetField("inventoryScale", BindingFlags.Static | BindingFlags.NonPublic).GetValue(this);
                return (float)o;
            }
        }

        public void TileFrame(int x, int y, bool reset = false, bool breaks = true)
        {
            // Accesses the WorldGen's TileFrame() method for keeping tiles looking presentable when placed with hax
            WorldGenWrapper.GetMethod("TileFrame").Invoke(null, new object[] { x, y, reset, !breaks });
        }

        public void SquareWallFrame(int x, int y, bool reset = false)
        {
            // It's the above, but for walls
            WorldGenWrapper.GetMethod("SquareWallFrame").Invoke(null, new object[] { x, y, reset });
        }

        #endregion

        #region Inventory Functions

        public static void CreateInventories()
        {
            // i[39] is now the trash slot. DO NOT place an item there, it will get overwritten and will cause you frustration!!

            #region Blank
            {
                Item[] i = new Item[53];

                for (int it = 0; it < i.Length; it++)
                {
                    i[it] = new Item();
                }
                i[0].SetDefaults("Copper Pickaxe");
                i[1].SetDefaults("Copper Hammer");
                i[2].SetDefaults("Blue Phaseblade");
                i[2].useStyle = 0;
                i[4].SetDefaults("Ivy Whip");

                Inventory inv = new Inventory(i, "Blank");
                Inventory.AddInventory(inv);
            }
            #endregion

            #region Tier 1 Armor
            {
                Item[] i = new Item[53];

                for (int it = 0; it < i.Length; it++)
                {
                    i[it] = new Item();
                }

                // Row 1
                i[0].SetDefaults("Ivy Whip");

                // Row 2
                i[10].SetDefaults("Copper Helmet");
                i[11].SetDefaults("Iron Helmet");
                i[12].SetDefaults("Silver Helmet");
                i[13].SetDefaults("Gold Helmet");
                i[15].SetDefaults("Mining Helmet");

                // Row 3
                i[20].SetDefaults("Copper Chainmail");
                i[21].SetDefaults("Iron Chainmail");
                i[22].SetDefaults("Silver Chainmail");
                i[23].SetDefaults("Gold Chainmail");
                i[25].SetDefaults("Mining Shirt");

                // Row 4
                i[30].SetDefaults("Copper Greaves");
                i[31].SetDefaults("Iron Greaves");
                i[32].SetDefaults("Silver Greaves");
                i[33].SetDefaults("Gold Greaves");
                i[35].SetDefaults("Mining Pants");

                // Accessories
                i[47].SetDefaults("Cloud in a Balloon");
                i[48].SetDefaults("Spectre Boots");
                i[49].SetDefaults("Obsidian Horseshoe");

                Inventory inv = new Inventory(i, "Tier 1 Armor");
                Inventory.AddInventory(inv);
            }
            #endregion

            #region Tier 2 Armor
            {
                Item[] i = new Item[53];

                for (int it = 0; it < i.Length; it++)
                {
                    i[it] = new Item();
                }

                // Row 1
                i[0].SetDefaults("Ivy Whip");

                // Row 2
                i[10].SetDefaults("Meteor Helmet");
                i[11].SetDefaults("Shadow Helmet");
                i[12].SetDefaults("Necro Helmet");
                i[13].SetDefaults("Jungle Hat");
                i[14].SetDefaults("Molten Helmet");

                // Row 3
                i[20].SetDefaults("Meteor Suit");
                i[21].SetDefaults("Shadow Scalemail");
                i[22].SetDefaults("Necro Breastplate");
                i[23].SetDefaults("Jungle Shirt");
                i[24].SetDefaults("Molten Breastplate");

                // Row 4
                i[30].SetDefaults("Meteor Leggings");
                i[31].SetDefaults("Shadow Greaves");
                i[32].SetDefaults("Necro Greaves");
                i[33].SetDefaults("Jungle Pants");
                i[34].SetDefaults("Molten Greaves");

                // Accessories
                i[47].SetDefaults("Cloud in a Balloon");
                i[48].SetDefaults("Spectre Boots");
                i[49].SetDefaults("Obsidian Horseshoe");

                Inventory inv = new Inventory(i, "Tier 2 Armor");
                Inventory.AddInventory(inv);
            }
            #endregion

            #region Tier 3 Armor
            {
                Item[] i = new Item[53];

                for (int it = 0; it < i.Length; it++)
                {
                    i[it] = new Item();
                }

                // Row 1
                i[0].SetDefaults("Ivy Whip");

                // Row 2
                i[10].SetDefaults("Cobalt Hat");
                i[11].SetDefaults("Cobalt Helmet");
                i[12].SetDefaults("Mythril Hood");
                i[13].SetDefaults("Mythril Helmet");
                i[14].SetDefaults("Adamantite Headgear");
                i[15].SetDefaults("Adamantite Helmet");
                i[16].SetDefaults("Hallowed Headgear");
                i[17].SetDefaults("Hallowed Helmet");

                // Row 3
                i[20].SetDefaults("Cobalt Breastplate");
                i[21].SetDefaults("Cobalt Mask");
                i[22].SetDefaults("Mythril Chainmail");
                i[23].SetDefaults("Mythril Hat");
                i[24].SetDefaults("Adamantite Breastplate");
                i[25].SetDefaults("Adamantite Mask");
                i[26].SetDefaults("Hallowed Plate Mail");
                i[27].SetDefaults("Hallowed Mask");

                // Row 4
                i[30].SetDefaults("Cobalt Leggings");
                i[32].SetDefaults("Mythril Greaves");
                i[34].SetDefaults("Adamantite Leggings");
                i[36].SetDefaults("Hallowed Greaves");

                // Accessories
                i[47].SetDefaults("Cloud in a Balloon");
                i[48].SetDefaults("Spectre Boots");
                i[49].SetDefaults("Obsidian Horseshoe");

                Inventory inv = new Inventory(i, "Tier 3 Armor");
                Inventory.AddInventory(inv);
            }
            #endregion

            #region Throwables & Explosives
            {
                Item[] i = new Item[53];

                for (int it = 0; it < i.Length; it++)
                {
                    i[it] = new Item();
                }

                // Row 1
                i[0].SetDefaults("Ivy Whip");

                // Row 2
                i[10].SetDefaults("Vile Powder");
                i[11].SetDefaults("Shuriken");
                i[12].SetDefaults("Bone");
                i[13].SetDefaults("Spiky Ball");
                i[14].SetDefaults("Throwing Knife");
                i[15].SetDefaults("Poisoned Knife");

                // Row 3
                i[20].SetDefaults("Dynamite");
				i[21].SetDefaults("Grenade");
                i[22].SetDefaults("Bomb");
                i[23].SetDefaults("Sticky Bomb");
                i[24].SetDefaults("Explosives");

                // Row 4
                i[30].SetDefaults("Flamarang");
                i[31].SetDefaults("Thorn Chakram");
				i[32].SetDefaults("Wooden Boomerang");
                i[33].SetDefaults("Enchanted Boomerang");
                i[34].SetDefaults("Light Disc");

                // Equipment
                i[44].SetDefaults("Sunglasses");

                // Accessories
                i[47].SetDefaults("Cloud in a Balloon");
                i[48].SetDefaults("Spectre Boots");
                i[49].SetDefaults("Obsidian Horseshoe");

                Inventory inv = new Inventory(i, "Throwables & Explosives");
                Inventory.AddInventory(inv);
            }
            #endregion

			#region Flails & Spears
            {
                Item[] i = new Item[53];

                for (int it = 0; it < i.Length; it++)
                {
                    i[it] = new Item();
                }

                // Row 1
                i[0].SetDefaults("Ivy Whip");

                // Row 2
                i[10].SetDefaults("Harpoon");
                i[11].SetDefaults("Ball O' Hurt");
                i[12].SetDefaults("Blue Moon");
                i[13].SetDefaults("Sunfury");
                i[14].SetDefaults("Dao of Pow");

                // Row 3
                i[20].SetDefaults("Spear");
                i[21].SetDefaults("Trident");
                i[22].SetDefaults("Dark Lance");
                i[23].SetDefaults("Cobalt Naginata");
                i[24].SetDefaults("Mythril Halberd");
                i[25].SetDefaults("Adamantite Glaive");
                i[26].SetDefaults("Gungnir");

                // Equipment
                i[44].SetDefaults("Sunglasses");

                // Accessories
                i[47].SetDefaults("Cloud in a Balloon");
                i[48].SetDefaults("Spectre Boots");
                i[49].SetDefaults("Obsidian Horseshoe");

                Inventory inv = new Inventory(i, "Flails & Spears");
                Inventory.AddInventory(inv);
            }
            #endregion

			#region Bows & Guns
            {
                Item[] i = new Item[53];

                for (int it = 0; it < i.Length; it++)
                {
                    i[it] = new Item();
                }

                // Row 1
                i[0].SetDefaults("Ivy Whip");
				
                // Row 2
                i[10].SetDefaults("Blowpipe");
                i[11].SetDefaults("Flintlock Pistol");
                i[12].SetDefaults("Musket");
                i[13].SetDefaults("Handgun");
                i[14].SetDefaults("Minishark");
                i[15].SetDefaults("Megashark");
                i[16].SetDefaults("Phoenix Blaster");
                i[17].SetDefaults("Sandgun");
                i[18].SetDefaults("Shotgun");
                i[19].SetDefaults("Space Gun");

                // Row 3
                i[20].SetDefaults("Star Cannon");
                i[21].SetDefaults("Flamethrower");
                i[22].SetDefaults("Clockwork Assault Rifle");
                i[23].SetDefaults("Wooden Bow");
                i[24].SetDefaults("Copper Bow");
                i[25].SetDefaults("Iron Bow");
                i[26].SetDefaults("Silver Bow");
                i[27].SetDefaults("Gold Bow");

                // Row 4
                i[30].SetDefaults("Demon Bow");
                i[31].SetDefaults("Molten Fury");
                i[32].SetDefaults("Cobalt Repeater");
                i[33].SetDefaults("Mythril Repeater");
                i[34].SetDefaults("Adamantite Repeater");
                i[35].SetDefaults("Hallowed Repeater");

                // Equipment
                i[44].SetDefaults("Sunglasses");

                // Accessories
                i[47].SetDefaults("Cloud in a Balloon");
                i[48].SetDefaults("Spectre Boots");
                i[49].SetDefaults("Obsidian Horseshoe");

                Inventory inv = new Inventory(i, "Bows & Guns");
                Inventory.AddInventory(inv);
            }
            #endregion

			#region Magic Weapons
            {
                Item[] i = new Item[53];

                for (int it = 0; it < i.Length; it++)
                {
                    i[it] = new Item();
                }

                // Row 1
                i[0].SetDefaults("Ivy Whip");
				
				// Row 2
                i[10].SetDefaults("Flower of Fire");
                i[11].SetDefaults("Vilethorn");
                i[12].SetDefaults("Magic Missile");
                i[13].SetDefaults("Flamelash");
                i[14].SetDefaults("Water Bolt");
                i[15].SetDefaults("Demon Scythe");
                i[16].SetDefaults("Crystal Storm");
                i[17].SetDefaults("Cursed Flames");

                i[20].SetDefaults("Aqua Scepter");
                i[21].SetDefaults("Laser Rifle");
                i[22].SetDefaults("Magic Dagger");
                i[23].SetDefaults("Magical Harp");
                i[24].SetDefaults("Rainbow Rod");
                i[25].SetDefaults("Ice Rod");

                // Equipment
                i[44].SetDefaults("Sunglasses");

                // Accessories
                i[47].SetDefaults("Cloud in a Balloon");
                i[48].SetDefaults("Spectre Boots");
                i[49].SetDefaults("Obsidian Horseshoe");

                Inventory inv = new Inventory(i, "Magic Weapons");
                Inventory.AddInventory(inv);
            }
            #endregion

			#region Swords
            {
                Item[] i = new Item[53];

                for (int it = 0; it < i.Length; it++)
                {
                    i[it] = new Item();
                }

                // Row 1
                i[0].SetDefaults("Ivy Whip");

                // Row 2
                i[10].SetDefaults("Wooden Sword");
                i[11].SetDefaults("Copper Shortsword");
                i[12].SetDefaults("Copper Broadsword");
                i[13].SetDefaults("Iron Shortsword");
                i[14].SetDefaults("Iron Broadsword");
                i[15].SetDefaults("Silver Shortsword");
                i[16].SetDefaults("Silver Broadsword");
                i[17].SetDefaults("Gold Shortsword");
                i[18].SetDefaults("Gold Broadsword");

                // Row 3
                i[20].SetDefaults("Night's Edge");
                i[21].SetDefaults("Light's Bane");
                i[22].SetDefaults("Starfury");
				i[23].SetDefaults("Muramasa");
                i[24].SetDefaults("Blade of Grass");
                i[25].SetDefaults("Fiery Greatsword");

                // Row 4
                i[30].SetDefaults("Cobalt Sword");
                i[31].SetDefaults("Mythril Sword");
                i[32].SetDefaults("Adamantite Sword");
                i[33].SetDefaults("Breaker Blade");
                i[34].SetDefaults("Excalibur");

                // Equipment
                i[44].SetDefaults("Sunglasses");

                // Accessories
                i[47].SetDefaults("Cloud in a Balloon");
                i[48].SetDefaults("Spectre Boots");
                i[49].SetDefaults("Obsidian Horseshoe");

                Inventory inv = new Inventory(i, "Swords");
                Inventory.AddInventory(inv);
            }
            #endregion

			#region Phase Weapons
            {
                Item[] i = new Item[53];

                for (int it = 0; it < i.Length; it++)
                {
                    i[it] = new Item();
                }

                // Row 1
                i[0].SetDefaults("Ivy Whip");

				// Row 2
                i[10].SetDefaults("White Phaseblade");
                i[11].SetDefaults("Blue Phaseblade");
                i[12].SetDefaults("Red Phaseblade");
                i[13].SetDefaults("Purple Phaseblade");
                i[14].SetDefaults("Green Phaseblade");
                i[15].SetDefaults("Yellow Phaseblade");

                // Row 3
                i[20].SetDefaults("White Phasesaber");
                i[21].SetDefaults("Blue Phasesaber");
                i[22].SetDefaults("Red Phasesaber");
                i[23].SetDefaults("Purple Phasesaber");
                i[24].SetDefaults("Green Phasesaber");
                i[25].SetDefaults("Yellow Phasesaber");

                // Equipment
                i[44].SetDefaults("Sunglasses");

                // Accessories
                i[47].SetDefaults("Cloud in a Balloon");
                i[48].SetDefaults("Spectre Boots");
                i[49].SetDefaults("Obsidian Horseshoe");

                Inventory inv = new Inventory(i, "Phase Weapons");
                Inventory.AddInventory(inv);
            }
            #endregion

            #region Tools
            {
                Item[] i = new Item[53];

                for (int it = 0; it < i.Length; it++)
                {
                    i[it] = new Item();
                }

                // Row 1
                i[0].SetDefaults("Ivy Whip");
                i[1].SetDefaults("Hamdrax");
                i[2].SetDefaults("Purification Poweder");
                i[3].SetDefaults("Holy Water");
                i[4].SetDefaults("Unholy Water");
                

                // Row 2
                i[10].SetDefaults("Copper Pickaxe");
                i[11].SetDefaults("Iron Pickaxe");
                i[12].SetDefaults("Silver Pickaxe");
                i[13].SetDefaults("Gold Pickaxe");
                i[14].SetDefaults("Nightmare Pickaxe");
                i[15].SetDefaults("Molten Pickaxe");
                i[16].SetDefaults("Cobalt Drill");
                i[17].SetDefaults("Mythril Drill");
                i[18].SetDefaults("Adamantite Drill");

                // Row 3
                i[20].SetDefaults("Copper Axe");
                i[21].SetDefaults("Iron Axe");
                i[22].SetDefaults("Silver Axe");
                i[23].SetDefaults("Gold Axe");
                i[24].SetDefaults("War Axe of the Night");
                i[25].SetDefaults("Meteor Hamaxe");
                i[26].SetDefaults("Molten Hamaxe");
                i[27].SetDefaults("Cobalt Chainsaw");
                i[28].SetDefaults("Mythril Chainsaw");
                i[29].SetDefaults("Adamantite Chainsaw");

                // Row 4
                i[30].SetDefaults("Wooden Hammer");
                i[31].SetDefaults("Copper Hammer");
                i[32].SetDefaults("Iron Hammer");
                i[33].SetDefaults("Silver Hammer");
                i[34].SetDefaults("Gold Hammer");
                i[35].SetDefaults("The Breaker");
                i[36].SetDefaults("Pwnhammer");

                // Equipment
                i[44].SetDefaults("Sunglasses");

                // Accessories
                i[47].SetDefaults("Cloud in a Balloon");
                i[48].SetDefaults("Spectre Boots");
                i[49].SetDefaults("Obsidian Horseshoe");

                Inventory inv = new Inventory(i, "Tools");
                Inventory.AddInventory(inv);
            }
            #endregion

            #region Movement Accessories
            {
                Item[] i = new Item[53];

                for (int it = 0; it < i.Length; it++)
                {
                    i[it] = new Item();
                }

                // Row 1
                i[0].SetDefaults("Ivy Whip");
                i[1].SetDefaults("Aglet");
                i[2].SetDefaults("Anklet of the Wind");
                i[3].SetDefaults("Hermes Boots");
                i[4].SetDefaults("Rocket Boots");
                i[5].SetDefaults("Angel Wings");
                i[6].SetDefaults("Demon Wings");
                i[7].SetDefaults("Spectre Boots");
                i[8].SetDefaults("Lucky Horseshoe");
                i[9].SetDefaults("Obsidian Horseshoe");

                // Row 2
                i[10].SetDefaults("Cloud in a Bottle");
                i[11].SetDefaults("Shiny Red Balloon");
                i[12].SetDefaults("Cloud in a Balloon");
                i[13].SetDefaults("Flipper");
                i[14].SetDefaults("Diving Helmet");
                i[15].SetDefaults("Diving Gear");

                // Equipment
                i[44].SetDefaults("Sunglasses");

                // Accessories
                i[47].SetDefaults("Cloud in a Balloon");
                i[48].SetDefaults("Spectre Boots");
                i[49].SetDefaults("Obsidian Horseshoe");

                Inventory inv = new Inventory(i, "Movement Accessories");
                Inventory.AddInventory(inv);
            }
            #endregion

            #region Combat Accessories
            {
                Item[] i = new Item[53];

                for (int it = 0; it < i.Length; it++)
                {
                    i[it] = new Item();
                }

                // Row 1
                i[0].SetDefaults("Ivy Whip");
                i[1].SetDefaults("Cobalt Shield");
                i[2].SetDefaults("Feral Claws");
                i[3].SetDefaults("Obsidian Skull");
                i[4].SetDefaults("Shackle");
                i[5].SetDefaults("Obsidian Shield");
                i[6].SetDefaults("Star Cloak");
                i[7].SetDefaults("Titan Glove");
                i[8].SetDefaults("Cross Necklace");

                // Row 2
                i[10].SetDefaults("Band of Regeneration");
                i[11].SetDefaults("Band of Starpower");
                i[12].SetDefaults("Nature's Gift");
                i[13].SetDefaults("Mana Flower");
                i[14].SetDefaults("Philosopher's Stone");
                i[15].SetDefaults("Ranger Emblem");
                i[16].SetDefaults("Sorcerer Emblem");
                i[17].SetDefaults("Warrior Emblem");

                // Equipment
                i[44].SetDefaults("Sunglasses");

                // Accessories
                i[47].SetDefaults("Cloud in a Balloon");
                i[48].SetDefaults("Spectre Boots");
                i[49].SetDefaults("Obsidian Horseshoe");

                Inventory inv = new Inventory(i, "Combat Accessories");
                Inventory.AddInventory(inv);
            }
            #endregion

            #region Miscellaneous Accessories
            {
                Item[] i = new Item[53];

                for (int it = 0; it < i.Length; it++)
                {
                    i[it] = new Item();
                }

                // Row 1
                i[0].SetDefaults("Ivy Whip");
                i[1].SetDefaults("Depth Meter");
                i[2].SetDefaults("Copper Watch");
                i[3].SetDefaults("Silver Watch");
                i[4].SetDefaults("Gold Watch");
                i[5].SetDefaults("Compass");
                i[6].SetDefaults("GPS");
                
                // Row 2
                i[10].SetDefaults("Ruler");
                i[11].SetDefaults("Toolbelt");
                i[12].SetDefaults("Moon Charm");
                i[13].SetDefaults("Neptune's Shell");

                // Equipment
                i[44].SetDefaults("Sunglasses");

                // Accessories
                i[47].SetDefaults("Cloud in a Balloon");
                i[48].SetDefaults("Spectre Boots");
                i[49].SetDefaults("Obsidian Horseshoe");

                Inventory inv = new Inventory(i, "Miscellaneous Accessories");
                Inventory.AddInventory(inv);
            }
            #endregion

            #region Miscellaneous
            {
                Item[] i = new Item[53];

                for (int it = 0; it < i.Length; it++)
                {
                    i[it] = new Item();
                }

                // Row 1
                i[0].SetDefaults("Ivy Whip");
                i[1].SetDefaults("Grappling Hook");
                i[2].SetDefaults("Dual Hook");
                i[3].SetDefaults("Dirt Rod");
                i[4].SetDefaults("Guide Voodoo Doll");
                i[5].SetDefaults("Orb of Light");
                i[6].SetDefaults("Fairy Bell");
                i[7].SetDefaults("Magic Mirror");
                i[8].SetDefaults("Whoopie Cushion");
                i[9].SetDefaults("Boulder");

                // Row 3
                i[10].SetDefaults("Goblin Battle Standard");
                i[11].SetDefaults("Suspicious Looking Eye");
                i[12].SetDefaults("Worm Food");
                i[13].SetDefaults("Slime Crown");
                i[14].SetDefaults("Mechanical Eye");
                i[15].SetDefaults("Mechanical Worm");
                i[16].SetDefaults("Mechanical Skull");
                i[17].SetDefaults("Golden Key");
                i[18].SetDefaults("Shadow Key");

                // Row 4
                i[20].SetDefaults("Copper Coin");
                i[21].SetDefaults("Silver Coin");
                i[22].SetDefaults("Gold Coin");
                i[23].SetDefaults("Platinum Coin");

                // Equipment
                i[44].SetDefaults("Sunglasses");

                // Accessories
                i[47].SetDefaults("Cloud in a Balloon");
                i[48].SetDefaults("Spectre Boots");
                i[49].SetDefaults("Obsidian Horseshoe");

                Inventory inv = new Inventory(i, "Miscellaneous");
                Inventory.AddInventory(inv);
            }
            #endregion

            #region Vanity Items
            {
                Item[] i = new Item[53];

                for (int it = 0; it < i.Length; it++)
                {
                    i[it] = new Item();
                }

                // Row 1
                i[0].SetDefaults("Ivy Whip");
                i[1].SetDefaults("Goggles");
                i[2].SetDefaults("Sunglasses");
                i[3].SetDefaults("Jungle Rose");
                i[4].SetDefaults("Fish Bowl");
                i[5].SetDefaults("Robe");
                i[6].SetDefaults("Mime Mask");
                i[7].SetDefaults("Bunny Hood");
                i[8].SetDefaults("Summer Hat");
                i[9].SetDefaults("Robot Hat");

                // Row 2
                i[10].SetDefaults("Archaeologist's Hat");
                i[11].SetDefaults("Plumber's Hat");
                i[12].SetDefaults("Top Hat");
                i[13].SetDefaults("Familiar Wig");
                i[14].SetDefaults("Red Hat");
                i[15].SetDefaults("Ninja Hood");
                i[16].SetDefaults("Hero's Hat");
                i[17].SetDefaults("Clown Hat");
                i[19].SetDefaults("Gold Crown");

                // Row 3
                i[20].SetDefaults("Archaeologist's Jacket");
                i[21].SetDefaults("Plumber's Shirt");
                i[22].SetDefaults("Tuxedo Shirt");
                i[23].SetDefaults("Familiar Shirt");
                i[24].SetDefaults("The Doctor's Shirt");
                i[25].SetDefaults("Ninja Shirt");
                i[26].SetDefaults("Hero's Shirt");
                i[27].SetDefaults("Clown Shirt");

                // Row 4
                i[30].SetDefaults("Archaeologist's Pants");
                i[31].SetDefaults("Plumber's Pants");
                i[32].SetDefaults("Tuxedo Pants");
                i[33].SetDefaults("Familiar Pants");
                i[34].SetDefaults("The Doctor's Pants");
                i[35].SetDefaults("Ninja Pants");
                i[36].SetDefaults("Hero's Pants");
                i[37].SetDefaults("Clown Pants");

                // Equipment
                i[44].SetDefaults("Sunglasses");

                // Accessories
                i[47].SetDefaults("Cloud in a Balloon");
                i[48].SetDefaults("Spectre Boots");
                i[49].SetDefaults("Obsidian Horseshoe");

                Inventory inv = new Inventory(i, "Vanity");
                Inventory.AddInventory(inv);
            }
            #endregion

            #region Consumables
            {
                Item[] i = new Item[53];

                for (int it = 0; it < i.Length; it++)
                {
                    i[it] = new Item();
                }

                // Row 1
                i[0].SetDefaults("Ivy Whip");
                i[1].SetDefaults("Lesser Healing Potion");
                i[2].SetDefaults("Lesser Mana Potion");
                i[3].SetDefaults("Lesser Restoration Potion");
                i[4].SetDefaults("Healing Potion");
                i[5].SetDefaults("Mana Potion");
                i[6].SetDefaults("Restoration Potion");
                i[7].SetDefaults("Greater Healing Potion");
                i[8].SetDefaults("Greater Mana Potion");

                // Row 2
                i[11].SetDefaults("Archery Potion");
                i[12].SetDefaults("Battle Potion");
                i[13].SetDefaults("Featherfall Potion");
                i[14].SetDefaults("Gills Potion"); // 291
                i[15].SetDefaults("Gravitation Potion");
                i[16].SetDefaults("Hunter Potion");
                i[17].SetDefaults("Invisibility Potion");
                i[18].SetDefaults("Ironskin Potion");
                i[19].SetDefaults("Magic Power Potion");

                // Row 3
                i[20].SetDefaults("Mana Regeneration Potion");
                i[21].SetDefaults("Night Owl Potion");
                i[22].SetDefaults("Obsidian Skin Potion");
                i[23].SetDefaults("Regeneration Potion");
                i[24].SetDefaults("Shine Potion");
                i[25].SetDefaults("Spelunker Potion");
                i[26].SetDefaults("Swiftness Potion");
                i[27].SetDefaults("Thorns Potion");
                i[28].SetDefaults("Water Walking Potion");

                // Row 4
                i[30].SetDefaults("Mushroom");
                i[31].SetDefaults("Glowing Mushroom");
                i[32].SetDefaults("Ale");
                i[33].SetDefaults("Bowl of Soup");
                i[34].SetDefaults("Goldfish");
                i[36].SetDefaults("Fallen Star");
                i[37].SetDefaults("Life Crystal");
                i[38].SetDefaults("Mana Crystal");

                // Equipment
                i[44].SetDefaults("Sunglasses");

                // Accessories
                i[47].SetDefaults("Cloud in a Balloon");
                i[48].SetDefaults("Spectre Boots");
                i[49].SetDefaults("Obsidian Horseshoe");

                Inventory inv = new Inventory(i, "Consumables");
                Inventory.AddInventory(inv);
            }
            #endregion

            #region Materials: Part 1
            {
                Item[] i = new Item[53];

                for (int it = 0; it < i.Length; it++)
                {
                    i[it] = new Item();
                }

                // Row 1
                i[0].SetDefaults("Copper Pickaxe");
                i[1].SetDefaults("Copper Hammer");
                i[2].SetDefaults("Blue Phaseblade");
                i[2].useStyle = 0;

                i[3].SetDefaults("Cobweb");
                i[4].SetDefaults("Silk");
                i[5].SetDefaults("Gel");
                i[6].SetDefaults("Lens");
                i[7].SetDefaults("Black Lens");
                i[8].SetDefaults("Iron Chain");
                i[9].SetDefaults("Hook");

                // Row 2
                i[10].SetDefaults("Copper Bar");
                i[11].SetDefaults("Iron Bar");
                i[12].SetDefaults("Silver Bar");
                i[13].SetDefaults("Gold Bar");
                i[14].SetDefaults("Demonite Bar");
                i[15].SetDefaults("Meteorite Bar");
                i[16].SetDefaults("Hellstone Bar");
                i[17].SetDefaults("Cobalt Bar");
                i[18].SetDefaults("Mythril Bar");
                i[19].SetDefaults("Adamantite Bar");

                // Row 3
                i[20].SetDefaults("Shadow Scale");
                i[21].SetDefaults("Tattered Cloth");
                i[22].SetDefaults("Leather");
                i[23].SetDefaults("Rotten Chunk");
                i[24].SetDefaults("Worm Tooth");
                i[25].SetDefaults("Cactus");
                i[26].SetDefaults("Stinger");
                i[27].SetDefaults("Water Bucket");
                i[28].SetDefaults("Lava Bucket");
                i[29].SetDefaults("Vile Powder");

                // Row 4
                i[30].SetDefaults("Feather");
                i[31].SetDefaults("Vine");
                i[32].SetDefaults("Jungle Spores");
                i[33].SetDefaults("Shark Fin");
                i[34].SetDefaults("Antlion Mandible");
                i[35].SetDefaults("Illegal Gun Parts");
                i[36].SetDefaults("Glowstick");
                i[37].SetDefaults("Green Dye");
                i[38].SetDefaults("Black Dye");

                // Equipment
                i[44].SetDefaults("Sunglasses");

                // Accessories
                i[47].SetDefaults("Cloud in a Balloon");
                i[48].SetDefaults("Spectre Boots");
                i[49].SetDefaults("Obsidian Horseshoe");

                Inventory inv = new Inventory(i, "Materials: Part 1");
                Inventory.AddInventory(inv);
            }
            #endregion

            #region Materials: Part 2
            {
                Item[] i = new Item[53];

                for (int it = 0; it < i.Length; it++)
                {
                    i[it] = new Item();
                }

                // Row 1
                i[0].SetDefaults("Ivy Whip");

                // Row 2
                i[10].SetDefaults("Bell");
                i[11].SetDefaults("Harp");
                i[12].SetDefaults("Spell Tome");
                i[13].SetDefaults("Cursed Flame");
                i[14].SetDefaults("Dark Shard");
                i[15].SetDefaults("Light Shard");
                i[16].SetDefaults("Pixie Dust");
                i[17].SetDefaults("Unicorn Horn");

                // Row 3
                i[20].SetDefaults("Soul of Flight");
                i[21].SetDefaults("Soul of Fright");
                i[22].SetDefaults("Soul of Light");
                i[23].SetDefaults("Soul of Might");
                i[24].SetDefaults("Soul of Night");
                i[25].SetDefaults("Soul of Sight");

                // Equipment
                i[44].SetDefaults("Sunglasses");

                // Accessories
                i[47].SetDefaults("Cloud in a Balloon");
                i[48].SetDefaults("Spectre Boots");
                i[49].SetDefaults("Obsidian Horseshoe");

                Inventory inv = new Inventory(i, "Materials: Part 2");
                Inventory.AddInventory(inv);
            }
            #endregion

            #region Mechanical
            {
                Item[] i = new Item[53];

                for (int it = 0; it < i.Length; it++)
                {
                    i[it] = new Item();
                }

                // Row 1
                i[0].SetDefaults("Ivy Whip");
                i[1].SetDefaults("Wrench");
                i[2].SetDefaults("Wire Cutter");

                // Row 2
                i[10].SetDefaults("Wire");
                i[11].SetDefaults("Lever");
                i[12].SetDefaults("Switch");
                i[13].SetDefaults("Brown Pressure Plate");
                i[14].SetDefaults("Gray Pressure Plate");
                i[15].SetDefaults("Green Pressure Plate");
                i[16].SetDefaults("Red Pressure Plate");
                i[17].SetDefaults("1 Second Timer");
                i[18].SetDefaults("3 Second Timer");
                i[19].SetDefaults("5 Second Timer");

                // Row 3
                i[20].SetDefaults("Active Stone Block");
                i[21].SetDefaults("Inactive Stone Block");
                i[22].SetDefaults("Inlet Pump");
                i[23].SetDefaults("Outlet Pump");
                i[24].SetDefaults("Dart Trap");

                // Equipment
                i[44].SetDefaults("Sunglasses");

                // Accessories
                i[47].SetDefaults("Cloud in a Balloon");
                i[48].SetDefaults("Spectre Boots");
                i[49].SetDefaults("Obsidian Horseshoe");

                Inventory inv = new Inventory(i, "Mechanical");
                Inventory.AddInventory(inv); ;
            }
            #endregion

            #region Ammo
            {
                Item[] i = new Item[53];

                for (int it = 0; it < i.Length; it++)
                {
                    i[it] = new Item();
                }

                // Row 1
                i[0].SetDefaults("Ivy Whip");

                // Row 2
                i[10].SetDefaults("Wooden Arrow");
                i[11].SetDefaults("Flaming Arrow");
                i[12].SetDefaults("Unholy Arrow");
                i[13].SetDefaults("Jester's Arrow");
                i[14].SetDefaults("Hellfire Arrow");
                i[15].SetDefaults("Holy Arrow");
                i[16].SetDefaults("Cursed Arrow");

                // Row 3
                i[20].SetDefaults("Seed");
                i[21].SetDefaults("Musket Ball");
                i[22].SetDefaults("Silver Bullet");
                i[23].SetDefaults("Meteor Shot");
                i[24].SetDefaults("Crystal Bullet");
                i[25].SetDefaults("Cursed Bullet");

                // Equipment
                i[44].SetDefaults("Sunglasses");

                // Accessories
                i[47].SetDefaults("Cloud in a Balloon");
                i[48].SetDefaults("Spectre Boots");
                i[49].SetDefaults("Obsidian Horseshoe");

                Inventory inv = new Inventory(i, "Ammo");
                Inventory.AddInventory(inv); ;
            }
            #endregion

            #region Alchemy
            {
                Item[] i = new Item[53];

                for (int it = 0; it < i.Length; it++)
                {
                    i[it] = new Item();
                }

                // Row 1
                i[0].SetDefaults("Copper Pickaxe");
                i[1].SetDefaults("Copper Hammer");
                i[2].SetDefaults("Blue Phaseblade");
                i[2].useStyle = 0;
                i[3].SetDefaults("Clay Pot");

                i[4].SetDefaults("Bottled Water");
                i[5].SetDefaults("Bottle");
                i[8].SetDefaults("Acorn");
                i[9].SetDefaults("Sunflower");

                // Row 2
                i[11].SetDefaults("Blinkroot Seeds");
                i[12].SetDefaults("Daybloom Seeds");
                i[13].SetDefaults("Fireblossom Seeds");
                i[14].SetDefaults("Moonglow Seeds");
                i[15].SetDefaults("Deathweed Seeds");
                i[16].SetDefaults("Waterleaf Seeds");

                // Row 3
                i[21].SetDefaults("Blinkroot");
                i[22].SetDefaults("Daybloom");
                i[23].SetDefaults("Fireblossom");
                i[24].SetDefaults("Moonglow");
                i[25].SetDefaults("Deathweed");
                i[26].SetDefaults("Waterleaf");

                // Equipment
                i[44].SetDefaults("Sunglasses");

                // Accessories
                i[47].SetDefaults("Cloud in a Balloon");
                i[48].SetDefaults("Spectre Boots");
                i[49].SetDefaults("Obsidian Horseshoe");

                Inventory inv = new Inventory(i, "Alchemy");
                Inventory.AddInventory(inv);
            }
            #endregion

            #region Statues: Part 1
            {
                Item[] i = new Item[53];

                for (int it = 0; it < i.Length; it++)
                {
                    i[it] = new Item();
                }

                // Row 1
                i[0].SetDefaults("Ivy Whip");
                i[1].SetDefaults("Angel Statue");
                i[2].SetDefaults("Anvil Statue");
                i[3].SetDefaults("Axe Statue");
                i[4].SetDefaults("Bat Statue");
                i[5].SetDefaults("Bird Statue");
                i[6].SetDefaults("Bomb Statue");
                i[7].SetDefaults("Boomerang Statue");
                i[8].SetDefaults("Boot Statue");
                i[9].SetDefaults("Bow Statue");

                // Row 2
                i[10].SetDefaults("Bunny Statue");
                i[11].SetDefaults("Chest Statue");
                i[12].SetDefaults("Corrupt Statue");
                i[13].SetDefaults("Crab Statue");
                i[14].SetDefaults("Cross Statue");
                i[15].SetDefaults("Eyeball Statue");
                i[16].SetDefaults("Fish Statue");
                i[17].SetDefaults("Gargoyle Statue");
                i[18].SetDefaults("Gloom Statue");
                i[19].SetDefaults("Goblin Statue");

                // Row 3
                i[20].SetDefaults("Hammer Statue");
                i[21].SetDefaults("Heart Statue");
                i[22].SetDefaults("Hornet Statue");
                i[23].SetDefaults("Imp Statue");
                i[24].SetDefaults("Jellyfish Statue");
                i[25].SetDefaults("King Statue");
                i[26].SetDefaults("Mushroom Statue");
                i[27].SetDefaults("Pickaxe Statue");
                i[28].SetDefaults("Pillar Statue");
                i[29].SetDefaults("Piranha Statue");

                // Equipment
                i[44].SetDefaults("Sunglasses");

                // Accessories
                i[47].SetDefaults("Cloud in a Balloon");
                i[48].SetDefaults("Spectre Boots");
                i[49].SetDefaults("Obsidian Horseshoe");

                Inventory inv = new Inventory(i, "Statues: Part 2");
                Inventory.AddInventory(inv);
            }
            #endregion

            #region Statues: Part 2
            {
                Item[] i = new Item[53];

                for (int it = 0; it < i.Length; it++)
                {
                    i[it] = new Item();
                }

                // Row 1
                i[0].SetDefaults("Ivy Whip");
                i[1].SetDefaults("Pot Statue");
                i[2].SetDefaults("Potion Statue");
                i[3].SetDefaults("Queen Statue");
                i[4].SetDefaults("Reaper Statue");
                i[5].SetDefaults("Shield Statue");
                i[6].SetDefaults("Skeleton Statue");
                i[7].SetDefaults("Slime Statue");
                i[8].SetDefaults("Spear Statue");
                i[9].SetDefaults("Star Statue");

                // Row 2
                i[10].SetDefaults("Sunflower Statue");
                i[11].SetDefaults("Sword Statue");
                i[12].SetDefaults("Tree Statue");
                i[13].SetDefaults("Woman Statue");

                // Equipment
                i[44].SetDefaults("Sunglasses");

                // Accessories
                i[47].SetDefaults("Cloud in a Balloon");
                i[48].SetDefaults("Spectre Boots");
                i[49].SetDefaults("Obsidian Horseshoe");

                Inventory inv = new Inventory(i, "Statues: Part 2");
                Inventory.AddInventory(inv);
            }
            #endregion

            #region Crafting Stations
            {
                Item[] i = new Item[53];

                for (int it = 0; it < i.Length; it++)
                {
                    i[it] = new Item();
                }

                // Row 1
                i[0].SetDefaults("Copper Pickaxe");
                i[1].SetDefaults("Copper Hammer");
                i[2].SetDefaults("Wooden Chair");
                i[3].SetDefaults("Wooden Table");
                i[4].SetDefaults("Work Bench");
                i[5].SetDefaults("Sawmill");
                i[6].SetDefaults("Keg");
                i[7].SetDefaults("Cooking Pot");
                i[8].SetDefaults("Iron Anvil");
                i[9].SetDefaults("Mythril Anvil");


                // Row 2
                i[10].SetDefaults("Furnace");
                i[11].SetDefaults("Hellforge");
                i[12].SetDefaults("Adamantite Forge");
                i[13].SetDefaults("Loom");
                i[14].SetDefaults("Bookcase");
                i[15].SetDefaults("Tinkerer's Workshop");

                // Equipment
                i[44].SetDefaults("Sunglasses");

                // Accessories
                i[47].SetDefaults("Cloud in a Balloon");
                i[48].SetDefaults("Spectre Boots");
                i[49].SetDefaults("Obsidian Horseshoe");
                
                Inventory inv = new Inventory(i, "Crafting Stations");
                Inventory.AddInventory(inv);
            }
            #endregion

            #region Decorations
            {
                Item[] i = new Item[53];

                for (int it = 0; it < i.Length; it++)
                {
                    i[it] = new Item();
                }

                // Row 1
                i[0].SetDefaults("Copper Pickaxe");
                i[1].SetDefaults("Copper Hammer");
                i[2].SetDefaults("Wooden Door");
                i[3].SetDefaults("Wooden Chair");
                i[4].SetDefaults("Wooden Table");
                i[5].SetDefaults("Bed");
                i[6].SetDefaults("Sign");
                i[7].SetDefaults("Tombstone");
                i[8].SetDefaults("Book");
                i[9].SetDefaults("Bookcase");

                // Row 2
                i[10].SetDefaults("Statue");
                i[11].SetDefaults("Toilet");
                i[12].SetDefaults("Bathtub");
                i[13].SetDefaults("Bench");
                i[14].SetDefaults("Piano");
                i[15].SetDefaults("Grandfather Clock");
                i[16].SetDefaults("Dresser");
                i[17].SetDefaults("Throne");
                i[18].SetDefaults("Pink Vase");
                i[19].SetDefaults("Bowl");

                // Row 3
                i[20].SetDefaults("Mannequin");
                i[21].SetDefaults("Mug");
                i[22].SetDefaults("Coral");
                i[23].SetDefaults("Crystal Shard");
                i[24].SetDefaults("Spike");
                i[25].SetDefaults("Red Banner");
                i[26].SetDefaults("Green Banner");
                i[27].SetDefaults("Blue Banner");
                i[28].SetDefaults("Yellow Banner");

                // Equipment
                i[44].SetDefaults("Sunglasses");

                // Accessories
                i[47].SetDefaults("Cloud in a Balloon");
                i[48].SetDefaults("Spectre Boots");
                i[49].SetDefaults("Obsidian Horseshoe");

                Inventory inv = new Inventory(i, "Decorations");
                Inventory.AddInventory(inv);
            }
            #endregion

            #region Music Boxes
            {
                Item[] i = new Item[53];

                for (int it = 0; it < i.Length; it++)
                {
                    i[it] = new Item();
                }

                // Row 1
                i[0].SetDefaults("Ivy Whip");

                // Row 2
                i[10].SetDefaults("Music Box");
                i[11].SetDefaults("Music Box (Boss 1)");
                i[12].SetDefaults("Music Box (Boss 2)");
                i[13].SetDefaults("Music Box (Boss 3)");
                i[14].SetDefaults("Music Box (Corruption)");
                i[15].SetDefaults("Music Box (Eerie)");
                i[16].SetDefaults("Music Box (Jungle)");
                i[17].SetDefaults("Music Box (Night)");
                i[18].SetDefaults("Music Box (Overworld Day)");
                i[19].SetDefaults("Music Box (The Hallow)");

                // Row 3
                i[20].SetDefaults("Music Box (Title)");
                i[21].SetDefaults("Music Box (Underground)");
                i[22].SetDefaults("Music Box (Underground Corruption)");
                i[23].SetDefaults("Music Box (Underground Hallow)");

                // Equipment
                i[44].SetDefaults("Sunglasses");

                // Accessories
                i[47].SetDefaults("Cloud in a Balloon");
                i[48].SetDefaults("Spectre Boots");
                i[49].SetDefaults("Obsidian Horseshoe");

                Inventory inv = new Inventory(i, "Music Boxes");
                Inventory.AddInventory(inv);
            }
            #endregion

            #region Lighting
            {
                Item[] i = new Item[53];

                for (int it = 0; it < i.Length; it++)
                {
                    i[it] = new Item();
                }

                // Row 1
                i[0].SetDefaults("Copper Pickaxe");
                i[1].SetDefaults("Copper Hammer");
                i[2].SetDefaults("Blue Phaseblade");
                i[2].useStyle = 0;

                i[3].SetDefaults("Torch");
                i[4].SetDefaults("Candle");
                i[5].SetDefaults("Water Candle");
                i[6].SetDefaults("Candelabra");
                i[7].SetDefaults("Skull Lantern");
                i[8].SetDefaults("Tiki Torch");
                i[9].SetDefaults("Lamp Post");

                // Row 3
                i[10].SetDefaults("Cursed Torch");
                i[11].SetDefaults("Demon Torch");
                i[12].SetDefaults("Blue Torch");
                i[13].SetDefaults("Green Torch");
                i[14].SetDefaults("Purple Torch");
                i[15].SetDefaults("Red Torch");
                i[16].SetDefaults("White Torch");
                i[17].SetDefaults("Yellow Torch");

                // Row 2
                i[20].SetDefaults("Copper Chandelier");
                i[21].SetDefaults("Silver Chandelier");
                i[22].SetDefaults("Gold Chandelier");
                i[23].SetDefaults("Chain Lantern");
                i[24].SetDefaults("Chinese Lantern");
                i[25].SetDefaults("Disco Ball");

                // Equipment
                i[44].SetDefaults("Sunglasses");

                // Accessories
                i[47].SetDefaults("Cloud in a Balloon");
                i[48].SetDefaults("Spectre Boots");
                i[49].SetDefaults("Obsidian Horseshoe");
                
                Inventory inv = new Inventory(i, "Lighting");
                Inventory.AddInventory(inv);
            }
            #endregion

            #region Storage
            {
                Item[] i = new Item[53];

                for (int it = 0; it < i.Length; it++)
                {
                    i[it] = new Item();
                }

                // Row 1
                i[0].SetDefaults("Copper Pickaxe");
                i[1].SetDefaults("Copper Hammer");
                i[2].SetDefaults("Blue Phaseblade");
                i[2].useStyle = 0;

                // Row 3
                i[10].SetDefaults("Chest");
                i[11].SetDefaults("Gold Chest");
                i[12].SetDefaults("Shadow Chest");
                i[13].SetDefaults("Barrel");
                i[14].SetDefaults("Trash Can");
                i[16].SetDefaults("Safe");
                i[17].SetDefaults("Piggy Bank");

                // Equipment
                i[44].SetDefaults("Sunglasses");

                // Accessories
                i[47].SetDefaults("Cloud in a Balloon");
                i[48].SetDefaults("Spectre Boots");
                i[49].SetDefaults("Obsidian Horseshoe");

                Inventory inv = new Inventory(i, "Storage");
                Inventory.AddInventory(inv);
            }
            #endregion

            #region Soils & Blocks
            {
                Item[] i = new Item[53];

                for (int it = 0; it < i.Length; it++)
                {
                    i[it] = new Item();
                }

                // Row 1
                i[0].SetDefaults("Copper Pickaxe");
                i[1].SetDefaults("Copper Hammer");
                i[2].SetDefaults("Blue Phaseblade");
                i[2].useStyle = 0;

                i[3].SetDefaults("Torch");
                i[4].SetDefaults("Wood Platform");
                i[5].SetDefaults("Wood");
                i[6].SetDefaults("Dirt Block");
                i[7].SetDefaults("Sand Block");
                i[8].SetDefaults("Clay Block");
                i[9].SetDefaults("Mud Block");

                // Row 2
                i[10].SetDefaults("Ash Block");
                i[11].SetDefaults("Silt Block");
                i[12].SetDefaults("Stone Block");
                i[13].SetDefaults("Ebonstone Block");
                i[14].SetDefaults("Pearlstone Block");
                i[15].SetDefaults("Pearlsand Block");
                i[16].SetDefaults("Ebonsand Block");
                i[17].SetDefaults("Mudstone Block");

                // Row 3
                i[20].SetDefaults("Grass Seeds");
                i[21].SetDefaults("Jungle Grass Seeds");
                i[22].SetDefaults("Mushroom Grass Seeds");
                i[23].SetDefaults("Corrupt Seeds");
                i[24].SetDefaults("Hallowed Seeds");

                // Equipment
                i[44].SetDefaults("Sunglasses");

                // Accessories
                i[47].SetDefaults("Cloud in a Balloon");
                i[48].SetDefaults("Spectre Boots");
                i[49].SetDefaults("Obsidian Horseshoe");

                Inventory inv = new Inventory(i, "Soils & Blocks");
                Inventory.AddInventory(inv);
            }
            #endregion

            #region Ores & Gems
            {
                Item[] i = new Item[53];

                for (int it = 0; it < i.Length; it++)
                {
                    i[it] = new Item();
                }

                // Row 1
                i[0].SetDefaults("Copper Pickaxe");
                i[1].SetDefaults("Copper Hammer");
                i[2].SetDefaults("Blue Phaseblade");
                i[2].useStyle = 0;

                i[3].SetDefaults("Torch");
                i[4].SetDefaults("Wood Platform");

                // Row 2
                i[10].SetDefaults("Copper Ore");
                i[11].SetDefaults("Iron Ore");
                i[12].SetDefaults("Silver Ore");
                i[13].SetDefaults("Gold Ore");
                i[14].SetDefaults("Demonite Ore");
                i[15].SetDefaults("Meteorite");
                i[16].SetDefaults("Obsidian");
                i[17].SetDefaults("Hellstone");

                // Row 3
                i[20].SetDefaults("Cobalt Ore");
                i[21].SetDefaults("Mythril Ore");
                i[22].SetDefaults("Adamantite Ore");

                // Row 4
                i[30].SetDefaults("Amethyst");
                i[31].SetDefaults("Diamond");
                i[32].SetDefaults("Emerald");
                i[33].SetDefaults("Ruby");
                i[34].SetDefaults("Sapphire");
                i[35].SetDefaults("Topaz");

                // Equipment
                i[44].SetDefaults("Sunglasses");

                // Accessories
                i[47].SetDefaults("Cloud in a Balloon");
                i[48].SetDefaults("Spectre Boots");
                i[49].SetDefaults("Obsidian Horseshoe");

                Inventory inv = new Inventory(i, "Ores & Gems");
                Inventory.AddInventory(inv);
            }
            #endregion

            #region Walls
            {
                Item[] i = new Item[53];

                for (int it = 0; it < i.Length; it++)
                {
                    i[it] = new Item();
                }

                // Row 1
                i[0].SetDefaults("Copper Pickaxe");
                i[1].SetDefaults("Copper Hammer");
                i[2].SetDefaults("Blue Phaseblade");
                i[2].useStyle = 0;

                i[3].SetDefaults("Dirt Wall");
                i[4].SetDefaults("Stone Wall");
                i[5].SetDefaults("Wood Wall");
                i[6].SetDefaults("Gray Brick Wall");
                i[7].SetDefaults("Red Brick Wall");
                i[8].SetDefaults("Glass Wall");
                i[9].SetDefaults("Planked Wall");

                // Row 2
                i[10].SetDefaults("Copper Brick Wall");
                i[11].SetDefaults("Silver Brick Wall");
                i[12].SetDefaults("Gold Brick Wall");
                i[13].SetDefaults("Obsidian Brick Wall");
                i[14].SetDefaults("Pink Brick Wall");
                i[15].SetDefaults("Green Brick Wall");
                i[16].SetDefaults("Blue Brick Wall");

                // Row 3
                i[20].SetDefaults("Cobalt Brick Wall");
                i[21].SetDefaults("Iridescent Brick Wall");
                i[22].SetDefaults("Mythril Brick Wall");
                i[23].SetDefaults("Pearlstone Brick Wall");
                i[24].SetDefaults("Mudstone Brick Wall");

                // Equipment
                i[44].SetDefaults("Sunglasses");

                // Accessories
                i[47].SetDefaults("Cloud in a Balloon");
                i[48].SetDefaults("Spectre Boots");
                i[49].SetDefaults("Obsidian Horseshoe");

                Inventory inv = new Inventory(i, "Walls");
                Inventory.AddInventory(inv);
            }
            #endregion

            #region Bricks
            {
                Item[] i = new Item[53];

                for (int it = 0; it < i.Length; it++)
                {
                    i[it] = new Item();
                }

                // Row 1
                i[0].SetDefaults("Copper Pickaxe");
                i[1].SetDefaults("Copper Hammer");
                i[2].SetDefaults("Blue Phaseblade");
                i[2].useStyle = 0;

                i[3].SetDefaults("Torch");
                i[4].SetDefaults("Wood Platform");
                i[5].SetDefaults("Gray Brick");
                i[6].SetDefaults("Red Brick");
                i[7].SetDefaults("Glass");
                i[8].SetDefaults("Wood");
                i[9].SetDefaults("Wooden Beam");

                // Row 2
                i[10].SetDefaults("Copper Brick");
                i[11].SetDefaults("Silver Brick");
                i[12].SetDefaults("Gold Brick");
                i[13].SetDefaults("Obsidian Brick");
                i[14].SetDefaults("Hellstone Brick");
                i[15].SetDefaults("Pink Brick");
                i[16].SetDefaults("Green Brick");
                i[17].SetDefaults("Blue Brick");

                //Row 3
                i[20].SetDefaults("Cobalt Brick");
                i[21].SetDefaults("Demonite Brick");
                i[22].SetDefaults("Iridescent Brick");
                i[23].SetDefaults("Mythril Brick");
                i[24].SetDefaults("Pearlstone Brick");

                // Equipment
                i[44].SetDefaults("Sunglasses");

                // Accessories
                i[47].SetDefaults("Cloud in a Balloon");
                i[48].SetDefaults("Spectre Boots");
                i[49].SetDefaults("Obsidian Horseshoe");

                Inventory inv = new Inventory(i, "Bricks");
                Inventory.AddInventory(inv);
            }
            #endregion
        }

        public int LoadInventory(int id)
        {
            if (id < 0)
            {
                id = Inventory.Inventories.Count - 1;
            }
            else if (id > Inventory.Inventories.Count - 1)
            {
                id = 0;
            }

            Inventory inv = Inventory.Inventories[id];            

            Item[] items = Inventory.IIArrayToItemArray(inv.Items);
            for (int i = 0; i < Inventory.Inventories[id].Items.Length; i++)
            {
                if (i < 44)
                {
                    player[myPlayer].inventory[i].SetDefaults(0);
                    player[myPlayer].inventory[i].SetDefaults(items[i].name);
                }
                else
                {
                    player[myPlayer].armor[i - 44].SetDefaults(0);
                    player[myPlayer].armor[i - 44].SetDefaults(items[i].name);
                }
            }

            if (displayMessages)
            {
                Main.NewText("Loaded " + inv.Name + " Inventory", Convert.ToByte(saveLoadInv[0]), Convert.ToByte(saveLoadInv[1]), Convert.ToByte(saveLoadInv[2]));
            }

            return inventoryType = id;
        }

        public int SaveInventory(int id)
        {
            if (id < 0)
            {
                id = Inventory.Inventories.Count - 1;
            }
            else if (id > Inventory.Inventories.Count - 1)
            {
                id = 0;
            }

            Inventory inv = Inventory.Inventories[id];

            for (int i = 0; i < Inventory.Inventories[id].Items.Length; i++)
            {
                if (i < 44)
                {
                    Inventory.Inventories[id].Items[i].ID = player[myPlayer].inventory[i].type;
                    Inventory.Inventories[id].Items[i].Name = player[myPlayer].inventory[i].name;
                }
                else
                {
                    Inventory.Inventories[id].Items[i].ID = player[myPlayer].armor[i - 44].type;
                    Inventory.Inventories[id].Items[i].Name = player[myPlayer].armor[i - 44].name;
                }
            }
            if (displayMessages)
            {
                Main.NewText("Saved " + inv.Name + " Inventory", Convert.ToByte(saveLoadInv[0]), Convert.ToByte(saveLoadInv[1]), Convert.ToByte(saveLoadInv[2]));
            }

            return inventoryType = id;
        }

        #endregion

        #region Update Functions

        public void UpdateSelection()
        {
            // Button clicked, set first selection point 
            if (mouseState.LeftButton == ButtonState.Pressed && oldMouseState.LeftButton == ButtonState.Released && player[myPlayer].inventory[player[myPlayer].selectedItem].name.ToLower().Contains("phaseblade"))
            {
                int x = (int)((Main.mouseState.X + Main.screenPosition.X) / 16f);
                int y = (int)((Main.mouseState.Y + Main.screenPosition.Y) / 16f);
                sel1 = new Vector2(x, y);
            }

            // Button is being held down, set second point and make sure the selection points are in the right order
            if (mouseState.LeftButton == ButtonState.Pressed && player[myPlayer].inventory[player[myPlayer].selectedItem].name.ToLower().Contains("phaseblade"))
            {
                int x = (int)((Main.mouseState.X + Main.screenPosition.X) / 16f);
                int y = (int)((Main.mouseState.Y + Main.screenPosition.Y) / 16f);
                sel2 = new Vector2(x, y) + Vector2.One;
                if (keyState.IsKeyDown(Keys.LeftShift) || keyState.IsKeyDown(Keys.RightShift))
                {
                    Vector2 size = sel1 - sel2;
                    if (Math.Abs(size.X) != Math.Abs(size.Y))
                    {
                        float min = Math.Min(Math.Abs(size.X), Math.Abs(size.Y));
                        if (sel2.X > sel1.X)
                        {
                            sel2 = new Vector2(sel1.X + min, sel2.Y);
                        }
                        else
                        {
                            sel2 = new Vector2(sel1.X - min, sel2.Y);
                        }
                        if (sel2.Y > sel1.Y)
                        {
                            sel2 = new Vector2(sel2.X, sel1.Y + min);
                        }
                        else
                        {
                            sel2 = new Vector2(sel2.X, sel1.Y - min);
                        }
                    }
                }
            }

            // Clear selection
            if (mouseState.RightButton == ButtonState.Pressed && player[myPlayer].inventory[player[myPlayer].selectedItem].name.ToLower().Contains("phaseblade"))
            {
                sel1 = -Vector2.One;
                sel2 = -Vector2.One;
            }

            // Check inside the selection and set SelectedTiles accordingly
            int minx = (int)Math.Min(sel1.X, sel2.X);
            int maxx = (int)Math.Max(sel1.X, sel2.X);
            int miny = (int)Math.Min(sel1.Y, sel2.Y);
            int maxy = (int)Math.Max(sel1.Y, sel2.Y);
            SelectedTiles = new bool[maxx - minx, maxy - miny];
            SelectionSize = new Point(maxx - minx, maxy - miny);
            SelectionPosition = new Point(minx, miny);
            for (int x = 0; x < SelectionSize.X; x++)
            {
                for (int y = 0; y < SelectionSize.Y; y++)
                {
                    SelectedTiles[x, y] = true;
                }
            }
        }

        #endregion

        #region Draw Functions

        public void DrawSelectionOverlay()
        {
            // TODO: Properly cull the tiles so I'm not killing people trying to select massive areas
            // BROKEN: This code offsets the selection position as you move it off the screen to left - i.e, moving right

            if ((sel1 == -Vector2.One && sel2 == -Vector2.One) || (sel1 == Vector2.Zero && sel2 == Vector2.Zero && SelectionSize.X == 0 && SelectionSize.Y == 0))
                return;

            Vector2 offset = new Vector2(((int)(screenPosition.X)), ((int)(screenPosition.Y)));
            int minx = (int)Math.Max(SelectionPosition.X * TileSize.X, (SelectionPosition.X * TileSize.X) - ((int)(screenPosition.X / TileSize.X)) * TileSize.X);
            int diffx = (int)(SelectionPosition.X * TileSize.X) - minx;
            int maxx = minx + (int)Math.Max(SelectionSize.X * TileSize.X, screenWidth) + diffx;
            int miny = (int)Math.Max(SelectionPosition.Y * TileSize.Y, screenPosition.Y);
            int diffy = (int)(SelectionPosition.Y * TileSize.Y) - miny;
            int maxy = miny + (int)Math.Min(SelectionSize.Y * TileSize.Y, screenHeight) + diffy;
            for (int x = minx; x < maxx; x += (int)TileSize.X)
            {
                for (int y = miny; y < maxy; y += (int)TileSize.Y)
                {
                    int tx = (int)((x - minx) / TileSize.X);
                    int ty = (int)((y - miny) / TileSize.Y);
                    if (ty >= SelectionSize.Y)
                        continue;
                    if (tx >= SelectionSize.X)
                        break;
                    if (SelectedTiles[tx, ty])
                    {
                        Vector2 cull = (new Vector2(tx + (minx / TileSize.X), ty + (miny / TileSize.Y)) * TileSize) - offset;
                        spriteBatch.Draw(DefaultTexture, cull, null, SelectionOverlay, 0, Vector2.Zero, TileSize, SpriteEffects.None, 0);
                    }
                }
            }
        }

        #endregion 

        #region OnExit Cleanup

        /*protected override void OnExiting(object sender, EventArgs args)
        {
            // This happens when a user clicks the Exit option at the main menu.
            // Nothing to see here, yet.
            
        }*/

        #endregion
    }
}
