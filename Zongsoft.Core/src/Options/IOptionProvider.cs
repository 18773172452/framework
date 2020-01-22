/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   �ӷ�(Popeye Zhong) <zongsoft@gmail.com>
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

namespace Zongsoft.Options
{
	/// <summary>
	/// �ṩѡ�����ݵĻ�ȡ�뱣�档
	/// </summary>
	public interface IOptionProvider
	{
		/// <summary>
		/// ����ָ����ѡ��·����ȡ��Ӧ��ѡ�����ݡ�
		/// </summary>
		/// <param name="text">Ҫ��ȡ��ѡ��·�����ʽ�ı����ñ��ʽ�Ľṹ��ο�<seealso cref="Zongsoft.Collections.HierarchicalExpression"/>��</param>
		/// <returns>��ȡ����ѡ�����ݶ���</returns>
		object GetOptionValue(string text);

		/// <summary>
		/// ��ָ����ѡ�����ݱ��浽ָ��·���Ĵ洢�����С�
		/// </summary>
		/// <param name="text">�������ѡ��·�����ʽ�ı����ñ��ʽ�Ľṹ��ο�<seealso cref="Zongsoft.Collections.HierarchicalExpression"/>��</param>
		/// <param name="value">�������ѡ�����</param>
		void SetOptionValue(string text, object value);
	}
}
