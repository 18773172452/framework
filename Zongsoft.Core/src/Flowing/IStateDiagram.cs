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
using System.Collections.Generic;

namespace Zongsoft.Flowing
{
	public interface IStateDiagram<TKey, TValue> where TKey : struct, IEquatable<TKey> where TValue : struct
	{
		#region 属性定义
		StateVector<TValue>[] Vectors { get; }
		#endregion

		#region 方法定义
		bool CanTransfer(StateVector<TValue> state) => this.CanTransfer(state.Source, state.Destination);
		bool CanTransfer(TValue source, TValue destination);
		void Transfer(IStateContext<TKey, TValue> context, IStateHandler<TKey, TValue> handler);

		State<TKey, TValue> GetState(TKey key);
		bool SetState(TKey key, TValue value, string description, IDictionary<object, object> parameters = null);
		bool SetState(State<TKey, TValue> state, string description, IDictionary<object, object> parameters = null);
		#endregion
	}
}
