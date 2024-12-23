using System;
using System.Collections.Generic;

using Xunit;

namespace Zongsoft.Externals.Lua.Tests;

public class LuaExpressionEvaluatorTest
{
	[Fact]
	public void TestEvaluateResult()
	{
		var evaluator = new LuaExpressionEvaluator();
		var result = evaluator.Evaluate(@"
		result = { id = 123, name = ""MyName""};
		result.gender = true;
		return result;");

		Assert.NotNull(result);
		Assert.IsType<Dictionary<string, object>>(result);
		var dictionary = (Dictionary<string, object>)result;
		Assert.True(dictionary.TryGetValue("id", out var id));
		Assert.Equal(123L, id);
		Assert.True(dictionary.TryGetValue("name", out var name));
		Assert.Equal("MyName", name);
		Assert.True(dictionary.TryGetValue("gender", out var gender));
		Assert.IsType<bool>(gender);
		Assert.True((bool)gender);
	}

	[Fact]
	public void TestEvaluateJson()
	{
		var evaluator = new LuaExpressionEvaluator();

		var result = evaluator.Evaluate(@"obj = {id = 100, name=""name""}; return json:serialize(obj);");
		Assert.NotNull(result);

		var variables = new Dictionary<string, object>() { { "text", result } };
		result = evaluator.Evaluate(@"obj = json:deserialize(text); obj.id=200; return obj;", variables);
		Assert.NotNull(result);
		Assert.IsAssignableFrom<IDictionary<string, object>>(result);

		if(result is IDictionary<string, object> dictionary)
		{
			Assert.Equal(2, dictionary.Count);
			Assert.Equal(200L, dictionary["id"]);
			Assert.Equal("name", dictionary["name"]);
		}
	}

	[Fact]
	public void TestEvaluateInvoke()
	{
		var evaluator = new LuaExpressionEvaluator();
		evaluator.Global["add"] = Add;
		evaluator.Global["subtract"] = Subtract;

		var result = evaluator.Evaluate("return 1+2", null);
		Assert.NotNull(result);
		Assert.Equal(3, Zongsoft.Common.Convert.ConvertValue<int>(result));

		var variables = new Dictionary<string, object>
		{
			{ "x",  100 },
			{ "y",  200 },
			{ "z",  300 },
		};

		result = evaluator.Evaluate("return x+y+z", variables);
		Assert.NotNull(result);
		Assert.Equal(600, Zongsoft.Common.Convert.ConvertValue<int>(result));

		result = evaluator.Evaluate("return add(x, 10)", variables);
		Assert.NotNull(result);
		Assert.Equal(110, Zongsoft.Common.Convert.ConvertValue<int>(result));
	}

	private static int Add(int a, int b) => a + b;
	private static int Subtract(int a, int b) => a - b;
}