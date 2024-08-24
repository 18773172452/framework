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
 * This file is part of Zongsoft.Data.MySql library.
 *
 * The Zongsoft.Data.MySql is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Data.MySql is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Data.MySql library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;

using Zongsoft.Data.Common;
using Zongsoft.Data.Common.Expressions;

namespace Zongsoft.Data.MySql
{
	public class MySqlSelectStatementVisitor : SelectStatementVisitor
	{
		#region 单例字段
		public static readonly MySqlSelectStatementVisitor Instance = new MySqlSelectStatementVisitor();
		#endregion

		#region 构造函数
		private MySqlSelectStatementVisitor() { }
		#endregion

		#region 重写方法
		protected override void OnVisit(ExpressionVisitorContext context, SelectStatement statement)
		{
			//调用基类同名方法
			base.OnVisit(context, statement);

			if(statement.Paging != null && statement.Paging.PageSize > 0)
				this.VisitPaging(context, statement.Paging);
		}
		#endregion

		#region 虚拟方法
		protected virtual void VisitPaging(ExpressionVisitorContext context, Paging paging)
		{
			if(context.Output.Length > 0)
				context.WriteLine();

			context.Write("LIMIT " + paging.PageSize.ToString());

			if(paging.PageIndex > 1)
				context.Write(" OFFSET " + ((paging.PageIndex - 1) * paging.PageSize).ToString());
		}
		#endregion
	}
}
