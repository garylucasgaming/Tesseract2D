using Engine.Core.ECS;
using Engine.Core.Serialization;
using System;
using System.Collections.Generic;

namespace Engine.Core.Runtime
{
    /// <summary>
    /// Host-level response to an assembly swap. Live component instances are
    /// not rehydrated; the host requires a scene reload instead.
    /// </summary>
    public enum HotReloadState
    {
        UpToDate,
        ReloadRequired
    }

    public sealed class HotReloadStateChangedEventArgs : EventArgs
    {
        public HotReloadStateChangedEventArgs(HotReloadState state)
        {
            State = state;
        }

        public HotReloadState State { get; }
    }

    /// <summary>
    /// Shared, host-owned frame loop. DesktopGL, headless tests, and the editor
    /// delegate the common update/render sequence to this type instead of each
    /// reimplementing manager and system lifecycle calls.
    /// </summary>
    public sealed class GameHost
    {
        private readonly HashSet<GameManager> _initializedManagers = new();
        private bool _initialized;
        private bool _shutdown;
        private string? _scenePath;
        private DatabaseManager? _sceneDatabase;

        public GameHost(
            IEngineContext? context = null,
            SessionGlobals? session = null,
            IFrameRenderer? renderer = null)
        {
            Context = context ?? session?.Context ?? EngineContext.Current;
            Session = session ?? new SessionGlobals(Context);
            Renderer = renderer ?? new NullFrameRenderer();
        }

        public IEngineContext Context { get; }
        public SessionGlobals Session { get; }
        public IFrameRenderer Renderer { get; }
        public GameScene? Scene { get; private set; }
        public HotReloadState HotReloadState { get; private set; } = HotReloadState.UpToDate;

        public event EventHandler<HotReloadStateChangedEventArgs>? HotReloadStateChanged;

        public void Initialize()
        {
            if(_initialized)
                return;

            Renderer.Initialize(Session);
            _initialized = true;
            _shutdown = false;
        }

        public GameScene LoadScene(string absoluteFilePath, DatabaseManager? database = null)
        {
            Initialize();

            ShutdownSceneManagers();

            Scene = SceneSerializer.LoadScene(
                absoluteFilePath,
                context: Context,
                database: database,
                session: Session);
            _scenePath = absoluteFilePath;
            _sceneDatabase = database;
            Context.ActiveScene = Scene;
            SetHotReloadState(HotReloadState.UpToDate);

            return Scene;
        }

        public void AttachScene(GameScene scene)
        {
            Initialize();

            ShutdownSceneManagers();

            Scene = scene ?? throw new ArgumentNullException(nameof(scene));
            _scenePath = null;
            _sceneDatabase = null;
            Context.ActiveScene = scene;
            SetHotReloadState(HotReloadState.UpToDate);
        }

        /// <summary>
        /// Records an assembly swap. Components already present in the scene are
        /// not rehydrated, so consumers must run <c>scene.reload</c>.
        /// </summary>
        public void NotifyAssemblyReloaded()
        {
            SetHotReloadState(HotReloadState.ReloadRequired);
        }

        /// <summary>
        /// Host command path for editor/UI callers. Unknown commands are ignored.
        /// </summary>
        public bool TryExecuteCommand(string command)
        {
            if(!string.Equals(command, "scene.reload", StringComparison.OrdinalIgnoreCase))
                return false;

            if(string.IsNullOrWhiteSpace(_scenePath))
                return false;

            ReloadScene();
            return true;
        }

        public GameScene ReloadScene()
        {
            if(string.IsNullOrWhiteSpace(_scenePath))
            {
                throw new InvalidOperationException(
                    "[GameHost] Cannot reload a scene that was attached without a file path.");
            }

            return LoadScene(_scenePath, _sceneDatabase);
        }

        /// <summary>
        /// Runs one variable-step frame. <paramref name="beforeSceneUpdate"/> is
        /// the host-specific slot between input/manager updates and the engine's
        /// system update (the editor uses it for gizmo and fixed-tick work).
        /// </summary>
        public void Update(float deltaTime, Action? beforeSceneUpdate = null)
        {
            var scene = RequireScene();

            Session.UpdateInput();
            InitializeSceneManagers(scene);

            foreach(var manager in scene.Managers.GetRegisteredManagers())
            {
                if(manager.IsActive)
                    manager.Update(deltaTime);
            }

            beforeSceneUpdate?.Invoke();
            scene.Update(deltaTime, Context.PlayState);
        }

        public void TickUpdate(float fixedDeltaTime)
        {
            RequireScene().TickUpdate(fixedDeltaTime, Context.PlayState);
        }

        public void Render()
        {
            Renderer.Render(RequireScene(), Session);
        }

        public void RunFrames(int frameCount, float deltaTime, float fixedDeltaTime = 1f / 60f)
        {
            Initialize();

            for(var frame = 0; frame < frameCount; frame++)
            {
                Update(deltaTime);
                TickUpdate(fixedDeltaTime);
                Render();
            }
        }

        public void Shutdown()
        {
            if(_shutdown)
                return;

            ShutdownSceneManagers();
            Renderer.Shutdown();
            _initialized = false;
            _shutdown = true;
        }

        private GameScene RequireScene()
        {
            Initialize();
            return Scene ?? throw new InvalidOperationException(
                "[GameHost] No scene is loaded. Call LoadScene or AttachScene first.");
        }

        private void SetHotReloadState(HotReloadState state)
        {
            if(HotReloadState == state)
                return;

            HotReloadState = state;
            HotReloadStateChanged?.Invoke(this, new HotReloadStateChangedEventArgs(state));
        }

        private void ShutdownSceneManagers()
        {
            if(Scene != null)
            {
                foreach(var manager in Scene.Managers.GetRegisteredManagers())
                {
                    manager.Shutdown();
                }
            }

            _initializedManagers.Clear();
        }

        private void InitializeSceneManagers(GameScene scene)
        {
            foreach(var manager in scene.Managers.GetRegisteredManagers())
            {
                if(!manager.IsActive)
                    continue;

                manager.ContextScene = scene;
                if(_initializedManagers.Add(manager))
                    manager.Initialize();
            }
        }
    }
}
