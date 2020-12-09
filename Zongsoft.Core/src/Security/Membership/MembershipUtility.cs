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
using System.Linq;
using System.Collections.Generic;

using Zongsoft.Data;

namespace Zongsoft.Security.Membership
{
	public static class MembershipUtility
	{
		#region 公共方法
		public static IEnumerable<TRole> GetAncestors<TRole>(IDataAccess dataAccess, uint memberId, MemberType memberType, string @namespace = null) where TRole : IRole
		{
			if(dataAccess == null)
				throw new ArgumentNullException(nameof(dataAccess));

			var filter = Mapping.Instance.Namespace.GetCondition(Mapping.Instance.Role, @namespace);

			if(filter != null)
				filter = new Condition("Role." + filter.Name, filter.Value, filter.Operator);

			var roles = new HashSet<TRole>(dataAccess.Select<Member<TRole, IUser>>(
				Mapping.Instance.Member,
				Condition.Equal(nameof(Member.MemberId), memberId) &
				Condition.Equal(nameof(Member.MemberType), memberType) &
				filter,
				"Role{*}").Select(p => p.Role), RoleComparer<TRole>.Instance);

			if(roles.Count > 0)
			{
				var intersection = roles.Select(role => role.RoleId).ToArray();

				while(intersection.Any())
				{
					var parents = dataAccess.Select<Member<TRole, IUser>>(
						Mapping.Instance.Member,
						Condition.In(nameof(Member.MemberId), intersection) &
						Condition.Equal(nameof(Member.MemberType), MemberType.Role) &
						filter,
						"Role{*}", DataSelectOptions.Distinct()).Select(p => p.Role).ToArray();

					intersection = parents.Except(roles, RoleComparer<TRole>.Instance).Select(p => p.RoleId).ToArray();
					roles.UnionWith(parents);
				}
			}

			return roles;
		}
		#endregion

		#region 内部方法
		internal static Condition GetIdentityCondition(string identity)
		{
			return GetIdentityCondition(identity, out _);
		}

		internal static Condition GetIdentityCondition(string identity, out UserIdentityType identityType)
		{
			if(string.IsNullOrWhiteSpace(identity))
				throw new ArgumentNullException(nameof(identity));

			if(identity.Contains("@"))
			{
				identityType = UserIdentityType.Email;
				return Condition.Equal(nameof(IUser.Email), identity);
			}

			if(IsNumericString(identity))
			{
				identityType = UserIdentityType.Phone;
				return Condition.Equal(nameof(IUser.Phone), identity);
			}

			identityType = UserIdentityType.Name;
			return Condition.Equal(nameof(IUser.Name), identity);
		}

		internal static UserIdentityType GetIdentityType(string identity)
		{
			if(string.IsNullOrEmpty(identity))
				throw new ArgumentNullException(nameof(identity));

			if(identity.Contains("@"))
				return UserIdentityType.Email;

			if(IsNumericString(identity))
				return UserIdentityType.Phone;

			return UserIdentityType.Name;
		}
		#endregion

		#region 私有方法
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private static bool IsNumericString(string text)
		{
			if(string.IsNullOrEmpty(text))
				return false;

			for(var i = 0; i < text.Length; i++)
			{
				if(text[i] < '0' || text[i] > '9')
					return false;
			}

			return true;
		}
		#endregion

		#region 嵌套子类
		private class RoleComparer<TRole> : IEqualityComparer<TRole> where TRole : IRole
		{
			public static readonly RoleComparer<TRole> Instance = new RoleComparer<TRole>();

			public bool Equals(TRole x, TRole y)
			{
				if(x == null)
					return y == null;
				else
					return y == null ? false : x.RoleId == y.RoleId;
			}

			public int GetHashCode(TRole role)
			{
				return (int)role.RoleId;
			}
		}
		#endregion
	}
}
