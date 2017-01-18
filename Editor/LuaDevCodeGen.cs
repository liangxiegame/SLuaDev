// The MIT License (MIT)

// Copyright 2015 Siney/Pangweiwei siney@yeah.net
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using SLua;

namespace SLuaDev
{
	using UnityEngine;
	using System.Collections;
	using System.Collections.Generic;
	using System.IO;
	using System;
	using System.Reflection;
	using UnityEditor;
	using LuaInterface;
	using System.Text;
	using System.Text.RegularExpressions;

    public class LuaDevCodeGen 
	{
		static public string GenPath = Application.dataPath + "/SluaDev/quickxlib/system_api/";
        public delegate void ExportGenericDelegate(Type t, string ns);
		
        static bool autoRefresh = true;

        static bool IsCompiling {
			get {
				if (EditorApplication.isCompiling) {
                    Debug.Log("Unity Editor is compiling, please wait.");
				}
				return EditorApplication.isCompiling;
			}
		}
		 
		[InitializeOnLoad]
		public class Startup
		{
			static bool isPlaying=false;
			static Startup()
			{
				EditorApplication.update += Update;
				// use this delegation to ensure dispose luavm at last
				EditorApplication.playmodeStateChanged+=()=>{
					
					if(isPlaying==true && EditorApplication.isPlaying==false) {
						if(LuaState.main!=null) LuaState.main.Dispose();
					}

					isPlaying=EditorApplication.isPlaying;
				};
			} 


			static void Update(){
				EditorApplication.update -= Update;
				Lua3rdMeta.Instance.ReBuildTypes();

                // Remind user to generate lua interface code
			    var remindGenerate = !EditorPrefs.HasKey("SLUA_REMIND_GENERTE_LUA_INTERFACE") || EditorPrefs.GetBool("SLUA_REMIND_GENERTE_LUA_INTERFACE");
				bool ok = System.IO.Directory.Exists(GenPath+"Unity");
				if (!ok && remindGenerate)
				{
				    if (EditorUtility.DisplayDialog("Slua", "Not found lua interface for Unity, generate it now?", "Generate", "No"))
				    {
				        GenerateAll();
				    }
				    else
				    {
				        if (!EditorUtility.DisplayDialog("Slua", "Remind you next time when no lua interface found for Unity?", "OK",
				            "Don't remind me next time!"))
				        {
                            EditorPrefs.SetBool("SLUA_REMIND_GENERTE_LUA_INTERFACE", false);
				        }
				        else
				        {
				            
                            EditorPrefs.SetBool("SLUA_REMIND_GENERTE_LUA_INTERFACE", true);
				        }
				        
				    }
				}
			}

		}
	
		[MenuItem("SLuaDev/Make")]
		static public void GenerateAll()
		{
            autoRefresh = false;
            Generate();
			GenerateUI();
			Custom();
			Generate3rdDll();
            autoRefresh = true;
            AssetDatabase.Refresh();
        }

        static void Generate()
		{
			if (IsCompiling) {
				return;
			}

			Assembly assembly = Assembly.Load("UnityEngine");
			Type[] types = assembly.GetExportedTypes();
			
			List<string> uselist;
			List<string> noUseList;
			
			CustomExport.OnGetNoUseList(out noUseList);
			CustomExport.OnGetUseList(out uselist);

            // Get use and nouse list from custom export.
            object[] aCustomExport = new object[1];
            InvokeEditorMethod<ICustomExportPost>("OnGetUseList", ref aCustomExport);
            if (null != aCustomExport[0])
            {
                if (null != uselist)
                {
                    uselist.AddRange((List<string>)aCustomExport[0]);
                }
                else
                {
                    uselist = (List<string>)aCustomExport[0];
                }
            }

            aCustomExport[0] = null;
            InvokeEditorMethod<ICustomExportPost>("OnGetNoUseList", ref aCustomExport);
            if (null != aCustomExport[0])
            {
                if ((null != noUseList))
                {
                    noUseList.AddRange((List<string>)aCustomExport[0]);
                }
                else
                {
                    noUseList = (List<string>)aCustomExport[0];
                }
            }

            List<Type> exports = new List<Type>();
			string path = GenPath + "/";
			foreach (Type t in types)
			{
				if (filterType(t, noUseList, uselist) && Generate(t, path))
					exports.Add(t);
			}
			
			if(autoRefresh)
			    AssetDatabase.Refresh();
			Debug.Log("Generate engine interface finished");
		}

		static bool filterType(Type t, List<string> noUseList, List<string> uselist)
		{
			// check type in uselist
			string fullName = t.FullName;
			if (uselist != null && uselist.Count > 0)
			{
				return uselist.Contains(fullName);
			}
			else
			{
				// check type not in nouselist
				foreach (string str in noUseList)
				{
					if (fullName.Contains(str))
					{
						return false;
					}
				}
				return true;
			}
		}
		
		static void GenerateUI()
		{
			if (IsCompiling) {
				return;
			}

			List<string> uselist;
			List<string> noUseList;

			CustomExport.OnGetNoUseList(out noUseList);
			CustomExport.OnGetUseList(out uselist);

            // Get use and nouse list from custom export.
            object[] aCustomExport = new object[1];
            InvokeEditorMethod<ICustomExportPost>("OnGetUseList", ref aCustomExport);
            if (null != aCustomExport[0])
            {
                if (null != uselist)
                {
                    uselist.AddRange((List<string>)aCustomExport[0]);
                }
                else
                {
                    uselist = (List<string>)aCustomExport[0];
                }
            }

            aCustomExport[0] = null;
            InvokeEditorMethod<ICustomExportPost>("OnGetNoUseList", ref aCustomExport);
            if (null != aCustomExport[0])
            {
                if ((null != noUseList))
                {
                    noUseList.AddRange((List<string>)aCustomExport[0]);
                }
                else
                {
                    noUseList = (List<string>)aCustomExport[0];
                }
            }

            Assembly assembly = Assembly.Load("UnityEngine.UI");
			Type[] types = assembly.GetExportedTypes();
			
			List<Type> exports = new List<Type>();
			string path = GenPath + "/";
			foreach (Type t in types)
			{
				if (filterType(t,noUseList,uselist) && Generate(t,path))
				{
					exports.Add(t);
				}
			}
			
			if(autoRefresh)
			    AssetDatabase.Refresh();
			Debug.Log("Generate UI interface finished");
		}

		static String FixPathName(string path) {
			if(path.EndsWith("\\") || path.EndsWith("/")) {
				return path.Substring(0,path.Length-1);
			}
			return path;
		}
		
		static void ClearUnity()
		{
			clear(new string[] { GenPath+"Unity" });
			Debug.Log("Clear Unity & UI complete.");
		}
		
