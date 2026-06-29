using Engine.Core.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Core.ECS
{
    public class ManagersManager
    {

        // Local Scene Managers (e.g., VoxelWorldManager, SceneAudioManager)
        private readonly List<GameManager> Managers = new();

        public GameScene ContextScene { get; set; } = null!;

        #region Local Scene Managers (Services)

        /// <summary>
        /// Registers a specialized data manager or service to this local scene workspace.
        /// </summary>
        public void AddManager(GameManager managerInstance)
        {
            if(managerInstance == null)
                return;

            Type managerType = managerInstance.GetType();

            // Prevent duplicate manager types of the exact same derived subclass from piling up
            if(Managers.Any(m => m.GetType() == managerType))
            {
                throw new ArgumentException($"A manager of type {managerType.Name} is already registered to this scene.");
            }

            Managers.Add(managerInstance);
        }

        /// <summary>
        /// Retrieves a specialized scene manager by its derived subclass type.
        /// </summary>
        public T? GetManager<T>() where T : GameManager
        {
            // Search the polymorphic list for any instance that matches or derives from T
            return Managers.OfType<T>().FirstOrDefault();
        }

        /// <summary>
        /// Exposes the flat list of scene-bound managers 
        /// </summary>
        public IReadOnlyList<GameManager> GetRegisteredManagers()
        {
            return Managers;
        }

        #endregion
    }
}
