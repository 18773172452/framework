﻿/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@qq.com>
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
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Zongsoft.Data
{
	/// <summary>
	/// 表示数据访问的抽象基类。
	/// </summary>
	[System.Reflection.DefaultMember(nameof(Filters))]
	public abstract class DataAccessBase : IDataAccess
	{
		#region 事件定义
		public event EventHandler<DataAccessErrorEventArgs> Error;
		public event EventHandler<DataExecutedEventArgs> Executed;
		public event EventHandler<DataExecutingEventArgs> Executing;
		public event EventHandler<DataExistedEventArgs> Existed;
		public event EventHandler<DataExistingEventArgs> Existing;
		public event EventHandler<DataAggregatedEventArgs> Aggregated;
		public event EventHandler<DataAggregatingEventArgs> Aggregating;
		public event EventHandler<DataIncrementedEventArgs> Incremented;
		public event EventHandler<DataIncrementingEventArgs> Incrementing;
		public event EventHandler<DataImportedEventArgs> Imported;
		public event EventHandler<DataImportingEventArgs> Importing;
		public event EventHandler<DataDeletedEventArgs> Deleted;
		public event EventHandler<DataDeletingEventArgs> Deleting;
		public event EventHandler<DataInsertedEventArgs> Inserted;
		public event EventHandler<DataInsertingEventArgs> Inserting;
		public event EventHandler<DataUpsertedEventArgs> Upserted;
		public event EventHandler<DataUpsertingEventArgs> Upserting;
		public event EventHandler<DataUpdatedEventArgs> Updated;
		public event EventHandler<DataUpdatingEventArgs> Updating;
		public event EventHandler<DataSelectedEventArgs> Selected;
		public event EventHandler<DataSelectingEventArgs> Selecting;
		#endregion

		#region 成员字段
		private string _name;
		private ISchemaParser _schema;
		private Common.ISequence _sequence;
		private IDataAccessNaming _naming;
		private DataAccessFilterCollection _filters;
		#endregion

		#region 构造函数
		protected DataAccessBase(string name)
		{
			_name = name ?? string.Empty;
			_naming = new DataAccessNaming();
			_filters = new DataAccessFilterCollection();
		}

		protected DataAccessBase(string name, IDataAccessNaming naming)
		{
			_name = name ?? string.Empty;
			_naming = naming ?? throw new ArgumentNullException(nameof(naming));
			_filters = new DataAccessFilterCollection();
		}
		#endregion

		#region 公共属性
		/// <summary>获取数据访问的应用（子系统/业务模块）名。</summary>
		public string Name
		{
			get => _name;
			set => _name = value ?? string.Empty;
		}

		/// <summary>获取数据访问名称映射器。</summary>
		public IDataAccessNaming Naming { get => _naming; }

		/// <summary>获取数据模式解析器。</summary>
		public ISchemaParser Schema { get => _schema ?? (_schema = this.CreateSchema()); }

		/// <summary>获取或设置数据序号生成器。</summary>
		public Common.ISequence Sequence
		{
			get => _sequence ??= this.CreateSequence();
			set => _sequence = value ?? throw new ArgumentNullException();
		}

		/// <summary>获取或设置数据序号生成器提供程序。</summary>
		[Services.ServiceDependency]
		public Services.IServiceProvider<Common.ISequence> SequenceProvider { get; set; }

		/// <summary>获取数据访问器的元数据容器。</summary>
		public abstract Metadata.IDataMetadataContainer Metadata { get; }

		/// <summary>获取数据访问过滤器集合。</summary>
		public ICollection<object> Filters { get => _filters; }
		#endregion

		#region 导入方法
		public int Import<T>(IEnumerable<T> data, IEnumerable<string> members, DataImportOptions options = null) =>
			data == null ? 0 : this.Import(this.GetName<T>(), data, members, options);
		int IDataAccess.Import(string name, IEnumerable data, IEnumerable<string> members, DataImportOptions options) =>
			data == null ? 0 : this.Import(name, data, members, options);
		public int Import(string name, IEnumerable data, IEnumerable<string> members, DataImportOptions options, Func<DataImportContextBase, bool> importing = null, Action<DataImportContextBase> imported = null)
		{
			if(string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));

			if(data == null)
				return 0;

			//创建数据访问上下文对象
			var context = this.CreateImportContext(name, data, members, options);

			//处理数据访问操作前的回调
			if(importing != null && importing(context))
				return context.Count;

			//激发“Importing”事件，如果被中断则返回
			if(this.OnImporting(context))
				return context.Count;

			//调用数据访问过滤器前事件
			this.OnFiltering(context);

			//执行数据导入操作
			this.OnImport(context);

			//调用数据访问过滤器后事件
			this.OnFiltered(context);

			//激发“Imported”事件
			this.OnImported(context);

			//处理数据访问操作后的回调
			imported?.Invoke(context);

			var result = context.Count;

			//处置上下文资源
			context.Dispose();

			//返回最终的结果
			return result;
		}

		public ValueTask<int> ImportAsync<T>(IEnumerable<T> data, IEnumerable<string> members, DataImportOptions options, CancellationToken cancellation = default) =>
			data == null ? ValueTask.FromResult(0) : this.ImportAsync(this.GetName<T>(), data, members, options, cancellation);
		ValueTask<int> IDataAccess.ImportAsync(string name, IEnumerable data, IEnumerable<string> members, DataImportOptions options, CancellationToken cancellation) =>
			data == null ? ValueTask.FromResult(0) : this.ImportAsync(name, data, members, options, cancellation, null, null);
		public async ValueTask<int> ImportAsync(string name, IEnumerable data, IEnumerable<string> members, DataImportOptions options, CancellationToken cancellation, Func<DataImportContextBase, bool> importing = null, Action<DataImportContextBase> imported = null)
		{
			if(string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));

			if(data == null)
				return 0;

			//创建数据访问上下文对象
			var context = this.CreateImportContext(name, data, members, options);

			//处理数据访问操作前的回调
			if(importing != null && importing(context))
				return context.Count;

			//激发“Importing”事件，如果被中断则返回
			if(this.OnImporting(context))
				return context.Count;

			//调用数据访问过滤器前事件
			this.OnFiltering(context);

			//执行数据导入操作
			await this.OnImportAsync(context, cancellation);

			//调用数据访问过滤器后事件
			this.OnFiltered(context);

			//激发“Imported”事件
			this.OnImported(context);

			//处理数据访问操作后的回调
			imported?.Invoke(context);

			var result = context.Count;

			//处置上下文资源
			context.Dispose();

			//返回最终的结果
			return result;
		}

		protected abstract void OnImport(DataImportContextBase context);
		protected abstract ValueTask OnImportAsync(DataImportContextBase context, CancellationToken cancellation = default);
		#endregion

		#region 执行方法
		public IEnumerable<T> Execute<T>(string name, IDictionary<string, object> inParameters, DataExecuteOptions options = null, Func<DataExecuteContextBase, bool> executing = null, Action<DataExecuteContextBase> executed = null) => this.Execute<T>(name, inParameters, out _, options, executing, executed);
		public IEnumerable<T> Execute<T>(string name, IDictionary<string, object> inParameters, out IDictionary<string, object> outParameters, DataExecuteOptions options = null, Func<DataExecuteContextBase, bool> executing = null, Action<DataExecuteContextBase> executed = null)
		{
			if(string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));

			//创建数据访问上下文对象
			var context = this.CreateExecuteContext(name, false, typeof(T), inParameters, options);

			//处理数据访问操作前的回调
			if(executing != null && executing(context))
			{
				//设置默认的返回参数值
				outParameters = context.OutParameters;

				//返回委托回调的结果
				return context.Result as IEnumerable<T>;
			}

			//激发“Executing”事件，如果被中断则返回
			if(this.OnExecuting(context))
			{
				//设置默认的返回参数值
				outParameters = context.OutParameters;

				//返回事件执行后的结果
				return context.Result as IEnumerable<T>;
			}

			//调用数据访问过滤器前事件
			this.OnFiltering(context);

			//执行数据操作方法
			this.OnExecute(context);

			//调用数据访问过滤器后事件
			this.OnFiltered(context);

			//激发“Executed”事件
			this.OnExecuted(context);

			//处理数据访问操作后的回调
			if(executed != null)
				executed(context);

			//再次更新返回参数值
			outParameters = context.OutParameters;

			var result = ToEnumerable<T>(context.Result);

			//处置上下文资源
			context.Dispose();

			//返回最终的结果
			return result;
		}

		public async Task<IEnumerable<T>> ExecuteAsync<T>(string name, IDictionary<string, object> inParameters, DataExecuteOptions options = null, Func<DataExecuteContextBase, bool> executing = null, Action<DataExecuteContextBase> executed = null, CancellationToken cancellation = default)
		{
			if(string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));

			//创建数据访问上下文对象
			var context = this.CreateExecuteContext(name, false, typeof(T), inParameters, options);

			//处理数据访问操作前的回调
			if(executing != null && executing(context))
				return context.Result as IEnumerable<T>;

			//激发“Executing”事件，如果被中断则返回
			if(this.OnExecuting(context))
				return context.Result as IEnumerable<T>;

			//调用数据访问过滤器前事件
			this.OnFiltering(context);

			//执行数据操作方法
			await this.OnExecuteAsync(context, cancellation);

			//调用数据访问过滤器后事件
			this.OnFiltered(context);

			//激发“Executed”事件
			this.OnExecuted(context);

			//处理数据访问操作后的回调
			if(executed != null)
				executed(context);

			var result = ToEnumerable<T>(context.Result);

			//处置上下文资源
			context.Dispose();

			//返回最终的结果
			return result;
		}

		public object ExecuteScalar(string name, IDictionary<string, object> inParameters, DataExecuteOptions options = null, Func<DataExecuteContextBase, bool> executing = null, Action<DataExecuteContextBase> executed = null) => this.ExecuteScalar(name, inParameters, out _, options, executing, executed);
		public object ExecuteScalar(string name, IDictionary<string, object> inParameters, out IDictionary<string, object> outParameters, DataExecuteOptions options = null, Func<DataExecuteContextBase, bool> executing = null, Action<DataExecuteContextBase> executed = null)
		{
			if(string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));

			//创建数据访问上下文对象
			var context = this.CreateExecuteContext(name, true, typeof(object), inParameters, options);

			//处理数据访问操作前的回调
			if(executing != null && executing(context))
			{
				//设置默认的返回参数值
				outParameters = context.OutParameters;

				//返回委托回调的结果
				return context.Result;
			}

			//激发“Executing”事件，如果被中断则返回
			if(this.OnExecuting(context))
			{
				//设置默认的返回参数值
				outParameters = context.OutParameters;

				//返回事件执行后的结果
				return context.Result;
			}

			//调用数据访问过滤器前事件
			this.OnFiltering(context);

			//执行数据操作方法
			this.OnExecute(context);

			//调用数据访问过滤器后事件
			this.OnFiltered(context);

			//激发“Executed”事件
			this.OnExecuted(context);

			//处理数据访问操作后的回调
			if(executed != null)
				executed(context);

			//再次更新返回参数值
			outParameters = context.OutParameters;

			var result = context.Result;

			//处置上下文资源
			context.Dispose();

			//返回最终的结果
			return result;
		}

		public async Task<object> ExecuteScalarAsync(string name, IDictionary<string, object> inParameters, DataExecuteOptions options = null, Func<DataExecuteContextBase, bool> executing = null, Action<DataExecuteContextBase> executed = null, CancellationToken cancellation = default)
		{
			if(string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));

			//创建数据访问上下文对象
			var context = this.CreateExecuteContext(name, true, typeof(object), inParameters, options);

			//处理数据访问操作前的回调
			if(executing != null && executing(context))
				return context.Result;

			//激发“Executing”事件，如果被中断则返回
			if(this.OnExecuting(context))
				return context.Result;

			//调用数据访问过滤器前事件
			this.OnFiltering(context);

			//执行数据操作方法
			await this.OnExecuteAsync(context, cancellation);

			//调用数据访问过滤器后事件
			this.OnFiltered(context);

			//激发“Executed”事件
			this.OnExecuted(context);

			//处理数据访问操作后的回调
			if(executed != null)
				executed(context);

			var result = context.Result;

			//处置上下文资源
			context.Dispose();

			//返回最终的结果
			return result;
		}

		protected abstract void OnExecute(DataExecuteContextBase context);
		protected abstract Task OnExecuteAsync(DataExecuteContextBase context, CancellationToken cancellation);
		#endregion

		#region 存在方法
		public bool Exists<T>(ICondition criteria, DataExistsOptions options = null, Func<DataExistContextBase, bool> existing = null, Action<DataExistContextBase> existed = null) => this.Exists(this.GetName<T>(), criteria, options, existing, existed);
		public bool Exists(string name, ICondition criteria, DataExistsOptions options = null, Func<DataExistContextBase, bool> existing = null, Action<DataExistContextBase> existed = null)
		{
			if(string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));

			//创建数据访问上下文对象
			var context = this.CreateExistContext(name, criteria, options);

			//处理数据访问操作前的回调
			if(existing != null && existing(context))
				return context.Result;

			//激发“Existing”事件，如果被中断则返回
			if(this.OnExisting(context))
				return context.Result;

			//调用数据访问过滤器前事件
			this.OnFiltering(context);

			//执行存在操作方法
			this.OnExists(context);

			//调用数据访问过滤器后事件
			this.OnFiltered(context);

			//激发“Existed”事件
			this.OnExisted(context);

			//处理数据访问操作后的回调
			if(existed != null)
				existed(context);

			var result = context.Result;

			//处置上下文资源
			context.Dispose();

			//返回最终的结果
			return result;
		}

		public Task<bool> ExistsAsync<T>(ICondition criteria, DataExistsOptions options = null, Func<DataExistContextBase, bool> existing = null, Action<DataExistContextBase> existed = null, CancellationToken cancellation = default) => this.ExistsAsync(this.GetName<T>(), criteria, options, existing, existed, cancellation);
		public async Task<bool> ExistsAsync(string name, ICondition criteria, DataExistsOptions options = null, Func<DataExistContextBase, bool> existing = null, Action<DataExistContextBase> existed = null, CancellationToken cancellation = default)
		{
			if(string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));

			//创建数据访问上下文对象
			var context = this.CreateExistContext(name, criteria, options);

			//处理数据访问操作前的回调
			if(existing != null && existing(context))
				return context.Result;

			//激发“Existing”事件，如果被中断则返回
			if(this.OnExisting(context))
				return context.Result;

			//调用数据访问过滤器前事件
			this.OnFiltering(context);

			//执行存在操作方法
			await this.OnExistsAsync(context, cancellation);

			//调用数据访问过滤器后事件
			this.OnFiltered(context);

			//激发“Existed”事件
			this.OnExisted(context);

			//处理数据访问操作后的回调
			if(existed != null)
				existed(context);

			var result = context.Result;

			//处置上下文资源
			context.Dispose();

			//返回最终的结果
			return result;
		}

		protected abstract void OnExists(DataExistContextBase context);
		protected abstract Task OnExistsAsync(DataExistContextBase context, CancellationToken cancellation);
		#endregion

		#region 聚合方法
		public int Count<T>(ICondition criteria = null, string member = null, DataAggregateOptions options = null) => this.Aggregate<int>(this.GetName<T>(), new DataAggregate(DataAggregateFunction.Count, member), criteria, null, null, null) ?? 0;
		public int Count(string name, ICondition criteria = null, string member = null, DataAggregateOptions options = null) => this.Aggregate<int>(name, new DataAggregate(DataAggregateFunction.Count, member), criteria, null, null, null) ?? 0;
		public TValue? Aggregate<T, TValue>(DataAggregateFunction function, string member, ICondition criteria = null, DataAggregateOptions options = null) where TValue : struct, IEquatable<TValue> => this.Aggregate<TValue>(this.GetName<T>(), new DataAggregate(function, member), criteria, options, null, null);
		public TValue? Aggregate<T, TValue>(DataAggregate aggregate, ICondition criteria = null, DataAggregateOptions options = null, Func<DataAggregateContextBase, bool> aggregating = null, Action<DataAggregateContextBase> aggregated = null) where TValue : struct, IEquatable<TValue> => this.Aggregate<TValue>(this.GetName<T>(), aggregate, criteria, options, aggregating, aggregated);
		public TValue? Aggregate<TValue>(string name, DataAggregateFunction function, string member, ICondition criteria = null, DataAggregateOptions options = null) where TValue : struct, IEquatable<TValue> => this.Aggregate<TValue>(name, new DataAggregate(function, member), criteria, options, null, null);
		public TValue? Aggregate<TValue>(string name, DataAggregate aggregate, ICondition criteria = null, DataAggregateOptions options = null, Func<DataAggregateContextBase, bool> aggregating = null, Action<DataAggregateContextBase> aggregated = null) where TValue : struct, IEquatable<TValue>
		{
			if(string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));

			//创建数据访问上下文对象
			var context = this.CreateAggregateContext(name, aggregate, criteria, options);

			//处理数据访问操作前的回调
			if(aggregating != null && aggregating(context))
				return context.GetValue<TValue>();

			//激发“Aggregating”事件，如果被中断则返回
			if(this.OnAggregating(context))
				return context.GetValue<TValue>();

			//调用数据访问过滤器前事件
			this.OnFiltering(context);

			//执行聚合操作方法
			this.OnAggregate(context);

			//调用数据访问过滤器后事件
			this.OnFiltered(context);

			//激发“Aggregated”事件
			this.OnAggregated(context);

			//处理数据访问操作后的回调
			if(aggregated != null)
				aggregated(context);

			var value = context.GetValue<TValue>();

			//处置上下文资源
			context.Dispose();

			//返回最终的结果
			return value;
		}

		public Task<TValue?> AggregateAsync<T, TValue>(DataAggregateFunction function, string member, ICondition criteria = null, DataAggregateOptions options = null, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue> => this.AggregateAsync<TValue>(this.GetName<T>(), new DataAggregate(function, member), criteria, options, null, null, cancellation);
		public Task<TValue?> AggregateAsync<T, TValue>(DataAggregate aggregate, ICondition criteria = null, DataAggregateOptions options = null, Func<DataAggregateContextBase, bool> aggregating = null, Action<DataAggregateContextBase> aggregated = null, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue> => this.AggregateAsync<TValue>(this.GetName<T>(), aggregate, criteria, options, aggregating, aggregated, cancellation);
		public Task<TValue?> AggregateAsync<TValue>(string name, DataAggregateFunction function, string member, ICondition criteria = null, DataAggregateOptions options = null, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue> => this.AggregateAsync<TValue>(name, new DataAggregate(function, member), criteria, options, null, null, cancellation);
		public async Task<TValue?> AggregateAsync<TValue>(string name, DataAggregate aggregate, ICondition criteria = null, DataAggregateOptions options = null, Func<DataAggregateContextBase, bool> aggregating = null, Action<DataAggregateContextBase> aggregated = null, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue>
		{
			if(string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));

			//创建数据访问上下文对象
			var context = this.CreateAggregateContext(name, aggregate, criteria, options);

			//处理数据访问操作前的回调
			if(aggregating != null && aggregating(context))
				return context.GetValue<TValue>();

			//激发“Aggregating”事件，如果被中断则返回
			if(this.OnAggregating(context))
				return context.GetValue<TValue>();

			//调用数据访问过滤器前事件
			this.OnFiltering(context);

			//执行聚合操作方法
			await this.OnAggregateAsync(context, cancellation);

			//调用数据访问过滤器后事件
			this.OnFiltered(context);

			//激发“Aggregated”事件
			this.OnAggregated(context);

			//处理数据访问操作后的回调
			if(aggregated != null)
				aggregated(context);

			var value = context.GetValue<TValue>();

			//处置上下文资源
			context.Dispose();

			//返回最终的结果
			return value;
		}

		protected abstract void OnAggregate(DataAggregateContextBase context);
		protected abstract Task OnAggregateAsync(DataAggregateContextBase context, CancellationToken cancellation);
		#endregion

		#region 递增方法
		public long Increment<T>(string member, ICondition criteria) => this.Increment(this.GetName<T>(), member, criteria, 1, null, null, null);
		public long Increment<T>(string member, ICondition criteria, DataIncrementOptions options) => this.Increment(this.GetName<T>(), member, criteria, 1, options, null, null);
		public long Increment<T>(string member, ICondition criteria, int interval) => this.Increment(this.GetName<T>(), member, criteria, interval, null, null, null);
		public long Increment<T>(string member, ICondition criteria, int interval, DataIncrementOptions options, Func<DataIncrementContextBase, bool> incrementing = null, Action<DataIncrementContextBase> incremented = null) => this.Increment(this.GetName<T>(), member, criteria, interval, options, incrementing, incremented);
		public long Increment(string name, string member, ICondition criteria) => this.Increment(name, member, criteria, 1, null, null, null);
		public long Increment(string name, string member, ICondition criteria, DataIncrementOptions options) => this.Increment(name, member, criteria, 1, options, null, null);
		public long Increment(string name, string member, ICondition criteria, int interval) => this.Increment(name, member, criteria, interval, null, null, null);
		public long Increment(string name, string member, ICondition criteria, int interval, DataIncrementOptions options, Func<DataIncrementContextBase, bool> incrementing = null, Action<DataIncrementContextBase> incremented = null)
		{
			if(string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));

			if(string.IsNullOrEmpty(member))
				throw new ArgumentNullException(nameof(member));

			//创建数据访问上下文对象
			var context = this.CreateIncrementContext(name, member, criteria, interval, options);

			//处理数据访问操作前的回调
			if(incrementing != null && incrementing(context))
				return context.Result;

			//激发“Incrementing”事件
			if(this.OnIncrementing(context))
				return context.Result;

			//调用数据访问过滤器前事件
			this.OnFiltering(context);

			//执行递增操作方法
			this.OnIncrement(context);

			//调用数据访问过滤器后事件
			this.OnFiltered(context);

			//激发“Incremented”事件
			this.OnIncremented(context);

			//处理数据访问操作后的回调
			if(incremented != null)
				incremented(context);

			var result = context.Result;

			//处置上下文资源
			context.Dispose();

			//返回最终的结果
			return result;
		}

		public Task<long> IncrementAsync<T>(string member, ICondition criteria, CancellationToken cancellation = default) => this.IncrementAsync(this.GetName<T>(), member, criteria, 1, null, null, null, cancellation);
		public Task<long> IncrementAsync<T>(string member, ICondition criteria, DataIncrementOptions options, CancellationToken cancellation = default) => this.IncrementAsync(this.GetName<T>(), member, criteria, 1, options, null, null, cancellation);
		public Task<long> IncrementAsync<T>(string member, ICondition criteria, int interval, CancellationToken cancellation = default) => this.IncrementAsync(this.GetName<T>(), member, criteria, interval, null, null, null, cancellation);
		public Task<long> IncrementAsync<T>(string member, ICondition criteria, int interval, DataIncrementOptions options, Func<DataIncrementContextBase, bool> incrementing = null, Action<DataIncrementContextBase> incremented = null, CancellationToken cancellation = default) => this.IncrementAsync(this.GetName<T>(), member, criteria, interval, options, incrementing, incremented, cancellation);
		public Task<long> IncrementAsync(string name, string member, ICondition criteria, CancellationToken cancellation = default) => this.IncrementAsync(name, member, criteria, 1, null, null, null, cancellation);
		public Task<long> IncrementAsync(string name, string member, ICondition criteria, DataIncrementOptions options, CancellationToken cancellation = default) => this.IncrementAsync(name, member, criteria, 1, options, null, null, cancellation);
		public Task<long> IncrementAsync(string name, string member, ICondition criteria, int interval, CancellationToken cancellation = default) => this.IncrementAsync(name, member, criteria, interval, null, null, null, cancellation);
		public async Task<long> IncrementAsync(string name, string member, ICondition criteria, int interval, DataIncrementOptions options, Func<DataIncrementContextBase, bool> incrementing = null, Action<DataIncrementContextBase> incremented = null, CancellationToken cancellation = default)
		{
			if(string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));

			if(string.IsNullOrEmpty(member))
				throw new ArgumentNullException(nameof(member));

			//创建数据访问上下文对象
			var context = this.CreateIncrementContext(name, member, criteria, interval, options);

			//处理数据访问操作前的回调
			if(incrementing != null && incrementing(context))
				return context.Result;

			//激发“Incrementing”事件
			if(this.OnIncrementing(context))
				return context.Result;

			//调用数据访问过滤器前事件
			this.OnFiltering(context);

			//执行递增操作方法
			await this.OnIncrementAsync(context, cancellation);

			//调用数据访问过滤器后事件
			this.OnFiltered(context);

			//激发“Incremented”事件
			this.OnIncremented(context);

			//处理数据访问操作后的回调
			if(incremented != null)
				incremented(context);

			var result = context.Result;

			//处置上下文资源
			context.Dispose();

			//返回最终的结果
			return result;
		}

		public long Decrement<T>(string member, ICondition criteria) => this.Decrement(this.GetName<T>(), member, criteria, 1, null, null, null);
		public long Decrement<T>(string member, ICondition criteria, DataIncrementOptions options) => this.Decrement(this.GetName<T>(), member, criteria, 1, options, null, null);
		public long Decrement<T>(string member, ICondition criteria, int interval) => this.Decrement(this.GetName<T>(), member, criteria, interval, null, null, null);
		public long Decrement<T>(string member, ICondition criteria, int interval, DataIncrementOptions options, Func<DataIncrementContextBase, bool> decrementing = null, Action<DataIncrementContextBase> decremented = null) => this.Increment(this.GetName<T>(), member, criteria, -interval, options, decrementing, decremented);
		public long Decrement(string name, string member, ICondition criteria) => this.Decrement(name, member, criteria, 1, null, null, null);
		public long Decrement(string name, string member, ICondition criteria, DataIncrementOptions options) => this.Decrement(name, member, criteria, 1, options, null, null);
		public long Decrement(string name, string member, ICondition criteria, int interval) => this.Decrement(name, member, criteria, interval, null, null, null);
		public long Decrement(string name, string member, ICondition criteria, int interval, DataIncrementOptions options, Func<DataIncrementContextBase, bool> decrementing = null, Action<DataIncrementContextBase> decremented = null) => this.Increment(name, member, criteria, -interval, options, decrementing, decremented);

		protected abstract void OnIncrement(DataIncrementContextBase context);
		protected abstract Task OnIncrementAsync(DataIncrementContextBase context, CancellationToken cancellation);
		#endregion

		#region 删除方法
		public int Delete<T>(ICondition criteria, string schema = null) => this.Delete(this.GetName<T>(), criteria, schema, null, null, null);
		public int Delete<T>(ICondition criteria, DataDeleteOptions options) => this.Delete(this.GetName<T>(), criteria, string.Empty, options, null, null);
		public int Delete<T>(ICondition criteria, string schema, DataDeleteOptions options, Func<DataDeleteContextBase, bool> deleting = null, Action<DataDeleteContextBase> deleted = null) => this.Delete(this.GetName<T>(), criteria, schema, options, deleting, deleted);
		public int Delete(string name, ICondition criteria, string schema = null) => this.Delete(name, criteria, schema, null, null, null);
		public int Delete(string name, ICondition criteria, DataDeleteOptions options) => this.Delete(name, criteria, string.Empty, options, null, null);
		public int Delete(string name, ICondition criteria, string schema, DataDeleteOptions options, Func<DataDeleteContextBase, bool> deleting = null, Action<DataDeleteContextBase> deleted = null) => this.Delete(name, criteria, this.Schema.Parse(name, schema), options, deleting, deleted);
		public int Delete(string name, ICondition criteria, ISchema schema, DataDeleteOptions options, Func<DataDeleteContextBase, bool> deleting = null, Action<DataDeleteContextBase> deleted = null)
		{
			if(string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));

			//创建数据访问上下文对象
			var context = this.CreateDeleteContext(name, criteria, schema, options);

			//处理数据访问操作前的回调
			if(deleting != null && deleting(context))
				return context.Count;

			//激发“Deleting”事件，如果被中断则返回
			if(this.OnDeleting(context))
				return context.Count;

			//调用数据访问过滤器前事件
			this.OnFiltering(context);

			//执行数据删除操作
			this.OnDelete(context);

			//调用数据访问过滤器后事件
			this.OnFiltered(context);

			//激发“Deleted”事件
			this.OnDeleted(context);

			//处理数据访问操作后的回调
			if(deleted != null)
				deleted(context);

			var result = context.Count;

			//处置上下文资源
			context.Dispose();

			//返回最终的结果
			return result;
		}

		public Task<int> DeleteAsync<T>(ICondition criteria, string schema = null, CancellationToken cancellation = default) => this.DeleteAsync(this.GetName<T>(), criteria, schema, null, null, null, cancellation);
		public Task<int> DeleteAsync<T>(ICondition criteria, DataDeleteOptions options, CancellationToken cancellation = default) => this.DeleteAsync(this.GetName<T>(), criteria, String.Empty, options, null, null, cancellation);
		public Task<int> DeleteAsync<T>(ICondition criteria, string schema, DataDeleteOptions options, Func<DataDeleteContextBase, bool> deleting = null, Action<DataDeleteContextBase> deleted = null, CancellationToken cancellation = default) => this.DeleteAsync(this.GetName<T>(), criteria, schema, options, deleting, deleted, cancellation);
		public Task<int> DeleteAsync(string name, ICondition criteria, string schema = null, CancellationToken cancellation = default) => this.DeleteAsync(name, criteria, schema, null, null, null, cancellation);
		public Task<int> DeleteAsync(string name, ICondition criteria, DataDeleteOptions options, CancellationToken cancellation = default) => this.DeleteAsync(name, criteria, string.Empty, options, null, null, cancellation);
		public Task<int> DeleteAsync(string name, ICondition criteria, string schema, DataDeleteOptions options, Func<DataDeleteContextBase, bool> deleting = null, Action<DataDeleteContextBase> deleted = null, CancellationToken cancellation = default) => this.DeleteAsync(name, criteria, this.Schema.Parse(name, schema), options, deleting, deleted, cancellation);
		public async Task<int> DeleteAsync(string name, ICondition criteria, ISchema schema, DataDeleteOptions options, Func<DataDeleteContextBase, bool> deleting = null, Action<DataDeleteContextBase> deleted = null, CancellationToken cancellation = default)
		{
			if(string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));

			//创建数据访问上下文对象
			var context = this.CreateDeleteContext(name, criteria, schema, options);

			//处理数据访问操作前的回调
			if(deleting != null && deleting(context))
				return context.Count;

			//激发“Deleting”事件，如果被中断则返回
			if(this.OnDeleting(context))
				return context.Count;

			//调用数据访问过滤器前事件
			this.OnFiltering(context);

			//执行数据删除操作
			await this.OnDeleteAsync(context, cancellation);

			//调用数据访问过滤器后事件
			this.OnFiltered(context);

			//激发“Deleted”事件
			this.OnDeleted(context);

			//处理数据访问操作后的回调
			if(deleted != null)
				deleted(context);

			var result = context.Count;

			//处置上下文资源
			context.Dispose();

			//返回最终的结果
			return result;
		}

		protected abstract void OnDelete(DataDeleteContextBase context);
		protected abstract Task OnDeleteAsync(DataDeleteContextBase context, CancellationToken cancellation);
		#endregion

		#region 插入方法
		public int Insert<T>(T data) => data == null ? 0 : this.Insert(this.GetName<T>(), data, string.Empty, null, null, null);
		public int Insert<T>(T data, DataInsertOptions options) => data == null ? 0 : this.Insert(this.GetName<T>(), data, string.Empty, options, null, null);
		public int Insert<T>(T data, string schema) => data == null ? 0 : this.Insert(this.GetName<T>(), data, schema, null, null, null);
		public int Insert<T>(T data, string schema, DataInsertOptions options, Func<DataInsertContextBase, bool> inserting = null, Action<DataInsertContextBase> inserted = null) => data == null ? 0 : this.Insert(this.GetName<T>(), data, schema, options, inserting, inserted);
		public int Insert<T>(object data) => data == null ? 0 : this.Insert(this.GetName<T>(), data, string.Empty, null, null, null);
		public int Insert<T>(object data, DataInsertOptions options) => data == null ? 0 : this.Insert(this.GetName<T>(), data, string.Empty, options, null, null);
		public int Insert<T>(object data, string schema) => data == null ? 0 : this.Insert(this.GetName<T>(), data, schema, null, null, null);
		public int Insert<T>(object data, string schema, DataInsertOptions options, Func<DataInsertContextBase, bool> inserting = null, Action<DataInsertContextBase> inserted = null) => data == null ? 0 : this.Insert(this.GetName<T>(), data, schema, options, inserting, inserted);

		public int Insert(string name, object data) => this.Insert(name, data, string.Empty, null, null, null);
		public int Insert(string name, object data, DataInsertOptions options) => this.Insert(name, data, string.Empty, options, null, null);
		public int Insert(string name, object data, string schema) => this.Insert(name, data, schema, null, null, null);
		public int Insert(string name, object data, string schema, DataInsertOptions options, Func<DataInsertContextBase, bool> inserting = null, Action<DataInsertContextBase> inserted = null) => this.Insert(name, data, this.Schema.Parse(name, schema, data.GetType()), options, inserting, inserted);
		public int Insert(string name, object data, ISchema schema, DataInsertOptions options, Func<DataInsertContextBase, bool> inserting = null, Action<DataInsertContextBase> inserted = null)
		{
			if(string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));

			if(data == null)
				return 0;

			//创建数据访问上下文对象
			var context = this.CreateInsertContext(name, false, data, schema, options);

			//处理数据访问操作前的回调
			if(inserting != null && inserting(context))
				return context.Count;

			//激发“Inserting”事件，如果被中断则返回
			if(this.OnInserting(context))
				return context.Count;

			//调用数据访问过滤器前事件
			this.OnFiltering(context);

			//执行数据插入操作
			this.OnInsert(context);

			//调用数据访问过滤器后事件
			this.OnFiltered(context);

			//激发“Inserted”事件
			this.OnInserted(context);

			//处理数据访问操作后的回调
			if(inserted != null)
				inserted(context);

			var result = context.Count;

			//处置上下文资源
			context.Dispose();

			//返回最终的结果
			return result;
		}

		public Task<int> InsertAsync<T>(T data, CancellationToken cancellation = default) => data == null ? Task.FromResult(0) : this.InsertAsync(this.GetName<T>(), data, string.Empty, null, null, null, cancellation);
		public Task<int> InsertAsync<T>(T data, DataInsertOptions options, CancellationToken cancellation = default) => data == null ? Task.FromResult(0) : this.InsertAsync(this.GetName<T>(), data, string.Empty, options, null, null, cancellation);
		public Task<int> InsertAsync<T>(T data, string schema, CancellationToken cancellation = default) => data == null ? Task.FromResult(0) : this.InsertAsync(this.GetName<T>(), data, schema, null, null, null, cancellation);
		public Task<int> InsertAsync<T>(T data, string schema, DataInsertOptions options, Func<DataInsertContextBase, bool> inserting = null, Action<DataInsertContextBase> inserted = null, CancellationToken cancellation = default) => data == null ? Task.FromResult(0) : this.InsertAsync(this.GetName<T>(), data, schema, options, inserting, inserted, cancellation);
		public Task<int> InsertAsync<T>(object data, CancellationToken cancellation = default) => data == null ? Task.FromResult(0) : this.InsertAsync(this.GetName<T>(), data, string.Empty, null, null, null, cancellation);
		public Task<int> InsertAsync<T>(object data, DataInsertOptions options, CancellationToken cancellation = default) => data == null ? Task.FromResult(0) : this.InsertAsync(this.GetName<T>(), data, string.Empty, options, null, null, cancellation);
		public Task<int> InsertAsync<T>(object data, string schema, CancellationToken cancellation = default) => data == null ? Task.FromResult(0) : this.InsertAsync(this.GetName<T>(), data, schema, null, null, null, cancellation);
		public Task<int> InsertAsync<T>(object data, string schema, DataInsertOptions options, Func<DataInsertContextBase, bool> inserting = null, Action<DataInsertContextBase> inserted = null, CancellationToken cancellation = default) => data == null ? Task.FromResult(0) : this.InsertAsync(this.GetName<T>(), data, schema, options, inserting, inserted, cancellation);

		public Task<int> InsertAsync(string name, object data, CancellationToken cancellation = default) => this.InsertAsync(name, data, string.Empty, null, null, null, cancellation);
		public Task<int> InsertAsync(string name, object data, DataInsertOptions options, CancellationToken cancellation = default) => this.InsertAsync(name, data, string.Empty, options, null, null, cancellation);
		public Task<int> InsertAsync(string name, object data, string schema, CancellationToken cancellation = default) => this.InsertAsync(name, data, schema, null, null, null, cancellation);
		public Task<int> InsertAsync(string name, object data, string schema, DataInsertOptions options, Func<DataInsertContextBase, bool> inserting = null, Action<DataInsertContextBase> inserted = null, CancellationToken cancellation = default) => this.InsertAsync(name, data, this.Schema.Parse(name, schema, data.GetType()), options, inserting, inserted, cancellation);
		public async Task<int> InsertAsync(string name, object data, ISchema schema, DataInsertOptions options, Func<DataInsertContextBase, bool> inserting = null, Action<DataInsertContextBase> inserted = null, CancellationToken cancellation = default)
		{
			if(string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));

			if(data == null)
				return 0;

			//创建数据访问上下文对象
			var context = this.CreateInsertContext(name, false, data, schema, options);

			//处理数据访问操作前的回调
			if(inserting != null && inserting(context))
				return context.Count;

			//激发“Inserting”事件，如果被中断则返回
			if(this.OnInserting(context))
				return context.Count;

			//调用数据访问过滤器前事件
			this.OnFiltering(context);

			//执行数据插入操作
			await this.OnInsertAsync(context, cancellation);

			//调用数据访问过滤器后事件
			this.OnFiltered(context);

			//激发“Inserted”事件
			this.OnInserted(context);

			//处理数据访问操作后的回调
			if(inserted != null)
				inserted(context);

			var result = context.Count;

			//处置上下文资源
			context.Dispose();

			//返回最终的结果
			return result;
		}

		public int InsertMany<T>(IEnumerable<T> items) => items == null ? 0 : this.InsertMany(this.GetName<T>(), items, string.Empty, null, null, null);
		public int InsertMany<T>(IEnumerable<T> items, DataInsertOptions options) => items == null ? 0 : this.InsertMany(this.GetName<T>(), items, string.Empty, options, null, null);
		public int InsertMany<T>(IEnumerable<T> items, string schema) => items == null ? 0 : this.InsertMany(this.GetName<T>(), items, schema, null, null, null);
		public int InsertMany<T>(IEnumerable<T> items, string schema, DataInsertOptions options, Func<DataInsertContextBase, bool> inserting = null, Action<DataInsertContextBase> inserted = null) => items == null ? 0 : this.InsertMany(this.GetName<T>(), items, schema, options, inserting, inserted);
		public int InsertMany<T>(IEnumerable items) => items == null ? 0 : this.InsertMany(this.GetName<T>(), items, string.Empty, null, null, null);
		public int InsertMany<T>(IEnumerable items, DataInsertOptions options) => items == null ? 0 : this.InsertMany(this.GetName<T>(), items, string.Empty, options, null, null);
		public int InsertMany<T>(IEnumerable items, string schema) => items == null ? 0 : this.InsertMany(this.GetName<T>(), items, schema, null, null, null);
		public int InsertMany<T>(IEnumerable items, string schema, DataInsertOptions options, Func<DataInsertContextBase, bool> inserting = null, Action<DataInsertContextBase> inserted = null) => items == null ? 0 : this.InsertMany(this.GetName<T>(), items, schema, options, inserting, inserted);
		public int InsertMany(string name, IEnumerable items) => this.InsertMany(name, items, string.Empty, null, null, null);
		public int InsertMany(string name, IEnumerable items, DataInsertOptions options) => this.InsertMany(name, items, string.Empty, options, null, null);
		public int InsertMany(string name, IEnumerable items, string schema) => this.InsertMany(name, items, schema, null, null, null);
		public int InsertMany(string name, IEnumerable items, string schema, DataInsertOptions options, Func<DataInsertContextBase, bool> inserting = null, Action<DataInsertContextBase> inserted = null) => this.InsertMany(name, items, this.Schema.Parse(name, schema, Common.TypeExtension.GetElementType(items.GetType())), options, inserting, inserted);
		public int InsertMany(string name, IEnumerable items, ISchema schema, DataInsertOptions options, Func<DataInsertContextBase, bool> inserting = null, Action<DataInsertContextBase> inserted = null)
		{
			if(string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));

			if(items == null)
				return 0;

			//创建数据访问上下文对象
			var context = this.CreateInsertContext(name, true, items, schema, options);

			//处理数据访问操作前的回调
			if(inserting != null && inserting(context))
				return context.Count;

			//激发“Inserting”事件，如果被中断则返回
			if(this.OnInserting(context))
				return context.Count;

			//调用数据访问过滤器前事件
			this.OnFiltering(context);

			//执行数据插入操作
			this.OnInsert(context);

			//调用数据访问过滤器后事件
			this.OnFiltered(context);

			//激发“Inserted”事件
			this.OnInserted(context);

			//处理数据访问操作后的回调
			if(inserted != null)
				inserted(context);

			var result = context.Count;

			//处置上下文资源
			context.Dispose();

			//返回最终的结果
			return result;
		}

		public Task<int> InsertManyAsync<T>(IEnumerable<T> items, CancellationToken cancellation = default) => items == null ? Task.FromResult(0) : this.InsertManyAsync(this.GetName<T>(), items, string.Empty, null, null, null, cancellation);
		public Task<int> InsertManyAsync<T>(IEnumerable<T> items, DataInsertOptions options, CancellationToken cancellation = default) => items == null ? Task.FromResult(0) : this.InsertManyAsync(this.GetName<T>(), items, string.Empty, options, null, null, cancellation);
		public Task<int> InsertManyAsync<T>(IEnumerable<T> items, string schema, CancellationToken cancellation = default) => items == null ? Task.FromResult(0) : this.InsertManyAsync(this.GetName<T>(), items, schema, null, null, null, cancellation);
		public Task<int> InsertManyAsync<T>(IEnumerable<T> items, string schema, DataInsertOptions options, Func<DataInsertContextBase, bool> inserting = null, Action<DataInsertContextBase> inserted = null, CancellationToken cancellation = default) => items == null ? Task.FromResult(0) : this.InsertManyAsync(this.GetName<T>(), items, schema, options, inserting, inserted, cancellation);
		public Task<int> InsertManyAsync<T>(IEnumerable items, CancellationToken cancellation = default) => items == null ? Task.FromResult(0) : this.InsertManyAsync(this.GetName<T>(), items, string.Empty, null, null, null, cancellation);
		public Task<int> InsertManyAsync<T>(IEnumerable items, DataInsertOptions options, CancellationToken cancellation = default) => items == null ? Task.FromResult(0) : this.InsertManyAsync(this.GetName<T>(), items, string.Empty, options, null, null, cancellation);
		public Task<int> InsertManyAsync<T>(IEnumerable items, string schema, CancellationToken cancellation = default) => items == null ? Task.FromResult(0) : this.InsertManyAsync(this.GetName<T>(), items, schema, null, null, null, cancellation);
		public Task<int> InsertManyAsync<T>(IEnumerable items, string schema, DataInsertOptions options, Func<DataInsertContextBase, bool> inserting = null, Action<DataInsertContextBase> inserted = null, CancellationToken cancellation = default) => items == null ? Task.FromResult(0) : this.InsertManyAsync(this.GetName<T>(), items, schema, options, inserting, inserted, cancellation);
		public Task<int> InsertManyAsync(string name, IEnumerable items, CancellationToken cancellation = default) => this.InsertManyAsync(name, items, string.Empty, null, null, null, cancellation);
		public Task<int> InsertManyAsync(string name, IEnumerable items, DataInsertOptions options, CancellationToken cancellation = default) => this.InsertManyAsync(name, items, string.Empty, options, null, null, cancellation);
		public Task<int> InsertManyAsync(string name, IEnumerable items, string schema, CancellationToken cancellation = default) => this.InsertManyAsync(name, items, schema, null, null, null, cancellation);
		public Task<int> InsertManyAsync(string name, IEnumerable items, string schema, DataInsertOptions options, Func<DataInsertContextBase, bool> inserting = null, Action<DataInsertContextBase> inserted = null, CancellationToken cancellation = default) => this.InsertManyAsync(name, items, this.Schema.Parse(name, schema, Common.TypeExtension.GetElementType(items.GetType())), options, inserting, inserted, cancellation);
		public async Task<int> InsertManyAsync(string name, IEnumerable items, ISchema schema, DataInsertOptions options, Func<DataInsertContextBase, bool> inserting = null, Action<DataInsertContextBase> inserted = null, CancellationToken cancellation = default)
		{
			if(string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));

			if(items == null)
				return 0;

			//创建数据访问上下文对象
			var context = this.CreateInsertContext(name, true, items, schema, options);

			//处理数据访问操作前的回调
			if(inserting != null && inserting(context))
				return context.Count;

			//激发“Inserting”事件，如果被中断则返回
			if(this.OnInserting(context))
				return context.Count;

			//调用数据访问过滤器前事件
			this.OnFiltering(context);

			//执行数据插入操作
			await this.OnInsertAsync(context, cancellation);

			//调用数据访问过滤器后事件
			this.OnFiltered(context);

			//激发“Inserted”事件
			this.OnInserted(context);

			//处理数据访问操作后的回调
			if(inserted != null)
				inserted(context);

			var result = context.Count;

			//处置上下文资源
			context.Dispose();

			//返回最终的结果
			return result;
		}

		protected abstract void OnInsert(DataInsertContextBase context);
		protected abstract Task OnInsertAsync(DataInsertContextBase context, CancellationToken cancellation);
		#endregion

		#region 增改方法
		public int Upsert<T>(T data) => data == null ? 0 : this.Upsert(this.GetName<T>(), data, string.Empty, null, null, null);
		public int Upsert<T>(T data, DataUpsertOptions options) => data == null ? 0 : this.Upsert(this.GetName<T>(), data, string.Empty, options, null, null);
		public int Upsert<T>(T data, string schema) => data == null ? 0 : this.Upsert(this.GetName<T>(), data, schema, null, null, null);
		public int Upsert<T>(T data, string schema, DataUpsertOptions options, Func<DataUpsertContextBase, bool> upserting = null, Action<DataUpsertContextBase> upserted = null) => data == null ? 0 : this.Upsert(this.GetName<T>(), data, schema, options, upserting, upserted);

		public int Upsert<T>(object data) => data == null ? 0 : this.Upsert(this.GetName<T>(), data, string.Empty, null, null, null);
		public int Upsert<T>(object data, DataUpsertOptions options) => data == null ? 0 : this.Upsert(this.GetName<T>(), data, string.Empty, options, null, null);
		public int Upsert<T>(object data, string schema) => data == null ? 0 : this.Upsert(this.GetName<T>(), data, schema, null, null, null);
		public int Upsert<T>(object data, string schema, DataUpsertOptions options, Func<DataUpsertContextBase, bool> upserting = null, Action<DataUpsertContextBase> upserted = null) => data == null ? 0 : this.Upsert(this.GetName<T>(), data, schema, options, upserting, upserted);

		public int Upsert(string name, object data) => this.Upsert(name, data, string.Empty, null, null, null);
		public int Upsert(string name, object data, DataUpsertOptions options) => this.Upsert(name, data, string.Empty, options, null, null);
		public int Upsert(string name, object data, string schema) => this.Upsert(name, data, schema, null, null, null);
		public int Upsert(string name, object data, string schema, DataUpsertOptions options, Func<DataUpsertContextBase, bool> upserting = null, Action<DataUpsertContextBase> upserted = null) => this.Upsert(name, data, this.Schema.Parse(name, schema, data.GetType()), options, upserting, upserted);
		public int Upsert(string name, object data, ISchema schema, DataUpsertOptions options, Func<DataUpsertContextBase, bool> upserting = null, Action<DataUpsertContextBase> upserted = null)
		{
			if(string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));

			if(data == null)
				return 0;

			//创建数据访问上下文对象
			var context = this.CreateUpsertContext(name, false, data, schema, options);

			//处理数据访问操作前的回调
			if(upserting != null && upserting(context))
				return context.Count;

			//激发“Upserting”事件，如果被中断则返回
			if(this.OnUpserting(context))
				return context.Count;

			//调用数据访问过滤器前事件
			this.OnFiltering(context);

			//执行数据插入操作
			this.OnUpsert(context);

			//调用数据访问过滤器后事件
			this.OnFiltered(context);

			//激发“Upserted”事件
			this.OnUpserted(context);

			//处理数据访问操作后的回调
			if(upserted != null)
				upserted(context);

			var result = context.Count;

			//处置上下文资源
			context.Dispose();

			//返回最终的结果
			return result;
		}

		public Task<int> UpsertAsync<T>(T data, CancellationToken cancellation = default) => data == null ? Task.FromResult(0) : this.UpsertAsync(this.GetName<T>(), data, string.Empty, null, null, null, cancellation);
		public Task<int> UpsertAsync<T>(T data, DataUpsertOptions options, CancellationToken cancellation = default) => data == null ? Task.FromResult(0) : this.UpsertAsync(this.GetName<T>(), data, string.Empty, options, null, null, cancellation);
		public Task<int> UpsertAsync<T>(T data, string schema, CancellationToken cancellation = default) => data == null ? Task.FromResult(0) : this.UpsertAsync(this.GetName<T>(), data, schema, null, null, null, cancellation);
		public Task<int> UpsertAsync<T>(T data, string schema, DataUpsertOptions options, Func<DataUpsertContextBase, bool> upserting = null, Action<DataUpsertContextBase> upserted = null, CancellationToken cancellation = default) => data == null ? Task.FromResult(0) : this.UpsertAsync(this.GetName<T>(), data, schema, options, upserting, upserted, cancellation);
		public Task<int> UpsertAsync<T>(object data, CancellationToken cancellation = default) => data == null ? Task.FromResult(0) : this.UpsertAsync(this.GetName<T>(), data, string.Empty, null, null, null, cancellation);
		public Task<int> UpsertAsync<T>(object data, DataUpsertOptions options, CancellationToken cancellation = default) => data == null ? Task.FromResult(0) : this.UpsertAsync(this.GetName<T>(), data, string.Empty, options, null, null, cancellation);
		public Task<int> UpsertAsync<T>(object data, string schema, CancellationToken cancellation = default) => data == null ? Task.FromResult(0) : this.UpsertAsync(this.GetName<T>(), data, schema, null, null, null, cancellation);
		public Task<int> UpsertAsync<T>(object data, string schema, DataUpsertOptions options, Func<DataUpsertContextBase, bool> upserting = null, Action<DataUpsertContextBase> upserted = null, CancellationToken cancellation = default) => data == null ? Task.FromResult(0) : this.UpsertAsync(this.GetName<T>(), data, schema, options, upserting, upserted, cancellation);

		public Task<int> UpsertAsync(string name, object data, CancellationToken cancellation = default) => this.UpsertAsync(name, data, string.Empty, null, null, null, cancellation);
		public Task<int> UpsertAsync(string name, object data, DataUpsertOptions options, CancellationToken cancellation = default) => this.UpsertAsync(name, data, string.Empty, options, null, null, cancellation);
		public Task<int> UpsertAsync(string name, object data, string schema, CancellationToken cancellation = default) => this.UpsertAsync(name, data, schema, null, null, null, cancellation);
		public Task<int> UpsertAsync(string name, object data, string schema, DataUpsertOptions options, Func<DataUpsertContextBase, bool> upserting = null, Action<DataUpsertContextBase> upserted = null, CancellationToken cancellation = default) => this.UpsertAsync(name, data, this.Schema.Parse(name, schema, data.GetType()), options, upserting, upserted, cancellation);

		public async Task<int> UpsertAsync(string name, object data, ISchema schema, DataUpsertOptions options, Func<DataUpsertContextBase, bool> upserting = null, Action<DataUpsertContextBase> upserted = null, CancellationToken cancellation = default)
		{
			if(string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));

			if(data == null)
				return 0;

			//创建数据访问上下文对象
			var context = this.CreateUpsertContext(name, false, data, schema, options);

			//处理数据访问操作前的回调
			if(upserting != null && upserting(context))
				return context.Count;

			//激发“Upserting”事件，如果被中断则返回
			if(this.OnUpserting(context))
				return context.Count;

			//调用数据访问过滤器前事件
			this.OnFiltering(context);

			//执行数据插入操作
			await this.OnUpsertAsync(context, cancellation);

			//调用数据访问过滤器后事件
			this.OnFiltered(context);

			//激发“Upserted”事件
			this.OnUpserted(context);

			//处理数据访问操作后的回调
			if(upserted != null)
				upserted(context);

			var result = context.Count;

			//处置上下文资源
			context.Dispose();

			//返回最终的结果
			return result;
		}

		public int UpsertMany<T>(IEnumerable<T> items) => items == null ? 0 : this.UpsertMany(this.GetName<T>(), items, string.Empty, null, null, null);
		public int UpsertMany<T>(IEnumerable<T> items, DataUpsertOptions options) => items == null ? 0 : this.UpsertMany(this.GetName<T>(), items, string.Empty, options, null, null);
		public int UpsertMany<T>(IEnumerable<T> items, string schema) => items == null ? 0 : this.UpsertMany(this.GetName<T>(), items, schema, null, null, null);
		public int UpsertMany<T>(IEnumerable<T> items, string schema, DataUpsertOptions options, Func<DataUpsertContextBase, bool> upserting = null, Action<DataUpsertContextBase> upserted = null) => items == null ? 0 : this.UpsertMany(this.GetName<T>(), items, schema, options, upserting, upserted);
		public int UpsertMany<T>(IEnumerable items) => items == null ? 0 : this.UpsertMany(this.GetName<T>(), items, string.Empty, null, null, null);
		public int UpsertMany<T>(IEnumerable items, DataUpsertOptions options) => items == null ? 0 : this.UpsertMany(this.GetName<T>(), items, string.Empty, options, null, null);
		public int UpsertMany<T>(IEnumerable items, string schema) => items == null ? 0 : this.UpsertMany(this.GetName<T>(), items, schema, null, null, null);
		public int UpsertMany<T>(IEnumerable items, string schema, DataUpsertOptions options, Func<DataUpsertContextBase, bool> upserting = null, Action<DataUpsertContextBase> upserted = null) => items == null ? 0 : this.UpsertMany(this.GetName<T>(), items, schema, options, upserting, upserted);

		public int UpsertMany(string name, IEnumerable items) => this.UpsertMany(name, items, string.Empty, null, null, null);
		public int UpsertMany(string name, IEnumerable items, DataUpsertOptions options) => this.UpsertMany(name, items, string.Empty, options, null, null);
		public int UpsertMany(string name, IEnumerable items, string schema) => this.UpsertMany(name, items, schema, null, null, null);
		public int UpsertMany(string name, IEnumerable items, string schema, DataUpsertOptions options, Func<DataUpsertContextBase, bool> upserting = null, Action<DataUpsertContextBase> upserted = null) => this.UpsertMany(name, items, this.Schema.Parse(name, schema, Common.TypeExtension.GetElementType(items.GetType())), options, upserting, upserted);
		public int UpsertMany(string name, IEnumerable items, ISchema schema, DataUpsertOptions options, Func<DataUpsertContextBase, bool> upserting = null, Action<DataUpsertContextBase> upserted = null)
		{
			if(string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));

			if(items == null)
				return 0;

			//创建数据访问上下文对象
			var context = this.CreateUpsertContext(name, true, items, schema, options);

			//处理数据访问操作前的回调
			if(upserting != null && upserting(context))
				return context.Count;

			//激发“Upserting”事件，如果被中断则返回
			if(this.OnUpserting(context))
				return context.Count;

			//调用数据访问过滤器前事件
			this.OnFiltering(context);

			//执行数据增改操作
			this.OnUpsert(context);

			//调用数据访问过滤器后事件
			this.OnFiltered(context);

			//激发“Upserted”事件
			this.OnUpserted(context);

			//处理数据访问操作后的回调
			if(upserted != null)
				upserted(context);

			var result = context.Count;

			//处置上下文资源
			context.Dispose();

			//返回最终的结果
			return result;
		}

		public Task<int> UpsertManyAsync<T>(IEnumerable<T> items, CancellationToken cancellation = default) => items == null ? Task.FromResult(0) : this.UpsertManyAsync(this.GetName<T>(), items, string.Empty, null, null, null, cancellation);
		public Task<int> UpsertManyAsync<T>(IEnumerable<T> items, DataUpsertOptions options, CancellationToken cancellation = default) => items == null ? Task.FromResult(0) : this.UpsertManyAsync(this.GetName<T>(), items, string.Empty, options, null, null, cancellation);
		public Task<int> UpsertManyAsync<T>(IEnumerable<T> items, string schema, CancellationToken cancellation = default) => items == null ? Task.FromResult(0) : this.UpsertManyAsync(this.GetName<T>(), items, schema, null, null, null, cancellation);
		public Task<int> UpsertManyAsync<T>(IEnumerable<T> items, string schema, DataUpsertOptions options, Func<DataUpsertContextBase, bool> upserting = null, Action<DataUpsertContextBase> upserted = null, CancellationToken cancellation = default) => items == null ? Task.FromResult(0) : this.UpsertManyAsync(this.GetName<T>(), items, schema, options, upserting, upserted, cancellation);

		public Task<int> UpsertManyAsync<T>(IEnumerable items, CancellationToken cancellation = default) => items == null ? Task.FromResult(0) : this.UpsertManyAsync(this.GetName<T>(), items, string.Empty, null, null, null, cancellation);
		public Task<int> UpsertManyAsync<T>(IEnumerable items, DataUpsertOptions options, CancellationToken cancellation = default) => items == null ? Task.FromResult(0) : this.UpsertManyAsync(this.GetName<T>(), items, string.Empty, options, null, null, cancellation);
		public Task<int> UpsertManyAsync<T>(IEnumerable items, string schema, CancellationToken cancellation = default) => items == null ? Task.FromResult(0) : this.UpsertManyAsync(this.GetName<T>(), items, schema, null, null, null, cancellation);
		public Task<int> UpsertManyAsync<T>(IEnumerable items, string schema, DataUpsertOptions options, Func<DataUpsertContextBase, bool> upserting = null, Action<DataUpsertContextBase> upserted = null, CancellationToken cancellation = default) => items == null ? Task.FromResult(0) : this.UpsertManyAsync(this.GetName<T>(), items, schema, options, upserting, upserted, cancellation);

		public Task<int> UpsertManyAsync(string name, IEnumerable items, CancellationToken cancellation = default) => this.UpsertManyAsync(name, items, string.Empty, null, null, null, cancellation);
		public Task<int> UpsertManyAsync(string name, IEnumerable items, DataUpsertOptions options, CancellationToken cancellation = default) => this.UpsertManyAsync(name, items, string.Empty, options, null, null, cancellation);
		public Task<int> UpsertManyAsync(string name, IEnumerable items, string schema, CancellationToken cancellation = default) => this.UpsertManyAsync(name, items, schema, null, null, null, cancellation);
		public Task<int> UpsertManyAsync(string name, IEnumerable items, string schema, DataUpsertOptions options, Func<DataUpsertContextBase, bool> upserting = null, Action<DataUpsertContextBase> upserted = null, CancellationToken cancellation = default) => this.UpsertManyAsync(name, items, this.Schema.Parse(name, schema, Common.TypeExtension.GetElementType(items.GetType())), options, upserting, upserted, cancellation);
		public async Task<int> UpsertManyAsync(string name, IEnumerable items, ISchema schema, DataUpsertOptions options, Func<DataUpsertContextBase, bool> upserting = null, Action<DataUpsertContextBase> upserted = null, CancellationToken cancellation = default)
		{
			if(string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));

			if(items == null)
				return 0;

			//创建数据访问上下文对象
			var context = this.CreateUpsertContext(name, true, items, schema, options);

			//处理数据访问操作前的回调
			if(upserting != null && upserting(context))
				return context.Count;

			//激发“Upserting”事件，如果被中断则返回
			if(this.OnUpserting(context))
				return context.Count;

			//调用数据访问过滤器前事件
			this.OnFiltering(context);

			//执行数据增改操作
			await this.OnUpsertAsync(context, cancellation);

			//调用数据访问过滤器后事件
			this.OnFiltered(context);

			//激发“Upserted”事件
			this.OnUpserted(context);

			//处理数据访问操作后的回调
			if(upserted != null)
				upserted(context);

			var result = context.Count;

			//处置上下文资源
			context.Dispose();

			//返回最终的结果
			return result;
		}

		protected abstract void OnUpsert(DataUpsertContextBase context);
		protected abstract Task OnUpsertAsync(DataUpsertContextBase context, CancellationToken cancellation);
		#endregion

		#region 更新方法
		public int Update<T>(T data) => data == null ? 0 : this.Update(this.GetName<T>(), data, null, string.Empty, null, null, null);
		public int Update<T>(T data, DataUpdateOptions options) => data == null ? 0 : this.Update(this.GetName<T>(), data, null, string.Empty, options, null, null);
		public int Update<T>(T data, string schema) => data == null ? 0 : this.Update(this.GetName<T>(), data, null, schema, null, null, null);
		public int Update<T>(T data, string schema, DataUpdateOptions options) => data == null ? 0 : this.Update(this.GetName<T>(), data, null, schema, options, null, null);
		public int Update<T>(T data, ICondition criteria) => data == null ? 0 : this.Update(this.GetName<T>(), data, criteria, string.Empty, null, null, null);
		public int Update<T>(T data, ICondition criteria, DataUpdateOptions options) => data == null ? 0 : this.Update(this.GetName<T>(), data, criteria, string.Empty, options, null, null);
		public int Update<T>(T data, ICondition criteria, string schema) => data == null ? 0 : this.Update(this.GetName<T>(), data, criteria, schema, null, null, null);
		public int Update<T>(T data, ICondition criteria, string schema, DataUpdateOptions options, Func<DataUpdateContextBase, bool> updating = null, Action<DataUpdateContextBase> updated = null) => data == null ? 0 : this.Update(this.GetName<T>(), data, criteria, schema, options, updating, updated);

		public int Update<T>(object data) => data == null ? 0 : this.Update(this.GetName<T>(), data, null, string.Empty, null, null, null);
		public int Update<T>(object data, DataUpdateOptions options) => data == null ? 0 : this.Update(this.GetName<T>(), data, null, string.Empty, options, null, null);
		public int Update<T>(object data, string schema) => data == null ? 0 : this.Update(this.GetName<T>(), data, null, schema, null, null, null);
		public int Update<T>(object data, string schema, DataUpdateOptions options) => data == null ? 0 : this.Update(this.GetName<T>(), data, null, schema, options, null, null);
		public int Update<T>(object data, ICondition criteria) => data == null ? 0 : this.Update(this.GetName<T>(), data, criteria, string.Empty, null, null, null);
		public int Update<T>(object data, ICondition criteria, DataUpdateOptions options) => data == null ? 0 : this.Update(this.GetName<T>(), data, criteria, string.Empty, options, null, null);
		public int Update<T>(object data, ICondition criteria, string schema) => data == null ? 0 : this.Update(this.GetName<T>(), data, criteria, schema, null, null, null);
		public int Update<T>(object data, ICondition criteria, string schema, DataUpdateOptions options, Func<DataUpdateContextBase, bool> updating = null, Action<DataUpdateContextBase> updated = null) => data == null ? 0 : this.Update(this.GetName<T>(), data, criteria, schema, options, updating, updated);

		public int Update(string name, object data) => this.Update(name, data, null, string.Empty, null, null, null);
		public int Update(string name, object data, DataUpdateOptions options) => this.Update(name, data, null, string.Empty, options, null, null);
		public int Update(string name, object data, string schema) => this.Update(name, data, null, schema, null, null, null);
		public int Update(string name, object data, string schema, DataUpdateOptions options) => this.Update(name, data, null, schema, options, null, null);
		public int Update(string name, object data, ICondition criteria) => this.Update(name, data, criteria, string.Empty, null, null, null);
		public int Update(string name, object data, ICondition criteria, DataUpdateOptions options) => this.Update(name, data, criteria, string.Empty, options, null, null);
		public int Update(string name, object data, ICondition criteria, string schema) => this.Update(name, data, criteria, schema, null, null, null);
		public int Update(string name, object data, ICondition criteria, string schema, DataUpdateOptions options, Func<DataUpdateContextBase, bool> updating = null, Action<DataUpdateContextBase> updated = null) => this.Update(name, data, criteria, this.Schema.Parse(name, schema, data.GetType()), options, updating, updated);
		public int Update(string name, object data, ICondition criteria, ISchema schema, DataUpdateOptions options, Func<DataUpdateContextBase, bool> updating = null, Action<DataUpdateContextBase> updated = null)
		{
			if(string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));

			if(data == null)
				return 0;

			//创建数据访问上下文对象
			var context = this.CreateUpdateContext(name, false, data, criteria, schema, options);

			//处理数据访问操作前的回调
			if(updating != null && updating(context))
				return context.Count;

			//激发“Updating”事件，如果被中断则返回
			if(this.OnUpdating(context))
				return context.Count;

			//调用数据访问过滤器前事件
			this.OnFiltering(context);

			//执行数据更新操作
			this.OnUpdate(context);

			//调用数据访问过滤器后事件
			this.OnFiltered(context);

			//激发“Updated”事件
			this.OnUpdated(context);

			//处理数据访问操作后的回调
			if(updated != null)
				updated(context);

			var result = context.Count;

			//处置上下文资源
			context.Dispose();

			//返回最终的结果
			return result;
		}

		public Task<int> UpdateAsync<T>(T data, CancellationToken cancellation = default) =>
			data == null ? Task.FromResult(0) : this.UpdateAsync(this.GetName<T>(), data, null, string.Empty, null, null, null, cancellation);
		public Task<int> UpdateAsync<T>(T data, DataUpdateOptions options, CancellationToken cancellation = default) =>
			data == null ? Task.FromResult(0) : this.UpdateAsync(this.GetName<T>(), data, null, string.Empty, options, null, null, cancellation);
		public Task<int> UpdateAsync<T>(T data, string schema, CancellationToken cancellation = default) =>
			data == null ? Task.FromResult(0) : this.UpdateAsync(this.GetName<T>(), data, null, schema, null, null, null, cancellation);
		public Task<int> UpdateAsync<T>(T data, string schema, DataUpdateOptions options, CancellationToken cancellation = default) =>
			data == null ? Task.FromResult(0) : this.UpdateAsync(this.GetName<T>(), data, null, schema, options, null, null, cancellation);
		public Task<int> UpdateAsync<T>(T data, ICondition criteria, CancellationToken cancellation = default) =>
			data == null ? Task.FromResult(0) : this.UpdateAsync(this.GetName<T>(), data, criteria, string.Empty, null, null, null, cancellation);
		public Task<int> UpdateAsync<T>(T data, ICondition criteria, DataUpdateOptions options, CancellationToken cancellation = default) =>
			data == null ? Task.FromResult(0) : this.UpdateAsync(this.GetName<T>(), data, criteria, string.Empty, options, null, null, cancellation);
		public Task<int> UpdateAsync<T>(T data, ICondition criteria, string schema, CancellationToken cancellation = default) =>
			data == null ? Task.FromResult(0) : this.UpdateAsync(this.GetName<T>(), data, criteria, schema, null, null, null, cancellation);
		public Task<int> UpdateAsync<T>(T data, ICondition criteria, string schema, DataUpdateOptions options, Func<DataUpdateContextBase, bool> updating = null, Action<DataUpdateContextBase> updated = null, CancellationToken cancellation = default) =>
			data == null ? Task.FromResult(0) : this.UpdateAsync(this.GetName<T>(), data, criteria, schema, options, updating, updated, cancellation);
		public Task<int> UpdateAsync<T>(object data, CancellationToken cancellation = default) =>
			data == null ? Task.FromResult(0) : this.UpdateAsync(this.GetName<T>(), data, null, string.Empty, null, null, null, cancellation);
		public Task<int> UpdateAsync<T>(object data, DataUpdateOptions options, CancellationToken cancellation = default) =>
			data == null ? Task.FromResult(0) : this.UpdateAsync(this.GetName<T>(), data, null, string.Empty, options, null, null, cancellation);
		public Task<int> UpdateAsync<T>(object data, string schema, CancellationToken cancellation = default) =>
			data == null ? Task.FromResult(0) : this.UpdateAsync(this.GetName<T>(), data, null, schema, null, null, null, cancellation);
		public Task<int> UpdateAsync<T>(object data, string schema, DataUpdateOptions options, CancellationToken cancellation = default) =>
			data == null ? Task.FromResult(0) : this.UpdateAsync(this.GetName<T>(), data, null, schema, options, null, null, cancellation);
		public Task<int> UpdateAsync<T>(object data, ICondition criteria, CancellationToken cancellation = default) =>
			data == null ? Task.FromResult(0) : this.UpdateAsync(this.GetName<T>(), data, criteria, string.Empty, null, null, null, cancellation);
		public Task<int> UpdateAsync<T>(object data, ICondition criteria, DataUpdateOptions options, CancellationToken cancellation = default) =>
			data == null ? Task.FromResult(0) : this.UpdateAsync(this.GetName<T>(), data, criteria, string.Empty, options, null, null, cancellation);
		public Task<int> UpdateAsync<T>(object data, ICondition criteria, string schema, CancellationToken cancellation = default) =>
			data == null ? Task.FromResult(0) : this.UpdateAsync(this.GetName<T>(), data, criteria, schema, null, null, null, cancellation);
		public Task<int> UpdateAsync<T>(object data, ICondition criteria, string schema, DataUpdateOptions options, Func<DataUpdateContextBase, bool> updating = null, Action<DataUpdateContextBase> updated = null, CancellationToken cancellation = default) =>
			data == null ? Task.FromResult(0) : this.UpdateAsync(this.GetName<T>(), data, criteria, schema, options, updating, updated, cancellation);

		public Task<int> UpdateAsync(string name, object data, CancellationToken cancellation = default) =>
			this.UpdateAsync(name, data, null, string.Empty, null, null, null, cancellation);
		public Task<int> UpdateAsync(string name, object data, DataUpdateOptions options, CancellationToken cancellation = default) =>
			this.UpdateAsync(name, data, null, string.Empty, options, null, null, cancellation);
		public Task<int> UpdateAsync(string name, object data, string schema, CancellationToken cancellation = default) =>
			this.UpdateAsync(name, data, null, schema, null, null, null, cancellation);
		public Task<int> UpdateAsync(string name, object data, string schema, DataUpdateOptions options, CancellationToken cancellation = default) =>
			this.UpdateAsync(name, data, null, schema, options, null, null, cancellation);
		public Task<int> UpdateAsync(string name, object data, ICondition criteria, CancellationToken cancellation = default) =>
			this.UpdateAsync(name, data, criteria, string.Empty, null, null, null, cancellation);
		public Task<int> UpdateAsync(string name, object data, ICondition criteria, DataUpdateOptions options, CancellationToken cancellation = default) =>
			this.UpdateAsync(name, data, criteria, string.Empty, options, null, null, cancellation);
		public Task<int> UpdateAsync(string name, object data, ICondition criteria, string schema, CancellationToken cancellation = default) =>
			this.UpdateAsync(name, data, criteria, schema, null, null, null, cancellation);
		public Task<int> UpdateAsync(string name, object data, ICondition criteria, string schema, DataUpdateOptions options, Func<DataUpdateContextBase, bool> updating = null, Action<DataUpdateContextBase> updated = null, CancellationToken cancellation = default) =>
			this.UpdateAsync(name, data, criteria, this.Schema.Parse(name, schema, data.GetType()), options, updating, updated, cancellation);
		public async Task<int> UpdateAsync(string name, object data, ICondition criteria, ISchema schema, DataUpdateOptions options, Func<DataUpdateContextBase, bool> updating = null, Action<DataUpdateContextBase> updated = null, CancellationToken cancellation = default)
		{
			if(string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));

			if(data == null)
				return 0;

			//创建数据访问上下文对象
			var context = this.CreateUpdateContext(name, false, data, criteria, schema, options);

			//处理数据访问操作前的回调
			if(updating != null && updating(context))
				return context.Count;

			//激发“Updating”事件，如果被中断则返回
			if(this.OnUpdating(context))
				return context.Count;

			//调用数据访问过滤器前事件
			this.OnFiltering(context);

			//执行数据更新操作
			await this.OnUpdateAsync(context, cancellation);

			//调用数据访问过滤器后事件
			this.OnFiltered(context);

			//激发“Updated”事件
			this.OnUpdated(context);

			//处理数据访问操作后的回调
			if(updated != null)
				updated(context);

			var result = context.Count;

			//处置上下文资源
			context.Dispose();

			//返回最终的结果
			return result;
		}

		public int UpdateMany<T>(IEnumerable<T> items) => this.UpdateMany(this.GetName<T>(), items, string.Empty, null, null, null);
		public int UpdateMany<T>(IEnumerable<T> items, DataUpdateOptions options) => this.UpdateMany(this.GetName<T>(), items, string.Empty, options, null, null);
		public int UpdateMany<T>(IEnumerable<T> items, string schema) => this.UpdateMany(this.GetName<T>(), items, schema, null, null, null);
		public int UpdateMany<T>(IEnumerable<T> items, string schema, DataUpdateOptions options, Func<DataUpdateContextBase, bool> updating = null, Action<DataUpdateContextBase> updated = null) => this.UpdateMany(this.GetName<T>(), items, schema, options, updating, updated);
		public int UpdateMany<T>(IEnumerable items) => this.UpdateMany(this.GetName<T>(), items, string.Empty, null, null, null);
		public int UpdateMany<T>(IEnumerable items, DataUpdateOptions options) => this.UpdateMany(this.GetName<T>(), items, string.Empty, options, null, null);
		public int UpdateMany<T>(IEnumerable items, string schema) => this.UpdateMany(this.GetName<T>(), items, schema, null, null, null);
		public int UpdateMany<T>(IEnumerable items, string schema, DataUpdateOptions options, Func<DataUpdateContextBase, bool> updating = null, Action<DataUpdateContextBase> updated = null) => this.UpdateMany(this.GetName<T>(), items, schema, options, updating, updated);

		public int UpdateMany(string name, IEnumerable items) => this.UpdateMany(name, items, string.Empty, null, null, null);
		public int UpdateMany(string name, IEnumerable items, DataUpdateOptions options) => this.UpdateMany(name, items, string.Empty, options, null, null);
		public int UpdateMany(string name, IEnumerable items, string schema) => this.UpdateMany(name, items, schema, null, null, null);
		public int UpdateMany(string name, IEnumerable items, string schema, DataUpdateOptions options, Func<DataUpdateContextBase, bool> updating = null, Action<DataUpdateContextBase> updated = null) => this.UpdateMany(name, items, this.Schema.Parse(name, schema, Common.TypeExtension.GetElementType(items.GetType())), options, updating, updated);
		public int UpdateMany(string name, IEnumerable items, ISchema schema, DataUpdateOptions options, Func<DataUpdateContextBase, bool> updating = null, Action<DataUpdateContextBase> updated = null)
		{
			if(string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));

			if(items == null)
				return 0;

			//创建数据访问上下文对象
			var context = this.CreateUpdateContext(name, true, items, null, schema, options);

			//处理数据访问操作前的回调
			if(updating != null && updating(context))
				return context.Count;

			//激发“Updating”事件，如果被中断则返回
			if(this.OnUpdating(context))
				return context.Count;

			//调用数据访问过滤器前事件
			this.OnFiltering(context);

			//执行数据更新操作
			this.OnUpdate(context);

			//调用数据访问过滤器后事件
			this.OnFiltered(context);

			//激发“Updated”事件
			this.OnUpdated(context);

			//处理数据访问操作后的回调
			if(updated != null)
				updated(context);

			var result = context.Count;

			//处置上下文资源
			context.Dispose();

			//返回最终的结果
			return result;
		}

		public Task<int> UpdateManyAsync<T>(IEnumerable<T> items, CancellationToken cancellation = default) =>
			this.UpdateManyAsync(this.GetName<T>(), items, string.Empty, null, null, null, cancellation);
		public Task<int> UpdateManyAsync<T>(IEnumerable<T> items, DataUpdateOptions options, CancellationToken cancellation = default) =>
			this.UpdateManyAsync(this.GetName<T>(), items, string.Empty, options, null, null, cancellation);
		public Task<int> UpdateManyAsync<T>(IEnumerable<T> items, string schema, CancellationToken cancellation = default) =>
			this.UpdateManyAsync(this.GetName<T>(), items, schema, null, null, null, cancellation);
		public Task<int> UpdateManyAsync<T>(IEnumerable<T> items, string schema, DataUpdateOptions options, Func<DataUpdateContextBase, bool> updating = null, Action<DataUpdateContextBase> updated = null, CancellationToken cancellation = default) =>
			this.UpdateManyAsync(this.GetName<T>(), items, schema, options, updating, updated, cancellation);
		public Task<int> UpdateManyAsync<T>(IEnumerable items, CancellationToken cancellation = default) =>
			this.UpdateManyAsync(this.GetName<T>(), items, string.Empty, null, null, null, cancellation);
		public Task<int> UpdateManyAsync<T>(IEnumerable items, DataUpdateOptions options, CancellationToken cancellation = default) =>
			this.UpdateManyAsync(this.GetName<T>(), items, string.Empty, options, null, null, cancellation);
		public Task<int> UpdateManyAsync<T>(IEnumerable items, string schema, CancellationToken cancellation = default) =>
			this.UpdateManyAsync(this.GetName<T>(), items, schema, null, null, null, cancellation);
		public Task<int> UpdateManyAsync<T>(IEnumerable items, string schema, DataUpdateOptions options, Func<DataUpdateContextBase, bool> updating = null, Action<DataUpdateContextBase> updated = null, CancellationToken cancellation = default) =>
			this.UpdateManyAsync(this.GetName<T>(), items, schema, options, updating, updated, cancellation);

		public Task<int> UpdateManyAsync(string name, IEnumerable items, CancellationToken cancellation = default) =>
			this.UpdateManyAsync(name, items, string.Empty, null, null, null, cancellation);
		public Task<int> UpdateManyAsync(string name, IEnumerable items, DataUpdateOptions options, CancellationToken cancellation = default) =>
			this.UpdateManyAsync(name, items, string.Empty, options, null, null, cancellation);
		public Task<int> UpdateManyAsync(string name, IEnumerable items, string schema, CancellationToken cancellation = default) =>
			this.UpdateManyAsync(name, items, schema, null, null, null, cancellation);
		public Task<int> UpdateManyAsync(string name, IEnumerable items, string schema, DataUpdateOptions options, Func<DataUpdateContextBase, bool> updating = null, Action<DataUpdateContextBase> updated = null, CancellationToken cancellation = default) =>
			this.UpdateManyAsync(name, items, this.Schema.Parse(name, schema, Common.TypeExtension.GetElementType(items.GetType())), options, updating, updated, cancellation);
		public async Task<int> UpdateManyAsync(string name, IEnumerable items, ISchema schema, DataUpdateOptions options, Func<DataUpdateContextBase, bool> updating = null, Action<DataUpdateContextBase> updated = null, CancellationToken cancellation = default)
		{
			if(string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));

			if(items == null)
				return 0;

			//创建数据访问上下文对象
			var context = this.CreateUpdateContext(name, true, items, null, schema, options);

			//处理数据访问操作前的回调
			if(updating != null && updating(context))
				return context.Count;

			//激发“Updating”事件，如果被中断则返回
			if(this.OnUpdating(context))
				return context.Count;

			//调用数据访问过滤器前事件
			this.OnFiltering(context);

			//执行数据更新操作
			await this.OnUpdateAsync(context, cancellation);

			//调用数据访问过滤器后事件
			this.OnFiltered(context);

			//激发“Updated”事件
			this.OnUpdated(context);

			//处理数据访问操作后的回调
			if(updated != null)
				updated(context);

			var result = context.Count;

			//处置上下文资源
			context.Dispose();

			//返回最终的结果
			return result;
		}

		protected abstract void OnUpdate(DataUpdateContextBase context);
		protected abstract Task OnUpdateAsync(DataUpdateContextBase context, CancellationToken cancellation);
		#endregion

		#region 普通查询
		public IEnumerable<T> Select<T>(DataSelectOptions options = null, params Sorting[] sortings) =>
			this.Select<T>(this.GetName<T>(), null, string.Empty, null, options, sortings, null, null);
		public IEnumerable<T> Select<T>(ICondition criteria, params Sorting[] sortings) =>
			this.Select<T>(this.GetName<T>(), criteria, string.Empty, null, null, sortings, null, null);
		public IEnumerable<T> Select<T>(ICondition criteria, DataSelectOptions options, params Sorting[] sortings) =>
			this.Select<T>(this.GetName<T>(), criteria, string.Empty, null, options, sortings, null, null);
		public IEnumerable<T> Select<T>(ICondition criteria, Paging paging, params Sorting[] sortings) =>
			this.Select<T>(this.GetName<T>(), criteria, string.Empty, paging, null, sortings, null, null);
		public IEnumerable<T> Select<T>(ICondition criteria, Paging paging, DataSelectOptions options, params Sorting[] sortings) =>
			this.Select<T>(this.GetName<T>(), criteria, string.Empty, paging, options, sortings, null, null);
		public IEnumerable<T> Select<T>(ICondition criteria, string schema, params Sorting[] sortings) =>
			this.Select<T>(this.GetName<T>(), criteria, schema, null, null, sortings, null, null);
		public IEnumerable<T> Select<T>(ICondition criteria, string schema, DataSelectOptions options, params Sorting[] sortings) =>
			this.Select<T>(this.GetName<T>(), criteria, schema, null, options, sortings, null, null);
		public IEnumerable<T> Select<T>(ICondition criteria, string schema, Paging paging, params Sorting[] sortings) =>
			this.Select<T>(this.GetName<T>(), criteria, schema, paging, null, sortings, null, null);
		public IEnumerable<T> Select<T>(ICondition criteria, string schema, Paging paging, DataSelectOptions options, params Sorting[] sortings) =>
			this.Select<T>(this.GetName<T>(), criteria, schema, paging, options, sortings, null, null);
		public IEnumerable<T> Select<T>(ICondition criteria, string schema, Paging paging, DataSelectOptions options, Sorting[] sortings, Func<DataSelectContextBase, bool> selecting, Action<DataSelectContextBase> selected) =>
			this.Select<T>(this.GetName<T>(), criteria, schema, paging, options, sortings, selecting, selected);

		public IEnumerable<T> Select<T>(string name, DataSelectOptions options = null, params Sorting[] sortings) =>
			this.Select<T>(name, null, string.Empty, null, options, sortings, null, null);
		public IEnumerable<T> Select<T>(string name, ICondition criteria, params Sorting[] sortings) =>
			this.Select<T>(name, criteria, string.Empty, null, null, sortings, null, null);
		public IEnumerable<T> Select<T>(string name, ICondition criteria, DataSelectOptions options, params Sorting[] sortings) =>
			this.Select<T>(name, criteria, string.Empty, null, options, sortings, null, null);
		public IEnumerable<T> Select<T>(string name, ICondition criteria, Paging paging, params Sorting[] sortings) =>
			this.Select<T>(name, criteria, string.Empty, paging, null, sortings, null, null);
		public IEnumerable<T> Select<T>(string name, ICondition criteria, Paging paging, DataSelectOptions options, params Sorting[] sortings) =>
			this.Select<T>(name, criteria, string.Empty, paging, options, sortings, null, null);
		public IEnumerable<T> Select<T>(string name, ICondition criteria, string schema, params Sorting[] sortings) =>
			this.Select<T>(name, criteria, schema, null, null, sortings, null, null);
		public IEnumerable<T> Select<T>(string name, ICondition criteria, string schema, DataSelectOptions options, params Sorting[] sortings) =>
			this.Select<T>(name, criteria, schema, null, options, sortings, null, null);
		public IEnumerable<T> Select<T>(string name, ICondition criteria, string schema, Paging paging, params Sorting[] sortings) =>
			this.Select<T>(name, criteria, schema, paging, null, sortings, null, null);
		public IEnumerable<T> Select<T>(string name, ICondition criteria, string schema, Paging paging, DataSelectOptions options, params Sorting[] sortings) =>
			this.Select<T>(name, criteria, schema, paging, options, sortings, null, null);
		public IEnumerable<T> Select<T>(string name, ICondition criteria, string schema, Paging paging, DataSelectOptions options, Sorting[] sortings, Func<DataSelectContextBase, bool> selecting, Action<DataSelectContextBase> selected) =>
			this.Select<T>(name, criteria, this.Schema.Parse(name, schema, typeof(T)), paging, options, sortings, selecting, selected);
		public IEnumerable<T> Select<T>(string name, ICondition criteria, ISchema schema, Paging paging, DataSelectOptions options, Sorting[] sortings, Func<DataSelectContextBase, bool> selecting, Action<DataSelectContextBase> selected)
		{
			if(string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));

			//创建数据访问上下文对象
			var context = this.CreateSelectContext(name, typeof(T), criteria, null, schema, paging, sortings, options);

			//执行查询方法
			return this.Select<T>(context, selecting, selected);
		}

		public Task<IEnumerable<T>> SelectAsync<T>(CancellationToken cancellation = default) =>
			this.SelectAsync<T>(this.GetName<T>(), null, string.Empty, null, null, null, null, null, cancellation);
		public Task<IEnumerable<T>> SelectAsync<T>(DataSelectOptions options, CancellationToken cancellation = default) =>
			this.SelectAsync<T>(this.GetName<T>(), null, string.Empty, null, options, null, null, null, cancellation);
		public Task<IEnumerable<T>> SelectAsync<T>(DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) =>
			this.SelectAsync<T>(this.GetName<T>(), null, string.Empty, null, options, sortings, null, null, cancellation);
		public Task<IEnumerable<T>> SelectAsync<T>(ICondition criteria, CancellationToken cancellation = default) =>
			this.SelectAsync<T>(this.GetName<T>(), criteria, string.Empty, null, null, null, null, null, cancellation);
		public Task<IEnumerable<T>> SelectAsync<T>(ICondition criteria, Sorting[] sortings, CancellationToken cancellation = default) =>
			this.SelectAsync<T>(this.GetName<T>(), criteria, string.Empty, null, null, sortings, null, null, cancellation);
		public Task<IEnumerable<T>> SelectAsync<T>(ICondition criteria, DataSelectOptions options, CancellationToken cancellation = default) =>
			this.SelectAsync<T>(this.GetName<T>(), criteria, string.Empty, null, options, null, null, null, cancellation);
		public Task<IEnumerable<T>> SelectAsync<T>(ICondition criteria, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) =>
			this.SelectAsync<T>(this.GetName<T>(), criteria, string.Empty, null, options, sortings, null, null, cancellation);
		public Task<IEnumerable<T>> SelectAsync<T>(ICondition criteria, Paging paging, CancellationToken cancellation = default) =>
			this.SelectAsync<T>(this.GetName<T>(), criteria, string.Empty, paging, null, null, null, null, cancellation);
		public Task<IEnumerable<T>> SelectAsync<T>(ICondition criteria, Paging paging, Sorting[] sortings, CancellationToken cancellation = default) =>
			this.SelectAsync<T>(this.GetName<T>(), criteria, string.Empty, paging, null, sortings, null, null, cancellation);
		public Task<IEnumerable<T>> SelectAsync<T>(ICondition criteria, Paging paging, DataSelectOptions options, CancellationToken cancellation = default) =>
			this.SelectAsync<T>(this.GetName<T>(), criteria, string.Empty, paging, options, null, null, null, cancellation);
		public Task<IEnumerable<T>> SelectAsync<T>(ICondition criteria, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) =>
			this.SelectAsync<T>(this.GetName<T>(), criteria, string.Empty, paging, options, sortings, null, null, cancellation);
		public Task<IEnumerable<T>> SelectAsync<T>(ICondition criteria, string schema, CancellationToken cancellation = default) =>
			this.SelectAsync<T>(this.GetName<T>(), criteria, schema, null, null, null, null, null, cancellation);
		public Task<IEnumerable<T>> SelectAsync<T>(ICondition criteria, string schema, Sorting[] sortings, CancellationToken cancellation = default) =>
			this.SelectAsync<T>(this.GetName<T>(), criteria, schema, null, null, sortings, null, null, cancellation);
		public Task<IEnumerable<T>> SelectAsync<T>(ICondition criteria, string schema, DataSelectOptions options, CancellationToken cancellation = default) =>
			this.SelectAsync<T>(this.GetName<T>(), criteria, schema, null, options, null, null, null, cancellation);
		public Task<IEnumerable<T>> SelectAsync<T>(ICondition criteria, string schema, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) =>
			this.SelectAsync<T>(this.GetName<T>(), criteria, schema, null, options, sortings, null, null, cancellation);
		public Task<IEnumerable<T>> SelectAsync<T>(ICondition criteria, string schema, Paging paging, CancellationToken cancellation = default) =>
			this.SelectAsync<T>(this.GetName<T>(), criteria, schema, paging, null, null, null, null, cancellation);
		public Task<IEnumerable<T>> SelectAsync<T>(ICondition criteria, string schema, Paging paging, Sorting[] sortings, CancellationToken cancellation = default) =>
			this.SelectAsync<T>(this.GetName<T>(), criteria, schema, paging, null, sortings, null, null, cancellation);
		public Task<IEnumerable<T>> SelectAsync<T>(ICondition criteria, string schema, Paging paging, DataSelectOptions options, CancellationToken cancellation = default) =>
			this.SelectAsync<T>(this.GetName<T>(), criteria, schema, paging, options, null, null, null, cancellation);
		public Task<IEnumerable<T>> SelectAsync<T>(ICondition criteria, string schema, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) =>
			this.SelectAsync<T>(this.GetName<T>(), criteria, schema, paging, options, sortings, null, null, cancellation);
		public Task<IEnumerable<T>> SelectAsync<T>(ICondition criteria, string schema, Paging paging, DataSelectOptions options, Sorting[] sortings, Func<DataSelectContextBase, bool> selecting, Action<DataSelectContextBase> selected, CancellationToken cancellation = default) =>
			this.SelectAsync<T>(this.GetName<T>(), criteria, schema, paging, options, sortings, selecting, selected, cancellation);

		public Task<IEnumerable<T>> SelectAsync<T>(string name, CancellationToken cancellation = default) =>
			this.SelectAsync<T>(name, null, string.Empty, null, null, null, null, null, cancellation);
		public Task<IEnumerable<T>> SelectAsync<T>(string name, DataSelectOptions options, CancellationToken cancellation = default) =>
			this.SelectAsync<T>(name, null, string.Empty, null, options, null, null, null, cancellation);
		public Task<IEnumerable<T>> SelectAsync<T>(string name, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) =>
			this.SelectAsync<T>(name, null, string.Empty, null, options, sortings, null, null, cancellation);
		public Task<IEnumerable<T>> SelectAsync<T>(string name, ICondition criteria, CancellationToken cancellation = default) =>
			this.SelectAsync<T>(name, criteria, string.Empty, null, null, null, null, null, cancellation);
		public Task<IEnumerable<T>> SelectAsync<T>(string name, ICondition criteria, Sorting[] sortings, CancellationToken cancellation = default) =>
			this.SelectAsync<T>(name, criteria, string.Empty, null, null, sortings, null, null, cancellation);
		public Task<IEnumerable<T>> SelectAsync<T>(string name, ICondition criteria, DataSelectOptions options, CancellationToken cancellation = default) =>
			this.SelectAsync<T>(name, criteria, string.Empty, null, options, null, null, null, cancellation);
		public Task<IEnumerable<T>> SelectAsync<T>(string name, ICondition criteria, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) =>
			this.SelectAsync<T>(name, criteria, string.Empty, null, options, sortings, null, null, cancellation);
		public Task<IEnumerable<T>> SelectAsync<T>(string name, ICondition criteria, Paging paging, CancellationToken cancellation = default) =>
			this.SelectAsync<T>(name, criteria, string.Empty, paging, null, null, null, null, cancellation);
		public Task<IEnumerable<T>> SelectAsync<T>(string name, ICondition criteria, Paging paging, Sorting[] sortings, CancellationToken cancellation = default) =>
			this.SelectAsync<T>(name, criteria, string.Empty, paging, null, sortings, null, null, cancellation);
		public Task<IEnumerable<T>> SelectAsync<T>(string name, ICondition criteria, Paging paging, DataSelectOptions options, CancellationToken cancellation = default) =>
			this.SelectAsync<T>(name, criteria, string.Empty, paging, options, null, null, null, cancellation);
		public Task<IEnumerable<T>> SelectAsync<T>(string name, ICondition criteria, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) =>
			this.SelectAsync<T>(name, criteria, string.Empty, paging, options, sortings, null, null, cancellation);
		public Task<IEnumerable<T>> SelectAsync<T>(string name, ICondition criteria, string schema, CancellationToken cancellation = default) =>
			this.SelectAsync<T>(name, criteria, schema, null, null, null, null, null, cancellation);
		public Task<IEnumerable<T>> SelectAsync<T>(string name, ICondition criteria, string schema, Sorting[] sortings, CancellationToken cancellation = default) =>
			this.SelectAsync<T>(name, criteria, schema, null, null, sortings, null, null, cancellation);
		public Task<IEnumerable<T>> SelectAsync<T>(string name, ICondition criteria, string schema, DataSelectOptions options, CancellationToken cancellation = default) =>
			this.SelectAsync<T>(name, criteria, schema, null, options, null, null, null, cancellation);
		public Task<IEnumerable<T>> SelectAsync<T>(string name, ICondition criteria, string schema, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) =>
			this.SelectAsync<T>(name, criteria, schema, null, options, sortings, null, null, cancellation);
		public Task<IEnumerable<T>> SelectAsync<T>(string name, ICondition criteria, string schema, Paging paging, CancellationToken cancellation = default) =>
			this.SelectAsync<T>(name, criteria, schema, paging, null, null, null, null, cancellation);
		public Task<IEnumerable<T>> SelectAsync<T>(string name, ICondition criteria, string schema, Paging paging, Sorting[] sortings, CancellationToken cancellation = default) =>
			this.SelectAsync<T>(name, criteria, schema, paging, null, sortings, null, null, cancellation);
		public Task<IEnumerable<T>> SelectAsync<T>(string name, ICondition criteria, string schema, Paging paging, DataSelectOptions options, CancellationToken cancellation = default) =>
			this.SelectAsync<T>(name, criteria, schema, paging, options, null, null, null, cancellation);
		public Task<IEnumerable<T>> SelectAsync<T>(string name, ICondition criteria, string schema, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) =>
			this.SelectAsync<T>(name, criteria, schema, paging, options, sortings, null, null, cancellation);
		public Task<IEnumerable<T>> SelectAsync<T>(string name, ICondition criteria, string schema, Paging paging, DataSelectOptions options, Sorting[] sortings, Func<DataSelectContextBase, bool> selecting, Action<DataSelectContextBase> selected, CancellationToken cancellation = default) =>
			 this.SelectAsync<T>(name, criteria, this.Schema.Parse(name, schema, typeof(T)), paging, options, sortings, selecting, selected, cancellation);
		public Task<IEnumerable<T>> SelectAsync<T>(string name, ICondition criteria, ISchema schema, Paging paging, DataSelectOptions options, Sorting[] sortings, Func<DataSelectContextBase, bool> selecting, Action<DataSelectContextBase> selected, CancellationToken cancellation = default)
		{
			if(string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));

			//创建数据访问上下文对象
			var context = this.CreateSelectContext(name, typeof(T), criteria, null, schema, paging, sortings, options);

			//执行查询方法
			return this.SelectAsync<T>(context, selecting, selected, cancellation);
		}
		#endregion

		#region 分组查询
		public IEnumerable<T> Select<T>(string name, Grouping grouping, params Sorting[] sortings) =>
			this.Select<T>(name, grouping, null, string.Empty, null, null, sortings, null, null);
		public IEnumerable<T> Select<T>(string name, Grouping grouping, DataSelectOptions options, params Sorting[] sortings) =>
			this.Select<T>(name, grouping, null, string.Empty, null, options, sortings, null, null);
		public IEnumerable<T> Select<T>(string name, Grouping grouping, Paging paging, DataSelectOptions options = null, params Sorting[] sortings) =>
			this.Select<T>(name, grouping, null, string.Empty, paging, options, sortings, null, null);
		public IEnumerable<T> Select<T>(string name, Grouping grouping, string schema, params Sorting[] sortings) =>
			this.Select<T>(name, grouping, null, schema, null, null, sortings, null, null);
		public IEnumerable<T> Select<T>(string name, Grouping grouping, string schema, DataSelectOptions options, params Sorting[] sortings) =>
			this.Select<T>(name, grouping, null, schema, null, options, sortings, null, null);
		public IEnumerable<T> Select<T>(string name, Grouping grouping, string schema, Paging paging, DataSelectOptions options = null, params Sorting[] sortings) =>
			this.Select<T>(name, grouping, null, schema, paging, options, sortings, null, null);
		public IEnumerable<T> Select<T>(string name, Grouping grouping, ICondition criteria, Paging paging, params Sorting[] sortings) =>
			this.Select<T>(name, grouping, criteria, (ISchema)null, paging, null, sortings, null, null);
		public IEnumerable<T> Select<T>(string name, Grouping grouping, ICondition criteria, string schema, params Sorting[] sortings) =>
			this.Select<T>(name, grouping, criteria, schema, null, null, sortings, null, null);
		public IEnumerable<T> Select<T>(string name, Grouping grouping, ICondition criteria, string schema, DataSelectOptions options, params Sorting[] sortings) =>
			this.Select<T>(name, grouping, criteria, schema, null, options, sortings, null, null);
		public IEnumerable<T> Select<T>(string name, Grouping grouping, ICondition criteria, string schema, Paging paging, DataSelectOptions options = null, params Sorting[] sortings) =>
			this.Select<T>(name, grouping, criteria, schema, paging, options, sortings, null, null);
		public IEnumerable<T> Select<T>(string name, Grouping grouping, ICondition criteria, string schema, Paging paging, DataSelectOptions options, Sorting[] sortings, Func<DataSelectContextBase, bool> selecting, Action<DataSelectContextBase> selected) =>
			this.Select<T>(name, grouping, criteria, string.IsNullOrWhiteSpace(schema) ? null : this.Schema.Parse(name, schema, typeof(T)), paging, options, sortings, selecting, selected);
		public IEnumerable<T> Select<T>(string name, Grouping grouping, ICondition criteria, ISchema schema, Paging paging, DataSelectOptions options, Sorting[] sortings, Func<DataSelectContextBase, bool> selecting, Action<DataSelectContextBase> selected)
		{
			if(string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));

			//创建数据访问上下文对象
			var context = this.CreateSelectContext(name, typeof(T), criteria, grouping, schema, paging, sortings, options);

			//执行查询方法
			return this.Select<T>(context, selecting, selected);
		}

		public Task<IEnumerable<T>> SelectAsync<T>(string name, Grouping grouping, CancellationToken cancellation = default) =>
			this.SelectAsync<T>(name, grouping, null, string.Empty, null, null, null, null, null, cancellation);
		public Task<IEnumerable<T>> SelectAsync<T>(string name, Grouping grouping, Sorting[] sortings, CancellationToken cancellation = default) =>
			this.SelectAsync<T>(name, grouping, null, string.Empty, null, null, sortings, null, null, cancellation);
		public Task<IEnumerable<T>> SelectAsync<T>(string name, Grouping grouping, DataSelectOptions options, CancellationToken cancellation = default) =>
			this.SelectAsync<T>(name, grouping, null, string.Empty, null, options, null, null, null, cancellation);
		public Task<IEnumerable<T>> SelectAsync<T>(string name, Grouping grouping, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) =>
			this.SelectAsync<T>(name, grouping, null, string.Empty, null, options, sortings, null, null, cancellation);
		public Task<IEnumerable<T>> SelectAsync<T>(string name, Grouping grouping, Paging paging, CancellationToken cancellation = default) =>
			this.SelectAsync<T>(name, grouping, null, string.Empty, paging, null, null, null, null, cancellation);
		public Task<IEnumerable<T>> SelectAsync<T>(string name, Grouping grouping, Paging paging, DataSelectOptions options, CancellationToken cancellation = default) =>
			this.SelectAsync<T>(name, grouping, null, string.Empty, paging, options, null, null, null, cancellation);
		public Task<IEnumerable<T>> SelectAsync<T>(string name, Grouping grouping, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) =>
			this.SelectAsync<T>(name, grouping, null, string.Empty, paging, options, sortings, null, null, cancellation);
		public Task<IEnumerable<T>> SelectAsync<T>(string name, Grouping grouping, string schema, CancellationToken cancellation = default) =>
			this.SelectAsync<T>(name, grouping, null, schema, null, null, null, null, null, cancellation);
		public Task<IEnumerable<T>> SelectAsync<T>(string name, Grouping grouping, string schema, Sorting[] sortings, CancellationToken cancellation = default) =>
			this.SelectAsync<T>(name, grouping, null, schema, null, null, sortings, null, null, cancellation);
		public Task<IEnumerable<T>> SelectAsync<T>(string name, Grouping grouping, string schema, DataSelectOptions options, CancellationToken cancellation = default) =>
			this.SelectAsync<T>(name, grouping, null, schema, null, options, null, null, null, cancellation);
		public Task<IEnumerable<T>> SelectAsync<T>(string name, Grouping grouping, string schema, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) =>
			this.SelectAsync<T>(name, grouping, null, schema, null, options, sortings, null, null, cancellation);
		public Task<IEnumerable<T>> SelectAsync<T>(string name, Grouping grouping, string schema, Paging paging, CancellationToken cancellation = default) =>
			this.SelectAsync<T>(name, grouping, null, schema, paging, null, null, null, null, cancellation);
		public Task<IEnumerable<T>> SelectAsync<T>(string name, Grouping grouping, string schema, Paging paging, DataSelectOptions options, CancellationToken cancellation = default) =>
			this.SelectAsync<T>(name, grouping, null, schema, paging, options, null, null, null, cancellation);
		public Task<IEnumerable<T>> SelectAsync<T>(string name, Grouping grouping, string schema, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) =>
			  this.SelectAsync<T>(name, grouping, null, schema, paging, options, sortings, null, null, cancellation);
		public Task<IEnumerable<T>> SelectAsync<T>(string name, Grouping grouping, ICondition criteria, CancellationToken cancellation = default) =>
			this.SelectAsync<T>(name, grouping, criteria, (ISchema)null, null, null, null, null, null, cancellation);
		public Task<IEnumerable<T>> SelectAsync<T>(string name, Grouping grouping, ICondition criteria, Paging paging, CancellationToken cancellation = default) =>
			this.SelectAsync<T>(name, grouping, criteria, (ISchema)null, paging, null, null, null, null, cancellation);
		public Task<IEnumerable<T>> SelectAsync<T>(string name, Grouping grouping, ICondition criteria, Paging paging, Sorting[] sortings, CancellationToken cancellation = default) =>
			this.SelectAsync<T>(name, grouping, criteria, (ISchema)null, paging, null, sortings, null, null, cancellation);
		public Task<IEnumerable<T>> SelectAsync<T>(string name, Grouping grouping, ICondition criteria, string schema, CancellationToken cancellation = default) =>
			this.SelectAsync<T>(name, grouping, criteria, schema, null, null, null, null, null, cancellation);
		public Task<IEnumerable<T>> SelectAsync<T>(string name, Grouping grouping, ICondition criteria, string schema, Sorting[] sortings, CancellationToken cancellation = default) =>
			this.SelectAsync<T>(name, grouping, criteria, schema, null, null, sortings, null, null, cancellation);
		public Task<IEnumerable<T>> SelectAsync<T>(string name, Grouping grouping, ICondition criteria, string schema, DataSelectOptions options, CancellationToken cancellation = default) =>
			this.SelectAsync<T>(name, grouping, criteria, schema, null, options, null, null, null, cancellation);
		public Task<IEnumerable<T>> SelectAsync<T>(string name, Grouping grouping, ICondition criteria, string schema, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) =>
			this.SelectAsync<T>(name, grouping, criteria, schema, null, options, sortings, null, null, cancellation);
		public Task<IEnumerable<T>> SelectAsync<T>(string name, Grouping grouping, ICondition criteria, string schema, Paging paging, DataSelectOptions options, CancellationToken cancellation = default) =>
			this.SelectAsync<T>(name, grouping, criteria, schema, paging, options, null, null, null, cancellation);
		public Task<IEnumerable<T>> SelectAsync<T>(string name, Grouping grouping, ICondition criteria, string schema, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) =>
			this.SelectAsync<T>(name, grouping, criteria, schema, paging, options, sortings, null, null, cancellation);
		public Task<IEnumerable<T>> SelectAsync<T>(string name, Grouping grouping, ICondition criteria, string schema, Paging paging, DataSelectOptions options, Sorting[] sortings, Func<DataSelectContextBase, bool> selecting, Action<DataSelectContextBase> selected, CancellationToken cancellation = default) =>
			this.SelectAsync<T>(name, grouping, criteria, string.IsNullOrWhiteSpace(schema) ? null : this.Schema.Parse(name, schema, typeof(T)), paging, options, sortings, selecting, selected, cancellation);
		public Task<IEnumerable<T>> SelectAsync<T>(string name, Grouping grouping, ICondition criteria, ISchema schema, Paging paging, DataSelectOptions options, Sorting[] sortings, Func<DataSelectContextBase, bool> selecting, Action<DataSelectContextBase> selected, CancellationToken cancellation = default)
		{
			if(string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));

			//创建数据访问上下文对象
			var context = this.CreateSelectContext(name, typeof(T), criteria, grouping, schema, paging, sortings, options);

			//执行查询方法
			return this.SelectAsync<T>(context, selecting, selected, cancellation);
		}
		#endregion

		#region 查询处理
		private IEnumerable<T> Select<T>(DataSelectContextBase context, Func<DataSelectContextBase, bool> selecting, Action<DataSelectContextBase> selected)
		{
			//处理数据访问操作前的回调
			if(selecting != null && selecting(context))
				return context.Result as IEnumerable<T>;

			//激发“Selecting”事件，如果被中断则返回
			if(this.OnSelecting(context))
				return context.Result as IEnumerable<T>;

			//调用数据访问过滤器前事件
			this.OnFiltering(context);

			//执行数据查询操作
			this.OnSelect(context);

			//调用数据访问过滤器后事件
			this.OnFiltered(context);

			//激发“Selected”事件
			this.OnSelected(context);

			//处理数据访问操作后的回调
			if(selected != null)
				selected(context);

			var result = ToEnumerable<T>(context.Result);

			//处置上下文资源
			context.Dispose();

			//返回最终的结果
			return result;
		}

		private async Task<IEnumerable<T>> SelectAsync<T>(DataSelectContextBase context, Func<DataSelectContextBase, bool> selecting, Action<DataSelectContextBase> selected, CancellationToken cancellation)
		{
			//处理数据访问操作前的回调
			if(selecting != null && selecting(context))
				return context.Result as IEnumerable<T>;

			//激发“Selecting”事件，如果被中断则返回
			if(this.OnSelecting(context))
				return context.Result as IEnumerable<T>;

			//调用数据访问过滤器前事件
			this.OnFiltering(context);

			//执行数据查询操作
			await this.OnSelectAsync(context, cancellation);

			//调用数据访问过滤器后事件
			this.OnFiltered(context);

			//激发“Selected”事件
			this.OnSelected(context);

			//处理数据访问操作后的回调
			if(selected != null)
				selected(context);

			var result = ToEnumerable<T>(context.Result);

			//处置上下文资源
			context.Dispose();

			//返回最终的结果
			return result;
		}

		protected abstract void OnSelect(DataSelectContextBase context);
		protected abstract Task OnSelectAsync(DataSelectContextBase context, CancellationToken cancellation);
		#endregion

		#region 虚拟方法
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private string GetName<T>() => this.GetName(typeof(T));
		protected virtual string GetName(Type type)
		{
			var name = _naming.Get(type);

			if(string.IsNullOrEmpty(name))
				throw new InvalidOperationException($"Missing data access name mapping of the '{type.FullName}' type.");

			return name;
		}

		protected virtual Common.ISequence CreateSequence()
		{
			var sequence = this.SequenceProvider.GetService(this.Name);

			if(sequence == null && !string.IsNullOrEmpty(this.Name))
				sequence = this.SequenceProvider.GetService(string.Empty);

			return sequence;
		}

		protected virtual void OnFiltering(IDataAccessContextBase context) => _filters.InvokeFiltering(context);
		protected virtual void OnFiltered(IDataAccessContextBase context) => _filters.InvokeFiltered(context);
		#endregion

		#region 抽象方法
		protected abstract ISchemaParser CreateSchema();
		protected abstract DataExecuteContextBase CreateExecuteContext(string name, bool isScalar, Type resultType, IDictionary<string, object> inParameters, IDataExecuteOptions options);
		protected abstract DataExistContextBase CreateExistContext(string name, ICondition criteria, IDataExistsOptions options);
		protected abstract DataAggregateContextBase CreateAggregateContext(string name, DataAggregate aggregate, ICondition criteria, IDataAggregateOptions options);
		protected abstract DataIncrementContextBase CreateIncrementContext(string name, string member, ICondition criteria, int interval, IDataIncrementOptions options);
		protected abstract DataImportContextBase CreateImportContext(string name, IEnumerable data, IEnumerable<string> members, IDataImportOptions options);
		protected abstract DataDeleteContextBase CreateDeleteContext(string name, ICondition criteria, ISchema schema, IDataDeleteOptions options);
		protected abstract DataInsertContextBase CreateInsertContext(string name, bool isMultiple, object data, ISchema schema, IDataInsertOptions options);
		protected abstract DataUpsertContextBase CreateUpsertContext(string name, bool isMultiple, object data, ISchema schema, IDataUpsertOptions options);
		protected abstract DataUpdateContextBase CreateUpdateContext(string name, bool isMultiple, object data, ICondition criteria, ISchema schema, IDataUpdateOptions options);
		protected abstract DataSelectContextBase CreateSelectContext(string name, Type entityType, ICondition criteria, Grouping grouping, ISchema schema, Paging paging, Sorting[] sortings, IDataSelectOptions options);
		#endregion

		#region 激发事件
		protected virtual void OnError(DataAccessErrorEventArgs args) => this.Error?.Invoke(this, args);
		protected virtual void OnExecuted(DataExecuteContextBase context) => this.Executed?.Invoke(this, new DataExecutedEventArgs(context));
		protected virtual bool OnExecuting(DataExecuteContextBase context)
		{
			var e = this.Executing;

			if(e == null)
				return false;

			var args = new DataExecutingEventArgs(context);
			e(this, args);
			return args.Cancel;
		}

		protected virtual void OnExisted(DataExistContextBase context) => this.Existed?.Invoke(this, new DataExistedEventArgs(context));
		protected virtual bool OnExisting(DataExistContextBase context)
		{
			var e = this.Existing;

			if(e == null)
				return false;

			var args = new DataExistingEventArgs(context);
			e(this, args);
			return args.Cancel;
		}

		protected virtual void OnAggregated(DataAggregateContextBase context) => this.Aggregated?.Invoke(this, new DataAggregatedEventArgs(context));
		protected virtual bool OnAggregating(DataAggregateContextBase context)
		{
			var e = this.Aggregating;

			if(e == null)
				return false;

			var args = new DataAggregatingEventArgs(context);
			e(this, args);
			return args.Cancel;
		}

		protected virtual void OnIncremented(DataIncrementContextBase context) => this.Incremented?.Invoke(this, new DataIncrementedEventArgs(context));
		protected virtual bool OnIncrementing(DataIncrementContextBase context)
		{
			var e = this.Incrementing;

			if(e == null)
				return false;

			var args = new DataIncrementingEventArgs(context);
			e(this, args);
			return args.Cancel;
		}

		protected virtual void OnImported(DataImportContextBase context) => this.Imported?.Invoke(this, new DataImportedEventArgs(context));
		protected virtual bool OnImporting(DataImportContextBase context)
		{
			var e = this.Importing;

			if(e == null)
				return false;

			var args = new DataImportingEventArgs(context);
			e(this, args);
			return args.Cancel;
		}

		protected virtual void OnDeleted(DataDeleteContextBase context) => this.Deleted?.Invoke(this, new DataDeletedEventArgs(context));
		protected virtual bool OnDeleting(DataDeleteContextBase context)
		{
			var e = this.Deleting;

			if(e == null)
				return false;

			var args = new DataDeletingEventArgs(context);
			e(this, args);
			return args.Cancel;
		}

		protected virtual void OnInserted(DataInsertContextBase context) => this.Inserted?.Invoke(this, new DataInsertedEventArgs(context));
		protected virtual bool OnInserting(DataInsertContextBase context)
		{
			var e = this.Inserting;

			if(e == null)
				return false;

			var args = new DataInsertingEventArgs(context);
			e(this, args);
			return args.Cancel;
		}

		protected virtual void OnUpserted(DataUpsertContextBase context) => this.Upserted?.Invoke(this, new DataUpsertedEventArgs(context));
		protected virtual bool OnUpserting(DataUpsertContextBase context)
		{
			var e = this.Upserting;

			if(e == null)
				return false;

			var args = new DataUpsertingEventArgs(context);
			e(this, args);
			return args.Cancel;
		}

		protected virtual void OnUpdated(DataUpdateContextBase context) => this.Updated?.Invoke(this, new DataUpdatedEventArgs(context));
		protected virtual bool OnUpdating(DataUpdateContextBase context)
		{
			var e = this.Updating;

			if(e == null)
				return false;

			var args = new DataUpdatingEventArgs(context);
			e(this, args);
			return args.Cancel;
		}

		protected virtual void OnSelected(DataSelectContextBase context) => this.Selected?.Invoke(this, new DataSelectedEventArgs(context));
		protected virtual bool OnSelecting(DataSelectContextBase context)
		{
			var e = this.Selecting;

			if(e == null)
				return false;

			var args = new DataSelectingEventArgs(context);
			e(this, args);
			return args.Cancel;
		}
		#endregion

		#region 私有方法
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private static IEnumerable<T> ToEnumerable<T>(object result) => Collections.Enumerable.Enumerate<T>(result);
		#endregion
	}
}
