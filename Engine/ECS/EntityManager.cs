
using Engine.Core.ECS.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Core.ECS
{
    public class EntityManager
    {
        // Master Storage: Fast O(1) retrieval, insertion, and deletion by ID
        private readonly Dictionary<Guid, GameObject> _entities = new();

        public GameScene ContextScene { get; set; } = null!;

        // High-Speed Filtering Cache: Component Type -> List of entities that own it
        // This stops systems from iterating through entities that lack their required components!
        private readonly Dictionary<Type, List<GameObject>> _componentTypeBuckets = new();

        public int EntityCount => _entities.Count;

        // Engine Events that systems can listen to
        public event Action<GameObject>? OnEntityCreated;
        public event Action<GameObject>? OnEntityRemoved;
        public event Action<GameObject, GameComponent>? OnComponentAdded;
        public event Action<GameObject, GameComponent>? OnComponentRemoved;



        //this adds an entity to the component bucket, as well as notifies any listeners who care about entities being created. 
        //it also subscribces to the entities on component added and removed events. so it can effectively update the component registry
        public void AddEntity(GameObject entity)
        {
            
            if(_entities.ContainsKey(entity.Id))
                return;

            _entities[entity.Id] = entity;

            entity.OnComponentAdded += HandleComponentAddedOnEntity;
            entity.OnComponentRemoved += HandleComponentRemovedFromEntity;


            if(entity.Components.ContainsKey(typeof(TransformComponent)) == false)
            {
                entity.AddComponent<TransformComponent>();
            }

            
            // Track this entity inside the optimized system lookup buckets
            foreach(var component in entity.Components)
            {
                RegisterEntityToComponentBucket(component.GetType(), entity);

            }
            OnEntityCreated?.Invoke(entity);
        }

        //adds a component to the entity. which will flag the entities component event
        public void AddComponentToEntity<T>(GameObject entity) where T : GameComponent, new()
        {
            if(_entities.ContainsKey(entity.Id))
            {
                entity.AddComponent<T>();
            }


            // 1. Tell GameObject to update its list (This handles instance creation)


        }

        // removes enetity from registry and unsubcribces from its events
        public void RemoveEntity(GameObject entity)
        {
            if(!_entities.ContainsKey(entity.Id))
                return;

            _entities.Remove(entity.Id);
            entity.OnComponentAdded -= HandleComponentAddedOnEntity;
            entity.OnComponentRemoved -= HandleComponentRemovedFromEntity;

            // Scrape the entity out of all quick-lookup cache buckets
            foreach(var bucket in _componentTypeBuckets.Values)
            {
                bucket.Remove(entity);

            }
            OnEntityRemoved?.Invoke(entity);
        }


        //removes component from entity and updates the registry. 
        public void RemoveComponentFromEntity<T>(GameObject entity) where T : GameComponent
        {
            if(_entities.ContainsKey(entity.Id))
            {
                var componentInstance = entity.GetComponent<T>();
                if(componentInstance == null)
                    return;
                entity.RemoveComponent<T>();
            }




        }

        //whenever an entity gets a component added to it, this meethod is subscribed to it's event and handles the registry update and fire for any listeners who care
        private void HandleComponentAddedOnEntity(GameObject entity, GameComponent component)
        {

            RegisterEntityToComponentBucket(component.GetType(), entity);
            OnComponentAdded?.Invoke(entity, component);
        }

        //whenever an entity has a component removed, this is subcribed to its event and handles the registry update and fires for any listeners who care
        private void HandleComponentRemovedFromEntity(GameObject entity, GameComponent component)
        {
            if(_componentTypeBuckets.TryGetValue(component.GetType(), out var bucket))
            {
                bucket.Remove(entity);
            }
            OnComponentRemoved?.Invoke(entity, component);
        }

        // finds a  entity  by its guid id. returns null if not found
        public GameObject? Find(Guid id)
        {
            return _entities.TryGetValue(id, out var entity) ? entity : null;
        }

        //finds an entity with a specific name
        public GameObject? Find(string name)
        {
            return _entities.Values.FirstOrDefault(e => e.Name == name);
        }

        //finds an entity  with a specific tag
        public GameObject? FindByTag(string tag)
        {
            return _entities.Values.FirstOrDefault(e => e.tags.Contains(tag));
        }



        // Returns ONLY the entities that actually possess the component type a system needs
        public IReadOnlyList<GameObject> GetEntitiesWithComponent(Type componentType)
        {
            if(_componentTypeBuckets.TryGetValue(componentType, out var bucket))
            {
                return bucket;
            }
            return Array.Empty<GameObject>();
        }

        public void RegisterEntityToComponentBucket(Type componentType, GameObject entity)
        {
            if(!_componentTypeBuckets.ContainsKey(componentType))
            {
                _componentTypeBuckets[componentType] = new List<GameObject>();
            }

            if(!_componentTypeBuckets[componentType].Contains(entity))
            {
                _componentTypeBuckets[componentType].Add(entity);
            }
        }

        // Exposed cleanly for serialization systems to export the raw flat array out to disk
        public List<GameObject> GetSerializableEntities()
        {
            return _entities.Values.ToList();
        }

        public IReadOnlyList<GameObject> GetQuery(IComponentQuery query)
        {
            List<GameObject> matchingEntities = new();

            // Iterate through your master O(1) dictionary values
            foreach(var entity in _entities.Values)
            {
                if(entity.isActive && query.IsMatched(entity))
                {
                    matchingEntities.Add(entity);
                }
            }

            return matchingEntities;
        }
    }
}
