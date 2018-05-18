﻿#if AOT
using System;
using System.Reflection;
using System.Linq;

namespace WebApiClient
{
    /// <summary>
    /// 提供HttpApiClient代理搜索和实例化
    /// </summary>
    static class HttpApiClientProxy
    {
        /// <summary>
        /// 代理类型的构造器的参数类型
        /// </summary>
        private static readonly Type[] proxyTypeCtorArgTypes = new Type[] { typeof(IApiInterceptor), typeof(MethodInfo[]) };

        /// <summary>
        /// 接口对应的代理类型的构造器缓存
        /// </summary>
        private static readonly ConcurrentCache<Type, ConstructorInfo> proxyTypeCtorCache = new ConcurrentCache<Type, ConstructorInfo>();

        /// <summary>
        /// 搜索接口的代理类型并实例化
        /// </summary>
        /// <param name="interfaceType">接口类型</param>
        /// <param name="interceptor">拦截器</param>
        /// <exception cref="TypeLoadException"></exception>
        /// <returns></returns>
        public static object CreateInstance(Type interfaceType, IApiInterceptor interceptor)
        {
            var proxyTypeCtor = proxyTypeCtorCache.GetOrAdd(interfaceType, type =>
            {
                var proxyType = FindProxyType(type);
                return proxyType?.GetConstructor(proxyTypeCtorArgTypes);
            });

            if (proxyTypeCtor == null)
            {
                throw new TypeLoadException($"找不到接口{interfaceType}的代理类，请为接口所在项目重新使用Nuget安装WebApiClient.AOT");
            }

            var apiMethods = interfaceType.GetAllApiMethods();
            return proxyTypeCtor.Invoke(new object[] { interceptor, apiMethods });
        }


        /// <summary>
        /// 查找代理类型
        /// </summary>
        /// <param name="interfaceType">接口类型</param>
        /// <returns></returns>
        private static Type FindProxyType(this Type interfaceType)
        {
            const string prefix = "$";
            var allTypes = interfaceType.GetTypeInfo().Assembly.GetTypes();

            if (interfaceType.GetTypeInfo().IsGenericType == false)
            {
                var declaringType = interfaceType.DeclaringType;
                var proxyTypeName = $"{prefix}{interfaceType.Name}";
                return allTypes.FirstOrDefault(item => item.Name == proxyTypeName && item.DeclaringType == declaringType);
            }
            else
            {
                var definition = interfaceType.GetGenericTypeDefinition();
                var declaringType = definition.DeclaringType;
                var proxyTypeName = $"{prefix}{definition.Name}";

                var targetType = allTypes.FirstOrDefault(item => item.Name == proxyTypeName && item.DeclaringType == declaringType);
                var proxyType = targetType?.MakeGenericType(interfaceType.GenericTypeArguments);
                return proxyType;
            }
        }
    }
}
#endif