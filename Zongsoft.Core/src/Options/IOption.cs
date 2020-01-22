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
using System.ComponentModel;
using System.Collections.Generic;

namespace Zongsoft.Options
{
	/// <summary>
	/// ��ʾѡ��Ļ������ܶ��壬�� <seealso cref="Zongsoft.Options.Option"/> ��ʵ�֡�
	/// </summary>
	/// <remarks>����ѡ���ʵ���ߴ� <see cref="Zongsoft.Options.Option"/> ����̳С�</remarks>
	public interface IOption
	{
		#region �¼�����
		event EventHandler Changed;
		event EventHandler Applied;
		event EventHandler Resetted;
		event CancelEventHandler Applying;
		event CancelEventHandler Resetting;
		#endregion

		#region ���Զ���
		object OptionObject
		{
			get;
		}

		IOptionView View
		{
			get;
			set;
		}

		IOptionViewBuilder ViewBuilder
		{
			get;
			set;
		}

		ICollection<IOptionProvider> Providers
		{
			get;
		}

		bool IsDirty
		{
			get;
		}
		#endregion

		#region ��������
		void Reset();
		void Apply();
		#endregion
	}
}
