using System;
using System.Linq;
using System.Linq.Expressions;
using Damasio34.Seedwork.Aggregates;

namespace Damasio34.GraphDiff
{

    internal class UpdateGraphConfigurationBuilder<T> : ExpressionVisitor
    {
        internal Expression<Func<IUpdateConfiguration<T>, object>> ConvertFrom(
            Expression<Func<IAggregateConfiguration<T>, object>> aggregateConfiguration)
        {
            var novaExp = Visit(aggregateConfiguration) as Expression<Func<IUpdateConfiguration<T>, object>>;
            return novaExp;
        }

        /// <summary>
        ///     Centraliza as conversões de tipo.
        ///     E.g.: Conversão de <c>IAggregateConfiguration</c> p/ <c>IUpdateConfiguration</c>.
        /// </summary>
        private Type VisitarTipo(Type type)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof (IAggregateConfiguration<>))
                return typeof (IUpdateConfiguration<>).MakeGenericType(type.GetGenericArguments());
            
            return type;
        }


        #region ' ExpressionVisitor overrides '

        protected override Expression VisitParameter(ParameterExpression node)
        {
            var novoTipo = VisitarTipo(node.Type);
            return novoTipo != node.Type ? Expression.Parameter(novoTipo, node.Name) : node;
        }

        protected override Expression VisitLambda<T1>(Expression<T1> node)
        {
            var novosParametros = node.Parameters.Select(p => VisitParameter(p) as ParameterExpression).ToList();

            var delegateParams = node.Type.GetGenericArguments().Select(VisitarTipo).ToArray();                 // array dos tipos @ Func<,,>
            Type delegateType = node.Type.GetGenericTypeDefinition().MakeGenericType(delegateParams.ToArray());

            var body = Visit(node.Body);

            return Expression.Lambda(delegateType, body, node.Name, node.TailCall, novosParametros);
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            switch (node.Method.Name)
            {
                case "HasMany":
                    return OwnedCollectionMethodCall(node);

                case "HasRequired":
                    return OwnedEntityMethodCall(node);
                
                default:
                    return node;
            }
        }

        #endregion

        /// <summary>
        ///     Cria uma chamada p/ 'UpdateConfigurationExtensions.OwnedCollection(agg, cfg)' a partir de um <c>IAggregateConfiguration</c>.
        /// </summary>
        private MethodCallExpression OwnedCollectionMethodCall(MethodCallExpression node)
        {
            if (node.Method.DeclaringType == null) throw new InvalidOperationException("DeclaringType of MemberInfo is null.");

            var tAggregador = node.Method.DeclaringType.GetGenericArguments().Single();
            var tProperty = node.Method.GetGenericArguments().Single();

            var novoObjeto = Visit(node.Object);
            var novosArgumentos = node.Arguments.Select(Visit).ToArray();

            // tipos
            var methodDefinition = typeof(UpdateConfigurationExtensions).GetMethods().Single(m => m.Name == "OwnedCollection" && m.GetParameters().Count() == 1 + node.Arguments.Count);
            var metodo = methodDefinition.MakeGenericMethod(new Type[] { tAggregador, tProperty });

            var arguments = novosArgumentos.ToList();
            arguments.Insert(0, novoObjeto);

            return Expression.Call(metodo, arguments);
        }

        /// <summary>
        ///     Cria uma chamada p/ 'UpdateConfigurationExtensions.OwnedCollection(agg, cfg)' a partir de um <c>IAggregateConfiguration</c>.
        /// </summary>
        private MethodCallExpression OwnedEntityMethodCall(MethodCallExpression node)
        {
            if (node.Method.DeclaringType == null) throw new InvalidOperationException("DeclaringType of MemberInfo is null.");

            var tAggregador = node.Method.DeclaringType.GetGenericArguments().Single();
            var tProperty = node.Method.GetGenericArguments().Single();

            var novoObjeto = Visit(node.Object);
            var novosArgumentos = node.Arguments.Select(Visit).ToArray();

            // tipos
            var methodDefinition = typeof(UpdateConfigurationExtensions).GetMethods().Single(m => m.Name == "OwnedEntity" && m.GetParameters().Count() == 1 + node.Arguments.Count);
            var metodo = methodDefinition.MakeGenericMethod(new Type[] { tAggregador, tProperty });

            var arguments = novosArgumentos.ToList();
            arguments.Insert(0, novoObjeto);

            return Expression.Call(metodo, arguments);
        }

    }
}
