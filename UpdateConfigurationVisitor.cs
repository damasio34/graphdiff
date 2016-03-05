/*
 * This code is provided as is with no warranty. If you find a bug please report it on github.
 * If you would like to use the code please leave this comment at the top of the page
 * (c) Brent McKendrick 2012
 */

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

// This should be internal only
namespace Damasio34.GraphDiff
{
    /// <summary>
    /// Used internally to represent the update graph
    /// </summary>
    internal abstract class UpdateMember
    {
        protected UpdateMember()
        {
            Members = new Stack<UpdateMember>();
        }
        public UpdateMember Parent { get; set; }
        public PropertyInfo Accessor { get; set; }
        public Stack<UpdateMember> Members { get; set; }
        public string IncludeString { get; set; }
        public bool IsCollection { get; set; }
        public bool IsOwned { get; set; }

        public bool HasMembers()
        {
            return Members.Count > 0;
        }

        internal abstract void SetValue(object obj, object value);

    }

    internal class UpdateMember<T> : UpdateMember
        where T : class
    {

        /// <summary>
        ///     Seta 'value' ao membro representada por esta classe no objeto 'obj'.
        ///     A necessidade deste método é para permitir a realização do Cast (assim
        ///     era necessário a identificação de 'T'.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="value"></param>
        internal override void SetValue(object obj, object value)
        {
            // Obs.: foi necessário realizar o Cast p/ dynamic 
            // para permitir que os operadores de cast ('explicit operator')
            // fossem avalidados em runtime. De outra forma, o código
            // levantava um InvalidCastException.
            var valueCasted = (T)(dynamic) value;

            Accessor.SetValue(obj, valueCasted, null);
        }
    }

    /// <summary>
    /// Used as a translator from the expression tree to the UpdateMember tree
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class UpdateConfigurationVisitor<T> : ExpressionVisitor
    {
        UpdateMember currentMember;
        UpdateMember previousMember = null;
        string currentMethod = "";

        /// <summary>
        /// Translates the Expression tree to a tree of UpdateMembers
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <returns></returns>
        public UpdateMember GetUpdateMembers<TProp>(Expression<Func<IUpdateConfiguration<T>, TProp>> expression)
            where TProp : class
        {
            var initialNode = new UpdateMember<TProp>();
            currentMember = initialNode;
            Visit(expression);
            return initialNode;
        }

        protected override Expression VisitMember(MemberExpression expression)
        {
            
            // Create new node for this item
            var newMember = (UpdateMember) Activator.CreateInstance(typeof (UpdateMember<>).MakeGenericType(expression.Type));
            newMember.Parent = currentMember;
            newMember.Accessor = (PropertyInfo) expression.Member;
            
            currentMember.Members.Push(newMember);
            previousMember = currentMember;
            currentMember = newMember;

            currentMember.IncludeString = previousMember.IncludeString != null
                ? previousMember.IncludeString + "." + currentMember.Accessor.Name
                : currentMember.Accessor.Name;

            // Chose if entity update or reference update and create expression
            switch (currentMethod)
            {
                case "OwnedEntity":
                    currentMember.IsOwned = true;
                    break;
                case "AssociatedEntity":
                    currentMember.IsOwned = false;
                    break;
                case "OwnedCollection":
                    currentMember.IsOwned = true;
                    currentMember.IsCollection = true;
                    break;
                case "AssociatedCollection":
                    currentMember.IsOwned = false;
                    currentMember.IsCollection = true;
                    break;
                default:
                    throw new NotSupportedException("The method used in the update mapping is not supported");
            }
            return base.VisitMember(expression);
        }

        protected override Expression VisitMethodCall(MethodCallExpression expression)
        {
            currentMethod = expression.Method.Name;

            // go left to right in the subtree (ignore first argument for now)
            for (int i = 1; i < expression.Arguments.Count; i++)
                Visit(expression.Arguments[i]);

            // go back up the tree and continue
            currentMember = currentMember.Parent;
            return Visit(expression.Arguments[0]);
        }
    }

    internal static class EFIncludeHelper
    {
        public static List<string> GetIncludeStrings(UpdateMember root)
        {
            var list = new List<string>();

            if (root.Members.Count == 0)
            {
                list.Add(root.IncludeString);
            }

            foreach (var member in root.Members)
            {
                list.AddRange(GetIncludeStrings(member));
            }
            return list;
        }
    }
}