		static void Custom()
		{
			if (IsCompiling) {
				return;
			}

			List<Type> exports = new List<Type>();
            string path = GenPath + "/";
			
			if (!Directory.Exists(path))
			{
				Directory.CreateDirectory(path);
			}
			
			ExportGenericDelegate fun = (Type t, string ns) =>
			{
				if (Generate(t, ns, path))
					exports.Add(t);
			};

            HashSet<string> namespaces = CustomExport.OnAddCustomNamespace();

            // Add custom namespaces.
            object[] aCustomExport = null;
            List<object> aCustomNs = LuaCodeGen.InvokeEditorMethod<ICustomExportPost>("OnAddCustomNamespace", ref aCustomExport);
            foreach (object cNsSet in aCustomNs)
            {
                foreach (string strNs in (HashSet<string>)cNsSet)
                {
                    namespaces.Add(strNs);
                }
            }

            Assembly assembly;
            Type[] types;

            try {
                // export plugin-dll
                assembly = Assembly.Load("Assembly-CSharp-firstpass");
                types = assembly.GetExportedTypes();
                foreach (Type t in types)
                {
                    if (t.IsDefined(typeof(CustomLuaClassAttribute), false) || namespaces.Contains(t.Namespace))
                    {
                        fun(t, null);
                    }
                }
            }
            catch(Exception){}

            // export self-dll
            assembly = Assembly.Load("Assembly-CSharp");
            types = assembly.GetExportedTypes();
            foreach (Type t in types)
            {
                if (t.IsDefined(typeof(CustomLuaClassAttribute), false) || namespaces.Contains(t.Namespace))
                {
                    fun(t, null);
                }
            }

            //detect interface ICustomExportPost,and call OnAddCustomClass
            aCustomExport = new object[] { fun };
			InvokeEditorMethod<ICustomExportPost>("OnAddCustomClass", ref aCustomExport);

            if(autoRefresh)
			    AssetDatabase.Refresh();
			
			Debug.Log("Generate custom interface finished");
		}

		static public List<object> InvokeEditorMethod<T>(string methodName, ref object[] parameters)
        {
            List<object> aReturn = new List<object>();
            System.Reflection.Assembly editorAssembly = System.Reflection.Assembly.Load("Assembly-CSharp-Editor");
			Type[] editorTypes = editorAssembly.GetExportedTypes();
			foreach (Type t in editorTypes)
			{
				if(typeof(T).IsAssignableFrom(t))
                {
					System.Reflection.MethodInfo method =  t.GetMethod(methodName,System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                    if (method != null)
                    {
                        object cRes = method.Invoke(null, parameters);
                        if (null != cRes)
                        {
                            aReturn.Add(cRes);
                        }
                    }
				}
			}

            return aReturn;
		}

        static public List<object> GetEditorField<T>(string strFieldName)
        {
            List<object> aReturn = new List<object>();
            System.Reflection.Assembly cEditorAssembly = System.Reflection.Assembly.Load("Assembly-CSharp-Editor");
            Type[] aEditorTypes = cEditorAssembly.GetExportedTypes();
            foreach (Type t in aEditorTypes)
            {
                if (typeof(T).IsAssignableFrom(t))
                {
                    FieldInfo cField = t.GetField(strFieldName, BindingFlags.Static | BindingFlags.Public);
                    if (null != cField)
                    {
                        object cValue = cField.GetValue(t);
                        if (null != cValue)
                        {
                            aReturn.Add(cValue);
                        }
                    }
                }
            }

            return aReturn;
        }

		static public void Generate3rdDll()
		{
			if (IsCompiling) {
				return;
			}

			List<Type> cust = new List<Type>();
			List<string> assemblyList = new List<string>();
			CustomExport.OnAddCustomAssembly(ref assemblyList);

            //detect interface ICustomExportPost,and call OnAddCustomAssembly
            object[] aCustomExport = new object[] { assemblyList };
            InvokeEditorMethod<ICustomExportPost>("OnAddCustomAssembly", ref aCustomExport);

			foreach (string assemblyItem in assemblyList)
			{
				Assembly assembly = Assembly.Load(assemblyItem);
				Type[] types = assembly.GetExportedTypes();
				foreach (Type t in types)
				{
					cust.Add(t);
				}
			}
			if (cust.Count > 0)
			{
				List<Type> exports = new List<Type>();
				string path = GenPath + "Dll/";
				if (!Directory.Exists(path))
				{
					Directory.CreateDirectory(path);
				}
				foreach (Type t in cust)
				{
					if (Generate(t,path))
						exports.Add(t);
				}
                if(autoRefresh)
				    AssetDatabase.Refresh();
				Debug.Log("Generate 3rdDll interface finished");
			}
		}
		static public void Clear3rdDll()
		{
			clear(new string[] { GenPath + "Dll" });
			Debug.Log("Clear AssemblyDll complete.");
		}
		[MenuItem("SLua/Clear")]
		static public void ClearCustom()
		{
			clear(new string[] { GenPath + "Custom" });
			Debug.Log("Clear custom complete.");
		}
		
		[MenuItem("SLua/All/Clear")]
		static public void ClearALL()
		{
			clear(new string[] { Path.GetDirectoryName(GenPath) });
			Debug.Log("Clear all complete.");
		}
		
		static void clear(string[] paths)
		{
			try
			{
				foreach (string path in paths)
				{
					System.IO.Directory.Delete(path, true);
					AssetDatabase.DeleteAsset(path);
				}
			}
			catch
			{
				
			}
			
			AssetDatabase.Refresh();
		}
		
		static bool Generate(Type t, string path)
		{
			return Generate(t, null, path);
		}
		
		static bool Generate(Type t, string ns, string path)
		{
			if (t.IsInterface)
				return false;
			
			DevCodeGenerator cg = new DevCodeGenerator();
			cg.givenNamespace = ns;
            cg.path = path;
			return cg.Generate(t);
		}
	}
	
	class DevCodeGenerator
	{
		static List<string> memberFilter = new List<string>
        {
            "AnimationClip.averageDuration",
            "AnimationClip.averageAngularSpeed",
            "AnimationClip.averageSpeed",
            "AnimationClip.apparentSpeed",
            "AnimationClip.isLooping",
            "AnimationClip.isAnimatorMotion",
            "AnimationClip.isHumanMotion",
            "AnimatorOverrideController.PerformOverrideClipListCleanup",
            "Caching.SetNoBackupFlag",
            "Caching.ResetNoBackupFlag",
            "Light.areaSize",
            "Security.GetChainOfTrustValue",
            "Texture2D.alphaIsTransparency",
            "WWW.movie",
            "WebCamTexture.MarkNonReadable",
            "WebCamTexture.isReadable",
            // i don't know why below 2 functions missed in iOS platform
            "*.OnRebuildRequested",
            // il2cpp not exixts
            "Application.ExternalEval",
            "GameObject.networkView",
            "Component.networkView",
            // unity5
            "AnimatorControllerParameter.name",
            "Input.IsJoystickPreconfigured",
            "Resources.LoadAssetAtPath",
        };


		static Dictionary<System.Type,List<MethodInfo>> GenerateExtensionMethodsMap(){
			Dictionary<Type,List<MethodInfo>> dic = new Dictionary<Type, List<MethodInfo>>();
			List<string> asems;
			CustomExport.OnGetAssemblyToGenerateExtensionMethod(out asems);

            // Get list from custom export.
            object[] aCustomExport = new object[1];
            LuaCodeGen.InvokeEditorMethod<ICustomExportPost>("OnGetAssemblyToGenerateExtensionMethod", ref aCustomExport);
            if (null != aCustomExport[0])
            {
                if (null != asems)
                {
                    asems.AddRange((List<string>)aCustomExport[0]);
                }
                else
                {
                    asems = (List<string>)aCustomExport[0];
                }
            }

            foreach (string assstr in asems)
			{
				Assembly assembly = Assembly.Load(assstr);
				foreach (Type type in assembly.GetExportedTypes())
				{
					if (type.IsSealed && !type.IsGenericType && !type.IsNested)
					{
						MethodInfo[] methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public);
						foreach (MethodInfo method in methods)
						{
							if (IsExtensionMethod(method))
							{
								Type extendedType = method.GetParameters()[0].ParameterType;
								if (!dic.ContainsKey(extendedType))
								{
									dic.Add(extendedType, new List<MethodInfo>());
								}
								dic[extendedType].Add(method);
							}
						}
					}
				}
			}
			return dic;
		}

