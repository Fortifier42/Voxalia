﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ShadowOperations.Shared;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using ShadowOperations.ClientGame.EntitySystem;
using ShadowOperations.ClientGame.GraphicsSystems;
using ShadowOperations.ClientGame.UISystem;
using OpenTK.Input;
using ShadowOperations.ClientGame.CommandSystem;

namespace ShadowOperations.ClientGame.ClientMainSystem
{
    /// <summary>
    /// The center of all client activity in Shadow Operations.
    /// </summary>
    public partial class Client
    {
        /// <summary>
        /// The primary running client.
        /// </summary>
        public static Client Central = null;

        /// <summary>
        /// Starts up a new server.
        /// </summary>
        public static void Init()
        {
            Central = new Client();
            Central.StartUp();
        }

        /// <summary>
        /// The main Window used by the game.
        /// </summary>
        public GameWindow Window;

        public ClientCommands Commands;

        public ClientCVar CVars;

        /// <summary>
        /// Start up and run the server.
        /// </summary>
        public void StartUp()
        {
            SysConsole.Output(OutputType.INIT, "Launching as new client, this is " + (this == Central ? "" : "NOT ") + "the Central client.");
            SysConsole.Output(OutputType.INIT, "Loading command system...");
            Commands = new ClientCommands();
            Commands.Init(new ClientOutputter(this), this); // TODO: Actual outputter
            SysConsole.Output(OutputType.INIT, "Loading CVar system...");
            CVars = new ClientCVar();
            CVars.Init(Commands.Output);
            SysConsole.Output(OutputType.INIT, "Generating window...");
            Window = new GameWindow(800, 600, GraphicsMode.Default, Program.GameName + " v" + Program.GameVersion,
                GameWindowFlags.Default, DisplayDevice.Default, 4, 3, GraphicsContextFlags.ForwardCompatible);
            Window.Load += new EventHandler<EventArgs>(Window_Load);
            Window.UpdateFrame += new EventHandler<FrameEventArgs>(Window_UpdateFrame);
            Window.RenderFrame += new EventHandler<FrameEventArgs>(Window_RenderFrame);
            Window.Mouse.Move += new EventHandler<MouseMoveEventArgs>(MouseHandler.Window_MouseMove);
            Window.KeyDown += new EventHandler<KeyboardKeyEventArgs>(KeyHandler.PrimaryGameWindow_KeyDown);
            Window.KeyPress += new EventHandler<KeyPressEventArgs>(KeyHandler.PrimaryGameWindow_KeyPress);
            Window.KeyUp += new EventHandler<KeyboardKeyEventArgs>(KeyHandler.PrimaryGameWindow_KeyUp);
            Window.Mouse.WheelChanged += new EventHandler<MouseWheelEventArgs>(KeyHandler.Mouse_Wheel);
            Window.Mouse.ButtonDown += new EventHandler<MouseButtonEventArgs>(KeyHandler.Mouse_ButtonDown);
            Window.Mouse.ButtonUp += new EventHandler<MouseButtonEventArgs>(KeyHandler.Mouse_ButtonUp);
            Window.Run();
        }

        public TextureEngine Textures;
        public ShaderEngine Shaders;
        public GLFontEngine Fonts;
        public FontSetEngine FontSets;
        public Renderer Rendering;

        public PlayerEntity Player;

        void Window_Load(object sender, EventArgs e)
        {
            SysConsole.Output(OutputType.INIT, "Window generated!");
            SysConsole.Output(OutputType.INIT, "Loading textures...");
            Textures = new TextureEngine();
            Textures.InitTextureSystem();
            SysConsole.Output(OutputType.INIT, "Loading shaders...");
            Shaders = new ShaderEngine();
            Shaders.InitShaderSystem();
            SysConsole.Output(OutputType.INIT, "Loading fonts...");
            Fonts = new GLFontEngine(Shaders);
            Fonts.Init();
            FontSets = new FontSetEngine(Fonts);
            FontSets.Init();
            SysConsole.Output(OutputType.INIT, "Loading rendering helper...");
            Rendering = new Renderer(Textures);
            SysConsole.Output(OutputType.INIT, "Preparing rendering engine...");
            InitRendering();
            SysConsole.Output(OutputType.INIT, "Building physics world...");
            BuildWorld();
            SysConsole.Output(OutputType.INIT, "Spawning a cuboid entity and the player...");
            CubeEntity ce = new CubeEntity(this, new Location(50, 50, 5));
            ce.SetPosition(new Location(0, 0, -5));
            ce.SetMass(0);
            SpawnEntity(ce);
            Player = new PlayerEntity(this);
            Player.SetPosition(new Location(0, 0, 10));
            SpawnEntity(Player);
        }
    }
}
