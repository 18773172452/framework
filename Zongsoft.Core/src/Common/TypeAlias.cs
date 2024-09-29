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
 * Copyright (C) 2010-2024 Zongsoft Studio <http://www.zongsoft.com>
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

namespace Zongsoft.Common
{
	public static partial class TypeAlias
	{
		#region 静态字段
		private static readonly Dictionary<string, Type> Types = new(StringComparer.OrdinalIgnoreCase)
		{
			{ nameof(Object), typeof(object) },
			{ nameof(DBNull), typeof(DBNull) },
			{ nameof(String), typeof(string) },
			{ nameof(Boolean), typeof(bool) },
			{ nameof(Guid), typeof(Guid) },
			{ nameof(Char), typeof(char) },
			{ nameof(Byte), typeof(byte) },
			{ nameof(SByte), typeof(sbyte) },
			{ nameof(Int16), typeof(short) },
			{ nameof(UInt16), typeof(ushort) },
			{ nameof(Int32), typeof(int) },
			{ nameof(UInt32), typeof(uint) },
			{ nameof(Int64), typeof(long) },
			{ nameof(UInt64), typeof(ulong) },
			{ nameof(Single), typeof(float) },
			{ nameof(Double), typeof(double) },
			{ nameof(Decimal), typeof(decimal) },
			{ nameof(TimeSpan), typeof(TimeSpan) },
			{ nameof(DateOnly), typeof(DateOnly) },
			{ nameof(TimeOnly), typeof(TimeOnly) },
			{ nameof(DateTime), typeof(DateTime) },
			{ nameof(DateTimeOffset), typeof(DateTimeOffset) },

			{ "void", typeof(void) },
			{ "bool", typeof(bool) },
			{ "uuid", typeof(Guid) },
			{ "float", typeof(float) },
			{ "short", typeof(short) },
			{ "ushort", typeof(ushort) },
			{ "int", typeof(int) },
			{ "integer", typeof(int) },
			{ "uint", typeof(uint) },
			{ "long", typeof(long) },
			{ "ulong", typeof(ulong) },
			{ "money", typeof(decimal) },
			{ "currency", typeof(decimal) },
			{ "date", typeof(DateOnly) },
			{ "time", typeof(TimeOnly) },
			{ "timestamp", typeof(DateTimeOffset) },
			{ "binary", typeof(byte[]) },
			{ "range", typeof(Zongsoft.Data.Range<>) },
			{ "mixture", typeof(Zongsoft.Data.Mixture<>) },

			{ nameof(IList), typeof(IList<>) },
			{ nameof(IEnumerable), typeof(IEnumerable<>) },
			{ nameof(ICollection), typeof(ICollection<>) },
			{ nameof(IDictionary), typeof(IDictionary<,>) },

			{ "List", typeof(List<>) },
			{ "ISet", typeof(ISet<>) },
			{ "Hashset", typeof(HashSet<>) },
			{ "Dictionary", typeof(Dictionary<,>) },

			{ "IReadOnlySet", typeof(IReadOnlySet<>) },
			{ "IReadOnlyList", typeof(IReadOnlyList<>) },
			{ "IReadOnlyCollection", typeof(IReadOnlyCollection<>) },
			{ "IReadOnlyDictionary", typeof(IReadOnlyDictionary<,>) },
		};
		#endregion

		#region 解析方法
		public static bool TryParse(string typeName, out Type result) => TryParse(typeName, true, out result);
		public static bool TryParse(string typeName, bool ignoreCase, out Type result)
		{
			result = Parse(typeName, false, ignoreCase);
			return result != null;
		}

		public static Type Parse(string typeName, bool throwException = true, bool ignoreCase = true)
		{
			if(string.IsNullOrEmpty(typeName))
				return throwException ? throw new ArgumentNullException(nameof(typeName)) : null;

			if(Types.TryGetValue(typeName, out var type) && !type.ContainsGenericParameters)
				return type;

			var token = throwException ? ParseCore(typeName, message => throw new InvalidOperationException(message)) : ParseCore(typeName);
			return string.IsNullOrEmpty(token.Type) ? Type.GetType(typeName, throwException, ignoreCase) : token.ToType();
		}
		#endregion
	}
}