		static bool IsExtensionMethod(MethodBase method){
			return method.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute),false);
		}



		static Dictionary<System.Type,List<MethodInfo>> extensionMethods = new Dictionary<Type, List<MethodInfo>>();

		static DevCodeGenerator(){
			extensionMethods = GenerateExtensionMethodsMap();
		}

		HashSet<string> funcname = new HashSet<string>();
		Dictionary<string, bool> directfunc = new Dictionary<string, bool>();
		
		public string givenNamespace;
        public string path;
		public bool includeExtension = SLuaSetting.Instance.exportExtensionMethod;
		public EOL eol = SLuaSetting.Instance.eol;

		class PropPair
		{
			public string get = "null";
			public string set = "null";
			public bool isInstance = true;
		}
		Dictionary<string, PropPair> propname = new Dictionary<string, PropPair>();
		
		int indent = 0;



		void WriteBindType(StreamWriter file, Type t, List<Type> exported, HashSet<Type> binded)
		{
			if (t == null || binded.Contains(t) || !exported.Contains(t))
				return;

			WriteBindType(file, t.BaseType, exported, binded);
			Write(file, "{0}.reg,", ExportName(t), binded);
			binded.Add(t);
		}

		public string DelegateExportFilename(string path, Type t)
		{
			string f;
			if (t.IsGenericType)
			{
				f = path + string.Format("Lua{0}_{1}.sublime-completions", _Name(GenericBaseName(t)), _Name(GenericName(t)));
			}
			else
			{
				f = path + "LuaDelegate_" + _Name(t.FullName) + ".sublime-completions";
			}
			return f;
		}
		
		
		public bool Generate(Type t)
		{
			if (!Directory.Exists(path))
			{
				Directory.CreateDirectory(path);
			}
			
			if (!t.IsGenericTypeDefinition && (!IsObsolete(t) && t != typeof(YieldInstruction) && t != typeof(Coroutine))
			    || (t.BaseType != null && t.BaseType == typeof(System.MulticastDelegate)))
			{
				if (t.IsEnum)
				{
					StreamWriter file = Begin(t);
					RegEnumFunction(t, file);
					End(file);
				}
				else if (t.BaseType == typeof(System.MulticastDelegate))
				{
					if (t.ContainsGenericParameters)
						return false;

					string f = DelegateExportFilename(path, t);
					
					StreamWriter file = new StreamWriter(f, false, Encoding.UTF8);
					file.NewLine = NewLine;
					WriteDelegate(t, file);
					file.Close();
					return false;
				}
				else
				{
					funcname.Clear();
					propname.Clear();
					directfunc.Clear();
					
					StreamWriter file = Begin(t);
					WriteConstructor(t, file);
					WriteFunction(t, file,false);
					WriteFunction(t, file, true);
					WriteField(t, file);
					End(file);

					if (t.BaseType != null && t.BaseType.Name.Contains("UnityEvent`"))
					{
						string basename = "LuaUnityEvent_" + _Name(GenericName(t.BaseType)) + ".sublime-completions";
						string f = path + basename;
						string checkf = LuaCodeGen.GenPath + "/" + basename;
						if (!File.Exists(checkf)) // if had exported
						{
							file = new StreamWriter(f, false, Encoding.UTF8);
							file.NewLine = NewLine;
							WriteEvent(t, file);
							file.Close();
						}
					}
				}
				
				return true;
			}
			return false;
		}

		void WriteDelegate(Type t, StreamWriter file)
		{
			string temp = @"
$FN 
                ua = ($FN)checkObj(l, p);
                ua = ($FN)ld.d;
            ua = ($ARGS) =>
";
			
			temp = temp.Replace("$TN", t.Name);
			temp = temp.Replace("$FN", SimpleType(t));
			MethodInfo mi = t.GetMethod("Invoke");
			List<int> outindex = new List<int>();
			List<int> refindex = new List<int>();
			temp = temp.Replace("$ARGS", ArgsList(mi, ref outindex, ref refindex));
			Write(file, temp);
			
			this.indent = 4;
			
			for (int n = 0; n < mi.GetParameters().Length; n++)
			{
				if (!outindex.Contains(n))
					Write(file, "pushValue(l,a{0});", n + 1);
			}
			
			Write(file, "ld.pcall({0}, error);", mi.GetParameters().Length - outindex.Count);

			int offset = 0;
			if (mi.ReturnType != typeof(void))
			{
				offset = 1;
				WriteValueCheck(file, mi.ReturnType, offset, "ret", "error+");
			}
			
			foreach (int i in outindex)
			{
				string a = string.Format("a{0}", i + 1);
				WriteCheckType(file, mi.GetParameters()[i].ParameterType, i + offset, a, "error+");
			}
			
			foreach (int i in refindex)
			{
				string a = string.Format("a{0}", i + 1);
				WriteCheckType(file, mi.GetParameters()[i].ParameterType, i + offset, a, "error+");
			}

		}
		
		string ArgsList(MethodInfo m, ref List<int> outindex, ref List<int> refindex)
		{
			string str = "";
			ParameterInfo[] pars = m.GetParameters();
			for (int n = 0; n < pars.Length; n++)
			{
				string t = SimpleType(pars[n].ParameterType);
				
				
				ParameterInfo p = pars[n];
				if (p.ParameterType.IsByRef && p.IsOut)
				{
					str += string.Format("out {0} a{1}", t, n + 1);
					outindex.Add(n);
				}
				else if (p.ParameterType.IsByRef)
				{
					str += string.Format("ref {0} a{1}", t, n + 1);
					refindex.Add(n);
				}
				else
					str += string.Format("{0} a{1}", t, n + 1);
				if (n < pars.Length - 1)
					str += ",";
				
				
			}
			return str;
		}
	
		void tryMake(Type t)
		{

			if (t.BaseType == typeof(System.MulticastDelegate))
			{
				DevCodeGenerator cg = new DevCodeGenerator();
				if (File.Exists(cg.DelegateExportFilename(LuaCodeGen.GenPath + "/", t)))
					return;
                cg.path = this.path;
				cg.Generate(t);
			}
		}
		
		void WriteEvent(Type t, StreamWriter file)
		{
			string temp = @"
    $CLS 
      $GN> $GN;
              $GN
                $GN> self =$GN>>(l);
                <$GN> a1;
                $GN> 
" + 
                
				GenericCallDecl(t.BaseType)


+ @"getTypeTable(l, typeof(LuaUnityEvent_$CLS).FullName);
            createTypeMetatable(l, null, typeof(LuaUnityEvent_$CLS), typeof(UnityEngine.Events.UnityEventBase));
        static bool checkType(IntPtr l,int p,out UnityEngine.Events.UnityAction<$GN> ua) {
                ua = (UnityEngine.Events.UnityAction<$GN>)ld.d;
            ua = ($ARGS) =>
            {
                $PUSHVALUES
}";
			
			
			temp = temp.Replace("$CLS", _Name(GenericName(t.BaseType)));
			temp = temp.Replace("$FNAME", FullName(t));
			temp = temp.Replace("$GN", GenericName(t.BaseType,","));
			temp = temp.Replace("$ARGS", ArgsDecl(t.BaseType));
			temp = temp.Replace("$PUSHVALUES", PushValues(t.BaseType));
			Write(file, temp);
		}

		string GenericCallDecl(Type t) {

			try
			{
				Type[] tt = t.GetGenericArguments();
				string ret = "";
				string args = "";
				for (int n = 0; n < tt.Length; n++)
				{
					string dt = SimpleType(tt[n]);
					ret+=string.Format("				{0} a{1};",dt,n)+NewLine;
                    //ret+=string.Format("				checkType(l,{0},out a{1});",n+2,n)+NewLine;
                    ret += "				" + GetCheckType(tt[n], n + 2, "a" + n) + NewLine;
                    args +=("a"+n);
					if (n < tt.Length - 1) args += ",";
				}
				ret+=string.Format("				self.Invoke({0});",args)+NewLine;
				return ret;
			}
			catch (Exception e)
			{
				Debug.Log(e.ToString());
				return "";
			}
		}

        string GetCheckType(Type t, int n, string v = "v", string prefix = "")
        {
            if (t.IsEnum)
            {
                return string.Format("checkEnum(l,{2}{0},out {1});", n, v, prefix);
            }
            else if (t.BaseType == typeof(System.MulticastDelegate))
            {
                return string.Format("int op=LuaDelegation.checkDelegate(l,{2}{0},out {1});", n, v, prefix);
            }
            else if (IsValueType(t))
            {
                return string.Format("checkValueType(l,{2}{0},out {1});", n, v, prefix);
            }
            else if (t.IsArray)
            {
                return string.Format("checkArray(l,{2}{0},out {1});", n, v, prefix);
            }
            else
            {
                return string.Format("checkType(l,{2}{0},out {1});", n, v, prefix);
            }
        }

        string PushValues(Type t) {
			try
			{
				Type[] tt = t.GetGenericArguments();
				string ret = "";
				for (int n = 0; n < tt.Length; n++)
				{
					ret+=("pushValue(l,v"+n+");");
				}
				return ret;
			}
			catch (Exception e)
			{
				Debug.Log(e.ToString());
				return "";
			}
		}

		string ArgsDecl(Type t)
		{
			try
			{
				Type[] tt = t.GetGenericArguments();
				string ret = "";
				for (int n = 0; n < tt.Length; n++)
				{
					string dt = SimpleType(tt[n]);
					dt+=(" v"+n);
					ret += dt;
					if (n < tt.Length - 1)
						ret += ",";
				}
				return ret;
			}
			catch (Exception e)
			{
				Debug.Log(e.ToString());
				return "";
			}
		}
		
		void RegEnumFunction(Type t, StreamWriter file)
		{
			// Write export function
			Write(file, "getEnumTable(l,\"{0}\");", string.IsNullOrEmpty(givenNamespace) ? FullName(t) : givenNamespace);

			foreach (string name in Enum.GetNames (t))
			{
				Write(file, "addMember(l,{0},\"{1}\");", Convert.ToInt32(Enum.Parse(t, name)), name);
			}
		}

		StreamWriter Begin(Type t)
		{
			string clsname = ExportName(t);
			string f = path + clsname + ".sublime-completions";
			StreamWriter file = new StreamWriter(f, false, Encoding.UTF8);
			file.NewLine = NewLine;
			// begin
			Write(file,"{");
			Write(file,"\t\"scope\": \"source.lua\",");
			Write(file,"\t\"completions\":");
			Write(file,"\t[");
			if (clsname.Split (".".ToCharArray ()).Length == 2) {
				Write(file,"\t\t\"local {0} = {1}\",\n", clsname.Split (".".ToCharArray ())[1], clsname);
				Write(file,"\t\t\"{0} = {1}\",\n", clsname.Split (".".ToCharArray ())[1], clsname);
			}
			return file;
		}
		
		private void End(StreamWriter file)
		{


			Write(file,"\t]");
			Write(file,"}\r\n");
			file.Flush();
			file.Close();
		}


		// add namespace for extension method
		void WriteExtraNamespace(StreamWriter file,Type t) {
			List<MethodInfo> lstMI;
			HashSet<string> nsset=new HashSet<string>();
			if(extensionMethods.TryGetValue(t,out lstMI)) {
				foreach(MethodInfo m in lstMI) {
					// if not writed
					if(!string.IsNullOrEmpty(m.ReflectedType.Namespace) && !nsset.Contains(m.ReflectedType.Namespace)) {
						Write(file,"using {0};",m.ReflectedType.Namespace);
						nsset.Add(m.ReflectedType.Namespace);
					}
				}
			}
		}

		private void WriteFunction(Type t, StreamWriter file, bool writeStatic = false)
		{
			BindingFlags bf = BindingFlags.Public | BindingFlags.DeclaredOnly;
			if (writeStatic)
				bf |= BindingFlags.Static;
			else
				bf |= BindingFlags.Instance;
			
			MethodInfo[] members = t.GetMethods(bf);
			List<MethodInfo> methods = new List<MethodInfo>();
			methods.AddRange(members);

			if(!writeStatic && this.includeExtension){
				if(extensionMethods.ContainsKey(t)){
					methods.AddRange(extensionMethods[t]);
				}
			}
			foreach (MethodInfo mi in methods)
			{
				bool instanceFunc;
				if (writeStatic && isPInvoke(mi, out instanceFunc))
				{
					directfunc.Add(t.FullName + "." + mi.Name, instanceFunc);
					continue;
				}
				
				string fn =  mi.Name;
				if (mi.MemberType == MemberTypes.Method
				    && !IsObsolete(mi)
				    && !DontExport(mi)
				    && !funcname.Contains(fn)
				    && isUsefullMethod(mi)
				    && !MemberInFilter(t, mi))
				{
					if (mi.IsStatic) {
						Write (file, string.Format ("\t\t{{ \"trigger\": \"{0}.{1}\",\"contents\": \"{0}.{2}\"}},\n", t.Name, fn + "(" + mi.Attributes.ToString () + ")", fn + "(" + mi.Attributes.ToString () + ")"));
					}
					else {
						Write (file, string.Format ("\t\t{{ \"trigger\": \"{0}:{1}\",\"contents\": \"{0}:{2}\"}},\n", t.Name, fn + "(" + mi.Attributes.ToString () + ")", fn + "(" + mi.Attributes.ToString () + ")"));
					}
					WriteFunctionImpl(file, mi, t, bf);
					funcname.Add(fn);
				}
			}
		}

		bool isPInvoke(MethodInfo mi, out bool instanceFunc)
		{
			if (mi.IsDefined(typeof(MonoPInvokeCallbackAttribute), false))
			{
				instanceFunc = !mi.IsDefined(typeof(StaticExportAttribute), false);
				return true;
			}
			instanceFunc = true;
			return false;
		}
		
		bool MemberInFilter(Type t, MemberInfo mi)
		{
			return memberFilter.Contains(t.Name + "." + mi.Name) || memberFilter.Contains("*." + mi.Name);
		}
		
		bool IsObsolete(MemberInfo t)
		{
			return t.IsDefined(typeof(ObsoleteAttribute), false);
		}

		string NewLine{
			get{
				switch(eol){
				case EOL.Native:
					return System.Environment.NewLine;
				case EOL.CRLF:
					return "\r\n";
				case EOL.CR:
					return "\r";
				case EOL.LF:
					return "\n";
				default:
					return "";
				}
			}
		}
			
		string constructorOrNot(Type t)
		{
			ConstructorInfo[] cons = GetValidConstructor(t);
			if (cons.Length > 0 || t.IsValueType)
				return "constructor";
			return "null";
		}
		
		bool CutBase(Type t)
		{
			if (t.FullName.StartsWith("System.Object"))
				return true;
			return false;
		}
		
		void WriteSet(StreamWriter file, Type t, string cls, string fn, bool isstatic = false,bool canread = true)
		{
			if (t.BaseType == typeof(MulticastDelegate))
			{
				if (isstatic)
				{
					Write(file, "if(op==0) {0}.{1}=v;", cls, fn);
					if(canread){
						Write(file, "else if(op==1) {0}.{1}+=v;", cls, fn);
						Write(file, "else if(op==2) {0}.{1}-=v;", cls, fn);
					}
				}
				else
				{
					Write(file, "if(op==0) self.{0}=v;", fn);
					if(canread){
						Write(file, "else if(op==1) self.{0}+=v;", fn);
						Write(file, "else if(op==2) self.{0}-=v;", fn);
					}
				}
			}
			else
			{
				if (isstatic)
				{
					Write(file, "{0}.{1}=v;", cls, fn);
				}
				else
				{
					Write(file, "self.{0}=v;", fn);
				}
			}
		}

        readonly static string[] keywords = { "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked", "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else", "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for", "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock", "long", "namespace", "new", "null", "object", "operator", "out", "override", "params", "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short", "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual", "void", "volatile", "while" };

        static string NormalName(string name)
        {
            if (Array.BinarySearch<string>(keywords, name) >= 0)
            {
                return "@" + name;
            }
            return name;
        }

        private void WriteField(Type t, StreamWriter file)
		{
			// Write field set/get
			
			FieldInfo[] fields = t.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
			foreach (FieldInfo fi in fields)
			{
				if (DontExport(fi) || IsObsolete(fi))
					continue;
				
				PropPair pp = new PropPair();
				pp.isInstance = !fi.IsStatic;
				
				if (fi.FieldType.BaseType != typeof(MulticastDelegate))
				{
					Write(file, "{0}", fi.Name);

					
					pp.get = "get_" + fi.Name;
				}
				
				
				
				
				if (!fi.IsLiteral && !fi.IsInitOnly)
				{
					Write(file, "set_{0}(IntPtr l) {{", fi.Name);
					if (fi.IsStatic)
					{
						Write(file, "{0} v;", TypeDecl(fi.FieldType));
						WriteCheckType(file, fi.FieldType, 2);
						WriteSet(file, fi.FieldType, TypeDecl(t), NormalName(fi.Name), true);
					}
					else
					{
						WriteCheckSelf(file, t);
						Write(file, "{0} v;", TypeDecl(fi.FieldType));
						WriteCheckType(file, fi.FieldType, 2);
						WriteSet(file, fi.FieldType, t.FullName, NormalName(fi.Name));
					}
				}
				
				propname.Add(fi.Name, pp);
				tryMake(fi.FieldType);
			}
			List<PropertyInfo> getter = new List<PropertyInfo>();
			List<PropertyInfo> setter = new List<PropertyInfo>();
			PropertyInfo[] props = t.GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
			foreach (PropertyInfo fi in props)
			{
				//if (fi.Name == "Item" || IsObsolete(fi) || MemberInFilter(t,fi) || DontExport(fi))
				if (IsObsolete(fi) || MemberInFilter(t, fi) || DontExport(fi))
					continue;
				if (fi.Name == "Item"
				    || (t.Name == "String" && fi.Name == "Chars")) // for string[]
				{
					//for this[]
					if (!fi.GetGetMethod().IsStatic && fi.GetIndexParameters().Length == 1)
					{
						if (fi.CanRead && !IsNotSupport(fi.PropertyType))
							getter.Add(fi);
						if (fi.CanWrite && fi.GetSetMethod() != null)
							setter.Add(fi);
					}
					continue;
				}
				PropPair pp = new PropPair();
				bool isInstance = true;
				
				if (fi.CanRead && fi.GetGetMethod() != null)
				{
					if (!IsNotSupport(fi.PropertyType))
					{
						Write(file, "get_{0}(IntPtr l) {{", fi.Name);

					}
					
				}
				
				if (fi.CanWrite && fi.GetSetMethod() != null)
				{
					Write(file, "set_{0}(IntPtr l) {{", fi.Name);
					if (fi.GetSetMethod().IsStatic)
					{
						WriteValueCheck(file, fi.PropertyType, 2);
						WriteSet(file, fi.PropertyType, TypeDecl(t), NormalName(fi.Name), true,fi.CanRead);
						isInstance = false;
					}
					else
					{
						WriteCheckSelf(file, t);
						WriteValueCheck(file, fi.PropertyType, 2);
						WriteSet(file, fi.PropertyType, TypeDecl(t), NormalName(fi.Name),false,fi.CanRead);
					}
					
					if (t.IsValueType)
						Write(file, "setBack(l,self);");
					pp.set = "set_" + fi.Name;
				}
				pp.isInstance = isInstance;
				
				propname.Add(fi.Name, pp);
				tryMake(fi.PropertyType);
			}
			//for this[]
			WriteItemFunc(t, file, getter, setter);
		}
		void WriteItemFunc(Type t, StreamWriter file, List<PropertyInfo> getter, List<PropertyInfo> setter)
		{
			
			//Write property this[] set/get
			if (getter.Count > 0)
			{
				//get
				bool first_get = true;
				WriteCheckSelf(file, t);
				if (getter.Count == 1)
				{
					PropertyInfo _get = getter[0];
					ParameterInfo[] infos = _get.GetIndexParameters();
					WriteValueCheck(file, infos[0].ParameterType, 2, "v");
				}
				else
				{
					for (int i = 0; i < getter.Count; i++)
					{
						PropertyInfo fii = getter[i];
						ParameterInfo[] infos = fii.GetIndexParameters();
						Write(file, "{0}(matchType(l,2,t,typeof({1}))){{", first_get ? "if" : "else if", infos[0].ParameterType);
						first_get = false;
					}
				}
				funcname.Add("getItem");
			}
			if (setter.Count > 0)
			{
				bool first_set = true;
				WriteCheckSelf(file, t);
				if (setter.Count == 1)
				{
					PropertyInfo _set = setter[0];
					ParameterInfo[] infos = _set.GetIndexParameters();
					WriteValueCheck(file, infos[0].ParameterType, 2);
					WriteValueCheck(file, _set.PropertyType, 3, "c");
				}
				else
				{
					Write(file, "LuaTypes t = LuaDLL.lua_type(l, 2);");
					for (int i = 0; i < setter.Count; i++)
					{
						PropertyInfo fii = setter[i];
						if (t.BaseType != typeof(MulticastDelegate))
						{
							ParameterInfo[] infos = fii.GetIndexParameters();
							Write(file, "{0}(matchType(l,2,t,typeof({1}))){{", first_set ? "if" : "else if", infos[0].ParameterType);
							WriteValueCheck(file, infos[0].ParameterType, 2, "v");
							WriteValueCheck(file, fii.PropertyType, 3, "c");
							first_set = false;
						}
					}
					Write(file, "LuaDLL.lua_pushstring(l,\"No matched override function to call\");");
				}
			}
		}
		

		
		void WriteCheckType(StreamWriter file, Type t, int n, string v = "v", string nprefix = "")
		{
			if (t.IsEnum)
				Write(file, "checkEnum(l,{2}{0},out {1});", n, v, nprefix);
			else if (t.BaseType == typeof(System.MulticastDelegate))
				Write(file, "int op=LuaDelegation.checkDelegate(l,{2}{0},out {1});", n, v, nprefix);
			else if (IsValueType(t))
				Write(file, "checkValueType(l,{2}{0},out {1});", n, v, nprefix);
			else if (t.IsArray)
				Write(file, "checkArray(l,{2}{0},out {1});", n, v, nprefix);
			else
				Write(file, "checkType(l,{2}{0},out {1});", n, v, nprefix);
		}
		
		void WriteValueCheck(StreamWriter file, Type t, int n, string v = "v", string nprefix = "")
		{
			Write(file, "{0} {1};", SimpleType(t), v);
			WriteCheckType(file, t, n, v, nprefix);
		}
		
		ConstructorInfo[] GetValidConstructor(Type t)
		{
			List<ConstructorInfo> ret = new List<ConstructorInfo>();
			if (t.GetConstructor(Type.EmptyTypes) == null && t.IsAbstract && t.IsSealed)
				return ret.ToArray();
			if (t.IsAbstract)
				return ret.ToArray();
			if (t.BaseType != null && t.BaseType.Name == "MonoBehaviour")
				return ret.ToArray();
			
			ConstructorInfo[] cons = t.GetConstructors(BindingFlags.Instance | BindingFlags.Public);
			foreach (ConstructorInfo ci in cons)
			{
				if (!IsObsolete(ci) && !DontExport(ci) && !ContainUnsafe(ci))
					ret.Add(ci);
			}
			return ret.ToArray();
		}
		
		bool ContainUnsafe(MethodBase mi)
		{
			foreach (ParameterInfo p in mi.GetParameters())
			{
				if (p.ParameterType.FullName.Contains("*"))
					return true;
			}
			return false;
		}
		
		bool DontExport(MemberInfo mi)
		{
		    var methodString = string.Format("{0}.{1}", mi.DeclaringType, mi.Name);
		    if (CustomExport.FunctionFilterList.Contains(methodString))
		        return true;

            // Check in custom export function filter list.
            List<object> aFuncFilterList = LuaCodeGen.GetEditorField<ICustomExportPost>("FunctionFilterList");
            foreach (object aFilterList in aFuncFilterList)
            {
                if (((List<string>)aFilterList).Contains(methodString))
                {
                    return true;
                }
            }

			return mi.IsDefined(typeof(DoNotToLuaAttribute), false);
		}
		
		void GenerateFunctionComplete(StreamWriter file,string className, string funcName,string[] valueName,bool isStatic = false)
			{
				StringBuilder noties = new StringBuilder ();
				StringBuilder content = new StringBuilder ();
				for (int i = 0;valueName != null && i < valueName.Length;i++)
				{
		
					noties.AppendFormat ("{0}", valueName [i]);
					content.AppendFormat ("${{{0}:{1}}}", i + 1,valueName [i]);
		
					if (i != valueName.Length - 1) {
						noties.Append (",");
						content.Append(",");
					}
				}
					Write (file,string.Format("\t\t{{ \"trigger\": \"{0}.{1}\",\"contents\": \"{0}.{2}\"}},\n", className, funcName + "(" + noties.ToString() + ")",funcName + "(" + content.ToString() + ")"));
				var plits = className.Split (".".ToCharArray ());
				if (plits.Length == 2) {
					if (isStatic) {
					Write (file,string.Format("\t\t{{ \"trigger\": \"{0}.{1}\",\"contents\": \"{0}.{2}\"}},\n", plits [1], funcName + "(" + noties.ToString () + ")", funcName + "(" + content.ToString () + ")"));
					}
					else {
					Write (file,string.Format("\t\t{{ \"trigger\": \":{0}\",\"contents\": \"{1}\"}},\n", funcName + "(" + noties.ToString () + ")", funcName + "(" + content.ToString () + ")"));
								Write (file,string.Format("\t\t{{ \"trigger\": \"{0}:{1}\",\"contents\": \"{0}:{2}\"}},\n", plits [1].Substring(0,1).ToLower() + plits [1].Substring(1), funcName + "(" + noties.ToString () + ")", funcName + "(" + content.ToString () + ")"));
					}
				} else if (plits.Length == 1) {
					if (isStatic) {
					Write (file,string.Format("\t\t{{ \"trigger\": \"{0}.{1}\",\"contents\": \"{0}.{2}\"}},\n", plits [0], funcName + "(" + noties.ToString () + ")", funcName + "(" + content.ToString () + ")"));			}
					else {
					Write (file,string.Format("\t\t{{ \"trigger\": \":{0}\",\"contents\": \"{1}\"}},\n", funcName + "(" + noties.ToString () + ")", funcName + "(" + content.ToString () + ")"));
						WriteMemberFunctionStr(file,plits [0].Substring(0,1).ToLower() + plits [0].Substring(1), funcName + "(" + noties.ToString () + ")", funcName + "(" + content.ToString () + ")");
					}
				}
			}
		private void WriteConstructor(Type t, StreamWriter file)
		{
			ConstructorInfo[] cons = GetValidConstructor(t);
			if (cons.Length > 0)
			{
				bool first = true;
				for (int n = 0; n < cons.Length; n++)
				{
					ConstructorInfo ci = cons[n];
					ParameterInfo[] pars = ci.GetParameters();
					
					if (cons.Length > 1)
					{
						if (isUniqueArgsCount (cons, ci))
							WriteCompletionStr (file, FullName (t) + "()");
						else
							WriteCompletionStr (file, FullName (t) + "(" + TypeDecl (pars) + ")");
					}
					
					for (int k = 0; k < pars.Length; k++)
					{
						ParameterInfo p = pars[k];
						bool hasParams = p.IsDefined(typeof(ParamArrayAttribute), false);
						CheckArgument(file, p.ParameterType, k, 2, p.IsOut, hasParams);
					}
					WriteCompletionStr (file, FullName (t) + "()");
					first = false;
				}

				if (cons.Length > 1)
				{
					if(t.IsValueType)
					{
						WriteCompletionStr (file, FullName (t) + "()");
					}
				}
			}
			else if (t.IsValueType) // default constructor
			{
				WriteCompletionStr (file, FullName (t) + "()");
			}
		}

		void WriteCompletionStr(StreamWriter file,string contentStr){
			Write (file, String.Format ("\t\t{{ \"trigger\": \"{0}\",\"contents\": \"{0}\"}},\n", contentStr + "()"));
		}

		void WriteStaticFunctionStr(StreamWriter file,string classname,string contentStr1,string contentStr2) {

		}

		void WriteMemberFunctionStr(StreamWriter file,string className,string contentStr1,string contentStr2) {
			Write (file,string.Format("\t\t{{ \"trigger\": \"{0}:{1}\",\"contents\": \"{0}:{2}\"}},\n",file,contentStr1,contentStr2));

		}

		void WriteError(StreamWriter file, string err)
		{
			Write(file, "LuaDLL.lua_pushstring(l,\"{0}\");",err);
		}

		void WriteReturn(StreamWriter file, string val)
		{
			Write(file, "pushValue(l,true);");
			Write(file, "pushValue(l,{0});",val);
		}
		
		bool IsNotSupport(Type t)
		{
			if (t.IsSubclassOf(typeof(Delegate)))
				return true;
			return false;
		}
		
		string[] prefix = new string[] { "System.Collections.Generic" };
		string RemoveRef(string s, bool removearray = true)
		{
			if (s.EndsWith("&")) s = s.Substring(0, s.Length - 1);
			if (s.EndsWith("[]") && removearray) s = s.Substring(0, s.Length - 2);
			if (s.StartsWith(prefix[0])) s = s.Substring(prefix[0].Length + 1, s.Length - prefix[0].Length - 1);
			
			s = s.Replace("+", ".");
			if (s.Contains("`"))
			{
				string regstr = @"`\d";
				Regex r = new Regex(regstr, RegexOptions.None);
				s = r.Replace(s, "");
				s = s.Replace("[", "<");
				s = s.Replace("]", ">");
			}
			return s;
		}
		
		string GenericBaseName(Type t)
		{
			string n = t.FullName;
			if (n.IndexOf('[') > 0)
			{
				n = n.Substring(0, n.IndexOf('['));
			}
			return n.Replace("+", ".");
		}
		string GenericName(Type t,string sep="_")
		{
			try
			{
				Type[] tt = t.GetGenericArguments();
				string ret = "";
				for (int n = 0; n < tt.Length; n++)
				{
					string dt = SimpleType(tt[n]);
					ret += dt;
					if (n < tt.Length - 1)
						ret += sep;
				}
				return ret;
			}
			catch (Exception e)
			{
				Debug.Log(e.ToString());
				return "";
			}
		}
		
		string _Name(string n)
		{
			string ret = "";
			for (int i = 0; i < n.Length; i++)
			{
				if (char.IsLetterOrDigit(n[i]))
					ret += n[i];
				else
					ret += "_";
			}
			return ret;
		}
		
		string TypeDecl(ParameterInfo[] pars,int paraOffset = 0)
		{
			string ret = "";
			for (int n = paraOffset; n < pars.Length; n++)
			{
                if (pars[n].IsOut)
                    ret += "LuaOut";
                else
					ret += "," + SimpleType(pars[n].ParameterType);
			}
			return ret;
		}
		
		bool isUsefullMethod(MethodInfo method)
		{
			if (method.Name != "GetType" && method.Name != "GetHashCode" && method.Name != "Equals" &&
			    method.Name != "ToString" && method.Name != "Clone" &&
			    method.Name != "GetEnumerator" && method.Name != "CopyTo" &&
			    method.Name != "op_Implicit" && method.Name != "op_Explicit" &&
			    !method.Name.StartsWith("get_", StringComparison.Ordinal) &&
			    !method.Name.StartsWith("set_", StringComparison.Ordinal) &&
			    !method.Name.StartsWith("add_", StringComparison.Ordinal) &&
			    !IsObsolete(method) && !method.IsGenericMethod &&
				method.ToString() != "Int32 Clamp(Int32, Int32, Int32)" &&
			    !method.Name.StartsWith("remove_", StringComparison.Ordinal))
			{
				return true;
			}
			return false;
		}

		
		MethodBase[] GetMethods(Type t, string name, BindingFlags bf)
		{

			List<MethodBase> methods = new List<MethodBase>();

			if(this.includeExtension && ((bf&BindingFlags.Instance) == BindingFlags.Instance)){
				if(extensionMethods.ContainsKey(t)){
					foreach(MethodInfo m in extensionMethods[t]){
						if(m.Name == name 
						   && !IsObsolete(m)
						   && !DontExport(m)
						   && isUsefullMethod(m)){
							methods.Add(m);
						}
					}
				}
			}

			MemberInfo[] cons = t.GetMember(name, bf);
			foreach (MemberInfo m in cons)
			{
				if (m.MemberType == MemberTypes.Method
				    && !IsObsolete(m)
				    && !DontExport(m)
				    && isUsefullMethod((MethodInfo)m))
					methods.Add((MethodBase)m);
			}
            methods.Sort((a, b) =>
           {
               return a.GetParameters().Length - b.GetParameters().Length;
           });
			return methods.ToArray();

		}
		
		void WriteFunctionImpl(StreamWriter file, MethodInfo m, Type t, BindingFlags bf)
		{
			MethodBase[] cons = GetMethods(t, m.Name, bf);

			if (cons.Length == 1) // no override function
			{
				if (isUsefullMethod(m) && !m.ReturnType.ContainsGenericParameters && !m.ContainsGenericParameters) // don't support generic method
					WriteFunctionCall(m, file, t,bf);
				else
				{
				}
			}
			else // 2 or more override function
			{
				
				bool first = true;
				for (int n = 0; n < cons.Length; n++)
				{
					if (cons[n].MemberType == MemberTypes.Method)
					{
						MethodInfo mi = cons[n] as MethodInfo;
						
						ParameterInfo[] pars = mi.GetParameters();
						if (isUsefullMethod(mi)
						    && !mi.ReturnType.ContainsGenericParameters
						    /*&& !ContainGeneric(pars)*/) // don't support generic method
						{
							bool isExtension = IsExtensionMethod(mi) && (bf & BindingFlags.Instance) == BindingFlags.Instance;
							if (isUniqueArgsCount(cons, mi))
								Write(file, "{0}(argc=={1}){{", first ? "if" : "else if", mi.IsStatic ? mi.GetParameters().Length : mi.GetParameters().Length + 1);
							else
								Write(file, "{0}(matchType(l,argc,{1}{2})){{", first ? "if" : "else if", mi.IsStatic&&!isExtension ? 1 : 2, TypeDecl(pars,isExtension?1:0));
							WriteFunctionCall(mi, file, t,bf);
							first = false;
						}
					}
				}
			}
		}
		
		bool isUniqueArgsCount(MethodBase[] cons, MethodBase mi)
		{
			foreach (MethodBase member in cons)
			{
				MethodBase m = (MethodBase)member;
				if (m != mi && mi.GetParameters().Length == m.GetParameters().Length)
					return false;
			}
			return true;
		}
		
		
		void WriteCheckSelf(StreamWriter file, Type t)
		{
			if (t.IsValueType)
			{
				Write(file, "{0} self;", TypeDecl(t));
				if(IsBaseType(t))
					Write(file, "checkType(l,1,out self);");
				else
					Write(file, "checkValueType(l,1,out self);");
			}
			else
				Write(file, "{0} self=({0})checkSelf(l);", TypeDecl(t));
		}


		private void WriteFunctionCall(MethodInfo m, StreamWriter file, Type t,BindingFlags bf)
		{

			bool isExtension = IsExtensionMethod(m) && (bf&BindingFlags.Instance) == BindingFlags.Instance;
			bool hasref = false;
			ParameterInfo[] pars = m.GetParameters();


			int argIndex = 1;
			int parOffset = 0;
			if (!m.IsStatic )
			{
				WriteCheckSelf(file, t);
				argIndex++;
			}
			else if(isExtension){
				WriteCheckSelf(file, t);
				parOffset ++;
			}
			for (int n = parOffset; n < pars.Length; n++)
			{
				ParameterInfo p = pars[n];
				string pn = p.ParameterType.Name;
				if (pn.EndsWith("&"))
				{
					hasref = true;
				}
				
				bool hasParams = p.IsDefined(typeof(ParamArrayAttribute), false);
				CheckArgument(file, p.ParameterType, n, argIndex, p.IsOut, hasParams);
			}
			
			string ret = "";
			if (m.ReturnType != typeof(void))
			{
				ret = "var ret=";
			}
			
			if (m.IsStatic && !isExtension)
			{
				if (m.Name == "op_Multiply")
					Write(file, "{0}a1*a2;", ret);
				else if (m.Name == "op_Subtraction")
					Write(file, "{0}a1-a2;", ret);
				else if (m.Name == "op_Addition")
					Write(file, "{0}a1+a2;", ret);
				else if (m.Name == "op_Division")
					Write(file, "{0}a1/a2;", ret);
				else if (m.Name == "op_UnaryNegation")
					Write(file, "{0}-a1;", ret);
				else if (m.Name == "op_UnaryPlus")
					Write(file, "{0}+a1;", ret);
				else if (m.Name == "op_Equality")
					Write(file, "{0}(a1==a2);", ret);
				else if (m.Name == "op_Inequality")
					Write(file, "{0}(a1!=a2);", ret);
				else if (m.Name == "op_LessThan")
					Write(file, "{0}(a1<a2);", ret);
				else if (m.Name == "op_GreaterThan")
					Write(file, "{0}(a2<a1);", ret);
				else if (m.Name == "op_LessThanOrEqual")
					Write(file, "{0}(a1<=a2);", ret);
				else if (m.Name == "op_GreaterThanOrEqual")
					Write(file, "{0}(a2<=a1);", ret);
				else{
					Write(file, "{3}{2}.{0}({1});", m.Name, FuncCall(m), TypeDecl(t), ret);
				}
			}
			else{
				Write(file, "{2}self.{0}({1});", m.Name, FuncCall(m,parOffset), ret);
			}

			int retcount = 1;
			
			
			// push out/ref value for return value
			if (hasref)
			{
				for (int n = 0; n < pars.Length; n++)
				{
					ParameterInfo p = pars[n];
					
					if (p.ParameterType.IsByRef)
					{
					}
				}
			}
		}

		string SimpleType(Type t)
		{
			
			string tn = t.Name;
			switch (tn)
			{
			case "Single":
				return "float";
			case "String":
				return "string";
			case "Double":
				return "double";
			case "Boolean":
				return "bool";
			case "Int32":
				return "int";
			case "Object":
				return FullName(t);
			default:
				tn = TypeDecl(t);
				return tn;
			}
		}
		
		void Write(StreamWriter file, string fmt, params object[] args)
		{

			fmt = System.Text.RegularExpressions.Regex.Replace(fmt, @"\r\n?|\n|\r", NewLine);

			if (fmt.StartsWith("}")) indent--;
			
			for (int n = 0; n < indent; n++)
				file.Write("\t");
			
			
			if (args.Length == 0)
				file.WriteLine(fmt);
			else
			{
				string line = string.Format(fmt, args);
				file.WriteLine(line);
			}
			
			if (fmt.EndsWith("{")) indent++;
		}
		
		private void CheckArgument(StreamWriter file, Type t, int n, int argstart, bool isout, bool isparams)
		{
			Write(file, "{0} a{1};", TypeDecl(t), n + 1);
			
			if (!isout)
			{
				if (t.IsEnum)
					Write(file, "checkEnum(l,{0},out a{1});", n + argstart, n + 1);
				else if (t.BaseType == typeof(System.MulticastDelegate))
				{
					tryMake(t);
					Write(file, "LuaDelegation.checkDelegate(l,{0},out a{1});", n + argstart, n + 1);
				}
				else if (isparams)
				{
					if(t.GetElementType().IsValueType && !IsBaseType(t.GetElementType()))
						Write(file, "checkValueParams(l,{0},out a{1});", n + argstart, n + 1);
					else
						Write(file, "checkParams(l,{0},out a{1});", n + argstart, n + 1);
				}
				else if(t.IsArray)
					Write(file, "checkArray(l,{0},out a{1});", n + argstart, n + 1);
				else if (IsValueType(t)) {
					if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>))
						Write(file, "checkNullable(l,{0},out a{1});", n + argstart, n + 1);
					else
						Write(file, "checkValueType(l,{0},out a{1});", n + argstart, n + 1);
				}
				else
					Write(file, "checkType(l,{0},out a{1});", n + argstart, n + 1);
			}
		}

		bool IsValueType(Type t)
		{
			return t.BaseType == typeof(ValueType) && !IsBaseType(t);
		}

		bool IsBaseType(Type t)
		{
			if (t.IsByRef) {
				t = t.GetElementType();
			}
			return t.IsPrimitive || LuaObject.isImplByLua(t);
		}
		
		string FullName(string str)
		{
			if (str == null)
			{
				throw new NullReferenceException();
			}
			return RemoveRef(str.Replace("+", "."));
		}
		
		string TypeDecl(Type t)
		{
			if (t.IsGenericType)
			{
				string ret = GenericBaseName(t);
				
				string gs = "";
				gs += "<";
				Type[] types = t.GetGenericArguments();
				for (int n = 0; n < types.Length; n++)
				{
					gs += TypeDecl(types[n]);
					if (n < types.Length - 1)
						gs += ",";
				}
				gs += ">";
				
				ret = Regex.Replace(ret, @"`\d", gs);
				
				return ret;
			}
			if (t.IsArray)
			{
				return TypeDecl(t.GetElementType()) + "[]";
			}
			else
				return RemoveRef(t.ToString(), false);
		}
		
		string ExportName(Type t)
		{
			if (t.IsGenericType)
			{
				return string.Format("Lua_{0}_{1}", _Name(GenericBaseName(t)), _Name(GenericName(t)));
			}
			else
			{
				string name = RemoveRef(t.FullName, true);
				name = "Lua_" + name;
				return name.Replace(".", "_");
			}
		}
		
		string FullName(Type t)
		{
			if (t.FullName == null)
			{
				Debug.Log(t.Name);
				return t.Name;
			}
			return FullName(t.FullName);
		}
		
		string FuncCall(MethodBase m,int parOffset = 0)
		{
			
			string str = "";
			ParameterInfo[] pars = m.GetParameters();
			for (int n = parOffset; n < pars.Length; n++)
			{
				ParameterInfo p = pars[n];
				if (p.ParameterType.IsByRef && p.IsOut)
					str += string.Format("out a{0}", n + 1);
				else if (p.ParameterType.IsByRef)
					str += string.Format("ref a{0}", n + 1);
				else
					str += string.Format("a{0}", n + 1);
				if (n < pars.Length - 1)
					str += ",";
			}
			return str;
		}
		
	}
}