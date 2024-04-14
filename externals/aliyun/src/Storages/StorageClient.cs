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
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Zongsoft.Externals.Aliyun.Storages
{
	public class StorageClient
	{
		#region 成员字段
		private HttpClient _http;
		private ICertificate _certificate;
		private StorageServiceCenter _serviceCenter;
		#endregion

		#region 构造函数
		internal StorageClient(StorageServiceCenter serviceCenter, ICertificate certificate)
		{
			_serviceCenter = serviceCenter ?? throw new ArgumentNullException(nameof(serviceCenter));
			_certificate = certificate ?? throw new ArgumentNullException(nameof(certificate));
			_http = new HttpClient(new HttpClientHandler(certificate, StorageAuthenticator.Instance));
		}
		#endregion

		#region 公共属性
		public ICertificate Certificate => _certificate;
		public StorageServiceCenter ServiceCenter => _serviceCenter;
		#endregion

		#region 内部属性
		internal HttpClient HttpClient => _http;
		#endregion

		#region 公共方法
		public async Task<bool> CopyAsync(string source, string destination)
		{
			var request = new HttpRequestMessage(HttpMethod.Put, _serviceCenter.GetRequestUrl(destination));

			request.Headers.Add(StorageHeaders.OSS_COPY_SOURCE, source);

			var result = await _http.SendAsync(request);
			return result.IsSuccessStatusCode;
		}

		public async Task<bool> DeleteAsync(string path) => (await _http.DeleteAsync(_serviceCenter.GetRequestUrl(path))).IsSuccessStatusCode;

		public async Task<Stream> DownloadAsync(string path, IDictionary<string, object> properties = null)
		{
			var response = await _http.GetAsync(_serviceCenter.GetRequestUrl(path));

			//确认返回是否是成功
			response.EnsureSuccessStatusCode();

			if(properties != null)
				this.FillProperties(response, properties);

			return await response.Content.ReadAsStreamAsync();
		}

		public Task<bool> CreateAsync(string path, IDictionary<string, object> extendedProperties = null) => this.CreateAsync(path, null, extendedProperties);
		public async Task<bool> CreateAsync(string path, Stream stream, IDictionary<string, object> extendedProperties = null)
		{
			var request = this.CreateHttpRequest(HttpMethod.Put, path, this.EnsureCreation(extendedProperties));

			if(stream != null)
				request.Content = new StreamContent(stream);

			return (await _http.SendAsync(request)).IsSuccessStatusCode;
		}

		public async Task<bool> ExistsAsync(string path)
		{
			var request = new HttpRequestMessage(HttpMethod.Head, _serviceCenter.GetRequestUrl(path));

			var result = await _http.SendAsync(request);
			return result.IsSuccessStatusCode;
		}

		public async Task<IDictionary<string, object>> GetExtendedPropertiesAsync(string path)
		{
			var request = new HttpRequestMessage(HttpMethod.Head, _serviceCenter.GetRequestUrl(path));
			var response = await _http.SendAsync(request);

			//确认返回是否是成功
			response.EnsureSuccessStatusCode();

			var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

			this.FillProperties(response, result);

			return result;
		}

		public async Task<bool> SetExtendedPropertiesAsync(string path, IDictionary<string, object> extendedProperties)
		{
			if(extendedProperties == null || extendedProperties.Count < 1)
				return false;

			var properties = await this.GetExtendedPropertiesAsync(path);

			foreach(var property in extendedProperties)
			{
				properties[property.Key] = property.Value;
			}

			var request = this.CreateHttpRequest(HttpMethod.Put, path, properties);
			request.Headers.Add(StorageHeaders.OSS_COPY_SOURCE, path);

			return (await _http.SendAsync(request)).IsSuccessStatusCode;
		}

		public async Task<StorageSearchResult> SearchAsync(string path, Func<string, string> getUrl)
		{
			if(!string.IsNullOrWhiteSpace(path))
			{
				if(path.Contains('?'))
					throw new ArgumentException(string.Format("The '{0}' pattern contains invalid characters.", path));

				var index = path.IndexOf('*');

				if(index >= 0 && index < path.Length - 1)
					throw new ArgumentException(string.Format("The '*' character at last only in the '{0}' pattern.", path));

				path = path.Trim('*');
			}

			string baseName, resourcePath;
			var url = _serviceCenter.GetBaseUrl(path, out baseName, out resourcePath) + "?list-type=2&prefix=" + Uri.EscapeDataString(resourcePath) + "&delimiter=/&max-keys=100";
			var response = await _http.GetAsync(url);

			//确保返回的内容是成功
			response.EnsureSuccessStatusCode();

			var resolver = new StorageSearchResultResolver(_serviceCenter, _http, getUrl);
			return await resolver.Resolve(response);
		}
		#endregion

		#region 内部方法
		internal StorageUploader GetUploader(string path, int bufferSize = 0) => new StorageUploader(this, path, bufferSize);
		internal StorageUploader GetUploader(string path, IDictionary<string, object> extendedProperties, int bufferSize = 0) => new StorageUploader(this, path, extendedProperties, bufferSize);

		internal HttpRequestMessage CreateHttpRequest(HttpMethod method, string path, IDictionary<string, object> extendedProperties = null)
		{
			var request = new HttpRequestMessage(method, _serviceCenter.GetRequestUrl(path));

			if(extendedProperties != null && extendedProperties.Count > 0)
			{
				foreach(var entry in extendedProperties)
				{
					if(entry.Value == null || string.IsNullOrEmpty(entry.Key) || entry.Key.Contains(':'))
						continue;

					request.Headers.Add(StorageHeaders.OSS_META + entry.Key.ToLowerInvariant(), Uri.EscapeDataString(entry.Value.ToString()));
				}
			}

			return request;
		}

		internal IDictionary<string, object> EnsureCreation(IDictionary<string, object> properties)
		{
			if(properties == null)
			{
				properties = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
				{
					[StorageHeaders.ZFS_CREATION_PROPERTY] = Utility.GetGmtTime()
				};
			}
			else
			{
				if(!properties.ContainsKey(StorageHeaders.ZFS_CREATION_PROPERTY))
					properties[StorageHeaders.ZFS_CREATION_PROPERTY] = Utility.GetGmtTime();
			}

			return properties;
		}
		#endregion

		#region 私有方法
		private void FillProperties(HttpResponseMessage response, IDictionary<string, object> properties)
		{
			if(response == null || !response.IsSuccessStatusCode || properties == null)
				return;

			if(response.Content != null && response.Content.Headers.LastModified.HasValue)
				properties[StorageHeaders.HTTP_LAST_MODIFIED_PROPERTY] = response.Content.Headers.LastModified.Value.ToString();

			if(response.Content != null && response.Content.Headers.ContentLength.HasValue)
				properties[StorageHeaders.HTTP_CONTENT_LENGTH_PROPERTY] = response.Content.Headers.ContentLength.Value.ToString();

			if(response.Headers.ETag != null && !string.IsNullOrWhiteSpace(response.Headers.ETag.Tag))
				properties[StorageHeaders.HTTP_ETAG_PROPERTY] = response.Headers.ETag.Tag.Trim('"');

			foreach(var header in response.Headers)
			{
				if(header.Key.Length > StorageHeaders.OSS_META.Length && header.Key.StartsWith(StorageHeaders.OSS_META))
				{
					var key = header.Key.Substring(StorageHeaders.OSS_META.Length);

					if(key.Length > 0)
						properties[key] = Uri.UnescapeDataString(string.Join("", header.Value));
				}
			}
		}
		#endregion
	}
}
