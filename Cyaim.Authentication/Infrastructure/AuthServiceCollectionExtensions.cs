﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Cyaim.Authentication.Infrastructure;
using Cyaim.Authentication.Infrastructure.Attributes;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Abstractions;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// 授权服务配置扩展
    /// </summary>
    public static class AuthServiceCollectionExtensions
    {
        /// <summary>
        /// 配置授权服务运行状态
        /// </summary>
        /// <param name="services"></param>
        /// <param name="setupAction"></param>
        public static void ConfigureAuth(this IServiceCollection services, Action<AuthOptions> setupAction)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (setupAction == null)
            {
                throw new ArgumentNullException(nameof(setupAction));
            }

            AuthOptions authOptions = new AuthOptions();
            setupAction(authOptions);
            services.AddSingleton(x => authOptions);

            services.TryAddSingleton<IAuthService, AuthenticationService>();

            ServiceProvider sp = services.BuildServiceProvider();
            IAuthService authService = sp.GetService<IAuthService>();

            Assembly assembly = null;
            if (string.IsNullOrEmpty(authOptions?.WatchAssemblyPath))
            {
                assembly = Assembly.GetEntryAssembly();
            }
            else
            {
                assembly = Assembly.LoadFile(authOptions.WatchAssemblyPath);
            }

            #region MyRegion

            ////加载授权节点
            //IEnumerable<AuthEndPointParm> authEndPointParms = new AuthEndPointParm[0];
            //string assemblyName = assembly.FullName.Split()[0]?.Trim(',') + ".Controllers";
            //var types = assembly.GetTypes().Where(x => !x.IsNestedPrivate && x.FullName.StartsWith(assemblyName)).ToList();
            //foreach (var item in types)
            //{
            //    var accessParm = authService.GetClassAccessParm(item);
            //    authEndPointParms = authEndPointParms.Union(accessParm);
            //    foreach (AuthEndPointParm parmItem in accessParm)
            //    {
            //        foreach (var attItem in parmItem.AuthEndPointAttributes)
            //        {
            //            if (attItem == null)
            //            {
            //                continue;
            //            }

            //            if (string.IsNullOrEmpty(attItem.AuthEndPoint))
            //            {
            //                attItem.AuthEndPoint = $"{parmItem.ControllerName}.{parmItem.ActionName}";
            //            }

            //            authService.RegisterAccessCode(attItem.AuthEndPoint, attItem.IsAllow);

            //            Console.WriteLine($"加载成功 -> {attItem.AuthEndPoint} 允许访问");
            //        }

            //    }

            //}

            //authOptions.WatchAuthEndPoint = authEndPointParms.ToArray();
            #endregion

            //加载授权节点
            IEnumerable<AuthEndPointAttribute> authEndPointParms = new AuthEndPointAttribute[0];
            string assemblyName = assembly.FullName.Split()[0]?.Trim(',') + ".Controllers";
            var types = assembly.GetTypes().Where(x => !x.IsNestedPrivate && x.FullName.StartsWith(assemblyName)).ToList();
            foreach (var item in types)
            {
                var accessParm = authService.GetClassAccessParm(item);
                authEndPointParms = authEndPointParms.Union(accessParm);
                foreach (AuthEndPointAttribute parmItem in accessParm)
                {
                    if (parmItem == null)
                    {
                        continue;
                    }

                    if (string.IsNullOrEmpty(parmItem.AuthEndPoint))
                    {
                        parmItem.AuthEndPoint = $"{authOptions.PreAccessEndPointKey}:{parmItem.ControllerName}.{parmItem.ActionName}";
                    }

                    authService.RegisterAccessCode(parmItem.AuthEndPoint, parmItem.IsAllow);

                    Console.WriteLine($"加载成功 -> {parmItem.AuthEndPoint} 允许访问");

                }

            }

            authOptions.WatchAuthEndPoint = authEndPointParms.ToArray();


            //获取处理后的缓存并覆盖
            var memoryCache = sp.GetService<IMemoryCache>();

            var existService = services.FirstOrDefault(x => x.ServiceType == typeof(IMemoryCache));
            services.Remove(existService);

            services.TryAddSingleton<IMemoryCache>(x => memoryCache);
        }


    }
}
