using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace EsentMapper.Utility
{
	internal static class Cache
	{
		public class IL
		{
			private readonly Type _type;

			internal delegate object Constructor();

			internal delegate object ParamterisedConstructor(object[] Params);

			internal delegate object Get(object Instance);

			internal delegate void Set(object Instance, object Value);

			internal delegate object Method(object Instance, object[] Params);

			internal Constructor _constructor;
			internal Dictionary<string, ParamterisedConstructor> _parameterisedConstructors;
			internal Dictionary<string, Get> _getProperties;
			internal Dictionary<string, Set> _setProperties;
			internal Dictionary<string, Method> _methods;
			internal Dictionary<string, Get> _staticGetProperties;
			internal Dictionary<string, Set> _staticSetProperties;
			internal Dictionary<string, Method> _staticMethods;

			internal IL(Type t)
			{
				_type = t;
			}

			#region Internal IL generators

			internal Method MethodInvoker(MethodInfo methodInfo, Type[] paramTypes)
			{
				// Determine the key
				String signature = methodInfo.Name;
				ParameterInfo[] parameters = methodInfo.GetParameters();
				//Type[] paramTypes = new Type[parameters.Length];

				if (paramTypes != null && paramTypes.Length > 0)
				{
					String[] sParams = new String[paramTypes.Length];
					for (int i = 0; i < parameters.Length; ++i)
					{
						sParams[i] = paramTypes[i].Name;
					}
					signature += String.Format("({0})", String.Join(",", sParams));
				}
				Dictionary<string, Method> cache;
				// The the IL is already cached, then return it
				if (methodInfo.IsStatic)
				{
					if (_staticMethods == null)
						_staticMethods = new Dictionary<string, Method>();
					else if (_staticMethods.ContainsKey(signature))
						return _staticMethods[signature];
					cache = _staticMethods;
				}
				else
				{
					if (_methods == null)
						_methods = new Dictionary<string, Method>();
					else if (_methods.ContainsKey(signature))
						return _methods[signature];
					cache = _methods;
				}
				// Generate the IL for the method
				DynamicMethod dynamicMethod = new DynamicMethod(string.Empty, typeof (object),
				                                                new Type[] {typeof (object), typeof (object[])},
				                                                methodInfo.DeclaringType.Module);
				ILGenerator il = dynamicMethod.GetILGenerator();

				LocalBuilder[] locals = new LocalBuilder[paramTypes.Length];
				// generates a local variable for each parameter
				for (int i = 0; i < paramTypes.Length; i++)
				{
					locals[i] = il.DeclareLocal(paramTypes[i], true);
				}
				// creates code to copy the parameters to the local variables
				for (int i = 0; i < paramTypes.Length; i++)
				{
					il.Emit(OpCodes.Ldarg_1);
					EmitFastInt(il, i);
					il.Emit(OpCodes.Ldelem_Ref);
					EmitCastToReference(il, paramTypes[i]);
					il.Emit(OpCodes.Stloc, locals[i]);
				}
				if (!methodInfo.IsStatic)
				{
					// loads the object into the stack
					il.Emit(OpCodes.Ldarg_0);
				}

				// loads the parameters copied to the local variables into the stack
				for (int i = 0; i < paramTypes.Length; i++)
				{
					if (parameters[i].ParameterType.IsByRef)
						il.Emit(OpCodes.Ldloca_S, locals[i]);
					else
						il.Emit(OpCodes.Ldloc, locals[i]);
				}

				// calls the method
				if (!methodInfo.IsStatic)
				{
					il.EmitCall(OpCodes.Callvirt, methodInfo, null);
				}
				else
				{
					il.EmitCall(OpCodes.Call, methodInfo, null);
				}

				// creates code for handling the return value
				if (methodInfo.ReturnType == typeof (void))
				{
					il.Emit(OpCodes.Ldnull);
				}
				else
				{
					EmitBoxIfNeeded(il, methodInfo.ReturnType);
				}

				// iterates through the parameters updating the parameters passed by ref
				for (int i = 0; i < paramTypes.Length; i++)
				{
					if (parameters[i].ParameterType.IsByRef)
					{
						il.Emit(OpCodes.Ldarg_1);
						EmitFastInt(il, i);
						il.Emit(OpCodes.Ldloc, locals[i]);
						if (locals[i].LocalType.IsValueType)
							il.Emit(OpCodes.Box, locals[i].LocalType);
						il.Emit(OpCodes.Stelem_Ref);
					}
				}

				// returns the value to the caller
				il.Emit(OpCodes.Ret);

				// converts the DynamicMethod to a FastInvokeHandler delegate to call to the method
				Method invoker = (Method) dynamicMethod.CreateDelegate(typeof (Method));
				cache.Add(signature, invoker);
				return (Method) invoker;
			}

			internal Get PropertyGetInvoker(PropertyInfo propInfo)
			{
				MethodInfo methodInfo = propInfo.GetGetMethod();
				Dictionary<string, Get> cache;

				if (methodInfo.IsStatic)
				{
					if (_staticGetProperties == null)
						_staticGetProperties = new Dictionary<string, Get>();
					else if (_staticGetProperties.ContainsKey(propInfo.Name))
						return _staticGetProperties[propInfo.Name];
					cache = _staticGetProperties;
				}
				else
				{
					if (_getProperties == null)
						_getProperties = new Dictionary<string, Get>();
					else if (_getProperties.ContainsKey(propInfo.Name))
						return _getProperties[propInfo.Name];
					cache = _getProperties;
				}
				DynamicMethod dynamicMethod = new DynamicMethod(string.Empty, typeof (object), new Type[] {typeof (object)},
				                                                propInfo.DeclaringType.Module);
				ILGenerator il = dynamicMethod.GetILGenerator();
				if (!methodInfo.IsStatic)
				{
					il.Emit(OpCodes.Ldarg_0);
					il.EmitCall(OpCodes.Callvirt, methodInfo, null);
				}
				else
					il.EmitCall(OpCodes.Call, methodInfo, null);
				EmitBoxIfNeeded(il, propInfo.PropertyType);
				il.Emit(OpCodes.Ret);
				Get invoker = (Get) dynamicMethod.CreateDelegate(typeof (Get));
				cache.Add(propInfo.Name, invoker);
				return invoker;
			}

			internal Set PropertySetInvoker(PropertyInfo propInfo)
			{
				MethodInfo methodInfo = propInfo.GetSetMethod();
				Dictionary<string, Set> cache;

				if (methodInfo.IsStatic)
				{
					if (_staticSetProperties == null)
						_staticSetProperties = new Dictionary<string, Set>();
					else if (_staticSetProperties.ContainsKey(propInfo.Name))
						return _staticSetProperties[propInfo.Name];
					cache = _staticSetProperties;
				}
				else
				{
					if (_setProperties == null)
						_setProperties = new Dictionary<string, Set>();
					else if (_setProperties.ContainsKey(propInfo.Name))
						return _setProperties[propInfo.Name];
					cache = _setProperties;
				}
				DynamicMethod dynamicMethod = new DynamicMethod(string.Empty, null, new Type[] {typeof (object), typeof (object)},
				                                                propInfo.DeclaringType.Module);
				ILGenerator il = dynamicMethod.GetILGenerator();
				if (!methodInfo.IsStatic)
					il.Emit(OpCodes.Ldarg_0);
				il.Emit(OpCodes.Ldarg_1);
				EmitCastToReference(il, propInfo.PropertyType);
				if (methodInfo.IsStatic)
					il.EmitCall(OpCodes.Call, methodInfo, null);
				else
					il.EmitCall(OpCodes.Callvirt, methodInfo, null);
				il.Emit(OpCodes.Ret);
				Set invoker = (Set) dynamicMethod.CreateDelegate(typeof (Set));
				cache.Add(propInfo.Name, invoker);
				return invoker;
			}

			internal ParamterisedConstructor GetConstructorInvoker(object[] Params)
			{
				Type[] paramTypes = new Type[Params.Length];
				String[] sParamTypes = new String[Params.Length];

				for (int i = 0; i < Params.Length; ++i)
				{
					paramTypes[i] = Params[i].GetType();
					sParamTypes[i] = paramTypes[i].ToString();
				}

				String signature = _type.Name + "(" + String.Join(",", sParamTypes) + ")";
				if (_parameterisedConstructors == null)
					_parameterisedConstructors = new Dictionary<string, ParamterisedConstructor>();
				else if (_parameterisedConstructors.ContainsKey(signature))
					return _parameterisedConstructors[signature];

				ConstructorInfo info = _type.GetConstructor(paramTypes);
				DynamicMethod dynamicMethod = new DynamicMethod(string.Empty, _type, new Type[] {typeof (object), typeof (object)},
				                                                info.DeclaringType.Module);
				ILGenerator il = dynamicMethod.GetILGenerator();

				// Copy the paramete4rs into local variables
				LocalBuilder[] locals = new LocalBuilder[paramTypes.Length];

				// generates a local variable for each parameter
				for (int i = 0; i < paramTypes.Length; i++)
				{
					locals[i] = il.DeclareLocal(paramTypes[i], true);
				}


				// creates code to copy the parameters to the local variables
				for (int i = 0; i < paramTypes.Length; i++)
				{
					il.Emit(OpCodes.Ldarg_1);
					EmitFastInt(il, i);
					il.Emit(OpCodes.Ldelem_Ref);
					EmitCastToReference(il, paramTypes[i]);
					il.Emit(OpCodes.Stloc, locals[i]);
				}
				for (int i = 0; i < paramTypes.Length; i++)
				{
					il.Emit(OpCodes.Ldloc, locals[i]);
				}
				// generates code to create a new object of the specified type using the specified constructor
				il.Emit(OpCodes.Newobj, info);
				// returns the value to the caller
				il.Emit(OpCodes.Ret);
				// converts the DynamicMethod to a FastCreateInstanceHandler delegate to create the object
				ParamterisedConstructor invoker =
					(ParamterisedConstructor) dynamicMethod.CreateDelegate(typeof (ParamterisedConstructor));
				_parameterisedConstructors.Add(signature, invoker);
				return invoker;
			}

			internal Constructor GetConstructorInvoker()
			{
				if (_constructor == null)
				{
					ConstructorInfo info = _type.GetConstructor(Type.EmptyTypes);
					DynamicMethod dynamicMethod = new DynamicMethod(string.Empty, _type, new Type[0], info.DeclaringType.Module);
					ILGenerator il = dynamicMethod.GetILGenerator();
					il.Emit(OpCodes.Newobj, info);
					il.Emit(OpCodes.Ret);
					_constructor = (Constructor) dynamicMethod.CreateDelegate(typeof (Constructor));
				}
				return _constructor;
			}

			#endregion

			#region Private Emit Generators

			private static void EmitCastToReference(ILGenerator il, System.Type type)
			{
				if (type.IsValueType)
				{
					il.Emit(OpCodes.Unbox_Any, type);
				}
				else
				{
					il.Emit(OpCodes.Castclass, type);
				}
			}

			private static void EmitBoxIfNeeded(ILGenerator il, System.Type type)
			{
				if (type.IsValueType)
				{
					il.Emit(OpCodes.Box, type);
				}
			}

			private static void EmitFastInt(ILGenerator il, int value)
			{
				// for small integers, emit the proper opcode
				switch (value)
				{
					case -1:
						il.Emit(OpCodes.Ldc_I4_M1);
						return;
					case 0:
						il.Emit(OpCodes.Ldc_I4_0);
						return;
					case 1:
						il.Emit(OpCodes.Ldc_I4_1);
						return;
					case 2:
						il.Emit(OpCodes.Ldc_I4_2);
						return;
					case 3:
						il.Emit(OpCodes.Ldc_I4_3);
						return;
					case 4:
						il.Emit(OpCodes.Ldc_I4_4);
						return;
					case 5:
						il.Emit(OpCodes.Ldc_I4_5);
						return;
					case 6:
						il.Emit(OpCodes.Ldc_I4_6);
						return;
					case 7:
						il.Emit(OpCodes.Ldc_I4_7);
						return;
					case 8:
						il.Emit(OpCodes.Ldc_I4_8);
						return;
				}

				// for bigger values emit the short or long opcode
				if (value > -129 && value < 128)
				{
					il.Emit(OpCodes.Ldc_I4_S, (SByte) value);
				}
				else
				{
					il.Emit(OpCodes.Ldc_I4, value);
				}
			}

			#endregion
		}

		public static Dictionary<Type, IL> _cache = new Dictionary<Type, IL>();

		public static object Construct(Type t)
		{
			IL code;
			if (!_cache.ContainsKey(t))
				_cache.Add(t, code = new IL(t));
			else
				code = _cache[t];
			return code.GetConstructorInvoker()();
		}

		public static object Construct(Type t, object[] Params)
		{
			IL code;
			if (!_cache.ContainsKey(t))
				_cache.Add(t, code = new IL(t));
			else
				code = _cache[t];
			return code.GetConstructorInvoker(Params)(Params);
		}

		public static object GetProperty(Type t, object instance, PropertyInfo info)
		{
			IL code;
			if (_cache.ContainsKey(t))
				code = _cache[t];
			else
				_cache.Add(t, code = new IL(t));
			return code.PropertyGetInvoker(info)(instance);
		}

		public static void SetProperty(Type t, object instance, PropertyInfo info, object Value)
		{
			IL code;
			if (_cache.ContainsKey(t))
				code = _cache[t];
			else
				_cache.Add(t, code = new IL(t));
			code.PropertySetInvoker(info)(instance, Value);
		}

		public static object CallMethod(Type t, object instance, MethodInfo info, Type[] ParamTypes, object[] Params)
		{
			IL code;
			if (_cache.ContainsKey(t))
				code = _cache[t];
			else
				_cache.Add(t, code = new IL(t));
			return code.MethodInvoker(info, ParamTypes)(instance, Params);
		}
	}

	/// <summary>
	/// Encapsulates a late-bound instance of an object
	/// </summary>
	/// <remarks>
	/// This class is used as an alternative to the System.Reflection classes and methods
	/// to access a Type's properties and Methods at runtime, based on data retrieved from
	/// runtime configuration. It generates and cache's they Type's MSIL code at runtime, 
	/// providing much more efficient access to the object's members than Reflection can.
	/// <para>
	/// An instance of this class can be used to construct an instance of the latebound class,
	/// and/or to call both either instance or static methods on the class. This is particularly 
	/// useful when all you have is an interface returned from some unknown plug-in, but the plug-in
	/// requires other properties in order to operate correctly.
	/// </para>
	/// </remarks>
	public sealed class Latebound
	{
		private object _instance;
		private Type _type;

		/// <summary>
		/// Latebound object constructor, when only the type of the object required is known.
		/// </summary>
		/// <param name="t">The type of object the caller requires an new instance of.</param>
		/// <remarks>
		/// This constructor will generate and call the IL required to create an instance of an
		/// object of Type t. This call is considerably faster than using reflection to perform
		/// the same job, particularly as the generated IL is cached to reuse whenever another
		/// instance of the object is required.
		/// <example>
		/// <code>
		/// Type t;
		/// 
		/// t = SomeType;
		/// Latebound someType = new Latebound(t);
		/// </code>
		/// </example>
		/// </remarks>
		public Latebound(Type t)
		{
			_type = t;
			_instance = Cache.Construct(t);
		}

		/// <summary>
		/// Latebound object parameterised constructor, where the unknown object's constructor
		/// requires parameters.
		/// </summary>
		/// <param name="t">The type of object the caller requires an instance of</param>
		/// <param name="Params">Parameters reuired for type t's constructor</param>
		/// <remarks>
		/// This constructor will generate and call the IL required to create an instance of an
		/// object of Type t, passing the supplied parameters to the appropriate constructor. 
		/// This call is considerably faster than using reflection to perform the same job,
		/// particularly as the generated IL is cached to reuse whenever another
		/// instance of the object is required.
		/// <para>
		/// Note that the types of the parameters passed into this constructor must match exactly
		/// the signature of required object's constructor parameters, otherwise the call will fail.
		/// </para>
		/// <para>
		/// This type of constructor is particularly useful when it is known that a type that 
		/// implements a known interface takes a parameterised constructor. The follwing example 
		/// constructs an object that implements IMyPlugin where the Type has been retrieved from
		/// an attribute on the class. It has been agreed that the plugins will be able to accept
		/// a configuration file's path as a constructor parameter, in order to initialise its 
		/// custom properties.
		/// <example>
		/// <code>
		/// Attribute attr = Attribute.GetCustomAttribute(MyPluginAssembly, typeof(PluginImplementationAttribute));
		/// Type t = attr.PluginImplentationType;
		/// String path = MySettingsPath;
		/// 
		/// Latebound MyPluginInstance = new Latebound(t, path);
		/// IMyPlugin iPlugin = (IMyPlugin)MyPluginInstance.Instance;
		/// </code>
		/// </example>
		/// </para>
		/// </remarks>
		public Latebound(Type t, params object[] Params)
		{
			_type = t;
			_instance = Cache.Construct(t, Params);
		}

		/// <summary>
		/// Creates a Latebound object from an existing instance of the type
		/// </summary>
		/// <param name="Instance">The object you want to call Latebound methods and properties on</param>
		/// <remarks>
		/// This type of latebound constructor is usefule when a plugin returns an interface pointer
		/// to a constructed object. The resulting latebound object will then allow the caller to
		/// call other properties and methods on the object that are not included in the interface.
		/// <example>
		/// <code>
		/// IMyPlugin myPlugin = PluginManager.CreateInstance("MyPluginName");
		/// Latebound myLateboundInstance = new Latebound(myPlugin);
		/// 
		/// object myPluginProperty = myLateboundInstance["ThePropertyName"];
		/// </code>
		/// </example>
		/// </remarks>
		public Latebound(object Instance)
		{
			_instance = Instance;
			_type = Instance.GetType();
		}


		public object this[string PropertyName]
		{
			get { return Cache.GetProperty(_type, _instance, _type.GetProperty(PropertyName)); }
			set { Cache.SetProperty(_type, _instance, _type.GetProperty(PropertyName), value); }
		}

		public object Call(string MethodName, params object[] Params)
		{
			MethodInfo info;
			Type[] types = null;
			if (Params != null && Params.Length > 0)
			{
				types = new Type[Params.Length];
				for (int i = 0; i < Params.Length; ++i)
					types[i] = Params[i].GetType();
				info = _type.GetMethod(MethodName, BindingFlags.ExactBinding | BindingFlags.Public | BindingFlags.Instance, null,
				                       types, null);
			}
			else
				info = _type.GetMethod(MethodName, BindingFlags.Public | BindingFlags.Instance);

			return Cache.CallMethod(_type, _instance, info, types, Params);
		}

		public object Instance
		{
			get { return _instance; }
		}

		public static object Get(Type t, string PropertyName)
		{
			return Cache.GetProperty(t, null, t.GetProperty(PropertyName));
		}

		public static void Set(Type t, string PropertyName, object Value)
		{
			Cache.SetProperty(t, null, t.GetProperty(PropertyName), Value);
		}

		public static object CallS(Type t, string MethodName, params object[] Params)
		{
			MethodInfo info;
			Type[] types = null;
			if (Params != null && Params.Length > 0)
			{
				types = new Type[Params.Length];
				for (int i = 0; i < Params.Length; ++i)
					types[i] = Params[i].GetType();
				info = t.GetMethod(MethodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.ExactBinding, null, types,
				                   null);
			}
			else
				info = t.GetMethod(MethodName, BindingFlags.Static | BindingFlags.Public);
			return Cache.CallMethod(t, null, info, null, Params);
		}
	}

	public sealed class Latebound<T> where T : class
	{
		private T _instance;

		public Latebound()
		{
			_instance = (T) Cache.Construct(typeof (T));
		}

		public Latebound(params object[] Params)
		{
			_instance = (T) Cache.Construct(typeof (T), Params);
		}

		public Latebound(object Instance)
		{
			if (!(Instance is T))
				throw new ArgumentException(String.Format("Parameter is not an instance of type {0}", typeof (T).FullName),
				                            "Instance");
			_instance = (T) Instance;
		}

		public object this[string PropertyName]
		{
			get { return Cache.GetProperty(typeof (T), _instance, typeof (T).GetProperty(PropertyName)); }
			set { Cache.SetProperty(typeof (T), _instance, typeof (T).GetProperty(PropertyName), value); }
		}

		public object Call(string MethodName, params object[] Params)
		{
			MethodInfo info;
			Type _type = typeof (T);
			Type[] types = null;
			if (Params != null && Params.Length > 0)
			{
				types = new Type[Params.Length];
				for (int i = 0; i < Params.Length; ++i)
					types[i] = Params[i].GetType();
				info = _type.GetMethod(MethodName, BindingFlags.ExactBinding | BindingFlags.Public | BindingFlags.Instance, null,
				                       types, null);
			}
			else
				info = _type.GetMethod(MethodName, BindingFlags.Public | BindingFlags.Instance);

			return Cache.CallMethod(_type, _instance, info, types, Params);
		}

		public T Instance
		{
			get { return _instance; }
		}

		public static object Get(string PropertyName)
		{
			return Cache.GetProperty(typeof (T), null, typeof (T).GetProperty(PropertyName));
		}

		public static void Set(string PropertyName, object Value)
		{
			Cache.SetProperty(typeof (T), null, typeof (T).GetProperty(PropertyName), Value);
		}

		public static object CallS(string MethodName, params object[] Params)
		{
			MethodInfo info;
			Type t = typeof (T);
			Type[] types = null;
			if (Params != null && Params.Length > 0)
			{
				types = new Type[Params.Length];
				for (int i = 0; i < Params.Length; ++i)
					types[i] = Params[i].GetType();
				info = t.GetMethod(MethodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.ExactBinding, null, types,
				                   null);
			}
			else
				info = t.GetMethod(MethodName, BindingFlags.Static | BindingFlags.Public);
			return Cache.CallMethod(t, null, info, types, Params);
		}
	}
}