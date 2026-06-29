using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Core.ECS
{
    public static class Query
    {
        // The starting gateway overloads
        public static IComponentQuery Has<T>() where T : GameComponent => new HasComponentQuery(typeof(T));
        public static IComponentQuery Not<T>() where T : GameComponent => new NotQuery(new HasComponentQuery(typeof(T)));

        // Accepts a flat comma-separated list of your custom fluent chains!
        public static IComponentQuery Or(params IComponentQuery[] queries) => new OrQuery(queries);
        public static IComponentQuery And(params IComponentQuery[] queries) => new AndQuery(queries);
    }

    public static class ComponentQueryExtensions
    {
        // Chains a flat component requirement: .And<MovementComponent>()
        public static IComponentQuery And<T>(this IComponentQuery left) where T : GameComponent
        {
            return new AndQuery(left, new HasComponentQuery(typeof(T)));
        }

        // Chains a nested query block: .And( Query.Has<A>().Or<B>() )
        public static IComponentQuery And(this IComponentQuery left, IComponentQuery right)
        {
            return new AndQuery(left, right);
        }

        // Chains an inverse component requirement: .Not<InvulnerableComponent>()
        public static IComponentQuery Not<T>(this IComponentQuery left) where T : GameComponent
        {
            return new AndQuery(left, new NotQuery(new HasComponentQuery(typeof(T))));
        }

        public static IComponentQuery Or<T>(this IComponentQuery left) where T : GameComponent
        {
            return new OrQuery(left, new HasComponentQuery(typeof(T)));
        }
    }

    public class HasComponentQuery : IComponentQuery
    {
        private readonly Type _type;
        public HasComponentQuery(Type type) => _type = type;
        public bool IsMatched(GameObject entity) => entity.Components.Any(c => _type.IsAssignableFrom(c.GetType()));
    }

    public class AndQuery : IComponentQuery
    {
        private readonly IComponentQuery[] _queries;

        // Constructor 1: Handles the flat array version -> new AndQuery(queries)
        public AndQuery(params IComponentQuery[] queries)
        {
            _queries = queries;
        }

        // Constructor 2: Handles the fluent chaining version -> new AndQuery(left, right)
        public AndQuery(IComponentQuery left, IComponentQuery right)
        {
            _queries = new[] { left, right };
        }

        public bool IsMatched(GameObject entity)
        {
            // Must match every single query in the array
            for(int i = 0; i < _queries.Length; i++)
            {
                if(!_queries[i].IsMatched(entity))
                    return false;
            }
            return true;
        }
    }

    public class OrQuery : IComponentQuery
    {
        private readonly IComponentQuery[] _queries;

        // Constructor 1: Handles the flat array version -> new OrQuery(queries)
        public OrQuery(params IComponentQuery[] queries)
        {
            _queries = queries;
        }

        // Constructor 2: Handles the fluent chaining version -> new OrQuery(left, right)
        public OrQuery(IComponentQuery left, IComponentQuery right)
        {
            _queries = new[] { left, right };
        }

        public bool IsMatched(GameObject entity)
        {
            // Returns true as soon as at least one query matches
            for(int i = 0; i < _queries.Length; i++)
            {
                if(_queries[i].IsMatched(entity))
                    return true;
            }
            return false;
        }
    }

    public class NotQuery : IComponentQuery
    {
        private readonly IComponentQuery _query;
        public NotQuery(IComponentQuery query) => _query = query;
        public bool IsMatched(GameObject entity) => !_query.IsMatched(entity);
    }
}
