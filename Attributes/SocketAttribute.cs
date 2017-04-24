﻿/* Unity3D Node Graph Framework
Copyright (c) 2017 Ava Pek

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
using System;

namespace Exodrifter.NodeGraph
{
	/// <summary>
	/// Used to specify a field as a socket.
	/// </summary>
	[AttributeUsage
		(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
	public class SocketAttribute : Attribute
	{
		public const int DEFAULT_WIDTH = 120;

		public readonly string name;
		public readonly int width;
		public readonly SocketFlags flags;

		public SocketAttribute(string name = null, int width = DEFAULT_WIDTH,
			SocketFlags flags = 0)
		{
			this.name = name;
			this.width = width;
			this.flags = flags;
		}
	}
}
