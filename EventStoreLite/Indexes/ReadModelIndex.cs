using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Raven.Client.Indexes;

namespace EventStoreLite.Indexes
{
    public class ReadModelIndex : AbstractMultiMapIndexCreationTask<IReadModel>
    {
        public ReadModelIndex(Assembly assembly)
        {
            if (assembly == null) throw new ArgumentNullException("assembly");
            this.AddMapForAllSub<IReadModel>(assembly, views => from view in views select new { view.Id });
        }

        private void AddMapForAllSub<TBase>(Assembly assembly,
            Expression<Func<IEnumerable<TBase>, IEnumerable>> expr)
        {
            // Index the base class.
            AddMap(expr);

            // Index child classes.
            var types = assembly.GetTypes().ToList();
            var children = types.Where(x => typeof(TBase).IsAssignableFrom(x)).ToList();
            var addMapGeneric = GetType().GetMethod("AddMap", BindingFlags.Instance | BindingFlags.NonPublic);

            foreach (var child in children)
            {
                var genericEnumerable = typeof(IEnumerable<>).MakeGenericType(child);
                var delegateType = typeof(Func<,>).MakeGenericType(genericEnumerable, typeof(IEnumerable));
                var lambdaExpression = Expression.Lambda(delegateType, expr.Body, Expression.Parameter(genericEnumerable, expr.Parameters[0].Name));
                addMapGeneric.MakeGenericMethod(child).Invoke(this, new object[] { lambdaExpression });
            }
        }
    }
}