﻿/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
 *
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Core library.
 *
 * The Zongsoft.Core is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Core is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Core library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.IO;
using System.Threading;
using System.Security.Claims;
using System.Collections.Generic;

namespace Zongsoft.Services
{
	[System.Reflection.DefaultMember(nameof(Modules))]
	public class ApplicationContext : IApplicationContext, IApplicationModule
	{
		#region 单例字段
		private volatile static IApplicationContext _current;
		#endregion

		#region 事件声明
		public event EventHandler Exiting;
		public event EventHandler Starting;
		public event EventHandler Started;
		#endregion

		#region 成员字段
		private string _name;
		#endregion

		#region 构造函数
		protected ApplicationContext() : this(string.Empty)
		{
		}

		protected ApplicationContext(string name)
		{
			if(string.IsNullOrWhiteSpace(name))
				_name = System.Reflection.Assembly.GetEntryAssembly().GetName().Name;
			else
				_name = this.Title = name.Trim();

			this.Modules = new Collections.NamedCollection<IApplicationModule>(p => p.Name);
			this.Initializers = new List<IApplicationInitializer>();
			this.Schemas = new ComponentModel.SchemaCollection();
			this.Properties = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
		}
		#endregion

		#region 单例属性
		/// <summary>
		/// 获取当前应用程序的<see cref="IApplicationContext"/>上下文。
		/// </summary>
		public static IApplicationContext Current
		{
			get => _current;
			protected set => _current = value ?? throw new ArgumentNullException();
		}
		#endregion

		#region 公共属性
		public virtual string Name
		{
			get
			{
				return string.IsNullOrEmpty(_name) ? this.Configuration?.GetSection("ApplicationName").Value : null;
			}
			protected set
			{
				if(string.IsNullOrWhiteSpace(value))
					throw new ArgumentNullException();

				_name = value.Trim();
			}
		}

		public string Title { get; set; }

		public string Description { get; set; }

		public virtual string ApplicationDirectory
		{
			get => AppContext.BaseDirectory;
		}

		public virtual Microsoft.Extensions.Configuration.IConfigurationRoot Configuration
		{
			get => null;
		}

		public virtual IApplicationEnvironment Environment
		{
			get => null;
		}

		public virtual IServiceProvider Services
		{
			get => ServiceProviderFactory.Instance.Default;
		}

		public virtual ClaimsPrincipal Principal
		{
			get
			{
				if(Thread.CurrentPrincipal is ClaimsPrincipal principal)
					return principal;
				else
					return new ClaimsPrincipal(Thread.CurrentPrincipal);
			}
		}

		public Collections.INamedCollection<IApplicationModule> Modules { get; }

		public ICollection<IApplicationInitializer> Initializers { get; }

		public Collections.INamedCollection<ComponentModel.Schema> Schemas { get; }

		public IDictionary<string, object> Properties { get; }
		#endregion

		#region 公共方法
		public string EnsureDirectory(string relativePath)
		{
			string fullPath = this.ApplicationDirectory;

			if(string.IsNullOrWhiteSpace(relativePath))
				return fullPath;

			var parts = relativePath.Split('/', '\\', Path.DirectorySeparatorChar);

			foreach(var part in parts)
			{
				if(string.IsNullOrWhiteSpace(part))
					continue;

				fullPath = Path.Combine(fullPath, part);

				if(!Directory.Exists(fullPath))
					Directory.CreateDirectory(fullPath);
			}

			return fullPath;
		}
		#endregion

		#region 激发事件
		protected virtual void OnExiting(EventArgs args)
		{
			this.Exiting?.Invoke(this, args);
		}

		protected virtual void OnStarting(EventArgs args)
		{
			this.Starting?.Invoke(this, args);
		}

		protected virtual void OnStarted(EventArgs args)
		{
			this.Started?.Invoke(this, args);
		}
		#endregion

		#region 重写方法
		public override string ToString()
		{
			if(string.IsNullOrEmpty(this.Title) || string.Equals(this.Name, this.Title))
				return this.Name;
			else
				return string.Format("[{0}] {1}", this.Name, this.Title);
		}
		#endregion
	}
}
