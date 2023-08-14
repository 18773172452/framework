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
 * This file is part of Zongsoft.Externals.Aliyun library.
 *
 * The Zongsoft.Externals.Aliyun is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.Aliyun is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.Aliyun library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;

namespace Zongsoft.Externals.Aliyun.Storages
{
	/// <summary>
	/// 表示存储服务中心的类。
	/// </summary>
	public class StorageServiceCenter : ServiceCenterBase
	{
		#region 常量定义
		//中国存储服务中心访问地址的前缀常量
		private const string OSS_CN_PREFIX = "oss-cn-";

		//美国存储服务中心访问地址的前缀常量
		private const string OSS_US_PREFIX = "oss-us-";
		#endregion

		#region 构造函数
		private StorageServiceCenter(ServiceCenterName name, bool isInternal) : base(name, isInternal)
		{
			this.Path = OSS_CN_PREFIX + base.Path;
		}
		#endregion

		#region 公共方法
		public string GetRequestUrl(string path, bool secured = false) => this.GetRequestUrl(path, secured, out _);
		public string GetRequestUrl(string path, bool secured, out string resourcePath)
		{
			ResolvePath(path, out var bucketName, out resourcePath);

			return secured ?
				string.Format("https://{0}.{1}/{2}", bucketName, this.Path, Uri.EscapeDataString(resourcePath).Replace("%2F", "/")) :
				string.Format("http://{0}.{1}/{2}", bucketName, this.Path, Uri.EscapeDataString(resourcePath).Replace("%2F", "/"));
		}
		#endregion

		#region 内部方法
		internal string GetBaseUrl(string path) => this.GetBaseUrl(path, out _, out _);
		internal string GetBaseUrl(string path, out string baseName, out string resourcePath)
		{
			ResolvePath(path, out baseName, out resourcePath);
			return string.Format("http://{0}.{1}", baseName.ToLowerInvariant(), this.Path);
		}
		#endregion

		#region 静态方法
		public static StorageServiceCenter GetInstance(ServiceCenterName name, bool isInternal = true) => name switch
		{
			ServiceCenterName.Beijing => isInternal ? Internal.Beijing : External.Beijing,
			ServiceCenterName.Qingdao => isInternal ? Internal.Qingdao : External.Qingdao,
			ServiceCenterName.Hangzhou => isInternal ? Internal.Hangzhou : External.Hangzhou,
			ServiceCenterName.Shenzhen => isInternal ? Internal.Shenzhen : External.Shenzhen,
			ServiceCenterName.Hongkong => isInternal ? Internal.Hongkong : External.Hongkong,
			_ => throw new NotSupportedException(),
		};
		#endregion

		#region 私有方法
		private static void ResolvePath(string path, out string bucketName, out string resourcePath)
		{
			if(string.IsNullOrWhiteSpace(path))
				throw new ArgumentNullException(nameof(path));

			path = path.Trim();
			var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);

			bucketName = parts[0];

			if(parts.Length > 1)
				resourcePath = string.Join('/', parts, 1, parts.Length - 1) + (path.EndsWith('/') ? '/' : string.Empty);
			else
				resourcePath = string.Empty;
		}
		#endregion

		#region 嵌套子类
		public static class External
		{
			/// <summary>北京存储服务中心的外部访问地址</summary>
			public static readonly StorageServiceCenter Beijing = new(ServiceCenterName.Beijing, false);

			/// <summary>青岛存储服务中心的外部访问地址</summary>
			public static readonly StorageServiceCenter Qingdao = new(ServiceCenterName.Qingdao, false);

			/// <summary>杭州存储服务中心的外部访问地址</summary>
			public static readonly StorageServiceCenter Hangzhou = new(ServiceCenterName.Hangzhou, false);

			/// <summary>深圳存储服务中心的外部访问地址</summary>
			public static readonly StorageServiceCenter Shenzhen = new(ServiceCenterName.Shenzhen, false);

			/// <summary>香港存储服务中心的外部访问地址</summary>
			public static readonly StorageServiceCenter Hongkong = new(ServiceCenterName.Hongkong, false);
		}

		public static class Internal
		{
			/// <summary>北京存储服务中心的内部访问地址</summary>
			public static readonly StorageServiceCenter Beijing = new(ServiceCenterName.Beijing, true);

			/// <summary>青岛存储服务中心的内部访问地址</summary>
			public static readonly StorageServiceCenter Qingdao = new(ServiceCenterName.Qingdao, true);

			/// <summary>杭州存储服务中心的内部访问地址</summary>
			public static readonly StorageServiceCenter Hangzhou = new(ServiceCenterName.Hangzhou, true);

			/// <summary>深圳存储服务中心的内部访问地址</summary>
			public static readonly StorageServiceCenter Shenzhen = new(ServiceCenterName.Shenzhen, true);

			/// <summary>香港存储服务中心的内部访问地址</summary>
			public static readonly StorageServiceCenter Hongkong = new(ServiceCenterName.Hongkong, true);
		}
		#endregion
	}
}