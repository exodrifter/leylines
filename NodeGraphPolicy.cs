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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Exodrifter.NodeGraph
{
	/// <summary>
	/// Defines a list of types and nodes that are allowed in a node graph.
	/// </summary>
	[Serializable]
	public class NodeGraphPolicy
	{
		[SerializeField]
		private bool useCustom = false;
		[SerializeField]
		private List<string> customFilters = new List<string>();

		[SerializeField]
		private bool useReflection = false;
		[SerializeField]
		private List<string> assemblyFilters = new List<string>();

		public SyncList<SearchResult> SearchItems
		{
			get
			{
				lock (this)
				{
					Init();
					return searchItems;
				}
			}
		}
		private SyncList<SearchResult> searchItems;

		private Job job;

		private bool oldUseCustom;
		private List<string> oldCustomFilters;
		private bool oldUseReflection;
		private List<string> oldAssemblyFilters;

		#region Properties

		public static NodeGraphPolicy DefaultPolicy
		{
			get
			{
				var ret = new NodeGraphPolicy();
				ret.useCustom = true;
				ret.CustomFilters.Add("Exodrifter.NodeGraph.DefaultNodes");
				ret.useReflection = false;
				return ret;
			}
		}

		public bool UseCustom
		{
			get { return useCustom; }
			set { useCustom = value; }
		}

		public List<string> CustomFilters
		{
			get { return customFilters; }
			set { customFilters = value; }
		}

		public bool UseReflection
		{
			get { return useReflection; }
			set { useReflection = value; }
		}

		public List<string> AssemblyFilters
		{
			get { return assemblyFilters; }
			set { assemblyFilters = value; }
		}

		#endregion

		private void Init()
		{
			// Check if cache is still valid
			if (job != null)
			{
				var same = true;
				if (oldUseCustom != useCustom)
				{
					same = false;
				}
				else if (!oldCustomFilters.SequenceEqual(customFilters))
				{
					same = false;
				}
				else if (oldUseReflection != useReflection)
				{
					same = false;
				}
				else if (!oldAssemblyFilters.SequenceEqual(assemblyFilters))
				{
					same = false;
				}

				if (same)
				{
					return;
				}
			}

			// Update cache
			oldUseCustom = useCustom;
			oldCustomFilters = new List<string>(customFilters);
			oldUseReflection = useReflection;
			oldAssemblyFilters = new List<string>(assemblyFilters);

			if (job != null)
			{
				job.Abort();
			}

			var results = new SyncList<SearchResult>();
			searchItems = results;
			job = new Job(() =>
			{
				InitReflectionData(results);
				InitCustomData(results);
			}).Start();
		}

		#region Custom

		private void InitCustomData(SyncList<SearchResult> results)
		{
			if (!useCustom)
			{
				return;
			}

			var types = GetClassesOfType<Node>();
			foreach (var type in types)
			{
				if (type.IsAbstract || type.IsInterface)
				{
					continue;
				}

				var attributes = (NodeAttribute[])
					Attribute.GetCustomAttributes(type, typeof(NodeAttribute));
				if (attributes.Length < 1)
				{
					continue;
				}
				var attribute = attributes[0];

				if (!CustomTypeIsAllowed(type))
				{
					continue;
				}

				var path = attribute.Path;
				if (string.IsNullOrEmpty(path))
				{
					path = type.FullName.Replace('.', '/').Replace('+', '/');
				}

				results.Add(new SearchResult(path, () =>
					{
						return (Node)ScriptableObject.CreateInstance(type);
					})
				);
			}
		}

		private static IEnumerable<Type> GetClassesOfType<T>() where T : class
		{
			return
				from assembly in AppDomain.CurrentDomain.GetAssemblies()
				from type in assembly.GetTypes()
				where typeof(T).IsAssignableFrom(type)
				select type;
		}

		private bool CustomTypeIsAllowed(Type type)
		{
			if (!useCustom)
			{
				return false;
			}

			return IsAllowedInFilter(type, customFilters, (x) =>
			{
				return x.FullName;
			});
		}

		#endregion

		#region Reflection

		private void InitReflectionData(SyncList<SearchResult> results)
		{
			if (!useReflection)
			{
				return;
			}

			var types = (
				from assembly in AppDomain.CurrentDomain.GetAssemblies()
				from type in assembly.GetTypes()
				where AssemblyIsAllowed(assembly)
				where !type.Name.StartsWith("<>") // Anonymous types
				where !typeof(Node).IsAssignableFrom(type) // Custom nodes
				select type
			).ToList();

			foreach (var type in types)
			{
				var typePath = type.FullName.Replace('.', '/').Replace('+', '/');

				// Ignore attribute types
				if (typeof(Attribute).IsAssignableFrom(type))
				{
					continue;
				}

				// Allow variable node if type is not a static class
				if (!type.IsAbstract || !type.IsSealed)
				{
					results.Add(new SearchResult(typePath, () =>
						{
							return CreateDynamicNode(type);
						})
					);
				}

				var members =
					from member in type.GetMembers()
					where member.MemberType == MemberTypes.Field
						|| member.MemberType == MemberTypes.Property
						|| member.MemberType == MemberTypes.Method
					select member;

				foreach (var member in members)
				{
					// Ignore obselete members
					if (member.GetCustomAttributes(true)
						.Any(t => t.GetType() == typeof(ObsoleteAttribute)))
					{
						continue;
					}

					if (member is MethodInfo)
					{
						var method = (MethodInfo)member;

						// Ignore special methods
						if (method.IsSpecialName)
						{
							continue;
						}
					}

					// Build the suffix
					string suffix;
					switch (member.MemberType)
					{
						case MemberTypes.Method:
							suffix = "()";
							break;
						case MemberTypes.Field:
						case MemberTypes.Property:

							// Ignore enum fields and properties
							if (type.IsEnum)
							{
								continue;
							}
							suffix = "";
							break;
						default:
							suffix = "?";
							break;
					}

					var memberPath = string.Format("{0}.{1}{2}",
						typePath, member.Name, suffix);

					results.Add(new SearchResult(memberPath, () =>
						{
							return CreateDynamicNode(type, member);
						})
					);
				}
			}
		}

		private Node CreateDynamicNode(Type type)
		{
			var node = ScriptableObject.CreateInstance<DynamicNode>();
			node.DisplayName = type.Name;

			node.AddOutputSocket(new DynamicSocket(type, type.Name,
				SocketFlags.AllowMultipleLinks | SocketFlags.Editable));

			return node;
		}

		private Node CreateDynamicNode(Type type, MemberInfo member)
		{
			var node = ScriptableObject.CreateInstance<DynamicNode>();

			switch (member.MemberType)
			{
				case MemberTypes.Method:
					node.name = type.Name + "." + member.Name + "()";

					var method = (MethodInfo)member;
					node.AddInputSocket(
						new DynamicSocket(typeof(ExecType), "execIn"));
					node.AddInputSocket(
						new DynamicSocket(type, type.Name, SocketFlags.Editable));

					var @params = new List<string>();
					foreach (var param in method.GetParameters())
					{
						node.AddInputSocket(
							new DynamicSocket(param.ParameterType, param.Name, SocketFlags.Editable));
						@params.Add(param.Name);
					}

					node.AddOutputSocket(
						new DynamicSocket(typeof(ExecType), "execOut", SocketFlags.AllowMultipleLinks));
					if (method.ReturnType != typeof(void))
					{
						node.AddOutputSocket(
							new DynamicSocket(method.ReturnType, "result", SocketFlags.AllowMultipleLinks));
					}

					node.AddExecInvoke(
						new ExecInvoke("execIn", "execOut", type.Name, "result", method.Name, InvokeType.CallMethod, @params));

					break;

				case MemberTypes.Field:
					node.name = type.Name + "." + member.Name;

					var field = (FieldInfo)member;
					node.AddInputSocket(
						new DynamicSocket(typeof(ExecType), "setExec"));
					node.AddInputSocket(
						new DynamicSocket(type, type.Name, SocketFlags.Editable));

					node.AddOutputSocket(
						new DynamicSocket(typeof(ExecType), "execOut", SocketFlags.AllowMultipleLinks));
					node.AddOutputSocket(
						new DynamicSocket(field.FieldType, field.Name, SocketFlags.AllowMultipleLinks));

					node.AddExecInvoke(
						new ExecInvoke("setExec", "execOut", type.Name, "result", field.Name, InvokeType.SetField));
					node.AddEvalInvoke(
						new EvalInvoke(type.Name, "result", member.Name, InvokeType.GetField));
					break;

				case MemberTypes.Property:
					node.name = type.Name + "." + member.Name;

					var property = (PropertyInfo)member;
					node.AddInputSocket(
						new DynamicSocket(typeof(ExecType), "setExec"));
					node.AddInputSocket(
						new DynamicSocket(type, type.Name, SocketFlags.Editable));

					node.AddOutputSocket(
						new DynamicSocket(typeof(ExecType), "execOut", SocketFlags.AllowMultipleLinks));
					node.AddOutputSocket(
						new DynamicSocket(property.PropertyType, property.Name, SocketFlags.AllowMultipleLinks));

					node.AddExecInvoke(
						new ExecInvoke("setExec", "execOut", type.Name, "result", property.Name, InvokeType.SetProperty));
					node.AddEvalInvoke(
						new EvalInvoke(type.Name, "result", member.Name, InvokeType.GetProperty));
					break;

				default:
					Debug.LogError(string.Format(
						"MemberType {0} is not supported.",
						member.MemberType));
					break;
			}

			return node;
		}

		private bool AssemblyIsAllowed(Assembly assembly)
		{
			if (!useReflection)
			{
				return false;
			}

			return IsAllowedInFilter(assembly, assemblyFilters, (x) =>
			{
				return x.GetName().Name;
			});
		}

		#endregion

		private static bool IsAllowedInFilter<T>
			(T obj, List<string> filters, Func<T, string> toString)
		{
			if (filters == null || filters.Count == 0)
			{
				return true;
			}

			var allowed = false;
			foreach (var filter in filters)
			{
				var setTo = true;
				var pattern = filter;

				// Detect ignore filters
				if (pattern.StartsWith("!"))
				{
					pattern = filter.Substring(1);
					setTo = false;
				}

				if (Regex.IsMatch(toString(obj), filter))
				{
					allowed = setTo;
				}
			}

			return allowed;
		}
	}
}
