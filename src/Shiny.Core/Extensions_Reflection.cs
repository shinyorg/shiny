﻿using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;


namespace Shiny
{
    public static class ReflectionExtensions
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsNullable(this Type type)
            => Nullable.GetUnderlyingType(type) != null;


        /// <summary>
        /// This will do a 1-1 shallow reflection copy - types & names must match or it will crash
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        public static void ReflectCopyTo(this object source, object target)
        {
            var srcProps = source.GetType().GetRuntimeProperties().Where(x => x.CanRead);
            var targetProps = target.GetType().GetRuntimeProperties().Where(x => x.CanWrite);
            foreach (var prop in srcProps)
            {
                var targetProp = targetProps.FirstOrDefault(x => x.Name.Equals(prop.Name));
                if (targetProp != null)
                {
                    var value = prop.GetValue(source, null);
                    targetProp.SetValue(target, value);
                }
            }
        }


        /// <summary>
        /// Reflects out property information based on the expression value
        /// </summary>
        /// <typeparam name="TSender"></typeparam>
        /// <typeparam name="TRet"></typeparam>
        /// <param name="sender"></param>
        /// <param name="expression"></param>
        /// <returns></returns>
        public static PropertyInfo GetPropertyInfo<TSender, TRet>(this TSender sender, Expression<Func<TSender, TRet>> expression)
        {
            if (sender == null)
                throw new ArgumentException("Sender is null");

            var member = (expression as LambdaExpression)?.Body as MemberExpression;
            if (member == null)
                throw new ArgumentException("Invalid lamba expression - body is not a member expression");

            var property = sender.GetType().GetRuntimeProperty(member.Member.Name);
            return property;
        }


        /// <summary>
        /// Unwraps nullable types if necessary
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static Type Unwrap(this Type type)
        {
            if (type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                type = Nullable.GetUnderlyingType(type);

            return type;
        }


        /// <summary>
        /// Gets an objects property dynamically through reflection - will throw an exception if proper has no getter or property does not exists
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="propertyName"></param>
        /// <param name="value"></param>
        public static object ReflectGet(this object obj, string propertyName)
        {
            var prop = obj.GetType().GetProperty(propertyName);
            if (prop == null)
                throw new ArgumentException($"Property {propertyName} does not exist on type {obj.GetType().FullName}");

            if (!prop.CanRead)
                throw new ArgumentException($"Property {propertyName} does not have a getter on type {obj.GetType().FullName}");

            return prop.GetValue(obj);
        }


        /// <summary>
        /// Gets an objects property dynamically through reflection - will throw an exception if proper has no getter or property does not exists
        /// </summary>
        /// <typeparam name="TSender"></typeparam>
        /// <typeparam name="TReturn"></typeparam>
        /// <param name="obj"></param>
        /// <param name="expression"></param>
        /// <returns></returns>
        public static TReturn ReflectGet<TSender, TReturn>(this TSender obj, Expression<Func<TSender, TReturn>> expression)
        {
            var prop = obj.GetPropertyInfo(expression);
            return (TReturn)prop.GetValue(obj);
        }


        /// <summary>
        /// Sets an objects property dynamically through reflection - will throw an exception if proper has no setter, wrong type, or property does not exists
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="propertyName"></param>
        /// <param name="value"></param>
        public static void ReflectSet(this object obj, string propertyName, object value)
        {
            var prop = obj.GetType().GetProperty(propertyName);
            if (prop == null)
                throw new ArgumentException($"Property {propertyName} does not exist on type {obj.GetType().FullName}");

            if (!prop.CanWrite)
                throw new ArgumentException($"Property {propertyName} does not have a setter on type {obj.GetType().FullName}");

            prop.SetValue(obj, value);
        }


        /// <summary>
        /// Creates and copies values from one object to a newly created object
        /// </summary>
        /// <typeparam name="TTo"></typeparam>
        /// <typeparam name="TFrom"></typeparam>
        /// <param name="from"></param>
        /// <returns></returns>
        public static TTo ReflectTransform<TTo, TFrom>(TFrom from) where TTo : new()
        {
            var to = new TTo();
            from.ReflectCopyTo(to);
            return to;
        }
    }
}
