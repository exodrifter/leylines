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
	[Flags]
	public enum SocketFlags
	{
		Editable = 1,
		AllowMultipleLinks = 2,
	}

	public static class SocketFlagsExtensions
	{
		public static bool AllowMultipleLinks(this SocketFlags flags)
		{
			return (flags & SocketFlags.AllowMultipleLinks)
				== SocketFlags.AllowMultipleLinks;
		}

		public static bool IsEditable(this SocketFlags flags)
		{
			return (flags & SocketFlags.Editable)
				== SocketFlags.Editable;
		}

		public static SocketFlags SetAllowMultipleLinks
			(this SocketFlags flags, bool val)
		{
			if (val)
			{
				return flags | SocketFlags.AllowMultipleLinks;
			}
			else
			{
				return flags & ~SocketFlags.AllowMultipleLinks;
			}
		}

		public static SocketFlags SetIsEditable
			(this SocketFlags flags, bool val)
		{
			if (val)
			{
				return flags | SocketFlags.Editable;
			}
			else
			{
				return flags & ~SocketFlags.Editable;
			}
		}
	}
}
