using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace EntityFrameworkCore.SqlChangeTracking.Expressions
{
    public class ChangeTableQueryable<T> : IQueryable<T>
    {
        public ChangeTableQueryable()
        {
            Provider = new ChangeTableQueryProvider();
            Expression = Expression.Constant(this);
        }

        public ChangeTableQueryable(ChangeTableQueryProvider provider, Expression expression)
        {
            Provider = provider;
            Expression = expression;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return Provider.Execute<IEnumerable<T>>(Expression).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Provider.Execute<IEnumerable>(Expression).GetEnumerator();
        }

        public Type ElementType => typeof(T);
        public Expression Expression { get; set; }
        public IQueryProvider Provider { get; set; }
    }

    public class ChangeTableQueryProvider : IQueryProvider
    {
        public IQueryable CreateQuery(Expression expression)
        {
            Type elementType = expression.Type;

            try
            {
                return (IQueryable)Activator.CreateInstance(typeof(ChangeTableQueryable<>).MakeGenericType(elementType), new object[] { this, expression });
            }
            catch (System.Reflection.TargetInvocationException tie)
            {
                throw tie.InnerException;
            }
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            return new ChangeTableQueryable<TElement>(this, expression);
        }

        public object Execute(Expression expression)
        {
            return ChangeTableQueryContext.Execute(expression, false);
        }

        public TResult Execute<TResult>(Expression expression)
        {
            bool IsEnumerable = (typeof(TResult).Name == "IEnumerable`1");

            return (TResult)ChangeTableQueryContext.Execute(expression, IsEnumerable);
        }
    }

    public class ChangeTableQueryContext
    {
        public static object Execute(Expression expression, bool trust)
        {
            return new object();
        }
    }

}
