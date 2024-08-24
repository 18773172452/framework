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
using System.Threading;

namespace Zongsoft.Services.Commands
{
	[CommandOption(KEY_TIMEOUT_OPTION, typeof(TimeSpan), DefaultValue = "5s", Description = "${Text.Command.Options.Timeout}")]
	public class WorkerResumeCommand : CommandBase<CommandContext>
	{
		#region 单例字段
		public static readonly WorkerResumeCommand Default = new WorkerResumeCommand();
		#endregion

		#region 常量定义
		private const string KEY_TIMEOUT_OPTION = "timeout";
		#endregion

		#region 构造函数
		public WorkerResumeCommand() : base("Resume")
		{
		}

		public WorkerResumeCommand(string name) : base(name)
		{
		}
		#endregion

		#region 执行方法
		protected override object OnExecute(CommandContext context)
		{
			//向上查找工作者命令对象，如果找到则获取其对应的工作者对象
			var worker = context.CommandNode.Find<WorkerCommandBase>(true)?.Worker;

			//如果指定的工作器查找失败，则抛出异常
			if(worker == null)
				throw new CommandException("Missing required worker of depends on.");

			//如果当前工作器状态不是暂停的，则忽略该请求
			if(worker.State != WorkerState.Paused)
				return worker;

			//恢复工作者
			worker.Resume();

			//调用恢复完成方法
			this.OnPaused(context, worker, context.Expression.Options.GetValue<TimeSpan>(KEY_TIMEOUT_OPTION));

			//返回执行成功的工作者
			return worker;
		}
		#endregion

		#region 虚拟方法
		protected virtual void OnPaused(CommandContext context, IWorker worker, TimeSpan timeout)
		{
			if(timeout <= TimeSpan.Zero)
				timeout = TimeSpan.FromSeconds(5);
			else if(timeout.TotalSeconds > 30)
				timeout = TimeSpan.FromSeconds(30);

			switch(worker.State)
			{
				case WorkerState.Running:
					this.OnSucceed(context.Output, worker);
					break;
				case WorkerState.Paused:
					this.OnFailed(context.Output, worker);
					break;
				case WorkerState.Resuming:
					SpinWait.SpinUntil(() => worker.State == WorkerState.Running, timeout);

					if(worker.State == WorkerState.Running)
						this.OnSucceed(context.Output, worker);
					else
						this.OnFailed(context.Output, worker);

					break;
			}
		}
		#endregion

		#region 私有方法
		private void OnFailed(ICommandOutlet output, IWorker worker)
		{
			output.WriteLine(Utility.GetWorkerActionContent(worker, string.Format(Properties.Resources.Text_Command_ExecutionFailed_Message, Properties.Resources.Text_WorkerResumeCommand_Name), CommandOutletColor.DarkRed));
		}

		private void OnSucceed(ICommandOutlet output, IWorker worker)
		{
			output.WriteLine(Utility.GetWorkerActionContent(worker, string.Format(Properties.Resources.Text_Command_ExecutionSucceed_Message, Properties.Resources.Text_WorkerResumeCommand_Name), CommandOutletColor.DarkGreen));
		}
		#endregion
	}
}
