﻿//HintName: Test_MyAsyncViewModel.GetValue.g.cs
// <auto-generated/>
using System;
using System.Net;
using Lombok.NET;

namespace Test;
#nullable enable
internal partial class MyAsyncViewModel
{
    public global::System.Threading.Tasks.Task<int> GetValueAsync(HttpStatusCode statusCode, int i) => global::System.Threading.Tasks.Task.FromResult(GetValue(statusCode, i));
}